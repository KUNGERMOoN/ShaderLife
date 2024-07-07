using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace GameOfLife
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("References")]
        public CameraController CameraController;
        public Material Board;
        public Material Skybox;

        private void Awake()
        {
            Instance = this;
            LoadSettings();

            var paletteFiles = Directory.GetFiles(PalettesPath, "*.png");
            var descriptionFiles = Directory.GetFiles(PalettesPath, "*.txt");
            if (paletteFiles.Length > 0)
            {
                Palettes = new();
                for (int i = 0; i < paletteFiles.Length; i++)
                {
                    string paletteFile = paletteFiles[i];
                    Palette palette;

                    string descriptionFile = Path.Combine(PalettesPath, $"{Path.GetFileNameWithoutExtension(paletteFile)}.txt");
                    if (descriptionFiles.Contains(descriptionFile))
                        palette = Palette.FromImage(paletteFile, descriptionFile);
                    else
                        palette = Palette.FromImage(paletteFile);

                    Palettes.Add(palette);

                    if (palette.Name == DefaultPalette.Name)
                        CurrentPalette = i;
                }
            }
            else
            {
                Palettes = new List<Palette> { DefaultPalette };
            }

            CurrentPalette = CurrentPalette;
        }
        public static Settings LoadedSettings => Instance.Settings;

        [Header("Properties")]
        [SerializeField, LabelText("Default Palette"), DisableInPlayMode, FilePath(AbsolutePath = true),
            Tooltip("A palette used in case no other palettes were found.\n" +
            "If any palette with the same name was fount, it will be selected.")]
        string DefaultPalette_Source;
        [HideInInspector]
        public Palette DefaultPalette;

        private void OnValidate()
        {
            if (!string.IsNullOrEmpty(DefaultPalette_Source) && File.Exists(DefaultPalette_Source))
            {
                string descriptionFile = Path.Combine(PalettesPath, $"{Path.GetFileNameWithoutExtension(DefaultPalette_Source)}.txt");
                if (File.Exists(descriptionFile))
                    DefaultPalette = Palette.FromImage(DefaultPalette_Source, descriptionFile);
                else
                    DefaultPalette = Palette.FromImage(DefaultPalette_Source);
            }
        }

        public List<Palette> Palettes { get; private set; }
        int currentPaletteIndex = 0;
        public int CurrentPalette
        {
            get => currentPaletteIndex;
            set
            {
                currentPaletteIndex = value;

                Skybox.SetColor("_Tint", Palette.DeadCell);
                Board.SetColor("_AliveCol", Palette.AliveCell);
                Board.SetColor("_DeadCol", Palette.DeadCell);
                Board.SetColor("_GridCol", Palette.Grid);
            }
        }
        public Palette Palette => Palettes[currentPaletteIndex];
        public string PalettesPath => Path.Combine(Application.dataPath, "Resources", "Palettes");


        public Settings Settings;

        public void SaveSettings()
        {
            string json = JsonUtility.ToJson(Settings, prettyPrint: true);
            File.WriteAllText(SettingsFilePath, json);
        }

        public void LoadSettings()
        {
            string path = SettingsFilePath;
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                Settings = (Settings)JsonUtility.FromJson(json, typeof(Settings));
            }
            else
            {
                Settings = new Settings();
            }
        }
        string SettingsFilePath => Path.Combine(Application.dataPath, "settings.json");
    }
}