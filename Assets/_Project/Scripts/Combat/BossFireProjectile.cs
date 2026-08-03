using ProjectZx.Core;
using ProjectZx.Player;
using ProjectZx.World;
using UnityEngine;

namespace ProjectZx.Combat
{
    /// <summary>
    /// Slow skull bolt from the Dungeon R40 BossB. No mid-flight tracking; dies after lifetime.
    /// </summary>
    public class BossFireProjectile : MonoBehaviour
    {
        const float DefaultSpeed = 2.2f;
        const float DefaultLifetime = 5f;
        const float HitRadius = 0.5f;
        const float SpinDegreesPerSecond = 140f;
        /// <summary>Boss skulls are slightly larger than regular caster bolts.</summary>
        const float BossSkullScale = 1.35f;

        Vector2 _velocity;
        float _life;
        int _damage;
        SpriteRenderer _renderer;
        Transform _player;
        bool _hit;

        public static void Spawn(Vector3 origin, Vector2 direction, int damage, float speed = DefaultSpeed, float lifetime = DefaultLifetime)
        {
            var dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.left;
            var go = new GameObject("BossFireProjectile");
            go.transform.position = origin;
            go.transform.localScale = Vector3.one * BossSkullScale;

            var sr = go.AddComponent<SpriteRenderer>();
            // Prefer the more menacing skulls (demon / titan) for boss bolts.
            sr.sprite = ArtLibrary.GetSkullProjectile(2 + Random.Range(0, 4));
            sr.color = Color.white;
            sr.sortingOrder = 20;
            go.AddComponent<YSortRenderer>().Configure(12);

            var proj = go.AddComponent<BossFireProjectile>();
            proj._velocity = dir * speed;
            proj._life = lifetime;
            proj._damage = Mathf.Max(1, damage);
            proj._renderer = sr;
            proj._player = GameObject.FindGameObjectWithTag("Player")?.transform;
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
            transform.Rotate(0f, 0f, -SpinDegreesPerSecond * Time.deltaTime);

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
