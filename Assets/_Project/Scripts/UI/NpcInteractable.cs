using System;
using UnityEngine;

namespace ProjectZx.UI
{
    public class NpcInteractable : MonoBehaviour
    {
        const float InteractRange = 2.6f;

        Action _onInteract;

        public float InteractRangeWorld => InteractRange;

        public void Initialize(Action onInteract)
        {
            _onInteract = onInteract;
        }

        public bool TryInteract(Transform player)
        {
            if (player == null) return false;
            if (Vector2.Distance(player.position, transform.position) > InteractRange) return false;
            _onInteract?.Invoke();
            return true;
        }
    }
}