using System;
using System.Collections.Generic;
using ProjectZx.Core;
using ProjectZx.HeroEditor;
using ProjectZx.Player;
using ProjectZx.World;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectZx.UI
{
    public partial class HubUi : MonoBehaviour
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

        // _shopPanel / _loadoutPanel + shop/loadout helpers live in HubUi.Shop.cs
        GameObject _statsPanel;
        GameObject _achievementsPanel;
        GameObject _mapPanel;
        GameObject _campfirePanel;
        GameObject _equipmentPanel;
        GameObject _questPanel;
        Image _questPortraitImage;
        GameObject _questPortraitFrame;
        Text _questTitleText;
        Text _questBodyText;
        Text _questStatusText;
        Text _questLogText;
        Button _questAcceptButton;
        Button _questTurnInButton;
        Button _questNextButton;
        Button _questMapsButton;
        Text _questAcceptLabel;
        Text _questTurnInLabel;
        QuestId _questPanelFocusId = QuestId.GrandWizardsPeril;
        QuestId[] _questPanelPool = Array.Empty<QuestId>();
        Text _equipmentStatusText;
        Text _campObjectiveText;
        Image _campObjectiveBg;
        GameObject _campObjectiveChip;
        float _campObjectivePulse;
        bool _readyTurnInToastShown;
        readonly List<Button> _equipmentButtons = new();

        GameObject _onboardingPanel;
        Text _onboardingTitle;
        Text _onboardingBody;
        Text _onboardingStepLabel;
        int _onboardingStep;
        GameObject _runToastPanel;
        Text _runToastText;
        float _runToastTimer;

        static readonly string[] OnboardingSteps =
        {
            "Welcome to Project Zx!\n\nThis is your camp. Upgrades and gold you bank from runs stay here forever.",
            "Talk to Mira the Outfitter (left of the campfire) for permanent upgrades.\nWhirlwind is a strong early pick.",
            "Talk to Captain Bren (right of the campfire) to start Emberwilds Survival.\nSurvive waves, level up, and bank gold on death or retreat.",
            "Talk to Archmage Thalor for quests.\nQuests teach the map unlock chain and pay gold rewards.",
            "Tip: Use Retreat anytime to bank gold safely.\nUnstuck (once per run) returns you to the map spawn.\n\nGood luck, hero!"
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
            var giver = QuestCatalog.GetGiverDisplayName(def.Id);
            ShowCampQuestToast($"Quest ready — talk to {giver} ({def.Title}). Tap the objective chip.");
        }

        void ShowCampQuestToast(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            // Reuse the rotating tip strip so ready turn-ins are readable once per camp visit.
            CampTipController.EnsureExists()?.ShowForcedTip(message);
        }

        void OnCampObjectiveClicked()
        {
            if (QuestCatalog.TryGetReadyToTurnInQuest(out var ready))
            {
                OpenQuestForId(ready.Id);
                return;
            }

            if (QuestCatalog.TryGetActiveQuest(out var active))
            {
                OpenQuestForId(active.Id);
                return;
            }

            if (QuestCatalog.TryFindHudQuestAvailable(out var available))
            {
                OpenQuestForId(available.Id);
                return;
            }

            OpenQuestGiver();
        }

        /// <summary>Open dialogue with the correct NPC pool, focused on this quest (not the pool's primary).</summary>
        void OpenQuestForId(QuestId questId)
        {
            _questPanelPool = QuestCatalog.GetQuestPoolFor(questId);
            OpenQuestDialogue(questId);
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

        static MedievalNpcLibrary.Cast PortraitCastForQuest(QuestId id)
        {
            if (id == QuestId.KnightsBestFriend)
                return MedievalNpcLibrary.Cast.Aldric;
            if (id == QuestId.GreyWizardsCrow
                || id == QuestId.CorvinsOmen
                || id == QuestId.CorvinsShade
                || id == QuestId.AshCrownRising)
                return MedievalNpcLibrary.Cast.Corvin;
            if (id == QuestId.LyraVigil)
                return MedievalNpcLibrary.Cast.Lyra;
            if (id == QuestId.BrensWatch)
                return MedievalNpcLibrary.Cast.Bren;
            if (id == QuestId.KaelsRecon || id == QuestId.KaelsFlank)
                return MedievalNpcLibrary.Cast.Kael;
            if (id == QuestId.MirasStock)
                return MedievalNpcLibrary.Cast.Mira;
            if (id == QuestId.NessasSalve)
                return MedievalNpcLibrary.Cast.Nessa;
            if (id == QuestId.GarricksAnvil)
                return MedievalNpcLibrary.Cast.Garrick;
            if (id == QuestId.TovesChart)
                return MedievalNpcLibrary.Cast.Tove;
            return MedievalNpcLibrary.Cast.Thalor;
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
                "Set class & technique at Mira's shop first.\nUnlocked maps start fresh at round 1.");

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
            TagMapButton(
                CreateButton(panel.transform, SurvivalMapNames.SurvivalButtonLabel(SurvivalMapKind.Outside), new Vector2(0, 80), () => EnterSurvival(SurvivalMapKind.Outside), large: true),
                SurvivalMapKind.Outside);
            TagMapButton(
                CreateButton(panel.transform, SurvivalMapNames.SurvivalButtonLabel(SurvivalMapKind.Inside), new Vector2(0, 15), () => EnterSurvival(SurvivalMapKind.Inside), large: true),
                SurvivalMapKind.Inside);
            TagMapButton(
                CreateButton(panel.transform, SurvivalMapNames.SurvivalButtonLabel(SurvivalMapKind.Dungeon), new Vector2(0, -50), () => EnterSurvival(SurvivalMapKind.Dungeon), large: true),
                SurvivalMapKind.Dungeon);
            TagMapButton(
                CreateButton(panel.transform, SurvivalMapNames.SurvivalButtonLabel(SurvivalMapKind.Crypt), new Vector2(0, -115), () => EnterSurvival(SurvivalMapKind.Crypt), large: true),
                SurvivalMapKind.Crypt);
            TagMapButton(
                CreateButton(panel.transform, SurvivalMapNames.SurvivalButtonLabel(SurvivalMapKind.Unlimited), new Vector2(0, -180), () => EnterSurvival(SurvivalMapKind.Unlimited), large: true),
                SurvivalMapKind.Unlimited);
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

            var portraitGo = new GameObject("QuestPortrait");
            portraitGo.transform.SetParent(frameGo.transform, false);
            var portraitRect = portraitGo.AddComponent<RectTransform>();
            portraitRect.anchorMin = new Vector2(0.5f, 0.5f);
            portraitRect.anchorMax = new Vector2(0.5f, 0.5f);
            portraitRect.pivot = new Vector2(0.5f, 0.5f);
            portraitRect.anchoredPosition = Vector2.zero;
            portraitRect.sizeDelta = new Vector2(240f, 240f);
            _questPortraitImage = portraitGo.AddComponent<Image>();
            _questPortraitImage.sprite = MedievalNpcLibrary.Portrait(MedievalNpcLibrary.Cast.Thalor)
                ?? ArtLibrary.WizardPortrait;
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

            _questLogText = CreateText(
                panel.transform,
                "",
                16,
                TextAnchor.MiddleCenter,
                new Vector2(140f, 118f),
                new Vector2(520f, 52f));
            _questLogText.alignment = TextAnchor.UpperLeft;
            _questLogText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _questLogText.verticalOverflow = VerticalWrapMode.Overflow;
            _questLogText.color = new Color(0.75f, 0.82f, 0.9f);

            _questBodyText = CreateText(
                panel.transform,
                "",
                22,
                TextAnchor.MiddleCenter,
                new Vector2(140f, -30f),
                new Vector2(520f, 240f));
            _questBodyText.alignment = TextAnchor.UpperLeft;
            _questBodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _questBodyText.verticalOverflow = VerticalWrapMode.Overflow;
            _questBodyText.color = new Color(0.94f, 0.96f, 0.98f);

            _questAcceptButton = CreateButton(panel.transform, "Accept", new Vector2(-120f, -210f), AcceptFocusedQuest);
            _questTurnInButton = CreateButton(panel.transform, "Turn In", new Vector2(80f, -210f), TurnInFocusedQuest);
            _questNextButton = CreateButton(panel.transform, "Next Task", new Vector2(280f, -210f), CycleQuestFocus);
            _questMapsButton = CreateButton(panel.transform, "Maps", new Vector2(420f, -210f), OpenMapSelectFromQuest);
            _questAcceptLabel = _questAcceptButton.GetComponentInChildren<Text>();
            _questTurnInLabel = _questTurnInButton.GetComponentInChildren<Text>();
            CreateButton(panel.transform, "Close", new Vector2(170f, -280f), () => panel.SetActive(false));

            panel.SetActive(false);
            return panel;
        }

        /// <summary>Archmage Thalor — pendant, gate, and crow while still hunting.</summary>
        public void OpenQuestGiver()
        {
            OpenQuestGiverWithPool(QuestCatalog.GetThalorQuestIds(), QuestCatalog.GrandWizardsPeril.Id);
        }

        /// <summary>Ashen Seer Corvin — crow, omen, shade, Ash Crown.</summary>
        public void OpenCorvinQuestGiver()
        {
            OpenQuestGiverWithPool(QuestCatalog.CorvinQuestIds, QuestId.AshCrownRising);
        }

        /// <summary>Sir Aldric — Ironvault greatsword quest.</summary>
        public void OpenKnightQuestGiver()
        {
            OpenQuestGiverWithPool(QuestCatalog.KnightQuestIds, QuestId.KnightsBestFriend);
        }

        /// <summary>Sister Lyra — Silent Ossuary vigil.</summary>
        public void OpenLyraQuestGiver()
        {
            OpenQuestGiverWithPool(QuestCatalog.LyraQuestIds, QuestId.LyraVigil);
        }

        /// <summary>Scout Kael — Emberwilds recon / flank side quests.</summary>
        public void OpenKaelQuestGiver()
        {
            OpenQuestGiverWithPool(QuestCatalog.KaelQuestIds, QuestId.KaelsRecon);
        }

        /// <summary>
        /// Mira the Outfitter: stock quest when open; otherwise shop.
        /// Quest panel Shop shortcut stays available while Mira's Stock is active.
        /// </summary>
        public void OpenMira()
        {
            var progress = QuestCatalog.GetProgress(QuestId.MirasStock);
            if (progress == QuestProgress.Available
                || progress == QuestProgress.Active
                || progress == QuestProgress.ReadyToTurnIn)
            {
                OpenMiraQuestGiver();
                return;
            }

            OpenShop();
        }

        public void OpenMiraQuestGiver()
        {
            OpenQuestGiverWithPool(QuestCatalog.MiraQuestIds, QuestId.MirasStock);
        }

        /// <summary>Herbalist Nessa — Warded Halls salve side quest.</summary>
        public void OpenNessaQuestGiver()
        {
            OpenQuestGiverWithPool(QuestCatalog.NessaQuestIds, QuestId.NessasSalve);
        }

        /// <summary>Smith Garrick — Ironvault anvil side quest.</summary>
        public void OpenGarrickQuestGiver()
        {
            OpenQuestGiverWithPool(QuestCatalog.GarrickQuestIds, QuestId.GarricksAnvil);
        }

        /// <summary>Cartographer Tove — Silent Ossuary chart side quest.</summary>
        public void OpenToveQuestGiver()
        {
            OpenQuestGiverWithPool(QuestCatalog.ToveQuestIds, QuestId.TovesChart);
        }

        /// <summary>
        /// Captain Bren: briefing when Endless Front quest is open; otherwise map select.
        /// </summary>
        public void OpenBren()
        {
            var progress = QuestCatalog.GetProgress(QuestId.BrensWatch);
            if (progress == QuestProgress.Available
                || progress == QuestProgress.Active
                || progress == QuestProgress.ReadyToTurnIn)
            {
                OpenBrenQuestGiver();
                return;
            }

            OpenMapSelect();
        }

        public void OpenBrenQuestGiver()
        {
            OpenQuestGiverWithPool(QuestCatalog.BrenQuestIds, QuestId.BrensWatch);
        }

        void OpenMapSelectFromQuest()
        {
            if (_questPanelFocusId == QuestId.MirasStock)
            {
                OpenShop();
                return;
            }

            OpenMapSelect();
        }

        void OpenQuestGiverWithPool(QuestId[] pool, QuestId fallbackId)
        {
            _questPanelPool = pool ?? Array.Empty<QuestId>();
            if (!QuestCatalog.TryGetPrimaryOpenQuest(pool, out var def, out _))
            {
                if (!QuestCatalog.TryGet(fallbackId, out def))
                    def = QuestCatalog.GrandWizardsPeril;
            }

            OpenQuestDialogue(def.Id);
        }

        /// <summary>Open the quest panel focused on a specific quest id.</summary>
        public void OpenQuestDialogue(QuestId questId)
        {
            RefreshGold();
            CloseAllHubPanels();
            _questPanelFocusId = questId;
            // Always bind the giver pool to the focused quest (chip / external opens used to fall back to Thalor).
            if (_questPanelPool == null || _questPanelPool.Length == 0
                || System.Array.IndexOf(_questPanelPool, questId) < 0)
            {
                _questPanelPool = QuestCatalog.GetQuestPoolFor(questId);
            }

            RefreshQuestPanel();
            if (_questPanel != null)
            {
                _questPanel.SetActive(true);
                var sparkleX = QuestCatalog.UsesQuestPortrait(questId) ? -300f : 0f;
                SparkleBurst.Play(_questPanel.transform, new Vector2(sparkleX, 40f), 8);
            }
        }

        void CycleQuestFocus()
        {
            if (_questPanelPool == null || _questPanelPool.Length <= 1) return;
            var start = Array.IndexOf(_questPanelPool, _questPanelFocusId);
            if (start < 0) start = 0;
            for (var step = 1; step <= _questPanelPool.Length; step++)
            {
                var next = _questPanelPool[(start + step) % _questPanelPool.Length];
                if (QuestCatalog.GetProgress(next) == QuestProgress.Locked) continue;
                _questPanelFocusId = next;
                RefreshQuestPanel();
                return;
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

            if (showPortrait)
            {
                if (_questPortraitImage != null)
                {
                    _questPortraitImage.sprite = MedievalNpcLibrary.Portrait(PortraitCastForQuest(def.Id))
                        ?? ArtLibrary.WizardPortrait;
                    _questPortraitImage.enabled = true;
                }
            }
            else if (_questPortraitImage != null)
            {
                _questPortraitImage.enabled = false;
            }

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
                var rewardSuffix = QuestCatalog.RewardSummary(def);
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

            var showPoolLog = _questPanelPool != null && _questPanelPool.Length > 1;
            var campDigest = QuestCatalog.BuildCampQuestDigest(def.Id);
            // Thalor without crow (pendant + path only): prefer camp-wide digest so other NPCs stay visible.
            var isThalorSlimPool = _questPanelPool != null
                && _questPanelPool.Length <= 2
                && System.Array.IndexOf(_questPanelPool, QuestId.GrandWizardsPeril) >= 0;
            var showCampDigest = !string.IsNullOrEmpty(campDigest)
                                 && campDigest.IndexOf('\n') >= 0
                                 && (!showPoolLog || isThalorSlimPool);
            if (_questLogText != null)
            {
                if (showCampDigest)
                    _questLogText.text = campDigest;
                else if (showPoolLog)
                    _questLogText.text = QuestCatalog.BuildQuestLog(_questPanelPool, def.Id);
                else
                    _questLogText.text = string.Empty;

                var showLog = !string.IsNullOrEmpty(_questLogText.text);
                _questLogText.gameObject.SetActive(showLog);
                var logRect = _questLogText.rectTransform;
                logRect.anchoredPosition = new Vector2(textX, 118f);
                logRect.sizeDelta = new Vector2(textW, showCampDigest ? 72f : 52f);
            }

            if (_questBodyText != null)
            {
                _questBodyText.text = QuestCatalog.GetQuestBodyText(def.Id, progress);
                var bodyRect = _questBodyText.rectTransform;
                var showLog = showPoolLog || showCampDigest;
                var bodyY = showLog ? -40f : -10f;
                var bodyH = showLog ? 220f : 280f;
                bodyRect.anchoredPosition = new Vector2(textX, bodyY);
                bodyRect.sizeDelta = new Vector2(textW, bodyH);
            }

            var canAccept = progress == QuestProgress.Available;
            var canTurnIn = progress == QuestProgress.ReadyToTurnIn;
            var canCycle = _questPanelPool != null && _questPanelPool.Length > 1;
            // Maps shortcut while briefing Endless Front quests (Bren or Corvin).
            // Mira reuses the same button as "Shop" so outfitter stays reachable mid-quest.
            var showFrontMaps = def.Id == QuestId.BrensWatch
                || def.Id == QuestId.CorvinsOmen
                || def.Id == QuestId.CorvinsShade
                || def.Id == QuestId.AshCrownRising;
            var showMiraShop = def.Id == QuestId.MirasStock;
            var showSideButton = showFrontMaps || showMiraShop;
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

            if (_questNextButton != null)
            {
                _questNextButton.gameObject.SetActive(canCycle);
                _questNextButton.interactable = canCycle;
            }

            if (_questMapsButton != null)
            {
                _questMapsButton.gameObject.SetActive(showSideButton);
                _questMapsButton.interactable = showSideButton;
                var mapsLabel = _questMapsButton.GetComponentInChildren<Text>();
                if (mapsLabel != null)
                    mapsLabel.text = showMiraShop ? "Shop" : "Maps";
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
            if (questId == QuestId.BrensWatch
                && QuestCatalog.GetProgress(QuestId.CorvinsOmen) == QuestProgress.Available)
            {
                ShowCampQuestToast("The dead stir — talk to Corvin by the treeline (Corvin's Omen).");
            }
            else if (questId == QuestId.CorvinsOmen
                     && QuestCatalog.GetProgress(QuestId.CorvinsShade) == QuestProgress.Available)
            {
                ShowCampQuestToast("A shade tore free — talk to Corvin (Corvin's Shade).");
            }
            else if (questId == QuestId.CorvinsShade
                     && QuestCatalog.GetProgress(QuestId.AshCrownRising) == QuestProgress.Available)
            {
                ShowCampQuestToast("The Ash Crown stirs — talk to Corvin (Ash Crown Rising).");
            }
            else if (questId == QuestId.AshCrownRising)
            {
                ShowCampQuestToast("Ash Crown answered — Altair unlocked in Loadout (Fateful power, no AOE).");
            }
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
                $"Flame Enchant: {(GameSave.FlameEnchantUnlocked ? "Owned" : "Sir Aldric quest reward")}\n" +
                $"Gold Magnet: {(GameSave.GoldMagnetUnlocked ? "Owned" : "Locked")}\n" +
                $"Thick Hide: T{GameSave.ThickHideLevel} ({(1f - GameSave.ThickHideDamageTakenMultiplier) * 100f:0}% DR)\n" +
                $"Second Wind: {GameSave.SecondWindMaxCharges} charge(s)/run\n" +
                $"Campfire Blessing: {(GameSave.CampfireBlessingUnlocked ? "Owned" : "Locked")}\n" +
                $"Achievement XP: x{Achievements.AchievementXpMultiplier:0.##}\n" +
                $"Silent Ossuary: {(GameSave.CryptMapUnlocked ? "Unlocked" : "Locked")}\n" +
                $"Endless Front: {(GameSave.UnlimitedMapUnlocked ? "Unlocked" : "Clear Silent Ossuary R50")}\n" +
                $"Ring: {EquipName(GameSave.EquippedRing)}\n" +
                $"Necklace: {EquipName(GameSave.EquippedNecklace)}\n" +
                $"Cape: {EquipName(GameSave.EquippedCape)}\n" +
                $"Helm: {EquipName(GameSave.EquippedHelm)}\n" +
                $"Equip DR: {EquipmentCatalog.CombinedDamageReduction() * 100f:0}%   Equip Block: {EquipmentCatalog.CombinedBlockChance() * 100f:0}%\n" +
                $"Spearman: {(GameSave.SpearmanUnlocked ? "Unlocked" : "Locked")}\n" +
                $"Bowman: {(GameSave.BowmanUnlocked ? "Unlocked" : "Locked")}\n" +
                $"Samurai: {(GameSave.SamuraiUnlocked ? "Unlocked" : "Locked")}\n" +
                $"Magician: {(GameSave.MagicianUnlocked ? "Unlocked" : "Clear Endless Front R80")}\n" +
                $"RowZi: {(GameSave.RowZiUnlocked ? "Unlocked" : "Meet at Emberwilds R20 door")}\n\n" +
                "LIFETIME RECORDS\n" +
                $"Zombie Kills: {GameSave.LifetimeZombieKills}\n" +
                $"Boss Kills: {GameSave.LifetimeBossKills}\n" +
                $"Deaths: {GameSave.LifetimeDeaths}\n" +
                $"Gold Earned: {GameSave.LifetimeGoldEarned}\n" +
                $"Highest Round: {GameSave.HighestRoundReached}\n" +
                $"Ironvault Best: {GameSave.DungeonHighestRoundReached}\n" +
                $"Ossuary Best: {GameSave.CryptHighestRoundReached}\n" +
                $"Endless Front Best: {GameSave.UnlimitedHighestRoundReached}\n" +
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

        static void TagMapButton(Button button, SurvivalMapKind kind)
        {
            if (button == null) return;
            var tag = button.GetComponent<MapSelectButtonTag>();
            if (tag == null)
                tag = button.gameObject.AddComponent<MapSelectButtonTag>();
            tag.Kind = kind;
            button.gameObject.name = $"MapBtn_{kind}";
        }

        static void RefreshMapButtons(GameObject panel)
        {
            if (panel == null) return;
            var recommended = GetRecommendedMap();

            foreach (var tag in panel.GetComponentsInChildren<MapSelectButtonTag>(true))
            {
                if (tag == null) continue;
                var button = tag.GetComponent<Button>();
                var label = tag.GetComponentInChildren<Text>();
                if (button == null || label == null) continue;

                switch (tag.Kind)
                {
                    case SurvivalMapKind.Outside:
                        button.interactable = true;
                        label.text = FormatMapButtonLabel(
                            SurvivalMapNames.SurvivalButtonLabel(SurvivalMapKind.Outside),
                            unlocked: true,
                            lockedHint: null,
                            recommended == SurvivalMapKind.Outside);
                        break;
                    case SurvivalMapKind.Inside:
                    {
                        var unlocked = GameSave.InsideMapUnlocked;
                        button.interactable = unlocked;
                        label.text = FormatMapButtonLabel(
                            SurvivalMapNames.SurvivalButtonLabel(SurvivalMapKind.Inside),
                            unlocked,
                            "Locked — clear Emberwilds R20 door",
                            unlocked && recommended == SurvivalMapKind.Inside);
                        break;
                    }
                    case SurvivalMapKind.Dungeon:
                    {
                        var unlocked = GameSave.DungeonMapUnlocked;
                        button.interactable = unlocked;
                        label.text = FormatMapButtonLabel(
                            SurvivalMapNames.SurvivalButtonLabel(SurvivalMapKind.Dungeon),
                            unlocked,
                            "Locked — clear Warded Halls R30 gateway",
                            unlocked && recommended == SurvivalMapKind.Dungeon);
                        break;
                    }
                    case SurvivalMapKind.Crypt:
                    {
                        var unlocked = GameSave.CryptMapUnlocked;
                        button.interactable = unlocked;
                        label.text = FormatMapButtonLabel(
                            SurvivalMapNames.SurvivalButtonLabel(SurvivalMapKind.Crypt),
                            unlocked,
                            "Locked — clear Ironvault R40 portal",
                            unlocked && recommended == SurvivalMapKind.Crypt);
                        break;
                    }
                    case SurvivalMapKind.Unlimited:
                    {
                        var unlocked = GameSave.UnlimitedMapUnlocked;
                        button.interactable = unlocked;
                        label.text = FormatMapButtonLabel(
                            SurvivalMapNames.SurvivalButtonLabel(SurvivalMapKind.Unlimited),
                            unlocked,
                            "Locked — clear Silent Ossuary R50",
                            unlocked && recommended == SurvivalMapKind.Unlimited);
                        break;
                    }
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
