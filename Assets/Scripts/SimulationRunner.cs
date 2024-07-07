using GameOfLife;
using GameOfLife.DataBinding;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class SimulationRunner : MonoBehaviour
{
    [Header("References")]
    public ComputeShader ComputeShader;
    public MeshRenderer Renderer;
    public Material Material;


    private Simulation Simulation;

    public readonly BindableProperty<int> Seed = new(1337);
    public readonly BindableProperty<float> Chance = new(0.2f);

    public readonly BindableProperty<bool> UpdateInRealtime = new(false);
    public readonly BindableProperty<uint> BoardUpdateRate = new(10);

    public enum LUTGenerationCause { NoLutsFound, NewSimulation }
    public event Action<LUTGenerationCause> LUTGenerationStarted;
    public event Action<(float progress, bool writingToFile)> LUTGenerationProgress;
    public event Action LUTGenerationFinished;

    public readonly BindableProperty<float> FPS = new();
    public readonly BindableProperty<float> SPS = new();

    public string StartingLUT { get; private set; }

    float generationProgress;

    private void OnEnable()
    {
        void CreateStartingSimulation(string lutPath)
        {
            CreateSimulation(1, lutPath);
            var gliderStart = new Vector2Int((BoardSize - 4) / 2, (BoardSize - 4) / 2);
            SetPixel(gliderStart + new Vector2Int(0, 0), true);
            SetPixel(gliderStart + new Vector2Int(1, 0), true);
            SetPixel(gliderStart + new Vector2Int(2, 0), true);
            SetPixel(gliderStart + new Vector2Int(2, 1), true);
            SetPixel(gliderStart + new Vector2Int(1, 2), true);
        }

        var LUTs = Directory.GetFiles(LookupTable.LUTsPath, $"*.{LookupTable.FileExtension}");
        if (LUTs.Length > 0)
        {
            var defaultLut = Path.Combine(LookupTable.LUTsPath, LookupTable.DefaultLUT);
            if (LUTs.Contains(defaultLut))
                StartingLUT = defaultLut;
            else StartingLUT = LUTs[0];
            CreateStartingSimulation(StartingLUT);
        }
        else
        {
            string path = Path.Combine(LookupTable.LUTsPath, LookupTable.DefaultLUT);
            StartingLUT = path;
            GenerateLUT(LookupTable.DefaultBirthCount, LookupTable.DefaultSurviveCount, path,
                () => { CreateStartingSimulation(path); },
                LUTGenerationCause.NoLutsFound);
        }
    }

    private void OnDestroy()
    {
        if (Simulation != null)
        {
            Simulation.Dispose();
            Simulation = null;
            lutGenerationCancellationSource.Cancel();
        }
    }

    float time;
    float updates;
    float simulationTime;
    float simulationUpdates;
    float progressOnLastInvoke = float.NegativeInfinity;
    private void Update()
    {
        if (generationProgress - progressOnLastInvoke >= 0.01f)
        {
            LUTGenerationProgress?.Invoke((generationProgress, false));
            progressOnLastInvoke = generationProgress;
        }
        if (UpdateInRealtime)
        {
            if (BoardUpdateRate == 0)
            {
                Simulation.UpdateBoard();
                simulationUpdates++;
            }
            else
            {
                if (simulationTime >= 1f / BoardUpdateRate)
                {
                    Simulation.UpdateBoard();
                    simulationUpdates++;
                    simulationTime = 0;
                }
                simulationTime += Time.deltaTime;
            }
        }

        updates++;
        time += Time.deltaTime;
        if (time >= GameManager.LoadedSettings.FPSCounterRefreshRate)
        {

            FPS.Value = updates / time;
            SPS.Value = simulationUpdates / time;

            time = 0;
            updates = 0;
            simulationUpdates = 0;
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


    readonly CancellationTokenSource lutGenerationCancellationSource = new();
    public async void GenerateLUT(bool[] birthCount, bool[] surviveCount, string path, Action finished = null, LUTGenerationCause cause = LUTGenerationCause.NewSimulation)
    {
        LookupTable builder = new(birthCount, surviveCount);
        IEnumerator enumerator = builder.Generate();

        LUTGenerationStarted?.Invoke(cause);

        CancellationToken token = lutGenerationCancellationSource.Token;
        await Task.Run(() =>
        {
            while (enumerator.MoveNext())
            {
                if (lutGenerationCancellationSource.IsCancellationRequested) return;
                generationProgress = (float)builder.GeneratedPacks / LookupTable.packedLength;
            }
        }, token);

        LUTGenerationProgress?.Invoke((0.9f, true));

        builder.WriteToFile(path);

        finished?.Invoke();
        LUTGenerationFinished?.Invoke();
    }
}
