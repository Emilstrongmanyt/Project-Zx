using ProjectZx.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectZx.UI
{
    public class GameHud : MonoBehaviour
    {
        Text _roundText;
        Text _hpText;
        Text _xpText;
        Text _goldText;
        Text _bannerText;
        float _bannerTimer;
        Transform _player;

        public static GameHud Instance { get; private set; }

        void Awake()
        {
            Instance = this;
            Build();
        }

        void Build()
        {
            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            var canvasGo = new GameObject("HudCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            _roundText = CreateText(canvasGo.transform, "Round 1", 30, new Vector2(-30, -30), TextAnchor.UpperLeft);
            _hpText = CreateText(canvasGo.transform, "HP 100/100", 26, new Vector2(-30, -70), TextAnchor.UpperLeft);
            _xpText = CreateText(canvasGo.transform, "XP 0", 26, new Vector2(-30, -110), TextAnchor.UpperLeft);
            _goldText = CreateText(canvasGo.transform, "Run Gold 0", 26, new Vector2(-30, -150), TextAnchor.UpperLeft);
            _bannerText = CreateText(canvasGo.transform, "", 36, Vector2.zero, TextAnchor.MiddleCenter);
            _bannerText.color = new Color(1f, 0.85f, 0.3f);
        }

        public void BindPlayer(Transform player) => _player = player;

        void Update()
        {
            if (_player == null) return;
            var stats = _player.GetComponent<PlayerStats>();
            if (stats == null) return;

            _hpText.text = $"HP {stats.CurrentHp}/{stats.MaxHp}";
            _xpText.text = $"XP {stats.RunXp}  Lv {stats.Level}";
            _goldText.text = $"Run Gold {stats.RunGold}";

            if (_bannerTimer > 0f)
            {
                _bannerTimer -= Time.deltaTime;
                if (_bannerTimer <= 0f) _bannerText.text = "";
            }

            if (stats.IsDead && _bannerTimer <= 0f)
            {
                _bannerText.text = "You fell. Gold banked. Returning to camp...";
                _bannerTimer = 999f;
            }
        }

        public void SetRound(int round) => _roundText.text = $"Round {round}";

        public void SetRoundComplete(int round)
        {
            _bannerText.text = $"Round {round} cleared!";
            _bannerTimer = 2f;
        }

        public void ShowBossWarning()
        {
            _bannerText.text = "BOSS INCOMING!";
            _bannerTimer = 2.5f;
        }

        static Text CreateText(Transform parent, string text, int size, Vector2 pos, TextAnchor anchor)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchor == TextAnchor.MiddleCenter ? new Vector2(0.5f, 0.5f) : new Vector2(0, 1);
            rect.anchorMax = anchor == TextAnchor.MiddleCenter ? new Vector2(0.5f, 0.5f) : new Vector2(0, 1);
            rect.pivot = anchor == TextAnchor.MiddleCenter ? new Vector2(0.5f, 0.5f) : new Vector2(0, 1);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(700, 50);
            var label = go.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = text;
            label.fontSize = size;
            label.color = Color.white;
            label.alignment = anchor;
            return label;
        }
    }
}