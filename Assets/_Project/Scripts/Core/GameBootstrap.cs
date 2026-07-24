using ProjectZx.Player;
using ProjectZx.UI;
using ProjectZx.Waves;
using ProjectZx.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectZx.Core
{
    public static class GameBootstrap
    {
        static bool _registered;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void RegisterSceneHook()
        {
            if (_registered) return;
            _registered = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == GameScenes.MainMenuMap)
                BuildMainMenu();
            else if (scene.name == GameScenes.SurvivalArena)
                BuildSurvival(GameSessionContext.SurvivalMap);
        }

        static void EnsureAudioManager()
        {
            if (AudioManager.Instance != null) return;
            new GameObject("AudioManager").AddComponent<AudioManager>();
        }

        static void BuildMainMenu()
        {
            EnsureAudioManager();
            AudioManager.Instance?.PlayCampBgm();
            // Deep water clear color so the ring reads clearly past the tile edge.
            SetupCamera(new Color(0.1f, 0.2f, 0.48f));
            GameFactory.CreateGrassField("CampGrass", ArenaBounds.CampWidth, ArenaBounds.CampHeight, 1f);

            // Keep trees/rocks off campfire, map NPCs, and hero stand points (generous radii).
            GameFactory.ClearScatterReservations();
            GameFactory.ReserveClearing(Vector2.zero, 4.5f);                 // campfire (map travel)
            GameFactory.ReserveClearing(new Vector2(-2.1f, 1.1f), 3.6f);     // wizard shop
            GameFactory.ReserveClearing(new Vector2(2.1f, 1.1f), 3.6f);      // knight map
            GameFactory.ReserveClearing(new Vector2(0f, 2.8f), 3.6f);        // achievement board
            GameFactory.ReserveClearing(new Vector2(-3.2f, -0.2f), 2.6f);    // treasure chest
            GameFactory.ReserveClearing(new Vector2(0f, -4.2f), 3.2f);       // player spawn
            GameFactory.ReserveClearing(new Vector2(2.6f, -3.4f), 3.0f);     // standby hero
            GameFactory.ReserveClearing(new Vector2(-2.6f, -3.4f), 2.8f);    // alternate hero slot
            // Extra pads between campfire and side NPCs (trees often wedged here).
            GameFactory.ReserveClearing(new Vector2(-1.2f, 0.6f), 2.4f);
            GameFactory.ReserveClearing(new Vector2(1.2f, 0.6f), 2.4f);
            GameFactory.ReserveClearing(new Vector2(0f, 1.4f), 2.8f);

            GameFactory.ScatterArenaObstacles(
                ArenaBounds.CampWidth - ArenaBounds.WaterMargin * 2f,
                ArenaBounds.CampHeight - ArenaBounds.WaterMargin * 2f,
                6, 10, 0);

            var campfire = GameFactory.CreateCampfire(Vector3.zero);
            var hub = new GameObject("HubUi").AddComponent<HubUi>();
            new GameObject("CampHeroManager").AddComponent<CampHeroManager>().Setup();

            // Campfire NPCs: +25% scale vs previous default (0.38 → 0.475).
            const float campNpcScale = 0.38f * 1.25f;
            GameFactory.CreateNpc("WizardShop", ArtLibrary.Wizard, new Vector3(-2.1f, 1.1f), () => hub.OpenShop(), campNpcScale);
            GameFactory.CreateNpc("KnightChallenge", ArtLibrary.Knight, new Vector3(2.1f, 1.1f), () => hub.OpenMapSelect(), campNpcScale);
            GameFactory.CreateNpc("AchievementBoard", ArtLibrary.AchievementKeeper, new Vector3(0f, 2.8f), () => hub.OpenAchievements(), 0.72f);
            GameFactory.CreateNpc("TreasureChest", ArtLibrary.TreasureChest, new Vector3(-3.2f, -0.2f), () => hub.OpenEquipmentChest(), 0.62f);

            var campfireNpc = campfire.AddComponent<NpcInteractable>();
            campfireNpc.Initialize(() => hub.OpenCampfireTravel());

            MovementJoystick.EnsureExists();
        }

        static void BuildSurvival(SurvivalMapKind mapKind)
        {
            EnsureAudioManager();
            switch (mapKind)
            {
                case SurvivalMapKind.Inside:
                    AudioManager.Instance?.PlayInsideBgm();
                    break;
                case SurvivalMapKind.Dungeon:
                    AudioManager.Instance?.PlayDungeonBgm();
                    break;
                default:
                    AudioManager.Instance?.PlayOutsideBgm();
                    break;
            }

            var isInside = mapKind == SurvivalMapKind.Inside;
            var isDungeon = mapKind == SurvivalMapKind.Dungeon;
            SetupCamera(isDungeon
                ? new Color(0.08f, 0.07f, 0.1f)
                : isInside
                    ? new Color(0.2f, 0.16f, 0.12f)
                    // Match water tile so survival shores read as a continuous lake edge.
                    : new Color(0.1f, 0.2f, 0.48f));

            const float arenaW = ArenaBounds.ArenaWidth;
            const float arenaH = ArenaBounds.ArenaHeight;
            GameFactory.CreateTiledField(
                isDungeon ? "DungeonFloor" : isInside ? "InsideFloor" : "OutsideFloor",
                arenaW,
                arenaH,
                mapKind,
                1f);

            GameFactory.ClearScatterReservations();
            GameFactory.ReserveClearing(Vector2.zero, 4.5f); // player spawn / fight space
            if (isInside)
                GameFactory.ScatterInsideObstacles(arenaW, arenaH);
            else if (isDungeon)
                GameFactory.ScatterCryptObstacles(arenaW, arenaH);
            else
                GameFactory.ScatterArenaObstacles(arenaW, arenaH, 14, 10, 3);

            var activeHero = GameSessionContext.SelectedHero;
            var activeClass = GameSave.GetHeroClass(activeHero);
            GameSessionContext.SelectedClass = activeClass;

            var player = GameFactory.CreatePlayer(
                Vector3.zero,
                true,
                activeClass,
                activeHero);
            var playerStats = player.GetComponent<PlayerStats>();
            if (!GameSessionContext.FreshSurvivalRun)
                playerStats?.RestoreSnapshot(GameSessionContext.RunSnapshot);

            // After RowZi unlock, the standby hero follows and assists with their own loadout.
            var standby = GameSave.GetStandbyHero();
            if (standby.HasValue && playerStats != null)
            {
                var companionClass = GameSave.GetHeroClass(standby.Value);
                GameFactory.CreateCompanion(player.transform, playerStats, standby.Value, companionClass);
            }

            var hud = new GameObject("GameHud").AddComponent<GameHud>();
            hud.BindPlayer(player.transform);

            var session = new GameObject("SurvivalSession").AddComponent<SurvivalSession>();
            session.Begin(player.transform, hud, mapKind);

            var bossAudio = new GameObject("BossProximityAudio").AddComponent<BossProximityAudio>();
            bossAudio.BindPlayer(player.transform);

            MovementJoystick.EnsureExists();
        }

        static void SetupCamera(Color background)
        {
            var cam = Camera.main;
            if (cam == null) return;

            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.backgroundColor = background;
            cam.transform.position = new Vector3(0f, 0f, -10f);

            if (cam.GetComponent<CenterCamera>() == null)
                cam.gameObject.AddComponent<CenterCamera>().BindWhenReady();
            else
                cam.GetComponent<CenterCamera>().BindWhenReady();
        }
    }
}