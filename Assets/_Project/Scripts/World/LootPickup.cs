using ProjectZx.Player;
using UnityEngine;

namespace ProjectZx.World
{
    public enum PickupType { Xp, Gold }

    public class LootPickup : MonoBehaviour
    {
        PickupType _type;
        int _amount;
        SpriteRenderer _renderer;

        static Sprite _xpSprite;
        static Sprite _goldSprite;

        public void Initialize(PickupType type, int amount)
        {
            _type = type;
            _amount = amount;
            _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sprite = type == PickupType.Xp ? GetXpSprite() : GetGoldSprite();
            _renderer.sortingOrder = 8;
            transform.localScale = Vector3.one * 0.4f;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            var stats = other.GetComponent<PlayerStats>();
            if (stats == null) return;

            if (_type == PickupType.Xp) stats.AddXp(_amount);
            else stats.AddRunGold(_amount);

            Destroy(gameObject);
        }

        static Sprite GetXpSprite()
        {
            if (_xpSprite != null) return _xpSprite;
            return _xpSprite = CreateDotSprite(new Color(0.2f, 0.9f, 1f));
        }

        static Sprite GetGoldSprite()
        {
            if (_goldSprite != null) return _goldSprite;
            return _goldSprite = CreateDotSprite(new Color(1f, 0.85f, 0.15f));
        }

        static Sprite CreateDotSprite(Color color)
        {
            const int size = 16;
            var tex = new Texture2D(size, size);
            var center = new Vector2(size / 2f, size / 2f);
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var dist = Vector2.Distance(new Vector2(x, y), center);
                tex.SetPixel(x, y, dist <= size * 0.42f ? color : Color.clear);
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
        }
    }
}