#if UNITY_EDITOR
using UnityEditor;

namespace BalloonGame.Editor
{
    [CustomEditor(typeof(BalloonGameSettings))]
    public class BalloonGameSettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Owner-only test commands are authorized by the Twitch user ID of the account signed in to Chat Balloon. Keeping the command words private is optional; other viewers cannot execute them.",
                MessageType.Info);
        }
    }
}
#endif
