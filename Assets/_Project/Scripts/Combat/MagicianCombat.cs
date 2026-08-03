using System.Collections.Generic;
using ProjectZx.Core;
using ProjectZx.Enemies;
using ProjectZx.Player;
using ProjectZx.World;
using UnityEngine;

namespace ProjectZx.Combat
{
    [RequireComponent(typeof(PlayerStats))]
    public class MagicianCombat : MonoBehaviour
    {
        const float PrimaryDamageMultiplier = 1.15f;
        /// <summary>Standard single-target primary: +40% base damage.</summary>
        const float StandardDamageBonus = 1.4f;
        const float SplashDamageMultiplier = 0.55f;
        const float SplashRadius = 1.6f;

        [SerializeField] float attackRange = 4f;
        [SerializeField] float attackInterval = 0.95f;
        [SerializeField] float castDuration = 0.32f;

        const float StaffRestAngle = -28f;
        const float StaffCastAngle = 18f;

        float _cooldown;
        float _castTimer;
        bool _casting;
        SpriteRenderer _bodyRenderer;
        Transform _staffPivot;

        public bool IsCasting => _casting;

        float AttackRange
        {
            get
            {
                var stats = GetComponent<PlayerStats>();
                var rangeMul = stats != null ? stats.AttackRangeMultiplier : GameSave.AttackRangeMultiplier;
                return attackRange * rangeMul;
            }
        }

        void Awake()
        {
            _bodyRenderer = GetComponent<SpriteRenderer>();
            SetupStaff();
        }

        void SetupStaff()
        {
            var pivotGo = new GameObject("StaffPivot");
            pivotGo.transform.SetParent(transform, false);
            pivotGo.transform.localPosition = new Vector3(0.08f, -0.22f, 0f);
            _staffPivot = pivotGo.transform;

            var staffGo = new GameObject("Staff");
            staffGo.transform.SetParent(_staffPivot, false);
            staffGo.transform.localPosition = new Vector3(0.28f, 0.04f, 0f);
            staffGo.transform.localScale = Vector3.one;

            var staffRenderer = staffGo.AddComponent<SpriteRenderer>();
            staffRenderer.sprite = ArtLibrary.Staff;
            staffRenderer.sortingOrder = 20;
            staffGo.AddComponent<YSortRenderer>().Configure(3);

            _staffPivot.localRotation = Quaternion.Euler(0f, 0f, StaffRestAngle);
        }

        void Update()
        {
            if (GetComponent<PlayerStats>().IsDead) return;

            _castTimer -= Time.deltaTime;
            if (_casting && _castTimer <= 0f)
            {
                _casting = false;
                if (_staffPivot != null)
                    _staffPivot.localRotation = Quaternion.Euler(0f, 0f, StaffRestAngle);
            }

            if (_casting) return;

            var stats = GetComponent<PlayerStats>();
            var attackSpeed = stats != null ? stats.EffectiveAttackSpeed : 1f;
            _cooldown -= Time.deltaTime * attackSpeed;
            if (_cooldown > 0f) return;

            var enemy = FindClosestEnemy();
            if (enemy == null) return;

            var dist = Vector2.Distance(transform.position, enemy.transform.position);
            if (dist > AttackRange) return;

            PerformCast(enemy);
        }

        void PerformCast(EnemyActor enemy)
        {
            AudioManager.Instance?.PlaySwingSfx();
            _cooldown = attackInterval;
            _casting = true;
            _castTimer = castDuration;

            if (_bodyRenderer != null)
                _bodyRenderer.flipX = enemy.transform.position.x < transform.position.x;

            if (_staffPivot != null)
                _staffPivot.localRotation = Quaternion.Euler(0f, 0f, StaffCastAngle);

            var stats = GetComponent<PlayerStats>();
            CombatDamage.Apply(stats, enemy, PrimaryDamageMultiplier * StandardDamageBonus);
            foreach (var other in FindEnemiesInSplash(enemy.transform.position))
            {
                if (other == enemy) continue;
                CombatDamage.Apply(stats, other, SplashDamageMultiplier);
            }
        }

        List<EnemyActor> FindEnemiesInSplash(Vector3 center)
        {
            var hits = new List<EnemyActor>();
            foreach (var enemy in Object.FindObjectsByType<EnemyActor>())
            {
                if (enemy == null || !enemy.IsAlive) continue;
                if (Vector2.Distance(center, enemy.transform.position) <= SplashRadius)
                    hits.Add(enemy);
            }

            return hits;
        }

        EnemyActor FindClosestEnemy()
        {
            EnemyActor best = null;
            var bestDist = float.MaxValue;
            foreach (var enemy in Object.FindObjectsByType<EnemyActor>())
            {
                if (enemy == null || !enemy.IsAlive) continue;
                var d = Vector2.Distance(transform.position, enemy.transform.position);
                if (d >= bestDist) continue;
                bestDist = d;
                best = enemy;
            }

            return best;
        }
    }
}