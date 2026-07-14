using ProjectZx.UI;
using ProjectZx.Waves;
using ProjectZx.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectZx.Core
{
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Initialize()
        {
            var scene = SceneManager.GetActiveScene().name;
            if (scene == GameScenes.MainMenuMap)
                BuildMainMenu();
            else if (scene == GameScenes.SurvivalArena)
                BuildSurvival();
        }

        static void BuildMainMenu()
        {
            SetupCamera(new Color(0.12f, 0.2f, 0.14f));
            GameFactory.CreateGrassField("CampGrass", 44f, 34f, 1f);
            GameFactory.CreateCampfire(Vector3.zero);

            var hub = new GameObject("HubUi").AddComponent<HubUi>();
            var player = GameFactory.CreatePlayer(new Vector3(0f, -4.2f), false);

            GameFactory.CreateNpc("WizardShop", ArtLibrary.Wizard, new Vector3(-2.1f, 1.1f), () => hub.OpenShop());
            GameFactory.CreateNpc("KnightChallenge", ArtLibrary.Knight, new Vector3(2.1f, 1.1f), () => hub.OpenMapSelect());
        }

        static void BuildSurvival()
        {
            SetupCamera(new Color(0.08f, 0.1f, 0.14f));
            GameFactory.CreateGround("ArenaGround", new Color(0.22f, 0.16f, 0.12f, 1f), 50f, 50f);

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
            var follow = cam.gameObject.AddComponent<CenterCamera>();
            follow.BindWhenReady();
        }
    }
}