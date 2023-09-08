using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Random = UnityEngine.Random;
using ReadOnlyAttribute = Sirenix.OdinInspector.ReadOnlyAttribute;

public class GameOfLife : MonoBehaviour
{
    #region Inspector
#pragma warning disable IDE0051

    [Header("References"), Required]
    public ComputeShader Shader;
    [FilePath(AbsolutePath = false, ParentFolder = "Assets", Extensions = ".lut")]
    public string LookupTable;

    [SerializeField, PropertySpace(SpaceBefore = 0, SpaceAfter = 8f)]
    Material material;
    Material lastMaterial; //TODO: we shouldn't need this
    public Material Material
    {
        get => material;
        set
        {
            material = value;
            if (Application.isPlaying && boardBuffer != null && flipBoardBuffer != null)
            {
                if (lastMaterial != null)
                {
                    lastMaterial.SetBuffer("cells", (ComputeBuffer)null);
                }
                if (material != null)
                {
                    material.SetBuffer("cells", Board.Flipped ? flipBoardBuffer : boardBuffer);
                    UpdateBoardSize();
                }

                lastMaterial = material;
            }
        }
    }


    [FoldoutGroup("Board"), InfoBox("$InspectorBoardSizeInfo"), SerializeField]
    int sizeLevel = 8;
    void UpdateBoardSize()
    {
        if (material != null)
        {
            Vector2Int size = BoardChunks;
            material.SetInteger("_sizeX", size.x);
            material.SetInteger("_sizeY", size.y);
            Shader.SetInts("Size", size.x, size.y);
        }
    }
    public int SizeLevel
    {
        get => sizeLevel;
        set
        {
            sizeLevel = value;
            UpdateBoardSize();
        }
    }
    string InspectorBoardSizeInfo()
    {
        Shader.GetKernelThreadGroupSizes(0, out uint groupSizeX, out uint groupSizeY, out _);

        return $@"Each thread simulates 4x2 cells.
Each group has {groupSizeX}x{groupSizeY} threads.
So, in total we are simulating {4 * groupSizeX}x{2 * groupSizeY} in each thread group.
SizeLevel = {SizeLevel}, so we create {SizeLevel}x{SizeLevel * 2} thread groups
(to make sure the board is square),
which gives us {groupSizeX * SizeLevel}x{groupSizeY * SizeLevel * 2} threads or
{4 * groupSizeX * SizeLevel}x{2 * groupSizeY * SizeLevel * 2} cells.
({4 * groupSizeX * SizeLevel * 2 * groupSizeY * SizeLevel * 2} cells in total)";
    }

    [FoldoutGroup("Board")]
    public bool RandomSeed = true;
    [FoldoutGroup("Board"), SerializeField, DisableIf("RandomSeed")]
    int seed = 1337;
    public int Seed
    {
        get => seed;
        set
        {
            seed = value;
            if (Application.isPlaying && Shader != null) Shader.SetInt("Seed", seed);
        }
    }

    [VerticalGroup("Board/button"), Space, DisableInPlayMode]
    public bool RandomiseOnAwake = true;
    [VerticalGroup("Board/button"), Button("Randomise")]
    void InspectorRandomise()
    {
        if (Application.isPlaying) Randomise();
    }
    [FoldoutGroup("Board"), SerializeField, Range(0, 1)]
    float chance = 0.2f;
    public float Chance
    {
        get => chance;
        set
        {
            chance = Mathf.Clamp01(value);
            if (Application.isPlaying) Shader.SetFloat("Chance", chance);
        }
    }

    [FoldoutGroup("Board"), Space]
    public List<Vector2Int> AliveOnStartup;


    [FoldoutGroup("Simulation")]
    public bool UpdateInRealtime;
    [FoldoutGroup("Simulation"), EnableIf("UpdateInRealtime")]
    public int BoardUpdateRate = 10;
    [FoldoutGroup("Simulation"), Button("UpdateBoard"), DisableInEditorMode]
    void InspectorUpdateBoard()
    {
        if (Application.isPlaying) UpdateBoard();
    }


    [FoldoutGroup("FPS Counter")]
    public float RefreshRate = 2;
    [FoldoutGroup("FPS Counter"), ShowInInspector, ReadOnly]
    public float FPS { get; private set; }
    [FoldoutGroup("FPS Counter"), ShowInInspector, ReadOnly]
    public float RealBoardUpdateRate { get; private set; }
    [FoldoutGroup("FPS Counter"), ShowInInspector, ReadOnly]
    public float AverageBoardUpdatePerformance { get; private set; }


    private void OnValidate()
    {
        Chance = chance;
        Seed = seed;
        Material = material;
        SizeLevel = sizeLevel;
    }

#pragma warning restore IDE0051
    #endregion Inspector

    public Vector3Int ThreadGroupSize
    {
        get
        {
            Shader.GetKernelThreadGroupSizes(0, out uint groupSizeX, out uint groupSizeY, out uint groupSizeZ);
            return new Vector3Int((int)groupSizeX, (int)groupSizeY, (int)groupSizeZ);
        }
    }

    public Vector3Int ThreadGroups => new(sizeLevel, sizeLevel * 2, 1);

    public Vector2Int BoardChunks
        => new(ThreadGroupSize.x * ThreadGroups.x, ThreadGroupSize.y * ThreadGroups.y);

    //Variables
    DoubleBoard<int> Board;

    ComputeBuffer boardBuffer;
    ComputeBuffer flipBoardBuffer;
    ComputeBuffer lookupBuffer;

    ComputeBuffer debugBuffer; //TODO: Remove this when we finish

    //AsyncGPUReadbackRequest BufferRequest; //TODO: Read data back from buffer asynchronically

    public enum Kernel
    {
        Update = 0,
        Randomise = 1,
        SetPixels = 2,
    }
    public Kernel[] AllKernels { get; private set; } = (Kernel[])Enum.GetValues(typeof(Kernel));


    float timeSinceCounterRefresh;
    int framesSinceCounterRefresh;
    private void Update()
    {
        timeSinceCounterRefresh += Time.deltaTime;
        framesSinceCounterRefresh++;

        if (timeSinceCounterRefresh > (1 / RefreshRate)) //Counter refresh
        {
            FPS = framesSinceCounterRefresh / timeSinceCounterRefresh;
        }

        if (UpdateInRealtime) //TODO: use update rate
        {
            UpdateBoard();
        }
    }

    private void Awake()
    {
        //Set up the CPU-side board
        Board = new(BoardChunks, true);
        Debug.Log($"Board size: {BoardChunks}");

        //Set up the Compute Shader and Material
        Shader.SetFloat("Chance", Chance);
        UpdateBoardSize();

        //Load Lookup Table
        LUTBuilder lookupTable = LUTBuilder.LoadFromFile(Path.GetFileName(LookupTable), Path.GetDirectoryName(LookupTable));
        lookupBuffer = new ComputeBuffer(LUTBuilder.packedLength, sizeof(byte) * 4);
        lookupBuffer.SetData(lookupTable.Packed);

        //Create buffers
        boardBuffer = new ComputeBuffer(Board.BufferSize, sizeof(int) * 2);
        boardBuffer.SetData(Board.GetCells());

        flipBoardBuffer = new ComputeBuffer(Board.BufferSize, sizeof(int) * 2);
        flipBoardBuffer.SetData(Board.GetCells());

        debugBuffer = new ComputeBuffer(Board.BufferSize, sizeof(int));

        //Link Buffers to Shaders
        foreach (Kernel kernel in AllKernels)
        {
            Shader.SetBuffer((int)kernel, "cells", boardBuffer);
            Shader.SetBuffer((int)kernel, "flipCells", flipBoardBuffer);
            Shader.SetBuffer((int)kernel, "LookupTable", lookupBuffer);
            Shader.SetBuffer((int)kernel, "debugBuffer", debugBuffer);
        }
        if (Material != null)
        {
            Material.SetBuffer("cells", boardBuffer);
            Material.SetBuffer("flipCells", flipBoardBuffer);
            Material.SetBuffer("debugBuffer", debugBuffer);
        }

        //Randomise the seed
        if (RandomSeed) RandomiseSeed();

        //Optionally randomise
        if (RandomiseOnAwake)
            Randomise();

        //TODO: get rid of this
        foreach (Vector2Int pos in AliveOnStartup)
        {
            Shader.SetInts("TargetPixel", pos.x, pos.y);
            Shader.Dispatch((int)Kernel.SetPixels, 1, 1, 1);
        }
    }

    private void OnDestroy()
    {
        boardBuffer?.Release();
        flipBoardBuffer?.Release();
        lookupBuffer?.Release();
        debugBuffer?.Release();
        Board.Dispose();
    }

    public void UpdateBoard()
    {
        FlipBuffer();
        Shader.Dispatch((int)Kernel.Update, ThreadGroups.x, ThreadGroups.y, ThreadGroups.z);

        /*DebugMessage[] messages = new DebugMessage[ThreadGroups.x * ThreadGroups.y];
        debugBuffer.GetData(messages);
        for (int x = 0; x < ThreadGroups.x; x++)
        {
            for (int y = 0; y < ThreadGroups.y; y++)
            {
                DebugMessage message = messages[x * ThreadGroups.y + y];
                Debug.Log($"Thread {x}, {y}: " +
                    $"cells from {message.startX}, {message.startY} to {message.endX}, {message.endY}");
            }
        }*/
        /*int[] result = new int[Board.BufferSize];
        debugBuffer.GetData(result);
        Vector2Int size = BoardChunks;
        int i = 0;
        for (int x = 0; x < size.x + 2; x++)
        {
            for (int y = 0; y < size.y + 2; y++)
            {
                bool simulated = result[i] != 0;
                if (!simulated)
                {
                    if (x == 0 || y == 0 || x == size.x + 1 || y == size.y + 1)
                        Debug.Log($"Cell correctly not simulated at ({x}, {y}), i = {i}");
                    else
                        Debug.LogError($"Cell uncorrectly not simulated at ({x}, {y}), i = {i}");
                }
                i++;
            }
        }*/
        //Debug.Log($"i: {i}, BufferSize: {Board.BufferSize}");
    }

    public void Randomise()
    {
        if (RandomSeed) RandomiseSeed();

        FlipBuffer();
        Shader.Dispatch((int)Kernel.Randomise, ThreadGroups.x, ThreadGroups.y, ThreadGroups.z);
    }

    void FlipBuffer()
    {
        Board.Flip();
        if (Board.Flipped)
        {
            Shader.EnableKeyword("FLIP_BUFFER");
            if (Material != null) Material.EnableKeyword("FLIP_BUFFER");
        }
        else
        {
            Shader.DisableKeyword("FLIP_BUFFER");
            if (Material != null) Material.DisableKeyword("FLIP_BUFFER");
        }
    }

    public void RandomiseSeed() => Seed = Random.Range(int.MinValue / 2, int.MaxValue / 2);

    /*RenderTexture CreateComputeTexture(int size, string ComputeShaderPropertyName, Kernel[] kernels)
    {
        RenderTexture texture = new(size, size, 24)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = ComputeShaderPropertyName
        };
        texture.Create();
        foreach (Kernel kernel in kernels)
            Shader.SetTexture((int)kernel, ComputeShaderPropertyName, texture);

        return texture;
    }*/
}
