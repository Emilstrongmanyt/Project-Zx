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
        const float SafeLeft = 88f;
        const float SafeRight = 140f;
        const float SafeTop = 36f;

        Text _roundText;
        Text _hpText;
        Text _xpText;
        Text _goldText;
        Text _bannerText;
        Text _levelUpTitle;
        Text _achievementToastTitle;
        Text _achievementToastBody;
        GameObject _levelUpPanel;
        GameObject _retreatPanel;
        GameObject _achievementToast;
        Transform _choiceButtonRoot;
        float _bannerTimer;
        float _achievementToastTimer;
        Transform _player;
        PlayerStats _stats;
        readonly List<GameObject> _choiceButtons = new();

        public static GameHud Instance { get; private set; }
        public bool IsChoosingUpgrade { get; private set; }

        void Awake()
        {
            Instance = this;
            Build();
            Achievements.OnUnlocked += OnAchievementUnlocked;
        }

        void OnDestroy()
        {
            Achievements.OnUnlocked -= OnAchievementUnlocked;
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
            // Above damage floaters (40) and joystick (50) so level-up talents stay on top.
            canvas.sortingOrder = 100;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            _roundText = CreateText(canvasGo.transform, "Round 1", 34, new Vector2(SafeLeft, -SafeTop), TextAnchor.UpperLeft);
            CreateUiIcon(canvasGo.transform, ArtLibrary.HpHeart, new Vector2(SafeLeft, -SafeTop - 46f), new Vector2(34, 34), TextAnchor.UpperLeft);
            _hpText = CreateText(canvasGo.transform, "HP 100/100", 28, new Vector2(SafeLeft + 42f, -SafeTop - 46f), TextAnchor.UpperLeft);
            CreateUiIcon(canvasGo.transform, ArtLibrary.XpGem, new Vector2(SafeLeft, -SafeTop - 90f), new Vector2(32, 32), TextAnchor.UpperLeft);
            _xpText = CreateText(canvasGo.transform, "Run XP 0/30", 28, new Vector2(SafeLeft + 42f, -SafeTop - 90f), TextAnchor.UpperLeft);
            CreateUiIcon(canvasGo.transform, ArtLibrary.GoldCoin, new Vector2(SafeLeft, -SafeTop - 134f), new Vector2(32, 32), TextAnchor.UpperLeft);
            _goldText = CreateText(canvasGo.transform, "Run Gold 0", 28, new Vector2(SafeLeft + 42f, -SafeTop - 134f), TextAnchor.UpperLeft);
            _bannerText = CreateText(canvasGo.transform, "", 44, Vector2.zero, TextAnchor.MiddleCenter);
            _bannerText.color = new Color(1f, 0.85f, 0.3f);

            _levelUpPanel = BuildLevelUpPanel(canvasGo.transform);
            _retreatPanel = BuildRetreatPanel(canvasGo.transform);
            _achievementToast = BuildAchievementToast(canvasGo.transform);
            CreateRetreatButton(canvasGo.transform);
        }

        GameObject BuildAchievementToast(Transform parent)
        {
            var panel = CreateDialogPanel(parent, "AchievementToast", new Vector2(0f, 120f), new Vector2(560f, 170f), ArtLibrary.ChallengeBoardUi);
            _achievementToastTitle = CreatePanelText(panel.transform, "Achievement Unlocked!", 30, new Vector2(0f, 42f), new Vector2(500f, 40f));
            _achievementToastTitle.color = new Color(1f, 0.9f, 0.45f);
            _achievementToastBody = CreatePanelText(panel.transform, "", 24, new Vector2(0f, -20f), new Vector2(500f, 80f));
            panel.SetActive(false);
            return panel;
        }

        void OnAchievementUnlocked(AchievementDef def)
        {
            if (_achievementToast == null || def.Title == null) return;
            if (_achievementToastTitle != null)
                _achievementToastTitle.text = "Achievement Unlocked!";
            if (_achievementToastBody != null)
                _achievementToastBody.text = $"{def.Title}\n{def.Description}";
            _achievementToast.SetActive(true);
            _achievementToastTimer = 4f;
        }

        void CreateRetreatButton(Transform parent)
        {
            var go = new GameObject("RetreatButton");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-SafeRight, -SafeTop);
            var size = new Vector2(240f, 58f);
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
            text.fontSize = 26;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
        }

        GameObject BuildRetreatPanel(Transform parent)
        {
            var panel = CreateDialogPanel(parent, "RetreatPanel", Vector2.zero, new Vector2(560, 300), ArtLibrary.ChallengeBoardUi);
            CreatePanelText(panel.transform, "Retreat to Camp?", 34, new Vector2(0, 78), new Vector2(500, 48));
            CreatePanelText(panel.transform, "Run gold will be saved. Current progress ends.", 24, new Vector2(0, 22), new Vector2(500, 64));

            CreateHudButton(panel.transform, "Yes, Retreat", new Vector2(-130, -78), ConfirmRetreat);
            CreateHudButton(panel.transform, "Keep Fighting", new Vector2(130, -78), () => _retreatPanel.SetActive(false));
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
            var size = new Vector2(220, 58);
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
        }

        GameObject BuildLevelUpPanel(Transform parent)
        {
            var panel = CreateDialogPanel(parent, "LevelUpPanel", Vector2.zero, new Vector2(620, 580), ArtLibrary.LevelUpUi);
            _levelUpTitle = CreatePanelText(panel.transform, "Level Up!", 40, new Vector2(0, 220), new Vector2(560, 54));
            CreatePanelText(panel.transform, "Pick one of four random boosts", 26, new Vector2(0, 168), new Vector2(560, 44));

            var rootGo = new GameObject("ChoiceButtons");
            rootGo.transform.SetParent(panel.transform, false);
            var rootRect = rootGo.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = new Vector2(0f, -24f);
            rootRect.sizeDelta = new Vector2(480f, 360f);
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
            {
                _stats.LevelUpChoiceRequired += OnLevelUpChoiceRequired;
                // Campfire Blessing grants a free pick at run start.
                if (_stats.PendingLevelUpChoices > 0)
                    OnLevelUpChoiceRequired(_stats.PendingLevelUpChoices);
            }
        }

        void OnLevelUpChoiceRequired(int remaining)
        {
            if (_levelUpPanel == null || _stats == null) return;

            IsChoosingUpgrade = true;
            Time.timeScale = 0f;
            FloatingDamageNumber.ClearAll();
            _levelUpTitle.text = remaining > 1 ? $"Level Up! ({remaining} picks)" : "Level Up!";
            PopulateChoiceButtons();
            _levelUpPanel.SetActive(true);
        }

        void PopulateChoiceButtons()
        {
            ClearChoiceButtons();
            var choices = PlayerStats.RollLevelUpChoices(_stats, 4);
            var yStart = 100f;
            const float yStep = -88f;

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
            _xpText.text = stats.Level >= StatCaps.MaxRunLevel
                ? $"Run XP MAX  Lv {stats.Level}/{StatCaps.MaxRunLevel}"
                : $"Run XP {stats.RunXp}/{stats.XpToNext}  Lv {stats.Level}";
            _goldText.text = $"Gold {stats.RunGold}";

            if (IsChoosingUpgrade || (_retreatPanel != null && _retreatPanel.activeSelf)) return;

            if (_achievementToastTimer > 0f)
            {
                _achievementToastTimer -= Time.deltaTime;
                if (_achievementToastTimer <= 0f && _achievementToast != null)
                    _achievementToast.SetActive(false);
            }

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
            var mapLabel = mapKind switch
            {
                SurvivalMapKind.Inside => "Inside",
                SurvivalMapKind.Dungeon => "Dungeon",
                _ => "Outside"
            };
            _roundText.text = $"{mapLabel} — Round {round}";
        }

        public void SetRoundComplete(int round)
        {
            _bannerText.text = $"Round {round} cleared!";
            _bannerTimer = 2f;
        }

        public void ShowWaveIncoming(int wave = 1, int totalWaves = 1)
        {
            _bannerText.text = totalWaves > 1
                ? $"Wave {wave}/{totalWaves} — Zombies incoming!"
                : "Zombies incoming!";
            _bannerTimer = totalWaves > 1 ? 1.8f : 1.5f;
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
            rect.sizeDelta = new Vector2(820, 56);
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
            var size = new Vector2(420, 68);
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
            textRect.offsetMin = new Vector2(10f, 4f);
            textRect.offsetMax = new Vector2(-10f, -4f);
            var text = textGo.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = label;
            text.fontSize = 28;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;

            _choiceButtons.Add(go);
        }
    }
}