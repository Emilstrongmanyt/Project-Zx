using ProjectZx.Core;
using ProjectZx.Player;
using UnityEngine;

namespace ProjectZx.World
{
    public enum PickupType { Xp, Gold, HpPotion, MapLoot }

    public class LootPickup : MonoBehaviour
    {
        const float BaseCollectRange = 1.45f;
        const float XpPickupScale = 0.55f * 1.5f;
        const float DroppedPickupScale = 0.55f * 3f * 1.5f;
        const float MapLootScale = 0.7f;

        PickupType _type;
        int _amount;
        SpriteRenderer _renderer;
        bool _collected;

        public PickupType Type => _type;

        public void Initialize(PickupType type, int amount)
        {
            _type = type;
            _amount = amount;
            _renderer = gameObject.AddComponent<SpriteRenderer>();

            switch (type)
            {
                case PickupType.Xp:
                    _renderer.sprite = ArtLibrary.XpGem;
                    transform.localScale = Vector3.one * XpPickupScale;
                    break;
                case PickupType.HpPotion:
                    _renderer.sprite = ArtLibrary.HpHeartDropped;
                    transform.localScale = Vector3.one * DroppedPickupScale;
                    break;
                case PickupType.MapLoot:
                    _renderer.sprite = ArtLibrary.PinkCrystal;
                    transform.localScale = Vector3.one * MapLootScale;
                    break;
                default:
                    _renderer.sprite = ArtLibrary.GoldCoinDropped;
                    transform.localScale = Vector3.one * DroppedPickupScale;
                    break;
            }

            _renderer.sortingOrder = type == PickupType.MapLoot ? 10 : 8;
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
            if (_collected) return;

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
                var lootRange = stats != null
                    ? BaseCollectRange * stats.EffectiveLootRangeMultiplier
                    : BaseCollectRange;
                if (Vector2.Distance(transform.position, playerTransform.position) > lootRange) return;
            }

            if (stats == null) return;
            Collect(stats);
        }

        /// <summary>Apply this pickup's reward and destroy it (used by map-loot crystal).</summary>
        public void ForceCollect(PlayerStats stats)
        {
            if (_collected || stats == null) return;
            // Map-loot crystals must not recursively vacuum other crystals.
            if (_type == PickupType.MapLoot)
            {
                _collected = true;
                Destroy(gameObject);
                return;
            }

            Collect(stats);
        }

        void Collect(PlayerStats stats)
        {
            if (_collected || stats == null) return;
            _collected = true;

            switch (_type)
            {
                case PickupType.Xp:
                    stats.AddXp(_amount);
                    break;
                case PickupType.HpPotion:
                    stats.Heal(_amount);
                    break;
                case PickupType.MapLoot:
                    CollectAllMapLoot(stats);
                    break;
                default:
                    stats.AddRunGold(_amount);
                    break;
            }

            Destroy(gameObject);
        }

        static void CollectAllMapLoot(PlayerStats stats)
        {
            var pickups = Object.FindObjectsByType<LootPickup>();
            for (var i = 0; i < pickups.Length; i++)
            {
                var pickup = pickups[i];
                if (pickup == null || pickup._collected) continue;
                if (pickup._type == PickupType.MapLoot) continue;
                pickup.ForceCollect(stats);
            }
        }
    }
}
