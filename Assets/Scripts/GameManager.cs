using System.IO;
using UnityEngine;

namespace GameOfLife
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public static Settings Settings { get; private set; }

        [Header("References")]
        public Material Board;
        public Material Skybox;

        private void Awake()
        {
            Instance = this;
            LoadSettings();
            LoadStartupPalette();
        }

        void LoadStartupPalette()
        {
            var defaultPalette = Path.Combine(PalettesPath, Settings.ColorPalette);
            if (File.Exists(defaultPalette))
            {
                if (SetPalette(defaultPalette))
                    return;
            }

            if (Directory.Exists(PalettesPath))
            {
                var palettes = Directory.GetFiles(PalettesPath);
                if (palettes.Length > 0)
                {
                    if (SetPalette(palettes[0]))
                        return;
                }
            }

            //If no palettes found, default to the hard-coded "Cold Light" palette
            SetPalette(
                new(008f / 255f, 000f / 255f, 031f / 255f),
                new(178f / 255f, 213f / 255f, 209f / 255f),
                new(068f / 255f, 077f / 255f, 132f / 255f));
        }

        public static string PalettesPath => Path.Combine(Application.streamingAssetsPath, "Palettes");

        public static bool SetPalette(string path)
        {
            if (!File.Exists(path)) return false;

            Texture2D tempTexture = new(1, 1);
            if (!tempTexture.LoadImage(File.ReadAllBytes(path))) return false;

            SetPalette(tempTexture.GetPixel(0, 0), tempTexture.GetPixel(2, 0), tempTexture.GetPixel(1, 0));
            Destroy(tempTexture);

            return true;
        }

        public static void SetPalette(Color background, Color cell, Color grid)
        {
            Instance.Skybox.SetColor("_Tint", background);
            Instance.Board.SetColor("_AliveCol", cell);
            Instance.Board.SetColor("_DeadCol", background);
            Instance.Board.SetColor("_GridCol", grid);
        }


        static string SettingsFilePath => Path.Combine(Application.dataPath, "settings.json");

        public static void SaveSettings()
        {
            string json = JsonUtility.ToJson(Settings, prettyPrint: true);
            File.WriteAllText(SettingsFilePath, json);
        }

        public static void LoadSettings()
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


        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
        }
    }
}