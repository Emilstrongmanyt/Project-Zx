using System.Collections.Generic;
using ProjectZx.Core;
using ProjectZx.Player;
using ProjectZx.Waves;
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
        GameObject _retreatPanel;
        Transform _choiceButtonRoot;
        float _bannerTimer;
        Transform _player;
        PlayerStats _stats;
        readonly List<GameObject> _choiceButtons = new();

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
            CreateUiIcon(canvasGo.transform, ArtLibrary.HpHeart, new Vector2(-58, -70), new Vector2(28, 28), TextAnchor.UpperLeft);
            _hpText = CreateText(canvasGo.transform, "HP 100/100", 24, new Vector2(-24, -70), TextAnchor.UpperLeft);
            _xpText = CreateText(canvasGo.transform, "Run XP 0/30", 24, new Vector2(-30, -108), TextAnchor.UpperLeft);
            CreateUiIcon(canvasGo.transform, ArtLibrary.GoldCoin, new Vector2(-58, -146), new Vector2(28, 28), TextAnchor.UpperLeft);
            _goldText = CreateText(canvasGo.transform, "Run Gold 0", 24, new Vector2(-24, -146), TextAnchor.UpperLeft);
            _bannerText = CreateText(canvasGo.transform, "", 36, Vector2.zero, TextAnchor.MiddleCenter);
            _bannerText.color = new Color(1f, 0.85f, 0.3f);

            _levelUpPanel = BuildLevelUpPanel(canvasGo.transform);
            _retreatPanel = BuildRetreatPanel(canvasGo.transform);
            CreateRetreatButton(canvasGo.transform);
        }

        void CreateRetreatButton(Transform parent)
        {
            var go = new GameObject("RetreatButton");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-30f, -30f);
            var size = new Vector2(220f, 52f);
            rect.sizeDelta = size;
            var image = go.AddComponent<Image>();
            UiSprites.ApplyButtonSprite(image, size);
            var button = go.AddComponent<Button>();
            button.onClick.AddListener(ShowRetreatConfirm);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var text = textGo.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = "Retreat";
            text.fontSize = 22;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
        }

        GameObject BuildRetreatPanel(Transform parent)
        {
            var panel = CreateDialogPanel(parent, "RetreatPanel", Vector2.zero, new Vector2(520, 280), ArtLibrary.ChallengeBoardUi);
            CreatePanelText(panel.transform, "Retreat to Camp?", 30, new Vector2(0, 70), new Vector2(460, 44));
            CreatePanelText(panel.transform, "Run gold will be saved. Current progress ends.", 20, new Vector2(0, 20), new Vector2(460, 60));

            CreateHudButton(panel.transform, "Yes, Retreat", new Vector2(-120, -70), ConfirmRetreat);
            CreateHudButton(panel.transform, "Keep Fighting", new Vector2(120, -70), () => _retreatPanel.SetActive(false));
            panel.SetActive(false);
            return panel;
        }

        void ShowRetreatConfirm()
        {
            if (IsChoosingUpgrade || _stats == null || _stats.IsDead) return;
            _retreatPanel.SetActive(true);
        }

        void ConfirmRetreat()
        {
            _retreatPanel.SetActive(false);
            SurvivalSession.Instance?.RetreatToCamp();
        }

        static void CreateHudButton(Transform parent, string label, Vector2 pos, System.Action onClick)
        {
            var go = new GameObject(label + "Button");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            var size = new Vector2(200, 52);
            rect.sizeDelta = size;
            var image = go.AddComponent<Image>();
            UiSprites.ApplyButtonSprite(image, size);
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
            text.fontSize = 20;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
        }

        GameObject BuildLevelUpPanel(Transform parent)
        {
            var panel = CreateDialogPanel(parent, "LevelUpPanel", Vector2.zero, new Vector2(560, 520), ArtLibrary.LevelUpUi);
            _levelUpTitle = CreatePanelText(panel.transform, "Level Up!", 34, new Vector2(0, 200), new Vector2(500, 50));
            CreatePanelText(panel.transform, "Pick one of four random boosts", 22, new Vector2(0, 155), new Vector2(500, 40));

            var rootGo = new GameObject("ChoiceButtons");
            rootGo.transform.SetParent(panel.transform, false);
            var rootRect = rootGo.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = new Vector2(0f, -20f);
            rootRect.sizeDelta = new Vector2(420f, 320f);
            _choiceButtonRoot = rootGo.transform;

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
            PopulateChoiceButtons();
            _levelUpPanel.SetActive(true);
        }

        void PopulateChoiceButtons()
        {
            ClearChoiceButtons();
            var choices = PlayerStats.RollLevelUpChoices(_stats, 4);
            var yStart = 90f;
            const float yStep = -80f;

            for (var i = 0; i < choices.Count; i++)
            {
                var choice = choices[i];
                var label = PlayerStats.GetChoiceLabel(choice);
                var y = yStart + yStep * i;
                CreateChoiceButton(_choiceButtonRoot, label, new Vector2(0f, y), () => ChooseUpgrade(choice));
            }
        }

        void ClearChoiceButtons()
        {
            foreach (var button in _choiceButtons)
            {
                if (button != null) Destroy(button);
            }
            _choiceButtons.Clear();
        }

        void ChooseUpgrade(RunLevelChoice choice)
        {
            if (_stats == null) return;

            _stats.ApplyRunLevelChoice(choice);

            if (_stats.PendingLevelUpChoices > 0)
            {
                _levelUpTitle.text = $"Level Up! ({_stats.PendingLevelUpChoices} picks)";
                PopulateChoiceButtons();
                return;
            }

            ClearChoiceButtons();
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

            if (IsChoosingUpgrade || (_retreatPanel != null && _retreatPanel.activeSelf)) return;

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

        static void CreateUiIcon(Transform parent, Sprite sprite, Vector2 pos, Vector2 size, TextAnchor anchor)
        {
            var go = new GameObject(sprite != null ? sprite.name + "Icon" : "Icon");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            var topAnchored = anchor == TextAnchor.UpperLeft || anchor == TextAnchor.UpperCenter || anchor == TextAnchor.UpperRight;
            if (topAnchored)
            {
                rect.anchorMin = new Vector2(anchor == TextAnchor.UpperRight ? 1f : anchor == TextAnchor.UpperCenter ? 0.5f : 0f, 1f);
                rect.anchorMax = rect.anchorMin;
                rect.pivot = new Vector2(anchor == TextAnchor.UpperRight ? 1f : anchor == TextAnchor.UpperCenter ? 0.5f : 0f, 1f);
            }
            else
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
            }

            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
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

        void CreateChoiceButton(Transform parent, string label, Vector2 pos, System.Action onClick)
        {
            var go = new GameObject(label + "Button");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            var size = new Vector2(360, 56);
            rect.sizeDelta = size;
            var image = go.AddComponent<Image>();
            UiSprites.ApplyButtonSprite(image, size);
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

            _choiceButtons.Add(go);
        }
    }
}