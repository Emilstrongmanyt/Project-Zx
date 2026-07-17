using UnityEngine;

namespace ProjectZx.UI
{
    /// <summary>
    /// World-space scrolling damage popup. White for enemies, red for the hero.
    /// </summary>
    public class FloatingDamageNumber : MonoBehaviour
    {
        const float Lifetime = 0.85f;
        const float RiseSpeed = 1.35f;
        const float HeadOffsetY = 0.85f;
        const float CharacterSize = 0.085f;
        const int FontSize = 64;
        const int SortOrder = 900;

        static readonly Color EnemyColor = Color.white;
        static readonly Color HeroColor = new Color(1f, 0.22f, 0.22f, 1f);

        TextMesh _text;
        MeshRenderer _renderer;
        float _age;
        Color _baseColor;
        Vector3 _velocity;

        public static void Spawn(Vector3 worldPosition, int amount, bool isHeroHit)
        {
            if (amount <= 0) return;

            var go = new GameObject("DamageNumber");
            go.transform.position = worldPosition + Vector3.up * HeadOffsetY;

            var number = go.AddComponent<FloatingDamageNumber>();
            number.Setup(amount, isHeroHit);
        }

        void Setup(int amount, bool isHeroHit)
        {
            _text = gameObject.AddComponent<TextMesh>();
            _text.text = amount.ToString();
            _text.fontSize = FontSize;
            _text.characterSize = CharacterSize;
            _text.anchor = TextAnchor.MiddleCenter;
            _text.alignment = TextAlignment.Center;
            _text.fontStyle = FontStyle.Bold;
            _baseColor = isHeroHit ? HeroColor : EnemyColor;
            _text.color = _baseColor;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
                _text.font = font;

            _renderer = GetComponent<MeshRenderer>();
            if (_renderer != null)
            {
                _renderer.sortingOrder = SortOrder;
                if (font != null && font.material != null)
                    _renderer.sharedMaterial = font.material;
            }

            // Slight horizontal jitter so stacked hits stay readable.
            var jitter = Random.Range(-0.22f, 0.22f);
            transform.position += new Vector3(jitter, 0f, 0f);
            _velocity = new Vector3(jitter * 0.15f, RiseSpeed, 0f);
            _age = 0f;
        }

        void LateUpdate()
        {
            _age += Time.deltaTime;
            transform.position += _velocity * Time.deltaTime;

            // Keep upright for orthographic 2D (no billboard flip with negative scales).
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            var t = Mathf.Clamp01(_age / Lifetime);
            if (_text != null)
            {
                var c = _baseColor;
                c.a = 1f - t * t;
                _text.color = c;
            }

            if (_age >= Lifetime)
                Destroy(gameObject);
        }
    }
}
