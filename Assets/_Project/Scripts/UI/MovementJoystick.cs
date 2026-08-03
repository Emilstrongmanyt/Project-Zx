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
        const int NormalSortOrder = 50;
        const int RepositionSortOrder = 220;
        // Bump when anchored-position space changes so old BR-space saves are discarded.
        const int PositionSpaceVersion = 2;
        const string PositionSpaceKey = "zx_joystick_pos_space";

        public static MovementJoystick Instance { get; private set; }

        public Vector2 Direction { get; private set; }
        public bool IsHeld { get; private set; }
        public bool RepositionMode { get; private set; }

        RectTransform _baseRect;
        RectTransform _knobRect;
        Canvas _canvas;
        float _knobRange;
        float _baseSize;
        int _pointerId = -1;
        bool _draggingBase;
        Vector2 _dragOffsetLocal;

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

        /// <summary>
        /// Settings menu: drag the whole stick to place it. Closing settings locks &amp; saves the position.
        /// </summary>
        public static void SetRepositionMode(bool enabled)
        {
            if (Instance != null)
                Instance.SetRepositionModeInternal(enabled);
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

            if (RepositionMode) return;

            if (!IsHeld) return;
            if (GameHud.Instance != null && GameHud.Instance.IsGamePaused)
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

            if (_knobRect != null) _knobRect.anchoredPosition = Vector2.zero;

            if (!enabled && RepositionMode)
                SetRepositionModeInternal(false);
        }

        void SetRepositionModeInternal(bool enabled)
        {
            if (RepositionMode == enabled) return;

            if (IsHeld) Release();
            RepositionMode = enabled && GameSave.UsesJoystickMovement;
            _draggingBase = false;
            Direction = Vector2.zero;

            if (_canvas != null)
                _canvas.sortingOrder = RepositionMode ? RepositionSortOrder : NormalSortOrder;

            if (!enabled && _baseRect != null)
                SaveCurrentPosition();

            // Ensure stick is visible while placing it (camp settings).
            if (RepositionMode && transform.childCount > 0)
                transform.GetChild(0).gameObject.SetActive(true);
        }

        void BuildUi()
        {
            EventSystemSetup.EnsureExists();

            var canvasGo = new GameObject("JoystickCanvas");
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = NormalSortOrder;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            _baseSize = 170f * UiScale;
            var knobSize = 72f * UiScale;
            _knobRange = 54f * UiScale;

            var baseGo = new GameObject("JoystickBase");
            baseGo.transform.SetParent(canvasGo.transform, false);
            _baseRect = baseGo.AddComponent<RectTransform>();
            // Center anchors so ScreenPointToLocalPoint matches anchoredPosition (anywhere on screen).
            _baseRect.anchorMin = new Vector2(0.5f, 0.5f);
            _baseRect.anchorMax = new Vector2(0.5f, 0.5f);
            _baseRect.pivot = new Vector2(0.5f, 0.5f);
            _baseRect.sizeDelta = new Vector2(_baseSize, _baseSize);
            _baseRect.anchoredPosition = ResolveDefaultOrSavedPosition();

            var baseImage = baseGo.AddComponent<Image>();
            if (StoneUi.Available && StoneUi.JoystickBg != null)
            {
                baseImage.sprite = StoneUi.JoystickBg;
                baseImage.color = Color.white;
                baseImage.preserveAspect = true;
            }
            else
            {
                baseImage.sprite = GetCircleSprite();
                baseImage.color = new Color(1f, 1f, 1f, 0.22f);
            }
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
            if (StoneUi.Available && StoneUi.JoystickKnob != null)
            {
                knobImage.sprite = StoneUi.JoystickKnob;
                knobImage.color = Color.white;
                knobImage.preserveAspect = true;
            }
            else
            {
                knobImage.sprite = GetCircleSprite();
                knobImage.color = new Color(1f, 1f, 1f, 0.42f);
            }
            knobImage.raycastTarget = false;
        }

        Vector2 ResolveDefaultOrSavedPosition()
        {
            // Old saves used bottom-right anchor space; discard them after space version change.
            if (PlayerPrefs.GetInt(PositionSpaceKey, 0) != PositionSpaceVersion)
            {
                PlayerPrefs.DeleteKey("zx_joystick_pos_x");
                PlayerPrefs.DeleteKey("zx_joystick_pos_y");
                PlayerPrefs.SetInt(PositionSpaceKey, PositionSpaceVersion);
                PlayerPrefs.Save();
            }

            if (GameSave.HasCustomJoystickPosition)
                return GameSave.JoystickAnchoredPosition;

            // Default: lower-right area in center-anchor space (1920x1080 reference).
            return new Vector2(620f, -280f);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!GameSave.UsesJoystickMovement) return;
            if (IsHeld) return;
            _pointerId = eventData.pointerId;
            IsHeld = true;

            if (RepositionMode)
            {
                _draggingBase = true;
                Direction = Vector2.zero;
                if (_knobRect != null) _knobRect.anchoredPosition = Vector2.zero;

                // Preserve grab offset so the stick doesn't jump under the finger.
                if (TryScreenToParentLocal(eventData.position, out var local))
                    _dragOffsetLocal = _baseRect.anchoredPosition - local;
                else
                    _dragOffsetLocal = Vector2.zero;

                MoveBaseToScreen(eventData.position);
                return;
            }

            // Stay centered until the player actually drags.
            if (_knobRect != null) _knobRect.anchoredPosition = Vector2.zero;
            Direction = Vector2.zero;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!GameSave.UsesJoystickMovement) return;
            if (!IsHeld || eventData.pointerId != _pointerId) return;

            if (RepositionMode && _draggingBase)
            {
                MoveBaseToScreen(eventData.position);
                return;
            }

            UpdateKnob(eventData.position);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId) return;

            if (RepositionMode)
                SaveCurrentPosition();

            Release();
        }

        public bool IsPointerOver(Vector2 screenPos)
        {
            if (!GameSave.UsesJoystickMovement || _baseRect == null) return false;
            return RectTransformUtility.RectangleContainsScreenPoint(_baseRect, screenPos, null);
        }

        bool TryScreenToParentLocal(Vector2 screenPos, out Vector2 local)
        {
            local = default;
            if (_baseRect == null || _baseRect.parent == null) return false;
            var parent = _baseRect.parent as RectTransform;
            if (parent == null) return false;
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent, screenPos, null, out local);
        }

        void MoveBaseToScreen(Vector2 screenPos)
        {
            if (!TryScreenToParentLocal(screenPos, out var local)) return;

            local += _dragOffsetLocal;

            var parent = _baseRect.parent as RectTransform;
            if (parent == null) return;

            // Keep the stick fully on-screen with a small margin (center-anchor space).
            var half = _baseSize * 0.5f;
            var margin = 24f;
            var halfW = parent.rect.width * 0.5f;
            var halfH = parent.rect.height * 0.5f;
            var minX = -halfW + half + margin;
            var maxX = halfW - half - margin;
            var minY = -halfH + half + margin;
            var maxY = halfH - half - margin;

            local.x = Mathf.Clamp(local.x, minX, maxX);
            local.y = Mathf.Clamp(local.y, minY, maxY);
            _baseRect.anchoredPosition = local;
        }

        void SaveCurrentPosition()
        {
            if (_baseRect == null) return;
            GameSave.JoystickAnchoredPosition = _baseRect.anchoredPosition;
            PlayerPrefs.SetInt(PositionSpaceKey, PositionSpaceVersion);
            PlayerPrefs.Save();
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
            _draggingBase = false;
            _dragOffsetLocal = Vector2.zero;
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
