using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
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

        LUTBuilder builder = new(BirthCount, SurviveCount);

        //TODO: EditorUtility.DisplayProgressBar

        builder.WriteToFile(
            FileName,
            Path,
            () => EditorUtility.DisplayDialog(
                "File overwrite warning",
                $"File \"{FileName}.{LUTBuilder.FileExtension}\" already exists in {Path}.\n" +
                $"Do you want to overwrite it?",
                "Yes",
                "Nah"));
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
        return LifeUtils.IsValidFileName(fileName, $"{Application.dataPath}/{Path}/{fileName}");
    }
}