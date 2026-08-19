using System;
using System.Collections.Generic;
using ProjectZx.Core;
using ProjectZx.HeroEditor;
using ProjectZx.Player;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectZx.UI
{
    public class HubUi : MonoBehaviour
    {
        public static HubUi Instance { get; private set; }

        /// <summary>True while any camp menu (shop, settings, map, etc.) is open.</summary>
        public bool IsAnyMenuOpen =>
            IsPanelOpen(_shopPanel) || IsPanelOpen(_loadoutPanel) || IsPanelOpen(_statsPanel)
            || IsPanelOpen(_achievementsPanel) || IsPanelOpen(_mapPanel) || IsPanelOpen(_campfirePanel)
            || IsPanelOpen(_equipmentPanel) || IsPanelOpen(_settingsPanel) || IsPanelOpen(_questPanel)
            || IsPanelOpen(_onboardingPanel)
            || CharacterCreatorUi.IsOpen;

        static bool IsPanelOpen(GameObject panel) => panel != null && panel.activeSelf;

        const float SafeRight = 140f;
        const float SafeTop = 36f;
        /// <summary>Shared almost-full-screen size for shop, settings, loadout, stats, etc.</summary>
        static readonly Vector2 HubMenuPanelSize = new Vector2(1100f, 980f);
        /// <summary>Uniform shrink so large Stone-bordered menus fit phone safe areas.</summary>
        const float HubMenuScale = 0.85f;

        Text _goldText;
        Text _statsBodyText;
        Text _achievementCountText;

        struct AchievementRowRefs
        {
            public AchievementId Id;
            public Image Background;
            public Text TitleText;
            public Text DescText;
        }

        readonly List<AchievementRowRefs> _achievementRows = new();

        GameObject _shopPanel;
        GameObject _loadoutPanel;
        GameObject _statsPanel;
        GameObject _achievementsPanel;
        GameObject _mapPanel;
        GameObject _campfirePanel;
        GameObject _equipmentPanel;
        GameObject _settingsPanel;
        GameObject _questPanel;
        Image _questPortraitImage;
        GameObject _questPortraitFrame;
        Text _questTitleText;
        Text _questBodyText;
        Text _questStatusText;
        Button _questAcceptButton;
        Button _questTurnInButton;
        Text _questAcceptLabel;
        Text _questTurnInLabel;
        QuestId _questPanelFocusId = QuestId.GrandWizardsPeril;
        float _questPortraitAnimTimer;
        int _questPortraitFrameIndex;
        Text _equipmentStatusText;
        Text _bgmVolumeLabel;
        Text _sfxVolumeLabel;
        Text _campObjectiveText;
        Image _campObjectiveBg;
        GameObject _campObjectiveChip;
        float _campObjectivePulse;
        bool _readyTurnInToastShown;
        readonly List<Button> _equipmentButtons = new();

        struct ClassPickerRefs
        {
            public Text StatusText;
            public Button BatterButton;
            public Button SpearmanButton;
            public Button BowmanButton;
            public Button SamuraiButton;
            public Button MagicianButton;
        }

        ClassPickerRefs _loadoutClassPicker;
        Text _techniqueStatusText;
        Button _techniqueStandardButton;
        Button _techniqueSpecialButton;
        Text _weaponTierStatusText;
        Button _weaponTierPrevButton;
        Button _weaponTierNextButton;
        Button _movementJoystickButton;
        Button _movementTapHoldButton;
        Button _rollZyClassicSkinButton;
        Button _rollZyUpgradedSkinButton;
        Text _rollZySkinStatusText;

        enum ShopUpgradeKind
        {
            MaxHp,
            Damage,
            Speed,
            Range,
            GoldMagnet,
            ThickHide,
            SecondWind,
            CampfireBlessing,
            Whirlwind,
            PiercingShot,
            FrostTip
        }

        struct UpgradeRowRefs
        {
            public Text Label;
            public Button BuyButton;
            public Button InfoButton;
            public Image CoinIcon;
            public ShopUpgradeKind Kind;
        }

        UpgradeRowRefs _hpRow;
        UpgradeRowRefs _damageRow;
        UpgradeRowRefs _speedRow;
        UpgradeRowRefs _rangeRow;
        UpgradeRowRefs _whirlwindRow;
        UpgradeRowRefs _piercingShotRow;
        UpgradeRowRefs _frostTipRow;
        UpgradeRowRefs _goldMagnetRow;
        UpgradeRowRefs _thickHideRow;
        UpgradeRowRefs _secondWindRow;
        UpgradeRowRefs _campfireBlessingRow;
        GameObject _shopInfoPanel;
        Text _shopInfoTitle;
        Text _shopInfoBody;
        GameObject _onboardingPanel;
        Text _onboardingTitle;
        Text _onboardingBody;
        Text _onboardingStepLabel;
        int _onboardingStep;
        GameObject _runToastPanel;
        Text _runToastText;
        float _runToastTimer;
        Button _largeDamageNumbersButton;
        Text _largeDamageNumbersLabel;

        static readonly string[] OnboardingSteps =
        {
            "Welcome to Project Zx!\n\nThis is your camp. Upgrades and gold you bank from runs stay here forever.",
            "Talk to the Wizard (left of the campfire) to buy permanent upgrades.\nWhirlwind is a strong early pick.",
            "Talk to the Knight (right of the campfire) to start Outside Survival.\nSurvive waves, level up, and bank gold on death or retreat.",
            "Talk to the Grand Wizard for quests.\nQuests teach the map unlock chain and pay gold rewards.",
            "Tip: Use Retreat anytime to bank gold safely.\nUnstuck (once per run) returns you to the map spawn.\n\nGood luck, clanker!"
        };

        void Awake()
        {
            Instance = this;
            Build();
            RefreshGold();
            Achievements.OnUnlocked += OnAchievementUnlockedAtCamp;
        }

        void Start()
        {
            TryShowLastRunToast();
            // Character creator runs before camp world spawn (GameBootstrap); only onboarding remains here.
            TryShowOnboarding();
            RefreshCampObjectiveTracker(force: true);
            TryShowReadyTurnInToast();
        }

        void OnDestroy()
        {
            Achievements.OnUnlocked -= OnAchievementUnlockedAtCamp;
            MovementJoystick.SetRepositionMode(false);
            if (Instance == this) Instance = null;
        }

        void OnAchievementUnlockedAtCamp(AchievementDef _)
        {
            RefreshGold();
        }

        void Build()
        {
            EventSystemSetup.EnsureExists();

            var canvasGo = new GameObject("HubCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            BuildHubGoldDisplay(canvasGo.transform);

            // Always available on the campfire map.
            CreateTopRightButton(canvasGo.transform, "Settings", new Vector2(-SafeRight, -SafeTop - 70f), OpenSettings);
            BuildCampObjectiveTracker(canvasGo.transform);

            _shopPanel = BuildShopPanel(canvasGo.transform);
            _loadoutPanel = BuildLoadoutPanel(canvasGo.transform);
            _statsPanel = BuildStatsPanel(canvasGo.transform);
            _achievementsPanel = BuildAchievementsPanel(canvasGo.transform);
            _mapPanel = BuildMapPanel(canvasGo.transform);
            _campfirePanel = BuildCampfirePanel(canvasGo.transform);
            _equipmentPanel = BuildEquipmentPanel(canvasGo.transform);
            _settingsPanel = BuildSettingsPanel(canvasGo.transform);
            _questPanel = BuildQuestPanel(canvasGo.transform);
            _onboardingPanel = BuildOnboardingPanel(canvasGo.transform);
            _runToastPanel = BuildRunToastPanel(canvasGo.transform);
        }

        void Update()
        {
            AnimateQuestPortraitTalk();
            TickRunToast();
            RefreshCampObjectiveTracker(force: false);
            PulseCampObjectiveChip();
        }

        void BuildCampObjectiveTracker(Transform parent)
        {
            _campObjectiveChip = new GameObject("CampObjectiveTracker");
            _campObjectiveChip.transform.SetParent(parent, false);
            var chipRect = _campObjectiveChip.AddComponent<RectTransform>();
            chipRect.anchorMin = new Vector2(1f, 1f);
            chipRect.anchorMax = new Vector2(1f, 1f);
            chipRect.pivot = new Vector2(1f, 1f);
            chipRect.anchoredPosition = new Vector2(-SafeRight, -SafeTop - 150f);
            chipRect.sizeDelta = new Vector2(440f, 100f);

            _campObjectiveBg = _campObjectiveChip.AddComponent<Image>();
            if (StoneUi.Available && StoneUi.ResourceBarBg != null)
            {
                _campObjectiveBg.sprite = StoneUi.ResourceBarBg;
                _campObjectiveBg.type = Image.Type.Sliced;
                _campObjectiveBg.color = new Color(1f, 1f, 1f, 0.92f);
            }
            else
            {
                _campObjectiveBg.color = new Color(0.06f, 0.07f, 0.12f, 0.78f);
            }

            var button = _campObjectiveChip.AddComponent<Button>();
            button.targetGraphic = _campObjectiveBg;
            button.onClick.AddListener(OnCampObjectiveClicked);

            var textGo = new GameObject("ObjectiveText");
            textGo.transform.SetParent(_campObjectiveChip.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16f, 10f);
            textRect.offsetMax = new Vector2(-16f, -10f);
            _campObjectiveText = textGo.AddComponent<Text>();
            _campObjectiveText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _campObjectiveText.fontSize = 22;
            _campObjectiveText.fontStyle = FontStyle.Bold;
            _campObjectiveText.color = new Color(1f, 0.94f, 0.8f);
            _campObjectiveText.alignment = TextAnchor.UpperRight;
            _campObjectiveText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _campObjectiveText.verticalOverflow = VerticalWrapMode.Truncate;
            _campObjectiveText.raycastTarget = false;
            _campObjectiveText.text = "";
            _campObjectiveChip.SetActive(false);
        }

        float _campObjectiveRefreshTimer;

        void RefreshCampObjectiveTracker(bool force)
        {
            if (_campObjectiveText == null || _campObjectiveChip == null) return;
            if (IsAnyMenuOpen)
            {
                _campObjectiveChip.SetActive(false);
                return;
            }

            _campObjectiveRefreshTimer -= Time.unscaledDeltaTime;
            if (!force && _campObjectiveRefreshTimer > 0f) return;
            _campObjectiveRefreshTimer = 0.5f;

            var line = QuestCatalog.BuildHudObjectiveLine();
            if (string.IsNullOrEmpty(line))
            {
                _campObjectiveChip.SetActive(false);
                _campObjectiveText.text = "";
                return;
            }

            _campObjectiveChip.SetActive(true);
            _campObjectiveText.text = line;

            var ready = QuestCatalog.HasReadyToTurnInQuest();
            if (_campObjectiveBg != null)
            {
                _campObjectiveBg.color = ready
                    ? new Color(1f, 0.86f, 0.35f, 0.95f)
                    : StoneUi.Available && StoneUi.ResourceBarBg != null
                        ? new Color(1f, 1f, 1f, 0.92f)
                        : new Color(0.06f, 0.07f, 0.12f, 0.78f);
            }

            if (_campObjectiveText != null)
            {
                _campObjectiveText.color = ready
                    ? new Color(0.18f, 0.12f, 0.04f)
                    : new Color(1f, 0.94f, 0.8f);
            }
        }

        void PulseCampObjectiveChip()
        {
            if (_campObjectiveChip == null || !_campObjectiveChip.activeSelf) return;
            if (!QuestCatalog.HasReadyToTurnInQuest()) return;

            _campObjectivePulse += Time.unscaledDeltaTime * 3.2f;
            var scale = 1f + Mathf.Sin(_campObjectivePulse) * 0.035f;
            _campObjectiveChip.transform.localScale = new Vector3(scale, scale, 1f);
        }

        void TryShowReadyTurnInToast()
        {
            if (_readyTurnInToastShown) return;
            if (!QuestCatalog.TryGetReadyToTurnInQuest(out var def)) return;
            _readyTurnInToastShown = true;
            SparkleBurst.Play(transform, new Vector2(520f, 280f), 14);
            // Chip pulse + gold tint already call attention; sparkle reinforces once per camp visit.
            _ = def;
        }

        void OnCampObjectiveClicked()
        {
            if (QuestCatalog.TryGetReadyToTurnInQuest(out var ready))
            {
                OpenQuestDialogue(ready.Id);
                return;
            }

            if (QuestCatalog.TryGetActiveQuest(out var active))
            {
                OpenQuestDialogue(active.Id);
                return;
            }

            OpenQuestGiver();
        }

        GameObject BuildOnboardingPanel(Transform parent)
        {
            // Compact coach panel — not full hub size.
            var panel = CreateDialogPanel(parent, "OnboardingPanel", Vector2.zero, new Vector2(720f, 520f), ArtLibrary.ChallengeBoardUi);
            _onboardingTitle = CreateText(panel.transform, "Getting Started", 32, TextAnchor.MiddleCenter, new Vector2(0, 190), new Vector2(640, 40));
            _onboardingStepLabel = CreateText(panel.transform, "1 / 5", 20, TextAnchor.MiddleCenter, new Vector2(0, 148), new Vector2(160, 28));
            _onboardingStepLabel.color = new Color(1f, 0.9f, 0.55f);
            _onboardingBody = CreateText(panel.transform, "", 22, TextAnchor.MiddleCenter, new Vector2(0, 10), new Vector2(640, 260));
            _onboardingBody.alignment = TextAnchor.UpperCenter;
            CreateButton(panel.transform, "Skip", new Vector2(-150, -190), CompleteOnboarding);
            CreateButton(panel.transform, "Next", new Vector2(150, -190), AdvanceOnboarding, large: true);
            panel.SetActive(false);
            return panel;
        }

        GameObject BuildRunToastPanel(Transform parent)
        {
            var panel = new GameObject("RunToast");
            panel.transform.SetParent(parent, false);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -SafeTop - 8f);
            rect.sizeDelta = new Vector2(720f, 72f);

            var bg = panel.AddComponent<Image>();
            if (StoneUi.Available && StoneUi.ResourceBarBg != null)
            {
                bg.sprite = StoneUi.ResourceBarBg;
                bg.type = Image.Type.Sliced;
                bg.color = Color.white;
            }
            else
            {
                bg.color = new Color(0.08f, 0.1f, 0.14f, 0.9f);
            }

            _runToastText = CreateText(panel.transform, "", 24, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(680f, 60f));
            _runToastText.color = new Color(1f, 0.94f, 0.72f);
            panel.SetActive(false);
            return panel;
        }

        void TryShowLastRunToast()
        {
            if (_runToastPanel == null || _runToastText == null) return;
            if (GameSave.LastRunGoldBanked <= 0 && GameSave.LastRunRound <= 0) return;

            var gold = GameSave.LastRunGoldBanked;
            var round = GameSave.LastRunRound;
            var kills = GameSave.LastRunKills;
            var died = GameSave.LastRunWasDeath;

            var parts = new System.Text.StringBuilder();
            if (gold > 0)
                parts.Append($"Banked {GoldFormat.Abbreviate(gold)} gold");
            if (round > 0)
            {
                if (parts.Length > 0) parts.Append("  ·  ");
                parts.Append(died ? $"Fell at R{round}" : $"Reached R{round}");
            }
            if (kills > 0)
            {
                if (parts.Length > 0) parts.Append("  ·  ");
                parts.Append($"{kills} kills");
            }

            if (parts.Length == 0) return;

            _runToastText.text = parts.ToString();
            _runToastPanel.SetActive(true);
            _runToastTimer = 5.5f;
            GameSave.ClearLastRunToast();
        }

        void TickRunToast()
        {
            if (_runToastPanel == null || !_runToastPanel.activeSelf) return;
            _runToastTimer -= Time.unscaledDeltaTime;
            if (_runToastTimer > 0f) return;
            _runToastPanel.SetActive(false);
        }

        void TryShowOnboarding()
        {
            if (GameSave.OnboardingCompleted) return;
            if (_onboardingPanel == null) return;
            _onboardingStep = 0;
            RefreshOnboardingStep();
            CloseAllHubPanels();
            _onboardingPanel.SetActive(true);
        }

        void RefreshOnboardingStep()
        {
            if (_onboardingBody == null) return;
            var total = OnboardingSteps.Length;
            var index = Mathf.Clamp(_onboardingStep, 0, total - 1);
            _onboardingBody.text = OnboardingSteps[index];
            if (_onboardingStepLabel != null)
                _onboardingStepLabel.text = $"{index + 1} / {total}";
            if (_onboardingTitle != null)
                _onboardingTitle.text = index == 0 ? "Welcome" : "Getting Started";
        }

        void AdvanceOnboarding()
        {
            if (_onboardingStep >= OnboardingSteps.Length - 1)
            {
                CompleteOnboarding();
                return;
            }

            _onboardingStep++;
            RefreshOnboardingStep();
        }

        void CompleteOnboarding()
        {
            GameSave.OnboardingCompleted = true;
            if (_onboardingPanel != null)
                _onboardingPanel.SetActive(false);
        }

        /// <summary>
        /// Cycles the 6-frame Wizard Portrait (2×3 of 64×64) while the quest dialogue is open
        /// so the wizard appears to talk during the quest text.
        /// </summary>
        void AnimateQuestPortraitTalk()
        {
            if (!IsPanelOpen(_questPanel) || _questPortraitImage == null) return;
            if (!QuestCatalog.UsesQuestPortrait(_questPanelFocusId)) return;

            var frames = ArtLibrary.WizardPortraitFrames;
            if (frames == null || frames.Length == 0) return;

            // Single-frame fallback: still nudge slightly so the portrait feels alive.
            if (frames.Length == 1)
            {
                _questPortraitAnimTimer += Time.unscaledDeltaTime;
                var bob = 1f + Mathf.Sin(_questPortraitAnimTimer * 6f) * 0.012f;
                _questPortraitImage.rectTransform.localScale = new Vector3(bob, bob, 1f);
                return;
            }

            // Talk cadence: slightly irregular frame steps read better than a rigid loop.
            _questPortraitAnimTimer += Time.unscaledDeltaTime;
            var step = 0.11f + (_questPortraitFrameIndex % 3) * 0.02f;
            if (_questPortraitAnimTimer < step) return;
            _questPortraitAnimTimer = 0f;

            // Hold closed-mouth frame 0 a bit longer between syllables.
            if (_questPortraitFrameIndex == 0 && UnityEngine.Random.value < 0.35f)
            {
                _questPortraitImage.sprite = frames[0];
                return;
            }

            _questPortraitFrameIndex = (_questPortraitFrameIndex + 1) % frames.Length;
            _questPortraitImage.sprite = frames[_questPortraitFrameIndex];
            _questPortraitImage.rectTransform.localScale = Vector3.one;
        }

        GameObject BuildShopPanel(Transform parent)
        {
            var panel = CreateDialogPanel(parent, "ShopPanel", Vector2.zero, HubMenuPanelSize, ArtLibrary.ShopUi);
            CreateText(panel.transform, "Upgrade Shop", 40, TextAnchor.MiddleCenter, new Vector2(0, 430), new Vector2(620, 52));
            CreateText(panel.transform, "Tap Info for full effects & current totals.", 18, TextAnchor.MiddleCenter, new Vector2(0, 385), new Vector2(800, 28));

            var scrollRoot = new GameObject("ShopScroll");
            scrollRoot.transform.SetParent(panel.transform, false);
            var scrollRectTransform = scrollRoot.AddComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            scrollRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRectTransform.pivot = new Vector2(0.5f, 0.5f);
            scrollRectTransform.anchoredPosition = new Vector2(0f, 20f);
            scrollRectTransform.sizeDelta = new Vector2(1000f, 660f);

            var scroll = scrollRoot.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 32f;

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollRoot.transform, false);
            var viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewport.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            scroll.viewport = viewportRect;

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            scroll.content = contentRect;

            var y = -10f;
            const float step = -64f;
            _hpRow = CreateShopUpgradeRow(content.transform, ShopUpgradeKind.MaxHp, y, BuyHp);
            y += step;
            _damageRow = CreateShopUpgradeRow(content.transform, ShopUpgradeKind.Damage, y, BuyDamage);
            y += step;
            _speedRow = CreateShopUpgradeRow(content.transform, ShopUpgradeKind.Speed, y, BuySpeed);
            y += step;
            _rangeRow = CreateShopUpgradeRow(content.transform, ShopUpgradeKind.Range, y, BuyRange);
            y += step;
            _goldMagnetRow = CreateShopUpgradeRow(content.transform, ShopUpgradeKind.GoldMagnet, y, BuyGoldMagnet);
            y += step;
            _thickHideRow = CreateShopUpgradeRow(content.transform, ShopUpgradeKind.ThickHide, y, BuyThickHide);
            y += step;
            _secondWindRow = CreateShopUpgradeRow(content.transform, ShopUpgradeKind.SecondWind, y, BuySecondWind);
            y += step;
            _campfireBlessingRow = CreateShopUpgradeRow(content.transform, ShopUpgradeKind.CampfireBlessing, y, BuyCampfireBlessing);
            y += step;
            _whirlwindRow = CreateShopUpgradeRow(content.transform, ShopUpgradeKind.Whirlwind, y, BuyWhirlwind);
            y += step;
            _piercingShotRow = CreateShopUpgradeRow(content.transform, ShopUpgradeKind.PiercingShot, y, BuyPiercingShot);
            y += step;
            _frostTipRow = CreateShopUpgradeRow(content.transform, ShopUpgradeKind.FrostTip, y, BuyFrostTip);

            contentRect.sizeDelta = new Vector2(0f, Mathf.Abs(y) + 80f);

            CreateButton(panel.transform, "Build Loadout", new Vector2(-220, -400), () => OpenLoadout(), large: true);
            CreateButton(panel.transform, "Character Stats", new Vector2(220, -400), () => OpenStats(), large: true);
            CreateButton(panel.transform, "Close", new Vector2(0, -470), () =>
            {
                if (_shopInfoPanel != null) _shopInfoPanel.SetActive(false);
                panel.SetActive(false);
            }, large: true);

            BuildShopInfoOverlay(panel.transform);
            panel.SetActive(false);
            return panel;
        }

        void BuildShopInfoOverlay(Transform shopPanel)
        {
            // Built as a child of the already-scaled shop panel — do not apply HubMenuScale again.
            _shopInfoPanel = new GameObject("ShopInfoOverlay");
            _shopInfoPanel.transform.SetParent(shopPanel, false);
            var rootRect = _shopInfoPanel.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            var dim = _shopInfoPanel.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.62f);
            dim.raycastTarget = true;

            var card = new GameObject("ShopInfoCard");
            card.transform.SetParent(_shopInfoPanel.transform, false);
            var cardRect = card.AddComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.sizeDelta = new Vector2(780f, 580f);
            var cardImage = card.AddComponent<Image>();
            UiSprites.ApplyPanelSprite(cardImage, ArtLibrary.LevelUpUi, largeMenu: false);

            _shopInfoTitle = CreateText(card.transform, "Upgrade Info", 34, TextAnchor.MiddleCenter, new Vector2(0, 230), new Vector2(700, 48));
            // UpperCenter + padded size so body stays inside the panel frame (not top-left overflow).
            _shopInfoBody = CreateText(card.transform, "", 22, TextAnchor.UpperCenter, new Vector2(0, -72), new Vector2(680, 360));
            _shopInfoBody.alignment = TextAnchor.UpperLeft;
            _shopInfoBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            _shopInfoBody.verticalOverflow = VerticalWrapMode.Truncate;
            CreateButton(card.transform, "Close", new Vector2(0, -230), () => _shopInfoPanel.SetActive(false), large: true);
            _shopInfoPanel.SetActive(false);
        }

        GameObject BuildAchievementsPanel(Transform parent)
        {
            var panel = CreateDialogPanel(parent, "AchievementsPanel", Vector2.zero, HubMenuPanelSize, ArtLibrary.ChallengeBoardUi);
            CreateText(panel.transform, "Achievements", 44, TextAnchor.MiddleCenter, new Vector2(0, 380), new Vector2(700, 58));
            _achievementCountText = CreateText(panel.transform, "", 28, TextAnchor.MiddleCenter, new Vector2(0, 330), new Vector2(700, 40));

            var scrollGo = new GameObject("AchievementScroll");
            scrollGo.transform.SetParent(panel.transform, false);
            var scrollRectTransform = scrollGo.AddComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            scrollRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRectTransform.pivot = new Vector2(0.5f, 0.5f);
            scrollRectTransform.anchoredPosition = new Vector2(0f, -14f);
            scrollRectTransform.sizeDelta = new Vector2(1000f, 600f);

            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollGo.transform, false);
            var viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            viewport.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.02f);

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;

            scroll.viewport = viewportRect;
            scroll.content = contentRect;

            var y = 0f;
            const float rowHeight = 104f;
            foreach (var def in Achievements.All)
            {
                _achievementRows.Add(CreateAchievementRow(content.transform, def, y));
                y -= rowHeight;
            }

            contentRect.sizeDelta = new Vector2(980f, Mathf.Abs(y));

            CreateButton(panel.transform, "Close", new Vector2(0, -390), () => panel.SetActive(false), large: true);
            panel.SetActive(false);
            return panel;
        }

        AchievementRowRefs CreateAchievementRow(Transform parent, AchievementDef def, float y)
        {
            var go = new GameObject(def.Id + "Row");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(960f, 94f);

            var image = go.AddComponent<Image>();
            UiSprites.ApplyButtonSprite(image, new Vector2(960f, 94f));
            go.AddComponent<Button>();

            var title = CreateText(go.transform, def.Title, 30, TextAnchor.UpperLeft, new Vector2(20f, -12f), new Vector2(900f, 40f));
            title.alignment = TextAnchor.UpperLeft;
            var desc = CreateText(go.transform, def.Description, 24, TextAnchor.UpperLeft, new Vector2(20f, -50f), new Vector2(900f, 36f));
            desc.alignment = TextAnchor.UpperLeft;
            desc.color = new Color(0.88f, 0.9f, 0.95f);

            return new AchievementRowRefs
            {
                Id = def.Id,
                Background = image,
                TitleText = title,
                DescText = desc
            };
        }

        GameObject BuildLoadoutPanel(Transform parent)
        {
            var panel = CreateDialogPanel(parent, "LoadoutPanel", Vector2.zero, HubMenuPanelSize, ArtLibrary.ShopUi);
            CreateText(panel.transform, "Build Loadout", 38, TextAnchor.MiddleCenter, new Vector2(0, 360), new Vector2(620, 52));
            CreateText(panel.transform, "Your class & technique apply to you and RowZi (she copies this loadout).\nMovement & audio live in Settings.", 18, TextAnchor.MiddleCenter, new Vector2(0, 312), new Vector2(820, 48));

            // Class section (3 rows: Batter/Spearman, Bowman/Samurai, Magician)
            _loadoutClassPicker = BuildClassPicker(panel.transform, 255f, 210f, 135f);

            // Technique section under Magician row
            CreateText(panel.transform, "Attack Technique", 26, TextAnchor.MiddleCenter, new Vector2(0, -55), new Vector2(620, 36));
            _techniqueStatusText = CreateText(panel.transform, "", 18, TextAnchor.MiddleCenter, new Vector2(0, -95), new Vector2(780, 44));
            _techniqueStatusText.alignment = TextAnchor.UpperCenter;
            _techniqueStandardButton = CreateButton(panel.transform, "Standard", new Vector2(-160, -155), () => SelectAttackMode(AttackMode.Standard));
            _techniqueSpecialButton = CreateButton(panel.transform, "Special", new Vector2(160, -155), SelectSpecialAttackMode);

            // Weapon material quality (any unlocked tier)
            CreateText(panel.transform, "Weapon Quality", 26, TextAnchor.MiddleCenter, new Vector2(0, -215), new Vector2(620, 36));
            _weaponTierStatusText = CreateText(panel.transform, "", 18, TextAnchor.MiddleCenter, new Vector2(0, -255), new Vector2(820, 40));
            _weaponTierStatusText.alignment = TextAnchor.UpperCenter;
            _weaponTierPrevButton = CreateButton(panel.transform, "◀ Lower", new Vector2(-160, -305), () => CycleWeaponTier(-1));
            _weaponTierNextButton = CreateButton(panel.transform, "Higher ▶", new Vector2(160, -305), () => CycleWeaponTier(1));

            CreateButton(panel.transform, "Back to Shop", new Vector2(-160, -375), () =>
            {
                panel.SetActive(false);
                OpenShop();
            });
            CreateButton(panel.transform, "Close", new Vector2(160, -375), () => panel.SetActive(false));
            panel.SetActive(false);
            return panel;
        }

        GameObject BuildSettingsPanel(Transform parent)
        {
            var panel = CreateDialogPanel(parent, "SettingsPanel", Vector2.zero, HubMenuPanelSize, ArtLibrary.ShopUi);
            CreateText(panel.transform, "Settings", 40, TextAnchor.MiddleCenter, new Vector2(0, 360), new Vector2(560, 52));

            CreateText(panel.transform, "Movement Control", 28, TextAnchor.MiddleCenter, new Vector2(0, 290), new Vector2(620, 40));
            CreateText(panel.transform, "Only one control style is active at a time.", 20, TextAnchor.MiddleCenter, new Vector2(0, 250), new Vector2(700, 32));
            _movementJoystickButton = CreateButton(panel.transform, "Joystick", new Vector2(-160, 185), () => SelectMovementControl(MovementControlType.Joystick));
            _movementTapHoldButton = CreateButton(panel.transform, "Tap / Hold", new Vector2(160, 185), () => SelectMovementControl(MovementControlType.TapHold));
            CreateText(panel.transform, "Drag the on-screen joystick to place it. Position locks when you close Settings.", 18, TextAnchor.MiddleCenter, new Vector2(0, 125), new Vector2(900, 40));

            CreateText(panel.transform, "RollZy Skin", 26, TextAnchor.MiddleCenter, new Vector2(0, 70), new Vector2(400, 36));
            _rollZySkinStatusText = CreateText(panel.transform, "", 18, TextAnchor.MiddleCenter, new Vector2(0, 35), new Vector2(900, 32));
            _rollZyClassicSkinButton = CreateButton(panel.transform, "Classic", new Vector2(-160, -15), () => SelectRollZySkin(upgraded: false));
            _rollZyUpgradedSkinButton = CreateButton(panel.transform, "Upgraded", new Vector2(160, -15), () => SelectRollZySkin(upgraded: true));

            CreateText(panel.transform, "Music Volume", 26, TextAnchor.MiddleCenter, new Vector2(0, -90), new Vector2(400, 36));
            _bgmVolumeLabel = CreateText(panel.transform, "70%", 22, TextAnchor.MiddleCenter, new Vector2(0, -130), new Vector2(120, 32));
            CreateButton(panel.transform, "−", new Vector2(-200, -130), () => AdjustBgmVolume(-0.1f));
            CreateButton(panel.transform, "+", new Vector2(200, -130), () => AdjustBgmVolume(0.1f));

            CreateText(panel.transform, "SFX Volume", 26, TextAnchor.MiddleCenter, new Vector2(0, -200), new Vector2(400, 36));
            _sfxVolumeLabel = CreateText(panel.transform, "85%", 22, TextAnchor.MiddleCenter, new Vector2(0, -240), new Vector2(120, 32));
            CreateButton(panel.transform, "−", new Vector2(-200, -240), () => AdjustSfxVolume(-0.1f));
            CreateButton(panel.transform, "+", new Vector2(200, -240), () => AdjustSfxVolume(0.1f));

            CreateText(panel.transform, "Accessibility", 26, TextAnchor.MiddleCenter, new Vector2(0, -290), new Vector2(400, 36));
            _largeDamageNumbersButton = CreateButton(panel.transform, "Large Damage Numbers", new Vector2(0, -335), ToggleLargeDamageNumbers);
            _largeDamageNumbersLabel = _largeDamageNumbersButton != null
                ? _largeDamageNumbersButton.GetComponentInChildren<Text>()
                : null;

            CreateButton(panel.transform, "Close", new Vector2(0, -400), () => CloseSettings(), large: true);
            panel.SetActive(false);
            return panel;
        }

        void ToggleLargeDamageNumbers()
        {
            GameSave.LargeDamageNumbers = !GameSave.LargeDamageNumbers;
            RefreshLargeDamageNumbersButton();
        }

        void RefreshLargeDamageNumbersButton()
        {
            if (_largeDamageNumbersLabel == null) return;
            _largeDamageNumbersLabel.text = GameSave.LargeDamageNumbers
                ? "Large Damage Numbers: ON"
                : "Large Damage Numbers: OFF";
        }

        GameObject BuildStatsPanel(Transform parent)
        {
            var panel = CreateDialogPanel(parent, "StatsPanel", Vector2.zero, HubMenuPanelSize, ArtLibrary.ShopUi);
            CreateText(panel.transform, "Character Stats", 36, TextAnchor.MiddleCenter, new Vector2(0, 330), new Vector2(560, 48));
            _statsBodyText = CreateText(panel.transform, "", 22, TextAnchor.MiddleCenter, new Vector2(0, -10), new Vector2(760, 580));
            _statsBodyText.alignment = TextAnchor.UpperLeft;

            CreateButton(panel.transform, "Back to Shop", new Vector2(-150, -330), () =>
            {
                panel.SetActive(false);
                OpenShop();
            });
            CreateButton(panel.transform, "Close", new Vector2(150, -330), () => panel.SetActive(false));
            panel.SetActive(false);
            return panel;
        }

        GameObject BuildMapPanel(Transform parent) =>
            BuildSharedMapSelectPanel(
                parent,
                "MapPanel",
                "Survival Challenge",
                "Set class & technique at the Wizard shop first.\nUnlocked maps start fresh at round 1.");

        GameObject BuildCampfirePanel(Transform parent) =>
            BuildSharedMapSelectPanel(
                parent,
                "CampfirePanel",
                "Campfire Travel",
                "Choose an unlocked map. Each run starts at round 1.\nRecommended map is marked below.");

        GameObject BuildSharedMapSelectPanel(
            Transform parent,
            string panelName,
            string title,
            string subtitle)
        {
            var panel = CreateDialogPanel(parent, panelName, Vector2.zero, HubMenuPanelSize, ArtLibrary.ChallengeBoardUi);
            CreateText(panel.transform, title, 40, TextAnchor.MiddleCenter, new Vector2(0, 250), new Vector2(700, 56));
            CreateText(panel.transform, subtitle, 22, TextAnchor.MiddleCenter, new Vector2(0, 175), new Vector2(760, 72));
            CreateButton(panel.transform, "Outside Survival", new Vector2(0, 80), () => EnterSurvival(SurvivalMapKind.Outside), large: true);
            CreateButton(panel.transform, "Inside Survival", new Vector2(0, 15), () => EnterSurvival(SurvivalMapKind.Inside), large: true);
            CreateButton(panel.transform, "Dungeon Survival", new Vector2(0, -50), () => EnterSurvival(SurvivalMapKind.Dungeon), large: true);
            CreateButton(panel.transform, "Crypt Survival", new Vector2(0, -115), () => EnterSurvival(SurvivalMapKind.Crypt), large: true);
            CreateButton(panel.transform, "Unlimited Survival", new Vector2(0, -180), () => EnterSurvival(SurvivalMapKind.Unlimited), large: true);
            CreateButton(panel.transform, "Close", new Vector2(0, -255), () => panel.SetActive(false), large: true);
            panel.SetActive(false);
            return panel;
        }

        GameObject BuildEquipmentPanel(Transform parent)
        {
            var panel = CreateDialogPanel(parent, "EquipmentPanel", Vector2.zero, HubMenuPanelSize, ArtLibrary.ShopUi);
            CreateText(panel.transform, "Treasure Chest", 36, TextAnchor.MiddleCenter, new Vector2(0, 420), new Vector2(700, 48));
            CreateText(panel.transform, "One ring, necklace, cape, and helm. Find drops in survival to unlock them here.", 18, TextAnchor.MiddleCenter, new Vector2(0, 375), new Vector2(960, 36));
            _equipmentStatusText = CreateText(panel.transform, "", 18, TextAnchor.MiddleCenter, new Vector2(0, 335), new Vector2(960, 40));

            CreateText(panel.transform, "Rings", 22, TextAnchor.MiddleCenter, new Vector2(0, 290), new Vector2(400, 28));
            CreateText(panel.transform, "Necklaces", 22, TextAnchor.MiddleCenter, new Vector2(0, 145), new Vector2(400, 28));
            CreateText(panel.transform, "Capes", 22, TextAnchor.MiddleCenter, new Vector2(0, 0), new Vector2(400, 28));
            CreateText(panel.transform, "Helms", 22, TextAnchor.MiddleCenter, new Vector2(0, -145), new Vector2(400, 28));

            _equipmentButtons.Clear();
            // Unequip + 3 items per type (4 columns).
            var slotX = new[] { -360f, -120f, 120f, 360f };
            var ringIndex = 0;
            var neckIndex = 0;
            var capeIndex = 0;
            var helmIndex = 0;
            const float ringY = 240f;
            const float neckY = 95f;
            const float capeY = -50f;
            const float helmY = -195f;

            // Unequip slots first (refresh order depends on this).
            _equipmentButtons.Add(CreateButton(panel.transform, "No Ring", new Vector2(slotX[ringIndex++], ringY), () =>
            {
                GameSave.UnequipSlot(EquipmentSlot.Ring);
                RefreshEquipmentPanel();
            }));
            _equipmentButtons.Add(CreateButton(panel.transform, "No Necklace", new Vector2(slotX[neckIndex++], neckY), () =>
            {
                GameSave.UnequipSlot(EquipmentSlot.Necklace);
                RefreshEquipmentPanel();
            }));
            _equipmentButtons.Add(CreateButton(panel.transform, "No Cape", new Vector2(slotX[capeIndex++], capeY), () =>
            {
                GameSave.UnequipSlot(EquipmentSlot.Cape);
                RefreshEquipmentPanel();
                HeroEditorCombatBridge.RefreshLoadoutOnPlayer();
            }));
            _equipmentButtons.Add(CreateButton(panel.transform, "No Helm", new Vector2(slotX[helmIndex++], helmY), () =>
            {
                GameSave.UnequipSlot(EquipmentSlot.Helm);
                RefreshEquipmentPanel();
                HeroEditorCombatBridge.RefreshLoadoutOnPlayer();
            }));

            foreach (var def in EquipmentCatalog.All)
            {
                var id = def.Id;
                switch (def.Slot)
                {
                    case EquipmentSlot.Ring:
                    {
                        var x = ringIndex < slotX.Length ? slotX[ringIndex++] : 0f;
                        _equipmentButtons.Add(CreateButton(panel.transform, def.DisplayName, new Vector2(x, ringY), () => SelectEquipment(id)));
                        break;
                    }
                    case EquipmentSlot.Necklace:
                    {
                        var x = neckIndex < slotX.Length ? slotX[neckIndex++] : 0f;
                        _equipmentButtons.Add(CreateButton(panel.transform, def.DisplayName, new Vector2(x, neckY), () => SelectEquipment(id)));
                        break;
                    }
                    case EquipmentSlot.Cape:
                    {
                        var x = capeIndex < slotX.Length ? slotX[capeIndex++] : 0f;
                        _equipmentButtons.Add(CreateButton(panel.transform, def.DisplayName, new Vector2(x, capeY), () => SelectEquipment(id)));
                        break;
                    }
                    case EquipmentSlot.Helm:
                    {
                        var x = helmIndex < slotX.Length ? slotX[helmIndex++] : 0f;
                        _equipmentButtons.Add(CreateButton(panel.transform, def.DisplayName, new Vector2(x, helmY), () => SelectEquipment(id)));
                        break;
                    }
                }
            }

            CreateButton(panel.transform, "Close", new Vector2(0, -380), () => panel.SetActive(false), large: true);
            panel.SetActive(false);
            return panel;
        }

        void SelectEquipment(EquipmentId id)
        {
            if (!GameSave.OwnsEquipment(id)) return;
            GameSave.Equip(id);
            RefreshEquipmentPanel();
            HeroEditorCombatBridge.RefreshLoadoutOnPlayer();
            SparkleBurst.Play(_equipmentPanel != null ? _equipmentPanel.transform : transform, Vector2.zero, 12);
        }

        void RefreshEquipmentPanel()
        {
            if (_equipmentStatusText != null)
            {
                var ring = EquipmentCatalog.Get(GameSave.EquippedRing);
                var neck = EquipmentCatalog.Get(GameSave.EquippedNecklace);
                var cape = EquipmentCatalog.Get(GameSave.EquippedCape);
                var helm = EquipmentCatalog.Get(GameSave.EquippedHelm);
                var ringName = ring.Id != EquipmentId.None ? ring.DisplayName : "None";
                var neckName = neck.Id != EquipmentId.None ? neck.DisplayName : "None";
                var capeName = cape.Id != EquipmentId.None ? cape.DisplayName : "None";
                var helmName = helm.Id != EquipmentId.None ? helm.DisplayName : "None";
                _equipmentStatusText.text =
                    $"Equipped: {ringName}  ·  {neckName}  ·  {capeName}  ·  {helmName}";
            }

            // Button order: No Ring/Necklace/Cape/Helm, then catalog All in order.
            var buttonIndex = 0;
            RefreshEquipButton(GetEquipButton(buttonIndex++), EquipmentId.None, EquipmentSlot.Ring, "No Ring");
            RefreshEquipButton(GetEquipButton(buttonIndex++), EquipmentId.None, EquipmentSlot.Necklace, "No Necklace");
            RefreshEquipButton(GetEquipButton(buttonIndex++), EquipmentId.None, EquipmentSlot.Cape, "No Cape");
            RefreshEquipButton(GetEquipButton(buttonIndex++), EquipmentId.None, EquipmentSlot.Helm, "No Helm");

            foreach (var def in EquipmentCatalog.All)
                RefreshEquipButton(GetEquipButton(buttonIndex++), def.Id, def.Slot, def.DisplayName);
        }

        Button GetEquipButton(int index)
        {
            if (index < 0 || index >= _equipmentButtons.Count) return null;
            return _equipmentButtons[index];
        }

        static void RefreshEquipButton(Button button, EquipmentId id, EquipmentSlot slot, string baseLabel)
        {
            if (button == null) return;

            var owned = id == EquipmentId.None || GameSave.OwnsEquipment(id);
            var equipped = id == EquipmentId.None
                ? GetEquippedInSlot(slot) == EquipmentId.None
                : GetEquippedInSlot(slot) == id;

            button.interactable = owned;
            var image = button.GetComponent<Image>();
            if (image != null)
            {
                if (!owned)
                    image.color = new Color(0.25f, 0.25f, 0.28f, 0.7f);
                else if (equipped)
                    image.color = new Color(0.28f, 0.5f, 0.32f, 0.98f);
                else
                    image.color = new Color(0.2f, 0.35f, 0.55f, 0.95f);
            }

            var label = button.GetComponentInChildren<Text>();
            if (label == null) return;

            if (id == EquipmentId.None)
            {
                label.text = baseLabel;
                return;
            }

            var def = EquipmentCatalog.Get(id);
            if (!owned)
                label.text = "??? (Find in survival)";
            else
                label.text = equipped ? $"{def.DisplayName} ✓" : $"{def.DisplayName}\n{def.Description}";
            label.fontSize = owned ? 18 : 16;
        }

        static EquipmentId GetEquippedInSlot(EquipmentSlot slot) => slot switch
        {
            EquipmentSlot.Ring => GameSave.EquippedRing,
            EquipmentSlot.Necklace => GameSave.EquippedNecklace,
            EquipmentSlot.Cape => GameSave.EquippedCape,
            EquipmentSlot.Helm => GameSave.EquippedHelm,
            _ => EquipmentId.None
        };

        public void OpenEquipmentChest()
        {
            CloseAllHubPanels();
            RefreshEquipmentPanel();
            if (_equipmentPanel != null)
            {
                _equipmentPanel.SetActive(true);
                SparkleBurst.Play(_equipmentPanel.transform, new Vector2(0f, 200f), 10);
            }
        }

        void CloseAllHubPanels()
        {
            if (_shopPanel != null) _shopPanel.SetActive(false);
            if (_loadoutPanel != null) _loadoutPanel.SetActive(false);
            if (_statsPanel != null) _statsPanel.SetActive(false);
            if (_achievementsPanel != null) _achievementsPanel.SetActive(false);
            if (_mapPanel != null) _mapPanel.SetActive(false);
            if (_campfirePanel != null) _campfirePanel.SetActive(false);
            if (_equipmentPanel != null) _equipmentPanel.SetActive(false);
            if (_questPanel != null) _questPanel.SetActive(false);
            if (_settingsPanel != null && _settingsPanel.activeSelf)
                CloseSettings();
            else if (_settingsPanel != null)
                _settingsPanel.SetActive(false);
        }

        GameObject BuildQuestPanel(Transform parent)
        {
            // Dialogue layout: Stone border, portrait left, quest copy right.
            var panel = CreateDialogPanel(parent, "QuestPanel", Vector2.zero, new Vector2(980f, 560f), ArtLibrary.ShopUi);

            // Square stone frame so the 64×64 talk portrait fills without curved gaps.
            var frameGo = new GameObject("PortraitFrame");
            frameGo.transform.SetParent(panel.transform, false);
            var frameRect = frameGo.AddComponent<RectTransform>();
            frameRect.anchorMin = new Vector2(0.5f, 0.5f);
            frameRect.anchorMax = new Vector2(0.5f, 0.5f);
            frameRect.pivot = new Vector2(0.5f, 0.5f);
            frameRect.anchoredPosition = new Vector2(-300f, 20f);
            frameRect.sizeDelta = new Vector2(280f, 280f);
            var frameImage = frameGo.AddComponent<Image>();
            if (StoneUi.ButtonSquare != null)
            {
                frameImage.sprite = StoneUi.ButtonSquare;
                frameImage.type = Image.Type.Sliced;
                frameImage.pixelsPerUnitMultiplier = 1f;
            }
            else if (StoneUi.ListFrame != null)
            {
                frameImage.sprite = StoneUi.ListFrame;
                frameImage.type = Image.Type.Sliced;
            }
            else
            {
                frameImage.color = new Color(0.35f, 0.28f, 0.22f, 0.95f);
            }

            _questPortraitFrame = frameGo;

            var portraitGo = new GameObject("WizardPortrait");
            portraitGo.transform.SetParent(frameGo.transform, false);
            var portraitRect = portraitGo.AddComponent<RectTransform>();
            portraitRect.anchorMin = new Vector2(0.5f, 0.5f);
            portraitRect.anchorMax = new Vector2(0.5f, 0.5f);
            portraitRect.pivot = new Vector2(0.5f, 0.5f);
            portraitRect.anchoredPosition = Vector2.zero;
            // Each talk frame is 64×64 (2×3 grid inside 128×192).
            portraitRect.sizeDelta = new Vector2(220f, 220f);
            _questPortraitImage = portraitGo.AddComponent<Image>();
            _questPortraitImage.sprite = ArtLibrary.WizardPortrait;
            _questPortraitImage.preserveAspect = true;
            _questPortraitImage.raycastTarget = false;

            _questTitleText = CreateText(
                panel.transform,
                "Quest",
                34,
                TextAnchor.MiddleCenter,
                new Vector2(140f, 200f),
                new Vector2(520f, 48f));
            _questTitleText.alignment = TextAnchor.MiddleLeft;

            _questStatusText = CreateText(
                panel.transform,
                "",
                18,
                TextAnchor.MiddleCenter,
                new Vector2(140f, 155f),
                new Vector2(520f, 28f));
            _questStatusText.alignment = TextAnchor.MiddleLeft;
            _questStatusText.color = new Color(1f, 0.9f, 0.55f);

            _questBodyText = CreateText(
                panel.transform,
                "",
                22,
                TextAnchor.MiddleCenter,
                new Vector2(140f, -10f),
                new Vector2(520f, 280f));
            _questBodyText.alignment = TextAnchor.UpperLeft;
            _questBodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _questBodyText.verticalOverflow = VerticalWrapMode.Overflow;
            _questBodyText.color = new Color(0.94f, 0.96f, 0.98f);

            _questAcceptButton = CreateButton(panel.transform, "Accept", new Vector2(40f, -210f), AcceptFocusedQuest);
            _questTurnInButton = CreateButton(panel.transform, "Turn In", new Vector2(300f, -210f), TurnInFocusedQuest);
            _questAcceptLabel = _questAcceptButton.GetComponentInChildren<Text>();
            _questTurnInLabel = _questTurnInButton.GetComponentInChildren<Text>();
            CreateButton(panel.transform, "Close", new Vector2(170f, -280f), () => panel.SetActive(false));

            panel.SetActive(false);
            return panel;
        }

        /// <summary>Grand Wizard only — never shows the knight's quest dialogue.</summary>
        public void OpenQuestGiver()
        {
            if (!QuestCatalog.TryGetPrimaryOpenQuest(
                    QuestCatalog.GrandWizardQuestIds, out var def, out _))
            {
                def = QuestCatalog.GrandWizardsPeril;
            }

            OpenQuestDialogue(def.Id);
        }

        /// <summary>Knight1 only — always opens his own quest, never the wizard's.</summary>
        public void OpenKnightQuestGiver()
        {
            if (!QuestCatalog.TryGetPrimaryOpenQuest(
                    QuestCatalog.KnightQuestIds, out var def, out _))
            {
                def = QuestCatalog.KnightsBestFriend;
            }

            OpenQuestDialogue(def.Id);
        }

        /// <summary>Open the quest panel focused on a specific quest id.</summary>
        public void OpenQuestDialogue(QuestId questId)
        {
            RefreshGold();
            CloseAllHubPanels();
            _questPanelFocusId = questId;
            RefreshQuestPanel();
            if (_questPanel != null)
            {
                _questPanel.SetActive(true);
                var sparkleX = QuestCatalog.UsesQuestPortrait(questId) ? -300f : 0f;
                SparkleBurst.Play(_questPanel.transform, new Vector2(sparkleX, 40f), 8);
            }
        }

        void RefreshQuestPanel()
        {
            if (!QuestCatalog.TryGet(_questPanelFocusId, out var def))
                def = QuestCatalog.GrandWizardsPeril;

            var progress = QuestCatalog.GetProgress(def.Id);
            var showPortrait = QuestCatalog.UsesQuestPortrait(def.Id);
            if (_questPortraitFrame != null)
                _questPortraitFrame.SetActive(showPortrait);

            if (_questPortraitImage != null)
            {
                if (showPortrait)
                {
                    var frames = ArtLibrary.WizardPortraitFrames;
                    _questPortraitImage.sprite = frames != null && frames.Length > 0
                        ? frames[0]
                        : ArtLibrary.WizardPortrait;
                    _questPortraitFrameIndex = 0;
                    _questPortraitAnimTimer = 0f;
                    _questPortraitImage.rectTransform.localScale = Vector3.one;
                    _questPortraitImage.enabled = true;
                }
                else
                {
                    _questPortraitImage.enabled = false;
                }
            }

            // When no portrait, use full dialogue width for body/title.
            var textX = showPortrait ? 140f : -20f;
            var textW = showPortrait ? 520f : 860f;
            if (_questTitleText != null)
            {
                _questTitleText.text = def.Title;
                var titleRect = _questTitleText.rectTransform;
                titleRect.anchoredPosition = new Vector2(textX, 200f);
                titleRect.sizeDelta = new Vector2(textW, 48f);
            }

            if (_questStatusText != null)
            {
                var rewardSuffix = def.Id == QuestId.KnightsBestFriend
                    ? $"{def.GoldReward} Gold + Flame Enchant"
                    : $"{def.GoldReward} Gold";
                _questStatusText.text = progress switch
                {
                    QuestProgress.Available => $"Available  ·  Reward: {rewardSuffix}",
                    QuestProgress.Active => string.IsNullOrEmpty(def.ActiveStatusHint)
                        ? "In progress"
                        : def.ActiveStatusHint,
                    QuestProgress.ReadyToTurnIn => $"Ready to turn in  ·  Reward: {rewardSuffix}",
                    QuestProgress.Completed => "Completed",
                    _ => "Locked"
                };
                var statusRect = _questStatusText.rectTransform;
                statusRect.anchoredPosition = new Vector2(textX, 155f);
                statusRect.sizeDelta = new Vector2(textW, 28f);
            }

            if (_questBodyText != null)
            {
                _questBodyText.text = progress switch
                {
                    QuestProgress.Available => def.OfferText,
                    QuestProgress.Active => def.ActiveText,
                    QuestProgress.ReadyToTurnIn => def.TurnInText,
                    QuestProgress.Completed => def.CompletedText,
                    _ => "Come back when you are ready for a new task."
                };
                var bodyRect = _questBodyText.rectTransform;
                bodyRect.anchoredPosition = new Vector2(textX, -10f);
                bodyRect.sizeDelta = new Vector2(textW, 280f);
            }

            var canAccept = progress == QuestProgress.Available;
            var canTurnIn = progress == QuestProgress.ReadyToTurnIn;
            if (_questAcceptButton != null)
            {
                _questAcceptButton.gameObject.SetActive(canAccept);
                _questAcceptButton.interactable = canAccept;
            }

            if (_questTurnInButton != null)
            {
                _questTurnInButton.gameObject.SetActive(canTurnIn);
                _questTurnInButton.interactable = canTurnIn;
            }

            if (_questAcceptLabel != null) _questAcceptLabel.text = "Accept";
            if (_questTurnInLabel != null) _questTurnInLabel.text = "Turn In";
        }

        void AcceptFocusedQuest()
        {
            if (!QuestCatalog.TryAccept(_questPanelFocusId)) return;
            RefreshGold();
            RefreshQuestPanel();
        }

        void TurnInFocusedQuest()
        {
            var questId = _questPanelFocusId;
            if (!QuestCatalog.TryTurnIn(questId, out var gold)) return;
            RefreshGold();
            RefreshQuestPanel();
            if (gold > 0)
                SparkleBurst.Play(_questPanel != null ? _questPanel.transform : transform, new Vector2(120f, 0f), 12);
            if (questId == QuestId.KnightsBestFriend && GameSave.FlameEnchantUnlocked)
                SparkleBurst.Play(_questPanel != null ? _questPanel.transform : transform, new Vector2(-120f, 40f), 10);
        }

        void OpenSettings()
        {
            CloseAllHubPanels();
            GameSave.HasOpenedSettings = true;
            RefreshSettingsPanel();
            if (_settingsPanel != null)
                _settingsPanel.SetActive(true);
            // Allow dragging the stick while Settings is open; lock on close.
            MovementJoystick.EnsureExists();
            MovementJoystick.SetRepositionMode(GameSave.UsesJoystickMovement);
        }

        void CloseSettings()
        {
            MovementJoystick.SetRepositionMode(false);
            if (_settingsPanel != null)
                _settingsPanel.SetActive(false);
        }

        void RefreshSettingsPanel()
        {
            RefreshMovementControlPicker();
            RefreshRollZySkinPicker();
            RefreshVolumeLabels();
            RefreshLargeDamageNumbersButton();
        }

        void SelectRollZySkin(bool upgraded)
        {
            if (upgraded && !GameSave.RollZyUpgradedSkinUnlocked) return;
            GameSave.UseUpgradedRollZySkin = upgraded;
            RefreshRollZySkinPicker();
            // Apply immediately on camp so the player sees the change.
            CampHeroManager.Instance?.RefreshAppearance();
        }

        void RefreshRollZySkinPicker()
        {
            var unlocked = GameSave.RollZyUpgradedSkinUnlocked;
            var usingUpgraded = GameSave.UseUpgradedRollZySkin;

            if (_rollZySkinStatusText != null)
            {
                _rollZySkinStatusText.text = unlocked
                    ? (usingUpgraded ? "Using upgraded RollZy (Dungeon clear)." : "Using classic RollZy.")
                    : "Upgraded skin unlocks after clearing Dungeon Survival.";
            }

            RefreshSkinButton(_rollZyClassicSkinButton, selected: !usingUpgraded, interactable: true, "Classic");
            RefreshSkinButton(
                _rollZyUpgradedSkinButton,
                selected: usingUpgraded,
                interactable: unlocked,
                unlocked ? "Upgraded" : "Upgraded (Locked)");
        }

        static void RefreshSkinButton(Button button, bool selected, bool interactable, string label)
        {
            if (button == null) return;
            button.interactable = interactable;
            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = !interactable
                    ? new Color(0.45f, 0.45f, 0.5f, 0.85f)
                    : selected
                        ? new Color(0.35f, 0.72f, 0.42f, 1f)
                        : Color.white;
            }

            var text = button.GetComponentInChildren<Text>();
            if (text != null) text.text = label;
        }

        void AdjustBgmVolume(float delta)
        {
            GameSave.BgmVolume = Mathf.Clamp01(GameSave.BgmVolume + delta);
            AudioManager.Instance?.ApplySavedVolumes();
            RefreshVolumeLabels();
        }

        void AdjustSfxVolume(float delta)
        {
            GameSave.SfxVolume = Mathf.Clamp01(GameSave.SfxVolume + delta);
            AudioManager.Instance?.ApplySavedVolumes();
            RefreshVolumeLabels();
            // Audible click feedback at the new SFX level.
            AudioManager.Instance?.PlaySwingSfx();
        }

        void RefreshVolumeLabels()
        {
            if (_bgmVolumeLabel != null)
                _bgmVolumeLabel.text = $"{Mathf.RoundToInt(GameSave.BgmVolume * 100f)}%";
            if (_sfxVolumeLabel != null)
                _sfxVolumeLabel.text = $"{Mathf.RoundToInt(GameSave.SfxVolume * 100f)}%";
        }

        void PlayUpgradeSparkles()
        {
            var parent = _shopPanel != null && _shopPanel.activeSelf
                ? _shopPanel.transform
                : transform;
            SparkleBurst.Play(parent, Vector2.zero, 16);
        }

        ClassPickerRefs BuildClassPicker(Transform parent, float titleY, float statusY, float buttonY)
        {
            CreateText(parent, "Choose Class", 28, TextAnchor.MiddleCenter, new Vector2(0, titleY), new Vector2(560, 40));
            return new ClassPickerRefs
            {
                StatusText = CreateText(parent, "", 22, TextAnchor.MiddleCenter, new Vector2(0, statusY), new Vector2(720, 44)),
                BatterButton = CreateButton(parent, "Batter", new Vector2(-160, buttonY), () => SelectClass(PlayerClass.Batter)),
                SpearmanButton = CreateButton(parent, "Spearman", new Vector2(160, buttonY), () => SelectClass(PlayerClass.Spearman)),
                BowmanButton = CreateButton(parent, "Bowman", new Vector2(-160, buttonY - 76f), () => SelectClass(PlayerClass.Bowman)),
                SamuraiButton = CreateButton(parent, "Samurai", new Vector2(160, buttonY - 76f), () => SelectClass(PlayerClass.Samurai)),
                MagicianButton = CreateButton(parent, "Magician", new Vector2(0, buttonY - 152f), () => SelectClass(PlayerClass.Magician))
            };
        }

        void SelectMovementControl(MovementControlType controlType)
        {
            GameSave.SelectedMovementControl = controlType;
            MovementJoystick.ApplyControlMode();
            // Keep reposition mode only while Settings is open and joystick is selected.
            var settingsOpen = _settingsPanel != null && _settingsPanel.activeSelf;
            MovementJoystick.SetRepositionMode(settingsOpen && controlType == MovementControlType.Joystick);
            RefreshMovementControlPicker();
        }

        void RefreshMovementControlPicker()
        {
            var selected = GameSave.SelectedMovementControl;
            RefreshMovementControlButton(_movementJoystickButton, MovementControlType.Joystick, selected, "Joystick");
            RefreshMovementControlButton(_movementTapHoldButton, MovementControlType.TapHold, selected, "Tap / Hold");
        }

        static void RefreshMovementControlButton(Button button, MovementControlType mode, MovementControlType selected, string label)
        {
            if (button == null) return;
            button.interactable = true;
            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = selected == mode
                    ? new Color(0.28f, 0.5f, 0.32f, 0.98f)
                    : new Color(0.2f, 0.35f, 0.55f, 0.95f);
            }

            var buttonLabel = button.GetComponentInChildren<Text>();
            if (buttonLabel != null)
                buttonLabel.text = label;
        }

        void SelectClass(PlayerClass playerClass)
        {
            if (playerClass == PlayerClass.Spearman && !GameSave.SpearmanUnlocked) return;
            if (playerClass == PlayerClass.Bowman && !GameSave.BowmanUnlocked) return;
            if (playerClass == PlayerClass.Samurai && !GameSave.SamuraiUnlocked) return;
            if (playerClass == PlayerClass.Magician && !GameSave.MagicianUnlocked) return;
            GameSave.SelectedClass = playerClass;
            RefreshLoadoutPanel();
            CampHeroManager.Instance?.RefreshAppearance();
            HeroEditorCombatBridge.RefreshLoadoutOnPlayer();
        }

        void SelectAttackMode(AttackMode mode)
        {
            if (!AttackModeCatalog.IsAvailableForClass(GameSave.SelectedClass, mode)) return;
            if (!AttackModeCatalog.IsUnlocked(mode)) return;
            GameSave.SetSelectedAttackMode(GameSave.SelectedClass, mode);
            RefreshTechniquePicker();
        }

        void SelectSpecialAttackMode()
        {
            var special = AttackModeCatalog.GetSpecialModeForClass(GameSave.SelectedClass);
            if (special == AttackMode.Standard) return;
            SelectAttackMode(special);
        }

        void RefreshLoadoutPanel()
        {
            RefreshClassPicker(_loadoutClassPicker);
            RefreshTechniquePicker();
            RefreshWeaponTierPicker();
        }

        void CycleWeaponTier(int direction)
        {
            var playerClass = GameSave.SelectedClass;
            var tiers = WeaponCatalog.GetSelectableTiers(playerClass);
            if (tiers.Count == 0) return;

            var current = GameSave.GetEquippedWeaponTier(playerClass);
            var index = tiers.IndexOf(current);
            if (index < 0) index = tiers.Count - 1;
            index = (index + direction + tiers.Count) % tiers.Count;
            GameSave.SetEquippedWeaponTier(playerClass, tiers[index]);
            RefreshWeaponTierPicker();
        }

        void RefreshWeaponTierPicker()
        {
            var playerClass = GameSave.SelectedClass;
            var equipped = GameSave.GetEquippedWeaponTier(playerClass);
            var max = WeaponCatalog.GetUnlockedTier(playerClass);
            var tiers = WeaponCatalog.GetSelectableTiers(playerClass);

            if (_weaponTierStatusText != null)
            {
                _weaponTierStatusText.text = tiers.Count <= 1
                    ? $"Wooden only — unlock Iron (Dungeon R30) and higher materials for this weapon."
                    : $"Equipped: {WeaponCatalog.GetTierName(equipped)}  ·  {WeaponCatalog.GetPerkSummary(equipped)}\n"
                      + $"Highest unlocked: {WeaponCatalog.GetTierName(max)}";
            }

            var canCycle = tiers.Count > 1;
            if (_weaponTierPrevButton != null) _weaponTierPrevButton.interactable = canCycle;
            if (_weaponTierNextButton != null) _weaponTierNextButton.interactable = canCycle;
        }

        void RefreshTechniquePicker()
        {
            var playerClass = GameSave.SelectedClass;
            var selected = GameSave.GetSelectedAttackMode(playerClass);
            var special = AttackModeCatalog.GetSpecialModeForClass(playerClass);
            var specialUnlocked = special != AttackMode.Standard && AttackModeCatalog.IsUnlocked(special);

            if (_techniqueStatusText != null)
                _techniqueStatusText.text = AttackModeCatalog.GetDescription(playerClass, selected);

            RefreshAttackModeButton(_techniqueStandardButton, AttackMode.Standard, selected, true, "Standard");

            if (_techniqueSpecialButton == null) return;

            if (special == AttackMode.Standard)
            {
                _techniqueSpecialButton.gameObject.SetActive(false);
                return;
            }

            _techniqueSpecialButton.gameObject.SetActive(true);
            RefreshAttackModeButton(
                _techniqueSpecialButton,
                special,
                selected,
                specialUnlocked,
                specialUnlocked
                    ? AttackModeCatalog.GetLabel(special, playerClass)
                    : AttackModeCatalog.GetLockedHint(special));
        }

        static void RefreshAttackModeButton(Button button, AttackMode mode, AttackMode selected, bool unlocked, string label)
        {
            if (button == null) return;

            button.interactable = unlocked;
            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = !unlocked
                    ? new Color(0.25f, 0.25f, 0.28f, 0.7f)
                    : selected == mode
                        ? new Color(0.28f, 0.5f, 0.32f, 0.98f)
                        : new Color(0.2f, 0.35f, 0.55f, 0.95f);
            }

            var buttonLabel = button.GetComponentInChildren<Text>();
            if (buttonLabel != null)
                buttonLabel.text = label;
        }

        static string GetClassStatusText(PlayerClass selected)
        {
            return selected switch
            {
                PlayerClass.Spearman => "Spearman — 180° arc thrust, 360° whirlwind",
                PlayerClass.Bowman => "Bowman — strong ranged arrows, piercing upgrade",
                PlayerClass.Samurai => "Samurai — double 180° katana swipe, triple with Whirlwind",
                PlayerClass.Magician => "Magician — splash spells",
                _ => "Batter — melee bat, 360° whirlwind"
            };
        }

        static string GetClassDisplayName(PlayerClass playerClass)
        {
            return playerClass switch
            {
                PlayerClass.Spearman => "Spearman",
                PlayerClass.Bowman => "Bowman",
                PlayerClass.Samurai => "Samurai",
                PlayerClass.Magician => "Magician",
                _ => "Batter"
            };
        }

        void RefreshClassPicker(ClassPickerRefs picker)
        {
            var selected = GameSave.SelectedClass;
            if (picker.StatusText != null)
                picker.StatusText.text = GetClassStatusText(selected);

            RefreshClassButton(picker.BatterButton, PlayerClass.Batter, true, "Batter");
            RefreshClassButton(picker.SpearmanButton, PlayerClass.Spearman, GameSave.SpearmanUnlocked, "Spearman — Outside R20 boss");
            RefreshClassButton(picker.BowmanButton, PlayerClass.Bowman, GameSave.BowmanUnlocked, "Bowman — Inside R30 clear");
            RefreshClassButton(picker.SamuraiButton, PlayerClass.Samurai, GameSave.SamuraiUnlocked, "Samurai — Dungeon R40 boss");
            RefreshClassButton(picker.MagicianButton, PlayerClass.Magician, GameSave.MagicianUnlocked, "Magician — Unlimited R80");
        }

        static void RefreshClassButton(Button button, PlayerClass playerClass, bool unlocked, string lockedLabel)
        {
            if (button == null) return;

            var selected = GameSave.SelectedClass;
            button.interactable = unlocked;
            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = !unlocked
                    ? new Color(0.25f, 0.25f, 0.28f, 0.7f)
                    : selected == playerClass
                        ? new Color(0.28f, 0.5f, 0.32f, 0.98f)
                        : new Color(0.2f, 0.35f, 0.55f, 0.95f);
            }

            var label = button.GetComponentInChildren<Text>();
            if (label != null)
                label.text = unlocked ? GetClassDisplayName(playerClass) : lockedLabel;
        }

        void EnterSurvival(SurvivalMapKind mapKind)
        {
            if (mapKind == SurvivalMapKind.Inside && !GameSave.InsideMapUnlocked) return;
            if (mapKind == SurvivalMapKind.Dungeon && !GameSave.DungeonMapUnlocked) return;
            if (mapKind == SurvivalMapKind.Crypt && !GameSave.CryptMapUnlocked) return;
            if (mapKind == SurvivalMapKind.Unlimited && !GameSave.UnlimitedMapUnlocked) return;

            GameSessionContext.ClearPendingNextMap();
            GameSessionContext.SurvivalMap = mapKind;
            GameSessionContext.SelectedHero = PlayableHero.RollZy;
            GameSessionContext.SelectedClass = GameSave.SelectedClass;
            GameSessionContext.FreshSurvivalRun = true;
            GameSessionContext.CarryRound = 0;
            // Every map starts a fresh run at round 1 (StartingRound 0 → ++).
            GameSessionContext.StartingRound = 0;
            GameSessionContext.RunSnapshot = default;
            _shopPanel.SetActive(false);
            _loadoutPanel.SetActive(false);
            _statsPanel.SetActive(false);
            _achievementsPanel.SetActive(false);
            CloseAllHubPanels();
            GameFactory.LoadScene(GameScenes.SurvivalArena);
        }

        UpgradeRowRefs CreateUpgradeRow(Transform parent, string label, int cost, float y, Action onBuy)
        {
            return new UpgradeRowRefs
            {
                Label = CreateText(parent, $"{label} — {cost}g", 28, TextAnchor.MiddleLeft, new Vector2(-254, y), new Vector2(400, 48)),
                BuyButton = CreateButton(parent, "Buy", new Vector2(266, y), onBuy, large: true)
            };
        }

        UpgradeRowRefs CreateShopUpgradeRow(Transform parent, ShopUpgradeKind kind, float y, Action onBuy)
        {
            var labelText = CreateText(parent, GetShopRowTitle(kind), 28, TextAnchor.MiddleLeft, new Vector2(-40f, y - 28f), new Vector2(420f, 52f));
            var labelRect = labelText.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 1f);
            labelRect.anchorMax = new Vector2(0.5f, 1f);
            labelRect.pivot = new Vector2(0.5f, 1f);
            labelRect.anchoredPosition = new Vector2(-200f, y);
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.horizontalOverflow = HorizontalWrapMode.Overflow;
            labelText.verticalOverflow = VerticalWrapMode.Truncate;

            var infoButton = CreateButton(parent, "Info", new Vector2(160f, y - 28f), () => OpenShopInfo(kind));
            var infoRect = infoButton.GetComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0.5f, 1f);
            infoRect.anchorMax = new Vector2(0.5f, 1f);
            infoRect.pivot = new Vector2(0.5f, 1f);
            infoRect.anchoredPosition = new Vector2(140f, y);
            infoRect.sizeDelta = new Vector2(110f, 52f);
            var infoImage = infoButton.GetComponent<Image>();
            if (infoImage != null)
                UiSprites.ApplyButtonSprite(infoImage, infoRect.sizeDelta, StoneButtonStyle.Primary);
            var infoLabel = infoButton.GetComponentInChildren<Text>();
            if (infoLabel != null)
                infoLabel.fontSize = 22;

            var buyButton = CreateButton(parent, "Buy", new Vector2(360f, y - 28f), onBuy, large: true);
            var buyRect = buyButton.GetComponent<RectTransform>();
            buyRect.anchorMin = new Vector2(0.5f, 1f);
            buyRect.anchorMax = new Vector2(0.5f, 1f);
            buyRect.pivot = new Vector2(0.5f, 1f);
            buyRect.anchoredPosition = new Vector2(360f, y);
            buyRect.sizeDelta = new Vector2(240f, 56f);
            var buyImage = buyButton.GetComponent<Image>();
            if (buyImage != null)
                UiSprites.ApplyButtonSprite(buyImage, buyRect.sizeDelta, StoneButtonStyle.Green);

            // Price text sits slightly left so a gold coin icon can sit on the right.
            var buyLabel = buyButton.GetComponentInChildren<Text>();
            if (buyLabel != null)
            {
                var buyLabelRect = buyLabel.GetComponent<RectTransform>();
                buyLabelRect.anchoredPosition = new Vector2(-10f, 0f);
                buyLabelRect.sizeDelta = new Vector2(180f, 46f);
                buyLabel.fontSize = 22;
            }

            var coinIcon = CreateBuyCoinIcon(buyButton.transform);

            return new UpgradeRowRefs
            {
                Label = labelText,
                BuyButton = buyButton,
                InfoButton = infoButton,
                CoinIcon = coinIcon,
                Kind = kind
            };
        }

        void OpenShopInfo(ShopUpgradeKind kind)
        {
            if (_shopInfoPanel == null) return;
            if (_shopInfoTitle != null)
                _shopInfoTitle.text = GetShopInfoTitle(kind);
            if (_shopInfoBody != null)
                _shopInfoBody.text = GetShopInfoBody(kind);
            _shopInfoPanel.SetActive(true);
            _shopInfoPanel.transform.SetAsLastSibling();
        }

        static string ToRoman(int value)
        {
            if (value <= 0) return "I";
            value = Mathf.Clamp(value, 1, 40);
            // Enough for permanent upgrade ranks.
            string[] romans =
            {
                "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X",
                "XI", "XII", "XIII", "XIV", "XV", "XVI", "XVII", "XVIII", "XIX", "XX",
                "XXI", "XXII", "XXIII", "XXIV", "XXV", "XXVI", "XXVII", "XXVIII", "XXIX", "XXX",
                "XXXI", "XXXII", "XXXIII", "XXXIV", "XXXV", "XXXVI", "XXXVII", "XXXVIII", "XXXIX", "XL"
            };
            return romans[value - 1];
        }

        static string RankedTitle(string baseName, int ownedLevel, bool maxed)
        {
            if (maxed)
                return $"{baseName} MAX";
            // Next purchase is rank ownedLevel+1 (I when none owned).
            return $"{baseName} {ToRoman(ownedLevel + 1)}";
        }

        static string GetShopRowTitle(ShopUpgradeKind kind) => kind switch
        {
            ShopUpgradeKind.MaxHp => RankedTitle("Max HP", GameSave.HpUpgradeLevel, GameSave.IsHpUpgradeMaxed),
            ShopUpgradeKind.Damage => RankedTitle("Damage", GameSave.DamageUpgradeLevel, GameSave.IsDamageUpgradeMaxed),
            ShopUpgradeKind.Speed => RankedTitle("Move Speed", GameSave.SpeedUpgradeLevel, GameSave.IsSpeedUpgradeMaxed),
            ShopUpgradeKind.Range => RankedTitle("Attack Range", GameSave.RangeUpgradeLevel, GameSave.IsRangeUpgradeMaxed),
            ShopUpgradeKind.GoldMagnet => GameSave.GoldMagnetUnlocked ? "Gold Magnet" : "Gold Magnet",
            ShopUpgradeKind.ThickHide => GameSave.ThickHideLevel >= 3
                ? "Thick Hide MAX"
                : $"Thick Hide {ToRoman(GameSave.ThickHideLevel + 1)}",
            ShopUpgradeKind.SecondWind => GameSave.SecondWindLevel >= 2
                ? "Second Wind MAX"
                : $"Second Wind {ToRoman(GameSave.SecondWindLevel + 1)}",
            ShopUpgradeKind.CampfireBlessing => "Campfire Blessing",
            ShopUpgradeKind.Whirlwind => "Whirlwind",
            ShopUpgradeKind.PiercingShot => "Piercing Shot",
            ShopUpgradeKind.FrostTip => "Frost Tip",
            _ => kind.ToString()
        };

        static string GetShopInfoTitle(ShopUpgradeKind kind) => kind switch
        {
            ShopUpgradeKind.MaxHp => "Max HP",
            ShopUpgradeKind.Damage => "Damage",
            ShopUpgradeKind.Speed => "Move Speed",
            ShopUpgradeKind.Range => "Attack Range",
            ShopUpgradeKind.GoldMagnet => "Gold Magnet",
            ShopUpgradeKind.ThickHide => "Thick Hide",
            ShopUpgradeKind.SecondWind => "Second Wind",
            ShopUpgradeKind.CampfireBlessing => "Campfire Blessing",
            ShopUpgradeKind.Whirlwind => "Whirlwind",
            ShopUpgradeKind.PiercingShot => "Piercing Shot",
            ShopUpgradeKind.FrostTip => "Frost Tip",
            _ => kind.ToString()
        };

        static string GetShopInfoBody(ShopUpgradeKind kind)
        {
            switch (kind)
            {
                case ShopUpgradeKind.MaxHp:
                    return
                        "Each rank: +15 Max HP (permanent).\n\n" +
                        $"Current Max HP: {GameSave.MaxHp}\n" +
                        $"Cap: {StatCaps.PermanentMaxHp}\n" +
                        $"Ranks owned: {GameSave.HpUpgradeLevel}\n" +
                        (GameSave.IsHpUpgradeMaxed
                            ? "Status: MAXED"
                            : $"Next rank: {ToRoman(GameSave.HpUpgradeLevel + 1)}  ·  Cost: {ShopCosts.NextHpCost}g");

                case ShopUpgradeKind.Damage:
                    return
                        "Each rank: +8% permanent damage.\n\n" +
                        $"Current multiplier: x{GameSave.DamageMultiplier:0.##}\n" +
                        $"Cap: x{StatCaps.PermanentMaxDamageMultiplier:0.#}\n" +
                        $"Ranks owned: {GameSave.DamageUpgradeLevel}\n" +
                        (GameSave.IsDamageUpgradeMaxed
                            ? "Status: MAXED"
                            : $"Next rank: {ToRoman(GameSave.DamageUpgradeLevel + 1)}  ·  Cost: {ShopCosts.NextDamageCost}g");

                case ShopUpgradeKind.Speed:
                    return
                        "Each rank: +6% permanent move speed.\n\n" +
                        $"Current multiplier: x{GameSave.SpeedMultiplier:0.##}\n" +
                        $"Cap: x{StatCaps.PermanentMaxSpeedMultiplier:0.#}\n" +
                        $"Ranks owned: {GameSave.SpeedUpgradeLevel}\n" +
                        (GameSave.IsSpeedUpgradeMaxed
                            ? "Status: MAXED"
                            : $"Next rank: {ToRoman(GameSave.SpeedUpgradeLevel + 1)}  ·  Cost: {ShopCosts.NextSpeedCost}g");

                case ShopUpgradeKind.Range:
                    return
                        "Each rank: +5% permanent attack range.\n\n" +
                        $"Current multiplier: x{GameSave.AttackRangeMultiplier:0.##}\n" +
                        $"Cap: x{StatCaps.PermanentMaxAttackRangeMultiplier:0.#}\n" +
                        $"Ranks owned: {GameSave.RangeUpgradeLevel}\n" +
                        (GameSave.IsRangeUpgradeMaxed
                            ? "Status: MAXED"
                            : $"Next rank: {ToRoman(GameSave.RangeUpgradeLevel + 1)}  ·  Cost: {ShopCosts.NextRangeCost}g");

                case ShopUpgradeKind.GoldMagnet:
                    return
                        "One-time upgrade.\n\n" +
                        "+25% gold from kills\n" +
                        "+25% loot pickup range\n\n" +
                        (GameSave.GoldMagnetUnlocked
                            ? "Status: Owned"
                            : $"Cost: {ShopCosts.GoldMagnet}g");

                case ShopUpgradeKind.ThickHide:
                {
                    var level = GameSave.ThickHideLevel;
                    var dr = level <= 0 ? 0 : level * 15;
                    return
                        "Reduces all damage taken (permanent tiers).\n\n" +
                        "I: −15% damage taken\n" +
                        "II: −30% damage taken (requires Inside clear)\n" +
                        "III: −45% damage taken (requires Dungeon clear)\n\n" +
                        $"Current: T{level} ({dr}% DR)\n" +
                        (level >= 3
                            ? "Status: MAXED"
                            : level == 1 && !GameSave.InsideSurvivalCleared
                                ? "Next: Locked — clear Inside Survival"
                                : level == 2 && !GameSave.DungeonSurvivalCleared
                                    ? "Next: Locked — clear Dungeon Survival"
                                    : $"Next rank: {ToRoman(level + 1)}  ·  Cost: {ShopCosts.NextThickHideCost}g");
                }

                case ShopUpgradeKind.SecondWind:
                {
                    var level = GameSave.SecondWindLevel;
                    return
                        "Auto-heal when you drop to 20% HP or below.\n\n" +
                        "I: heal 30% Max HP once per run\n" +
                        "II: two uses per run (requires Inside clear)\n\n" +
                        $"Current charges/run: {GameSave.SecondWindMaxCharges}\n" +
                        (level >= 2
                            ? "Status: MAXED"
                            : level == 1 && !GameSave.InsideSurvivalCleared
                                ? "Next: Locked — clear Inside Survival"
                                : $"Next rank: {ToRoman(level + 1)}  ·  Cost: {ShopCosts.NextSecondWindCost}g");
                }

                case ShopUpgradeKind.CampfireBlessing:
                    return
                        "One-time upgrade.\n\n" +
                        "Start each survival run with a free level-up talent pick.\n\n" +
                        (GameSave.CampfireBlessingUnlocked
                            ? "Status: Owned"
                            : $"Cost: {ShopCosts.CampfireBlessing}g");

                case ShopUpgradeKind.Whirlwind:
                    return
                        "One-time upgrade for Batter / Spearman / Samurai.\n\n" +
                        "Unlocks a powerful alternate attack technique.\n" +
                        "Enable it in Build Loadout after purchase.\n\n" +
                        (GameSave.WhirlwindUnlocked
                            ? "Status: Owned"
                            : $"Cost: {ShopCosts.Whirlwind}g");

                case ShopUpgradeKind.PiercingShot:
                    return
                        "One-time Bowman upgrade.\n\n" +
                        "Arrows pierce through up to 5 enemies.\n" +
                        "Requires Bowman unlocked.\n\n" +
                        (GameSave.PiercingShotUnlocked
                            ? "Status: Owned"
                            : !GameSave.BowmanUnlocked
                                ? "Status: Locked — unlock Bowman first"
                                : $"Cost: {ShopCosts.PiercingShot}g");

                case ShopUpgradeKind.FrostTip:
                    return
                        "One-time upgrade for Batter / Spearman / Bowman / Samurai.\n\n" +
                        "Hits chill enemies for 1s (−60% move speed).\n\n" +
                        (GameSave.FrostTipUnlocked
                            ? "Status: Owned"
                            : $"Cost: {ShopCosts.FrostTip}g");

                default:
                    return string.Empty;
            }
        }

        static Image CreateBuyCoinIcon(Transform buttonTransform)
        {
            var go = new GameObject("GoldCoinIcon");
            go.transform.SetParent(buttonTransform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-14f, 0f);
            rect.sizeDelta = new Vector2(28f, 28f);
            var image = go.AddComponent<Image>();
            image.sprite = StoneUi.Available && StoneUi.ResourceIconCoin != null
                ? StoneUi.ResourceIconCoin
                : StoneUi.Available && StoneUi.IconGold != null
                    ? StoneUi.IconGold
                    : ArtLibrary.GoldCoin;
            image.raycastTarget = false;
            return image;
        }

        void BuyHp()
        {
            if (GameSave.IsHpUpgradeMaxed) return;
            if (!GameSave.TrySpendGold(ShopCosts.NextHpCost)) return;
            GameSave.HpUpgradeLevel++;
            OnShopUpgradePurchased();
        }

        void BuyDamage()
        {
            if (GameSave.IsDamageUpgradeMaxed) return;
            if (!GameSave.TrySpendGold(ShopCosts.NextDamageCost)) return;
            GameSave.DamageUpgradeLevel++;
            OnShopUpgradePurchased();
        }

        void BuySpeed()
        {
            if (GameSave.IsSpeedUpgradeMaxed) return;
            if (!GameSave.TrySpendGold(ShopCosts.NextSpeedCost)) return;
            GameSave.SpeedUpgradeLevel++;
            OnShopUpgradePurchased();
        }

        void BuyRange()
        {
            if (GameSave.IsRangeUpgradeMaxed) return;
            if (!GameSave.TrySpendGold(ShopCosts.NextRangeCost)) return;
            GameSave.RangeUpgradeLevel++;
            OnShopUpgradePurchased();
        }

        void BuyGoldMagnet()
        {
            if (GameSave.GoldMagnetUnlocked) return;
            if (!GameSave.TrySpendGold(ShopCosts.GoldMagnet)) return;
            GameSave.GoldMagnetUnlocked = true;
            OnShopUpgradePurchased();
        }

        void BuyThickHide()
        {
            var level = GameSave.ThickHideLevel;
            if (level >= 3) return;
            if (level >= 1 && !GameSave.InsideSurvivalCleared) return;
            if (level >= 2 && !GameSave.DungeonSurvivalCleared) return;
            if (!GameSave.TrySpendGold(ShopCosts.NextThickHideCost)) return;
            GameSave.ThickHideLevel = level + 1;
            OnShopUpgradePurchased();
        }

        void BuySecondWind()
        {
            var level = GameSave.SecondWindLevel;
            if (level >= 2) return;
            if (level >= 1 && !GameSave.InsideSurvivalCleared) return;
            if (!GameSave.TrySpendGold(ShopCosts.NextSecondWindCost)) return;
            GameSave.SecondWindLevel = level + 1;
            OnShopUpgradePurchased();
        }

        void BuyCampfireBlessing()
        {
            if (GameSave.CampfireBlessingUnlocked) return;
            if (!GameSave.TrySpendGold(ShopCosts.CampfireBlessing)) return;
            GameSave.CampfireBlessingUnlocked = true;
            OnShopUpgradePurchased();
        }

        void BuyFrostTip()
        {
            if (GameSave.FrostTipUnlocked) return;
            // Batter is always available; melee/ranged tip classes also benefit.
            if (!GameSave.TrySpendGold(ShopCosts.FrostTip)) return;
            GameSave.FrostTipUnlocked = true;
            OnShopUpgradePurchased();
            if (_loadoutPanel != null && _loadoutPanel.activeSelf) RefreshLoadoutPanel();
        }

        void OnShopUpgradePurchased()
        {
            RefreshGold();
            RefreshShopRows();
            PlayUpgradeSparkles();
        }

        void RefreshShopRows()
        {
            SetUpgradeRow(_hpRow, GetShopRowTitle(ShopUpgradeKind.MaxHp), ShopCosts.NextHpCost, GameSave.IsHpUpgradeMaxed);
            SetUpgradeRow(_damageRow, GetShopRowTitle(ShopUpgradeKind.Damage), ShopCosts.NextDamageCost, GameSave.IsDamageUpgradeMaxed);
            SetUpgradeRow(_speedRow, GetShopRowTitle(ShopUpgradeKind.Speed), ShopCosts.NextSpeedCost, GameSave.IsSpeedUpgradeMaxed);
            SetUpgradeRow(_rangeRow, GetShopRowTitle(ShopUpgradeKind.Range), ShopCosts.NextRangeCost, GameSave.IsRangeUpgradeMaxed);

            if (GameSave.GoldMagnetUnlocked)
                SetOwnedRow(_goldMagnetRow, "Gold Magnet");
            else
                SetUpgradeRow(_goldMagnetRow, "Gold Magnet", ShopCosts.GoldMagnet, false);

            RefreshThickHideRow();
            RefreshSecondWindRow();

            if (GameSave.CampfireBlessingUnlocked)
                SetOwnedRow(_campfireBlessingRow, "Campfire Blessing");
            else
                SetUpgradeRow(_campfireBlessingRow, "Campfire Blessing", ShopCosts.CampfireBlessing, false);

            if (GameSave.WhirlwindUnlocked)
                SetOwnedRow(_whirlwindRow, "Whirlwind");
            else
                SetUpgradeRow(_whirlwindRow, "Whirlwind", ShopCosts.Whirlwind, false);

            if (GameSave.PiercingShotUnlocked)
                SetOwnedRow(_piercingShotRow, "Piercing Shot");
            else if (!GameSave.BowmanUnlocked)
                SetLockedRow(_piercingShotRow, "Piercing Shot");
            else
                SetUpgradeRow(_piercingShotRow, "Piercing Shot", ShopCosts.PiercingShot, false);

            if (GameSave.FrostTipUnlocked)
                SetOwnedRow(_frostTipRow, "Frost Tip");
            else
                SetUpgradeRow(_frostTipRow, "Frost Tip", ShopCosts.FrostTip, false);
        }

        void RefreshThickHideRow()
        {
            var level = GameSave.ThickHideLevel;
            if (level >= 3)
            {
                SetOwnedRow(_thickHideRow, "Thick Hide MAX");
                return;
            }

            var title = $"Thick Hide {ToRoman(level + 1)}";
            if (level == 1 && !GameSave.InsideSurvivalCleared)
            {
                SetLockedRow(_thickHideRow, title);
                return;
            }

            if (level == 2 && !GameSave.DungeonSurvivalCleared)
            {
                SetLockedRow(_thickHideRow, title);
                return;
            }

            SetUpgradeRow(_thickHideRow, title, ShopCosts.NextThickHideCost, false);
        }

        void RefreshSecondWindRow()
        {
            var level = GameSave.SecondWindLevel;
            if (level >= 2)
            {
                SetOwnedRow(_secondWindRow, "Second Wind MAX");
                return;
            }

            var title = $"Second Wind {ToRoman(level + 1)}";
            if (level == 1 && !GameSave.InsideSurvivalCleared)
            {
                SetLockedRow(_secondWindRow, title);
                return;
            }

            SetUpgradeRow(_secondWindRow, title, ShopCosts.NextSecondWindCost, false);
        }

        static void SetLockedRow(UpgradeRowRefs row, string label)
        {
            if (row.Label != null)
                row.Label.text = label;

            if (row.BuyButton != null)
            {
                row.BuyButton.interactable = false;
                var image = row.BuyButton.GetComponent<Image>();
                if (image != null)
                {
                    UiSprites.ApplyButtonSprite(image, new Vector2(240f, 56f));
                    image.color = new Color(0.32f, 0.34f, 0.38f, 0.75f);
                }

                var buyLabel = row.BuyButton.GetComponentInChildren<Text>();
                if (buyLabel != null)
                {
                    buyLabel.text = "Locked";
                    buyLabel.color = new Color(0.72f, 0.74f, 0.78f);
                }
            }

            if (row.CoinIcon != null)
                row.CoinIcon.enabled = false;
        }

        static void SetUpgradeRow(UpgradeRowRefs row, string label, int cost, bool maxed)
        {
            if (row.Label != null)
                row.Label.text = label;

            if (row.BuyButton != null)
            {
                row.BuyButton.interactable = !maxed;
                var image = row.BuyButton.GetComponent<Image>();
                if (image != null)
                {
                    UiSprites.ApplyButtonSprite(image, new Vector2(240f, 56f),
                        maxed ? StoneButtonStyle.Primary : StoneButtonStyle.Green);
                    image.color = Color.white;
                }

                var buyLabel = row.BuyButton.GetComponentInChildren<Text>();
                if (buyLabel != null)
                {
                    buyLabel.text = maxed ? "MAX" : $"Buy {GoldFormat.Abbreviate(cost)}";
                    buyLabel.color = Color.white;
                }
            }

            if (row.CoinIcon != null)
                row.CoinIcon.enabled = !maxed;
        }

        static void SetOwnedRow(UpgradeRowRefs row, string label)
        {
            if (row.Label != null)
                row.Label.text = label;

            if (row.BuyButton != null)
            {
                row.BuyButton.interactable = false;
                var image = row.BuyButton.GetComponent<Image>();
                if (image != null)
                {
                    UiSprites.ApplyButtonSprite(image, new Vector2(240f, 56f));
                    image.color = new Color(0.42f, 0.44f, 0.48f, 0.88f);
                }

                var buyLabel = row.BuyButton.GetComponentInChildren<Text>();
                if (buyLabel != null)
                {
                    buyLabel.text = "Owned";
                    buyLabel.color = new Color(0.82f, 0.84f, 0.88f);
                }
            }

            if (row.CoinIcon != null)
                row.CoinIcon.enabled = false;
        }

        void BuyWhirlwind()
        {
            if (GameSave.WhirlwindUnlocked) return;
            if (!GameSave.TrySpendGold(ShopCosts.Whirlwind)) return;
            GameSave.WhirlwindUnlocked = true;
            OnShopUpgradePurchased();
            if (_loadoutPanel != null && _loadoutPanel.activeSelf) RefreshLoadoutPanel();
            ShowShopPurchaseHint("Hint: Enable Whirlwind In \"Build Loadout\"");
        }

        void ShowShopPurchaseHint(string message)
        {
            if (_shopPanel == null || string.IsNullOrEmpty(message)) return;

            var existing = _shopPanel.transform.Find("PurchaseHint");
            if (existing != null)
                Destroy(existing.gameObject);

            var hint = CreateText(
                _shopPanel.transform,
                message,
                26,
                TextAnchor.MiddleCenter,
                new Vector2(0f, 360f),
                new Vector2(900f, 48f));
            hint.gameObject.name = "PurchaseHint";
            hint.color = new Color(1f, 0.92f, 0.45f, 1f);
            Destroy(hint.gameObject, 6f);
        }

        void BuyPiercingShot()
        {
            if (GameSave.PiercingShotUnlocked || !GameSave.BowmanUnlocked) return;
            if (!GameSave.TrySpendGold(ShopCosts.PiercingShot)) return;
            GameSave.PiercingShotUnlocked = true;
            OnShopUpgradePurchased();
            if (_loadoutPanel != null && _loadoutPanel.activeSelf) RefreshLoadoutPanel();
        }

        public void OpenShop()
        {
            RefreshGold();
            RefreshShopRows();
            CloseAllHubPanels();
            if (_shopInfoPanel != null) _shopInfoPanel.SetActive(false);
            _shopPanel.SetActive(true);
        }

        public void OpenLoadout()
        {
            RefreshLoadoutPanel();
            CloseAllHubPanels();
            _loadoutPanel.SetActive(true);
        }

        public void OpenStats()
        {
            RefreshStats();
            CloseAllHubPanels();
            _statsPanel.SetActive(true);
        }

        public void OpenAchievements()
        {
            // Retroactively grant arsenal achievements if progress already unlocked the tiers.
            Achievements.EvaluateWeaponTierAchievements();
            RefreshAchievements();
            CloseAllHubPanels();
            _achievementsPanel.SetActive(true);
        }

        void RefreshAchievements()
        {
            if (_achievementCountText != null)
                _achievementCountText.text =
                    $"Unlocked {Achievements.UnlockedCount}/{Achievements.All.Count}  ·  +{Achievements.CompletionGoldReward} gold each";

            foreach (var row in _achievementRows)
            {
                var unlocked = Achievements.IsUnlocked(row.Id);
                if (row.Background != null)
                {
                    row.Background.color = unlocked
                        ? new Color(0.35f, 0.72f, 0.42f, 1f)
                        : new Color(0.42f, 0.44f, 0.48f, 0.82f);
                }

                if (row.TitleText != null)
                {
                    row.TitleText.text = unlocked
                        ? Achievements.GetDef(row.Id).Title
                        : $"???  {Achievements.GetDef(row.Id).Title}";
                    row.TitleText.color = unlocked ? Color.white : new Color(0.78f, 0.8f, 0.84f);
                }

                if (row.DescText != null)
                {
                    row.DescText.text = Achievements.GetDef(row.Id).Description;
                    row.DescText.color = unlocked
                        ? new Color(0.92f, 0.96f, 0.98f)
                        : new Color(0.62f, 0.66f, 0.7f);
                }
            }
        }

        void RefreshStats()
        {
            if (_statsBodyText == null) return;

            var selected = GameSave.SelectedClass;
            var className = GetClassDisplayName(selected);
            var weaponTier = WeaponCatalog.GetEquippedTier(selected);
            var baseDamage = 10f * GameSave.DamageMultiplier * EquipmentCatalog.CombinedDamageMultiplier()
                * WeaponCatalog.DamageMultiplier(selected);
            if (selected == PlayerClass.Bowman) baseDamage *= 1.4f;
            else if (selected == PlayerClass.Spearman) baseDamage *= 1.15f;
            else if (selected == PlayerClass.Samurai) baseDamage *= 0.7f;
            var moveSpeed = TapMovement.DefaultBaseSpeed * GameSave.SpeedMultiplier
                            * EquipmentCatalog.CombinedMoveSpeedMultiplier();
            var maxHp = GameSave.MaxHp + EquipmentCatalog.CombinedBonusMaxHp();
            var movementLabel = GameSave.UsesJoystickMovement ? "Joystick" : "Tap / Hold";
            var rangeMul = GameSave.AttackRangeMultiplier;

            var attackMode = GameSave.GetSelectedAttackMode(selected);
            var technique = AttackModeCatalog.GetLabel(attackMode, selected);
            var standby = GameSave.GetStandbyHero();
            var companionLine = standby.HasValue
                ? $"Companion: RowZi (copies your {className} loadout, 20% dmg)\n"
                : "Companion: Unlock RowZi at R20 door\n";

            _statsBodyText.text =
                "CURRENT BUILD\n" +
                $"Hero: RollZy\n" +
                $"Class: {className}\n" +
                $"Technique: {technique}\n" +
                companionLine +
                $"Movement: {movementLabel}\n" +
                $"Weapon: {WeaponCatalog.GetTierName(weaponTier)} ({WeaponCatalog.GetPerkSummary(weaponTier)})\n" +
                $"Max HP: {maxHp}\n" +
                $"Base Damage: {baseDamage:0.#}\n" +
                $"Move Speed: {moveSpeed:0.##}\n" +
                $"Attack Range: x{rangeMul:0.##}\n" +
                $"HP Upgrades: {GameSave.HpUpgradeLevel}   Damage: {GameSave.DamageUpgradeLevel}   Speed: {GameSave.SpeedUpgradeLevel}   Range: {GameSave.RangeUpgradeLevel}\n" +
                $"Whirlwind: {(GameSave.WhirlwindUnlocked ? "Owned" : "Locked")}\n" +
                $"Piercing Shot: {(GameSave.PiercingShotUnlocked ? "Owned" : "Locked")}\n" +
                $"Frost Tip: {(GameSave.FrostTipUnlocked ? "Owned" : "Locked")}\n" +
                $"Flame Enchant: {(GameSave.FlameEnchantUnlocked ? "Owned" : "Knight quest reward")}\n" +
                $"Gold Magnet: {(GameSave.GoldMagnetUnlocked ? "Owned" : "Locked")}\n" +
                $"Thick Hide: T{GameSave.ThickHideLevel} ({(1f - GameSave.ThickHideDamageTakenMultiplier) * 100f:0}% DR)\n" +
                $"Second Wind: {GameSave.SecondWindMaxCharges} charge(s)/run\n" +
                $"Campfire Blessing: {(GameSave.CampfireBlessingUnlocked ? "Owned" : "Locked")}\n" +
                $"Achievement XP: x{Achievements.AchievementXpMultiplier:0.##}\n" +
                $"Crypt Map: {(GameSave.CryptMapUnlocked ? "Unlocked" : "Locked")}\n" +
                $"Unlimited Map: {(GameSave.UnlimitedMapUnlocked ? "Unlocked" : "Clear Crypt R50")}\n" +
                $"Ring: {EquipName(GameSave.EquippedRing)}\n" +
                $"Necklace: {EquipName(GameSave.EquippedNecklace)}\n" +
                $"Cape: {EquipName(GameSave.EquippedCape)}\n" +
                $"Helm: {EquipName(GameSave.EquippedHelm)}\n" +
                $"Equip DR: {EquipmentCatalog.CombinedDamageReduction() * 100f:0}%   Equip Block: {EquipmentCatalog.CombinedBlockChance() * 100f:0}%\n" +
                $"Spearman: {(GameSave.SpearmanUnlocked ? "Unlocked" : "Locked")}\n" +
                $"Bowman: {(GameSave.BowmanUnlocked ? "Unlocked" : "Locked")}\n" +
                $"Samurai: {(GameSave.SamuraiUnlocked ? "Unlocked" : "Locked")}\n" +
                $"Magician: {(GameSave.MagicianUnlocked ? "Unlocked" : "Clear Unlimited R80")}\n" +
                $"RowZi: {(GameSave.RowZiUnlocked ? "Unlocked" : "Meet at R20 door")}\n\n" +
                "LIFETIME RECORDS\n" +
                $"Zombie Kills: {GameSave.LifetimeZombieKills}\n" +
                $"Boss Kills: {GameSave.LifetimeBossKills}\n" +
                $"Deaths: {GameSave.LifetimeDeaths}\n" +
                $"Gold Earned: {GameSave.LifetimeGoldEarned}\n" +
                $"Highest Round: {GameSave.HighestRoundReached}\n" +
                $"Dungeon Best: {GameSave.DungeonHighestRoundReached}\n" +
                $"Crypt Best: {GameSave.CryptHighestRoundReached}\n" +
                $"Unlimited Best: {GameSave.UnlimitedHighestRoundReached}\n" +
                $"Weapons ({className}): {WeaponCatalog.GetUnlockProgressSummary(selected)}";
        }

        static string EquipName(EquipmentId id)
        {
            if (id == EquipmentId.None) return "None";
            var def = EquipmentCatalog.Get(id);
            return def.Id != EquipmentId.None ? def.DisplayName : "None";
        }

        public void OpenMapSelect()
        {
            CloseAllHubPanels();
            RefreshMapButtons(_mapPanel);
            _mapPanel.SetActive(true);
        }

        public void OpenCampfireTravel()
        {
            CloseAllHubPanels();
            RefreshMapButtons(_campfirePanel);
            _campfirePanel.SetActive(true);
        }

        static void RefreshMapButtons(GameObject panel)
        {
            if (panel == null) return;
            var recommended = GetRecommendedMap();

            foreach (var button in panel.GetComponentsInChildren<Button>(true))
            {
                if (button == null) continue;
                var label = button.GetComponentInChildren<Text>();
                if (label == null) continue;
                var text = label.text ?? string.Empty;

                // Match by map name substring so locked labels still refresh next open.
                if (text.Contains("Outside Survival") && !text.Contains("Inside"))
                {
                    button.interactable = true;
                    label.text = FormatMapButtonLabel(
                        "Outside Survival",
                        unlocked: true,
                        lockedHint: null,
                        recommended == SurvivalMapKind.Outside);
                }
                else if (text.Contains("Inside Survival") || text.Contains("Outside R20") || text.Contains("Inside —"))
                {
                    var unlocked = GameSave.InsideMapUnlocked;
                    button.interactable = unlocked;
                    label.text = FormatMapButtonLabel(
                        "Inside Survival",
                        unlocked,
                        "Inside — clear Outside R20 door",
                        unlocked && recommended == SurvivalMapKind.Inside);
                }
                else if (text.Contains("Dungeon Survival") || text.Contains("Inside R30") || text.Contains("Dungeon —"))
                {
                    var unlocked = GameSave.DungeonMapUnlocked;
                    button.interactable = unlocked;
                    label.text = FormatMapButtonLabel(
                        "Dungeon Survival",
                        unlocked,
                        "Dungeon — clear Inside R30 gateway",
                        unlocked && recommended == SurvivalMapKind.Dungeon);
                }
                else if (text.Contains("Crypt Survival") || text.Contains("Dungeon R40") || text.Contains("Crypt —"))
                {
                    var unlocked = GameSave.CryptMapUnlocked;
                    button.interactable = unlocked;
                    label.text = FormatMapButtonLabel(
                        "Crypt Survival",
                        unlocked,
                        "Crypt — clear Dungeon R40 portal",
                        unlocked && recommended == SurvivalMapKind.Crypt);
                }
                else if (text.Contains("Unlimited Survival") || text.Contains("Crypt R50") || text.Contains("Unlimited —"))
                {
                    var unlocked = GameSave.UnlimitedMapUnlocked;
                    button.interactable = unlocked;
                    label.text = FormatMapButtonLabel(
                        "Unlimited Survival",
                        unlocked,
                        "Unlimited — clear Crypt R50",
                        unlocked && recommended == SurvivalMapKind.Unlimited);
                }
            }
        }

        /// <summary>
        /// Next map in the unlock chain the player should play (or Unlimited once fully unlocked).
        /// </summary>
        public static SurvivalMapKind GetRecommendedMap()
        {
            if (!GameSave.InsideMapUnlocked) return SurvivalMapKind.Outside;
            if (!GameSave.DungeonMapUnlocked) return SurvivalMapKind.Inside;
            if (!GameSave.CryptMapUnlocked) return SurvivalMapKind.Dungeon;
            if (!GameSave.UnlimitedMapUnlocked) return SurvivalMapKind.Crypt;
            return SurvivalMapKind.Unlimited;
        }

        static string FormatMapButtonLabel(
            string unlockedLabel,
            bool unlocked,
            string lockedHint,
            bool recommended)
        {
            if (!unlocked)
                return lockedHint ?? unlockedLabel;
            return recommended ? $"{unlockedLabel}  ★ Recommended" : unlockedLabel;
        }

        public void RefreshGold()
        {
            if (_goldText != null) _goldText.text = GoldFormat.Abbreviate(GameSave.Gold);
        }

        void BuildHubGoldDisplay(Transform parent)
        {
            // Stone resource chip: bar background + coin icon + abbreviated gold.
            var chip = new GameObject("GoldChip");
            chip.transform.SetParent(parent, false);
            var chipRect = chip.AddComponent<RectTransform>();
            chipRect.anchorMin = new Vector2(1f, 1f);
            chipRect.anchorMax = new Vector2(1f, 1f);
            chipRect.pivot = new Vector2(1f, 1f);
            chipRect.anchoredPosition = new Vector2(-SafeRight + 40f, -SafeTop);
            chipRect.sizeDelta = new Vector2(220f, 56f);

            var bg = chip.AddComponent<Image>();
            if (StoneUi.Available && StoneUi.ResourceBarBg != null)
            {
                bg.sprite = StoneUi.ResourceBarBg;
                bg.type = Image.Type.Sliced;
                bg.color = Color.white;
            }
            else
            {
                bg.color = new Color(0.08f, 0.1f, 0.14f, 0.72f);
            }

            var coinSprite = StoneUi.Available && StoneUi.ResourceIconCoin != null
                ? StoneUi.ResourceIconCoin
                : StoneUi.Available && StoneUi.IconGold != null
                    ? StoneUi.IconGold
                    : ArtLibrary.GoldCoin;

            var coinGo = new GameObject("CoinIcon");
            coinGo.transform.SetParent(chip.transform, false);
            var coinRect = coinGo.AddComponent<RectTransform>();
            coinRect.anchorMin = new Vector2(0f, 0.5f);
            coinRect.anchorMax = new Vector2(0f, 0.5f);
            coinRect.pivot = new Vector2(0f, 0.5f);
            coinRect.anchoredPosition = new Vector2(14f, 0f);
            coinRect.sizeDelta = new Vector2(40f, 40f);
            var coinImage = coinGo.AddComponent<Image>();
            coinImage.sprite = coinSprite;
            coinImage.raycastTarget = false;

            _goldText = CreateText(chip.transform, "0", 28, TextAnchor.MiddleCenter, new Vector2(18f, 0f), new Vector2(140f, 42f));
            _goldText.alignment = TextAnchor.MiddleCenter;
            _goldText.color = new Color(1f, 0.94f, 0.72f);
        }

        static Text CreateText(Transform parent, string text, int size, TextAnchor anchor, Vector2 pos, Vector2 sizeDelta)
        {
            var go = new GameObject("Text");
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
            rect.sizeDelta = sizeDelta;
            var label = go.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = text;
            label.fontSize = size;
            label.color = Color.white;
            label.alignment = anchor;
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
            // Scale whole panel (border + content) so layouts stay proportional and fit the screen.
            go.transform.localScale = Vector3.one * HubMenuScale;
            var image = go.AddComponent<Image>();
            // Same Stone border as talent pick windows (popup_bg); fall back to legacy art.
            UiSprites.ApplyPanelSprite(image, background, largeMenu: false);
            return go;
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

        static Button CreateButton(Transform parent, string label, Vector2 pos, Action onClick, bool large = false)
        {
            var go = new GameObject(label + "Button");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            var size = large ? new Vector2(300, 72) : new Vector2(240, 58);
            // Compact +/- volume buttons
            if (label == "−" || label == "+")
                size = new Vector2(88, 58);
            rect.sizeDelta = size;
            var image = go.AddComponent<Image>();
            UiSprites.ApplyButtonSprite(image, size);
            var button = go.AddComponent<Button>();
            button.onClick.AddListener(() => onClick());
            var fontSize = large ? 30 : 24;
            if (label == "−" || label == "+")
                fontSize = 32;
            var labelText = CreateText(go.transform, label, fontSize, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(size.x - 24f, size.y - 10f));
            labelText.horizontalOverflow = HorizontalWrapMode.Wrap;
            labelText.verticalOverflow = VerticalWrapMode.Truncate;
            return button;
        }

        static Button CreateTopRightButton(Transform parent, string label, Vector2 pos, Action onClick)
        {
            var go = new GameObject(label + "Button");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = pos;
            var size = new Vector2(200, 56);
            rect.sizeDelta = size;
            var image = go.AddComponent<Image>();
            UiSprites.ApplyButtonSprite(image, size);
            var button = go.AddComponent<Button>();
            button.onClick.AddListener(() => onClick());
            var labelText = CreateText(go.transform, label, 24, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(size.x - 20f, size.y - 10f));
            labelText.horizontalOverflow = HorizontalWrapMode.Wrap;
            labelText.verticalOverflow = VerticalWrapMode.Truncate;
            return button;
        }
    }
}