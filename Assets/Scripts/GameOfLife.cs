using Sirenix.OdinInspector;
using System.IO;
using UnityEngine;

public class GameOfLife : MonoBehaviour, System.IDisposable
{
#pragma warning disable IDE0051
    #region Inspector
    [BoxGroup("Base", showLabel: false)]
    public ComputeShader ComputeShader;
    [BoxGroup("Base"), FilePath(AbsolutePath = false, ParentFolder = "Assets", Extensions = ".lut")]
    public string LookupTable;
    [BoxGroup("Base"), DetailedInfoBox("Calculate Board Size", "$InspectorBoardSizeInfo")]
    public int SizeLevel;
    [BoxGroup("Base")]
    public bool RandomiseOnStart = true;
    [BoxGroup("Base"), Button]
    void NewSimulation() => CreateSimulation();

    [FoldoutGroup("Simulation"), SerializeField, LabelText("Material")]
    Material material;
    public Material Material
    {
        get => material;
        set
        {
            material = value;
            MeshRenderer.sharedMaterial = material;

            if (simulation != null) simulation.Material = value;
        }
    }
    [BoxGroup("Simulation/Randomise", ShowLabel = false)]
    public bool RandomSeed;
    [BoxGroup("Simulation/Randomise"), HideIf("$RandomSeed")]
    public int Seed = 1337;
    [BoxGroup("Simulation/Randomise"), Range(0, 1)]
    public float Chance;
    [BoxGroup("Simulation/Randomise"), Button("Randomise"), DisableInEditorMode]
    void InspectorRandomise() => Randomise();

    [BoxGroup("Simulation/Update", ShowLabel = false)]
    public bool UpdateInRealtime;
    [BoxGroup("Simulation/Update"), EnableIf("UpdateInRealtime")]
    public int BoardUpdateRate = 10;
    [BoxGroup("Simulation/Update"), Button("Update Board"), DisableInEditorMode]
    void InspectorUpdateBoard() => UpdateBoard();

    [BoxGroup("Simulation/Edit", ShowLabel = false), DisableInEditorMode, LabelText("Position"), ShowInInspector]
    Vector2Int SetPixel_position = Vector2Int.zero;
    [BoxGroup("Simulation/Edit"), DisableInEditorMode, LabelText("Value"), ShowInInspector]
    bool SetPixel_value = true;
    [BoxGroup("Simulation/Edit"), DisableInEditorMode, Button("Set Pixel")]
    void InspectorSetPixel() => SetPixel(SetPixel_position, SetPixel_value);

    private void OnValidate()
    {
        Material = material;

        SizeLevel = Mathf.Max(SizeLevel, 1);
        if (ComputeShader != null)
        {
            ComputeShader.GetKernelThreadGroupSizes(0, out uint groupSizeX, out _, out _);
            if (simulation == null) Material.SetInteger("_BoardSize", (int)groupSizeX * SizeLevel * 4);
        }

        if (simulation != null)
            SetPixel_position.Clamp(Vector2Int.zero, new Vector2Int(simulation.Size - 1, simulation.Size - 1));
    }

    string InspectorBoardSizeInfo()
    {
        if (ComputeShader == null)
            return $"The {nameof(ComputeShader)} property has not been set, and so the board size can't be calculated";

        ComputeShader.GetKernelThreadGroupSizes(0, out uint groupSizeX, out uint groupSizeY, out _);

        return $@"Each thread has 4x2 cells.
Each thread group has {((int)groupSizeX).ThousandSpacing()}x{((int)groupSizeY).ThousandSpacing()} threads.
So, in total we are simulating {(4 * (int)groupSizeX).ThousandSpacing()}x{(2 * (int)groupSizeY).ThousandSpacing()} in each thread group.
SizeLevel = {SizeLevel.ThousandSpacing()}, so we create {(SizeLevel).ThousandSpacing()}x{(SizeLevel * 2).ThousandSpacing()} thread groups
(to make sure the board is square),
which gives us {((int)groupSizeX * SizeLevel).ThousandSpacing()}x{((int)groupSizeY * SizeLevel * 2).ThousandSpacing()} threads or
{(4 * (int)groupSizeX * SizeLevel).ThousandSpacing()}x{(2 * (int)groupSizeY * SizeLevel * 2).ThousandSpacing()} cells.
({(4 * (int)groupSizeX * SizeLevel * 2 * (int)groupSizeY * SizeLevel * 2).ThousandSpacing()} cells in total)";
    }
    #endregion Inspector
#pragma warning restore IDE0051

    Simulation simulation;
    MeshRenderer meshRenderer;
    MeshRenderer MeshRenderer
    {
        get
        {
            if (meshRenderer == null)
            {
                meshRenderer = GetComponent<MeshRenderer>();
            }
            return meshRenderer;
        }
    }

    private void Start()
    {
        CreateSimulation();
    }

    private void OnDestroy() => Dispose();

    float timeSinceUpdate;
    private void Update()
    {
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

    public void CreateSimulation()
    {
        if (!Application.isPlaying) return;
        if (ComputeShader == null || !File.Exists(Path.Combine(Application.dataPath, LookupTable)))
            return;

        if (simulation != null) Dispose();

        var lookupTable = LUTBuilder.LoadFromFile(Path.GetFileName(LookupTable), Path.GetDirectoryName(LookupTable));

        simulation = new Simulation(ComputeShader, SizeLevel, lookupTable);
        simulation.Material = material;

        if (RandomSeed) RandomiseSeed();
        if (RandomiseOnStart) Randomise();
    }

    public void UpdateBoard()
    {
        if (simulation == null) return;

        simulation.UpdateBoard();
    }

    public void Randomise()
    {
        if (simulation == null) return;

        if (RandomSeed) RandomiseSeed();
        simulation.Randomise(Seed, Chance);
    }

    public void RandomiseSeed() => Seed = Random.Range(int.MinValue / 2, int.MaxValue / 2);

    public void SetPixel(Vector2Int position, bool value)
    {
        if (simulation == null) return;

        simulation.SetPixel(position, value);
    }

    public void Dispose()
    {
        if (simulation != null)
        {
            simulation.Dispose();
            simulation = null;
        }
    }
}
