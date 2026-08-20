using System;
using ProjectZx.Core;
using UnityEngine;

namespace ProjectZx.World
{
    /// <summary>
    /// Fantasy Medieval Character Pack idle frames shipped under Resources/MedievalNpc.
    /// Cast: Mira, Bren, Thalor, Corvin, Aldric — more heroes available for later expansions.
    /// </summary>
    public static class MedievalNpcLibrary
    {
        public const string MiraFolder = "MedievalNpc/Mira";
        public const string BrenFolder = "MedievalNpc/Bren";
        public const string ThalorFolder = "MedievalNpc/Thalor";
        public const string CorvinFolder = "MedievalNpc/Corvin";
        public const string AldricFolder = "MedievalNpc/Aldric";

        public const float CampScale = 0.58f;
        public const float QuestScale = 0.68f;
        public const float KnightScale = 0.78f;
        public const float IdleFps = 8f;

        static readonly Sprite[][] Cache = new Sprite[5][];

        public enum Cast
        {
            Mira = 0,
            Bren = 1,
            Thalor = 2,
            Corvin = 3,
            Aldric = 4
        }

        public static string FolderFor(Cast cast) => cast switch
        {
            Cast.Mira => MiraFolder,
            Cast.Bren => BrenFolder,
            Cast.Thalor => ThalorFolder,
            Cast.Corvin => CorvinFolder,
            Cast.Aldric => AldricFolder,
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
            float proximityRadius = 2.8f)
        {
            var frames = LoadIdle(cast);
            if (frames.Length == 0)
            {
                var fallback = ArtLibrary.Wizard;
                return GameFactory.CreateNpc(name, fallback, position, onInteract, scale);
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

            return go;
        }
    }
}
