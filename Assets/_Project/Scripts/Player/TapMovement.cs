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
        const float ArrivalDistance = 0.12f;
        const float NpcTapRadius = 1.8f;

        [SerializeField] float baseSpeed = 4.5f;
        [SerializeField] bool allowNpcInteraction = true;

        Vector2? _moveTarget;
        NpcInteractable _pendingNpc;
        Camera _camera;
        Rigidbody2D _rb;
        SpriteRenderer _renderer;
        Sprite _idle;
        Sprite _walk;
        int _lastHandledTouchId = -1;
        int _lastHandledFrame = -1;

        public void Configure(bool npcInteraction) => allowNpcInteraction = npcInteraction;

        void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            Touch.onFingerDown += OnFingerDown;
        }

        void OnDisable()
        {
            Touch.onFingerDown -= OnFingerDown;
        }

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _renderer = GetComponent<SpriteRenderer>();
            _camera = Camera.main;
            _idle = ArtLibrary.PlayerIdle;
            _walk = ArtLibrary.PlayerWalk;

            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        void Update()
        {
            ReadMouse();
            UpdateSprite();
        }

        void FixedUpdate()
        {
            if (_moveTarget == null)
            {
                _rb.linearVelocity = Vector2.zero;
                TryCompletePendingNpcInteract();
                return;
            }

            var speed = baseSpeed * GameSave.SpeedMultiplier;
            var target = _moveTarget.Value;
            var delta = target - _rb.position;

            if (delta.magnitude < ArrivalDistance)
            {
                _rb.MovePosition(target);
                _rb.linearVelocity = Vector2.zero;
                _moveTarget = null;
                TryCompletePendingNpcInteract();
                return;
            }

            var step = speed * Time.fixedDeltaTime;
            var current = _rb.position;
            var next = Vector2.MoveTowards(current, target, step);
            _rb.MovePosition(next);
            _rb.linearVelocity = (next - current) / Time.fixedDeltaTime;
            TryCompletePendingNpcInteract();
        }

        void OnFingerDown(Finger finger)
        {
            if (finger == null) return;
            TryTapToMove(finger.screenPosition, finger.index);
        }

        void ReadMouse()
        {
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return;

            var mouse = Mouse.current;
            if (mouse?.leftButton.wasPressedThisFrame != true) return;
            TryTapToMove(mouse.position.ReadValue(), -1);
        }

        void TryTapToMove(Vector2 screenPos, int touchId)
        {
            if (_lastHandledFrame == Time.frameCount && _lastHandledTouchId == touchId) return;
            _lastHandledFrame = Time.frameCount;
            _lastHandledTouchId = touchId;

            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return;
            if (IsPointerOverBlockingUi(screenPos)) return;

            var world = ScreenToWorld(screenPos);

            if (allowNpcInteraction)
            {
                var npc = FindNpcAtTap(world);
                if (npc != null)
                {
                    if (npc.TryInteract(transform))
                    {
                        ClearMovement();
                        return;
                    }

                    _pendingNpc = npc;
                    _moveTarget = npc.transform.position;
                    return;
                }
            }

            _pendingNpc = null;
            _moveTarget = world;
        }

        void TryCompletePendingNpcInteract()
        {
            if (_pendingNpc == null) return;
            if (!NpcInRange(_pendingNpc)) return;

            if (_pendingNpc.TryInteract(transform))
                ClearMovement();
        }

        bool NpcInRange(NpcInteractable npc)
        {
            if (npc == null) return false;
            return Vector2.Distance(transform.position, npc.transform.position) <= npc.InteractRangeWorld;
        }

        static NpcInteractable FindNpcAtTap(Vector2 worldPos)
        {
            var npcs = UnityEngine.Object.FindObjectsByType<NpcInteractable>();
            NpcInteractable best = null;
            var bestDist = float.MaxValue;

            foreach (var npc in npcs)
            {
                if (npc == null) continue;
                var dist = Vector2.Distance(worldPos, npc.transform.position);
                if (dist > NpcTapRadius || dist >= bestDist) continue;
                bestDist = dist;
                best = npc;
            }

            return best;
        }

        void ClearMovement()
        {
            _moveTarget = null;
            _pendingNpc = null;
            _rb.linearVelocity = Vector2.zero;
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
            var z = Mathf.Abs(_camera.transform.position.z);
            return _camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, z));
        }

        void UpdateSprite()
        {
            if (_renderer == null) return;
            var combat = GetComponent<PlayerCombat>();
            if (combat != null && combat.IsSwinging) return;

            var moving = _moveTarget != null || _rb.linearVelocity.sqrMagnitude > 0.01f;
            _renderer.sprite = moving ? _walk : _idle;
            if (moving && _rb.linearVelocity.x != 0f)
                _renderer.flipX = _rb.linearVelocity.x < 0f;
        }
    }
}