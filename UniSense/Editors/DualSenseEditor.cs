using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UniSense;
using UnityEngine.InputSystem;


[CustomEditor(typeof(DualSense))]
public class DualSenseEditor : Editor
{
    private bool hasPlayerInput = false;
    public override void OnInspectorGUI()
    {

        EditorGUI.BeginChangeCheck();
        
       
        
        if(!Application.isPlaying && !(target as DualSense).gameObject.TryGetComponent(out PlayerInput playerInput))
        {

            EditorGUILayout.LabelField("DualSense Component Requires PlayerInput", EditorStyles.helpBox);
            if(GUILayout.Button("Add PlayerInput"))
            {
                (target as DualSense).gameObject.AddComponent<PlayerInput>();
                hasPlayerInput = true;
            }
            if (EditorGUI.EndChangeCheck()) serializedObject.ApplyModifiedProperties();
            return;
        }
        
        

        hasPlayerInput = true;
        DoSinglePlayerSection();
       
        if(EditorGUI.EndChangeCheck()) serializedObject.ApplyModifiedProperties();
    }
    
    private void DoSinglePlayerSection()
    {
        EditorGUILayout.LabelField("Single/Multi-Player Mode", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        var isMulitplayer = serializedObject.FindProperty(nameof(DualSense.IsMultiplayer));

        if (Application.isPlaying)
        {
            EditorGUI.BeginDisabledGroup(true);
            if (isMulitplayer.boolValue) EditorGUILayout.LabelField("Multi-Player Mode Active");
            else EditorGUILayout.LabelField("Single-Player Mode Active");
        }
        
        if (!isMulitplayer.boolValue)
        {
            EditorGUILayout.LabelField("Single-Player Settings (Inactive in Multiplayer Mode)", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            var allowMK = serializedObject.FindProperty(nameof(DualSense.AllowKeyboardMouse));
            EditorGUILayout.PropertyField(allowMK);
            var allowGen = serializedObject.FindProperty(nameof(DualSense.AllowGenericController));
            EditorGUILayout.PropertyField(allowGen);
            EditorGUI.indentLevel--;
        }

        if (Application.isPlaying)
        { 
            if(!isMulitplayer.boolValue) EditorGUILayout.LabelField("Exit Play Mode to Change", EditorStyles.helpBox);
            EditorGUI.EndDisabledGroup();
        }
        EditorGUI.indentLevel--;
    }
}
