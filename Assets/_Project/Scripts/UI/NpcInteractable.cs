using System;
using UnityEngine;

namespace ProjectZx.UI
{
    public class NpcInteractable : MonoBehaviour
    {
        const float InteractRange = 2.6f;

        string _prompt;
        Action _onInteract;
        bool _playerNear;

        public float InteractRangeWorld => InteractRange;

        public void Initialize(string prompt, Action onInteract)
        {
            _prompt = prompt;
            _onInteract = onInteract;
        }

        public bool TryInteract(Transform player)
        {
            if (player == null) return false;
            if (Vector2.Distance(player.position, transform.position) > InteractRange) return false;
            _onInteract?.Invoke();
            return true;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            _playerNear = true;
            HubUi.Instance?.ShowNearbyHint(_prompt);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            _playerNear = false;
            HubUi.Instance?.HideNearbyHint();
        }

        void OnDestroy()
        {
            if (_playerNear) HubUi.Instance?.HideNearbyHint();
        }
    }
}