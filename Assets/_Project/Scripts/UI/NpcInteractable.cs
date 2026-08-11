using System;
using UnityEngine;

namespace ProjectZx.UI
{
    public class NpcInteractable : MonoBehaviour
    {
        const float InteractRange = 3f;

        Action _onInteract;

        public float InteractRangeWorld => InteractRange;

        public void Initialize(Action onInteract)
        {
            _onInteract = onInteract;
        }

        public bool TryInteract(Transform player)
        {
            if (player == null) return false;
            // Block world NPC / hero taps while any camp menu is open (shop, settings, etc.).
            if (HubUi.Instance != null && HubUi.Instance.IsAnyMenuOpen) return false;
            if (Vector2.Distance(player.position, transform.position) > InteractRange) return false;
            _onInteract?.Invoke();
            return true;
        }
    }
}