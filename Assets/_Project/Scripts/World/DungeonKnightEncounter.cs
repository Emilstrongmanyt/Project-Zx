using System.Collections;
using ProjectZx.Core;
using ProjectZx.UI;
using UnityEngine;

namespace ProjectZx.World
{
    /// <summary>
    /// Sir Aldric stranded in Ironvault. Tap while near to open a door home;
    /// he walks through and later appears at camp for his quest.
    /// </summary>
    public class DungeonKnightEncounter : MonoBehaviour
    {
        const float WalkDuration = 0.9f;

        bool _leaving;
        SpriteRenderer _renderer;

        public static GameObject Spawn(Vector2 position)
        {
            var go = MedievalNpcLibrary.Create(
                "DungeonKnight",
                MedievalNpcLibrary.Cast.Aldric,
                new Vector3(position.x, position.y, 0f),
                onInteract: null,
                MedievalNpcLibrary.KnightScale,
                proximityRadius: 1.5f);
            var knight = go.AddComponent<DungeonKnightEncounter>();
            go.AddComponent<NpcInteractable>().Initialize(knight.OnInteract);
            return go;
        }

        public static void TrySpawnInDungeon()
        {
            if (GameSessionContext.SurvivalMap != SurvivalMapKind.Dungeon) return;
            if (GameSave.DungeonKnightReturnedToCamp) return;
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
            var doorPos = transform.position + Vector3.right * 1.6f;
            var door = GameFactory.CreateArenaDoor(doorPos);
            door.name = "KnightHomeDoor";
            var enter = door.GetComponent<ArenaDoor>();
            if (enter != null) Object.Destroy(enter);

            GameHud.Instance?.ShowBanner("Sir Aldric opens a door back to camp…", 2.8f);
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
                var alpha = 1f - ease * 0.85f;
                if (_renderer != null && _renderer.enabled)
                {
                    var c = _renderer.color;
                    c.a = alpha;
                    _renderer.color = c;
                }

                yield return null;
            }

            GameSave.DungeonKnightReturnedToCamp = true;
            GameHud.Instance?.ShowBanner("Sir Aldric returned to camp. Speak with him there.", 3.4f);
            WorldSparkle.Play(doorPos, 10);

            if (door != null) Object.Destroy(door);
            Destroy(gameObject);
        }
    }
}
