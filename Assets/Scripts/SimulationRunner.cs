using MiniBinding;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class SimulationRunner : MonoBehaviour
{
    [Header("References")]
    public ComputeShader ComputeShader;
    public MeshRenderer Renderer;
    public Material Material;


    private Simulation Simulation;

    public readonly Bindable<int> Seed = new(1337);
    public readonly Bindable<float> Chance = new(0.2f);

    public readonly Bindable<bool> UpdateInRealtime = new(false);
    public readonly Bindable<uint> BoardUpdateRate = new(10);

    public enum LUTGenerationCause { NoLutsFound, NewSimulation }
    public event Action<LUTGenerationCause> LUTGenerationStarted;
    public event Action<(float progress, bool writingToFile)> LUTGenerationProgress;
    public event Action LUTGenerationFinished;

    IEnumerator LUTGenerationEnumerator;

    private void Awake()
    {
        var LUTs = Directory.GetFiles(LookupTable.LUTsPath, $"*.{LookupTable.FileExtension}");
        if (LUTs.Length > 0)
        {
            var defaultLut = Path.Combine(LookupTable.LUTsPath, LookupTable.DefaultLUT);
            if (LUTs.Contains(defaultLut))
                CreateSimulation(1, defaultLut);
            else CreateSimulation(1, LUTs[0]);
        }
        else
        {
            string path = Path.Combine(LookupTable.LUTsPath, LookupTable.DefaultLUT);
            StartLUTGeneration(LookupTable.DefaultBirthCount, LookupTable.DefaultSurviveCount, path,
                LUTGenerationCause.NoLutsFound);
            LUTGenerationFinished += OnDone;
            void OnDone()
            {
                LUTGenerationFinished -= OnDone;
                CreateSimulation(1, path);
            }
        }
    }

    private void OnDestroy()
    {
        if (Simulation != null)
        {
            Simulation.Dispose();
            Simulation = null;
        }
    }

    float timeSinceUpdate;
    private void Update()
    {
        if (LUTGenerationEnumerator != null)
        {
            bool finished = !LUTGenerationEnumerator.MoveNext();
            if (finished)
            {
                LUTGenerationEnumerator = null;
            }
        }
        else if (UpdateInRealtime)
        {
            if (BoardUpdateRate == 0)
            {
                Simulation.UpdateBoard();
            }
            else
            {
                if (timeSinceUpdate >= 1f / BoardUpdateRate)
                {
                    Simulation.UpdateBoard();
                    timeSinceUpdate = 0;
                }
                timeSinceUpdate += Time.deltaTime;
            }
        }
    }

    public void CreateSimulation(int sizelevel, string lookupTablePath)
    {
        Simulation?.Dispose();

        var lookupTable = LookupTable.ReadFromFile(lookupTablePath);

        Simulation = new(ComputeShader, sizelevel, lookupTable)
        {
            Material = Material
        };
    }


    public int BoardSize => Simulation != null ? Simulation.Size : 1;

    public void UpdateBoard() => Simulation?.UpdateBoard();

    public void Randomise()
    {
        Simulation?.Randomise(Seed, Chance);
        Seed.Value = UnityEngine.Random.Range(int.MinValue / 2, int.MaxValue / 2);
    }

    public void ClearBoard() => Simulation?.Clear();

    public void SetPixel(Vector2Int position, bool value) => Simulation?.SetPixel(position, value);

    public void StartLUTGeneration(int[] birthCount, int[] surviveCount, string path, LUTGenerationCause cause = LUTGenerationCause.NewSimulation)
    {
        if (string.IsNullOrEmpty(path)) return;

        if (LUTGenerationEnumerator != null) return;

        LUTGenerationEnumerator = GenerateLUT(birthCount, surviveCount, path, cause);
    }

    IEnumerator<float> GenerateLUT(int[] birthCount, int[] surviveCount, string path, LUTGenerationCause cause)
    {
        LookupTable builder = new(birthCount, surviveCount);
        IEnumerator enumerator = builder.Generate();

        LUTGenerationStarted?.Invoke(cause);

        float lastProgress = 0f;
        while (enumerator.MoveNext())
        {
            float progress = (float)builder.GeneratedPacks / LookupTable.packedLength;
            progress *= 0.9f; //So we end the generation at 90% and the last 10% are for writing to file
            if (progress - lastProgress >= 0.01f)
            {
                lastProgress = MathF.Floor(progress * 100) / 100;
                LUTGenerationProgress?.Invoke((progress, false));
                yield return progress;
            }
        }

        LUTGenerationProgress?.Invoke((0.9f, true));

        builder.WriteToFile(path);
        LUTGenerationFinished?.Invoke();
        yield return 1f;
    }
}
