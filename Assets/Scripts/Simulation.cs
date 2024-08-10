using System;
using UnityEngine;

namespace GameOfLife
{
    public class Simulation : IDisposable
    {
        public readonly ComputeShader Shader;
        public readonly LookupTable LookupTable;

        Material material;
        public Material Material
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
                    newMaterial.SetInteger("_BoardSize", CellsDimension);
                    if (bufferFlipped) newMaterial.EnableKeyword("FLIP_BUFFER");
                    else newMaterial.DisableKeyword("FLIP_BUFFER");
                }

                material = newMaterial;
            }
        }

        public static readonly Vector3Int ThreadGroupSize = new(8, 8, 1);

        public static int CalculateBoardDimension(int sizeLevel)
            => sizeLevel * 32;

        public readonly int SizeLevel;
        public readonly Vector2Int ThreadGroups;
        public readonly Vector2Int Chunks;
        public readonly int CellsDimension;

        //Implementation of double-buffering the board
        //For more info, see Shaders/GameOfLifeSimulation.compute
        readonly ComputeBuffer boardBufferA;
        readonly ComputeBuffer boardBufferB;
        bool bufferFlipped = false;

        readonly ComputeBuffer lookupBuffer;

        public Simulation(ComputeShader shader, int sizeLevel, LookupTable lut)
        {
            if (shader == null) throw new ArgumentNullException(nameof(shader));
            if (sizeLevel < 1) throw new ArgumentException($"{nameof(sizeLevel)} cannot be smaller than 1", nameof(sizeLevel));

            Shader = shader;
            LookupTable = lut;
            SizeLevel = sizeLevel;

            ThreadGroups = new(sizeLevel, sizeLevel * 2);
            Chunks = new(ThreadGroupSize.x * ThreadGroups.x, ThreadGroupSize.y * ThreadGroups.y);
            CellsDimension = CalculateBoardDimension(sizeLevel);

            var chunksWithPadding = (Chunks.x + 2) * (Chunks.y + 2);
            var bufferSize = chunksWithPadding / 4;
            //^ This is always divisble by 4, because:
            //bufferSize = (8 * SizeLevel + 2) * (8 * SizeLevel * 2 + 2) = 4 * (4 * SizeLevel + 1)(8 * SizeLevel + 1)

            boardBufferA = new ComputeBuffer(bufferSize, sizeof(int));
            boardBufferB = new ComputeBuffer(bufferSize, sizeof(int));

            Shader.SetInts("Size", Chunks.x, Chunks.y);
            FlipBuffer();

            lookupBuffer = new ComputeBuffer(LookupTable.packedLength, sizeof(byte) * 4);
            lookupBuffer.SetData(LookupTable.PackedContents);

            //Link Buffers to the shader
            foreach (ComputeKernel kernel in AllKernels)
            {
                shader.SetBuffer((int)kernel, "chunksA", boardBufferA);
                shader.SetBuffer((int)kernel, "chunksB", boardBufferB);
                Shader.SetBuffer((int)kernel, "LookupTable", lookupBuffer);
            }
        }

        public void Dispose()
        {
            boardBufferA.Release();
            boardBufferB.Release();
            lookupBuffer.Release();
        }

        public void UpdateBoard()
        {
            FlipBuffer();
            Shader.Dispatch((int)ComputeKernel.Update, ThreadGroups.x, ThreadGroups.y, 1);
        }

        public void Randomise(int seed, float chance)
        {
            FlipBuffer();
            Shader.SetInt("Seed", seed);
            Shader.SetFloat("Chance", Mathf.Clamp01(chance));
            Shader.Dispatch((int)ComputeKernel.Randomise, ThreadGroups.x, ThreadGroups.y, 1);
        }

        public void Clear()
        {
            FlipBuffer();
            Shader.Dispatch((int)ComputeKernel.Clear, ThreadGroups.x, ThreadGroups.y, 1);
        }

        public void SetPixel(Vector2Int position, bool value)
        {
            if (Mathf.Max(position.x, position.y) > CellsDimension - 1)
                throw new ArgumentException($"Parameter {nameof(position)} is too big. " +
                    $"It cannot be bigger than {nameof(Simulation)}.{nameof(CellsDimension)} - 1.", nameof(position));

            Shader.SetBool("TargetValue", value);
            Shader.SetInts("TargetPixel", position.x, position.y);

            Shader.Dispatch((int)ComputeKernel.SetPixel, 1, 1, 1);
        }

        void FlipBuffer()
        {
            bufferFlipped = !bufferFlipped;
            if (bufferFlipped)
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

        enum ComputeKernel
        {
            Update = 0,
            Randomise = 1,
            Clear = 2,
            SetPixel = 3
        }
        static readonly ComputeKernel[] AllKernels
            = (ComputeKernel[])Enum.GetValues(typeof(ComputeKernel));
    }
}
