using ProjectZx.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectZx.UI
{
    /// <summary>
    /// Scrolling damage popup over a unit's head.
    /// Uses screen-space UI (not TextMesh) so numbers render correctly under URP 2D.
    /// White for enemies, red for the hero, gold for crits, pink for burn DoT. Always draws under modal HUD.
    /// </summary>
    public class FloatingDamageNumber : MonoBehaviour
    {
        const float Lifetime = 0.9f;
        const float CritLifetime = 1.15f;
        const float RisePixelsPerSecond = 90f;
        const float CritRisePixelsPerSecond = 110f;
        const float HeadOffsetY = 0.95f;
        const int FontSize = 48;
        const int CritFontSize = 62;
        const int CanvasSortOrder = 40;

        static readonly Color EnemyColor = Color.white;
        static readonly Color HeroColor = new Color(1f, 0.2f, 0.2f, 1f);
        static readonly Color BurnColor = new Color(1f, 0.45f, 0.85f, 1f);
        static readonly Color BleedColor = new Color(0.85f, 0.08f, 0.12f, 1f);
        static readonly Color BlockColor = new Color(0.55f, 0.85f, 1f, 1f);
        static readonly Color CritColor = new Color(1f, 0.88f, 0.2f, 1f);

        static Canvas _canvas;
        static Font _font;

        RectTransform _rect;
        Text _label;
        Vector3 _worldAnchor;
        float _age;
        float _lifetime;
        float _risePixels;
        float _riseSpeed;
        float _xJitterPixels;
        Color _baseColor;
        float _startScale = 1f;

        public static void Spawn(Vector3 worldPosition, int amount, bool isHeroHit, bool isCrit = false)
        {
            if (isCrit && !isHeroHit)
            {
                SpawnCrit(worldPosition, amount);
                return;
            }

            Spawn(worldPosition, amount, isHeroHit ? HeroColor : EnemyColor);
        }

        public static void SpawnBurn(Vector3 worldPosition, int amount)
        {
            Spawn(worldPosition, amount, BurnColor);
        }

        public static void SpawnBleed(Vector3 worldPosition, int amount)
        {
            Spawn(worldPosition, amount, BleedColor);
        }

        /// <summary>Full block / timed shield absorb feedback.</summary>
        public static void SpawnBlock(Vector3 worldPosition)
        {
            if (GameHud.Instance != null && GameHud.Instance.IsGamePaused) return;

            EnsureCanvas();
            if (_canvas == null) return;

            var go = new GameObject("BlockNumber");
            go.transform.SetParent(_canvas.transform, false);

            var number = go.AddComponent<FloatingDamageNumber>();
            number.SetupText(worldPosition, "BLOCK", BlockColor, FontSize, Lifetime, RisePixelsPerSecond, 1f);
        }

        public static void SpawnCrit(Vector3 worldPosition, int amount)
        {
            if (amount <= 0) return;
            if (GameHud.Instance != null && GameHud.Instance.IsGamePaused) return;

            EnsureCanvas();
            if (_canvas == null) return;

            var go = new GameObject("CritNumber");
            go.transform.SetParent(_canvas.transform, false);

            var number = go.AddComponent<FloatingDamageNumber>();
            var fontSize = ScaledFont(CritFontSize);
            number.SetupText(
                worldPosition,
                $"CRIT {amount}",
                CritColor,
                fontSize,
                CritLifetime,
                CritRisePixelsPerSecond,
                1.15f);
        }

        public static void Spawn(Vector3 worldPosition, int amount, Color color)
        {
            if (amount <= 0) return;
            if (GameHud.Instance != null && GameHud.Instance.IsGamePaused) return;

            EnsureCanvas();
            if (_canvas == null) return;

            var go = new GameObject("DamageNumber");
            go.transform.SetParent(_canvas.transform, false);

            var number = go.AddComponent<FloatingDamageNumber>();
            number.Setup(worldPosition, amount, color);
        }

        public static void ClearAll()
        {
            if (_canvas == null) return;
            for (var i = _canvas.transform.childCount - 1; i >= 0; i--)
            {
                var child = _canvas.transform.GetChild(i);
                if (child != null)
                    Object.Destroy(child.gameObject);
            }
        }

        static int ScaledFont(int baseSize)
            => GameSave.LargeDamageNumbers ? Mathf.RoundToInt(baseSize * 1.35f) : baseSize;

        static void EnsureCanvas()
        {
            if (_canvas != null) return;

            var go = new GameObject("DamageNumberCanvas");
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = CanvasSortOrder;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        void Setup(Vector3 worldPosition, int amount, Color color)
        {
            SetupText(
                worldPosition,
                amount.ToString(),
                color,
                ScaledFont(FontSize),
                Lifetime,
                RisePixelsPerSecond,
                1f);
        }

        void SetupText(
            Vector3 worldPosition,
            string text,
            Color color,
            int fontSize,
            float lifetime,
            float riseSpeed,
            float startScale)
        {
            _worldAnchor = worldPosition + Vector3.up * HeadOffsetY;
            _baseColor = color;
            _xJitterPixels = Random.Range(-28f, 28f);
            _risePixels = 0f;
            _age = 0f;
            _lifetime = lifetime;
            _riseSpeed = riseSpeed;
            _startScale = startScale;

            _rect = gameObject.AddComponent<RectTransform>();
            _rect.anchorMin = new Vector2(0.5f, 0.5f);
            _rect.anchorMax = new Vector2(0.5f, 0.5f);
            _rect.pivot = new Vector2(0.5f, 0.5f);
            _rect.sizeDelta = new Vector2(280f, 90f);
            _rect.localScale = Vector3.one * _startScale;

            _label = gameObject.AddComponent<Text>();
            if (_font != null)
                _label.font = _font;
            _label.text = text;
            _label.fontSize = fontSize;
            _label.fontStyle = FontStyle.Bold;
            _label.alignment = TextAnchor.MiddleCenter;
            _label.color = _baseColor;
            _label.horizontalOverflow = HorizontalWrapMode.Overflow;
            _label.verticalOverflow = VerticalWrapMode.Overflow;
            _label.raycastTarget = false;

            var shadow = gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            shadow.effectDistance = new Vector2(2.5f, -2.5f);
            shadow.useGraphicAlpha = true;

            // Extra outline for crit readability on busy combat.
            if (fontSize >= CritFontSize - 4)
            {
                var outline = gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0.15f, 0.05f, 0f, 0.9f);
                outline.effectDistance = new Vector2(1.5f, -1.5f);
                outline.useGraphicAlpha = true;
            }

            SyncScreenPosition();
        }

        void LateUpdate()
        {
            if (GameHud.Instance != null && GameHud.Instance.IsGamePaused)
            {
                Destroy(gameObject);
                return;
            }

            _age += Time.deltaTime;
            _risePixels += _riseSpeed * Time.deltaTime;

            var t = Mathf.Clamp01(_age / Mathf.Max(0.01f, _lifetime));
            if (_label != null)
            {
                var c = _baseColor;
                c.a = 1f - t * t;
                _label.color = c;
            }

            // Crits pop then settle slightly.
            if (_rect != null && _startScale > 1.01f)
            {
                var pop = Mathf.Lerp(_startScale, 1f, Mathf.Clamp01(t * 2.2f));
                _rect.localScale = Vector3.one * pop;
            }

            SyncScreenPosition();

            if (_age >= _lifetime)
                Destroy(gameObject);
        }

        void SyncScreenPosition()
        {
            if (_rect == null || _canvas == null) return;

            var cam = Camera.main;
            if (cam == null) return;

            var screen = cam.WorldToScreenPoint(_worldAnchor);
            if (screen.z < 0f)
            {
                if (_label != null) _label.enabled = false;
                return;
            }

            if (_label != null && !_label.enabled)
                _label.enabled = true;

            _rect.position = new Vector3(
                screen.x + _xJitterPixels,
                screen.y + _risePixels,
                0f);
        }
    }
}
