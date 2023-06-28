using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class GameOfLife : MonoBehaviour
{
    public ComputeShader Shader;
    public RenderTexture Texture;
    public RenderTexture HeatmapTexture;
    public int HeatmapScale = 4;
    public Material Material;

    public List<Vector2Int> TestList;
    public Material TestMaterial1, TestMaterial2;

    Cell[] Cells;

    string InspectorBoardSize()
    {
        decimal d = Size * Size;
        var f = new NumberFormatInfo { NumberGroupSeparator = " ", NumberDecimalDigits = 0 };

        return $"Board size: {d.ToString("n", f)} cells";
    }

    [Space, InfoBox("$InspectorBoardSize")]
    public int SizeExponent = 8;
    public int Size => 1 << SizeExponent;

    [Range(0, 1)]
    public float Chance;

    ComputeBuffer boardBuffer;
    ComputeBuffer debugBuffer;

    Vector3Int ThreadGroups;

    enum Kernel
    {
        InitKernel = 0,
        UpdateKernel = 1,
        FlipUpdateKernel = 2,
    }

    public Cell this[int x, int y]
    {
        get => Cells[(x + 1) * (Size + 2) + y + 1];
        set => Cells[(x + 1) * (Size + 2) + y + 1] = value;
    }

    private void Awake()
    {
        Cells = new Cell[(Size + 2) * (Size + 2)];

        foreach (var cell in TestList)
            this[cell.x, cell.y] = Cell.On;

        StartCompute();
    }

    /*private void Update()
    {
        Shader.Dispatch((int)Kernel.DebugKernel, ThreadGroups.x, ThreadGroups.y, ThreadGroups.z);
    }*/

    bool flip;
    [Button]
    public void UpdateBoard()
    {
        int kernel = (int)(flip ? Kernel.FlipUpdateKernel : Kernel.UpdateKernel);
        Shader.Dispatch(kernel, ThreadGroups.x, ThreadGroups.y, ThreadGroups.z);
        boardBuffer.GetData(Cells);

        /*int[] debug = new int[(Size + 2) * (Size + 2)];
        debugBuffer.GetData(debug);
        for (int x = 0; x < Size + 2; x++)
        {
            for (int y = 0; y < Size + 2; y++)
            {
                bool b = debug[x * (Size + 1) + y + 1] != 100;

                Transform p = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
                p.position = new Vector3(x, y);
                p.GetComponent<MeshRenderer>().material = b ? TestMaterial1 : TestMaterial2;
                p.name = b ? "A" : "B";
            }
        }*/

        flip = !flip;
    }

    private void StartCompute()
    {
        Kernel[] allKernels = (Kernel[])Enum.GetValues(typeof(Kernel));

        uint groupSizeX, groupSizeY, groupSizeZ;
        Shader.GetKernelThreadGroupSizes(0, out groupSizeX, out groupSizeY, out groupSizeZ);
        ThreadGroups = new Vector3Int(Size / (int)groupSizeX, Size / (int)groupSizeY, 1);

        //Initialize RenderTextures
        Texture = CreateComputeTexture(Size, "Rendered", allKernels);
        if (Material) Material.SetTexture("_MainTex", Texture);

        HeatmapTexture = CreateComputeTexture(Size / HeatmapScale + 2, "Heatmap", allKernels);
        if (Material) Material.SetTexture("_DensityTex", HeatmapTexture);

        //Set Compute Shader properties and buffers
        Shader.SetInt("Size", Size);
        Shader.SetFloat("Chance", Chance);

        debugBuffer = new((Size + 2) * (Size + 2), sizeof(int));
        debugBuffer.SetData(new int[(Size + 2) * (Size + 2)]);

        boardBuffer = new ComputeBuffer(Cells.Length, sizeof(int) * 2);
        boardBuffer.SetData(Cells);
        foreach (Kernel kernel in allKernels)
        {
            Shader.SetBuffer((int)kernel, "BufferCells", boardBuffer);
            Shader.SetBuffer((int)kernel, "debug", debugBuffer);
        }

        //Initialize Compute Shader
        Shader.Dispatch((int)Kernel.InitKernel, ThreadGroups.x, ThreadGroups.y, ThreadGroups.z);
        boardBuffer.GetData(Cells);

        //Run the first frame
        flip = true;
        UpdateBoard();
    }

    RenderTexture CreateComputeTexture(int size, string ComputeShaderPropertyName, Kernel[] kernels)
    {
        RenderTexture texture = new(size, size, 24);
        texture.enableRandomWrite = true;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.name = ComputeShaderPropertyName;
        texture.Create();
        foreach (Kernel kernel in kernels)
            Shader.SetTexture((int)kernel, ComputeShaderPropertyName, texture);

        return texture;
    }

    private void OnApplicationQuit()
    {
        boardBuffer?.Release();
    }
}
