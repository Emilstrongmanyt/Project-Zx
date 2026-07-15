using System;
using ProjectZx.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectZx.UI
{
    public class HubUi : MonoBehaviour
    {
        public static HubUi Instance { get; private set; }

        Text _goldText;

        GameObject _shopPanel;
        GameObject _mapPanel;
        GameObject _campfirePanel;

        struct ClassPickerRefs
        {
            public Text StatusText;
            public Button BatterButton;
            public Button SpearmanButton;
        }

        ClassPickerRefs _mapClassPicker;
        ClassPickerRefs _campClassPicker;

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

            _goldText = CreateText(canvasGo.transform, "0", 28, TextAnchor.UpperRight, new Vector2(-30, -30), new Vector2(200, 50));
            _goldText.alignment = TextAnchor.UpperRight;

            _shopPanel = BuildShopPanel(canvasGo.transform);
            _mapPanel = BuildMapPanel(canvasGo.transform);
            _campfirePanel = BuildCampfirePanel(canvasGo.transform);
        }

        GameObject BuildShopPanel(Transform parent)
        {
            var panel = CreateDialogPanel(parent, "ShopPanel", Vector2.zero, new Vector2(700, 500), ArtLibrary.ShopUi);

            CreateUpgradeRow(panel.transform, "Max HP +15", 50, 70, () => BuyHp());
            CreateUpgradeRow(panel.transform, "Damage +8%", 75, -10, () => BuyDamage());
            CreateUpgradeRow(panel.transform, "Move Speed +6%", 60, -90, () => BuySpeed());
            CreateWhirlwindRow(panel.transform);

            CreateButton(panel.transform, "Close", new Vector2(0, -230), () => panel.SetActive(false));
            panel.SetActive(false);
            return panel;
        }

        GameObject BuildMapPanel(Transform parent)
        {
            var panel = CreateDialogPanel(parent, "MapPanel", Vector2.zero, new Vector2(680, 360), ArtLibrary.ChallengeBoardUi);
            _mapClassPicker = BuildClassPicker(panel.transform, 130f, 95f, 45f);
            CreateButton(panel.transform, "Outside Survival", new Vector2(0, -35), () => EnterSurvival(SurvivalMapKind.Outside));
            CreateButton(panel.transform, "Close", new Vector2(0, -120), () => panel.SetActive(false));
            panel.SetActive(false);
            return panel;
        }

        GameObject BuildCampfirePanel(Transform parent)
        {
            var panel = CreateDialogPanel(parent, "CampfirePanel", Vector2.zero, new Vector2(680, 420), ArtLibrary.ChallengeBoardUi);
            CreateText(panel.transform, "Campfire Travel", 28, TextAnchor.MiddleCenter, new Vector2(0, 170), new Vector2(500, 40));
            _campClassPicker = BuildClassPicker(panel.transform, 125f, 90f, 40f);
            CreateButton(panel.transform, "Outside Survival", new Vector2(0, -35), () => EnterSurvival(SurvivalMapKind.Outside));
            CreateButton(panel.transform, "Inside Survival", new Vector2(0, -110), () => EnterSurvival(SurvivalMapKind.Inside));
            CreateButton(panel.transform, "Close", new Vector2(0, -185), () => panel.SetActive(false));
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
                SpearmanButton = CreateButton(parent, "Spearman", new Vector2(130, buttonY), () => SelectClass(PlayerClass.Spearman))
            };
        }

        void SelectClass(PlayerClass playerClass)
        {
            if (playerClass == PlayerClass.Spearman && !GameSave.SpearmanUnlocked) return;
            GameSave.SelectedClass = playerClass;
            RefreshClassPicker(_mapClassPicker);
            RefreshClassPicker(_campClassPicker);
        }

        void RefreshClassPicker(ClassPickerRefs picker)
        {
            var selected = GameSave.SelectedClass;
            if (picker.StatusText != null)
            {
                picker.StatusText.text = selected == PlayerClass.Spearman
                    ? "Spearman — long reach, single target"
                    : "Batter — melee bat, whirlwind upgrade";
            }

            if (picker.BatterButton != null)
            {
                var image = picker.BatterButton.GetComponent<Image>();
                if (image != null)
                    image.color = selected == PlayerClass.Batter
                        ? new Color(0.28f, 0.5f, 0.32f, 0.98f)
                        : new Color(0.2f, 0.35f, 0.55f, 0.95f);
            }

            if (picker.SpearmanButton != null)
            {
                var unlocked = GameSave.SpearmanUnlocked;
                picker.SpearmanButton.interactable = unlocked;
                var image = picker.SpearmanButton.GetComponent<Image>();
                if (image != null)
                {
                    image.color = !unlocked
                        ? new Color(0.25f, 0.25f, 0.28f, 0.7f)
                        : selected == PlayerClass.Spearman
                            ? new Color(0.28f, 0.5f, 0.32f, 0.98f)
                            : new Color(0.2f, 0.35f, 0.55f, 0.95f);
                }

                var label = picker.SpearmanButton.GetComponentInChildren<Text>();
                if (label != null)
                    label.text = unlocked ? "Spearman" : "Spearman (Beat R20 Boss)";
            }
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
            _mapPanel.SetActive(false);
            _campfirePanel.SetActive(false);
            GameFactory.LoadScene(GameScenes.SurvivalArena);
        }

        void CreateUpgradeRow(Transform parent, string label, int cost, float y, Action onBuy)
        {
            CreateText(parent, $"{label} — {cost}g", 24, TextAnchor.MiddleLeft, new Vector2(-220, y), new Vector2(360, 40));
            CreateButton(parent, "Buy", new Vector2(220, y), onBuy);
        }

        void BuyHp()
        {
            if (GameSave.TrySpendGold(50)) GameSave.HpUpgradeLevel++;
            RefreshGold();
        }

        void BuyDamage()
        {
            if (GameSave.TrySpendGold(75)) GameSave.DamageUpgradeLevel++;
            RefreshGold();
        }

        void BuySpeed()
        {
            if (GameSave.TrySpendGold(60)) GameSave.SpeedUpgradeLevel++;
            RefreshGold();
        }

        void CreateWhirlwindRow(Transform parent)
        {
            if (GameSave.WhirlwindUnlocked)
            {
                CreateText(parent, "Whirlwind — Owned", 24, TextAnchor.MiddleLeft, new Vector2(-220, -170), new Vector2(360, 40));
                return;
            }

            CreateUpgradeRow(parent, "Whirlwind Attack", 500, -170, BuyWhirlwind);
        }

        void BuyWhirlwind()
        {
            if (GameSave.WhirlwindUnlocked) return;
            if (GameSave.TrySpendGold(500)) GameSave.WhirlwindUnlocked = true;
            RefreshGold();
            if (_shopPanel != null)
            {
                _shopPanel.SetActive(false);
                OpenShop();
            }
        }

        public void OpenShop() { RefreshGold(); _shopPanel.SetActive(true); }

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

        static Button CreateButton(Transform parent, string label, Vector2 pos, Action onClick)
        {
            var go = new GameObject(label + "Button");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(220, 52);
            var image = go.AddComponent<Image>();
            image.color = new Color(0.2f, 0.35f, 0.55f, 0.95f);
            var button = go.AddComponent<Button>();
            button.onClick.AddListener(() => onClick());
            CreateText(go.transform, label, 22, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(200, 44));
            return button;
        }
    }
}