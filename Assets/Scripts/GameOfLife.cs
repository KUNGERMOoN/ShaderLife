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
Each group has {((int)groupSizeX).ThousandSpacing()}x{((int)groupSizeY).ThousandSpacing()} threads.
So, in total we are simulating {(4 * (int)groupSizeX).ThousandSpacing()}x{(2 * (int)groupSizeY).ThousandSpacing()} in each thread group.
SizeLevel = {SizeLevel.ThousandSpacing()}, so we create {(SizeLevel).ThousandSpacing()}x{(SizeLevel * 2).ThousandSpacing()} thread groups
(to make sure the board is square),
which gives us {((int)groupSizeX * SizeLevel).ThousandSpacing()}x{((int)groupSizeY * SizeLevel * 2).ThousandSpacing()} threads or
{(4 * (int)groupSizeX * SizeLevel).ThousandSpacing()}x{(2 * (int)groupSizeY * SizeLevel * 2).ThousandSpacing()} cells.
({(4 * (int)groupSizeX * SizeLevel * 2 * (int)groupSizeY * SizeLevel * 2).ThousandSpacing()} cells in total)";
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
    [VerticalGroup("Board/button"), Button("Randomise"), DisableInEditorMode]
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

    LUTBuilder lookupTable;
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

    //TODO: Get rid of this when we finish testing
    /*[TableMatrix]
    public int[,] chunks = new int[,]
    {
        {108, 178, 128},
        {017, 137, 009},
        {226, 019, 178}
    };

    [FoldoutGroup("Test"), Button, DisableInEditorMode]
    public void Test(int[,] chunks)
    {
        int x = chunks[1, 1];
        int a = chunks[0, 0];
        int b = chunks[1, 0];
        int c = chunks[2, 0];
        int d = chunks[0, 1];
        int e = chunks[2, 1];
        int f = chunks[0, 2];
        int g = chunks[1, 2];
        int h = chunks[2, 2];

        int input =
            ((a << 23) & 8388608) +
            ((b << 19) & 7864320) +
            ((c << 15) & 262144) +
            ((d << 13) & 131072) +
            ((d << 11) & 2048) +
            ((e << 5) & 4096) +
            ((e << 3) & 64) +
            ((f << 1) & 32) +
            ((g >> 3) & 30) +
            ((h >> 7) & 1) +
            ((x << 9) & 122880) +
            ((x << 7) & 1920);

        for (int x_ = 0; x_ < 3; x_++)
        {
            for (int y_ = 0; y_ < 3; y_++)
            {
                Debug.Log($"Chunk {x_}, {y_}:\n{LUTBuilder.LogConfiguration(LUTBuilder.ToCells(chunks[x_, y_], 2, 4))}");
            }
        }

        byte result = lookupTable.Simulate(input);
        Debug.Log(
$@"The result of simulating pattern
{LUTBuilder.LogConfiguration(LUTBuilder.ToCells(input, 4, 6))}(number {input})
is a pattern:

{LUTBuilder.LogConfiguration(LUTBuilder.ToCells(result, 2, 4))}(number {result})

");
    }*/

    private void Awake()
    {
        //Set up the CPU-side board
        Board = new(BoardChunks, true);
        Debug.Log($"Board size: {BoardChunks}");

        //Set up the Compute Shader and Material
        Shader.SetFloat("Chance", Chance);
        UpdateBoardSize();

        //Load Lookup Table
        lookupTable = LUTBuilder.LoadFromFile(Path.GetFileName(LookupTable), Path.GetDirectoryName(LookupTable));
        lookupBuffer = new ComputeBuffer(LUTBuilder.packedLength, sizeof(byte) * 4);
        lookupBuffer.SetData(lookupTable.Packed);

        //Create buffers
        boardBuffer = new ComputeBuffer(Board.BufferSize, sizeof(int) * 2);
        boardBuffer.SetData(Board.GetCells());

        flipBoardBuffer = new ComputeBuffer(Board.BufferSize, sizeof(int) * 2);
        flipBoardBuffer.SetData(Board.GetCells());

        debugBuffer = new ComputeBuffer(Board.BufferSize, sizeof(uint));

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
#if false
        uint[] debug = new uint[Board.BufferSize];
        debugBuffer.GetData(debug);
        Vector2Int size = BoardChunks;
        //int i = 0;
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++) //, i++)
            {
                //Ported from GameOfLifeGeneration.compute
                int i = (x + 1) * (BoardChunks.y + 2) + y + 1;

                //Index: Correct
                //Debug.Log(i);

                //Neighbours: Correct
                //Debug.Log($"Input in chunk {x}, {y}: {LUTBuilder.LogConfiguration(LUTBuilder.ToCells(debug[i], 4, 6))}");

                //Result: Correct? (untested)
                /*int index = (int)debug[i];
                int result = (lookupTable.Packed[index / 4] >> ((index % 4) * 8)) & 255;
                Debug.Log($"Result in {x}, {y} from the Lookup Table: " +
                    $"{LUTBuilder.LogConfiguration(LUTBuilder.ToCells(result, 2, 4))}\n" +
@$"{index}th element in Lookup Table = {lookupTable.Bytes[index]}, or
{index % 4}th part of the {index / 4}th packed element = {result})

");*/
            }
        }
#endif
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
