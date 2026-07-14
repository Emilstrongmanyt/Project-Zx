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
        }

        GameObject BuildShopPanel(Transform parent)
        {
            var panel = CreatePanel(parent, "ShopPanel", Vector2.zero, new Vector2(700, 420), new Color(0.05f, 0.08f, 0.12f, 0.92f));

            CreateUpgradeRow(panel.transform, "Max HP +15", 50, 40, () => BuyHp());
            CreateUpgradeRow(panel.transform, "Damage +8%", 75, -50, () => BuyDamage());
            CreateUpgradeRow(panel.transform, "Move Speed +6%", 60, -140, () => BuySpeed());

            CreateButton(panel.transform, "Close", new Vector2(0, -200), () => panel.SetActive(false));
            panel.SetActive(false);
            return panel;
        }

        GameObject BuildMapPanel(Transform parent)
        {
            var panel = CreatePanel(parent, "MapPanel", Vector2.zero, new Vector2(620, 220), new Color(0.08f, 0.05f, 0.1f, 0.92f));
            CreateButton(panel.transform, "Enter Survival Arena", new Vector2(0, 20), () =>
            {
                panel.SetActive(false);
                GameFactory.LoadScene(GameScenes.SurvivalArena);
            });
            CreateButton(panel.transform, "Close", new Vector2(0, -80), () => panel.SetActive(false));
            panel.SetActive(false);
            return panel;
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

        static GameObject CreatePanel(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
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
            image.color = color;
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