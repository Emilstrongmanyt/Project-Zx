using System.Collections;
using ProjectZx.Core;
using ProjectZx.UI;
using UnityEngine;

namespace ProjectZx.World
{
    /// <summary>
    /// Knight stranded in Dungeon Survival. Tap while near to open a door home;
    /// he walks through and later appears at camp for his quest.
    /// </summary>
    public class DungeonKnightEncounter : MonoBehaviour
    {
        const float WalkDuration = 0.9f;

        bool _leaving;
        SpriteRenderer _renderer;

        public static GameObject Spawn(Vector2 position)
        {
            var go = GameFactory.CreateSprite(
                "DungeonKnight",
                ArtLibrary.Knight1,
                new Vector3(position.x, position.y, 0f),
                // Match camp Knight1 scale (quest NPC base × 1.5).
                scale: 0.38f * 1.25f * 1.85f * 1.5f,
                sortingOrder: 8);
            go.AddComponent<YSortRenderer>().Configure(4);
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 1.5f;
            var knight = go.AddComponent<DungeonKnightEncounter>();
            go.AddComponent<NpcInteractable>().Initialize(knight.OnInteract);
            return go;
        }

        public static void TrySpawnInDungeon()
        {
            if (GameSessionContext.SurvivalMap != SurvivalMapKind.Dungeon) return;
            if (GameSave.DungeonKnightReturnedToCamp) return;
            // Stand a short walk from spawn so the player notices him early.
            Spawn(new Vector2(5.5f, 2.2f));
        }

        void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        void OnInteract()
        {
            if (_leaving || GameSave.DungeonKnightReturnedToCamp) return;
            _leaving = true;
            StartCoroutine(ReturnHomeRoutine());
        }

        IEnumerator ReturnHomeRoutine()
        {
            // Spawn a door the knight will use (player cannot enter this one).
            var doorPos = transform.position + Vector3.right * 1.6f;
            var door = GameFactory.CreateArenaDoor(doorPos);
            door.name = "KnightHomeDoor";
            // Non-interactive visual only — strip any enter component if present.
            var enter = door.GetComponent<ArenaDoor>();
            if (enter != null) Object.Destroy(enter);

            GameHud.Instance?.ShowBanner("The knight opens a door back to camp…", 2.8f);
            WorldSparkle.Play(transform.position, 8);

            var start = transform.position;
            var end = doorPos;
            var t = 0f;
            while (t < WalkDuration)
            {
                t += Time.deltaTime;
                var u = Mathf.Clamp01(t / WalkDuration);
                var ease = u * u * (3f - 2f * u);
                transform.position = Vector3.Lerp(start, end, ease);
                if (_renderer != null)
                {
                    var c = _renderer.color;
                    c.a = 1f - ease * 0.85f;
                    _renderer.color = c;
                }

                yield return null;
            }

            GameSave.DungeonKnightReturnedToCamp = true;
            GameHud.Instance?.ShowBanner("The knight returned to camp. Speak with him there.", 3.4f);
            WorldSparkle.Play(doorPos, 10);

            if (door != null) Object.Destroy(door);
            Destroy(gameObject);
        }
    }
}
