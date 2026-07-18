using UnityEngine;
using UnityEngine.UI;

namespace ProjectZx.UI
{
    /// <summary>
    /// Scrolling damage popup over a unit's head.
    /// Uses screen-space UI (not TextMesh) so numbers render correctly under URP 2D.
    /// White for enemies, red for the hero.
    /// </summary>
    public class FloatingDamageNumber : MonoBehaviour
    {
        const float Lifetime = 0.9f;
        const float RisePixelsPerSecond = 90f;
        const float HeadOffsetY = 0.95f;
        const int FontSize = 48;
        const int CanvasSortOrder = 250;

        static readonly Color EnemyColor = Color.white;
        static readonly Color HeroColor = new Color(1f, 0.2f, 0.2f, 1f);

        static Canvas _canvas;
        static Font _font;

        RectTransform _rect;
        Text _label;
        Vector3 _worldAnchor;
        float _age;
        float _risePixels;
        float _xJitterPixels;
        Color _baseColor;

        public static void Spawn(Vector3 worldPosition, int amount, bool isHeroHit)
        {
            if (amount <= 0) return;

            EnsureCanvas();
            if (_canvas == null) return;

            var go = new GameObject("DamageNumber");
            go.transform.SetParent(_canvas.transform, false);

            var number = go.AddComponent<FloatingDamageNumber>();
            number.Setup(worldPosition, amount, isHeroHit);
        }

        static void EnsureCanvas()
        {
            if (_canvas != null) return;

            var go = new GameObject("DamageNumberCanvas");
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = CanvasSortOrder;
            // No GraphicRaycaster — numbers must never block taps.

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        void Setup(Vector3 worldPosition, int amount, bool isHeroHit)
        {
            _worldAnchor = worldPosition + Vector3.up * HeadOffsetY;
            _baseColor = isHeroHit ? HeroColor : EnemyColor;
            _xJitterPixels = Random.Range(-28f, 28f);
            _risePixels = 0f;
            _age = 0f;

            _rect = gameObject.AddComponent<RectTransform>();
            _rect.anchorMin = new Vector2(0.5f, 0.5f);
            _rect.anchorMax = new Vector2(0.5f, 0.5f);
            _rect.pivot = new Vector2(0.5f, 0.5f);
            _rect.sizeDelta = new Vector2(220f, 80f);

            _label = gameObject.AddComponent<Text>();
            if (_font != null)
                _label.font = _font;
            _label.text = amount.ToString();
            _label.fontSize = FontSize;
            _label.fontStyle = FontStyle.Bold;
            _label.alignment = TextAnchor.MiddleCenter;
            _label.color = _baseColor;
            _label.horizontalOverflow = HorizontalWrapMode.Overflow;
            _label.verticalOverflow = VerticalWrapMode.Overflow;
            _label.raycastTarget = false;

            // Soft shadow so white numbers stay readable on bright tiles.
            var shadow = gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
            shadow.effectDistance = new Vector2(2f, -2f);
            shadow.useGraphicAlpha = true;

            SyncScreenPosition();
        }

        void LateUpdate()
        {
            _age += Time.deltaTime;
            _risePixels += RisePixelsPerSecond * Time.deltaTime;

            var t = Mathf.Clamp01(_age / Lifetime);
            if (_label != null)
            {
                var c = _baseColor;
                c.a = 1f - t * t;
                _label.color = c;
            }

            SyncScreenPosition();

            if (_age >= Lifetime)
                Destroy(gameObject);
        }

        void SyncScreenPosition()
        {
            if (_rect == null || _canvas == null) return;

            var cam = Camera.main;
            if (cam == null) return;

            var screen = cam.WorldToScreenPoint(_worldAnchor);
            // Behind camera / invalid depth — hide for this frame.
            if (screen.z < 0f)
            {
                _label.enabled = false;
                return;
            }

            if (_label != null && !_label.enabled)
                _label.enabled = true;

            // Overlay canvas: RectTransform.position is in screen pixels.
            _rect.position = new Vector3(
                screen.x + _xJitterPixels,
                screen.y + _risePixels,
                0f);
        }
    }
}
