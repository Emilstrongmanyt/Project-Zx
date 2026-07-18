using ProjectZx.Core;
using ProjectZx.UI;
using UnityEngine;

namespace ProjectZx.Player
{
    public class CampHeroManager : MonoBehaviour
    {
        public static CampHeroManager Instance { get; private set; }

        const float PlayerScale = 0.42f * 1.3f;
        const float NpcScale = 0.38f;

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

        public void SelectHeroFromNpc(PlayableHero hero, Vector3 npcPosition)
        {
            if (hero == PlayableHero.RowZi && !GameSave.RowZiUnlocked) return;

            var selected = GameSave.SanitizeHero(hero);
            if (GameSave.SelectedHero == selected && _player != null) return;

            var oldPlayerPosition = _player != null ? _player.transform.position : DefaultPlayerSpawn;
            GameSave.SelectedHero = selected;
            // Stand in each other's place so the swap feels like trading spots.
            Refresh(npcPosition, oldPlayerPosition);
        }

        void Refresh(Vector3 playerPosition, Vector3 standbyPosition)
        {
            DestroyObject(_standbyNpc);
            DestroyObject(_player);

            var hero = GameSave.SanitizeHero(GameSave.SelectedHero);
            _player = GameFactory.CreatePlayer(playerPosition, false, GameSave.SelectedClass, hero, PlayerScale);

            var standby = GameSave.GetStandbyHero();
            if (!standby.HasValue) return;

            _standbyNpc = GameFactory.CreateHeroCampNpc(standbyPosition, standby.Value, NpcScale);
        }

        static void DestroyObject(GameObject go)
        {
            if (go != null) Destroy(go);
        }
    }
}