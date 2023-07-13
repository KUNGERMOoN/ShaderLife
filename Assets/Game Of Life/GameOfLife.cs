using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
using ReadOnlyAttribute = Sirenix.OdinInspector.ReadOnlyAttribute;

public class GameOfLife : MonoBehaviour
{
    #region Inspector

    [Header("References"), Required]
    public ComputeShader Shader;

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
    FlipBoard<Cell> Board;

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
        Packed4Bytes[] lookupTable = GenerateLookupTable(new int[] { 3 }, new int[] { 2, 3 });
        //TODO: allow customization of bornCount and SurviveCount

        Board = new FlipBoard<Cell>(SizeExponent, true);

        uint groupSizeX, groupSizeY;
        Shader.GetKernelThreadGroupSizes(0, out groupSizeX, out groupSizeY, out _);
        ThreadGroups = new Vector3Int(Board.Size / (int)groupSizeX, Board.Size / (int)groupSizeY, 1);

        //Initialize RenderTexture
        Texture = CreateComputeTexture(Board.Size, "Rendered", AllKernels);
        if (Material != null) Material.mainTexture = Texture;

        //Set Compute Shader properties and buffers
        Shader.SetInt("Size", Board.Size);
        Shader.SetFloat("Chance", Chance);


        lookupBuffer = new ComputeBuffer(lookupTable.Length, sizeof(byte) * 4);
        lookupBuffer.SetData(lookupTable);

        boardBuffer = new ComputeBuffer(Board.BufferSize, sizeof(int) * 2);
        boardBuffer.SetData(Board.GetCells());

        flipBoardBuffer = new ComputeBuffer(Board.BufferSize, sizeof(int) * 2);
        flipBoardBuffer.SetData(Board.GetCells());
        foreach (Kernel kernel in AllKernels)
        {
            Shader.SetBuffer((int)kernel, "CellsBuffer", boardBuffer);
            Shader.SetBuffer((int)kernel, "FlipCellsBuffer", flipBoardBuffer);
            Shader.SetBuffer((int)kernel, "Configurations", lookupBuffer);
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





        /*//DEBUG
        const int beforeNumber = 82696; //<- input
        const int beforeRows = 4;
        const int beforeColumns = 6;
        const int afterRows = 2;
        const int afterColumns = 4;

        bool[,] beforeCells = ToCells(beforeNumber, beforeRows, beforeColumns);

        byte afterNumber = SimulateConfiguration(beforeNumber,
            new int[] { 3 }, new int[] { 2, 3 },
            beforeRows, beforeColumns);

        bool[,] afterCells = ToCells(afterNumber, afterRows, afterColumns);

        Debug.Log($"\n" +
            $"Before:\n" +
            $"  Number: {beforeNumber}\n" +
            $"  Cells:\n" +
            $"  {debugConfiguration(beforeCells)}\n" +
            $"  \n" +
            $"After:\n" +
            $"  Number: {afterNumber}\n" +
            $"  Cells:\n" +
            $"  {debugConfiguration(afterCells)}\n");*/
    }

    /*string debugConfiguration(bool[,] cells)
    {
        string result = "\n";
        for (int y = 0; y < cells.GetLength(1); y++)
        {
            for (int x = 0; x < cells.GetLength(0); x++)
            {
                result += (cells[x, y] ? 1 : 0) + " ";
            }
            result += "\n";
        }
        return result;
    }*/

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

    Packed4Bytes[] GenerateLookupTable(int[] bornCount, int[] surviveCount)
    {
        const int rows = 4;
        const int columns = 6;

        //Amount of all possible configurations (1 byte each)
        const int configurations = 1 << (rows * columns);

        //Amount of elements required to store all possible configurations when they are packed into ints
        //(each int contains exacly 4 configurations)
        const int packedConfigurations = configurations / 4;

        //We pack our configurations
        Packed4Bytes[] result = new Packed4Bytes[1 << packedConfigurations];
        for (int i = 0; i < packedConfigurations; i++)
        {
            byte configuration1 = SimulateConfiguration(i * 4, bornCount, surviveCount, rows, columns);
            byte configuration2 = SimulateConfiguration(i * 4 + 1, bornCount, surviveCount, rows, columns);
            byte configuration3 = SimulateConfiguration(i * 4 + 2, bornCount, surviveCount, rows, columns);
            byte configuration4 = SimulateConfiguration(i * 4 + 3, bornCount, surviveCount, rows, columns);

            Packed4Bytes packed = new()
            {
                Byte1 = configuration1,
                Byte2 = configuration2,
                Byte3 = configuration3,
                Byte4 = configuration4
            };

            result[i] = packed;
        }

        return result;
    }

    byte SimulateConfiguration(int asNumber, int[] bornCount, int[] surviveCount, int rows, int columns)
    {
        int newRows = rows - 2;
        int newColumns = columns - 2;

        bool[,] cells = ToCells(asNumber, rows, columns);
        bool[,] newCells = new bool[newColumns, newRows];
        for (int y = 0; y < newRows; y++)
        {
            for (int x = 0; x < newColumns; x++)
            {
                bool wasAlive = cells[x + 1, y + 1];
                int neighbours = countNeighbours(cells, x + 1, y + 1);
                newCells[x, y] =
                    bornCount.Contains(neighbours) ||
                    (surviveCount.Contains(neighbours) && wasAlive);
            }
        }
        return (byte)ToNumber(newCells, newRows, newColumns);
    }

    int countNeighbours(bool[,] cells, int x, int y)
    {
        int count = 0;
        count += cells[x - 1, y - 1] ? 1 : 0;
        count += cells[x - 1, y] ? 1 : 0;
        count += cells[x - 1, y + 1] ? 1 : 0;
        count += cells[x, y - 1] ? 1 : 0;
        count += cells[x, y + 1] ? 1 : 0;
        count += cells[x + 1, y - 1] ? 1 : 0;
        count += cells[x + 1, y] ? 1 : 0;
        count += cells[x + 1, y + 1] ? 1 : 0;

        return count;
    }

    bool[,] ToCells(int number, int rows, int columns)
    {
        bool[,] result = new bool[columns, rows];
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                result[columns - x - 1, rows - y - 1] = number % 2 == 1;
                number /= 2;
            }
        }

        return result;
    }

    int ToNumber(bool[,] cells, int rows, int columns)
    {
        int result = 0;
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                result = (result << 1) + (cells[x, y] ? 1 : 0);
            }
        }
        return result;
    }
}
