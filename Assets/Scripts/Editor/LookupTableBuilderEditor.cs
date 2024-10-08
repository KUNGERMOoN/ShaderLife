//Editor utility for generating lookup tables (.lut) for other life-like celluar automata
//Currently requires Odin Inspector to be added to the project
//Alternatively, you can use the runtime lookup table builder
#if ODIN_INSPECTOR && UNITY_EDITOR
using GameOfLife;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System.Collections;
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
    [ListDrawerSettings(DefaultExpandedState = true, IsReadOnly = true, ShowIndexLabels = true)]
    public bool[] BirthCount = LookupTable.DefaultBirthCount;

    [Space]
    [InfoBox("If the cell was alive in previous iteration, it will survive in this iteration " +
        "if the amount of his alive neighbours is included in this list")]
    [ListDrawerSettings(DefaultExpandedState = true, IsReadOnly = true, ShowIndexLabels = true)]
    public bool[] SurviveCount = LookupTable.DefaultSurviveCount;

    [PropertySpace, Button]
    public void BuildLookupTable()
    {
        string suggestedFileName = LookupTable.GenerateRulestring(BirthCount, SurviveCount);

        string path = EditorUtility.SaveFilePanel(
            "Generate Lookup Table", LookupTable.LUTsPath, suggestedFileName, LookupTable.FileExtension);

        if (!string.IsNullOrEmpty(path))
        {
            LookupTable builder = new(BirthCount, SurviveCount);
            IEnumerator enumerator = builder.Generate();

            string title = $"Generating Lookup Table: {Path.GetFileName(path)}";
            while (enumerator.MoveNext())
            {
                float progress = (float)builder.GeneratedPacks / LookupTable.packedLength;
                EditorUtility.DisplayProgressBar(
                    title,
                    $"Generating...  {Mathf.Round(progress * 100)}%",
                    progress);
            }

            EditorUtility.DisplayProgressBar(
                    title,
                    $"Writing to file...",
                    1);

            builder.WriteToFile(path);

            EditorUtility.ClearProgressBar();
        }
    }
}
#endif