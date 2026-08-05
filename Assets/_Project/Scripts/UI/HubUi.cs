using System;
using System.Collections.Generic;
using ProjectZx.Core;
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
            || IsPanelOpen(_equipmentPanel) || IsPanelOpen(_settingsPanel);

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
        Text _equipmentStatusText;
        Text _bgmVolumeLabel;
        Text _sfxVolumeLabel;
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
        Button _movementJoystickButton;
        Button _movementTapHoldButton;

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

        void Awake()
        {
            Instance = this;
            Build();
            RefreshGold();
            Achievements.OnUnlocked += OnAchievementUnlockedAtCamp;
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

            _shopPanel = BuildShopPanel(canvasGo.transform);
            _loadoutPanel = BuildLoadoutPanel(canvasGo.transform);
            _statsPanel = BuildStatsPanel(canvasGo.transform);
            _achievementsPanel = BuildAchievementsPanel(canvasGo.transform);
            _mapPanel = BuildMapPanel(canvasGo.transform);
            _campfirePanel = BuildCampfirePanel(canvasGo.transform);
            _equipmentPanel = BuildEquipmentPanel(canvasGo.transform);
            _settingsPanel = BuildSettingsPanel(canvasGo.transform);
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
            cardRect.sizeDelta = new Vector2(760f, 540f);
            var cardImage = card.AddComponent<Image>();
            UiSprites.ApplyPanelSprite(cardImage, ArtLibrary.LevelUpUi, largeMenu: false);

            _shopInfoTitle = CreateText(card.transform, "Upgrade Info", 34, TextAnchor.MiddleCenter, new Vector2(0, 200), new Vector2(680, 48));
            _shopInfoBody = CreateText(card.transform, "", 22, TextAnchor.UpperLeft, new Vector2(0, -10), new Vector2(660, 320));
            _shopInfoBody.alignment = TextAnchor.UpperLeft;
            _shopInfoBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            _shopInfoBody.verticalOverflow = VerticalWrapMode.Overflow;
            CreateButton(card.transform, "Close", new Vector2(0, -210), () => _shopInfoPanel.SetActive(false), large: true);
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
            CreateText(panel.transform, "Build Loadout", 38, TextAnchor.MiddleCenter, new Vector2(0, 350), new Vector2(620, 52));
            CreateText(panel.transform, "Class is saved per hero. Swap heroes at camp to set the companion build.\nMovement & audio live in Settings.", 18, TextAnchor.MiddleCenter, new Vector2(0, 300), new Vector2(820, 48));

            // Class section (3 rows: Batter/Spearman, Bowman/Samurai, Magician)
            _loadoutClassPicker = BuildClassPicker(panel.transform, 250f, 205f, 130f);

            // Technique section under Magician row
            CreateText(panel.transform, "Attack Technique", 28, TextAnchor.MiddleCenter, new Vector2(0, -80), new Vector2(620, 40));
            _techniqueStatusText = CreateText(panel.transform, "", 20, TextAnchor.MiddleCenter, new Vector2(0, -126), new Vector2(780, 52));
            _techniqueStatusText.alignment = TextAnchor.UpperCenter;
            _techniqueStandardButton = CreateButton(panel.transform, "Standard", new Vector2(-160, -200), () => SelectAttackMode(AttackMode.Standard));
            _techniqueSpecialButton = CreateButton(panel.transform, "Special", new Vector2(160, -200), SelectSpecialAttackMode);

            CreateButton(panel.transform, "Back to Shop", new Vector2(-160, -300), () =>
            {
                panel.SetActive(false);
                OpenShop();
            });
            CreateButton(panel.transform, "Close", new Vector2(160, -300), () => panel.SetActive(false));
            panel.SetActive(false);
            return panel;
        }

        GameObject BuildSettingsPanel(Transform parent)
        {
            var panel = CreateDialogPanel(parent, "SettingsPanel", Vector2.zero, HubMenuPanelSize, ArtLibrary.ShopUi);
            CreateText(panel.transform, "Settings", 40, TextAnchor.MiddleCenter, new Vector2(0, 300), new Vector2(560, 52));

            CreateText(panel.transform, "Movement Control", 28, TextAnchor.MiddleCenter, new Vector2(0, 220), new Vector2(620, 40));
            CreateText(panel.transform, "Only one control style is active at a time.", 20, TextAnchor.MiddleCenter, new Vector2(0, 180), new Vector2(700, 32));
            _movementJoystickButton = CreateButton(panel.transform, "Joystick", new Vector2(-160, 110), () => SelectMovementControl(MovementControlType.Joystick));
            _movementTapHoldButton = CreateButton(panel.transform, "Tap / Hold", new Vector2(160, 110), () => SelectMovementControl(MovementControlType.TapHold));
            CreateText(panel.transform, "Drag the on-screen joystick to place it. Position locks when you close Settings.", 18, TextAnchor.MiddleCenter, new Vector2(0, 55), new Vector2(900, 40));

            CreateText(panel.transform, "Music Volume", 26, TextAnchor.MiddleCenter, new Vector2(0, -10), new Vector2(400, 36));
            _bgmVolumeLabel = CreateText(panel.transform, "70%", 22, TextAnchor.MiddleCenter, new Vector2(0, -50), new Vector2(120, 32));
            CreateButton(panel.transform, "−", new Vector2(-200, -50), () => AdjustBgmVolume(-0.1f));
            CreateButton(panel.transform, "+", new Vector2(200, -50), () => AdjustBgmVolume(0.1f));

            CreateText(panel.transform, "SFX Volume", 26, TextAnchor.MiddleCenter, new Vector2(0, -130), new Vector2(400, 36));
            _sfxVolumeLabel = CreateText(panel.transform, "85%", 22, TextAnchor.MiddleCenter, new Vector2(0, -170), new Vector2(120, 32));
            CreateButton(panel.transform, "−", new Vector2(-200, -170), () => AdjustSfxVolume(-0.1f));
            CreateButton(panel.transform, "+", new Vector2(200, -170), () => AdjustSfxVolume(0.1f));

            CreateButton(panel.transform, "Close", new Vector2(0, -280), () => CloseSettings(), large: true);
            panel.SetActive(false);
            return panel;
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

        GameObject BuildMapPanel(Transform parent)
        {
            var panel = CreateDialogPanel(parent, "MapPanel", Vector2.zero, HubMenuPanelSize, ArtLibrary.ChallengeBoardUi);
            CreateText(panel.transform, "Survival Challenge", 40, TextAnchor.MiddleCenter, new Vector2(0, 250), new Vector2(700, 56));
            CreateText(panel.transform, "Set class & technique at the Wizard shop first.\nUnlocked maps start fresh at round 1.", 24, TextAnchor.MiddleCenter, new Vector2(0, 175), new Vector2(760, 72));
            CreateButton(panel.transform, "Outside Survival", new Vector2(0, 80), () => EnterSurvival(SurvivalMapKind.Outside), large: true);
            CreateButton(panel.transform, "Inside Survival", new Vector2(0, 15), () => EnterSurvival(SurvivalMapKind.Inside), large: true);
            CreateButton(panel.transform, "Dungeon Survival", new Vector2(0, -50), () => EnterSurvival(SurvivalMapKind.Dungeon), large: true);
            CreateButton(panel.transform, "Crypt Survival", new Vector2(0, -115), () => EnterSurvival(SurvivalMapKind.Crypt), large: true);
            CreateButton(panel.transform, "Unlimited Survival", new Vector2(0, -180), () => EnterSurvival(SurvivalMapKind.Unlimited), large: true);
            CreateButton(panel.transform, "Close", new Vector2(0, -255), () => panel.SetActive(false), large: true);
            panel.SetActive(false);
            return panel;
        }

        GameObject BuildCampfirePanel(Transform parent)
        {
            var panel = CreateDialogPanel(parent, "CampfirePanel", Vector2.zero, HubMenuPanelSize, ArtLibrary.ChallengeBoardUi);
            CreateText(panel.transform, "Campfire Travel", 34, TextAnchor.MiddleCenter, new Vector2(0, 250), new Vector2(640, 48));
            CreateText(panel.transform, "Choose an unlocked map. Each run starts at round 1.", 20, TextAnchor.MiddleCenter, new Vector2(0, 185), new Vector2(680, 48));
            CreateButton(panel.transform, "Outside Survival", new Vector2(0, 100), () => EnterSurvival(SurvivalMapKind.Outside));
            CreateButton(panel.transform, "Inside Survival", new Vector2(0, 40), () => EnterSurvival(SurvivalMapKind.Inside));
            CreateButton(panel.transform, "Dungeon Survival", new Vector2(0, -20), () => EnterSurvival(SurvivalMapKind.Dungeon));
            CreateButton(panel.transform, "Crypt Survival", new Vector2(0, -80), () => EnterSurvival(SurvivalMapKind.Crypt));
            CreateButton(panel.transform, "Unlimited Survival", new Vector2(0, -140), () => EnterSurvival(SurvivalMapKind.Unlimited));
            CreateButton(panel.transform, "Close", new Vector2(0, -210), () => panel.SetActive(false));
            panel.SetActive(false);
            return panel;
        }

        GameObject BuildEquipmentPanel(Transform parent)
        {
            var panel = CreateDialogPanel(parent, "EquipmentPanel", Vector2.zero, HubMenuPanelSize, ArtLibrary.ShopUi);
            CreateText(panel.transform, "Treasure Chest", 36, TextAnchor.MiddleCenter, new Vector2(0, 390), new Vector2(700, 48));
            CreateText(panel.transform, "One ring, necklace, and cape. Drops unlock here after you find them.", 18, TextAnchor.MiddleCenter, new Vector2(0, 345), new Vector2(920, 36));
            _equipmentStatusText = CreateText(panel.transform, "", 20, TextAnchor.MiddleCenter, new Vector2(0, 300), new Vector2(920, 40));

            CreateText(panel.transform, "Rings", 24, TextAnchor.MiddleCenter, new Vector2(0, 250), new Vector2(400, 32));
            CreateText(panel.transform, "Necklaces", 24, TextAnchor.MiddleCenter, new Vector2(0, 70), new Vector2(400, 32));
            CreateText(panel.transform, "Capes", 24, TextAnchor.MiddleCenter, new Vector2(0, -110), new Vector2(400, 32));

            _equipmentButtons.Clear();
            // Unequip + 3 items per type (4 columns).
            var slotX = new[] { -360f, -120f, 120f, 360f };
            var ringIndex = 0;
            var neckIndex = 0;
            var capeIndex = 0;
            const float ringY = 190f;
            const float neckY = 10f;
            const float capeY = -170f;

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
                }
            }

            CreateButton(panel.transform, "Close", new Vector2(0, -360), () => panel.SetActive(false), large: true);
            panel.SetActive(false);
            return panel;
        }

        void SelectEquipment(EquipmentId id)
        {
            if (!GameSave.OwnsEquipment(id)) return;
            GameSave.Equip(id);
            RefreshEquipmentPanel();
            SparkleBurst.Play(_equipmentPanel != null ? _equipmentPanel.transform : transform, Vector2.zero, 12);
        }

        void RefreshEquipmentPanel()
        {
            if (_equipmentStatusText != null)
            {
                var ring = EquipmentCatalog.Get(GameSave.EquippedRing);
                var neck = EquipmentCatalog.Get(GameSave.EquippedNecklace);
                var cape = EquipmentCatalog.Get(GameSave.EquippedCape);
                var ringName = ring.Id != EquipmentId.None ? ring.DisplayName : "None";
                var neckName = neck.Id != EquipmentId.None ? neck.DisplayName : "None";
                var capeName = cape.Id != EquipmentId.None ? cape.DisplayName : "None";
                _equipmentStatusText.text = $"Equipped: {ringName}  ·  {neckName}  ·  {capeName}";
            }

            // Button order: No Ring, No Necklace, No Cape, then catalog All in order.
            var buttonIndex = 0;
            RefreshEquipButton(GetEquipButton(buttonIndex++), EquipmentId.None, EquipmentSlot.Ring, "No Ring");
            RefreshEquipButton(GetEquipButton(buttonIndex++), EquipmentId.None, EquipmentSlot.Necklace, "No Necklace");
            RefreshEquipButton(GetEquipButton(buttonIndex++), EquipmentId.None, EquipmentSlot.Cape, "No Cape");

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
            if (_settingsPanel != null && _settingsPanel.activeSelf)
                CloseSettings();
            else if (_settingsPanel != null)
                _settingsPanel.SetActive(false);
        }

        void OpenSettings()
        {
            CloseAllHubPanels();
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
            RefreshVolumeLabels();
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
            RefreshClassButton(picker.SpearmanButton, PlayerClass.Spearman, GameSave.SpearmanUnlocked, "Spearman (Beat R20 Boss)");
            RefreshClassButton(picker.BowmanButton, PlayerClass.Bowman, GameSave.BowmanUnlocked, "Bowman (Clear R30 Inside)");
            RefreshClassButton(picker.SamuraiButton, PlayerClass.Samurai, GameSave.SamuraiUnlocked, "Samurai (Dungeon R40 Boss)");
            RefreshClassButton(picker.MagicianButton, PlayerClass.Magician, GameSave.MagicianUnlocked, "Magician (Clear Unlimited R80)");
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

            GameSessionContext.SurvivalMap = mapKind;
            GameSessionContext.SelectedHero = GameSave.SanitizeHero(GameSave.SelectedHero);
            GameSessionContext.SelectedClass = GameSave.GetHeroClass(GameSessionContext.SelectedHero);
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
                        "One-time upgrade for Spearman / Bowman / Samurai.\n\n" +
                        "Hits chill enemies for 1s (−60% move speed).\n\n" +
                        (GameSave.FrostTipUnlocked
                            ? "Status: Owned"
                            : !GameSave.SpearmanUnlocked && !GameSave.BowmanUnlocked && !GameSave.SamuraiUnlocked
                                ? "Status: Locked — unlock Spearman, Bowman, or Samurai"
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
            if (!GameSave.SpearmanUnlocked && !GameSave.BowmanUnlocked && !GameSave.SamuraiUnlocked) return;
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
            else if (!GameSave.SpearmanUnlocked && !GameSave.BowmanUnlocked && !GameSave.SamuraiUnlocked)
                SetLockedRow(_frostTipRow, "Frost Tip");
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
            var weaponTier = WeaponCatalog.GetUnlockedTier();
            var baseDamage = 10f * GameSave.DamageMultiplier * EquipmentCatalog.CombinedDamageMultiplier()
                * WeaponCatalog.DamageMultiplier();
            if (selected == PlayerClass.Bowman) baseDamage *= 1.4f;
            else if (selected == PlayerClass.Spearman) baseDamage *= 1.15f;
            else if (selected == PlayerClass.Samurai) baseDamage *= 0.7f;
            var moveSpeed = 4.5f * GameSave.SpeedMultiplier * EquipmentCatalog.CombinedMoveSpeedMultiplier();
            var maxHp = GameSave.MaxHp + EquipmentCatalog.CombinedBonusMaxHp();
            var movementLabel = GameSave.UsesJoystickMovement ? "Joystick" : "Tap / Hold";
            var rangeMul = GameSave.AttackRangeMultiplier;

            var attackMode = GameSave.GetSelectedAttackMode(selected);
            var technique = AttackModeCatalog.GetLabel(attackMode, selected);
            var standby = GameSave.GetStandbyHero();
            var companionLine = standby.HasValue
                ? $"Companion: {GameSave.GetHeroDisplayName(standby.Value)} ({GetClassDisplayName(GameSave.GetHeroClass(standby.Value))}, 20% dmg)\n"
                : "Companion: Unlock RowZi at R20 door\n";

            _statsBodyText.text =
                "CURRENT BUILD\n" +
                $"Hero: {GameSave.GetHeroDisplayName(GameSave.SelectedHero)}\n" +
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
                $"Flame Enchant: {(GameSave.FlameEnchantUnlocked ? "Owned" : "Clear Dungeon R40")}\n" +
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
                $"Weapons: {WeaponCatalog.GetUnlockProgressSummary()}";
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
            foreach (var button in panel.GetComponentsInChildren<Button>(true))
            {
                if (button == null) continue;
                var label = button.GetComponentInChildren<Text>();
                if (label == null) continue;
                var text = label.text ?? string.Empty;

                if (text.Contains("Inside Survival"))
                {
                    var unlocked = GameSave.InsideMapUnlocked;
                    button.interactable = unlocked;
                    label.text = unlocked ? "Inside Survival" : "Inside Survival (Locked)";
                }
                else if (text.Contains("Dungeon Survival"))
                {
                    var unlocked = GameSave.DungeonMapUnlocked;
                    button.interactable = unlocked;
                    label.text = unlocked ? "Dungeon Survival" : "Dungeon Survival (Locked)";
                }
                else if (text.Contains("Crypt Survival"))
                {
                    var unlocked = GameSave.CryptMapUnlocked;
                    button.interactable = unlocked;
                    label.text = unlocked ? "Crypt Survival" : "Crypt Survival (Locked)";
                }
                else if (text.Contains("Unlimited Survival"))
                {
                    var unlocked = GameSave.UnlimitedMapUnlocked;
                    button.interactable = unlocked;
                    label.text = unlocked
                        ? "Unlimited Survival"
                        : "Unlimited Survival (Clear Crypt R50)";
                }
            }
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