using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System.IO;
using UnityEditor;
using UnityEngine;

public class LookupTextureBuilderEditor : OdinEditorWindow
{
    [MenuItem("Window/Lookup Texture Builder")]
    static void OpenWindow()
    {
        GetWindow<LookupTextureBuilderEditor>();
    }

    [InfoBox("Cell will become alive if the amount of his alive neighbours is included in this list")]
    [ListDrawerSettings(DefaultExpandedState = true)]
    public int[] BirthCount = { 3 };

    [Space]
    [InfoBox("Additionally, cell can become alive if the amount of his alive " +
        "neighbours is included in this list and the cell was alive in the previous frame")]
    [ListDrawerSettings(DefaultExpandedState = true)]
    public int[] SurviveCount = { 2, 3 };

    [Space]
    [FolderPath(ParentFolder = "Assets")]
    public string TexturePath = "Lookup Textures";
    [ShowInInspector]
    public string TextureName
    {
        get => string.IsNullOrEmpty(customTextureName) ? GenerateFileName() : customTextureName;
        set => customTextureName = value;
    }
    [SerializeField, HideInInspector]
    string customTextureName;

    [PropertySpace, Button]
    public void BuildLookupTexture()
    {
        string filename = $"{TextureName.Replace(".png", "")}.png";
        string relativePath = $"{TexturePath}/{filename}";
        string path = $"{Application.dataPath}/{relativePath}";
        if (File.Exists(path))
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "File overwrite warning",
                $"File {filename} already exists in {TexturePath}.\n" +
                $"Do you want to overwrite it?",
                "Yes",
                "Nah");
            if (overwrite == false) return;
        }

        File.WriteAllBytes(path, new byte[] { });
        AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceUpdate);
    }

    private string GenerateFileName()
    {
        string b = "", s = "";
        foreach (int num in BirthCount) b += num;
        foreach (int num in SurviveCount) s += num;

        return $"B{b}/S{s} Lookup Texture";
    }
}