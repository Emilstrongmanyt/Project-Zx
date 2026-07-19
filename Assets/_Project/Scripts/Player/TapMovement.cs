using System.Collections.Generic;
using ProjectZx.Combat;
using ProjectZx.Core;
using ProjectZx.UI;
using ProjectZx.Enemies;
using ProjectZx.World;
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
        const float NpcTapRadius = 2.4f;
        const float CastSkin = 0.04f;
        const float StuckClearDelay = 0.35f;

        [SerializeField] float baseSpeed = 4.5f;
        [SerializeField] bool allowNpcInteraction = true;

        Vector2? _moveTarget;
        NpcInteractable _pendingNpc;
        ArenaDoor _pendingDoor;
        ArenaGateway _pendingGateway;
        Camera _camera;
        Rigidbody2D _rb;
        SpriteRenderer _renderer;
        Sprite _idle;
        Sprite _walk;
        Sprite _walkA;
        Sprite _walkB;
        PlayableHero _hero;
        bool _facesRightByDefault;
        float _walkAnimTimer;
        bool _useWalkFrameA = true;
        int _chaseTouchId = -1;
        bool _chaseMouse;
        float _blockedTimer;
        readonly List<RaycastHit2D> _castHits = new();

        public void Configure(bool npcInteraction, PlayableHero hero = PlayableHero.RollZy)
        {
            allowNpcInteraction = npcInteraction;
            _hero = GameSave.SanitizeHero(hero);
            ApplyHeroSprites();
        }

        void ApplyHeroSprites()
        {
            var set = ArtLibrary.GetHeroSprites(_hero);
            _idle = set.Idle ?? ArtLibrary.PlayerIdle;
            _walkA = set.WalkA ?? ArtLibrary.PlayerWalk;
            _walkB = set.WalkB ?? _walkA;
            _walk = _walkA;
            _facesRightByDefault = set.FacesRightByDefault;
            if (_renderer != null) _renderer.sprite = _idle;
        }

        void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            Touch.onFingerDown += OnFingerDown;
            Touch.onFingerMove += OnFingerMove;
            Touch.onFingerUp += OnFingerUp;
        }

        void OnDisable()
        {
            Touch.onFingerDown -= OnFingerDown;
            Touch.onFingerMove -= OnFingerMove;
            Touch.onFingerUp -= OnFingerUp;
        }

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _renderer = GetComponent<SpriteRenderer>();
            _camera = Camera.main;

            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.useFullKinematicContacts = true;
        }

        void Update()
        {
            ReadMouse();
            UpdateSprite();
        }

        void FixedUpdate()
        {
            if (GameSave.UsesJoystickMovement)
            {
                var joystick = MovementJoystick.Instance;
                var joyDir = joystick != null ? joystick.Direction : Vector2.zero;
                if (joyDir.sqrMagnitude > 0.01f)
                {
                    _moveTarget = null;
                    _pendingNpc = null;
                    _pendingDoor = null;
                    _pendingGateway = null;
                    _chaseTouchId = -1;
                    _chaseMouse = false;
                    _blockedTimer = 0f;
                    MoveByDelta(joyDir.normalized * GetSpeed() * Time.fixedDeltaTime);
                    return;
                }

                _rb.linearVelocity = Vector2.zero;
                TryCompletePendingNpcInteract();
                TryCompletePendingDoor();
                TryCompletePendingGateway();
                return;
            }

            // Tap / hold movement mode only.
            if (_moveTarget == null)
            {
                _rb.linearVelocity = Vector2.zero;
                TryCompletePendingNpcInteract();
                TryCompletePendingDoor();
                TryCompletePendingGateway();
                return;
            }

            var target = _moveTarget.Value;
            var delta = target - _rb.position;

            if (delta.magnitude < ArrivalDistance)
            {
                _rb.linearVelocity = Vector2.zero;
                if (_chaseTouchId < 0 && !_chaseMouse)
                    _moveTarget = null;
                TryCompletePendingNpcInteract();
                TryCompletePendingDoor();
                TryCompletePendingGateway();
                return;
            }

            var step = GetSpeed() * Time.fixedDeltaTime;
            if (MoveByDelta(Vector2.ClampMagnitude(delta, step)))
                _blockedTimer = 0f;
            else
                _blockedTimer += Time.fixedDeltaTime;

            if (_blockedTimer >= StuckClearDelay && _chaseTouchId < 0 && !_chaseMouse)
                _moveTarget = null;

            TryCompletePendingNpcInteract();
            TryCompletePendingDoor();
            TryCompletePendingGateway();
        }

        float GetSpeed()
        {
            var stats = GetComponent<PlayerStats>();
            var runSpeed = stats != null ? stats.RunSpeedMultiplier : 1f;
            return baseSpeed * GameSave.SpeedMultiplier * runSpeed;
        }

        bool MoveByDelta(Vector2 delta)
        {
            if (delta.sqrMagnitude < 0.00001f)
            {
                _rb.linearVelocity = Vector2.zero;
                return false;
            }

            if (TryMoveDelta(delta, out var hitNormal))
                return true;

            var distance = delta.magnitude;
            var direction = delta / distance;

            if (hitNormal.sqrMagnitude > 0.0001f)
            {
                var slide = direction - Vector2.Dot(direction, hitNormal) * hitNormal;
                if (slide.sqrMagnitude > 0.0001f && TryMoveDelta(slide.normalized * distance, out _))
                    return true;
            }

            var perp = new Vector2(-direction.y, direction.x) * distance;
            if (TryMoveDelta(perp, out _) || TryMoveDelta(-perp, out _))
                return true;

            var blendedA = (direction + perp.normalized).normalized * distance * 0.75f;
            if (TryMoveDelta(blendedA, out _))
                return true;

            var blendedB = (direction - perp.normalized).normalized * distance * 0.75f;
            if (TryMoveDelta(blendedB, out _))
                return true;

            _rb.linearVelocity = Vector2.zero;
            return false;
        }

        bool TryMoveDelta(Vector2 delta, out Vector2 hitNormal)
        {
            hitNormal = Vector2.zero;
            if (delta.sqrMagnitude < 0.00001f) return false;

            var distance = delta.magnitude;
            var direction = delta / distance;
            var filter = new ContactFilter2D();
            filter.useTriggers = false;
            filter.useLayerMask = false;

            _castHits.Clear();
            var hitCount = _rb.Cast(direction, filter, _castHits, distance);
            var allowed = distance;
            if (hitCount > 0)
            {
                var blockingIndex = FindFirstBlockingHit(hitCount);
                if (blockingIndex < 0)
                    allowed = distance;
                else
                {
                    hitNormal = _castHits[blockingIndex].normal;
                    allowed = Mathf.Max(0f, _castHits[blockingIndex].distance - CastSkin);
                }
            }

            if (allowed <= 0.0001f) return false;

            _rb.MovePosition(_rb.position + direction * allowed);
            _rb.linearVelocity = direction * (allowed / Time.fixedDeltaTime);
            return true;
        }

        int FindFirstBlockingHit(int hitCount)
        {
            for (var i = 0; i < hitCount; i++)
            {
                var col = _castHits[i].collider;
                if (col == null) continue;

                var enemy = col.GetComponent<EnemyActor>() ?? col.GetComponentInParent<EnemyActor>();
                if (enemy != null && !enemy.IsAlive) continue;

                return i;
            }

            return -1;
        }

        void OnFingerDown(Finger finger)
        {
            if (finger == null) return;
            BeginPointer(finger.screenPosition, finger.index);
        }

        void OnFingerMove(Finger finger)
        {
            if (finger == null || finger.index != _chaseTouchId) return;
            UpdateChaseTarget(finger.screenPosition);
        }

        void OnFingerUp(Finger finger)
        {
            if (finger == null || finger.index != _chaseTouchId) return;
            EndChase();
        }

        void ReadMouse()
        {
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return;

            var mouse = Mouse.current;
            if (mouse == null) return;

            if (mouse.leftButton.wasPressedThisFrame)
                BeginPointer(mouse.position.ReadValue(), -1);

            if (_chaseMouse && mouse.leftButton.isPressed)
                UpdateChaseTarget(mouse.position.ReadValue());

            if (_chaseMouse && mouse.leftButton.wasReleasedThisFrame)
                EndChase();
        }

        void BeginPointer(Vector2 screenPos, int touchId)
        {
            if (GameHud.Instance != null && GameHud.Instance.IsChoosingUpgrade) return;
            if (MovementJoystick.Instance != null && MovementJoystick.Instance.IsPointerOver(screenPos)) return;
            if (IsPointerOverBlockingUi(screenPos)) return;

            // In joystick mode, taps only walk-to/interact with NPCs and doors (no free move targets).
            if (GameSave.UsesJoystickMovement)
            {
                TrySetMoveTarget(screenPos, isChase: false, movementAllowed: false);
                return;
            }

            _chaseTouchId = touchId;
            _chaseMouse = touchId < 0;
            TrySetMoveTarget(screenPos, isChase: false, movementAllowed: true);
        }

        void UpdateChaseTarget(Vector2 screenPos)
        {
            if (!GameSave.UsesTapHoldMovement) return;
            if (GameHud.Instance != null && GameHud.Instance.IsChoosingUpgrade) return;
            if (MovementJoystick.Instance != null && MovementJoystick.Instance.IsPointerOver(screenPos)) return;
            if (IsPointerOverBlockingUi(screenPos)) return;
            TrySetMoveTarget(screenPos, true, movementAllowed: true);
        }

        void EndChase()
        {
            _chaseTouchId = -1;
            _chaseMouse = false;
        }

        void TrySetMoveTarget(Vector2 screenPos, bool isChase, bool movementAllowed)
        {
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return;

            var world = ScreenToWorld(screenPos);

            if (allowNpcInteraction && TryHandleNpcTap(world, isChase))
                return;

            if (!isChase)
            {
                var door = FindDoorAtTap(world);
                if (door != null)
                {
                    if (door.TryEnter(transform))
                    {
                        ClearMovement();
                        return;
                    }

                    if (!movementAllowed) return;

                    _pendingDoor = door;
                    _pendingGateway = null;
                    _pendingNpc = null;
                    _moveTarget = door.transform.position;
                    return;
                }

                var gateway = FindGatewayAtTap(world);
                if (gateway != null)
                {
                    if (gateway.TryEnter(transform))
                    {
                        ClearMovement();
                        return;
                    }

                    if (!movementAllowed) return;

                    _pendingGateway = gateway;
                    _pendingDoor = null;
                    _pendingNpc = null;
                    _moveTarget = gateway.transform.position;
                    return;
                }
            }

            if (!movementAllowed) return;

            if (isChase)
            {
                _pendingNpc = null;
                _pendingDoor = null;
                _pendingGateway = null;
            }

            if (IsWaterAt(world)) return;

            _moveTarget = world;
        }

        static bool IsWaterAt(Vector2 world)
        {
            var hits = Physics2D.OverlapPointAll(world);
            foreach (var hit in hits)
            {
                if (hit == null) continue;
                if (hit.GetComponent<WaterTile>() != null || hit.GetComponentInParent<WaterTile>() != null)
                    return true;
            }

            return false;
        }

        bool TryHandleNpcTap(Vector2 world, bool isChase)
        {
            var npc = FindNpcAtTap(world);
            if (npc == null) return false;

            if (npc.TryInteract(transform))
            {
                ClearMovement();
                return true;
            }

            if (isChase) return false;

            _pendingNpc = npc;
            _pendingDoor = null;
            _pendingGateway = null;
            _moveTarget = npc.transform.position;
            return true;
        }

        void TryCompletePendingNpcInteract()
        {
            if (_pendingNpc == null) return;
            if (!NpcInRange(_pendingNpc)) return;

            if (_pendingNpc.TryInteract(transform))
                ClearMovement();
        }

        void TryCompletePendingDoor()
        {
            if (_pendingDoor == null) return;
            if (Vector2.Distance(transform.position, _pendingDoor.transform.position) > 2.2f) return;
            if (_pendingDoor.TryEnter(transform))
                ClearMovement();
        }

        void TryCompletePendingGateway()
        {
            if (_pendingGateway == null) return;
            if (Vector2.Distance(transform.position, _pendingGateway.transform.position) > 2.2f) return;
            if (_pendingGateway.TryEnter(transform))
                ClearMovement();
        }

        static ArenaDoor FindDoorAtTap(Vector2 worldPos)
        {
            var doors = UnityEngine.Object.FindObjectsByType<ArenaDoor>();
            ArenaDoor best = null;
            var bestDist = float.MaxValue;

            foreach (var door in doors)
            {
                if (door == null) continue;
                var dist = Vector2.Distance(worldPos, door.transform.position);
                if (dist > NpcTapRadius || dist >= bestDist) continue;
                bestDist = dist;
                best = door;
            }

            return best;
        }

        static ArenaGateway FindGatewayAtTap(Vector2 worldPos)
        {
            var gateways = UnityEngine.Object.FindObjectsByType<ArenaGateway>();
            ArenaGateway best = null;
            var bestDist = float.MaxValue;

            foreach (var gateway in gateways)
            {
                if (gateway == null) continue;
                var dist = Vector2.Distance(worldPos, gateway.transform.position);
                if (dist > NpcTapRadius || dist >= bestDist) continue;
                bestDist = dist;
                best = gateway;
            }

            return best;
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
            _pendingDoor = null;
            _pendingGateway = null;
            _chaseTouchId = -1;
            _chaseMouse = false;
            _blockedTimer = 0f;
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
                if (name == "ShopPanel" || name == "LoadoutPanel" || name == "StatsPanel" || name == "AchievementsPanel" || name == "MapPanel" || name == "LevelUpPanel" || name == "CampfirePanel" || name == "RetreatPanel")
                    return true;
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

            var batter = GetComponent<PlayerCombat>();
            if (batter != null && batter.IsSwinging) return;
            var spearman = GetComponent<SpearmanCombat>();
            if (spearman != null && spearman.IsThrusting) return;
            var samurai = GetComponent<SamuraiCombat>();
            if (samurai != null && samurai.IsSwiping) return;
            var bowman = GetComponent<BowmanCombat>();
            if (bowman != null && bowman.IsDrawing) return;
            var magician = GetComponent<MagicianCombat>();
            if (magician != null && magician.IsCasting) return;

            var joyDir = GameSave.UsesJoystickMovement && MovementJoystick.Instance != null
                ? MovementJoystick.Instance.Direction
                : Vector2.zero;
            var moving = joyDir.sqrMagnitude > 0.01f || _moveTarget != null || _rb.linearVelocity.sqrMagnitude > 0.01f;
            if (!moving)
            {
                _walkAnimTimer = 0f;
                _useWalkFrameA = true;
                _renderer.sprite = _idle;
                return;
            }

            _walkAnimTimer -= Time.deltaTime;
            if (_walkAnimTimer <= 0f)
            {
                _walkAnimTimer = 0.18f;
                _useWalkFrameA = !_useWalkFrameA;
            }

            _renderer.sprite = _useWalkFrameA ? _walkA : _walkB;

            var faceX = joyDir.sqrMagnitude > 0.01f
                ? joyDir.x
                : _moveTarget.HasValue
                    ? _moveTarget.Value.x - transform.position.x
                    : _rb.linearVelocity.x;

            if (faceX == 0f) return;

            _renderer.flipX = _facesRightByDefault ? faceX < 0f : faceX < 0f;
        }
    }
}