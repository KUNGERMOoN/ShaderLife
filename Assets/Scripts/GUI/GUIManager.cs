using AnotherFileBrowser.Windows;
using GameOfLife.DataBinding;
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameOfLife.GUI
{
    public class GUIManager : MonoBehaviour
    {
        [Header("References")]
        public UIDocument Document;
        public SimulationRunner Simulation;

        public bool Focused
        {
            get => Document.rootVisualElement.focusController.focusedElement != null;
        }

        private void Awake()
        {
            Simulation.LUTGenerationStarted += cause =>
            {
                Q<Popup>("generatingLUT").Open();
                Q<Label>("additionalLUTGenerationInfo").style.display =
                    cause == SimulationRunner.LUTGenerationCause.NoLutsFound
                    ? DisplayStyle.Flex : DisplayStyle.None;
            };
            Simulation.LUTGenerationUpdate += ctx =>
            {
                Q<ProgressBar>("LUTprogressBar").value = ctx.progress;
                Q<Label>("LUTprogressPrecentage").text = $"{Mathf.Round(ctx.progress * 100)}%";
                Q<Label>("LUTProgressStage").text = ctx.writingToFile ? "Writing to file..." : "Generating...";
            };
            Simulation.LUTGenerationFinished += Q<Popup>("generatingLUT").Close;
        }

        private void Start()
        {
            Q<SwitchButton>("play").Bind(Simulation.UpdateInRealtime);
            Q<SwitchButton>("pause").Bind(Simulation.UpdateInRealtime,
                x => !x, x => !x);

            Q<Button>("next").Bind(Simulation.UpdateBoard);
            Q<Button>("clear").Bind(Simulation.ClearBoard);

            Q<UnsignedIntegerField>("simulationRate").Bind(Simulation.BoardUpdateRate);

            Q<IntegerField>("seed").Bind(Simulation.Seed);
            Q<Slider>("chance").Bind(Simulation.Chance);
            Q<Button>("randomize").Bind(Simulation.Randomise);

            Q<Button>("newSimulation").Bind(Q<Popup>("newSimulationPopup").Open);
            Q<UnsignedIntegerField>("size").RegisterCallback<ChangeEvent<uint>>(NewSizeValueChanged);
            UpdateHintBoardSize((int)Q<UnsignedIntegerField>("size").value);
            Q<Button>("createNew").Bind(CreateSimulation);

            Q<Button>("loadLUTFromFile").Bind(LoadLUT);
            Q<Button>("newLUT").Bind(Q<Popup>("newLUTPopup").Open);
            SetLUTPath(Simulation.StartingLUT);

            Q<Button>("lifeLikeInfo").Bind(() =>
                Application.OpenURL("https://conwaylife.com/wiki/List_of_Life-like_rules"));
            Q<Button>("lifeLikeInfo2").Bind(() =>
                Application.OpenURL("https://conwaylife.com/wiki/Life-like_cellular_automaton"));
            Q<Button>("generateLookupTable").Bind(GenerateLUT);

            SetupToggles("birth", LookupTable.DefaultBirthCount, birthCountToggles);
            SetupToggles("survive", LookupTable.DefaultSurviveCount, surviveCountToggles);
            BindToggles(birthCountToggles);
            BindToggles(surviveCountToggles);

            Q<Button>("computeShadersUnsupportedExit").Bind(GameManager.Quit);

            Q<Button>("startWarningClose").Bind(Q<Popup>("startWarning").Close);
            Q<Button>("startWarningExit").Bind(GameManager.Quit);

            Q<Button>("quitPopupCancel").Bind(Q<Popup>("quitPopup").Close);
            Q<Button>("quitPopupQuit").Bind(GameManager.Quit);

            Simulation.FPS.Bind(fps => Q<Label>("fps").text = $"{Mathf.Round(fps)} FPS");
            Simulation.SPS.Bind(sps => Q<Label>("sps").text = $"{Mathf.Round(sps)} ticks/sec");

            if (SystemInfo.supportsComputeShaders == false)
                Q<Popup>("computeShadersUnsupportedPopup").Open();
            else Q<Popup>("startWarning").Open();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                var popup = Popup.OpenedPopups.LastOrDefault();
                if (popup != null)
                {
                    if (popup.Closeable == true)
                        popup.Close();
                }
                else
                    Q<Popup>("quitPopup").Open();
            }

        }

        readonly Toggle[] birthCountToggles = new Toggle[9];
        readonly Toggle[] surviveCountToggles = new Toggle[9];

        void SetupToggles(string toggleNameBase, bool[] defaultValues, Toggle[] result)
        {
            for (int i = 0; i < result.Length; i++)
            {
                Toggle toggle = Q<Toggle>(toggleNameBase + i);
                result[i] = toggle;
                toggle.value = defaultValues[i];
            }
        }

        void BindToggles(Toggle[] toggles)
        {
            foreach (var toggle in toggles)
                toggle.Bind(ctx => UpdateRulestring());
        }

        void UpdateRulestring()
            => Q<Label>("rulestring").text = "Rulestring: "
                + LookupTable.GenerateRulestring(
                    birthCountToggles.Select(toggle => toggle.value).ToArray(),
                    surviveCountToggles.Select(toggle => toggle.value).ToArray());

        readonly BrowserProperties fileBrowserProperties = new()
        {
            initialDir = LookupTable.LUTsPath,
            filter = $"Lookup Table (*.{LookupTable.FileExtension})|*.{LookupTable.FileExtension}"
        };

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
                    throw new NotImplementedException("TODO: display an incorrect file warning");
            });
        }

        void NewSizeValueChanged(ChangeEvent<uint> ctx)
            => UpdateHintBoardSize((int)ctx.newValue);

        void UpdateHintBoardSize(int sizeLevel)
        {
            var size = GameOfLife.Simulation.CalculateBoardDimension(sizeLevel);
            Q<Label>("expectedSize").text = $"{size}x{size} cells";
        }

        void CreateSimulation()
        {
            Simulation.CreateSimulation(
                    (int)Q<UnsignedIntegerField>("size").value,
                    Path.Combine(LookupTable.LUTsPath, Q<TextField>("lutPath").value));

            Q<Popup>("newSimulationPopup").Close();
        }

        void SetLUTPath(string absolutePath)
            => Q<TextField>("lutPath").value = Path.GetRelativePath(LookupTable.LUTsPath, absolutePath);

        void GenerateLUT()
        {
            var birthCount = birthCountToggles.Select(toggle => toggle.value).ToArray();
            var surviveCount = surviveCountToggles.Select(toggle => toggle.value).ToArray();
            string suggestedFileName = LookupTable.GenerateRulestring(birthCount, surviveCount).Replace('/', ' ');

            new FileBrowser().SaveFileBrowser(new BrowserProperties()
            {
                title = "Generate Lookup Table",
                filter = fileBrowserProperties.filter,
                restoreDirectory = true
            },
            suggestedFileName, LookupTable.FileExtension,
            path =>
            {
                if (string.IsNullOrEmpty(path)) return;
                Simulation.GenerateLUT(birthCount, surviveCount, path, () =>
                {
                    Q<Popup>("newLUTPopup").Close();
                    SetLUTPath(path);
                });
            });
        }

        private void OnDestroy()
        {
            UIBindingManager.Dispose();
            ButtonCallback.Dispose();
        }

        T Q<T>(string name = null, string className = null) where T : VisualElement
            => Document.rootVisualElement.Q<T>(name, className);
    }
}
