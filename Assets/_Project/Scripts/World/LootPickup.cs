using ProjectZx.Core;
using ProjectZx.Player;
using UnityEngine;

namespace ProjectZx.World
{
    public enum PickupType { Xp, Gold, HpPotion }

    public class LootPickup : MonoBehaviour
    {
        const float BaseCollectRange = 1.45f;
        const float XpPickupScale = 0.55f * 1.5f;
        const float DroppedPickupScale = 0.55f * 3f * 1.5f;

        PickupType _type;
        int _amount;
        SpriteRenderer _renderer;

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
                default:
                    _renderer.sprite = ArtLibrary.GoldCoinDropped;
                    transform.localScale = Vector3.one * DroppedPickupScale;
                    break;
            }

            _renderer.sortingOrder = 8;
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

            switch (_type)
            {
                case PickupType.Xp:
                    stats.AddXp(_amount);
                    break;
                case PickupType.HpPotion:
                    stats.Heal(_amount);
                    break;
                default:
                    stats.AddRunGold(_amount);
                    break;
            }

            Destroy(gameObject);
        }
    }
}