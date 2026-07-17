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
            SetupCamera(new Color(0.12f, 0.2f, 0.14f));
            GameFactory.CreateGrassField("CampGrass", 44f, 34f, 1f);
            GameFactory.ScatterArenaObstacles(40f, 30f, 6, 10, 0);

            var campfire = GameFactory.CreateCampfire(Vector3.zero);
            var hub = new GameObject("HubUi").AddComponent<HubUi>();
            new GameObject("CampHeroManager").AddComponent<CampHeroManager>().Setup();

            GameFactory.CreateNpc("WizardShop", ArtLibrary.Wizard, new Vector3(-2.1f, 1.1f), () => hub.OpenShop());
            GameFactory.CreateNpc("KnightChallenge", ArtLibrary.Knight, new Vector3(2.1f, 1.1f), () => hub.OpenMapSelect());
            GameFactory.CreateNpc("AchievementKeeper", ArtLibrary.AchievementKeeper, new Vector3(0f, 2.8f), () => hub.OpenAchievements());

            var campfireNpc = campfire.AddComponent<NpcInteractable>();
            campfireNpc.Initialize(() => hub.OpenCampfireTravel());

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
                    AudioManager.Instance?.PlayInsideBgm();
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
                    : new Color(0.14f, 0.28f, 0.12f));

            const float arenaW = 64f;
            const float arenaH = 48f;
            GameFactory.CreateTiledField(
                isDungeon ? "DungeonFloor" : isInside ? "InsideFloor" : "OutsideFloor",
                arenaW,
                arenaH,
                mapKind,
                1f);

            if (isInside)
                GameFactory.ScatterInsideObstacles(arenaW, arenaH);
            else if (isDungeon)
                GameFactory.ScatterCryptObstacles(arenaW, arenaH);
            else
                GameFactory.ScatterArenaObstacles(arenaW, arenaH, 14, 10, 3);

            var player = GameFactory.CreatePlayer(
                Vector3.zero,
                true,
                GameSessionContext.SelectedClass,
                GameSessionContext.SelectedHero);
            if (!GameSessionContext.FreshSurvivalRun)
            {
                var stats = player.GetComponent<PlayerStats>();
                stats?.RestoreSnapshot(GameSessionContext.RunSnapshot);
            }

            var hud = new GameObject("GameHud").AddComponent<GameHud>();
            hud.BindPlayer(player.transform);

            var session = new GameObject("SurvivalSession").AddComponent<SurvivalSession>();
            session.Begin(player.transform, hud, mapKind);

            var bossAudio = new GameObject("BossProximityAudio").AddComponent<BossProximityAudio>();
            bossAudio.BindPlayer(player.transform);
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