using UnityEngine;

namespace ProjectZx.GanzSe
{
    /// <summary>
    /// Resources bridge to the imported GanzSe modular prefab.
    /// Asset lives at Resources/GanzSe/NpcCatalog.
    /// </summary>
    [CreateAssetMenu(menuName = "Project Zx/GanzSe NPC Catalog", fileName = "NpcCatalog")]
    public class GanzSeNpcCatalog : ScriptableObject
    {
        const string ResourcePath = "GanzSe/NpcCatalog";

        [SerializeField] GameObject modularCharacterPrefab;

        static GanzSeNpcCatalog _cached;

        public GameObject ModularCharacterPrefab => modularCharacterPrefab;

        public static GanzSeNpcCatalog Load()
        {
            if (_cached != null) return _cached;
            _cached = Resources.Load<GanzSeNpcCatalog>(ResourcePath);
            return _cached;
        }

        public static GameObject LoadPrefab()
        {
            var catalog = Load();
            if (catalog != null && catalog.modularCharacterPrefab != null)
                return catalog.modularCharacterPrefab;

#if UNITY_EDITOR
            // Editor fallback if the Resources asset has not been wired yet.
            var editorPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/URP GanzSe Free Modular Character Pack/Prefabs/Modular Character/GanzSe Free Modular Character Update 1_1.prefab");
            if (editorPrefab != null) return editorPrefab;
#endif
            return null;
        }
    }
}
