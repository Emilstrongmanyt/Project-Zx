#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ProjectZx.GanzSe.Editor
{
    /// <summary>
    /// Batch-mode / menu validation: assemble each NPC role and dump a PNG
    /// so we can verify heads exist before shipping TestFlight.
    /// </summary>
    public static class GanzSeBakeValidator
    {
        const string OutDir = "Assets/_Project/Temp/GanzSeValidate";

        [MenuItem("Project Zx/GanzSe/Validate NPC Bakes")]
        public static void ValidateFromMenu() => Validate();

        public static void Validate()
        {
            Directory.CreateDirectory(OutDir);

            var prefab = GanzSeNpcCatalog.LoadPrefab();
            if (prefab == null)
            {
                Debug.LogError("[GanzSeValidate] ModularCharacter prefab missing.");
                EditorApplication.Exit(1);
                return;
            }

            var ok = 0;
            foreach (GanzSeNpcRole role in System.Enum.GetValues(typeof(GanzSeNpcRole)))
            {
                GanzSeRenderStudio.Warm(role);
                if (!GanzSeRenderStudio.TryGetSprite(role, out var sprite) || sprite == null)
                {
                    Debug.LogError($"[GanzSeValidate] {role}: no baked sprite.");
                    continue;
                }

                var tex = sprite.texture;
                if (tex == null)
                {
                    Debug.LogError($"[GanzSeValidate] {role}: sprite has no texture.");
                    continue;
                }

                var path = Path.Combine(OutDir, role + ".png");
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Debug.Log($"[GanzSeValidate] Wrote {path} ({tex.width}x{tex.height})");
                ok++;
            }

            AssetDatabase.Refresh();
            Debug.Log($"[GanzSeValidate] Done. {ok} role bake(s).");
            if (Application.isBatchMode)
                EditorApplication.Exit(ok > 0 ? 0 : 2);
        }
    }
}
#endif
