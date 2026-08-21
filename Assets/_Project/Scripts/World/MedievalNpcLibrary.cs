using System;
using ProjectZx.Core;
using UnityEngine;

namespace ProjectZx.World
{
    /// <summary>
    /// Fantasy Medieval Character Pack idle frames shipped under Resources/MedievalNpc.
    /// </summary>
    public static class MedievalNpcLibrary
    {
        public const string MiraFolder = "MedievalNpc/Mira";
        public const string BrenFolder = "MedievalNpc/Bren";
        public const string ThalorFolder = "MedievalNpc/Thalor";
        public const string CorvinFolder = "MedievalNpc/Corvin";
        public const string AldricFolder = "MedievalNpc/Aldric";
        public const string LyraFolder = "MedievalNpc/Lyra";
        public const string KaelFolder = "MedievalNpc/Kael";
        public const string NessaFolder = "MedievalNpc/Nessa";
        public const string GarrickFolder = "MedievalNpc/Garrick";
        public const string ToveFolder = "MedievalNpc/Tove";

        public const float CampScale = 0.58f;
        public const float QuestScale = 0.68f;
        public const float KnightScale = 0.78f;
        public const float IdleFps = 8f;

        static readonly Sprite[][] Cache = new Sprite[10][];

        public enum Cast
        {
            Mira = 0,
            Bren = 1,
            Thalor = 2,
            Corvin = 3,
            Aldric = 4,
            Lyra = 5,
            Kael = 6,
            Nessa = 7,
            Garrick = 8,
            Tove = 9
        }

        public static string FolderFor(Cast cast) => cast switch
        {
            Cast.Mira => MiraFolder,
            Cast.Bren => BrenFolder,
            Cast.Thalor => ThalorFolder,
            Cast.Corvin => CorvinFolder,
            Cast.Aldric => AldricFolder,
            Cast.Lyra => LyraFolder,
            Cast.Kael => KaelFolder,
            Cast.Nessa => NessaFolder,
            Cast.Garrick => GarrickFolder,
            Cast.Tove => ToveFolder,
            _ => MiraFolder
        };

        public static Sprite[] LoadIdle(Cast cast)
        {
            var i = (int)cast;
            if (Cache[i] != null && Cache[i].Length > 0)
                return Cache[i];

            var folder = FolderFor(cast);
            var frames = Resources.LoadAll<Sprite>(folder);
            if (frames == null || frames.Length == 0)
            {
                Debug.LogError($"[MedievalNpc] No sprites at Resources/{folder}");
                Cache[i] = Array.Empty<Sprite>();
                return Cache[i];
            }

            Array.Sort(frames, (a, b) => string.CompareOrdinal(a.name, b.name));
            Cache[i] = frames;
            return frames;
        }

        public static Sprite Portrait(Cast cast)
        {
            var frames = LoadIdle(cast);
            return frames.Length > 0 ? frames[0] : null;
        }

        public static GameObject Create(
            string name,
            Cast cast,
            Vector3 position,
            Action onInteract,
            float scale,
            float proximityRadius = 2.8f,
            bool flipX = false)
        {
            var frames = LoadIdle(cast);
            if (frames.Length == 0)
            {
                var fallback = ArtLibrary.Wizard;
                var fallbackGo = GameFactory.CreateNpc(name, fallback, position, onInteract, scale);
                ApplyFlipX(fallbackGo, flipX);
                return fallbackGo;
            }

            var go = GameFactory.CreateAnimatedNpc(
                name,
                frames,
                position,
                onInteract,
                scale,
                IdleFps);

            var col = go.GetComponent<CircleCollider2D>();
            if (col != null)
                col.radius = proximityRadius;

            ApplyFlipX(go, flipX);
            return go;
        }

        static void ApplyFlipX(GameObject go, bool flipX)
        {
            if (!flipX || go == null) return;
            var s = go.transform.localScale;
            s.x = -Mathf.Abs(s.x);
            go.transform.localScale = s;
        }
    }
}
