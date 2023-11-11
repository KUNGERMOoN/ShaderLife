using Sirenix.OdinInspector;
using System;
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
    Material lastMaterial;
    public Material Material
    {
        get => material;
        set
        {
            material = value;
            if (Application.isPlaying && boardBufferA != null && boardBufferB != null)
            {
                if (lastMaterial != null)
                {
                    lastMaterial.SetBuffer("chunksA", (ComputeBuffer)null);
                    lastMaterial.SetBuffer("chunksB", (ComputeBuffer)null);
                }
                if (material != null)
                {
                    material.SetBuffer("chunksA", boardBufferA);
                    material.SetBuffer("chunksB", boardBufferB);
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
            material.SetInteger("_BoardSize", size.x * 4);
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


    [FoldoutGroup("Simulation")]
    public bool UpdateInRealtime;
    [FoldoutGroup("Simulation"), EnableIf("UpdateInRealtime")]
    public int BoardUpdateRate = 10; //Add controls for frame-based optimising and add "overcharge" mode for more than 1 update every frame
    [FoldoutGroup("Simulation"), Button("Update Board"), DisableInEditorMode]
    void InspectorUpdateBoard()
    {
        if (Application.isPlaying) UpdateBoard();
    }


    [FoldoutGroup("Edit"), DisableInEditorMode, LabelText("Position"), ShowInInspector]
    Vector2Int SetPixels_position = Vector2Int.zero;
    [FoldoutGroup("Edit"), DisableInEditorMode, LabelText("Value"), ShowInInspector]
    bool SetPixels_value = true;
    [FoldoutGroup("Edit"), DisableInEditorMode, Button("Set Pixel")]
    void InspectorSetPixel()
    {
        if (Application.isPlaying) SetPixel(SetPixels_position, SetPixels_value);
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

    public Vector2Int Cells
        => new(BoardChunks.x * 4, BoardChunks.y * 2);

    //Variables
    DoubleBoard<int> Board;

    //Implementation of double-buffering the board
    //For more info, see Shaders/GameOfLifeSimulation.compute
    ComputeBuffer boardBufferA;
    ComputeBuffer boardBufferB;

    LUTBuilder lookupTable;
    ComputeBuffer lookupBuffer;

    public enum Kernel
    {
        Update = 0,
        Randomise = 1,
        SetPixel = 2,
    }
    public Kernel[] AllKernels { get; private set; } = (Kernel[])Enum.GetValues(typeof(Kernel));


    float timeSinceUpdate;
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

        if (UpdateInRealtime)
        {
            if (timeSinceUpdate >= (float)1 / BoardUpdateRate)
            {
                UpdateBoard();
                timeSinceUpdate = 0;
            }
            timeSinceUpdate += Time.deltaTime;
        }
    }

    private void Awake()
    {
        //Set up the CPU-side board
        Board = new(BoardChunks, true);
        FlipBuffer();

        //Set up the Compute Shader and Material
        Shader.SetFloat("Chance", Chance);
        UpdateBoardSize();

        //Load Lookup Table
        lookupTable = LUTBuilder.LoadFromFile(Path.GetFileName(LookupTable), Path.GetDirectoryName(LookupTable));
        lookupBuffer = new ComputeBuffer(LUTBuilder.packedLength, sizeof(byte) * 4);
        lookupBuffer.SetData(lookupTable.Packed);

        //Create buffers
        boardBufferA = new ComputeBuffer(Board.BufferSize, sizeof(int) * 2);
        boardBufferA.SetData(Board.GetCells());

        boardBufferB = new ComputeBuffer(Board.BufferSize, sizeof(int) * 2);
        boardBufferB.SetData(Board.GetCells());

        //Link Buffers to Shaders
        foreach (Kernel kernel in AllKernels)
        {
            Shader.SetBuffer((int)kernel, "chunksA", boardBufferA);
            Shader.SetBuffer((int)kernel, "chunksB", boardBufferB);
            Shader.SetBuffer((int)kernel, "LookupTable", lookupBuffer);
        }
        if (Material != null)
        {
            Material.SetBuffer("chunksA", boardBufferA);
            Material.SetBuffer("chunksB", boardBufferB);
        }

        //Randomise the seed
        if (RandomSeed) RandomiseSeed();

        //Optionally randomise
        if (RandomiseOnAwake)
            Randomise();
    }

    private void OnDestroy()
    {
        boardBufferA?.Release();
        boardBufferB?.Release();
        lookupBuffer?.Release();
        Board.Dispose();
    }

    public void UpdateBoard()
    {
        FlipBuffer();
        Shader.Dispatch((int)Kernel.Update, ThreadGroups.x, ThreadGroups.y, ThreadGroups.z);
    }

    public void Randomise()
    {
        if (RandomSeed) RandomiseSeed();

        FlipBuffer();
        Shader.Dispatch((int)Kernel.Randomise, ThreadGroups.x, ThreadGroups.y, ThreadGroups.z);
    }

    public void SetPixel(Vector2Int position, bool value)
    {
        Shader.SetBool("TargetValue", value);
        Shader.SetInts("TargetPixel", position.x, position.y);

        Shader.Dispatch((int)Kernel.SetPixel, 1, 1, 1);
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
}
