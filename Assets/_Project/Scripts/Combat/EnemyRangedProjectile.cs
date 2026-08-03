using ProjectZx.Core;
using ProjectZx.Player;
using ProjectZx.World;
using UnityEngine;

namespace ProjectZx.Combat
{
    /// <summary>
    /// Straight-line bolt from late-map ranged demons. No tracking; dies on hit or lifetime.
    /// </summary>
    public class EnemyRangedProjectile : MonoBehaviour
    {
        const float DefaultSpeed = 5.5f;
        const float DefaultLifetime = 3.2f;
        const float HitRadius = 0.4f;

        Vector2 _velocity;
        float _life;
        int _damage;
        float _animTimer;
        int _frame;
        SpriteRenderer _renderer;
        Transform _player;
        bool _hit;

        public static void Spawn(
            Vector3 origin,
            Vector2 direction,
            int damage,
            float speed = DefaultSpeed,
            float lifetime = DefaultLifetime)
        {
            var dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.left;
            var go = new GameObject("EnemyRangedProjectile");
            go.transform.position = origin;
            go.transform.localScale = Vector3.one * 0.28f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ArtLibrary.GetBossFireBoltFrame(0);
            sr.color = new Color(0.75f, 0.55f, 1f, 1f);
            sr.sortingOrder = 20;
            go.AddComponent<YSortRenderer>().Configure(12);

            var proj = go.AddComponent<EnemyRangedProjectile>();
            proj._velocity = dir * speed;
            proj._life = lifetime;
            proj._damage = Mathf.Max(1, damage);
            proj._renderer = sr;
            proj._player = GameObject.FindGameObjectWithTag("Player")?.transform;

            var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            go.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        void Update()
        {
            _life -= Time.deltaTime;
            if (_life <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            transform.position += (Vector3)(_velocity * Time.deltaTime);

            _animTimer -= Time.deltaTime;
            if (_animTimer <= 0f && _renderer != null)
            {
                _animTimer = 0.1f;
                _frame++;
                _renderer.sprite = ArtLibrary.GetBossFireBoltFrame(_frame);
            }

            if (_hit || _player == null) return;
            if (Vector2.Distance(transform.position, _player.position) > HitRadius) return;

            var stats = _player.GetComponent<PlayerStats>();
            if (stats == null || stats.IsDead) return;

            _hit = true;
            stats.TakeDamage(_damage);
            Destroy(gameObject);
        }
    }
}
