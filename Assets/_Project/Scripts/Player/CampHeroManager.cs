using ProjectZx.Core;
using ProjectZx.UI;
using UnityEngine;

namespace ProjectZx.Player
{
    public class CampHeroManager : MonoBehaviour
    {
        public static CampHeroManager Instance { get; private set; }

        const float PlayerScale = 0.42f * 1.3f;
        // Standby hero at campfire — +25% vs previous camp NPC scale.
        const float NpcScale = 0.38f * 1.25f;

        static readonly Vector3 DefaultPlayerSpawn = new(0f, -4.2f, 0f);
        static readonly Vector3 DefaultStandbySpawn = new(2.6f, -3.4f, 0f);

        GameObject _player;
        GameObject _standbyNpc;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Setup()
        {
            Refresh(DefaultPlayerSpawn, DefaultStandbySpawn);
        }

        /// <summary>Rebuild camp player/standby after skin or class appearance changes.</summary>
        public void RefreshAppearance()
        {
            var playerPos = _player != null ? _player.transform.position : DefaultPlayerSpawn;
            var standbyPos = _standbyNpc != null ? _standbyNpc.transform.position : DefaultStandbySpawn;
            Refresh(playerPos, standbyPos);
        }

        /// <summary>Hero swap is disabled — RowZi is companion-only and mirrors the player loadout.</summary>
        public void SelectHeroFromNpc(PlayableHero hero, Vector3 npcPosition)
        {
        }

        void Refresh(Vector3 playerPosition, Vector3 standbyPosition)
        {
            DestroyObject(_standbyNpc);
            DestroyObject(_player);

            GameSave.SelectedHero = PlayableHero.RollZy;
            var hero = PlayableHero.RollZy;
            _player = GameFactory.CreatePlayer(playerPosition, false, GameSave.SelectedClass, hero, PlayerScale);

            var standby = GameSave.GetStandbyHero();
            if (!standby.HasValue) return;

            // Decorative only — not tappable for swap.
            _standbyNpc = GameFactory.CreateHeroCampNpc(standbyPosition, standby.Value, NpcScale, interactive: false);
        }

        static void DestroyObject(GameObject go)
        {
            if (go != null) Destroy(go);
        }
    }
}