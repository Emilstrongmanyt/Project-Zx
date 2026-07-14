using System.Collections.Generic;
using ProjectZx.Combat;
using ProjectZx.Core;
using ProjectZx.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace ProjectZx.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class TapMovement : MonoBehaviour
    {
        const float DragThresholdPixels = 28f;
        const float ArrivalDistance = 0.08f;

        [SerializeField] float baseSpeed = 4.5f;
        [SerializeField] bool allowNpcInteraction = true;

        enum InputMode { None, MoveToPoint, HoldDirection }

        InputMode _mode = InputMode.None;
        Vector2? _moveTarget;
        Vector2 _holdDirection;
        Vector2 _touchStartScreen;
        Camera _camera;
        Rigidbody2D _rb;
        SpriteRenderer _renderer;
        Sprite _idle;
        Sprite _walk;

        void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _renderer = GetComponent<SpriteRenderer>();
            _camera = Camera.main;
            _idle = ArtLibrary.PlayerIdle;
            _walk = ArtLibrary.PlayerWalk;
        }

        void Update()
        {
            ReadPointer();
            UpdateSprite();
        }

        void FixedUpdate()
        {
            var speed = baseSpeed * GameSave.SpeedMultiplier;

            if (_mode == InputMode.HoldDirection)
            {
                _rb.linearVelocity = _holdDirection * speed;
                return;
            }

            if (_mode != InputMode.MoveToPoint || _moveTarget == null)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            var delta = _moveTarget.Value - (Vector2)transform.position;
            if (delta.magnitude < ArrivalDistance)
            {
                _moveTarget = null;
                _mode = InputMode.None;
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            _rb.linearVelocity = delta.normalized * speed;
        }

        void ReadPointer()
        {
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return;

            if (Touch.activeTouches.Count > 0)
            {
                var touch = Touch.activeTouches[0];
                HandlePointer(ToLegacyPhase(touch.phase), touch.screenPosition, touch.touchId);
                return;
            }

            var mouse = Mouse.current;
            if (mouse == null) return;

            var pos = mouse.position.ReadValue();
            if (mouse.leftButton.wasPressedThisFrame)
                HandlePointer(TouchPhase.Began, pos, -1);
            else if (mouse.leftButton.wasReleasedThisFrame)
                HandlePointer(TouchPhase.Ended, pos, -1);
            else if (mouse.leftButton.isPressed)
                HandlePointer(TouchPhase.Moved, pos, -1);
        }

        static TouchPhase ToLegacyPhase(UnityEngine.InputSystem.TouchPhase phase)
        {
            return phase switch
            {
                UnityEngine.InputSystem.TouchPhase.Began => TouchPhase.Began,
                UnityEngine.InputSystem.TouchPhase.Moved => TouchPhase.Moved,
                UnityEngine.InputSystem.TouchPhase.Stationary => TouchPhase.Stationary,
                UnityEngine.InputSystem.TouchPhase.Ended => TouchPhase.Ended,
                UnityEngine.InputSystem.TouchPhase.Canceled => TouchPhase.Canceled,
                _ => TouchPhase.Canceled
            };
        }

        void HandlePointer(TouchPhase phase, Vector2 screenPos, int fingerId)
        {
            if (IsPointerOverBlockingUi(screenPos)) return;

            var world = ScreenToWorld(screenPos);

            if (phase == TouchPhase.Began)
            {
                _touchStartScreen = screenPos;
                if (allowNpcInteraction && TryInteractWithNpc(world))
                    return;

                _mode = InputMode.MoveToPoint;
                _moveTarget = world;
                _holdDirection = Vector2.zero;
                return;
            }

            if (phase == TouchPhase.Ended || phase == TouchPhase.Canceled)
            {
                if (_mode == InputMode.HoldDirection)
                {
                    _mode = InputMode.None;
                    _holdDirection = Vector2.zero;
                }
                return;
            }

            if (_mode != InputMode.MoveToPoint && _mode != InputMode.HoldDirection)
                return;

            var drag = Vector2.Distance(screenPos, _touchStartScreen);
            if (drag >= DragThresholdPixels)
            {
                _mode = InputMode.HoldDirection;
                _moveTarget = null;
            }

            if (_mode == InputMode.HoldDirection)
            {
                var dir = world - (Vector2)transform.position;
                _holdDirection = dir.sqrMagnitude > 0.01f ? dir.normalized : Vector2.zero;
            }
            else if (_mode == InputMode.MoveToPoint)
            {
                _moveTarget = world;
            }
        }

        bool TryInteractWithNpc(Vector2 worldPos)
        {
            const float tapRadius = 1.1f;
            var npcs = UnityEngine.Object.FindObjectsByType<NpcInteractable>();
            NpcInteractable best = null;
            var bestTapDist = float.MaxValue;

            foreach (var npc in npcs)
            {
                if (npc == null) continue;
                var tapDist = Vector2.Distance(worldPos, npc.transform.position);
                if (tapDist > tapRadius || tapDist >= bestTapDist) continue;
                bestTapDist = tapDist;
                best = npc;
            }

            if (best == null || !best.TryInteract(transform)) return false;

            _mode = InputMode.None;
            _moveTarget = null;
            _holdDirection = Vector2.zero;
            _rb.linearVelocity = Vector2.zero;
            return true;
        }

        bool IsPointerOverBlockingUi(Vector2 screenPos)
        {
            if (EventSystem.current == null) return false;

            var eventData = new PointerEventData(EventSystem.current) { position = screenPos };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            foreach (var result in results)
            {
                if (result.gameObject == null || !result.gameObject.activeInHierarchy) continue;
                if (result.gameObject.GetComponentInParent<Button>() != null) return true;

                var image = result.gameObject.GetComponent<Image>();
                if (image == null || !image.raycastTarget) continue;
                var name = result.gameObject.name;
                if (name == "ShopPanel" || name == "MapPanel") return true;
            }

            return false;
        }

        Vector2 ScreenToWorld(Vector2 screen)
        {
            var z = -_camera.transform.position.z;
            return _camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, z));
        }

        void UpdateSprite()
        {
            if (_renderer == null) return;
            var combat = GetComponent<PlayerCombat>();
            if (combat != null && combat.IsSwinging) return;

            var moving = _rb.linearVelocity.sqrMagnitude > 0.01f;
            _renderer.sprite = moving ? _walk : _idle;
            if (moving && _rb.linearVelocity.x != 0f)
                _renderer.flipX = _rb.linearVelocity.x < 0f;
        }
    }
}