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

        ClassPickerRefs _mapClassPicker;
        ClassPickerRefs _campClassPicker;

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
            _statsPanel = BuildStatsPanel(canvasGo.transform);
            _achievementsPanel = BuildAchievementsPanel(canvasGo.transform);
            _mapPanel = BuildMapPanel(canvasGo.transform);
            _campfirePanel = BuildCampfirePanel(canvasGo.transform);
        }

        GameObject BuildShopPanel(Transform parent)
        {
            var panel = CreateDialogPanel(parent, "ShopPanel", Vector2.zero, new Vector2(700, 620), ArtLibrary.ShopUi);

            _hpRow = CreateUpgradeRow(panel.transform, "Max HP +15", 50, 100, () => BuyHp());
            _damageRow = CreateUpgradeRow(panel.transform, "Damage +8%", 75, 56, () => BuyDamage());
            _speedRow = CreateUpgradeRow(panel.transform, "Move Speed +6%", 60, 12, () => BuySpeed());
            _whirlwindRow = CreateUpgradeRow(panel.transform, "Whirlwind (360°/180°)", 500, -32, BuyWhirlwind);
            _piercingShotRow = CreateUpgradeRow(panel.transform, "Piercing Shot (Bowman)", 2000, -76, BuyPiercingShot);

            CreateButton(panel.transform, "Character Stats", new Vector2(-130, -220), () => OpenStats());
            CreateButton(panel.transform, "Close", new Vector2(130, -220), () => panel.SetActive(false));
            panel.SetActive(false);
            return panel;
        }

        GameObject BuildAchievementsPanel(Transform parent)
        {
            var panel = CreateDialogPanel(parent, "AchievementsPanel", Vector2.zero, new Vector2(800, 640), ArtLibrary.ChallengeBoardUi);
            CreateText(panel.transform, "Achievements", 30, TextAnchor.MiddleCenter, new Vector2(0, 280), new Vector2(500, 44));
            _achievementCountText = CreateText(panel.transform, "", 20, TextAnchor.MiddleCenter, new Vector2(0, 245), new Vector2(500, 30));

            var scrollGo = new GameObject("AchievementScroll");
            scrollGo.transform.SetParent(panel.transform, false);
            var scrollRectTransform = scrollGo.AddComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            scrollRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRectTransform.pivot = new Vector2(0.5f, 0.5f);
            scrollRectTransform.anchoredPosition = new Vector2(0f, -10f);
            scrollRectTransform.sizeDelta = new Vector2(720f, 430f);

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
            const float rowHeight = 72f;
            foreach (var def in Achievements.All)
            {
                _achievementRows.Add(CreateAchievementRow(content.transform, def, y));
                y -= rowHeight;
            }

            contentRect.sizeDelta = new Vector2(700f, Mathf.Abs(y));

            CreateButton(panel.transform, "Close", new Vector2(0, -285), () => panel.SetActive(false));
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
            rect.sizeDelta = new Vector2(680f, 64f);

            var image = go.AddComponent<Image>();
            UiSprites.ApplyButtonSprite(image, new Vector2(680f, 64f));
            go.AddComponent<Button>();

            var title = CreateText(go.transform, def.Title, 20, TextAnchor.UpperLeft, new Vector2(14f, -8f), new Vector2(640f, 28f));
            title.alignment = TextAnchor.UpperLeft;
            var desc = CreateText(go.transform, def.Description, 17, TextAnchor.UpperLeft, new Vector2(14f, -34f), new Vector2(640f, 24f));
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
            var panel = CreateDialogPanel(parent, "StatsPanel", Vector2.zero, new Vector2(720, 560), ArtLibrary.ShopUi);
            CreateText(panel.transform, "Character Stats", 30, TextAnchor.MiddleCenter, new Vector2(0, 230), new Vector2(500, 44));
            _statsBodyText = CreateText(panel.transform, "", 21, TextAnchor.MiddleCenter, new Vector2(0, -10), new Vector2(620, 400));
            _statsBodyText.alignment = TextAnchor.UpperLeft;

            CreateButton(panel.transform, "Back to Shop", new Vector2(-130, -240), () =>
            {
                panel.SetActive(false);
                OpenShop();
            });
            CreateButton(panel.transform, "Close", new Vector2(130, -240), () => panel.SetActive(false));
            panel.SetActive(false);
            return panel;
        }

        GameObject BuildMapPanel(Transform parent)
        {
            var panel = CreateDialogPanel(parent, "MapPanel", Vector2.zero, new Vector2(680, 460), ArtLibrary.ChallengeBoardUi);
            _mapClassPicker = BuildClassPicker(panel.transform, 165f, 120f, 55f);
            CreateButton(panel.transform, "Outside Survival", new Vector2(0, -95), () => EnterSurvival(SurvivalMapKind.Outside));
            CreateButton(panel.transform, "Close", new Vector2(0, -175), () => panel.SetActive(false));
            panel.SetActive(false);
            return panel;
        }

        GameObject BuildCampfirePanel(Transform parent)
        {
            var panel = CreateDialogPanel(parent, "CampfirePanel", Vector2.zero, new Vector2(680, 520), ArtLibrary.ChallengeBoardUi);
            CreateText(panel.transform, "Campfire Travel", 28, TextAnchor.MiddleCenter, new Vector2(0, 210), new Vector2(500, 40));
            _campClassPicker = BuildClassPicker(panel.transform, 160f, 115f, 50f);
            CreateButton(panel.transform, "Outside Survival", new Vector2(0, -100), () => EnterSurvival(SurvivalMapKind.Outside));
            CreateButton(panel.transform, "Inside Survival", new Vector2(0, -165), () => EnterSurvival(SurvivalMapKind.Inside));
            CreateButton(panel.transform, "Close", new Vector2(0, -230), () => panel.SetActive(false));
            panel.SetActive(false);
            return panel;
        }

        ClassPickerRefs BuildClassPicker(Transform parent, float titleY, float statusY, float buttonY)
        {
            CreateText(parent, "Choose Class", 24, TextAnchor.MiddleCenter, new Vector2(0, titleY), new Vector2(500, 36));
            return new ClassPickerRefs
            {
                StatusText = CreateText(parent, "", 20, TextAnchor.MiddleCenter, new Vector2(0, statusY), new Vector2(560, 32)),
                BatterButton = CreateButton(parent, "Batter", new Vector2(-130, buttonY), () => SelectClass(PlayerClass.Batter)),
                SpearmanButton = CreateButton(parent, "Spearman", new Vector2(130, buttonY), () => SelectClass(PlayerClass.Spearman)),
                BowmanButton = CreateButton(parent, "Bowman", new Vector2(-130, buttonY - 58f), () => SelectClass(PlayerClass.Bowman)),
                MagicianButton = CreateButton(parent, "Magician", new Vector2(130, buttonY - 58f), () => SelectClass(PlayerClass.Magician))
            };
        }

        void SelectClass(PlayerClass playerClass)
        {
            if (playerClass == PlayerClass.Spearman && !GameSave.SpearmanUnlocked) return;
            if (playerClass == PlayerClass.Bowman && !GameSave.BowmanUnlocked) return;
            if (playerClass == PlayerClass.Magician && !GameSave.MagicianUnlocked) return;
            GameSave.SelectedClass = playerClass;
            RefreshClassPicker(_mapClassPicker);
            RefreshClassPicker(_campClassPicker);
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
            GameSessionContext.FreshSurvivalRun = true;
            GameSessionContext.CarryRound = 0;
            GameSessionContext.RunSnapshot = default;
            _shopPanel.SetActive(false);
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
                Label = CreateText(parent, $"{label} — {cost}g", 20, TextAnchor.MiddleLeft, new Vector2(-195, y), new Vector2(280, 32)),
                BuyButton = CreateButton(parent, "Buy", new Vector2(205, y), onBuy)
            };
        }

        void BuyHp()
        {
            if (GameSave.IsHpUpgradeMaxed) return;
            if (GameSave.TrySpendGold(50)) GameSave.HpUpgradeLevel++;
            RefreshGold();
            RefreshShopRows();
        }

        void BuyDamage()
        {
            if (GameSave.IsDamageUpgradeMaxed) return;
            if (GameSave.TrySpendGold(75)) GameSave.DamageUpgradeLevel++;
            RefreshGold();
            RefreshShopRows();
        }

        void BuySpeed()
        {
            if (GameSave.IsSpeedUpgradeMaxed) return;
            if (GameSave.TrySpendGold(60)) GameSave.SpeedUpgradeLevel++;
            RefreshGold();
            RefreshShopRows();
        }

        void RefreshShopRows()
        {
            SetUpgradeRow(_hpRow, "Max HP +15", 50, GameSave.IsHpUpgradeMaxed, $"Max HP {GameSave.MaxHp}/{StatCaps.PermanentMaxHp}");
            SetUpgradeRow(_damageRow, "Damage +8%", 75, GameSave.IsDamageUpgradeMaxed, $"Damage x{GameSave.DamageMultiplier:0.##} (max x{StatCaps.PermanentMaxDamageMultiplier:0.#})");
            SetUpgradeRow(_speedRow, "Move Speed +6%", 60, GameSave.IsSpeedUpgradeMaxed, $"Speed x{GameSave.SpeedMultiplier:0.##} (max x{StatCaps.PermanentMaxSpeedMultiplier:0.#})");

            if (GameSave.WhirlwindUnlocked)
                SetOwnedRow(_whirlwindRow, "Whirlwind (360°/180°)");
            else
                SetUpgradeRow(_whirlwindRow, "Whirlwind (360°/180°)", 500, false, string.Empty);

            if (GameSave.PiercingShotUnlocked)
                SetOwnedRow(_piercingShotRow, "Piercing Shot (Bowman)");
            else if (!GameSave.BowmanUnlocked)
                SetLockedRow(_piercingShotRow, "Piercing Shot (Bowman)", "Unlock Bowman first");
            else
                SetUpgradeRow(_piercingShotRow, "Piercing Shot (Bowman)", 2000, false, string.Empty);
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
            if (GameSave.TrySpendGold(500)) GameSave.WhirlwindUnlocked = true;
            RefreshGold();
            RefreshShopRows();
        }

        void BuyPiercingShot()
        {
            if (GameSave.PiercingShotUnlocked || !GameSave.BowmanUnlocked) return;
            if (GameSave.TrySpendGold(2000)) GameSave.PiercingShotUnlocked = true;
            RefreshGold();
            RefreshShopRows();
        }

        public void OpenShop()
        {
            RefreshGold();
            RefreshShopRows();
            _statsPanel.SetActive(false);
            _achievementsPanel.SetActive(false);
            _shopPanel.SetActive(true);
        }

        public void OpenStats()
        {
            RefreshStats();
            _shopPanel.SetActive(false);
            _achievementsPanel.SetActive(false);
            _statsPanel.SetActive(true);
        }

        public void OpenAchievements()
        {
            RefreshAchievements();
            _shopPanel.SetActive(false);
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

            _statsBodyText.text =
                "CURRENT BUILD\n" +
                $"Class: {className}\n" +
                $"Max HP: {GameSave.MaxHp}\n" +
                $"Base Damage: {baseDamage:0.#}\n" +
                $"Move Speed: {moveSpeed:0.##}\n" +
                $"HP Upgrades: {GameSave.HpUpgradeLevel}   Damage: {GameSave.DamageUpgradeLevel}   Speed: {GameSave.SpeedUpgradeLevel}\n" +
                $"Whirlwind: {(GameSave.WhirlwindUnlocked ? "Owned" : "Locked")}\n" +
                $"Piercing Shot: {(GameSave.PiercingShotUnlocked ? "Owned" : "Locked")}\n" +
                $"Spearman: {(GameSave.SpearmanUnlocked ? "Unlocked" : "Locked")}\n" +
                $"Bowman: {(GameSave.BowmanUnlocked ? "Unlocked" : "Locked")}\n" +
                $"Magician: {(GameSave.MagicianUnlocked ? "Unlocked" : "Coming Soon")}\n\n" +
                "LIFETIME RECORDS\n" +
                $"Zombie Kills: {GameSave.LifetimeZombieKills}\n" +
                $"Boss Kills: {GameSave.LifetimeBossKills}\n" +
                $"Deaths: {GameSave.LifetimeDeaths}\n" +
                $"Gold Earned: {GameSave.LifetimeGoldEarned}\n" +
                $"Highest Round: {GameSave.HighestRoundReached}";
        }

        public void OpenMapSelect()
        {
            RefreshClassPicker(_mapClassPicker);
            _mapPanel.SetActive(true);
        }

        public void OpenCampfireTravel()
        {
            if (!GameSave.InsideMapUnlocked)
            {
                OpenMapSelect();
                return;
            }

            RefreshClassPicker(_campClassPicker);
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

        static Button CreateButton(Transform parent, string label, Vector2 pos, Action onClick)
        {
            var go = new GameObject(label + "Button");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            var size = new Vector2(220, 52);
            rect.sizeDelta = size;
            var image = go.AddComponent<Image>();
            UiSprites.ApplyButtonSprite(image, size);
            var button = go.AddComponent<Button>();
            button.onClick.AddListener(() => onClick());
            CreateText(go.transform, label, 22, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(200, 44));
            return button;
        }
    }
}