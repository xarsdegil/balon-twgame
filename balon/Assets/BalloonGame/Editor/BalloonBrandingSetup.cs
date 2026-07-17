#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace BalloonGame.Editor
{
    public static class BalloonBrandingSetup
    {
        private const string ProductName = "Chat Balloon";
        private const string CompanyName = "xarsleech";
        private const string ApplicationIdentifier = "com.xarsleech.chatballoon";
        private const string IconPath = "Assets/BalloonGame/Art/AppIcon.png";

        [InitializeOnLoadMethod]
        private static void ApplyBrandingWhenReady()
        {
            EditorApplication.delayCall += ApplyBranding;
        }

        [MenuItem("Tools/Balloon Game/Apply Project Branding")]
        public static void ApplyBranding()
        {
            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon == null)
                return;

            PlayerSettings.productName = ProductName;
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.SetApplicationIdentifier(
                UnityEditor.Build.NamedBuildTarget.Standalone,
                ApplicationIdentifier);
            int[] iconSizes = PlayerSettings.GetIconSizes(
                UnityEditor.Build.NamedBuildTarget.Standalone,
                IconKind.Application);
            Texture2D[] icons = new Texture2D[iconSizes.Length];
            for (int index = 0; index < icons.Length; index++)
                icons[index] = icon;
            PlayerSettings.SetIcons(
                UnityEditor.Build.NamedBuildTarget.Standalone,
                icons,
                IconKind.Application);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
