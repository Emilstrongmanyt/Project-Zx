using ProjectZx.Core;
using ProjectZx.Player;
using ProjectZx.UI;
using UnityEngine;

namespace ProjectZx.World
{
    public enum PickupType { Xp, Gold, HpPotion, MapLoot, Equipment }

    public class LootPickup : MonoBehaviour
    {
        const float BaseCollectRange = 1.45f;
        const float XpPickupScale = 0.55f * 1.5f;
        const float DroppedPickupScale = 0.55f * 3f * 1.5f;
        const float MapLootScale = 0.7f;
        const float EquipmentPickupScale = 0.85f;

        PickupType _type;
        int _amount;
        EquipmentId _equipmentId;
        SpriteRenderer _renderer;
        bool _collected;

        public PickupType Type => _type;

        public void Initialize(PickupType type, int amount)
        {
            _type = type;
            _amount = amount;
            _equipmentId = EquipmentId.None;
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
                case PickupType.Equipment:
                    _equipmentId = EquipmentCatalog.IsValid((EquipmentId)amount)
                        ? (EquipmentId)amount
                        : EquipmentCatalog.RollRandomDrop();
                    _renderer.sprite = EquipmentCatalog.GetIcon(_equipmentId) ?? ArtLibrary.GoldCoin;
                    transform.localScale = Vector3.one * EquipmentPickupScale;
                    break;
                default:
                    _renderer.sprite = ArtLibrary.GoldCoinDropped;
                    transform.localScale = Vector3.one * DroppedPickupScale;
                    break;
            }

            _renderer.sortingOrder = type is PickupType.MapLoot or PickupType.Equipment ? 10 : 8;
        }

        public void InitializeEquipment(EquipmentId equipmentId)
        {
            Initialize(PickupType.Equipment, (int)equipmentId);
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

        /// <summary>Apply this pickup's reward and destroy it (used by map-loot crystal / companion).</summary>
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

        /// <summary>Companion / external collect — credits the given player stats.</summary>
        public void CollectFor(PlayerStats stats) => Collect(stats);

        void Collect(PlayerStats stats)
        {
            if (_collected || stats == null) return;
            stats = stats.LootCreditTarget;
            if (stats == null) return;
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
                case PickupType.Equipment:
                    CollectEquipment();
                    break;
                default:
                    stats.AddRunGold(_amount);
                    break;
            }

            Destroy(gameObject);
        }

        void CollectEquipment()
        {
            var id = EquipmentCatalog.IsValid(_equipmentId)
                ? _equipmentId
                : EquipmentCatalog.RollRandomDrop();
            var def = EquipmentCatalog.Get(id);
            if (def.Id == EquipmentId.None) return;

            if (GameSave.UnlockEquipment(id))
            {
                GameHud.Instance?.ShowBanner($"Found {def.DisplayName}! Equip it at the camp chest.", 3.2f);
            }
            else
            {
                GameHud.Instance?.ShowBanner($"Already own {def.DisplayName}.", 2f);
            }
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
