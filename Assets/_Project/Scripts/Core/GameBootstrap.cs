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
                BuildSurvival();
        }

        static void BuildMainMenu()
        {
            SetupCamera(new Color(0.12f, 0.2f, 0.14f));
            GameFactory.CreateGrassField("CampGrass", 44f, 34f, 1f);
            GameFactory.CreateCampfire(Vector3.zero);

            var hub = new GameObject("HubUi").AddComponent<HubUi>();
            GameFactory.CreatePlayer(new Vector3(0f, -4.2f), false);

            GameFactory.CreateNpc("WizardShop", ArtLibrary.Wizard, new Vector3(-2.1f, 1.1f), () => hub.OpenShop());
            GameFactory.CreateNpc("KnightChallenge", ArtLibrary.Knight, new Vector3(2.1f, 1.1f), () => hub.OpenMapSelect());
        }

        static void BuildSurvival()
        {
            SetupCamera(new Color(0.1f, 0.08f, 0.12f));
            const float arenaW = 64f;
            const float arenaH = 48f;
            GameFactory.CreateArenaField("ArenaFloor", arenaW, arenaH, 1f);
            GameFactory.ScatterArenaObstacles(arenaW, arenaH, 22);

            var player = GameFactory.CreatePlayer(Vector3.zero, true);
            var hud = new GameObject("GameHud").AddComponent<GameHud>();
            hud.BindPlayer(player.transform);

            var session = new GameObject("SurvivalSession").AddComponent<SurvivalSession>();
            session.Begin(player.transform, hud);
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