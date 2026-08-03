using ProjectZx.Core;
using ProjectZx.Player;
using ProjectZx.World;
using UnityEngine;

namespace ProjectZx.Combat
{
    /// <summary>
    /// Straight-line skull bolt from late-map ranged demons. No tracking; dies on hit or lifetime.
    /// </summary>
    public class EnemyRangedProjectile : MonoBehaviour
    {
        const float DefaultSpeed = 5.5f;
        const float DefaultLifetime = 3.2f;
        const float HitRadius = 0.42f;
        const float SpinDegreesPerSecond = 220f;

        Vector2 _velocity;
        float _life;
        int _damage;
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
            // Size is baked into the ArtLibrary skull PPU (~0.55 world units).
            go.transform.localScale = Vector3.one;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ArtLibrary.GetRandomSkullProjectile();
            sr.color = Color.white;
            sr.sortingOrder = 20;
            go.AddComponent<YSortRenderer>().Configure(12);

            var proj = go.AddComponent<EnemyRangedProjectile>();
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
            // Gentle spin so skulls read as thrown projectiles without flight-angle skew.
            transform.Rotate(0f, 0f, SpinDegreesPerSecond * Time.deltaTime);

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
