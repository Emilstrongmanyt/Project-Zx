using ProjectZx.Combat;
using ProjectZx.Core;
using ProjectZx.HeroEditor;
using ProjectZx.World;
using UnityEngine;

namespace ProjectZx.Player
{
    /// <summary>
    /// Inactive hero in survival: follows the player, uses their own class loadout combat,
    /// and vacuums nearby loot for the leader (at 20% damage via PlayerStats).
    /// </summary>
    [RequireComponent(typeof(PlayerStats))]
    public class CompanionFollower : MonoBehaviour
    {
        const float FollowDistance = 1.55f;
        const float FollowSideOffset = 0.65f;
        // ~25% slower than the previous companion so they trail a bit behind the leader.
        const float MoveSpeed = 5.2f * 0.75f;
        const float ArriveSnap = 0.08f;
        const float LootScanInterval = 0.12f;
        /// <summary>If farther than this from the leader on a wrap map, hard-snap beside them.</summary>
        const float WrapResyncDistance = 8f;

        Transform _leader;
        PlayerStats _leaderStats;
        PlayerStats _stats;
        Rigidbody2D _rb;
        SpriteRenderer _renderer;
        HeroEditorCharacterView _heroView;
        Sprite _idle;
        Sprite _walkA;
        Sprite _walkB;
        bool _facesRightByDefault;
        float _walkAnimTimer;
        bool _useWalkFrameA = true;
        float _lootTimer;
        Vector2 _lastLeaderDir = Vector2.left;

        public void Bind(Transform leader, PlayerStats leaderStats, PlayableHero hero)
        {
            _leader = leader;
            _leaderStats = leaderStats;
            _stats = GetComponent<PlayerStats>();
            _rb = GetComponent<Rigidbody2D>();
            _renderer = GetComponent<SpriteRenderer>();
            _heroView = GetComponent<HeroEditorCharacterView>();

            var set = ArtLibrary.GetHeroSprites(hero);
            _idle = set.Idle;
            _walkA = set.WalkA != null ? set.WalkA : set.Idle;
            _walkB = set.WalkB != null ? set.WalkB : _walkA;
            _facesRightByDefault = set.FacesRightByDefault;

            if (_heroView != null && _heroView.IsReady)
            {
                if (_renderer != null) _renderer.enabled = false;
            }
            else if (_renderer != null)
            {
                _renderer.sprite = _idle;
            }

            if (_leader != null)
                SetWorldPosition((Vector2)_leader.position + Vector2.left * FollowDistance);
        }

        void FixedUpdate()
        {
            if (_leader == null || _leaderStats == null || _leaderStats.IsDead)
            {
                ApplyIdleSprite();
                return;
            }

            // Physics step: stay glued through wraps (leader moves in FixedUpdate).
            FollowLeader();
        }

        void Update()
        {
            if (_leader == null || _leaderStats == null || _leaderStats.IsDead)
            {
                ApplyIdleSprite();
                return;
            }

            UpdateFacingAndWalk();
            CollectNearbyLoot();
        }

        Vector2 GetWorldPosition()
        {
            if (_rb != null) return _rb.position;
            return transform.position;
        }

        void SetWorldPosition(Vector2 pos)
        {
            if (_rb != null)
            {
                _rb.linearVelocity = Vector2.zero;
                _rb.position = pos;
            }

            transform.position = new Vector3(pos.x, pos.y, transform.position.z);
        }

        void FollowLeader()
        {
            var leaderPos = (Vector2)_leader.position;
            // Prefer leader rigidbody if present (matches teleport frame).
            var leaderRb = _leader.GetComponent<Rigidbody2D>();
            if (leaderRb != null) leaderPos = leaderRb.position;

            var selfPos = GetWorldPosition();
            var toLeader = leaderPos - selfPos;
            var distToLeader = toLeader.magnitude;

            // After a wrap (or any large separation), snap beside the leader immediately.
            if (ArenaBounds.WorldWrapEnabled && distToLeader > WrapResyncDistance)
            {
                SnapBesideLeader(leaderPos);
                return;
            }

            if (toLeader.sqrMagnitude > 0.04f)
                _lastLeaderDir = toLeader.normalized;

            var behind = -_lastLeaderDir;
            var side = new Vector2(-behind.y, behind.x);
            var target = leaderPos + behind * FollowDistance + side * FollowSideOffset;

            var toTarget = target - selfPos;
            var dist = toTarget.magnitude;
            if (dist <= ArriveSnap)
            {
                SetWorldPosition(target);
                return;
            }

            var step = MoveSpeed * (_stats != null ? _stats.RunSpeedMultiplier : 1f)
                       * GameSave.SpeedMultiplier * Time.fixedDeltaTime;
            if (step >= dist)
                SetWorldPosition(target);
            else
                SetWorldPosition(selfPos + toTarget / dist * step);
        }

        void SnapBesideLeader(Vector2 leaderPos)
        {
            var dir = _lastLeaderDir.sqrMagnitude > 0.01f ? _lastLeaderDir : Vector2.right;
            var behind = -dir;
            var side = new Vector2(-behind.y, behind.x);
            SetWorldPosition(leaderPos + behind * FollowDistance + side * FollowSideOffset);
        }

        /// <summary>
        /// Same wrap offset as the leader — keeps assist hero continuous through the border.
        /// </summary>
        public void TeleportWithLeader(Vector2 wrapDelta)
        {
            if (wrapDelta.sqrMagnitude < 0.25f) return;
            SetWorldPosition(GetWorldPosition() + wrapDelta);

            // If still far (stale transform), hard-snap to follow slot.
            if (_leader == null) return;
            var leaderPos = (Vector2)_leader.position;
            var leaderRb = _leader.GetComponent<Rigidbody2D>();
            if (leaderRb != null) leaderPos = leaderRb.position;
            if (Vector2.Distance(GetWorldPosition(), leaderPos) > WrapResyncDistance)
                SnapBesideLeader(leaderPos);
        }

        void UpdateFacingAndWalk()
        {
            if (_leader == null) return;
            if (IsBusyAttacking()) return;

            var leaderPos = (Vector2)_leader.position;
            var leaderRb = _leader.GetComponent<Rigidbody2D>();
            if (leaderRb != null) leaderPos = leaderRb.position;

            var moving = (leaderPos - GetWorldPosition()).sqrMagnitude > 0.12f;
            var faceRight = _lastLeaderDir.x >= 0f;

            if (_heroView != null && _heroView.IsReady)
            {
                _heroView.SetFacing(faceRight);
                _heroView.SetMoving(moving);
                return;
            }

            if (_renderer == null) return;
            if (moving)
            {
                _renderer.flipX = _facesRightByDefault ? !faceRight : faceRight;

                _walkAnimTimer += Time.deltaTime;
                if (_walkAnimTimer >= 0.16f)
                {
                    _walkAnimTimer = 0f;
                    _useWalkFrameA = !_useWalkFrameA;
                }

                _renderer.sprite = _useWalkFrameA ? _walkA : _walkB;
            }
            else
            {
                ApplyIdleSprite();
            }
        }

        void ApplyIdleSprite()
        {
            _walkAnimTimer = 0f;
            _useWalkFrameA = true;
            if (_heroView != null && _heroView.IsReady)
            {
                _heroView.SetMoving(false);
                return;
            }

            if (_renderer == null) return;
            _renderer.sprite = _idle;
        }

        bool IsBusyAttacking()
        {
            var batter = GetComponent<PlayerCombat>();
            if (batter != null && batter.IsSwinging) return true;
            var spearman = GetComponent<SpearmanCombat>();
            if (spearman != null && spearman.IsThrusting) return true;
            var samurai = GetComponent<SamuraiCombat>();
            if (samurai != null && samurai.IsSwiping) return true;
            var bowman = GetComponent<BowmanCombat>();
            if (bowman != null && bowman.IsDrawing) return true;
            var magician = GetComponent<MagicianCombat>();
            if (magician != null && magician.IsCasting) return true;
            return false;
        }

        void CollectNearbyLoot()
        {
            _lootTimer -= Time.deltaTime;
            if (_lootTimer > 0f) return;
            _lootTimer = LootScanInterval;

            var credit = _leaderStats != null
                ? _leaderStats.LootCreditTarget
                : _stats != null ? _stats.LootCreditTarget : null;
            if (credit == null || credit.IsDead) return;

            var range = 1.45f * credit.EffectiveLootRangeMultiplier;
            var crystalRange = range * 1.25f;
            var pickups = Object.FindObjectsByType<LootPickup>();
            for (var i = 0; i < pickups.Length; i++)
            {
                var pickup = pickups[i];
                if (pickup == null) continue;
                var maxRange = pickup.Type == PickupType.EpicCrystal ? crystalRange : range;
                if (Vector2.Distance(GetWorldPosition(), pickup.transform.position) > maxRange) continue;
                pickup.CollectFor(credit);
            }
        }
    }
}
