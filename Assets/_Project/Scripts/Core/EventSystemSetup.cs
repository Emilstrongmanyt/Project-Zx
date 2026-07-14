using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace ProjectZx.Core
{
    public static class EventSystemSetup
    {
        public static void EnsureExists()
        {
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null) return;

            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }
    }
}