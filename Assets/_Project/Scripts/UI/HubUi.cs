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
            var panel = CreateDialogPanel(parent, "ShopPanel", Vector2.zero, new Vector2(700, 420), ArtLibrary.ShopUi);

            CreateUpgradeRow(panel.transform, "Max HP +15", 50, 40, () => BuyHp());
            CreateUpgradeRow(panel.transform, "Damage +8%", 75, -50, () => BuyDamage());
            CreateUpgradeRow(panel.transform, "Move Speed +6%", 60, -140, () => BuySpeed());

            CreateButton(panel.transform, "Close", new Vector2(0, -200), () => panel.SetActive(false));
            panel.SetActive(false);
            return panel;
        }

        GameObject BuildMapPanel(Transform parent)
        {
            var panel = CreateDialogPanel(parent, "MapPanel", Vector2.zero, new Vector2(620, 220), ArtLibrary.ChallengeBoardUi);
            CreateButton(panel.transform, "Outside Survival", new Vector2(0, 30), () => EnterSurvival(SurvivalMapKind.Outside));
            CreateButton(panel.transform, "Close", new Vector2(0, -80), () => panel.SetActive(false));
            panel.SetActive(false);
            return panel;
        }

        GameObject BuildCampfirePanel(Transform parent)
        {
            var panel = CreateDialogPanel(parent, "CampfirePanel", Vector2.zero, new Vector2(620, 220), ArtLibrary.ChallengeBoardUi);
            CreateText(panel.transform, "Campfire Travel", 28, TextAnchor.MiddleCenter, new Vector2(0, 60), new Vector2(500, 40));
            CreateButton(panel.transform, "Outside Survival", new Vector2(0, 10), () => EnterSurvival(SurvivalMapKind.Outside));
            CreateButton(panel.transform, "Inside Survival", new Vector2(0, -70), () => EnterSurvival(SurvivalMapKind.Inside));
            CreateButton(panel.transform, "Close", new Vector2(0, -140), () => panel.SetActive(false));
            panel.SetActive(false);
            return panel;
        }

        void EnterSurvival(SurvivalMapKind mapKind)
        {
            if (mapKind == SurvivalMapKind.Inside && !GameSave.InsideMapUnlocked) return;

            GameSessionContext.SurvivalMap = mapKind;
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

        public void OpenShop() { RefreshGold(); _shopPanel.SetActive(true); }
        public void OpenMapSelect() { _mapPanel.SetActive(true); }

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

        static void CreateButton(Transform parent, string label, Vector2 pos, Action onClick)
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
        }
    }
}