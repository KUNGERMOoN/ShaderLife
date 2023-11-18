using System;
using UnityEngine;

public class GameOfLife : IDisposable
{
    public readonly ComputeShader ComputeShader;

    public readonly LUTBuilder LookupTable;

    Material material;
    public Material Material //TODO: Test
    {
        get => material;
        set
        {
            Material newMaterial = value;
            if (material != null)
            {
                material.SetBuffer("chunksA", (ComputeBuffer)null);
                material.SetBuffer("chunksB", (ComputeBuffer)null);
            }
            if (newMaterial != null)
            {
                newMaterial.SetBuffer("chunksA", boardBufferA);
                newMaterial.SetBuffer("chunksB", boardBufferB);
                material.SetInteger("_BoardSize", Cells.x);
            }

            material = newMaterial;
        }
    }

    public readonly int SizeLevel;
    public int Size => Cells.x;

    int seed = 1337;
    public int Seed //TODO: Test
    {
        get => seed;
        set => SetSeed(value);
    }
    void SetSeed(int seed)
    {
        this.seed = seed;
        ComputeShader.SetInt("Seed", seed);
    }

    float chance = 0.2f;
    public float Chance //TODO: Test
    {
        get => chance;
        set => SetChance(value);
    }
    void SetChance(float chance)
    {
        this.chance = Mathf.Clamp01(chance);
        ComputeShader.SetFloat("Chance", chance);
    }

    public Vector3Int ThreadGroupSize
    {
        get
        {
            ComputeShader.GetKernelThreadGroupSizes(0, out uint groupSizeX, out uint groupSizeY, out uint groupSizeZ);
            return new Vector3Int((int)groupSizeX, (int)groupSizeY, (int)groupSizeZ);
        }
    }

    readonly DoubleBoard<int> Board;

    //Implementation of double-buffering the board
    //For more info, see Shaders/GameOfLifeSimulation.compute
    readonly ComputeBuffer boardBufferA;
    readonly ComputeBuffer boardBufferB;

    readonly ComputeBuffer lookupBuffer;

    public enum ComputeKernel
    {
        Update = 0,
        Randomise = 1,
        SetPixel = 2,
    }
    public static readonly ComputeKernel[] AllKernels
        = (ComputeKernel[])Enum.GetValues(typeof(ComputeKernel));

    public GameOfLife(ComputeShader simulation, int sizeLevel, LUTBuilder lut)
    {
        if (simulation == null) throw new ArgumentNullException(nameof(simulation));

        ComputeShader = simulation;
        LookupTable = lut;
        SizeLevel = sizeLevel;

        //Set up the board
        Board = new(BoardChunks, true);

        boardBufferA = new ComputeBuffer(Board.BufferSize, sizeof(int) * 2);
        boardBufferA.SetData(Board.GetCells());

        boardBufferB = new ComputeBuffer(Board.BufferSize, sizeof(int) * 2);
        boardBufferB.SetData(Board.GetCells());

        ComputeShader.SetInts("Size", BoardChunks.x, BoardChunks.y);
        FlipBuffer();

        //Setup other params
        SetChance(chance);
        SetSeed(seed);

        lookupBuffer = new ComputeBuffer(LUTBuilder.packedLength, sizeof(byte) * 4);
        lookupBuffer.SetData(LookupTable.Packed);

        //Link Buffers to the shader
        foreach (ComputeKernel kernel in AllKernels)
        {
            ComputeShader.SetBuffer((int)kernel, "chunksA", boardBufferA);
            ComputeShader.SetBuffer((int)kernel, "chunksB", boardBufferB);
            ComputeShader.SetBuffer((int)kernel, "LookupTable", lookupBuffer);
        }
    }

    public void Dispose()
    {
        boardBufferA?.Release();
        boardBufferB?.Release();
        lookupBuffer?.Release();
        Board.Dispose();
    }

    public void UpdateBoard()
    {
        FlipBuffer();
        ComputeShader.Dispatch((int)ComputeKernel.Update, ThreadGroups.x, ThreadGroups.y, ThreadGroups.z);
    }

    public void Randomise()
    {
        FlipBuffer();
        ComputeShader.Dispatch((int)ComputeKernel.Randomise, ThreadGroups.x, ThreadGroups.y, ThreadGroups.z);
    }

    public void SetPixel(Vector2Int position, bool value)
    {
        ComputeShader.SetBool("TargetValue", value);
        ComputeShader.SetInts("TargetPixel", position.x, position.y);

        ComputeShader.Dispatch((int)ComputeKernel.SetPixel, 1, 1, 1);
    }

    void FlipBuffer()
    {
        Board.Flip();
        if (Board.Flipped)
        {
            ComputeShader.EnableKeyword("FLIP_BUFFER");
            if (Material != null) Material.EnableKeyword("FLIP_BUFFER");
        }
        else
        {
            ComputeShader.DisableKeyword("FLIP_BUFFER");
            if (Material != null) Material.DisableKeyword("FLIP_BUFFER");
        }
    }

    public Vector3Int ThreadGroups => new(SizeLevel, SizeLevel * 2, 1);

    public Vector2Int BoardChunks
        => new(ThreadGroupSize.x * ThreadGroups.x, ThreadGroupSize.y * ThreadGroups.y);

    public Vector2Int Cells
        => new(BoardChunks.x * 4, BoardChunks.y * 2); //Same as: new(Size, Size)
}
