using UnityEditor;
using UnityEngine;

namespace Pengi.GameSystem.Editor
{
    [CustomEditor(typeof(GameConfiguration))]
    public class GameConfigurationEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GUI.enabled = Application.isPlaying;

            GameConfiguration e = target as GameConfiguration;
            
            GUILayout.Space(4f);
            if (GUILayout.Button("Reset Save Data"))
                e.ResetSaveData();
            
            GUILayout.Space(4f);
            if (GUILayout.Button("Reset Options"))
                e.ResetOptions();
            
            GUILayout.Space(4f);
            if (GUILayout.Button("Delete playerPrefs"))
                PlayerPrefs.DeleteAll();
        }
    }
}