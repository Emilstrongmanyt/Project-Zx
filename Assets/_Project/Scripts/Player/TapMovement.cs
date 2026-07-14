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
        const float ArrivalDistance = 0.08f;

        [SerializeField] float baseSpeed = 4.5f;
        [SerializeField] bool allowNpcInteraction = true;

        Vector2? _moveTarget;
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
            if (_moveTarget == null)
            {
                _rb.linearVelocity = Vector2.zero;
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
                return;
            }

            var step = speed * Time.fixedDeltaTime;
            var current = _rb.position;
            var next = Vector2.MoveTowards(current, target, step);
            _rb.MovePosition(next);
            _rb.linearVelocity = (next - current) / Time.fixedDeltaTime;
        }

        void ReadPointer()
        {
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return;

            if (Touch.activeTouches.Count > 0)
            {
                foreach (var touch in Touch.activeTouches)
                {
                    if (touch.phase != UnityEngine.InputSystem.TouchPhase.Began) continue;
                    TryTapToMove(touch.screenPosition);
                }
                return;
            }

            var mouse = Mouse.current;
            if (mouse?.leftButton.wasPressedThisFrame == true)
                TryTapToMove(mouse.position.ReadValue());
        }

        void TryTapToMove(Vector2 screenPos)
        {
            if (IsPointerOverBlockingUi(screenPos)) return;

            var world = ScreenToWorld(screenPos);
            if (allowNpcInteraction && TryInteractWithNpc(world)) return;

            _moveTarget = world;
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

            _moveTarget = null;
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