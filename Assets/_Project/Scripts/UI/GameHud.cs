using ProjectZx.Core;
using ProjectZx.Player;
using UnityEngine;
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
        Text _levelUpTitle;
        GameObject _levelUpPanel;
        float _bannerTimer;
        Transform _player;
        PlayerStats _stats;

        public static GameHud Instance { get; private set; }
        public bool IsChoosingUpgrade { get; private set; }

        void Awake()
        {
            Instance = this;
            Build();
        }

        void OnDestroy()
        {
            if (_stats != null)
                _stats.LevelUpChoiceRequired -= OnLevelUpChoiceRequired;
            if (Instance == this) Instance = null;
            if (IsChoosingUpgrade) Time.timeScale = 1f;
        }

        void Build()
        {
            EventSystemSetup.EnsureExists();

            var canvasGo = new GameObject("HudCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            _roundText = CreateText(canvasGo.transform, "Round 1", 30, new Vector2(-30, -30), TextAnchor.UpperLeft);
            _hpText = CreateText(canvasGo.transform, "HP 100/100", 26, new Vector2(-30, -70), TextAnchor.UpperLeft);
            _xpText = CreateText(canvasGo.transform, "Run XP 0/30", 26, new Vector2(-30, -110), TextAnchor.UpperLeft);
            _goldText = CreateText(canvasGo.transform, "Run Gold 0", 26, new Vector2(-30, -150), TextAnchor.UpperLeft);
            _bannerText = CreateText(canvasGo.transform, "", 36, Vector2.zero, TextAnchor.MiddleCenter);
            _bannerText.color = new Color(1f, 0.85f, 0.3f);

            _levelUpPanel = BuildLevelUpPanel(canvasGo.transform);
        }

        GameObject BuildLevelUpPanel(Transform parent)
        {
            var panel = CreateDialogPanel(parent, "LevelUpPanel", Vector2.zero, new Vector2(560, 460), ArtLibrary.LevelUpUi);
            _levelUpTitle = CreatePanelText(panel.transform, "Level Up!", 34, new Vector2(0, 170), new Vector2(500, 50));
            CreatePanelText(panel.transform, "Pick a run boost", 22, new Vector2(0, 120), new Vector2(500, 40));

            CreateChoiceButton(panel.transform, "+10% Speed", new Vector2(0, 55), () => ChooseUpgrade(RunLevelChoice.Speed));
            CreateChoiceButton(panel.transform, "+15 Max HP", new Vector2(0, -25), () => ChooseUpgrade(RunLevelChoice.Hp));
            CreateChoiceButton(panel.transform, "+12% Attack", new Vector2(0, -105), () => ChooseUpgrade(RunLevelChoice.Attack));
            CreateChoiceButton(panel.transform, "+12% Attack Speed", new Vector2(0, -185), () => ChooseUpgrade(RunLevelChoice.AttackSpeed));

            panel.SetActive(false);
            return panel;
        }

        public void BindPlayer(Transform player)
        {
            if (_stats != null)
                _stats.LevelUpChoiceRequired -= OnLevelUpChoiceRequired;

            _player = player;
            _stats = player != null ? player.GetComponent<PlayerStats>() : null;

            if (_stats != null)
                _stats.LevelUpChoiceRequired += OnLevelUpChoiceRequired;
        }

        void OnLevelUpChoiceRequired(int remaining)
        {
            if (_levelUpPanel == null || _stats == null) return;

            IsChoosingUpgrade = true;
            Time.timeScale = 0f;
            _levelUpTitle.text = remaining > 1 ? $"Level Up! ({remaining} picks)" : "Level Up!";
            _levelUpPanel.SetActive(true);
        }

        void ChooseUpgrade(RunLevelChoice choice)
        {
            if (_stats == null) return;

            _stats.ApplyRunLevelChoice(choice);

            if (_stats.PendingLevelUpChoices > 0)
            {
                _levelUpTitle.text = $"Level Up! ({_stats.PendingLevelUpChoices} picks)";
                return;
            }

            _levelUpPanel.SetActive(false);
            IsChoosingUpgrade = false;
            Time.timeScale = 1f;
        }

        void Update()
        {
            if (_player == null) return;
            var stats = _player.GetComponent<PlayerStats>();
            if (stats == null) return;

            _hpText.text = $"HP {stats.CurrentHp}/{stats.MaxHp}";
            _xpText.text = $"Run XP {stats.RunXp}/{stats.XpToNext}  Lv {stats.Level}";
            _goldText.text = $"Gold {stats.RunGold}";

            if (IsChoosingUpgrade) return;

            if (_bannerTimer > 0f)
            {
                _bannerTimer -= Time.deltaTime;
                if (_bannerTimer <= 0f) _bannerText.text = "";
            }

            if (stats.IsDead && _bannerTimer <= 0f)
            {
                _bannerText.text = "You fell";
                _bannerTimer = 999f;
            }
        }

        public void SetRound(int round, SurvivalMapKind mapKind)
        {
            var mapLabel = mapKind == SurvivalMapKind.Inside ? "Inside" : "Outside";
            _roundText.text = $"{mapLabel} — Round {round}";
        }

        public void SetRoundComplete(int round)
        {
            _bannerText.text = $"Round {round} cleared!";
            _bannerTimer = 2f;
        }

        public void ShowWaveIncoming()
        {
            _bannerText.text = "Zombies incoming!";
            _bannerTimer = 1.5f;
        }

        public void ShowBossWarning(bool roundTwentyBoss = false)
        {
            _bannerText.text = roundTwentyBoss ? "ROUND 20 BOSS!" : "BOSS INCOMING!";
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
            label.raycastTarget = false;
            return label;
        }

        static Text CreatePanelText(Transform parent, string text, int size, Vector2 pos, Vector2 sizeDelta)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = sizeDelta;
            var label = go.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = text;
            label.fontSize = size;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            return label;
        }

        static GameObject CreateDialogPanel(Transform parent, string name, Vector2 pos, Vector2 size, Sprite background)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            var image = go.AddComponent<Image>();
            if (background != null)
            {
                image.sprite = background;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }
            else
            {
                image.color = new Color(0.04f, 0.06f, 0.1f, 0.94f);
            }
            return go;
        }

        static void CreateChoiceButton(Transform parent, string label, Vector2 pos, System.Action onClick)
        {
            var go = new GameObject(label + "Button");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(360, 56);
            var image = go.AddComponent<Image>();
            image.color = new Color(0.18f, 0.32f, 0.5f, 0.96f);
            var button = go.AddComponent<Button>();
            button.onClick.AddListener(() => onClick());

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var text = textGo.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = label;
            text.fontSize = 24;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
        }
    }
}