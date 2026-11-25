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
            public Button PaladinButton;
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
            FrostTip,
            Shattering
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
        UpgradeRowRefs _shatteringRow;
        UpgradeRowRefs _goldMagnetRow;
        UpgradeRowRefs _thickHideRow;
        UpgradeRowRefs _secondWindRow;
        UpgradeRowRefs _campfireBlessingRow;
        GameObject _shopInfoPanel;
        Text _shopInfoTitle;
        Text _shopInfoBody;
        Text _shopBanner;
        Text _loadoutBanner;
        Text _altairRelicLine;
        ScrollRect _shopScroll;

        const float ShopInfoWidth = 140f;
        const float ShopBuyWidth = 220f;
        const float ShopRowHeight = 76f;
        const float BuyLabelWidth = 178f;
        const string GoalPlaceholder = "Next step appears here.";

        GameObject BuildShopPanel(Transform parent)
        {
            var panel = CreateDialogPanel(parent, "ShopPanel", Vector2.zero, HubMenuPanelSize, ArtLibrary.ShopUi);
            CreateText(panel.transform, "Upgrade Shop", HubLayout.TitleFont, TextAnchor.MiddleCenter, new Vector2(0, 316), new Vector2(700, 48));
            _shopBanner = CreateGoalBanner(panel.transform, new Vector2(0f, 248f));

            // Viewport 768×400 at (-20, -20) spans y = -220 to 180. Scrollbar sits in the right 20 px of the 808 band.
            _shopScroll = HubLayout.CreateVerticalScroll(panel.transform, new Vector2(-20f, -20f), new Vector2(768f, 400f), 400f);
            var scrollbar = CreateSideScrollbar(panel.transform, new Vector2(394f, -20f), 400f);
            _shopScroll.verticalScrollbar = scrollbar;
            _shopScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            var content = _shopScroll.content;
            var rows = content.gameObject.AddComponent<VerticalLayoutGroup>();
            rows.spacing = 8f;
            rows.padding = new RectOffset(0, 0, 4, 8);
            rows.childAlignment = TextAnchor.UpperCenter;
            rows.childControlWidth = true;
            rows.childControlHeight = true;
            rows.childForceExpandWidth = true;
            rows.childForceExpandHeight = false;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _hpRow = CreateShopUpgradeRow(content, ShopUpgradeKind.MaxHp, BuyHp);
            _damageRow = CreateShopUpgradeRow(content, ShopUpgradeKind.Damage, BuyDamage);
            _speedRow = CreateShopUpgradeRow(content, ShopUpgradeKind.Speed, BuySpeed);
            _rangeRow = CreateShopUpgradeRow(content, ShopUpgradeKind.Range, BuyRange);
            _goldMagnetRow = CreateShopUpgradeRow(content, ShopUpgradeKind.GoldMagnet, BuyGoldMagnet);
            _thickHideRow = CreateShopUpgradeRow(content, ShopUpgradeKind.ThickHide, BuyThickHide);
            _secondWindRow = CreateShopUpgradeRow(content, ShopUpgradeKind.SecondWind, BuySecondWind);
            _campfireBlessingRow = CreateShopUpgradeRow(content, ShopUpgradeKind.CampfireBlessing, BuyCampfireBlessing);
            _whirlwindRow = CreateShopUpgradeRow(content, ShopUpgradeKind.Whirlwind, BuyWhirlwind);
            _piercingShotRow = CreateShopUpgradeRow(content, ShopUpgradeKind.PiercingShot, BuyPiercingShot);
            _frostTipRow = CreateShopUpgradeRow(content, ShopUpgradeKind.FrostTip, BuyFrostTip);
            _shatteringRow = CreateShopUpgradeRow(content, ShopUpgradeKind.Shattering, BuyShattering);

            var footerY = HubLayout.FooterCenterY;
            var footerSize = new Vector2(248f, 72f);
            CreateButton(panel.transform, "Build Loadout", new Vector2(-260f, footerY), () => OpenLoadout(), large: true, size: footerSize);
            CreateButton(panel.transform, "Character Stats", new Vector2(0f, footerY), () => OpenStats(), large: true, size: footerSize);
            CreateButton(panel.transform, "Close", new Vector2(260f, footerY), () =>
            {
                if (_shopInfoPanel != null) _shopInfoPanel.SetActive(false);
                panel.SetActive(false);
            }, large: true, size: footerSize);

            BuildShopInfoOverlay(panel.transform);
            panel.SetActive(false);
            return panel;
        }

        void BuildShopInfoOverlay(Transform shopPanel)
        {
            // Child of the shop panel. This overlay is not its own canvas.
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

            _shopInfoTitle = CreateText(card.transform, "Upgrade Info", 34, TextAnchor.MiddleCenter, new Vector2(0, 240), new Vector2(700, 44));
            var tapHint = CreateText(card.transform, "Tap Info for full effects & current totals.", 18, TextAnchor.MiddleCenter, new Vector2(0, 198), new Vector2(680, 28));
            tapHint.color = new Color(0.8f, 0.82f, 0.86f);
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
            CreateText(panel.transform, "Build Loadout", HubLayout.TitleFont, TextAnchor.MiddleCenter, new Vector2(0, 316), new Vector2(700, 48));

            var scroll = HubLayout.CreateVerticalScroll(
                panel.transform,
                new Vector2(0f, 20f),
                new Vector2(768f, 520f),
                520f);
            var body = scroll.content;
            var column = body.gameObject.AddComponent<VerticalLayoutGroup>();
            column.spacing = 16f;
            column.padding = new RectOffset(2, 2, 8, 16);
            column.childAlignment = TextAnchor.UpperCenter;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = false;
            column.childForceExpandHeight = false;
            var fitter = body.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _loadoutBanner = CreateGoalBanner(body, Vector2.zero);
            CreateLayoutText(body, "Your class & technique apply to you and RowZi (she copies this loadout).\nMovement & audio live in Settings.", 18, 760f, 48f, TextAnchor.MiddleCenter);

            var classBlock = CreateStack(body, 8f, 760f);
            CreateLayoutText(classBlock, "Choose Class", 26, 760f, 36f, TextAnchor.MiddleCenter);
            var classStatus = CreateLayoutText(classBlock, "", 20, 760f, 72f, TextAnchor.MiddleCenter);

            var classGrid = CreateStack(body, 12f, 764f);
            var batterRow = CreatePairRow(classGrid, 764f, 88f);
            var spearmanRow = CreatePairRow(classGrid, 764f, 88f);
            var bowmanRow = CreatePairRow(classGrid, 764f, 88f);
            _loadoutClassPicker = new ClassPickerRefs
            {
                StatusText = classStatus,
                BatterButton = CreateClassButton(batterRow, "Batter", () => SelectClass(PlayerClass.Batter)),
                SpearmanButton = CreateClassButton(batterRow, "Spearman", () => SelectClass(PlayerClass.Spearman)),
                BowmanButton = CreateClassButton(spearmanRow, "Bowman", () => SelectClass(PlayerClass.Bowman)),
                SamuraiButton = CreateClassButton(spearmanRow, "Samurai", () => SelectClass(PlayerClass.Samurai)),
                MagicianButton = CreateClassButton(bowmanRow, "Magician", () => SelectClass(PlayerClass.Magician)),
                PaladinButton = CreateClassButton(bowmanRow, "Paladin", () => SelectClass(PlayerClass.Paladin))
            };

            var technique = CreateStack(body, 12f, 760f);
            CreateLayoutText(technique, "Attack Technique", 26, 760f, 36f, TextAnchor.MiddleCenter);
            _techniqueStatusText = CreateLayoutText(technique, "", 20, 760f, 64f, TextAnchor.UpperCenter);
            var techniqueRow = CreatePairRow(technique, 492f, 76f);
            _techniqueStandardButton = CreatePairButton(techniqueRow, "Standard", 240f, 76f, () => SelectAttackMode(AttackMode.Standard));
            _techniqueSpecialButton = CreatePairButton(techniqueRow, "Special", 240f, 76f, SelectSpecialAttackMode);

            var weapon = CreateStack(body, 12f, 760f);
            CreateLayoutText(weapon, "Weapon Quality", 26, 760f, 36f, TextAnchor.MiddleCenter);
            _weaponTierStatusText = CreateLayoutText(weapon, "", 22, 760f, 160f, TextAnchor.UpperCenter);
            var cycleRow = CreatePairRow(weapon, 492f, 76f);
            _weaponTierPrevButton = CreatePairButton(cycleRow, "◀ Lower", 240f, 76f, () => CycleWeaponTier(-1));
            _weaponTierNextButton = CreatePairButton(cycleRow, "Higher ▶", 240f, 76f, () => CycleWeaponTier(1));
            _altairRelicLine = CreateLayoutText(weapon, "Altair relic unlocked — cycle to equip (Fateful power).", 20, 760f, 40f, TextAnchor.MiddleCenter);
            _altairRelicLine.gameObject.SetActive(false);

            var footerY = HubLayout.FooterCenterY;
            var footerSize = new Vector2(280f, 72f);
            CreateButton(panel.transform, "Back to Shop", new Vector2(-170f, footerY), () =>
            {
                panel.SetActive(false);
                OpenShop();
            }, large: true, size: footerSize);
            CreateButton(panel.transform, "Close", new Vector2(170f, footerY), () => panel.SetActive(false), large: true, size: footerSize);
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

        void SelectClass(PlayerClass playerClass)
        {
            if (playerClass == PlayerClass.Spearman && !GameSave.SpearmanUnlocked) return;
            if (playerClass == PlayerClass.Bowman && !GameSave.BowmanUnlocked) return;
            if (playerClass == PlayerClass.Samurai && !GameSave.SamuraiUnlocked) return;
            if (playerClass == PlayerClass.Magician && !GameSave.MagicianUnlocked) return;
            if (playerClass == PlayerClass.Paladin && !GameSave.PaladinUnlocked) return;
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
            var tiers = WeaponCatalog.GetSelectableTiers(playerClass);

            if (_weaponTierStatusText != null)
                _weaponTierStatusText.text = WeaponCatalog.GetLoadoutProgressText(playerClass);

            if (_altairRelicLine != null)
            {
                var showAltair = GameSave.AltairUnlocked
                    && WeaponCatalog.GetEquippedTier(playerClass) != WeaponMaterialTier.Altair;
                _altairRelicLine.gameObject.SetActive(showAltair);
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
                PlayerClass.Spearman => "Spearman — long reach and a hard jab. Sweep is 240°, not a full spin.",
                PlayerClass.Bowman => "Bowman — longest range and high crit. Weak once a pack reaches you.",
                PlayerClass.Samurai => "Samurai — close multi-hit crits. Each swing hits softer until a crit lands.",
                PlayerClass.Magician => "Magician — splash around the main target. Slow casts, soft on one enemy.",
                PlayerClass.Paladin => "Paladin — more health and defense, short mace (~30% less damage). Blocks reflect 25%.",
                _ => "Batter — close bat and a full 360° whirlwind. No extra defense or range."
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
                PlayerClass.Paladin => "Paladin",
                _ => "Batter"
            };
        }

        void RefreshClassPicker(ClassPickerRefs picker)
        {
            var selected = GameSave.SelectedClass;
            if (picker.StatusText != null)
                picker.StatusText.text = GetClassStatusText(selected);

            RefreshClassButton(picker.BatterButton, PlayerClass.Batter, true, "Batter");
            RefreshClassButton(picker.SpearmanButton, PlayerClass.Spearman, GameSave.SpearmanUnlocked, "Spearman\nEmberwilds R20 boss");
            RefreshClassButton(picker.BowmanButton, PlayerClass.Bowman, GameSave.BowmanUnlocked, "Bowman\nWarded Halls R30 clear");
            RefreshClassButton(picker.SamuraiButton, PlayerClass.Samurai, GameSave.SamuraiUnlocked, "Samurai\nIronvault R40 boss");
            RefreshClassButton(picker.MagicianButton, PlayerClass.Magician, GameSave.MagicianUnlocked, "Magician\nEndless Front R80");
            RefreshClassButton(picker.PaladinButton, PlayerClass.Paladin, GameSave.PaladinUnlocked, $"Paladin\n{GameSave.PaladinUnlockKills:N0} enemies");
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
            {
                label.text = unlocked ? GetClassDisplayName(playerClass) : lockedLabel;
                label.fontSize = unlocked ? 24 : 18;
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                label.verticalOverflow = VerticalWrapMode.Truncate;
            }
        }

        UpgradeRowRefs CreateUpgradeRow(Transform parent, string label, int cost, float y, Action onBuy)
        {
            return new UpgradeRowRefs
            {
                Label = CreateText(parent, $"{label} — {cost}g", 28, TextAnchor.MiddleLeft, new Vector2(-254, y), new Vector2(400, 48)),
                BuyButton = CreateButton(parent, "Buy", new Vector2(266, y), onBuy, large: true)
            };
        }

        UpgradeRowRefs CreateShopUpgradeRow(Transform parent, ShopUpgradeKind kind, Action onBuy)
        {
            var row = new GameObject(kind + "Row");
            row.transform.SetParent(parent, false);
            row.AddComponent<RectTransform>();
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            var rowElement = row.AddComponent<LayoutElement>();
            rowElement.preferredHeight = ShopRowHeight;
            rowElement.minHeight = ShopRowHeight;
            rowElement.flexibleWidth = 1f;

            var labelText = CreateText(row.transform, GetShopRowTitle(kind), HubLayout.BodyFont, TextAnchor.MiddleLeft, Vector2.zero, new Vector2(360f, ShopRowHeight));
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.horizontalOverflow = HorizontalWrapMode.Wrap;
            labelText.verticalOverflow = VerticalWrapMode.Truncate;
            var labelElement = labelText.gameObject.AddComponent<LayoutElement>();
            labelElement.flexibleWidth = 1f;
            labelElement.minWidth = 120f;
            labelElement.preferredHeight = ShopRowHeight;

            var infoButton = CreateButton(row.transform, "Info", Vector2.zero, () => OpenShopInfo(kind), size: new Vector2(ShopInfoWidth, ShopRowHeight));
            PinShopButton(infoButton, ShopInfoWidth, StoneButtonStyle.Primary);
            var infoLabel = infoButton.GetComponentInChildren<Text>();
            if (infoLabel != null)
                infoLabel.fontSize = HubLayout.BodyFont;

            var buyButton = CreateButton(row.transform, "Buy", Vector2.zero, onBuy, size: new Vector2(ShopBuyWidth, ShopRowHeight));
            PinShopButton(buyButton, ShopBuyWidth, StoneButtonStyle.Green);
            var buyLabel = buyButton.GetComponentInChildren<Text>();
            if (buyLabel != null)
            {
                buyLabel.fontSize = HubLayout.BodyFont;
                buyLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
                buyLabel.verticalOverflow = VerticalWrapMode.Truncate;
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
            // Thick Hide and Second Wind only. A rank past III must not read as XL.
            string[] romans = { "I", "II", "III" };
            if (value > romans.Length) return value.ToString();
            return romans[value - 1];
        }

        static string RankedTitle(string baseName, int ownedLevel, bool maxed)
        {
            if (maxed)
                return $"{baseName} MAX";
            return $"{baseName} {ownedLevel + 1}";
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
            ShopUpgradeKind.Shattering => "Shattering",
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
            ShopUpgradeKind.Shattering => "Shattering",
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
                        (StatCaps.IronvaultShopBand ? "Ironvault clear unlocked extra ranks.\n" : "") +
                        $"Ranks owned: {GameSave.HpUpgradeLevel}\n" +
                        (GameSave.IsHpUpgradeMaxed
                            ? "Status: MAXED"
                            : $"Next rank: {GameSave.HpUpgradeLevel + 1}  ·  Cost: {ShopCosts.NextHpCost}g");

                case ShopUpgradeKind.Damage:
                    return
                        "Each rank: +8% permanent damage.\n\n" +
                        $"Current multiplier: x{GameSave.DamageMultiplier:0.##}\n" +
                        $"Cap: x{StatCaps.PermanentMaxDamageMultiplier:0.#}\n" +
                        (StatCaps.IronvaultShopBand ? "Ironvault clear unlocked extra ranks.\n" : "") +
                        $"Ranks owned: {GameSave.DamageUpgradeLevel}\n" +
                        (GameSave.IsDamageUpgradeMaxed
                            ? "Status: MAXED"
                            : $"Next rank: {GameSave.DamageUpgradeLevel + 1}  ·  Cost: {ShopCosts.NextDamageCost}g");

                case ShopUpgradeKind.Speed:
                    return
                        "Each rank: +6% permanent move speed.\n\n" +
                        "Speed ranks stop at the Crypt cap on purpose; later rounds add enemy health, not speed.\n\n" +
                        $"Current multiplier: x{GameSave.SpeedMultiplier:0.##}\n" +
                        $"Cap: x{StatCaps.PermanentMaxSpeedMultiplier:0.#}\n" +
                        $"Ranks owned: {GameSave.SpeedUpgradeLevel}\n" +
                        (GameSave.IsSpeedUpgradeMaxed
                            ? "Status: MAXED"
                            : $"Next rank: {GameSave.SpeedUpgradeLevel + 1}  ·  Cost: {ShopCosts.NextSpeedCost}g");

                case ShopUpgradeKind.Range:
                    return
                        "Each rank: +5% permanent attack range.\n\n" +
                        $"Current multiplier: x{GameSave.AttackRangeMultiplier:0.##}\n" +
                        $"Cap: x{StatCaps.PermanentMaxAttackRangeMultiplier:0.#}\n" +
                        (StatCaps.IronvaultShopBand ? "Ironvault clear unlocked extra ranks.\n" : "") +
                        $"Ranks owned: {GameSave.RangeUpgradeLevel}\n" +
                        (GameSave.IsRangeUpgradeMaxed
                            ? "Status: MAXED"
                            : $"Next rank: {GameSave.RangeUpgradeLevel + 1}  ·  Cost: {ShopCosts.NextRangeCost}g");

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
                        "One-time upgrade for Batter / Spearman / Samurai / Paladin.\n\n" +
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
                        "One-time upgrade for Batter / Spearman / Bowman / Samurai / Paladin.\n\n" +
                        "Hits chill enemies for 1s (−60% move speed).\n\n" +
                        (GameSave.FrostTipUnlocked
                            ? "Status: Owned"
                            : $"Cost: {ShopCosts.FrostTip}g");

                case ShopUpgradeKind.Shattering:
                    return
                        "One-time upgrade. Requires an Ironvault clear.\n\n" +
                        "Your hits deal +40% damage to slowed enemies.\n\n" +
                        (GameSave.ShatteringUnlocked
                            ? "Status: Owned"
                            : !GameSave.DungeonSurvivalCleared
                                ? "Status: Locked — clear Ironvault Survival"
                                : $"Cost: {ShopCosts.Shattering}g");

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

        void BuyShattering()
        {
            if (GameSave.ShatteringUnlocked || !GameSave.DungeonSurvivalCleared) return;
            if (!GameSave.TrySpendGold(ShopCosts.Shattering)) return;
            GameSave.ShatteringUnlocked = true;
            OnShopUpgradePurchased();
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

            if (GameSave.ShatteringUnlocked)
                SetOwnedRow(_shatteringRow, "Shattering");
            else if (!GameSave.DungeonSurvivalCleared)
                SetLockedRow(_shatteringRow, "Shattering");
            else
                SetUpgradeRow(_shatteringRow, "Shattering", ShopCosts.Shattering, false);
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
                    UiSprites.ApplyButtonSprite(image, new Vector2(ShopBuyWidth, ShopRowHeight));
                    image.color = new Color(0.32f, 0.34f, 0.38f, 0.75f);
                }

                var buyLabel = row.BuyButton.GetComponentInChildren<Text>();
                if (buyLabel != null)
                    buyLabel.color = new Color(0.72f, 0.74f, 0.78f);
                PlaceBuyLabel(buyLabel, row.CoinIcon, "Locked", showCoin: false);
            }
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
                    UiSprites.ApplyButtonSprite(image, new Vector2(ShopBuyWidth, ShopRowHeight),
                        maxed ? StoneButtonStyle.Primary : StoneButtonStyle.Green);
                    image.color = Color.white;
                }

                var buyLabel = row.BuyButton.GetComponentInChildren<Text>();
                if (buyLabel != null)
                    buyLabel.color = Color.white;
                PlaceBuyLabel(buyLabel, row.CoinIcon, maxed ? "MAX" : $"Buy {GoldFormat.Abbreviate(cost)}", showCoin: !maxed);
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
                    UiSprites.ApplyButtonSprite(image, new Vector2(ShopBuyWidth, ShopRowHeight));
                    image.color = new Color(0.42f, 0.44f, 0.48f, 0.88f);
                }

                var buyLabel = row.BuyButton.GetComponentInChildren<Text>();
                if (buyLabel != null)
                    buyLabel.color = new Color(0.82f, 0.84f, 0.88f);
                PlaceBuyLabel(buyLabel, row.CoinIcon, "Owned", showCoin: false);
            }
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
                22,
                TextAnchor.MiddleCenter,
                new Vector2(0f, 196f),
                new Vector2(760f, 32f));
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
            if (_shopScroll != null)
                _shopScroll.verticalNormalizedPosition = 1f;
        }

        Text CreateGoalBanner(Transform parent, Vector2 anchoredPosition)
        {
            var go = new GameObject("GoalBanner");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(760f, 72f);
            var image = go.AddComponent<Image>();
            image.color = new Color(0.08f, 0.07f, 0.05f, 0.55f);
            image.raycastTarget = false;
            var element = go.AddComponent<LayoutElement>();
            element.preferredWidth = 760f;
            element.preferredHeight = 72f;
            element.minHeight = 72f;
            var label = CreateText(go.transform, GoalPlaceholder, HubLayout.BodyFont, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(728f, 64f));
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }

        static Text CreateLayoutText(Transform parent, string text, int font, float width, float height, TextAnchor anchor)
        {
            var label = CreateText(parent, text, font, anchor, Vector2.zero, new Vector2(width, height));
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            var element = label.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = width;
            element.preferredHeight = height;
            element.minHeight = height;
            return label;
        }

        static Transform CreateStack(Transform parent, float spacing, float width)
        {
            var go = new GameObject("Section");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, 0f);
            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            var element = go.AddComponent<LayoutElement>();
            element.preferredWidth = width;
            return go.transform;
        }

        static Transform CreatePairRow(Transform parent, float width, float height)
        {
            var go = new GameObject("ButtonRow");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            var element = go.AddComponent<LayoutElement>();
            element.preferredWidth = width;
            element.preferredHeight = height;
            element.minHeight = height;
            return go.transform;
        }

        static Button CreateClassButton(Transform row, string label, Action onClick) =>
            CreatePairButton(row, label, 376f, 88f, onClick);

        static Button CreatePairButton(Transform row, string label, float width, float height, Action onClick)
        {
            var button = CreateButton(row, label, Vector2.zero, onClick, size: new Vector2(width, height));
            var element = button.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = width;
            element.minWidth = width;
            element.preferredHeight = height;
            element.minHeight = height;
            var text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.verticalOverflow = VerticalWrapMode.Truncate;
            }

            return button;
        }

        static void PinShopButton(Button button, float width, StoneButtonStyle style)
        {
            if (button == null) return;
            var element = button.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = width;
            element.minWidth = width;
            element.preferredHeight = ShopRowHeight;
            element.minHeight = ShopRowHeight;
            element.flexibleWidth = 0f;
            var image = button.GetComponent<Image>();
            if (image != null)
                UiSprites.ApplyButtonSprite(image, new Vector2(width, ShopRowHeight), style);
        }

        static void PlaceBuyLabel(Text buyLabel, Image coin, string text, bool showCoin)
        {
            if (buyLabel == null)
            {
                if (coin != null) coin.enabled = showCoin;
                return;
            }

            buyLabel.text = text;
            buyLabel.fontSize = HubLayout.BodyFont;
            buyLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            buyLabel.verticalOverflow = VerticalWrapMode.Truncate;
            var rect = buyLabel.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            var useCoin = showCoin && buyLabel.preferredWidth <= BuyLabelWidth;
            if (coin != null) coin.enabled = useCoin;
            if (useCoin)
            {
                rect.anchoredPosition = new Vector2(-21f, 0f);
                rect.sizeDelta = new Vector2(BuyLabelWidth, 46f);
            }
            else
            {
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(ShopBuyWidth - 14f, 46f);
            }
        }

        static Scrollbar CreateSideScrollbar(Transform parent, Vector2 center, float height)
        {
            var barGo = new GameObject("Scrollbar");
            barGo.transform.SetParent(parent, false);
            var rect = barGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = center;
            rect.sizeDelta = new Vector2(20f, height);

            var background = barGo.AddComponent<Image>();
            if (StoneUi.Available && StoneUi.ListFrame != null)
            {
                background.sprite = StoneUi.ListFrame;
                background.type = Image.Type.Sliced;
                background.color = Color.white;
            }
            else
            {
                background.color = new Color(0.12f, 0.1f, 0.08f, 0.85f);
            }

            var sliding = new GameObject("Sliding Area");
            sliding.transform.SetParent(barGo.transform, false);
            var slidingRect = sliding.AddComponent<RectTransform>();
            slidingRect.anchorMin = Vector2.zero;
            slidingRect.anchorMax = Vector2.one;
            slidingRect.offsetMin = new Vector2(2f, 4f);
            slidingRect.offsetMax = new Vector2(-2f, -4f);

            var handleGo = new GameObject("Handle");
            handleGo.transform.SetParent(sliding.transform, false);
            var handleRect = handleGo.AddComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = Vector2.zero;
            handleRect.offsetMax = Vector2.zero;
            var handleImage = handleGo.AddComponent<Image>();
            handleImage.color = new Color(0.78f, 0.7f, 0.5f, 0.95f);

            var scrollbar = barGo.AddComponent<Scrollbar>();
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleImage;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            return scrollbar;
        }

        public void OpenLoadout()
        {
            RefreshLoadoutPanel();
            CloseAllHubPanels();
            _loadoutPanel.SetActive(true);
        }
    }
}