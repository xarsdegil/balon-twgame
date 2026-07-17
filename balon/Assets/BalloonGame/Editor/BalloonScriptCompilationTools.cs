#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;

namespace BalloonGame.Editor
{
    public static class BalloonScriptCompilationTools
    {
        [MenuItem("Tools/Balloon Game/Force Script Recompile")]
        public static void ForceScriptRecompile()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            CompilationPipeline.RequestScriptCompilation(
                RequestScriptCompilationOptions.CleanBuildCache);
        }
    }
}
#endif
