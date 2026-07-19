using ProjectZx.Combat;
using ProjectZx.Core;
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

        Transform _leader;
        PlayerStats _leaderStats;
        PlayerStats _stats;
        SpriteRenderer _renderer;
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
            _renderer = GetComponent<SpriteRenderer>();

            var set = ArtLibrary.GetHeroSprites(hero);
            _idle = set.Idle;
            _walkA = set.WalkA != null ? set.WalkA : set.Idle;
            _walkB = set.WalkB != null ? set.WalkB : _walkA;
            _facesRightByDefault = set.FacesRightByDefault;

            if (_renderer != null)
                _renderer.sprite = _idle;

            if (_leader != null)
                transform.position = _leader.position + (Vector3)(Vector2.left * FollowDistance);
        }

        void Update()
        {
            if (_leader == null || _leaderStats == null || _leaderStats.IsDead)
            {
                // Leader gone — idle in place.
                ApplyIdleSprite();
                return;
            }

            FollowLeader();
            UpdateFacingAndWalk();
            CollectNearbyLoot();
        }

        void FollowLeader()
        {
            var leaderPos = (Vector2)_leader.position;
            var leaderDelta = leaderPos - (Vector2)transform.position;
            if (leaderDelta.sqrMagnitude > 0.04f)
                _lastLeaderDir = leaderDelta.normalized;

            // Prefer a slot slightly behind and to the side of the leader.
            var behind = -_lastLeaderDir;
            var side = new Vector2(-behind.y, behind.x);
            var target = leaderPos + behind * FollowDistance + side * FollowSideOffset;

            var toTarget = target - (Vector2)transform.position;
            var dist = toTarget.magnitude;
            if (dist <= ArriveSnap)
            {
                transform.position = target;
                return;
            }

            var step = MoveSpeed * (_stats != null ? _stats.RunSpeedMultiplier : 1f)
                       * GameSave.SpeedMultiplier * Time.deltaTime;
            if (step >= dist)
                transform.position = target;
            else
                transform.position = (Vector2)transform.position + toTarget / dist * step;
        }

        void UpdateFacingAndWalk()
        {
            if (_renderer == null || _leader == null) return;
            if (IsBusyAttacking()) return;

            var moving = ((Vector2)_leader.position - (Vector2)transform.position).sqrMagnitude > 0.12f;
            if (moving)
            {
                var faceRight = _lastLeaderDir.x >= 0f;
                // Match hero sheet default facing.
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
            if (_renderer == null) return;
            _walkAnimTimer = 0f;
            _useWalkFrameA = true;
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

            var credit = _stats != null ? _stats.LootCreditTarget : _leaderStats;
            if (credit == null) return;

            var range = 1.45f * (credit.EffectiveLootRangeMultiplier);
            var pickups = Object.FindObjectsByType<LootPickup>();
            for (var i = 0; i < pickups.Length; i++)
            {
                var pickup = pickups[i];
                if (pickup == null) continue;
                if (Vector2.Distance(transform.position, pickup.transform.position) > range) continue;
                pickup.CollectFor(credit);
            }
        }
    }
}
