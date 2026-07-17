using ProjectZx.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectZx.UI
{
    public class MovementJoystick : MonoBehaviour
    {
        const float Deadzone = 0.12f;
        const float UiScale = 1.3f;

        public static MovementJoystick Instance { get; private set; }

        public Vector2 Direction { get; private set; }
        public bool IsHeld { get; private set; }

        RectTransform _baseRect;
        RectTransform _knobRect;
        float _knobRange;
        int _pointerId = -1;

        static Sprite _circleSprite;

        public static void EnsureExists()
        {
            if (Instance == null)
                new GameObject("MovementJoystick").AddComponent<MovementJoystick>();
            ApplyControlMode();
        }

        public static void ApplyControlMode()
        {
            if (Instance != null)
                Instance.ApplyControlModeInternal();
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            BuildUi();
            ApplyControlModeInternal();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (!GameSave.UsesJoystickMovement)
            {
                if (IsHeld) Release();
                return;
            }

            if (!IsHeld) return;
            if (GameHud.Instance != null && GameHud.Instance.IsChoosingUpgrade)
                Release();
        }

        void ApplyControlModeInternal()
        {
            var enabled = GameSave.UsesJoystickMovement;
            if (!enabled && IsHeld) Release();
            Direction = Vector2.zero;

            // Keep the root object active so the singleton survives mode switches;
            // hide only the on-screen canvas when tap/hold is selected.
            if (transform.childCount > 0)
            {
                var canvas = transform.GetChild(0).gameObject;
                canvas.SetActive(enabled);
            }
        }

        void BuildUi()
        {
            EventSystemSetup.EnsureExists();

            var canvasGo = new GameObject("JoystickCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            var baseSize = 170f * UiScale;
            var knobSize = 72f * UiScale;
            _knobRange = 54f * UiScale;

            var baseGo = new GameObject("JoystickBase");
            baseGo.transform.SetParent(canvasGo.transform, false);
            _baseRect = baseGo.AddComponent<RectTransform>();
            _baseRect.anchorMin = new Vector2(1f, 0f);
            _baseRect.anchorMax = new Vector2(1f, 0f);
            _baseRect.pivot = new Vector2(1f, 0f);
            _baseRect.anchoredPosition = new Vector2(-220f * UiScale, 210f * UiScale);
            _baseRect.sizeDelta = new Vector2(baseSize, baseSize);

            var baseImage = baseGo.AddComponent<Image>();
            baseImage.sprite = GetCircleSprite();
            baseImage.color = new Color(1f, 1f, 1f, 0.22f);
            baseImage.raycastTarget = true;
            baseGo.AddComponent<JoystickHitArea>().Bind(this);

            var knobGo = new GameObject("JoystickKnob");
            knobGo.transform.SetParent(baseGo.transform, false);
            _knobRect = knobGo.AddComponent<RectTransform>();
            _knobRect.anchorMin = new Vector2(0.5f, 0.5f);
            _knobRect.anchorMax = new Vector2(0.5f, 0.5f);
            _knobRect.pivot = new Vector2(0.5f, 0.5f);
            _knobRect.sizeDelta = new Vector2(knobSize, knobSize);

            var knobImage = knobGo.AddComponent<Image>();
            knobImage.sprite = GetCircleSprite();
            knobImage.color = new Color(1f, 1f, 1f, 0.42f);
            knobImage.raycastTarget = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!GameSave.UsesJoystickMovement) return;
            if (IsHeld) return;
            _pointerId = eventData.pointerId;
            IsHeld = true;
            UpdateKnob(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!GameSave.UsesJoystickMovement) return;
            if (!IsHeld || eventData.pointerId != _pointerId) return;
            UpdateKnob(eventData.position);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId) return;
            Release();
        }

        public bool IsPointerOver(Vector2 screenPos)
        {
            if (!GameSave.UsesJoystickMovement || _baseRect == null) return false;
            return RectTransformUtility.RectangleContainsScreenPoint(_baseRect, screenPos, null);
        }

        void UpdateKnob(Vector2 screenPos)
        {
            if (_baseRect == null || _knobRect == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _baseRect, screenPos, null, out var local);

            var clamped = Vector2.ClampMagnitude(local, _knobRange);
            _knobRect.anchoredPosition = clamped;

            var normalized = clamped / _knobRange;
            Direction = normalized.magnitude < Deadzone ? Vector2.zero : normalized.normalized;
        }

        void Release()
        {
            IsHeld = false;
            _pointerId = -1;
            Direction = Vector2.zero;
            if (_knobRect != null) _knobRect.anchoredPosition = Vector2.zero;
        }

        static Sprite GetCircleSprite()
        {
            if (_circleSprite != null) return _circleSprite;

            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var center = new Vector2(size * 0.5f, size * 0.5f);
            var radius = size * 0.5f - 1f;

            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var dist = Vector2.Distance(new Vector2(x, y), center);
                tex.SetPixel(x, y, dist <= radius ? Color.white : Color.clear);
            }

            tex.Apply();
            _circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return _circleSprite;
        }

        sealed class JoystickHitArea : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
        {
            MovementJoystick _owner;

            public void Bind(MovementJoystick owner) => _owner = owner;

            public void OnPointerDown(PointerEventData eventData) => _owner?.OnPointerDown(eventData);
            public void OnDrag(PointerEventData eventData) => _owner?.OnDrag(eventData);
            public void OnPointerUp(PointerEventData eventData) => _owner?.OnPointerUp(eventData);
        }
    }
}
