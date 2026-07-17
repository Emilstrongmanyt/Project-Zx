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

        const float SafeRight = 140f;
        const float SafeTop = 36f;

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

        struct ClassPickerRefs
        {
            public Text StatusText;
            public Button BatterButton;
            public Button SpearmanButton;
            public Button BowmanButton;
            public Button MagicianButton;
        }

        ClassPickerRefs _loadoutClassPicker;
        Text _techniqueStatusText;
        Button _techniqueStandardButton;
        Button _techniqueSpecialButton;
        Button _movementJoystickButton;
        Button _movementTapHoldButton;

        struct UpgradeRowRefs
        {
            public Text Label;
            public Button BuyButton;
        }

        UpgradeRowRefs _hpRow;
        UpgradeRowRefs _damageRow;
        UpgradeRowRefs _speedRow;
        UpgradeRowRefs _whirlwindRow;
        UpgradeRowRefs _piercingShotRow;
        UpgradeRowRefs _frostTipRow;
        UpgradeRowRefs _goldMagnetRow;
        UpgradeRowRefs _thickHideRow;
        UpgradeRowRefs _secondWindRow;
        UpgradeRowRefs _campfireBlessingRow;

        void Awake()
        {
            Instance = this;
            Build();
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

            CreateUiIcon(canvasGo.transform, ArtLibrary.GoldCoin, new Vector2(-SafeRight, -SafeTop), new Vector2(34, 34), TextAnchor.UpperRight);
            _goldText = CreateText(canvasGo.transform, "0", 24, TextAnchor.UpperRight, new Vector2(-SafeRight + 72f, -SafeTop), new Vector2(100, 36));
            _goldText.alignment = TextAnchor.MiddleRight;

            _shopPanel = BuildShopPanel(canvasGo.transform);
            _loadoutPanel = BuildLoadoutPanel(canvasGo.transform);
            _statsPanel = BuildStatsPanel(canvasGo.transform);
            _achievementsPanel = BuildAchievementsPanel(canvasGo.transform);
            _mapPanel = BuildMapPanel(canvasGo.transform);
            _campfirePanel = BuildCampfirePanel(canvasGo.transform);
        }

        GameObject BuildShopPanel(Transform parent)
        {
            var panel = CreateDialogPanel(parent, "ShopPanel", Vector2.zero, new Vector2(960, 900), ArtLibrary.ShopUi);
            CreateText(panel.transform, "Wizard Shop", 32, TextAnchor.MiddleCenter, new Vector2(0, 390), new Vector2(500, 44));

            var scrollRoot = new GameObject("ShopScroll");
            scrollRoot.transform.SetParent(panel.transform, false);
            var scrollRectTransform = scrollRoot.AddComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            scrollRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRectTransform.pivot = new Vector2(0.5f, 0.5f);
            scrollRectTransform.anchoredPosition = new Vector2(0f, 40f);
            scrollRectTransform.sizeDelta = new Vector2(880f, 620f);

            var scroll = scrollRoot.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;

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

            var y = -8f;
            const float step = -56f;
            _hpRow = CreateShopUpgradeRow(content.transform, "Max HP +15", ShopCosts.HpUpgrade, y, BuyHp);
            y += step;
            _damageRow = CreateShopUpgradeRow(content.transform, "Damage +8%", ShopCosts.DamageUpgrade, y, BuyDamage);
            y += step;
            _speedRow = CreateShopUpgradeRow(content.transform, "Move Speed +6%", ShopCosts.SpeedUpgrade, y, BuySpeed);
            y += step;
            _goldMagnetRow = CreateShopUpgradeRow(content.transform, "Gold Magnet (+25% gold & loot range)", ShopCosts.GoldMagnet, y, BuyGoldMagnet);
            y += step;
            _thickHideRow = CreateShopUpgradeRow(content.transform, "Thick Hide (−15% damage taken)", ShopCosts.ThickHide, y, BuyThickHide);
            y += step;
            _secondWindRow = CreateShopUpgradeRow(content.transform, "Second Wind (heal 30% once under 20% HP)", ShopCosts.SecondWind, y, BuySecondWind);
            y += step;
            _campfireBlessingRow = CreateShopUpgradeRow(content.transform, "Campfire Blessing (free level-up at run start)", ShopCosts.CampfireBlessing, y, BuyCampfireBlessing);
            y += step;
            _whirlwindRow = CreateShopUpgradeRow(content.transform, "Whirlwind (360°/180°)", ShopCosts.Whirlwind, y, BuyWhirlwind);
            y += step;
            _piercingShotRow = CreateShopUpgradeRow(content.transform, "Piercing Shot (Bowman)", ShopCosts.PiercingShot, y, BuyPiercingShot);
            y += step;
            _frostTipRow = CreateShopUpgradeRow(content.transform, "Frost Tip (freeze zombies 0.5–1s)", ShopCosts.FrostTip, y, BuyFrostTip);

            contentRect.sizeDelta = new Vector2(0f, Mathf.Abs(y) + 64f);

            CreateButton(panel.transform, "Build Loadout", new Vector2(-195, -360), () => OpenLoadout(), large: true);
            CreateButton(panel.transform, "Character Stats", new Vector2(195, -360), () => OpenStats(), large: true);
            CreateButton(panel.transform, "Close", new Vector2(0, -420), () => panel.SetActive(false), large: true);
            panel.SetActive(false);
            return panel;
        }

        GameObject BuildAchievementsPanel(Transform parent)
        {
            var panel = CreateDialogPanel(parent, "AchievementsPanel", Vector2.zero, new Vector2(1040, 832), ArtLibrary.ChallengeBoardUi);
            CreateText(panel.transform, "Achievements", 40, TextAnchor.MiddleCenter, new Vector2(0, 364), new Vector2(650, 56));
            _achievementCountText = CreateText(panel.transform, "", 26, TextAnchor.MiddleCenter, new Vector2(0, 318), new Vector2(650, 40));

            var scrollGo = new GameObject("AchievementScroll");
            scrollGo.transform.SetParent(panel.transform, false);
            var scrollRectTransform = scrollGo.AddComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            scrollRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRectTransform.pivot = new Vector2(0.5f, 0.5f);
            scrollRectTransform.anchoredPosition = new Vector2(0f, -14f);
            scrollRectTransform.sizeDelta = new Vector2(936f, 560f);

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
            const float rowHeight = 94f;
            foreach (var def in Achievements.All)
            {
                _achievementRows.Add(CreateAchievementRow(content.transform, def, y));
                y -= rowHeight;
            }

            contentRect.sizeDelta = new Vector2(910f, Mathf.Abs(y));

            CreateButton(panel.transform, "Close", new Vector2(0, -370), () => panel.SetActive(false), large: true);
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
            rect.sizeDelta = new Vector2(884f, 84f);

            var image = go.AddComponent<Image>();
            UiSprites.ApplyButtonSprite(image, new Vector2(884f, 84f));
            go.AddComponent<Button>();

            var title = CreateText(go.transform, def.Title, 26, TextAnchor.UpperLeft, new Vector2(18f, -10f), new Vector2(832f, 36f));
            title.alignment = TextAnchor.UpperLeft;
            var desc = CreateText(go.transform, def.Description, 22, TextAnchor.UpperLeft, new Vector2(18f, -44f), new Vector2(832f, 32f));
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
            var panel = CreateDialogPanel(parent, "LoadoutPanel", Vector2.zero, new Vector2(860, 820), ArtLibrary.ShopUi);
            CreateText(panel.transform, "Build Loadout", 32, TextAnchor.MiddleCenter, new Vector2(0, 360), new Vector2(560, 48));

            // Class section
            _loadoutClassPicker = BuildClassPicker(panel.transform, 300f, 260f, 190f);

            // Technique section (clear gap under class buttons at ~190-132)
            CreateText(panel.transform, "Attack Technique", 24, TextAnchor.MiddleCenter, new Vector2(0, 70), new Vector2(560, 36));
            _techniqueStatusText = CreateText(panel.transform, "", 17, TextAnchor.MiddleCenter, new Vector2(0, 28), new Vector2(700, 48));
            _techniqueStatusText.alignment = TextAnchor.UpperCenter;
            _techniqueStandardButton = CreateButton(panel.transform, "Standard", new Vector2(-150, -40), () => SelectAttackMode(AttackMode.Standard));
            _techniqueSpecialButton = CreateButton(panel.transform, "Special", new Vector2(150, -40), SelectSpecialAttackMode);

            // Movement control (mutually exclusive)
            CreateText(panel.transform, "Movement Control", 24, TextAnchor.MiddleCenter, new Vector2(0, -120), new Vector2(560, 36));
            CreateText(panel.transform, "Only one control style is active at a time.", 16, TextAnchor.MiddleCenter, new Vector2(0, -152), new Vector2(640, 28));
            _movementJoystickButton = CreateButton(panel.transform, "Joystick", new Vector2(-150, -210), () => SelectMovementControl(MovementControlType.Joystick));
            _movementTapHoldButton = CreateButton(panel.transform, "Tap / Hold", new Vector2(150, -210), () => SelectMovementControl(MovementControlType.TapHold));

            CreateButton(panel.transform, "Back to Shop", new Vector2(-150, -320), () =>
            {
                panel.SetActive(false);
                OpenShop();
            });
            CreateButton(panel.transform, "Close", new Vector2(150, -320), () => panel.SetActive(false));
            panel.SetActive(false);
            return panel;
        }

        GameObject BuildStatsPanel(Transform parent)
        {
            var panel = CreateDialogPanel(parent, "StatsPanel", Vector2.zero, new Vector2(760, 700), ArtLibrary.ShopUi);
            CreateText(panel.transform, "Character Stats", 30, TextAnchor.MiddleCenter, new Vector2(0, 300), new Vector2(500, 44));
            _statsBodyText = CreateText(panel.transform, "", 19, TextAnchor.MiddleCenter, new Vector2(0, -10), new Vector2(680, 520));
            _statsBodyText.alignment = TextAnchor.UpperLeft;

            CreateButton(panel.transform, "Back to Shop", new Vector2(-130, -300), () =>
            {
                panel.SetActive(false);
                OpenShop();
            });
            CreateButton(panel.transform, "Close", new Vector2(130, -300), () => panel.SetActive(false));
            panel.SetActive(false);
            return panel;
        }

        GameObject BuildMapPanel(Transform parent)
        {
            var panel = CreateDialogPanel(parent, "MapPanel", Vector2.zero, new Vector2(806, 390), ArtLibrary.ChallengeBoardUi);
            CreateText(panel.transform, "Outside Survival", 36, TextAnchor.MiddleCenter, new Vector2(0, 91), new Vector2(650, 52));
            CreateText(panel.transform, "Set class & technique at the Wizard shop first.", 24, TextAnchor.MiddleCenter, new Vector2(0, 26), new Vector2(700, 62));
            CreateButton(panel.transform, "Start Run", new Vector2(0, -72), () => EnterSurvival(SurvivalMapKind.Outside), large: true);
            CreateButton(panel.transform, "Close", new Vector2(0, -162), () => panel.SetActive(false), large: true);
            panel.SetActive(false);
            return panel;
        }

        GameObject BuildCampfirePanel(Transform parent)
        {
            var panel = CreateDialogPanel(parent, "CampfirePanel", Vector2.zero, new Vector2(620, 380), ArtLibrary.ChallengeBoardUi);
            CreateText(panel.transform, "Campfire Travel", 28, TextAnchor.MiddleCenter, new Vector2(0, 120), new Vector2(500, 40));
            CreateText(panel.transform, "Set class & technique at the Wizard shop first.", 18, TextAnchor.MiddleCenter, new Vector2(0, 70), new Vector2(540, 48));
            CreateButton(panel.transform, "Outside Survival", new Vector2(0, -10), () => EnterSurvival(SurvivalMapKind.Outside));
            CreateButton(panel.transform, "Inside Survival", new Vector2(0, -75), () => EnterSurvival(SurvivalMapKind.Inside));
            CreateButton(panel.transform, "Close", new Vector2(0, -145), () => panel.SetActive(false));
            panel.SetActive(false);
            return panel;
        }

        ClassPickerRefs BuildClassPicker(Transform parent, float titleY, float statusY, float buttonY)
        {
            CreateText(parent, "Choose Class", 24, TextAnchor.MiddleCenter, new Vector2(0, titleY), new Vector2(500, 36));
            return new ClassPickerRefs
            {
                StatusText = CreateText(parent, "", 18, TextAnchor.MiddleCenter, new Vector2(0, statusY), new Vector2(640, 40)),
                BatterButton = CreateButton(parent, "Batter", new Vector2(-150, buttonY), () => SelectClass(PlayerClass.Batter)),
                SpearmanButton = CreateButton(parent, "Spearman", new Vector2(150, buttonY), () => SelectClass(PlayerClass.Spearman)),
                BowmanButton = CreateButton(parent, "Bowman", new Vector2(-150, buttonY - 70f), () => SelectClass(PlayerClass.Bowman)),
                MagicianButton = CreateButton(parent, "Magician", new Vector2(150, buttonY - 70f), () => SelectClass(PlayerClass.Magician))
            };
        }

        void SelectMovementControl(MovementControlType controlType)
        {
            GameSave.SelectedMovementControl = controlType;
            MovementJoystick.ApplyControlMode();
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
            RefreshMovementControlPicker();
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
                PlayerClass.Spearman => "Spearman — long reach, 180° whirlwind",
                PlayerClass.Bowman => "Bowman — ranged arrows, piercing upgrade",
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
            RefreshClassButton(picker.BowmanButton, PlayerClass.Bowman, GameSave.BowmanUnlocked, "Bowman (Clear R50 Inside)");
            RefreshClassButton(picker.MagicianButton, PlayerClass.Magician, GameSave.MagicianUnlocked, "Magician (Coming Soon)");
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

            GameSessionContext.SurvivalMap = mapKind;
            GameSessionContext.SelectedClass = GameSave.SelectedClass;
            GameSessionContext.SelectedHero = GameSave.SelectedHero;
            GameSessionContext.FreshSurvivalRun = true;
            GameSessionContext.CarryRound = 0;
            GameSessionContext.RunSnapshot = default;
            _shopPanel.SetActive(false);
            _loadoutPanel.SetActive(false);
            _statsPanel.SetActive(false);
            _achievementsPanel.SetActive(false);
            _mapPanel.SetActive(false);
            _campfirePanel.SetActive(false);
            GameFactory.LoadScene(GameScenes.SurvivalArena);
        }

        UpgradeRowRefs CreateUpgradeRow(Transform parent, string label, int cost, float y, Action onBuy)
        {
            return new UpgradeRowRefs
            {
                Label = CreateText(parent, $"{label} — {cost}g", 26, TextAnchor.MiddleLeft, new Vector2(-254, y), new Vector2(364, 42)),
                BuyButton = CreateButton(parent, "Buy", new Vector2(266, y), onBuy, large: true)
            };
        }

        UpgradeRowRefs CreateShopUpgradeRow(Transform parent, string label, int cost, float y, Action onBuy)
        {
            var labelText = CreateText(parent, $"{label} — {cost}g", 22, TextAnchor.MiddleLeft, new Vector2(-40f, y - 24f), new Vector2(520f, 40f));
            var labelRect = labelText.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 1f);
            labelRect.anchorMax = new Vector2(0.5f, 1f);
            labelRect.pivot = new Vector2(0.5f, 1f);
            labelRect.anchoredPosition = new Vector2(-90f, y);
            labelText.alignment = TextAnchor.MiddleLeft;

            var buyButton = CreateButton(parent, "Buy", new Vector2(300f, y - 24f), onBuy, large: true);
            var buyRect = buyButton.GetComponent<RectTransform>();
            buyRect.anchorMin = new Vector2(0.5f, 1f);
            buyRect.anchorMax = new Vector2(0.5f, 1f);
            buyRect.pivot = new Vector2(0.5f, 1f);
            buyRect.anchoredPosition = new Vector2(300f, y);
            buyRect.sizeDelta = new Vector2(200f, 48f);

            return new UpgradeRowRefs
            {
                Label = labelText,
                BuyButton = buyButton
            };
        }

        void BuyHp()
        {
            if (GameSave.IsHpUpgradeMaxed) return;
            if (GameSave.TrySpendGold(ShopCosts.HpUpgrade)) GameSave.HpUpgradeLevel++;
            RefreshGold();
            RefreshShopRows();
        }

        void BuyDamage()
        {
            if (GameSave.IsDamageUpgradeMaxed) return;
            if (GameSave.TrySpendGold(ShopCosts.DamageUpgrade)) GameSave.DamageUpgradeLevel++;
            RefreshGold();
            RefreshShopRows();
        }

        void BuySpeed()
        {
            if (GameSave.IsSpeedUpgradeMaxed) return;
            if (GameSave.TrySpendGold(ShopCosts.SpeedUpgrade)) GameSave.SpeedUpgradeLevel++;
            RefreshGold();
            RefreshShopRows();
        }

        void BuyGoldMagnet()
        {
            if (GameSave.GoldMagnetUnlocked) return;
            if (GameSave.TrySpendGold(ShopCosts.GoldMagnet)) GameSave.GoldMagnetUnlocked = true;
            RefreshGold();
            RefreshShopRows();
        }

        void BuyThickHide()
        {
            if (GameSave.ThickHideUnlocked) return;
            if (GameSave.TrySpendGold(ShopCosts.ThickHide)) GameSave.ThickHideUnlocked = true;
            RefreshGold();
            RefreshShopRows();
        }

        void BuySecondWind()
        {
            if (GameSave.SecondWindUnlocked) return;
            if (GameSave.TrySpendGold(ShopCosts.SecondWind)) GameSave.SecondWindUnlocked = true;
            RefreshGold();
            RefreshShopRows();
        }

        void BuyCampfireBlessing()
        {
            if (GameSave.CampfireBlessingUnlocked) return;
            if (GameSave.TrySpendGold(ShopCosts.CampfireBlessing)) GameSave.CampfireBlessingUnlocked = true;
            RefreshGold();
            RefreshShopRows();
        }

        void BuyFrostTip()
        {
            if (GameSave.FrostTipUnlocked) return;
            if (!GameSave.SpearmanUnlocked && !GameSave.BowmanUnlocked) return;
            if (GameSave.TrySpendGold(ShopCosts.FrostTip)) GameSave.FrostTipUnlocked = true;
            RefreshGold();
            RefreshShopRows();
            if (_loadoutPanel != null && _loadoutPanel.activeSelf) RefreshLoadoutPanel();
        }

        void RefreshShopRows()
        {
            SetUpgradeRow(_hpRow, "Max HP +15", ShopCosts.HpUpgrade, GameSave.IsHpUpgradeMaxed, $"Max HP {GameSave.MaxHp}/{StatCaps.PermanentMaxHp}");
            SetUpgradeRow(_damageRow, "Damage +8%", ShopCosts.DamageUpgrade, GameSave.IsDamageUpgradeMaxed, $"Damage x{GameSave.DamageMultiplier:0.##} (max x{StatCaps.PermanentMaxDamageMultiplier:0.#})");
            SetUpgradeRow(_speedRow, "Move Speed +6%", ShopCosts.SpeedUpgrade, GameSave.IsSpeedUpgradeMaxed, $"Speed x{GameSave.SpeedMultiplier:0.##} (max x{StatCaps.PermanentMaxSpeedMultiplier:0.#})");

            if (GameSave.GoldMagnetUnlocked)
                SetOwnedRow(_goldMagnetRow, "Gold Magnet (+25% gold & loot range)");
            else
                SetUpgradeRow(_goldMagnetRow, "Gold Magnet (+25% gold & loot range)", ShopCosts.GoldMagnet, false, string.Empty);

            if (GameSave.ThickHideUnlocked)
                SetOwnedRow(_thickHideRow, "Thick Hide (−15% damage taken)");
            else
                SetUpgradeRow(_thickHideRow, "Thick Hide (−15% damage taken)", ShopCosts.ThickHide, false, string.Empty);

            if (GameSave.SecondWindUnlocked)
                SetOwnedRow(_secondWindRow, "Second Wind (heal 30% once under 20% HP)");
            else
                SetUpgradeRow(_secondWindRow, "Second Wind (heal 30% once under 20% HP)", ShopCosts.SecondWind, false, string.Empty);

            if (GameSave.CampfireBlessingUnlocked)
                SetOwnedRow(_campfireBlessingRow, "Campfire Blessing (free level-up at run start)");
            else
                SetUpgradeRow(_campfireBlessingRow, "Campfire Blessing (free level-up at run start)", ShopCosts.CampfireBlessing, false, string.Empty);

            if (GameSave.WhirlwindUnlocked)
                SetOwnedRow(_whirlwindRow, "Whirlwind (360°/180°)");
            else
                SetUpgradeRow(_whirlwindRow, "Whirlwind (360°/180°)", ShopCosts.Whirlwind, false, string.Empty);

            if (GameSave.PiercingShotUnlocked)
                SetOwnedRow(_piercingShotRow, "Piercing Shot (Bowman)");
            else if (!GameSave.BowmanUnlocked)
                SetLockedRow(_piercingShotRow, "Piercing Shot (Bowman)", "Unlock Bowman first");
            else
                SetUpgradeRow(_piercingShotRow, "Piercing Shot (Bowman)", ShopCosts.PiercingShot, false, string.Empty);

            if (GameSave.FrostTipUnlocked)
                SetOwnedRow(_frostTipRow, "Frost Tip (freeze zombies 0.5–1s)");
            else if (!GameSave.SpearmanUnlocked && !GameSave.BowmanUnlocked)
                SetLockedRow(_frostTipRow, "Frost Tip (freeze zombies 0.5–1s)", "Unlock Spearman or Bowman");
            else
                SetUpgradeRow(_frostTipRow, "Frost Tip (freeze zombies 0.5–1s)", ShopCosts.FrostTip, false, string.Empty);
        }

        static void SetLockedRow(UpgradeRowRefs row, string label, string reason)
        {
            if (row.Label != null)
                row.Label.text = $"{label} — {reason}";

            if (row.BuyButton != null)
            {
                row.BuyButton.interactable = false;
                var image = row.BuyButton.GetComponent<Image>();
                if (image != null)
                {
                    UiSprites.ApplyButtonSprite(image, new Vector2(220f, 52f));
                    image.color = new Color(0.32f, 0.34f, 0.38f, 0.75f);
                }

                var buyLabel = row.BuyButton.GetComponentInChildren<Text>();
                if (buyLabel != null)
                {
                    buyLabel.text = "Locked";
                    buyLabel.color = new Color(0.72f, 0.74f, 0.78f);
                }
            }
        }

        static void SetUpgradeRow(UpgradeRowRefs row, string label, int cost, bool maxed, string maxLabel)
        {
            if (row.Label != null)
                row.Label.text = maxed ? maxLabel : $"{label} — {cost}g";

            if (row.BuyButton != null)
            {
                row.BuyButton.interactable = !maxed;
                var image = row.BuyButton.GetComponent<Image>();
                if (image != null)
                {
                    UiSprites.ApplyButtonSprite(image, new Vector2(220f, 52f));
                    image.color = Color.white;
                }

                var buyLabel = row.BuyButton.GetComponentInChildren<Text>();
                if (buyLabel != null)
                {
                    buyLabel.text = maxed ? "MAX" : "Buy";
                    buyLabel.color = Color.white;
                }
            }
        }

        static void SetOwnedRow(UpgradeRowRefs row, string label)
        {
            if (row.Label != null)
                row.Label.text = $"{label} — Owned";

            if (row.BuyButton != null)
            {
                row.BuyButton.interactable = false;
                var image = row.BuyButton.GetComponent<Image>();
                if (image != null)
                {
                    UiSprites.ApplyButtonSprite(image, new Vector2(220f, 52f));
                    image.color = new Color(0.42f, 0.44f, 0.48f, 0.88f);
                }

                var buyLabel = row.BuyButton.GetComponentInChildren<Text>();
                if (buyLabel != null)
                {
                    buyLabel.text = "Owned";
                    buyLabel.color = new Color(0.82f, 0.84f, 0.88f);
                }
            }
        }

        void BuyWhirlwind()
        {
            if (GameSave.WhirlwindUnlocked) return;
            if (GameSave.TrySpendGold(ShopCosts.Whirlwind)) GameSave.WhirlwindUnlocked = true;
            RefreshGold();
            RefreshShopRows();
            if (_loadoutPanel != null && _loadoutPanel.activeSelf) RefreshLoadoutPanel();
        }

        void BuyPiercingShot()
        {
            if (GameSave.PiercingShotUnlocked || !GameSave.BowmanUnlocked) return;
            if (GameSave.TrySpendGold(ShopCosts.PiercingShot)) GameSave.PiercingShotUnlocked = true;
            RefreshGold();
            RefreshShopRows();
            if (_loadoutPanel != null && _loadoutPanel.activeSelf) RefreshLoadoutPanel();
        }

        public void OpenShop()
        {
            RefreshGold();
            RefreshShopRows();
            _loadoutPanel.SetActive(false);
            _statsPanel.SetActive(false);
            _achievementsPanel.SetActive(false);
            _shopPanel.SetActive(true);
        }

        public void OpenLoadout()
        {
            RefreshLoadoutPanel();
            _shopPanel.SetActive(false);
            _statsPanel.SetActive(false);
            _achievementsPanel.SetActive(false);
            _loadoutPanel.SetActive(true);
        }

        public void OpenStats()
        {
            RefreshStats();
            _shopPanel.SetActive(false);
            _loadoutPanel.SetActive(false);
            _achievementsPanel.SetActive(false);
            _statsPanel.SetActive(true);
        }

        public void OpenAchievements()
        {
            RefreshAchievements();
            _shopPanel.SetActive(false);
            _loadoutPanel.SetActive(false);
            _statsPanel.SetActive(false);
            _achievementsPanel.SetActive(true);
        }

        void RefreshAchievements()
        {
            if (_achievementCountText != null)
                _achievementCountText.text = $"Unlocked {Achievements.UnlockedCount}/{Achievements.All.Count}";

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
            var baseDamage = 10f * GameSave.DamageMultiplier;
            if (selected == PlayerClass.Bowman) baseDamage *= 0.9f;
            var moveSpeed = 4.5f * GameSave.SpeedMultiplier;
            var movementLabel = GameSave.UsesJoystickMovement ? "Joystick" : "Tap / Hold";

            var attackMode = GameSave.GetSelectedAttackMode(selected);
            var technique = AttackModeCatalog.GetLabel(attackMode, selected);

            _statsBodyText.text =
                "CURRENT BUILD\n" +
                $"Hero: {GameSave.GetHeroDisplayName(GameSave.SelectedHero)}\n" +
                $"Class: {className}\n" +
                $"Technique: {technique}\n" +
                $"Movement: {movementLabel}\n" +
                $"Max HP: {GameSave.MaxHp}\n" +
                $"Base Damage: {baseDamage:0.#}\n" +
                $"Move Speed: {moveSpeed:0.##}\n" +
                $"HP Upgrades: {GameSave.HpUpgradeLevel}   Damage: {GameSave.DamageUpgradeLevel}   Speed: {GameSave.SpeedUpgradeLevel}\n" +
                $"Whirlwind: {(GameSave.WhirlwindUnlocked ? "Owned" : "Locked")}\n" +
                $"Piercing Shot: {(GameSave.PiercingShotUnlocked ? "Owned" : "Locked")}\n" +
                $"Frost Tip: {(GameSave.FrostTipUnlocked ? "Owned" : "Locked")}\n" +
                $"Gold Magnet: {(GameSave.GoldMagnetUnlocked ? "Owned" : "Locked")}\n" +
                $"Thick Hide: {(GameSave.ThickHideUnlocked ? "Owned" : "Locked")}\n" +
                $"Second Wind: {(GameSave.SecondWindUnlocked ? "Owned" : "Locked")}\n" +
                $"Campfire Blessing: {(GameSave.CampfireBlessingUnlocked ? "Owned" : "Locked")}\n" +
                $"Spearman: {(GameSave.SpearmanUnlocked ? "Unlocked" : "Locked")}\n" +
                $"Bowman: {(GameSave.BowmanUnlocked ? "Unlocked" : "Locked")}\n" +
                $"Magician: {(GameSave.MagicianUnlocked ? "Unlocked" : "Coming Soon")}\n" +
                $"RowZi: {(GameSave.RowZiUnlocked ? "Unlocked" : "Meet at R20 door")}\n\n" +
                "LIFETIME RECORDS\n" +
                $"Zombie Kills: {GameSave.LifetimeZombieKills}\n" +
                $"Boss Kills: {GameSave.LifetimeBossKills}\n" +
                $"Deaths: {GameSave.LifetimeDeaths}\n" +
                $"Gold Earned: {GameSave.LifetimeGoldEarned}\n" +
                $"Highest Round: {GameSave.HighestRoundReached}";
        }

        public void OpenMapSelect()
        {
            _mapPanel.SetActive(true);
        }

        public void OpenCampfireTravel()
        {
            if (!GameSave.InsideMapUnlocked)
            {
                OpenMapSelect();
                return;
            }

            _campfirePanel.SetActive(true);
        }

        public void RefreshGold()
        {
            if (_goldText != null) _goldText.text = GameSave.Gold.ToString();
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
            var image = go.AddComponent<Image>();
            if (background != null)
            {
                image.sprite = background;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }
            else
            {
                image.color = new Color(0.05f, 0.08f, 0.12f, 0.92f);
            }
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
            var size = large ? new Vector2(286, 68) : new Vector2(220, 52);
            rect.sizeDelta = size;
            var image = go.AddComponent<Image>();
            UiSprites.ApplyButtonSprite(image, size);
            var button = go.AddComponent<Button>();
            button.onClick.AddListener(() => onClick());
            var fontSize = large ? 28 : 22;
            CreateText(go.transform, label, fontSize, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(size.x - 20f, size.y - 8f));
            return button;
        }
    }
}