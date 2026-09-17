using System;
using ProjectZx.Core;
using ProjectZx.HeroEditor;
using ProjectZx.Player;
using ProjectZx.World;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectZx.UI
{
    /// <summary>
    /// Mira shop + Build Loadout panel for <see cref="HubUi"/>.
    /// Behavior-unchanged extract (PR-03) - keeps HubUi.cs from growing further.
    /// </summary>
    public partial class HubUi
    {
        GameObject _shopPanel;
        GameObject _loadoutPanel;

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
        GameObject _shopInfoOverlay;
        Text _shopInfoTitle;
        Text _shopInfoBody;

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
            RefreshClassButton(picker.SpearmanButton, PlayerClass.Spearman, GameSave.SpearmanUnlocked, "Spearman — Emberwilds R20 boss");
            RefreshClassButton(picker.BowmanButton, PlayerClass.Bowman, GameSave.BowmanUnlocked, "Bowman — Warded Halls R30 clear");
            RefreshClassButton(picker.SamuraiButton, PlayerClass.Samurai, GameSave.SamuraiUnlocked, "Samurai — Ironvault R40 boss");
            RefreshClassButton(picker.MagicianButton, PlayerClass.Magician, GameSave.MagicianUnlocked, "Magician — Endless Front R80");
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
                        "II: −30% damage taken (requires Warded Halls clear)\n" +
                        "III: −45% damage taken (requires Ironvault clear)\n\n" +
                        $"Current: T{level} ({dr}% DR)\n" +
                        (level >= 3
                            ? "Status: MAXED"
                            : level == 1 && !GameSave.InsideSurvivalCleared
                                ? "Next: Locked — clear Warded Halls Survival"
                                : level == 2 && !GameSave.DungeonSurvivalCleared
                                    ? "Next: Locked — clear Ironvault Survival"
                                    : $"Next rank: {ToRoman(level + 1)}  ·  Cost: {ShopCosts.NextThickHideCost}g");
                }

                case ShopUpgradeKind.SecondWind:
                {
                    var level = GameSave.SecondWindLevel;
                    return
                        "Auto-heal when you drop to 20% HP or below.\n\n" +
                        "I: heal 30% Max HP once per run\n" +
                        "II: two uses per run (requires Warded Halls clear)\n\n" +
                        $"Current charges/run: {GameSave.SecondWindMaxCharges}\n" +
                        (level >= 2
                            ? "Status: MAXED"
                            : level == 1 && !GameSave.InsideSurvivalCleared
                                ? "Next: Locked — clear Warded Halls Survival"
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
    }
}