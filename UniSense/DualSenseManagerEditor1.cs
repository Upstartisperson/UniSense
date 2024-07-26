using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DualSenseManager))]
public class DualSenseManagerEditor1 : Editor
{
    private int num;

    //public override void OnInspectorGUI()
    //{
    //    base.OnInspectorGUI();
    //    var tar = target as DualSenseManager;

    //    if(tar.EnableSplitScreen)
    //    {
    //        var maxPlayers = serializedObject.FindProperty(nameof(DualSenseManager.));
    //        EditorGUI.indentLevel++;
    //        EditorGUILayout.PropertyField(maxPlayers);
    //    }
    //}
}
