using MiniBinding;
using System;
using System.IO;
using System.Linq;
using UnityEngine;

public class SimulationRunner : MonoBehaviour
{
    [Header("References")]
    public ComputeShader ComputeShader;
    public MeshRenderer Renderer;
    public Material Material;


    public Simulation Simulation { get; private set; }

    public readonly Bindable<bool> RandomSeed = new(true);
    public readonly Bindable<int> Seed = new(1337);
    public readonly Bindable<float> Chance = new(0.2f);

    public readonly Bindable<bool> UpdateInRealtime = new(false);
    public readonly Bindable<uint> BoardUpdateRate = new(10);

    private void Start()
    {
        var LUTs = Directory.GetFiles(LookupTable.LUTsPath);
        if (LUTs.Length > 0)
        {
            if (LUTs.Contains(LookupTable.DefaultLUT))
                CreateSimulation(1, Path.Combine(LookupTable.LUTsPath, LookupTable.DefaultLUT));
            else CreateSimulation(1, Path.Combine(LookupTable.LUTsPath, LUTs[0]));
        }
        else
        {
            //TODO: Figure out what to do (maybe create an empty LUT at runtime
            //or re-generate the default LUT and use it)
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
        if (UpdateInRealtime.Value)
        {
            if (BoardUpdateRate.Value == 0)
            {
                Simulation.UpdateBoard();
            }
            else
            {
                if (timeSinceUpdate >= 1f / BoardUpdateRate.Value)
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

    public void Randomise()
    {
        if (RandomSeed.Value) RandomiseSeed();
        Simulation.Randomise(Seed.Value, Chance.Value);
    }

    void RandomiseSeed() => Seed.Value = UnityEngine.Random.Range(int.MinValue / 2, int.MaxValue / 2);
}
