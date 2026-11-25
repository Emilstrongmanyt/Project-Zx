using UnityEngine;
using UnityEngine.UI;

namespace ProjectZx.UI
{
    /// <summary>
    /// Phone-safe measurements for camp menus. Dialogs use the live PopupBg slice
    /// (80 px uniform). 96 is that frame plus 16 px of pad, not PanelFrame's top ornament.
    /// </summary>
    public static class HubLayout
    {
        public const float ReferenceWidth = 1920f;
        public const float ReferenceHeight = 1080f;
        public const float BorderInset = 96f;
        public static readonly Vector2 MenuPanelSize = new Vector2(1000f, 880f);

        public const float ButtonHeight = 76f;
        public const float ButtonGap = 12f;
        public const int BodyFont = 22;
        public const int TitleFont = 34;
        public const int MinFont = 18;

        public const float FallbackSafeRight = 140f;
        public const float FallbackSafeTop = 36f;

        public static void ApplyPhoneScaler(CanvasScaler scaler)
        {
            if (scaler == null) return;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.matchWidthOrHeight = 0.5f;
        }

        public static float ContentTop => MenuPanelSize.y * 0.5f - BorderInset;
        public static float ContentBottom => -ContentTop;
        public static float ContentWidth => MenuPanelSize.x - BorderInset * 2f;
        /// <summary>Footer button center. A 72 px button spans about y = -332 to -260.</summary>
        public static float FooterCenterY => ContentBottom + 48f;

        /// <summary>
        /// Corner insets in reference pixels. Full-screen safe rects (the editor) keep 140 / 36.
        /// </summary>
        public static void ReadCornerInsets(out float safeRight, out float safeTop)
        {
            safeRight = FallbackSafeRight;
            safeTop = FallbackSafeTop;
            var width = Screen.width;
            var height = Screen.height;
            if (width <= 0 || height <= 0) return;

            var safe = Screen.safeArea;
            var insetRight = width - safe.xMax;
            var insetTop = height - safe.yMax;
            if (insetRight <= 1f && insetTop <= 1f && safe.xMin <= 1f)
                return;

            var scale = PhoneScale(width, height);
            if (scale <= 0.01f) return;
            safeRight = Mathf.Max(FallbackSafeRight, insetRight / scale);
            safeTop = Mathf.Max(FallbackSafeTop, insetTop / scale);
        }

        public static ScrollRect CreateVerticalScroll(
            Transform parent,
            Vector2 center,
            Vector2 viewportSize,
            float contentHeight)
        {
            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(parent, false);
            var scrollRect = scrollGo.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.5f, 0.5f);
            scrollRect.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRect.pivot = new Vector2(0.5f, 0.5f);
            scrollRect.anchoredPosition = center;
            scrollRect.sizeDelta = viewportSize;

            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollGo.transform, false);
            var viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            var viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, contentHeight);

            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            return scroll;
        }

        public static void PlaceFromTop(RectTransform rect, float x, float centerFromTop)
        {
            if (rect == null) return;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, -centerFromTop);
        }

        static float PhoneScale(float width, float height)
        {
            var logWidth = Mathf.Log(width / ReferenceWidth, 2f);
            var logHeight = Mathf.Log(height / ReferenceHeight, 2f);
            return Mathf.Pow(2f, Mathf.Lerp(logWidth, logHeight, 0.5f));
        }
    }
}
