using System.Collections;
using ProjectZx.Core;
using ProjectZx.UI;
using UnityEngine;
// GameFactory lives in ProjectZx.Core

namespace ProjectZx.World
{
    /// <summary>
    /// Grey Wizard in crow form (Inside Survival, after R10). Tap while near to free him.
    /// </summary>
    public class DarkBirdRescue : MonoBehaviour
    {
        const float FlyDuration = 0.85f;
        const float FlyDistance = 18f;

        bool _rescued;
        SpriteRenderer _renderer;

        public static GameObject Spawn(Vector2 position)
        {
            var go = GameFactory.CreateSprite(
                "DarkBirdRescue",
                ArtLibrary.DarkBird,
                new Vector3(position.x, position.y, 0f),
                // 32×32 at 100 PPU is tiny — 2× base, then ×1.5 for readability on mobile.
                scale: 2f * 1.5f,
                sortingOrder: 8);
            go.AddComponent<YSortRenderer>().Configure(6);
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 1.4f;
            var bird = go.AddComponent<DarkBirdRescue>();
            go.AddComponent<NpcInteractable>().Initialize(bird.OnInteract);
            return go;
        }

        void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        void OnInteract()
        {
            if (_rescued) return;
            if (QuestCatalog.GetProgress(QuestId.GreyWizardsCrow) != QuestProgress.Active) return;

            _rescued = true;
            GameSave.QuestGreyWizardRescued = true;
            GameHud.Instance?.ShowBanner("The crow is free! Return to the Grand Wizard at camp.", 3.5f);
            WorldSparkle.Play(transform.position, 10);
            StartCoroutine(FlyAway());
        }

        IEnumerator FlyAway()
        {
            var start = transform.position;
            var dir = (Random.insideUnitCircle.normalized + Vector2.up * 0.65f).normalized;
            if (dir.sqrMagnitude < 0.01f) dir = Vector2.up;
            var end = start + (Vector3)(dir * FlyDistance);
            var t = 0f;
            while (t < FlyDuration)
            {
                t += Time.deltaTime;
                var u = Mathf.Clamp01(t / FlyDuration);
                var ease = u * u;
                transform.position = Vector3.Lerp(start, end, ease);
                if (_renderer != null)
                {
                    var c = _renderer.color;
                    c.a = 1f - ease;
                    _renderer.color = c;
                }

                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
