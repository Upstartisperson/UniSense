using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SplitScreenBlueprintBuilder))]
public class SplitScreenBiulderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        SplitScreenBlueprintBuilder splitScreenBlueprintBuilder = (SplitScreenBlueprintBuilder)target;
        if (GUILayout.Button("Save To Blueprint"))
        {
            splitScreenBlueprintBuilder.Execute();
        }
        if (GUILayout.Button("Recover From Blueprint"))
        {
            splitScreenBlueprintBuilder.Recover();
        }
        if (GUILayout.Button("Reset Defaults"))
        {
            splitScreenBlueprintBuilder.ResetDefaults();
        }
    }
}
