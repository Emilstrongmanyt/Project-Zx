#if UNITY_EDITOR
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace ProjectZx.Editor
{
    public static class CompileCheck
    {
        public static void Run()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            var timeoutAt = EditorApplication.timeSinceStartup + 300d;
            while (EditorApplication.isCompiling && EditorApplication.timeSinceStartup < timeoutAt)
                Thread.Sleep(100);

            if (EditorApplication.isCompiling)
            {
                Debug.LogError("Unity script compilation timed out after 300 seconds.");
                EditorApplication.Exit(2);
                return;
            }

            if (EditorUtility.scriptCompilationFailed)
            {
                Debug.LogError("Unity script compilation failed.");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log("Unity script compilation succeeded.");
            EditorApplication.Exit(0);
        }
    }
}
#endif