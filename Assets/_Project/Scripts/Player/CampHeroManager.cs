using ProjectZx.Core;
using ProjectZx.UI;
using UnityEngine;

namespace ProjectZx.Player
{
    public class CampHeroManager : MonoBehaviour
    {
        public static CampHeroManager Instance { get; private set; }

        const float PlayerScale = 0.42f;
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
            if (GameSave.SelectedHero == hero) return;

            var oldPlayerPosition = _player != null ? _player.transform.position : DefaultPlayerSpawn;
            GameSave.SelectedHero = hero;
            Refresh(npcPosition, oldPlayerPosition);
        }

        void Refresh(Vector3 playerPosition, Vector3 standbyPosition)
        {
            DestroyObject(_standbyNpc);
            DestroyObject(_player);

            _player = GameFactory.CreatePlayer(playerPosition, false, GameSave.SelectedClass, GameSave.SelectedHero, PlayerScale);

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