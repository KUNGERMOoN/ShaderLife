using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Random = UnityEngine.Random;
using ReadOnlyAttribute = Sirenix.OdinInspector.ReadOnlyAttribute;

public class GameOfLife : MonoBehaviour
{
    #region Inspector

    [Header("References"), Required]
    public ComputeShader Shader;
    public Texture LookupTexture;

    [SerializeField, PropertySpace(SpaceBefore = 0, SpaceAfter = 8f)]
    Material material;
    Material lastMaterial;
    public Material Material
    {
        get => material;
        set
        {
            material = value;
            if (Application.isPlaying)
            {
                if (lastMaterial != null)
                {
                    lastMaterial.mainTexture = null;
                }
                if (material != null)
                {
                    material.mainTexture = Texture;
                }

                lastMaterial = material;
            }
        }
    }


    [FoldoutGroup("Board"), InfoBox("$InspectorBoardSize")]
    public int SizeExponent = 8;
    string InspectorBoardSize()
    {
        decimal d = (1 << SizeExponent) * (1 << SizeExponent);
        var f = new NumberFormatInfo { NumberGroupSeparator = " ", NumberDecimalDigits = 0 };

        return $"Board size: {d.ToString("n", f)} cells";
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
            if (Application.isPlaying) Shader?.SetInt("Seed", seed);
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
    }

    #endregion Inspector


    //Variables
    DoubleBoard<int> Board;

    RenderTexture Texture;

    ComputeBuffer boardBuffer;
    ComputeBuffer flipBoardBuffer;
    ComputeBuffer lookupBuffer;

    //AsyncGPUReadbackRequest BufferRequest; //TODO: Read data back from buffer asynchronically

    Vector3Int ThreadGroups;

    public enum Kernel
    {
        Update = 0,
        FlipUpdate = 1,
        Randomise = 2,
        FlipRandomise = 3,
        SetPixels = 4,
        FlipSetPixels = 5
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
        Board = new DoubleBoard<int>(SizeExponent, true);

        uint groupSizeX, groupSizeY;
        Shader.GetKernelThreadGroupSizes(0, out groupSizeX, out groupSizeY, out _);
        ThreadGroups = new Vector3Int(Board.Size / (int)groupSizeX, Board.Size / (int)groupSizeY, 1);

        //Initialize RenderTexture
        Texture = CreateComputeTexture(Board.Size, "Rendered", AllKernels);
        if (Material != null) Material.mainTexture = Texture;

        //Set Compute Shader properties and buffers
        Shader.SetInt("Size", Board.Size);
        Shader.SetFloat("Chance", Chance);

        boardBuffer = new ComputeBuffer(Board.BufferSize, sizeof(int) * 2);
        boardBuffer.SetData(Board.GetCells());

        flipBoardBuffer = new ComputeBuffer(Board.BufferSize, sizeof(int) * 2);
        flipBoardBuffer.SetData(Board.GetCells());
        foreach (Kernel kernel in AllKernels)
        {
            Shader.SetBuffer((int)kernel, "cellsA", boardBuffer);
            Shader.SetBuffer((int)kernel, "cellsB", flipBoardBuffer);
            Shader.SetTexture((int)kernel, "LookupTexture", LookupTexture);
        }

        if (RandomSeed) RandomiseSeed();

        //Optionally randomise
        if (RandomiseOnAwake)
            Randomise();

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
        Board.Dispose();
    }

    public void UpdateBoard()
    {
        int kernel = (int)(Board.Flipped ? Kernel.FlipUpdate : Kernel.Update);

        Shader.Dispatch(kernel, ThreadGroups.x, ThreadGroups.y, ThreadGroups.z);
        Board.Flip();
    }

    public void Randomise()
    {
        if (RandomSeed) RandomiseSeed();

        int kernel = (int)(Board.Flipped ? Kernel.FlipRandomise : Kernel.Randomise);

        Shader.Dispatch(kernel, ThreadGroups.x, ThreadGroups.y, ThreadGroups.z);
        Board.Flip();
    }

    public void RandomiseSeed() => Seed = Random.Range(int.MinValue / 2, int.MaxValue / 2);

    RenderTexture CreateComputeTexture(int size, string ComputeShaderPropertyName, Kernel[] kernels)
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
    }
}
