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
        GameObject _equipmentPanel;
        Text _equipmentStatusText;
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

            CreateUiIcon(canvasGo.transform, ArtLibrary.GoldCoin, new Vector2(-SafeRight, -SafeTop), new Vector2(40, 40), TextAnchor.UpperRight);
            _goldText = CreateText(canvasGo.transform, "0", 30, TextAnchor.UpperRight, new Vector2(-SafeRight + 80f, -SafeTop), new Vector2(120, 42));
            _goldText.alignment = TextAnchor.MiddleRight;

            _shopPanel = BuildShopPanel(canvasGo.transform);
            _loadoutPanel = BuildLoadoutPanel(canvasGo.transform);
            _statsPanel = BuildStatsPanel(canvasGo.transform);
            _achievementsPanel = BuildAchievementsPanel(canvasGo.transform);
            _mapPanel = BuildMapPanel(canvasGo.transform);
            _campfirePanel = BuildCampfirePanel(canvasGo.transform);
            _equipmentPanel = BuildEquipmentPanel(canvasGo.transform);
        }

        GameObject BuildShopPanel(Transform parent)
        {
            var panel = CreateDialogPanel(parent, "ShopPanel", Vector2.zero, new Vector2(1100, 980), ArtLibrary.ShopUi);
            CreateText(panel.transform, "Wizard Shop", 40, TextAnchor.MiddleCenter, new Vector2(0, 430), new Vector2(620, 52));

            var scrollRoot = new GameObject("ShopScroll");
            scrollRoot.transform.SetParent(panel.transform, false);
            var scrollRectTransform = scrollRoot.AddComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            scrollRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRectTransform.pivot = new Vector2(0.5f, 0.5f);
            scrollRectTransform.anchoredPosition = new Vector2(0f, 36f);
            scrollRectTransform.sizeDelta = new Vector2(1000f, 680f);

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
            const float step = -68f;
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
            _whirlwindRow = CreateShopUpgradeRow(content.transform, "Whirlwind (360°)", ShopCosts.Whirlwind, y, BuyWhirlwind);
            y += step;
            _piercingShotRow = CreateShopUpgradeRow(content.transform, "Piercing Shot (Bowman)", ShopCosts.PiercingShot, y, BuyPiercingShot);
            y += step;
            _frostTipRow = CreateShopUpgradeRow(content.transform, "Frost Tip (freeze zombies 0.5–1s)", ShopCosts.FrostTip, y, BuyFrostTip);

            contentRect.sizeDelta = new Vector2(0f, Mathf.Abs(y) + 80f);

            CreateButton(panel.transform, "Build Loadout", new Vector2(-220, -400), () => OpenLoadout(), large: true);
            CreateButton(panel.transform, "Character Stats", new Vector2(220, -400), () => OpenStats(), large: true);
            CreateButton(panel.transform, "Close", new Vector2(0, -470), () => panel.SetActive(false), large: true);
            panel.SetActive(false);
            return panel;
        }

        GameObject BuildAchievementsPanel(Transform parent)
        {
            var panel = CreateDialogPanel(parent, "AchievementsPanel", Vector2.zero, new Vector2(1100, 880), ArtLibrary.ChallengeBoardUi);
            CreateText(panel.transform, "Achievements", 44, TextAnchor.MiddleCenter, new Vector2(0, 380), new Vector2(700, 58));
            _achievementCountText = CreateText(panel.transform, "", 30, TextAnchor.MiddleCenter, new Vector2(0, 330), new Vector2(700, 44));

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
            var panel = CreateDialogPanel(parent, "LoadoutPanel", Vector2.zero, new Vector2(960, 980), ArtLibrary.ShopUi);
            CreateText(panel.transform, "Build Loadout", 38, TextAnchor.MiddleCenter, new Vector2(0, 430), new Vector2(620, 52));
            CreateText(panel.transform, "Class is saved per hero. Swap heroes at camp to set the companion build.", 18, TextAnchor.MiddleCenter, new Vector2(0, 388), new Vector2(820, 36));

            // Class section (3 rows: Batter/Spearman, Bowman/Samurai, Magician)
            _loadoutClassPicker = BuildClassPicker(panel.transform, 340f, 295f, 220f);

            // Technique section under Magician row (~220-152 = 68)
            CreateText(panel.transform, "Attack Technique", 28, TextAnchor.MiddleCenter, new Vector2(0, 10), new Vector2(620, 40));
            _techniqueStatusText = CreateText(panel.transform, "", 20, TextAnchor.MiddleCenter, new Vector2(0, -36), new Vector2(780, 52));
            _techniqueStatusText.alignment = TextAnchor.UpperCenter;
            _techniqueStandardButton = CreateButton(panel.transform, "Standard", new Vector2(-160, -110), () => SelectAttackMode(AttackMode.Standard));
            _techniqueSpecialButton = CreateButton(panel.transform, "Special", new Vector2(160, -110), SelectSpecialAttackMode);

            // Movement control (mutually exclusive)
            CreateText(panel.transform, "Movement Control", 28, TextAnchor.MiddleCenter, new Vector2(0, -200), new Vector2(620, 40));
            CreateText(panel.transform, "Only one control style is active at a time.", 20, TextAnchor.MiddleCenter, new Vector2(0, -236), new Vector2(700, 32));
            _movementJoystickButton = CreateButton(panel.transform, "Joystick", new Vector2(-160, -300), () => SelectMovementControl(MovementControlType.Joystick));
            _movementTapHoldButton = CreateButton(panel.transform, "Tap / Hold", new Vector2(160, -300), () => SelectMovementControl(MovementControlType.TapHold));

            CreateButton(panel.transform, "Back to Shop", new Vector2(-160, -400), () =>
            {
                panel.SetActive(false);
                OpenShop();
            });
            CreateButton(panel.transform, "Close", new Vector2(160, -400), () => panel.SetActive(false));
            panel.SetActive(false);
            return panel;
        }

        GameObject BuildStatsPanel(Transform parent)
        {
            var panel = CreateDialogPanel(parent, "StatsPanel", Vector2.zero, new Vector2(860, 780), ArtLibrary.ShopUi);
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
            var panel = CreateDialogPanel(parent, "MapPanel", Vector2.zero, new Vector2(860, 560), ArtLibrary.ChallengeBoardUi);
            CreateText(panel.transform, "Survival Challenge", 40, TextAnchor.MiddleCenter, new Vector2(0, 200), new Vector2(700, 56));
            CreateText(panel.transform, "Set class & technique at the Wizard shop first.\nUnlocked maps start fresh at round 1.", 24, TextAnchor.MiddleCenter, new Vector2(0, 120), new Vector2(760, 72));
            CreateButton(panel.transform, "Outside Survival", new Vector2(0, 30), () => EnterSurvival(SurvivalMapKind.Outside), large: true);
            CreateButton(panel.transform, "Inside Survival", new Vector2(0, -50), () => EnterSurvival(SurvivalMapKind.Inside), large: true);
            CreateButton(panel.transform, "Dungeon Survival", new Vector2(0, -130), () => EnterSurvival(SurvivalMapKind.Dungeon), large: true);
            CreateButton(panel.transform, "Close", new Vector2(0, -220), () => panel.SetActive(false), large: true);
            panel.SetActive(false);
            return panel;
        }

        GameObject BuildCampfirePanel(Transform parent)
        {
            var panel = CreateDialogPanel(parent, "CampfirePanel", Vector2.zero, new Vector2(760, 560), ArtLibrary.ChallengeBoardUi);
            CreateText(panel.transform, "Campfire Travel", 34, TextAnchor.MiddleCenter, new Vector2(0, 200), new Vector2(640, 48));
            CreateText(panel.transform, "Choose an unlocked map. Each run starts at round 1.", 20, TextAnchor.MiddleCenter, new Vector2(0, 140), new Vector2(680, 48));
            CreateButton(panel.transform, "Outside Survival", new Vector2(0, 50), () => EnterSurvival(SurvivalMapKind.Outside));
            CreateButton(panel.transform, "Inside Survival", new Vector2(0, -30), () => EnterSurvival(SurvivalMapKind.Inside));
            CreateButton(panel.transform, "Dungeon Survival", new Vector2(0, -110), () => EnterSurvival(SurvivalMapKind.Dungeon));
            CreateButton(panel.transform, "Close", new Vector2(0, -200), () => panel.SetActive(false));
            panel.SetActive(false);
            return panel;
        }

        GameObject BuildEquipmentPanel(Transform parent)
        {
            var panel = CreateDialogPanel(parent, "EquipmentPanel", Vector2.zero, new Vector2(980, 820), ArtLibrary.ShopUi);
            CreateText(panel.transform, "Treasure Chest", 38, TextAnchor.MiddleCenter, new Vector2(0, 350), new Vector2(700, 48));
            CreateText(panel.transform, "Equip 1 ring and 1 necklace. Drops unlock here after you find them.", 20, TextAnchor.MiddleCenter, new Vector2(0, 300), new Vector2(860, 40));
            _equipmentStatusText = CreateText(panel.transform, "", 22, TextAnchor.MiddleCenter, new Vector2(0, 250), new Vector2(860, 48));

            CreateText(panel.transform, "Rings", 28, TextAnchor.MiddleCenter, new Vector2(0, 190), new Vector2(400, 36));
            CreateText(panel.transform, "Necklaces", 28, TextAnchor.MiddleCenter, new Vector2(0, -10), new Vector2(400, 36));

            _equipmentButtons.Clear();
            var ringX = new[] { -280f, 0f, 280f };
            var ringIndex = 0;
            var neckX = new[] { -280f, 0f, 280f };
            var neckIndex = 0;

            // Unequip slots first.
            _equipmentButtons.Add(CreateButton(panel.transform, "No Ring", new Vector2(ringX[ringIndex++], 120f), () =>
            {
                GameSave.UnequipSlot(EquipmentSlot.Ring);
                RefreshEquipmentPanel();
            }));
            _equipmentButtons.Add(CreateButton(panel.transform, "No Necklace", new Vector2(neckX[neckIndex++], -80f), () =>
            {
                GameSave.UnequipSlot(EquipmentSlot.Necklace);
                RefreshEquipmentPanel();
            }));

            foreach (var def in EquipmentCatalog.All)
            {
                var id = def.Id;
                if (def.Slot == EquipmentSlot.Ring)
                {
                    var x = ringIndex < ringX.Length ? ringX[ringIndex++] : 0f;
                    _equipmentButtons.Add(CreateButton(panel.transform, def.DisplayName, new Vector2(x, 120f), () => SelectEquipment(id)));
                }
                else
                {
                    var x = neckIndex < neckX.Length ? neckX[neckIndex++] : 0f;
                    _equipmentButtons.Add(CreateButton(panel.transform, def.DisplayName, new Vector2(x, -80f), () => SelectEquipment(id)));
                }
            }

            CreateButton(panel.transform, "Close", new Vector2(0, -300), () => panel.SetActive(false), large: true);
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
                var ringName = ring.Id != EquipmentId.None ? ring.DisplayName : "None";
                var neckName = neck.Id != EquipmentId.None ? neck.DisplayName : "None";
                _equipmentStatusText.text = $"Equipped: {ringName}  ·  {neckName}";
            }

            // Button order: No Ring, No Necklace, then catalog All in order.
            var buttonIndex = 0;
            RefreshEquipButton(GetEquipButton(buttonIndex++), EquipmentId.None, EquipmentSlot.Ring, "No Ring");
            RefreshEquipButton(GetEquipButton(buttonIndex++), EquipmentId.None, EquipmentSlot.Necklace, "No Necklace");

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
                ? (slot == EquipmentSlot.Ring ? GameSave.EquippedRing == EquipmentId.None : GameSave.EquippedNecklace == EquipmentId.None)
                : (slot == EquipmentSlot.Ring ? GameSave.EquippedRing == id : GameSave.EquippedNecklace == id);

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
            RefreshClassButton(picker.BowmanButton, PlayerClass.Bowman, GameSave.BowmanUnlocked, "Bowman (Clear R50 Inside)");
            RefreshClassButton(picker.SamuraiButton, PlayerClass.Samurai, GameSave.SamuraiUnlocked, "Samurai (Dungeon R40 Boss)");
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
            if (mapKind == SurvivalMapKind.Dungeon && !GameSave.DungeonMapUnlocked) return;

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

        UpgradeRowRefs CreateShopUpgradeRow(Transform parent, string label, int cost, float y, Action onBuy)
        {
            var labelText = CreateText(parent, $"{label} — {cost}g", 26, TextAnchor.MiddleLeft, new Vector2(-40f, y - 28f), new Vector2(620f, 52f));
            var labelRect = labelText.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 1f);
            labelRect.anchorMax = new Vector2(0.5f, 1f);
            labelRect.pivot = new Vector2(0.5f, 1f);
            labelRect.anchoredPosition = new Vector2(-100f, y);
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.horizontalOverflow = HorizontalWrapMode.Wrap;
            labelText.verticalOverflow = VerticalWrapMode.Truncate;

            var buyButton = CreateButton(parent, "Buy", new Vector2(340f, y - 28f), onBuy, large: true);
            var buyRect = buyButton.GetComponent<RectTransform>();
            buyRect.anchorMin = new Vector2(0.5f, 1f);
            buyRect.anchorMax = new Vector2(0.5f, 1f);
            buyRect.pivot = new Vector2(0.5f, 1f);
            buyRect.anchoredPosition = new Vector2(340f, y);
            buyRect.sizeDelta = new Vector2(220f, 56f);

            return new UpgradeRowRefs
            {
                Label = labelText,
                BuyButton = buyButton
            };
        }

        void BuyHp()
        {
            if (GameSave.IsHpUpgradeMaxed) return;
            if (!GameSave.TrySpendGold(ShopCosts.HpUpgrade)) return;
            GameSave.HpUpgradeLevel++;
            OnShopUpgradePurchased();
        }

        void BuyDamage()
        {
            if (GameSave.IsDamageUpgradeMaxed) return;
            if (!GameSave.TrySpendGold(ShopCosts.DamageUpgrade)) return;
            GameSave.DamageUpgradeLevel++;
            OnShopUpgradePurchased();
        }

        void BuySpeed()
        {
            if (GameSave.IsSpeedUpgradeMaxed) return;
            if (!GameSave.TrySpendGold(ShopCosts.SpeedUpgrade)) return;
            GameSave.SpeedUpgradeLevel++;
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
            if (GameSave.ThickHideUnlocked) return;
            if (!GameSave.TrySpendGold(ShopCosts.ThickHide)) return;
            GameSave.ThickHideUnlocked = true;
            OnShopUpgradePurchased();
        }

        void BuySecondWind()
        {
            if (GameSave.SecondWindUnlocked) return;
            if (!GameSave.TrySpendGold(ShopCosts.SecondWind)) return;
            GameSave.SecondWindUnlocked = true;
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
                SetOwnedRow(_whirlwindRow, "Whirlwind (360°)");
            else
                SetUpgradeRow(_whirlwindRow, "Whirlwind (360°)", ShopCosts.Whirlwind, false, string.Empty);

            if (GameSave.PiercingShotUnlocked)
                SetOwnedRow(_piercingShotRow, "Piercing Shot (Bowman)");
            else if (!GameSave.BowmanUnlocked)
                SetLockedRow(_piercingShotRow, "Piercing Shot (Bowman)", "Unlock Bowman first");
            else
                SetUpgradeRow(_piercingShotRow, "Piercing Shot (Bowman)", ShopCosts.PiercingShot, false, string.Empty);

            if (GameSave.FrostTipUnlocked)
                SetOwnedRow(_frostTipRow, "Frost Tip (freeze zombies 0.5–1s)");
            else if (!GameSave.SpearmanUnlocked && !GameSave.BowmanUnlocked && !GameSave.SamuraiUnlocked)
                SetLockedRow(_frostTipRow, "Frost Tip (freeze zombies 0.5–1s)", "Unlock Spearman, Bowman, or Samurai");
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
                    UiSprites.ApplyButtonSprite(image, new Vector2(240f, 56f));
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
        }

        void BuyWhirlwind()
        {
            if (GameSave.WhirlwindUnlocked) return;
            if (!GameSave.TrySpendGold(ShopCosts.Whirlwind)) return;
            GameSave.WhirlwindUnlocked = true;
            OnShopUpgradePurchased();
            if (_loadoutPanel != null && _loadoutPanel.activeSelf) RefreshLoadoutPanel();
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
            RefreshAchievements();
            CloseAllHubPanels();
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
            var baseDamage = 10f * GameSave.DamageMultiplier * EquipmentCatalog.CombinedDamageMultiplier();
            if (selected == PlayerClass.Bowman) baseDamage *= 1.26f;
            else if (selected == PlayerClass.Spearman) baseDamage *= 1.15f;
            else if (selected == PlayerClass.Samurai) baseDamage *= 0.7f;
            var moveSpeed = 4.5f * GameSave.SpeedMultiplier;
            var maxHp = GameSave.MaxHp + EquipmentCatalog.CombinedBonusMaxHp();
            var movementLabel = GameSave.UsesJoystickMovement ? "Joystick" : "Tap / Hold";

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
                $"Max HP: {maxHp}\n" +
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
                $"Ring: {EquipName(GameSave.EquippedRing)}\n" +
                $"Necklace: {EquipName(GameSave.EquippedNecklace)}\n" +
                $"Spearman: {(GameSave.SpearmanUnlocked ? "Unlocked" : "Locked")}\n" +
                $"Bowman: {(GameSave.BowmanUnlocked ? "Unlocked" : "Locked")}\n" +
                $"Samurai: {(GameSave.SamuraiUnlocked ? "Unlocked" : "Locked")}\n" +
                $"Magician: {(GameSave.MagicianUnlocked ? "Unlocked" : "Coming Soon")}\n" +
                $"RowZi: {(GameSave.RowZiUnlocked ? "Unlocked" : "Meet at R20 door")}\n\n" +
                "LIFETIME RECORDS\n" +
                $"Zombie Kills: {GameSave.LifetimeZombieKills}\n" +
                $"Boss Kills: {GameSave.LifetimeBossKills}\n" +
                $"Deaths: {GameSave.LifetimeDeaths}\n" +
                $"Gold Earned: {GameSave.LifetimeGoldEarned}\n" +
                $"Highest Round: {GameSave.HighestRoundReached}";
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
            }
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
            var size = large ? new Vector2(300, 72) : new Vector2(240, 58);
            rect.sizeDelta = size;
            var image = go.AddComponent<Image>();
            UiSprites.ApplyButtonSprite(image, size);
            var button = go.AddComponent<Button>();
            button.onClick.AddListener(() => onClick());
            var fontSize = large ? 30 : 24;
            var labelText = CreateText(go.transform, label, fontSize, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(size.x - 24f, size.y - 10f));
            labelText.horizontalOverflow = HorizontalWrapMode.Wrap;
            labelText.verticalOverflow = VerticalWrapMode.Truncate;
            return button;
        }
    }
}