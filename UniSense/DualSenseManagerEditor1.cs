using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UniSense.PlayerManager
{
    [CustomEditor(typeof(DualSenseManager))]
    public class DualSenseManagerEditor1 : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            var setup = serializedObject.FindProperty(nameof(DualSenseManager._ManagerConfigured));
            if (!setup.boolValue)
            {
                if(Application.isPlaying)
                {
                    EditorGUILayout.HelpBox(EditorGUIUtility.TrTextContent("Manager Not Configured End Player To Configure"));
                    if (EditorGUI.EndChangeCheck()) serializedObject.ApplyModifiedProperties();
                    return;
                }
                if(GUILayout.Button("Configure Manager"))
                {
                    var manager = (DualSenseManager)target;
                    manager.ConfigureManager();
                    setup.boolValue = true;
                }
                if (EditorGUI.EndChangeCheck()) serializedObject.ApplyModifiedProperties();
                return;
            }
            DoNotificationSectionUI();
            DoJoinUI();
            DoSplitScreenSectionUI();
            if (EditorGUI.EndChangeCheck()) serializedObject.ApplyModifiedProperties();

            if (EditorApplication.isPlaying)
                DoDebugUI();
        }

        private void DoDebugUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(EditorGUIUtility.TrTextContent("Debug Info"), EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true);

            UniSensePlayer[] players = (target as DualSenseManager).Players.ToArray();
            if (players.Length == 0)
            {
                EditorGUILayout.LabelField("No Players");
            }
            else
            {
                for(int i = 0; i < players.Length; i++)
                {
                    int id = players[i].PlayerId;
                    
                    EditorGUILayout.LabelField("Player #|Id: " + i +"|" + id + "; U.S. Id: " + players[i].UnisenseId + "; Active = " + players[i].Active + "; Type = " + players[i].DeviceType );
                }
            }
            EditorGUI.EndDisabledGroup();
        }

        private void DoNotificationSectionUI()
        {
            var notificationBehavior = serializedObject.FindProperty(nameof(DualSenseManager.notificationBehavoir));
            EditorGUILayout.PropertyField(notificationBehavior);
            var config = serializedObject.FindProperty(nameof(DualSenseManager._ManagerConfigured));
            EditorGUILayout.PropertyField(config);
            switch ((NotificationBehavoir)notificationBehavior.intValue)
            {
                case NotificationBehavoir.SendMessages:
                    EditorGUILayout.HelpBox(EditorGUIUtility.TrTextContent("Will SendMessage() to GameObject: OnPlayerJoined, OnPlayerLeft"));
                    break;
                case NotificationBehavoir.BroadCastMessages:
                    EditorGUILayout.HelpBox(EditorGUIUtility.TrTextContent("Will BroadcastMessage() to GameObject: OnPlayerJoined, OnPlayerLeft"));
                    break;
                default:
                    break;
            }
        }
        private void DoJoinUI()
        {
            EditorGUILayout.LabelField("Joining", EditorStyles.boldLabel);
            var joinBehavoir = serializedObject.FindProperty(nameof(DualSenseManager._JoinBehavoir));
            EditorGUILayout.PropertyField(joinBehavoir);
            EditorGUI.indentLevel++;
            switch ((JoinBehavoir)joinBehavoir.intValue)
            {
                 case JoinBehavoir.JoinPlayersWhenJoinActionIsTriggered:
                    var JoinAction = serializedObject.FindProperty(nameof(DualSenseManager._JoinAction));
                    EditorGUILayout.PropertyField(JoinAction);
                    goto case JoinBehavoir.JoinPlayersAutomatically;
                case JoinBehavoir.JoinPlayersAutomatically:
                    var PlayerPrefab = serializedObject.FindProperty(nameof(DualSenseManager._PlayerPrefab));
                    EditorGUILayout.PropertyField(PlayerPrefab);
                    break;
                default:
                    break;
            }
            EditorGUI.indentLevel--;
            var maxPlayers = serializedObject.FindProperty(nameof(DualSenseManager._MaxPlayers));
            EditorGUILayout.PropertyField(maxPlayers);
            var allowKeyboardMouse = serializedObject.FindProperty(nameof(DualSenseManager._AllowMouseKeyboard));
            EditorGUILayout.PropertyField(allowKeyboardMouse);
            var allowGeneric = serializedObject.FindProperty(nameof(DualSenseManager._AllowGeneric));
            EditorGUILayout.PropertyField(allowGeneric);
        }
        private void DoSplitScreenSectionUI()
        {
            EditorGUILayout.LabelField("Split-Screen", EditorStyles.boldLabel);
            var enableSplitScreen = serializedObject.FindProperty(nameof(DualSenseManager._EnableSplitScreen));
            EditorGUILayout.PropertyField(enableSplitScreen);
            if (!enableSplitScreen.boolValue) return;
            EditorGUI.indentLevel++;
            var isCustomSplitScreen = serializedObject.FindProperty(nameof(DualSenseManager._customSplitScreen));
            EditorGUILayout.PropertyField(isCustomSplitScreen);
            if (isCustomSplitScreen.boolValue)
            {
                EditorGUI.indentLevel++;
                var customSplitScreen = serializedObject.FindProperty(nameof(DualSenseManager._customBlueprint));
                EditorGUILayout.PropertyField(customSplitScreen);
                EditorGUI.indentLevel--;
            }
            else
            {
                var maintainAspect = serializedObject.FindProperty(nameof(DualSenseManager._MaintianAscpectRatio));
                EditorGUILayout.PropertyField(maintainAspect);
                
            }
            var fixedNumScreens = serializedObject.FindProperty(nameof(DualSenseManager._SetFixedNumber));
            EditorGUILayout.PropertyField(fixedNumScreens);
            if (fixedNumScreens.boolValue)
            {
                EditorGUI.indentLevel++;
                var numScreens = serializedObject.FindProperty(nameof(DualSenseManager._NumScreens));
                EditorGUILayout.PropertyField(numScreens);
                EditorGUI.indentLevel--;
            }
            var screenRect = serializedObject.FindProperty(nameof(DualSenseManager._ScreenSpace));
            EditorGUILayout.PropertyField(screenRect);
            EditorGUI.indentLevel--;

        }
    }
}