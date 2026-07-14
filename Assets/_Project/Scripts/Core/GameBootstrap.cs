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
            SetupCamera();
            GameFactory.CreateGround("CampGround", new Color(0.18f, 0.28f, 0.16f, 1f), 40f, 30f);

            var player = GameFactory.CreatePlayer(Vector3.zero, false);
            var hub = new GameObject("HubUi").AddComponent<HubUi>();

            GameFactory.CreateNpc("WizardShop", ArtLibrary.Wizard, new Vector3(-5f, 0f), "Wizard — Tap to open upgrades", () => hub.OpenShop());
            GameFactory.CreateNpc("KnightChallenge", ArtLibrary.Knight, new Vector3(5f, 0f), "Knight — Tap to choose a map", () => hub.OpenMapSelect());

            CreateLabel("Upgrade Shop", new Vector3(-5f, 2.2f));
            CreateLabel("Challenge Board", new Vector3(5f, 2.2f));
        }

        static void BuildSurvival()
        {
            SetupCamera();
            GameFactory.CreateGround("ArenaGround", new Color(0.22f, 0.16f, 0.12f, 1f), 50f, 50f);

            var player = GameFactory.CreatePlayer(Vector3.zero, true);
            var hud = new GameObject("GameHud").AddComponent<GameHud>();
            hud.BindPlayer(player.transform);

            var session = new GameObject("SurvivalSession").AddComponent<SurvivalSession>();
            session.Begin(player.transform, hud);
        }

        static void SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.backgroundColor = new Color(0.08f, 0.1f, 0.14f);
            cam.transform.position = new Vector3(0f, 0f, -10f);
            var follow = cam.gameObject.AddComponent<CenterCamera>();
            follow.BindWhenReady();
        }

        static void CreateLabel(string text, Vector3 worldPos)
        {
            var go = new GameObject(text);
            go.transform.position = worldPos;
            var mesh = go.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.fontSize = 28;
            mesh.characterSize = 0.08f;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.color = new Color(0.95f, 0.95f, 0.8f);
        }
    }
}