using System;
using UnityEngine;

namespace ProjectZx.UI
{
    public class NpcInteractable : MonoBehaviour
    {
        string _prompt;
        Action _onInteract;
        bool _playerNear;

        public void Initialize(string prompt, Action onInteract)
        {
            _prompt = prompt;
            _onInteract = onInteract;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            _playerNear = true;
            HubUi.Instance?.ShowPrompt(_prompt, _onInteract);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            _playerNear = false;
            HubUi.Instance?.HidePrompt();
        }

        void OnDestroy()
        {
            if (_playerNear) HubUi.Instance?.HidePrompt();
        }
    }
}