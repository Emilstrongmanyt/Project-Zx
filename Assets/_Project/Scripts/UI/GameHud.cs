using System.Collections;
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
        Text _dpsText;
        Text _objectiveText;
        Image _hpFill;
        Image _xpFill;
        Text _bannerText;
        Text _levelUpTitle;
        Text _epicTitle;
        Text _achievementToastTitle;
        Text _achievementToastBody;
        GameObject _levelUpPanel;
        GameObject _epicPanel;
        GameObject _retreatPanel;
        GameObject _runResultsPanel;
        GameObject _achievementToast;
        Transform _choiceButtonRoot;
        Transform _epicChoiceRoot;
        float _bannerTimer;
        bool _stickyBanner;
        float _achievementToastTimer;
        float _objectiveRefreshTimer;
        Transform _player;
        PlayerStats _stats;
        readonly List<GameObject> _choiceButtons = new();
        readonly List<GameObject> _epicChoiceButtons = new();
        readonly Queue<AchievementDef> _pendingAchievementToasts = new();
        AchievementDef _activeAchievementToast;
        bool _hasActiveAchievementToast;
        Text _retreatStatsText;
        Text _runResultsTitle;
        Text _runResultsBody;
        Button _unstuckButton;
        Text _unstuckLabel;
        GameObject _runResultsRetryButton;
        GameObject _runResultsCampButton;
        GameObject _runResultsNextButton;
        bool _unstuckUsedThisRun;
        bool _choosingLevelUp;
        bool _choosingEpic;
        bool _runResultsOpen;
        bool _runResultsHandled;
        SurvivalMapKind _runResultsMap = SurvivalMapKind.Outside;
        SurvivalMapKind _runResultsNextMap = SurvivalMapKind.Outside;
        bool _runResultsHasNext;

        public static GameHud Instance { get; private set; }
        public bool IsChoosingUpgrade => _choosingLevelUp || _choosingEpic;
        public bool IsRetreatMenuOpen => _retreatPanel != null && _retreatPanel.activeSelf;
        public bool IsRunResultsOpen => _runResultsOpen;
        /// <summary>Talent pick, retreat, or death results — freezes combat and blocks movement.</summary>
        public bool IsGamePaused => IsChoosingUpgrade || IsRetreatMenuOpen || IsRunResultsOpen;

        void Awake()
        {
            Instance = this;
            DpsTracker.Reset();
            Build();
            Achievements.OnUnlocked += OnAchievementUnlocked;
        }

        void OnDestroy()
        {
            Achievements.OnUnlocked -= OnAchievementUnlocked;
            if (_stats != null)
            {
                _stats.LevelUpChoiceRequired -= OnLevelUpChoiceRequired;
                _stats.EpicChoiceRequired -= OnEpicChoiceRequired;
            }
            if (Instance == this) Instance = null;
            if (IsGamePaused) Time.timeScale = 1f;
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

            _roundText = CreateText(canvasGo.transform, "Round 1", 30, new Vector2(SafeLeft, -SafeTop), TextAnchor.UpperLeft);
            _roundText.color = new Color(1f, 0.94f, 0.78f);

            // Stone-styled HP / XP bars + gold chip.
            BuildHudStatBar(
                canvasGo.transform,
                "HpBar",
                new Vector2(SafeLeft, -SafeTop - 42f),
                StoneUi.Available && StoneUi.IconHp != null ? StoneUi.IconHp : ArtLibrary.HpHeart,
                StoneUi.HudBarFillHp,
                out _hpFill,
                out _hpText,
                "100/100");

            BuildHudStatBar(
                canvasGo.transform,
                "XpBar",
                new Vector2(SafeLeft, -SafeTop - 98f),
                StoneUi.Available && StoneUi.IconXp != null ? StoneUi.IconXp : ArtLibrary.XpGem,
                StoneUi.HudBarFillXp,
                out _xpFill,
                out _xpText,
                "0/30");

            BuildHudGoldChip(canvasGo.transform, new Vector2(SafeLeft, -SafeTop - 154f));
            BuildHudDpsChip(canvasGo.transform, new Vector2(SafeLeft, -SafeTop - 208f));
            BuildObjectiveTracker(canvasGo.transform);

            _bannerText = CreateText(canvasGo.transform, "", 44, Vector2.zero, TextAnchor.MiddleCenter);
            _bannerText.color = new Color(1f, 0.85f, 0.3f);

            _levelUpPanel = BuildLevelUpPanel(canvasGo.transform);
            _epicPanel = BuildEpicTalentPanel(canvasGo.transform);
            _retreatPanel = BuildRetreatPanel(canvasGo.transform);
            _runResultsPanel = BuildRunResultsPanel(canvasGo.transform);
            _achievementToast = BuildAchievementToast(canvasGo.transform);
            CreateRetreatButton(canvasGo.transform);
            RefreshObjectiveTracker(force: true);
        }

        void BuildObjectiveTracker(Transform parent)
        {
            var chip = new GameObject("ObjectiveTracker");
            chip.transform.SetParent(parent, false);
            var chipRect = chip.AddComponent<RectTransform>();
            chipRect.anchorMin = new Vector2(1f, 1f);
            chipRect.anchorMax = new Vector2(1f, 1f);
            chipRect.pivot = new Vector2(1f, 1f);
            // Sit under the Retreat button (58px tall + gap).
            chipRect.anchoredPosition = new Vector2(-SafeRight, -SafeTop - 72f);
            chipRect.sizeDelta = new Vector2(420f, 96f);

            var bg = chip.AddComponent<Image>();
            if (StoneUi.Available && StoneUi.ResourceBarBg != null)
            {
                bg.sprite = StoneUi.ResourceBarBg;
                bg.type = Image.Type.Sliced;
                bg.color = new Color(1f, 1f, 1f, 0.92f);
            }
            else
            {
                bg.color = new Color(0.06f, 0.07f, 0.12f, 0.78f);
            }

            var textGo = new GameObject("ObjectiveText");
            textGo.transform.SetParent(chip.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16f, 10f);
            textRect.offsetMax = new Vector2(-16f, -10f);
            _objectiveText = textGo.AddComponent<Text>();
            _objectiveText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _objectiveText.fontSize = 22;
            _objectiveText.fontStyle = FontStyle.Bold;
            _objectiveText.color = new Color(1f, 0.94f, 0.8f);
            _objectiveText.alignment = TextAnchor.UpperRight;
            _objectiveText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _objectiveText.verticalOverflow = VerticalWrapMode.Truncate;
            _objectiveText.raycastTarget = false;
            _objectiveText.text = "";
            chip.SetActive(false);
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
            if (def.Title == null) return;
            HubUi.Instance?.RefreshGold();
            // Never cover level-up / epic talent options — queue toast until combat resumes.
            if (IsChoosingUpgrade)
            {
                SuppressAchievementToastForTalentMenu();
                _pendingAchievementToasts.Enqueue(def);
                return;
            }

            ShowAchievementToast(def);
        }

        void ShowAchievementToast(AchievementDef def)
        {
            if (_achievementToast == null || def.Title == null) return;
            // Talent menus always win — never draw toast over picks.
            if (IsChoosingUpgrade)
            {
                _pendingAchievementToasts.Enqueue(def);
                HideAchievementToastImmediate();
                return;
            }

            _activeAchievementToast = def;
            _hasActiveAchievementToast = true;
            if (_achievementToastTitle != null)
                _achievementToastTitle.text = "Achievement Unlocked!";
            if (_achievementToastBody != null)
                _achievementToastBody.text =
                    $"{def.Title}\n{def.Description}\n+{Achievements.CompletionGoldReward} gold";
            // Keep toast under dialog panels so a race cannot cover talent buttons.
            _achievementToast.transform.SetAsFirstSibling();
            _achievementToast.SetActive(true);
            _achievementToastTimer = 4f;
        }

        /// <summary>
        /// Hide any visible achievement toast before talent UI opens. Re-queues the active toast.
        /// </summary>
        void SuppressAchievementToastForTalentMenu()
        {
            if (_hasActiveAchievementToast)
            {
                _pendingAchievementToasts.Enqueue(_activeAchievementToast);
                _hasActiveAchievementToast = false;
            }

            HideAchievementToastImmediate();
        }

        void HideAchievementToastImmediate()
        {
            if (_achievementToast != null)
            {
                _achievementToast.SetActive(false);
                _achievementToast.transform.SetAsFirstSibling();
            }

            _achievementToastTimer = 0f;
            _hasActiveAchievementToast = false;
        }

        void FlushPendingAchievementToasts()
        {
            if (_pendingAchievementToasts.Count == 0 || IsChoosingUpgrade) return;
            ShowAchievementToast(_pendingAchievementToasts.Dequeue());
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
            var panel = CreateDialogPanel(parent, "RetreatPanel", Vector2.zero, new Vector2(760, 820), ArtLibrary.ChallengeBoardUi);
            CreatePanelText(panel.transform, "Retreat to Camp?", 36, new Vector2(0, 350), new Vector2(700, 48));
            CreatePanelText(
                panel.transform,
                "Run gold is saved. Current run progress ends.",
                22,
                new Vector2(0, 308),
                new Vector2(700, 36));

            _retreatStatsText = CreatePanelText(
                panel.transform,
                "",
                20,
                new Vector2(0, 40),
                new Vector2(680, 420));
            _retreatStatsText.alignment = TextAnchor.UpperLeft;
            _retreatStatsText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _retreatStatsText.verticalOverflow = VerticalWrapMode.Truncate;
            _retreatStatsText.color = new Color(0.92f, 0.94f, 0.98f);

            // Once per run: teleport back to map spawn (0,0).
            _unstuckButton = CreateHudButton(panel.transform, "Unstuck", new Vector2(0, -250), UseUnstuck);
            _unstuckLabel = _unstuckButton != null
                ? _unstuckButton.GetComponentInChildren<Text>()
                : null;

            CreateHudButton(panel.transform, "Yes, Retreat", new Vector2(-150, -340), ConfirmRetreat);
            CreateHudButton(panel.transform, "Keep Fighting", new Vector2(150, -340), CloseRetreatPanel);
            panel.SetActive(false);
            return panel;
        }

        void ShowRetreatConfirm()
        {
            if (IsChoosingUpgrade || IsRunResultsOpen || _stats == null || _stats.IsDead) return;
            Time.timeScale = 0f;
            FloatingDamageNumber.ClearAll();
            SuppressAchievementToastForTalentMenu();
            RefreshRetreatStats();
            RefreshUnstuckButton();
            if (_retreatPanel != null)
            {
                _retreatPanel.transform.SetAsLastSibling();
                _retreatPanel.SetActive(true);
            }
        }

        GameObject BuildRunResultsPanel(Transform parent)
        {
            var panel = CreateDialogPanel(parent, "RunResultsPanel", Vector2.zero, new Vector2(760, 640), ArtLibrary.ChallengeBoardUi);
            _runResultsTitle = CreatePanelText(panel.transform, "Run Over", 40, new Vector2(0, 240), new Vector2(700, 52));
            _runResultsTitle.color = new Color(1f, 0.86f, 0.45f);
            _runResultsBody = CreatePanelText(panel.transform, "", 26, new Vector2(0, 20), new Vector2(680, 320));
            _runResultsBody.alignment = TextAnchor.UpperCenter;
            _runResultsBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            _runResultsBody.verticalOverflow = VerticalWrapMode.Truncate;
            _runResultsBody.color = new Color(0.93f, 0.95f, 0.98f);

            _runResultsRetryButton = CreateHudButton(panel.transform, "Retry", new Vector2(-220, -230), ConfirmRunResultsRetry).gameObject;
            _runResultsCampButton = CreateHudButton(panel.transform, "Camp", new Vector2(0, -230), ConfirmRunResultsCamp).gameObject;
            _runResultsNextButton = CreateHudButton(panel.transform, "Next Map", new Vector2(220, -230), ConfirmRunResultsNext).gameObject;
            panel.SetActive(false);
            return panel;
        }

        /// <summary>
        /// Death / stage-clear / victory results — pauses until the player picks a button.
        /// Stage clears pass <paramref name="nextMap"/> to offer Enter Next Map.
        /// </summary>
        public IEnumerator ShowRunResultsAndWait(
            bool died,
            int round,
            int kills,
            int goldBanked,
            SurvivalMapKind mapKind,
            SurvivalMapKind? nextMap = null,
            string titleOverride = null,
            string unlockSummary = null)
        {
            if (_runResultsPanel == null)
            {
                _runResultsHandled = true;
                yield break;
            }

            _runResultsMap = mapKind;
            _runResultsHasNext = nextMap.HasValue;
            if (nextMap.HasValue)
                _runResultsNextMap = nextMap.Value;

            _runResultsHandled = false;
            _runResultsOpen = true;
            Time.timeScale = 0f;
            FloatingDamageNumber.ClearAll();
            SuppressAchievementToastForTalentMenu();
            ClearStickyBanner();

            if (_retreatPanel != null)
                _retreatPanel.SetActive(false);

            if (_runResultsTitle != null)
            {
                _runResultsTitle.text = !string.IsNullOrEmpty(titleOverride)
                    ? titleOverride
                    : died ? "You Fell" : "Run Complete";
            }

            var mapLabel = SurvivalMapNames.ShortName(mapKind);

            var footer = died
                ? "Your run gold is safe at camp.\nRetry this map or return home."
                : _runResultsHasNext
                    ? "Gold is banked. Return to camp or enter the next map."
                    : "Victory! Gold is banked at camp.";

            var unlockBlock = string.IsNullOrEmpty(unlockSummary) ? "" : $"\n{unlockSummary}\n";

            var body =
                $"{mapLabel} Survival\n\n" +
                $"Round reached: {round}\n" +
                $"Kills: {kills}\n" +
                $"Gold banked: {GoldFormat.Abbreviate(goldBanked)}\n" +
                unlockBlock +
                $"\n{footer}";

            if (_runResultsBody != null)
                _runResultsBody.text = body;

            // Death: Retry + Camp. Stage clear: Camp + Next Map. Victory: Camp only.
            if (_runResultsRetryButton != null)
                _runResultsRetryButton.SetActive(died);
            if (_runResultsNextButton != null)
                _runResultsNextButton.SetActive(!died && _runResultsHasNext);
            if (_runResultsCampButton != null)
            {
                _runResultsCampButton.SetActive(true);
                var campRect = _runResultsCampButton.GetComponent<RectTransform>();
                if (campRect != null)
                {
                    if (died)
                        campRect.anchoredPosition = new Vector2(150, -230);
                    else if (_runResultsHasNext)
                        campRect.anchoredPosition = new Vector2(-150, -230);
                    else
                        campRect.anchoredPosition = new Vector2(0, -230);
                }
            }

            if (_runResultsRetryButton != null && died)
            {
                var retryRect = _runResultsRetryButton.GetComponent<RectTransform>();
                if (retryRect != null)
                    retryRect.anchoredPosition = new Vector2(-150, -230);
            }

            if (_runResultsNextButton != null && !died && _runResultsHasNext)
            {
                var nextRect = _runResultsNextButton.GetComponent<RectTransform>();
                if (nextRect != null)
                    nextRect.anchoredPosition = new Vector2(150, -230);
                var nextLabel = _runResultsNextButton.GetComponentInChildren<Text>();
                if (nextLabel != null)
                    nextLabel.text = NextMapButtonLabel(_runResultsNextMap);
            }

            _runResultsPanel.transform.SetAsLastSibling();
            _runResultsPanel.SetActive(true);

            while (!_runResultsHandled)
                yield return null;
        }

        static string NextMapButtonLabel(SurvivalMapKind map) => map switch
        {
            SurvivalMapKind.Inside => "Enter Warded Halls",
            SurvivalMapKind.Dungeon => "Enter Ironvault",
            SurvivalMapKind.Crypt => "Enter Silent Ossuary",
            SurvivalMapKind.Unlimited => "Enter Endless Front",
            _ => "Next Map"
        };

        void ConfirmRunResultsRetry()
        {
            if (!_runResultsOpen) return;
            CloseRunResultsPanel();

            // Keep banked gold toast meaningful after a death retry (fresh run, no camp visit).
            GameSave.ClearLastRunToast();
            GameSessionContext.ClearPendingNextMap();
            GameSessionContext.SurvivalMap = _runResultsMap;
            GameSessionContext.FreshSurvivalRun = true;
            GameSessionContext.StartingRound = 0;
            GameSessionContext.CarryRound = 0;
            GameSessionContext.RunSnapshot = default;
            GameFactory.LoadScene(GameScenes.SurvivalArena);
        }

        void ConfirmRunResultsCamp()
        {
            if (!_runResultsOpen) return;
            CloseRunResultsPanel();

            GameSessionContext.ClearPendingNextMap();
            GameSessionContext.FreshSurvivalRun = true;
            GameSessionContext.StartingRound = 0;
            GameSessionContext.CarryRound = 0;
            GameSessionContext.RunSnapshot = default;
            GameFactory.LoadScene(GameScenes.MainMenuMap);
        }

        void ConfirmRunResultsNext()
        {
            if (!_runResultsOpen || !_runResultsHasNext) return;
            CloseRunResultsPanel();

            GameSave.ClearLastRunToast();
            GameSessionContext.SurvivalMap = _runResultsNextMap;
            GameSessionContext.ClearPendingNextMap();
            GameSessionContext.FreshSurvivalRun = true;
            GameSessionContext.StartingRound = 0;
            GameSessionContext.CarryRound = 0;
            GameSessionContext.RunSnapshot = default;
            GameFactory.LoadScene(GameScenes.SurvivalArena);
        }

        void CloseRunResultsPanel()
        {
            _runResultsHandled = true;
            _runResultsOpen = false;
            if (_runResultsPanel != null)
                _runResultsPanel.SetActive(false);
            Time.timeScale = 1f;
        }

        void RefreshRetreatStats()
        {
            if (_retreatStatsText == null) return;
            if (_stats == null)
            {
                _retreatStatsText.text = "No run data.";
                return;
            }

            _retreatStatsText.text = "Current upgrades\n\n" + _stats.BuildRunStatusSummary();
        }

        void RefreshUnstuckButton()
        {
            if (_unstuckButton == null) return;
            var available = !_unstuckUsedThisRun && _stats != null && !_stats.IsDead;
            _unstuckButton.interactable = available;
            if (_unstuckLabel != null)
                _unstuckLabel.text = _unstuckUsedThisRun ? "Unstuck (used)" : "Unstuck";
        }

        void UseUnstuck()
        {
            if (_unstuckUsedThisRun || _stats == null || _stats.IsDead) return;

            _unstuckUsedThisRun = true;
            var spawn = Vector2.zero;
            var rb = _stats.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.position = spawn;
            }
            else
            {
                _stats.transform.position = new Vector3(spawn.x, spawn.y, _stats.transform.position.z);
            }

            // Companion follows after unstuck so they do not stay stuck elsewhere.
            var companion = Object.FindAnyObjectByType<CompanionFollower>();
            if (companion != null)
            {
                var cRb = companion.GetComponent<Rigidbody2D>();
                var cPos = spawn + Vector2.left * 0.8f;
                if (cRb != null)
                {
                    cRb.linearVelocity = Vector2.zero;
                    cRb.position = cPos;
                }
                else
                {
                    companion.transform.position = new Vector3(cPos.x, cPos.y, companion.transform.position.z);
                }
            }

            RefreshUnstuckButton();
            ShowBanner("Returned to spawn. Unstuck used for this run.", 2.6f);
            CloseRetreatPanel();
        }

        void CloseRetreatPanel()
        {
            if (_retreatPanel != null)
                _retreatPanel.SetActive(false);
            if (!IsChoosingUpgrade)
                Time.timeScale = 1f;
        }

        void ConfirmRetreat()
        {
            if (_retreatPanel != null)
                _retreatPanel.SetActive(false);
            Time.timeScale = 1f;
            SurvivalSession.Instance?.RetreatToCamp();
        }

        static Button CreateHudButton(Transform parent, string label, Vector2 pos, System.Action onClick)
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
            return button;
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

        GameObject BuildEpicTalentPanel(Transform parent)
        {
            var panel = CreateDialogPanel(parent, "EpicTalentPanel", Vector2.zero, new Vector2(680, 620), ArtLibrary.LevelUpUi);
            _epicTitle = CreatePanelText(panel.transform, "Epic Talent!", 40, new Vector2(0, 240), new Vector2(620, 54));
            _epicTitle.color = new Color(0.92f, 0.55f, 1f);
            CreatePanelText(panel.transform, "Boss crystal — choose one powerful talent", 24, new Vector2(0, 185), new Vector2(620, 44));

            var rootGo = new GameObject("EpicChoiceButtons");
            rootGo.transform.SetParent(panel.transform, false);
            var rootRect = rootGo.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = new Vector2(0f, -30f);
            rootRect.sizeDelta = new Vector2(560f, 400f);
            _epicChoiceRoot = rootGo.transform;

            panel.SetActive(false);
            return panel;
        }

        public void BindPlayer(Transform player)
        {
            if (_stats != null)
            {
                _stats.LevelUpChoiceRequired -= OnLevelUpChoiceRequired;
                _stats.EpicChoiceRequired -= OnEpicChoiceRequired;
            }

            _player = player;
            _stats = player != null ? player.GetComponent<PlayerStats>() : null;

            if (_stats != null)
            {
                _stats.LevelUpChoiceRequired += OnLevelUpChoiceRequired;
                _stats.EpicChoiceRequired += OnEpicChoiceRequired;
                // Campfire Blessing grants a free pick at run start.
                if (_stats.PendingLevelUpChoices > 0)
                    OnLevelUpChoiceRequired(_stats.PendingLevelUpChoices);
                else if (_stats.PendingEpicChoices > 0)
                    OnEpicChoiceRequired(_stats.PendingEpicChoices);
            }
        }

        void OnLevelUpChoiceRequired(int remaining)
        {
            if (_levelUpPanel == null || _stats == null) return;
            // Epic panel takes priority if already open; level-up will resume after.
            if (_choosingEpic) return;

            OpenLevelUpPanel(remaining);
        }

        void OpenLevelUpPanel(int remaining)
        {
            _choosingLevelUp = true;
            Time.timeScale = 0f;
            FloatingDamageNumber.ClearAll();
            // Achievement toast must be gone before talent buttons appear.
            SuppressAchievementToastForTalentMenu();
            if (_levelUpTitle != null)
                _levelUpTitle.text = remaining > 1 ? $"Level Up! ({remaining} picks)" : "Level Up!";
            PopulateChoiceButtons();
            if (_levelUpPanel != null)
            {
                _levelUpPanel.transform.SetAsLastSibling();
                _levelUpPanel.SetActive(true);
            }

            SparkleBurst.Play(_levelUpPanel.transform, new Vector2(0f, 180f), 18);
        }

        void OnEpicChoiceRequired(int remaining)
        {
            if (_epicPanel == null || _stats == null) return;
            // Finish level-up first if both are pending.
            if (_choosingLevelUp) return;

            OpenEpicPanel(remaining);
        }

        void OpenEpicPanel(int remaining)
        {
            _choosingEpic = true;
            Time.timeScale = 0f;
            FloatingDamageNumber.ClearAll();
            SuppressAchievementToastForTalentMenu();
            if (_epicTitle != null)
            {
                _epicTitle.text = remaining > 1
                    ? $"Epic Talent! ({remaining} picks)"
                    : "Epic Talent!";
            }

            PopulateEpicChoiceButtons();
            if (_epicPanel != null)
            {
                _epicPanel.transform.SetAsLastSibling();
                _epicPanel.SetActive(true);
            }

            SparkleBurst.Play(_epicPanel.transform, new Vector2(0f, 180f), 22);
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
                CreateChoiceButton(_choiceButtonRoot, label, new Vector2(0f, y), () => ChooseUpgrade(choice), _choiceButtons);
            }
        }

        void PopulateEpicChoiceButtons()
        {
            ClearEpicChoiceButtons();
            var choices = EpicTalentCatalog.RollChoices(_stats != null ? _stats.EpicOwnedMask : 0);
            var yStart = 110f;
            const float yStep = -108f;

            for (var i = 0; i < choices.Count; i++)
            {
                var choice = choices[i];
                var label = EpicTalentCatalog.GetButtonLabel(choice);
                var y = yStart + yStep * i;
                CreateChoiceButton(
                    _epicChoiceRoot,
                    label,
                    new Vector2(0f, y),
                    () => ChooseEpicTalent(choice),
                    _epicChoiceButtons,
                    new Vector2(520f, 92f),
                    22);
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

        void ClearEpicChoiceButtons()
        {
            foreach (var button in _epicChoiceButtons)
            {
                if (button != null) Destroy(button);
            }
            _epicChoiceButtons.Clear();
        }

        void ChooseUpgrade(RunLevelChoice choice)
        {
            if (_stats == null) return;

            _stats.ApplyRunLevelChoice(choice);
            if (_levelUpPanel != null)
                SparkleBurst.Play(_levelUpPanel.transform, Vector2.zero, 14);

            if (_stats.PendingLevelUpChoices > 0)
            {
                if (_levelUpTitle != null)
                    _levelUpTitle.text = $"Level Up! ({_stats.PendingLevelUpChoices} picks)";
                PopulateChoiceButtons();
                return;
            }

            ClearChoiceButtons();
            _levelUpPanel.SetActive(false);
            _choosingLevelUp = false;

            if (_stats.PendingEpicChoices > 0)
            {
                OpenEpicPanel(_stats.PendingEpicChoices);
                return;
            }

            ResumeAfterTalentSelection();
        }

        void ChooseEpicTalent(EpicTalentId choice)
        {
            if (_stats == null) return;

            if (!_stats.ApplyEpicTalent(choice))
            {
                // Pick was ignored (no pending choices) — keep panel open if still needed.
                if (_stats.PendingEpicChoices > 0)
                    PopulateEpicChoiceButtons();
                return;
            }

            if (_epicPanel != null)
                SparkleBurst.Play(_epicPanel.transform, Vector2.zero, 16);

            // Phoenix needs a clear "armed" signal so players know the revive is ready.
            if (choice == EpicTalentId.PhoenixHeart)
                ShowBanner("Phoenix Heart armed — first death revives you!", 2.8f);
            else
                ShowBanner($"{EpicTalentCatalog.GetTitle(choice)}!", 2f);

            if (_stats.PendingEpicChoices > 0)
            {
                if (_epicTitle != null)
                    _epicTitle.text = $"Epic Talent! ({_stats.PendingEpicChoices} picks)";
                PopulateEpicChoiceButtons();
                return;
            }

            ClearEpicChoiceButtons();
            _epicPanel.SetActive(false);
            _choosingEpic = false;

            if (_stats.PendingLevelUpChoices > 0)
            {
                OpenLevelUpPanel(_stats.PendingLevelUpChoices);
                return;
            }

            ResumeAfterTalentSelection();
        }

        void ResumeAfterTalentSelection()
        {
            Time.timeScale = 1f;
            _stats?.GrantTalentSelectionIFrames(1f);
            FlushPendingAchievementToasts();
        }

        void Update()
        {
            if (_player == null) return;
            var stats = _player.GetComponent<PlayerStats>();
            if (stats == null) return;

            if (_hpText != null)
                _hpText.text = $"{stats.CurrentHp}/{stats.MaxHp}";
            if (_hpFill != null)
                _hpFill.fillAmount = stats.MaxHp > 0
                    ? Mathf.Clamp01((float)stats.CurrentHp / stats.MaxHp)
                    : 0f;

            if (stats.Level >= StatCaps.MaxRunLevel)
            {
                if (_xpText != null) _xpText.text = $"MAX Lv {stats.Level}";
                if (_xpFill != null) _xpFill.fillAmount = 1f;
            }
            else
            {
                if (_xpText != null) _xpText.text = $"{stats.RunXp}/{stats.XpToNext}  Lv {stats.Level}";
                if (_xpFill != null)
                    _xpFill.fillAmount = stats.XpToNext > 0
                        ? Mathf.Clamp01((float)stats.RunXp / stats.XpToNext)
                        : 0f;
            }

            if (_goldText != null)
                _goldText.text = GoldFormat.Abbreviate(stats.RunGold);

            // Freeze DPS during talent pick / retreat (timeScale is already 0; explicit flag too).
            DpsTracker.SetPaused(IsGamePaused);
            if (!IsGamePaused)
                DpsTracker.Tick();

            if (_dpsText != null)
                _dpsText.text = FormatDpsLabel(DpsTracker.DisplayDps);

            // Objective tracker still refreshes while paused so stage-hold text stays visible.
            RefreshObjectiveTracker(force: false);

            if (IsGamePaused) return;

            if (_achievementToastTimer > 0f)
            {
                _achievementToastTimer -= Time.deltaTime;
                if (_achievementToastTimer <= 0f)
                {
                    HideAchievementToastImmediate();
                    // Chain any toasts that fired while a talent menu was open.
                    FlushPendingAchievementToasts();
                }
            }
            else if (_pendingAchievementToasts.Count > 0
                     && (_achievementToast == null || !_achievementToast.activeSelf))
            {
                FlushPendingAchievementToasts();
            }

            if (_bannerTimer > 0f && !_stickyBanner)
            {
                _bannerTimer -= Time.deltaTime;
                if (_bannerTimer <= 0f && _bannerText != null)
                    _bannerText.text = "";
            }
        }

        void RefreshObjectiveTracker(bool force)
        {
            if (_objectiveText == null) return;

            _objectiveRefreshTimer -= Time.unscaledDeltaTime;
            if (!force && _objectiveRefreshTimer > 0f) return;
            _objectiveRefreshTimer = 0.45f;

            var session = SurvivalSession.Instance;
            string line = null;
            if (session != null && session.IsStageHoldActive)
            {
                var hold = session.GetStageHoldObjective();
                if (!string.IsNullOrEmpty(hold))
                    line = $"Objective: {hold}";
            }

            if (string.IsNullOrEmpty(line))
                line = QuestCatalog.BuildHudObjectiveLine();

            var chip = _objectiveText.transform.parent != null
                ? _objectiveText.transform.parent.gameObject
                : null;

            if (string.IsNullOrEmpty(line))
            {
                if (chip != null) chip.SetActive(false);
                _objectiveText.text = "";
                return;
            }

            if (chip != null) chip.SetActive(true);
            _objectiveText.text = line;
        }

        public void SetRound(int round, SurvivalMapKind mapKind)
        {
            var mapLabel = SurvivalMapNames.ShortName(mapKind);
            _roundText.text = $"{mapLabel} — Round {round}";
        }

        public void SetRoundComplete(int round)
        {
            _bannerText.text = $"Round {round} cleared!";
            _bannerTimer = 2f;
        }

        public void ShowBanner(string message, float duration = 2f)
        {
            if (_bannerText == null || string.IsNullOrEmpty(message)) return;
            _stickyBanner = false;
            _bannerText.text = message;
            _bannerTimer = duration;
        }

        /// <summary>Stage-hold portal guidance — stays until cleared or a normal banner replaces it.</summary>
        public void ShowStickyBanner(string message)
        {
            if (_bannerText == null || string.IsNullOrEmpty(message)) return;
            _stickyBanner = true;
            _bannerText.text = message;
            _bannerTimer = 9999f;
            RefreshObjectiveTracker(force: true);
        }

        public void ClearStickyBanner()
        {
            _stickyBanner = false;
            _bannerTimer = 0f;
            if (_bannerText != null)
                _bannerText.text = "";
        }

        public void ShowWaveIncoming(int wave = 1, int totalWaves = 1)
        {
            _bannerText.text = totalWaves > 1
                ? $"Wave {wave}/{totalWaves} — Zombies incoming!"
                : "Zombies incoming!";
            _bannerTimer = totalWaves > 1 ? 1.8f : 1.5f;
        }

        public void ShowBossWarning(bool stageBoss = false)
        {
            if (stageBoss && GameSessionContext.SurvivalMap == SurvivalMapKind.Crypt)
                _bannerText.text = "MINOTAUR — STAGE BOSS!";
            else
                _bannerText.text = stageBoss ? "STAGE BOSS!" : "BOSS INCOMING!";
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
            UiSprites.ApplyPanelSprite(image, background, largeMenu: false);
            return go;
        }

        static void BuildHudStatBar(
            Transform parent,
            string name,
            Vector2 pos,
            Sprite icon,
            Sprite fillSprite,
            out Image fillImage,
            out Text label,
            string initialText)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(0f, 1f);
            rootRect.pivot = new Vector2(0f, 1f);
            rootRect.anchoredPosition = pos;
            rootRect.sizeDelta = new Vector2(360f, 48f);

            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(root.transform, false);
            var iconRect = iconGo.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(0f, 0f);
            iconRect.sizeDelta = new Vector2(40f, 40f);
            var iconImage = iconGo.AddComponent<Image>();
            iconImage.sprite = icon;
            iconImage.raycastTarget = false;

            var bar = new GameObject("Bar");
            bar.transform.SetParent(root.transform, false);
            var barRect = bar.AddComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0f, 0.5f);
            barRect.anchorMax = new Vector2(0f, 0.5f);
            barRect.pivot = new Vector2(0f, 0.5f);
            barRect.anchoredPosition = new Vector2(48f, 0f);
            barRect.sizeDelta = new Vector2(300f, 36f);

            var track = bar.AddComponent<Image>();
            if (StoneUi.Available && StoneUi.HudBarBorder != null)
            {
                track.sprite = StoneUi.HudBarBorder;
                track.type = Image.Type.Sliced;
                track.color = Color.white;
            }
            else
            {
                track.color = new Color(0.08f, 0.1f, 0.14f, 0.82f);
            }

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(bar.transform, false);
            var fillRect = fillGo.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(8f, 7f);
            fillRect.offsetMax = new Vector2(-8f, -7f);
            fillImage = fillGo.AddComponent<Image>();
            if (fillSprite != null)
            {
                fillImage.sprite = fillSprite;
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Horizontal;
                fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
                fillImage.fillAmount = 1f;
                fillImage.color = Color.white;
            }
            else
            {
                fillImage.color = new Color(0.35f, 0.78f, 0.42f, 0.95f);
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Horizontal;
                fillImage.fillAmount = 1f;
            }
            fillImage.raycastTarget = false;

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(bar.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12f, 0f);
            textRect.offsetMax = new Vector2(-12f, 0f);
            label = textGo.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = initialText;
            label.fontSize = 20;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
        }

        void BuildHudGoldChip(Transform parent, Vector2 pos)
        {
            var chip = new GameObject("GoldChip");
            chip.transform.SetParent(parent, false);
            var chipRect = chip.AddComponent<RectTransform>();
            chipRect.anchorMin = new Vector2(0f, 1f);
            chipRect.anchorMax = new Vector2(0f, 1f);
            chipRect.pivot = new Vector2(0f, 1f);
            chipRect.anchoredPosition = pos;
            chipRect.sizeDelta = new Vector2(200f, 48f);

            var bg = chip.AddComponent<Image>();
            if (StoneUi.Available && StoneUi.ResourceBarBg != null)
            {
                bg.sprite = StoneUi.ResourceBarBg;
                bg.type = Image.Type.Sliced;
                bg.color = Color.white;
            }
            else
            {
                bg.color = new Color(0.08f, 0.1f, 0.14f, 0.82f);
            }

            var coinSprite = StoneUi.Available && StoneUi.ResourceIconCoin != null
                ? StoneUi.ResourceIconCoin
                : StoneUi.Available && StoneUi.IconGold != null
                    ? StoneUi.IconGold
                    : ArtLibrary.GoldCoin;

            var coinGo = new GameObject("Coin");
            coinGo.transform.SetParent(chip.transform, false);
            var coinRect = coinGo.AddComponent<RectTransform>();
            coinRect.anchorMin = new Vector2(0f, 0.5f);
            coinRect.anchorMax = new Vector2(0f, 0.5f);
            coinRect.pivot = new Vector2(0f, 0.5f);
            coinRect.anchoredPosition = new Vector2(10f, 0f);
            coinRect.sizeDelta = new Vector2(36f, 36f);
            var coinImage = coinGo.AddComponent<Image>();
            coinImage.sprite = coinSprite;
            coinImage.raycastTarget = false;

            var textGo = new GameObject("GoldText");
            textGo.transform.SetParent(chip.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(48f, 0f);
            textRect.offsetMax = new Vector2(-10f, 0f);
            _goldText = textGo.AddComponent<Text>();
            _goldText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _goldText.text = "0";
            _goldText.fontSize = 24;
            _goldText.color = new Color(1f, 0.94f, 0.72f);
            _goldText.alignment = TextAnchor.MiddleLeft;
            _goldText.raycastTarget = false;
        }

        void BuildHudDpsChip(Transform parent, Vector2 pos)
        {
            // Single calm 3s-window DPS chip (matches gold chip styling).
            var chip = new GameObject("DpsChip");
            chip.transform.SetParent(parent, false);
            var chipRect = chip.AddComponent<RectTransform>();
            chipRect.anchorMin = new Vector2(0f, 1f);
            chipRect.anchorMax = new Vector2(0f, 1f);
            chipRect.pivot = new Vector2(0f, 1f);
            chipRect.anchoredPosition = pos;
            chipRect.sizeDelta = new Vector2(200f, 44f);

            var bg = chip.AddComponent<Image>();
            if (StoneUi.Available && StoneUi.ResourceBarBg != null)
            {
                bg.sprite = StoneUi.ResourceBarBg;
                bg.type = Image.Type.Sliced;
                bg.color = Color.white;
            }
            else
            {
                bg.color = new Color(0.1f, 0.08f, 0.12f, 0.82f);
            }

            var iconSprite = ArtLibrary.Arrow != null
                ? ArtLibrary.Arrow
                : ArtLibrary.Sparkles != null
                    ? ArtLibrary.Sparkles
                    : ArtLibrary.GoldCoin;

            if (iconSprite != null)
            {
                var iconGo = new GameObject("DpsIcon");
                iconGo.transform.SetParent(chip.transform, false);
                var iconRect = iconGo.AddComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0f, 0.5f);
                iconRect.anchorMax = new Vector2(0f, 0.5f);
                iconRect.pivot = new Vector2(0f, 0.5f);
                iconRect.anchoredPosition = new Vector2(10f, 0f);
                iconRect.sizeDelta = new Vector2(32f, 32f);
                var iconImage = iconGo.AddComponent<Image>();
                iconImage.sprite = iconSprite;
                iconImage.color = new Color(1f, 0.78f, 0.55f, 1f);
                iconImage.raycastTarget = false;
            }

            var textGo = new GameObject("DpsText");
            textGo.transform.SetParent(chip.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(iconSprite != null ? 46f : 12f, 0f);
            textRect.offsetMax = new Vector2(-10f, 0f);
            _dpsText = textGo.AddComponent<Text>();
            _dpsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _dpsText.text = "DPS 0";
            _dpsText.fontSize = 22;
            _dpsText.fontStyle = FontStyle.Bold;
            _dpsText.color = new Color(1f, 0.78f, 0.58f);
            _dpsText.alignment = TextAnchor.MiddleLeft;
            _dpsText.raycastTarget = false;
        }

        static string FormatDpsLabel(float dps)
        {
            if (dps < 1f) return "DPS 0";
            if (dps < 1000f) return $"DPS {Mathf.RoundToInt(dps)}";
            return $"DPS {GoldFormat.Abbreviate(Mathf.RoundToInt(dps))}";
        }

        void CreateChoiceButton(
            Transform parent,
            string label,
            Vector2 pos,
            System.Action onClick,
            List<GameObject> trackList,
            Vector2? sizeOverride = null,
            int fontSize = 28)
        {
            var go = new GameObject("ChoiceButton");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            var size = sizeOverride ?? new Vector2(420, 68);
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
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;

            trackList.Add(go);
        }
    }
}