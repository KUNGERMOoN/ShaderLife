using AnotherFileBrowser.Windows;
using MiniBinding;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

public class GUIManager : MonoBehaviour
{
    [Header("References")]
    public UIDocument Document;
    public SimulationRunner Simulation;

    VisualElement root;

    readonly BrowserProperties fileBrowserProperties = new()
    {
        initialDir = LookupTable.LUTsPath,
        filter = $"Lookup Table (*.{LookupTable.FileExtension})|*.{LookupTable.FileExtension}"
    };

    public bool Focused
    {
        get => root.focusController.focusedElement != null;
    }

    private void OnEnable()
    {
        root = Document.rootVisualElement;

        MiniBinder.Bind(Q<SwitchButton>("play"), Simulation.UpdateInRealtime);
        MiniBinder.Bind(Q<SwitchButton>("pause"), Simulation.UpdateInRealtime,
            x => !x, x => !x);

        MiniBinder.Bind(Q<Button>("next"), Simulation.UpdateBoard);
        MiniBinder.Bind(Q<Button>("clear"), Simulation.ClearBoard);

        MiniBinder.Bind(Q<UnsignedIntegerField>("simulationSpeed"), Simulation.BoardUpdateRate);

        MiniBinder.Bind(Q<IntegerField>("seed"), Simulation.Seed);
        MiniBinder.Bind(Q<Slider>("chance"), Simulation.Chance);
        MiniBinder.Bind(Q<Button>("randomize"), Simulation.Randomise);

        MiniBinder.Bind(Q<Button>("newSimulation"), Q<Popup>("newSimulationPopup").Open);
        MiniBinder.Bind(Q<Button>("loadLUTFromFile"), LoadLUT);
        MiniBinder.Bind(Q<Button>("newLUT"), Q<Popup>("newLUTPopup").Open);
        MiniBinder.Bind(Q<Button>("createNew"), CreateSimulation);

        MiniBinder.Bind(Q<Button>("lifeLikeInfo"), () =>
            Application.OpenURL("https://conwaylife.com/wiki/Life-like_cellular_automaton"));
        MiniBinder.Bind(Q<Button>("lifeLikeInfo2"), () =>
            Application.OpenURL("https://conwaylife.com/wiki/List_of_Life-like_rules"));
        MiniBinder.Bind(Q<Button>("generateLookupTable"), GenerateLUT);

        Simulation.LUTGenerationStarted += cause =>
        {
            Q<Popup>("generatingLUT").Open();
            Q<Label>("additionalLUTGenerationInfo").style.display =
                cause == SimulationRunner.LUTGenerationCause.NoLutsFound
                ? DisplayStyle.Flex : DisplayStyle.None;
        };
        Simulation.LUTGenerationProgress += ctx =>
        {
            Q<ProgressBar>("LUTprogressBar").value = ctx.progress;
            Q<Label>("LUTprogressPrecentage").text = $"{Mathf.Round(ctx.progress * 100)}%";
            Q<Label>("LUTProgressStage").text = ctx.writingToFile ? "Writing to file..." : "Generating...";
        };
        Simulation.LUTGenerationFinished += () =>
        {
            Q<Popup>("generatingLUT").Close();
            if (!string.IsNullOrEmpty(generatedLUTPath))
            {
                Q<Popup>("newLUTPopup").Close();
                Q<TextField>("lutPath").value = Path.GetRelativePath(LookupTable.LUTsPath, generatedLUTPath);
                generatedLUTPath = null;
            }
        };
    }

    void LoadLUT()
    {
        new FileBrowser().OpenFileBrowser(new BrowserProperties()
        {
            title = "Load Lookup Table",
            initialDir = fileBrowserProperties.initialDir,
            filter = fileBrowserProperties.filter
        },
        path =>
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
                Q<TextField>("lutPath").value = Path.GetRelativePath(LookupTable.LUTsPath, path);
            else
                throw new System.NotImplementedException("TODO: display an incorrect file warning");
        });
    }

    void CreateSimulation()
    {
        Simulation.CreateSimulation(
                (int)Q<UnsignedIntegerField>("size").value,
                Path.Combine(LookupTable.LUTsPath, Q<TextField>("lutPath").value));

        Q<Popup>("newSimulationPopup").Close();
    }

    string generatedLUTPath = null;
    void GenerateLUT()
    {
        int[] birthCount = GatherLUTRuleCount(Q<VisualElement>("birthToggles"));
        int[] surviveCount = GatherLUTRuleCount(Q<VisualElement>("surviveToggles"));

        string suggestedFileName =
            $"B{string.Join("", birthCount)}" +
            $"S{string.Join("", surviveCount)} Lookup Table";

        new FileBrowser().SaveFileBrowser(new BrowserProperties()
        {
            title = "Generate Lookup Table",
            initialDir = fileBrowserProperties.initialDir,
            filter = fileBrowserProperties.filter
        },
        suggestedFileName, LookupTable.FileExtension,
        path =>
        {
            Simulation.StartLUTGeneration(birthCount, surviveCount, path);
            generatedLUTPath = path;
        });
    }

    int[] GatherLUTRuleCount(VisualElement parentElement)
    {
        List<int> result = new();
        int i = 0;
        foreach (VisualElement child in parentElement.Children())
        {
            Toggle toggle = child as Toggle;
            if (toggle == null) continue;

            if (toggle.value == true)
            {
                result.Add(i);
                i++;
            }
        }
        return result.ToArray();
    }

    private void OnDisable()
    {
        MiniBinder.UnbindUI();
    }

    T Q<T>(string name) where T : VisualElement
        => root.Q<T>(name);
}
