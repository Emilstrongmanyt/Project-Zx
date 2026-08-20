using UnityEngine;

namespace ProjectZx.GanzSe
{
    /// <summary>
    /// Loads the GanzSe modular character prefab for builds and editor.
    /// Prefers Resources/GanzSe/ModularCharacter (shipped copy) so device builds
    /// do not depend on a ScriptableObject prefab reference that can break.
    /// </summary>
    [CreateAssetMenu(menuName = "Project Zx/GanzSe NPC Catalog", fileName = "NpcCatalog")]
    public class GanzSeNpcCatalog : ScriptableObject
    {
        const string ResourcePrefabPath = "GanzSe/ModularCharacter";
        const string ResourceCatalogPath = "GanzSe/NpcCatalog";

        [SerializeField] GameObject modularCharacterPrefab;

        static GanzSeNpcCatalog _cached;
        static GameObject _prefabCached;

        public GameObject ModularCharacterPrefab => modularCharacterPrefab;

        public static GameObject LoadPrefab()
        {
            if (_prefabCached != null) return _prefabCached;

            // 1) Resources copy — reliable on device / TestFlight.
            _prefabCached = Resources.Load<GameObject>(ResourcePrefabPath);
            if (_prefabCached != null) return _prefabCached;

            // 2) ScriptableObject reference (editor wiring).
            var catalog = Resources.Load<GanzSeNpcCatalog>(ResourceCatalogPath);
            _cached = catalog;
            if (catalog != null && catalog.modularCharacterPrefab != null)
            {
                _prefabCached = catalog.modularCharacterPrefab;
                return _prefabCached;
            }

#if UNITY_EDITOR
            // 3) Editor-only direct pack path.
            _prefabCached = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/URP GanzSe Free Modular Character Pack/Prefabs/Modular Character/GanzSe Free Modular Character Update 1_1.prefab");
            if (_prefabCached != null) return _prefabCached;
#endif

            Debug.LogError(
                "[GanzSe] ModularCharacter prefab missing from Resources/GanzSe/. " +
                "Camp NPCs will use legacy sprites.");
            return null;
        }
    }
}
