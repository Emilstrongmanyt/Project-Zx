using ProjectZx.Core;
using ProjectZx.Player;
using UnityEngine;

namespace ProjectZx.World
{
    public enum PickupType { Xp, Gold }

    public class LootPickup : MonoBehaviour
    {
        const float BaseCollectRange = 1.45f;

        PickupType _type;
        int _amount;
        SpriteRenderer _renderer;

        public void Initialize(PickupType type, int amount)
        {
            _type = type;
            _amount = amount;
            _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sprite = type == PickupType.Xp ? ArtLibrary.HpHeartDropped : ArtLibrary.GoldCoinDropped;
            _renderer.sortingOrder = 8;
            transform.localScale = Vector3.one * 0.55f;
        }

        void Update()
        {
            TryCollect();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            TryCollect(other);
        }

        void TryCollect(Collider2D other = null)
        {
            Transform playerTransform;
            PlayerStats stats;

            if (other != null)
            {
                if (!other.CompareTag("Player")) return;
                playerTransform = other.transform;
                stats = other.GetComponent<PlayerStats>();
            }
            else
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player == null) return;
                playerTransform = player.transform;
                stats = player.GetComponent<PlayerStats>();
                var lootRange = stats != null ? BaseCollectRange * stats.RunLootRangeMultiplier : BaseCollectRange;
                if (Vector2.Distance(transform.position, playerTransform.position) > lootRange) return;
            }

            if (stats == null) return;

            if (_type == PickupType.Xp) stats.AddXp(_amount);
            else stats.AddRunGold(_amount);

            Destroy(gameObject);
        }
    }
}