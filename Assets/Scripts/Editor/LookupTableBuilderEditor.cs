using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System.IO;
using UnityEditor;
using UnityEngine;

public class LookupTableBuilderEditor : OdinEditorWindow
{
    [MenuItem("Tools/Lookup Table Builder")]
    static void OpenWindow()
    {
        GetWindow<LookupTableBuilderEditor>("LookupTextureBuilder");
    }


    [InfoBox("If the cell was not alive in previous iteration, it will be born in this iteration " +
        "if the amount of his alive neighbours is included in this list")]
    [ListDrawerSettings(DefaultExpandedState = true)]
    public int[] BirthCount = { 3 };

    [Space]
    [InfoBox("If the cell was alive in previous iteration, it will survive in this iteration " +
        "if the amount of his alive neighbours is included in this list")]
    [ListDrawerSettings(DefaultExpandedState = true)]
    public int[] SurviveCount = { 2, 3 };

    [Space]
    [FolderPath(ParentFolder = "Assets")]
    public string Path = "Lookup Tables";
    [ShowInInspector, ValidateInput("@IsValidFileName()",
        MessageType = InfoMessageType.Error,
        DefaultMessage = "The file name is not valid!")]
    public string FileName
    {
        get => string.IsNullOrEmpty(customFileName) ? GenerateFileName() : customFileName;
        set
        {
            if (value != GenerateFileName())
                customFileName = value;
        }
    }
    [SerializeField, HideInInspector]
    string customFileName;

    [PropertySpace, Button, EnableIf("@IsValidFileName()")]
    public void BuildLookupTable()
    {
        if (IsValidFileName() == false) return;
        string fileName = FileName;

        if (File.Exists($"{Application.dataPath}/{Path}/{fileName.Split('.')[0]}.{LUTBuilder.FileExtension}"))
        {
            if (EditorUtility.DisplayDialog(
                    "File overwrite warning",
                    $"File \"{fileName}.{LUTBuilder.FileExtension}\" already exists in {Path}.\n" +
                    $"Do you want to overwrite it?",
                    "Yes",
                    "Nah") == false)
                return;
        }

        //Generate LUT file
        LUTBuilder builder = new(BirthCount, SurviveCount);
        builder.BeginGenerate();

        while (builder.UpdateGenerate() == false)
        {
            float progress = (float)builder.GeneratedConfigurations / LUTBuilder.packedLength;
            EditorUtility.DisplayProgressBar(
                $"Generating Lookup Table \"{fileName}\"",
                $"Generating...  {Mathf.Round(progress * 100)}%",
                progress);
        }

        EditorUtility.DisplayProgressBar(
                $"Generating Lookup Table \"{fileName}\"",
                $"Writing to file...",
                1);

        builder.WriteToFile(FileName, Path);

        EditorUtility.ClearProgressBar();
    }

    private string GenerateFileName()
    {
        string b = "", s = "";
        foreach (int num in BirthCount) b += num;
        foreach (int num in SurviveCount) s += num;

        return $"B{b}S{s} Lookup Table";
    }

    bool IsValidFileName()
    {
        string fileName = $"{FileName.Split('.')[0]}.{LUTBuilder.FileExtension}";
        return Utils.IsValidFileName(fileName, $"{Application.dataPath}/{Path}/{fileName}");
    }
}