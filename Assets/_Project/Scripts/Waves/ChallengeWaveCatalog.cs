using System.Collections.Generic;
using ProjectZx.Core;
using ProjectZx.Enemies;

namespace ProjectZx.Waves
{
    public enum ChallengeFlavor
    {
        Pressure = 0,
        Combo = 1
    }

    /// <summary>One spawn batch inside a scripted challenge round.</summary>
    public readonly struct ChallengeSpawnSpec
    {
        public readonly EnemyMovementMode Mode;
        public readonly EnemyZombieKind Kind;
        public readonly bool Ranged;
        public readonly bool Elite;
        public readonly int Count;

        public ChallengeSpawnSpec(
            EnemyMovementMode mode,
            EnemyZombieKind kind,
            int count,
            bool ranged = false,
            bool elite = false)
        {
            Mode = mode;
            Kind = kind;
            Count = count;
            Ranged = ranged;
            Elite = elite;
        }
    }

    /// <summary>Scripted trash composition for a specific map/round.</summary>
    public readonly struct ChallengeRoundDef
    {
        public readonly string Banner;
        public readonly ChallengeFlavor Flavor;
        public readonly ChallengeSpawnSpec[] Specs;

        public ChallengeRoundDef(string banner, ChallengeFlavor flavor, ChallengeSpawnSpec[] specs)
        {
            Banner = banner;
            Flavor = flavor;
            Specs = specs;
        }
    }

    /// <summary>
    /// Predetermined pressure / combo rounds every ~5–10. Normal rounds stay probabilistic.
    /// Decade / stage bosses are still spawned by SurvivalSession after trash.
    /// </summary>
    public static class ChallengeWaveCatalog
    {
        static readonly Dictionary<(SurvivalMapKind map, int round), ChallengeRoundDef> Table =
            BuildTable();

        public static bool TryGet(SurvivalMapKind map, int round, out ChallengeRoundDef def)
            => Table.TryGetValue((map, round), out def);

        static Dictionary<(SurvivalMapKind, int), ChallengeRoundDef> BuildTable()
        {
            var t = new Dictionary<(SurvivalMapKind, int), ChallengeRoundDef>();

            // --- Outside (stage hold R20) ---
            t[(SurvivalMapKind.Outside, 5)] = Pressure(
                "Sprint Pack!",
                Spec(EnemyMovementMode.Sprint, EnemyZombieKind.Outside, 18));
            t[(SurvivalMapKind.Outside, 10)] = Pressure(
                "Charge Ambush!",
                Spec(EnemyMovementMode.Charge, EnemyZombieKind.Outside, 22));
            t[(SurvivalMapKind.Outside, 15)] = Combo(
                "Flank Pattern!",
                Spec(EnemyMovementMode.Chase, EnemyZombieKind.Outside, 14),
                Spec(EnemyMovementMode.Strafe, EnemyZombieKind.Outside, 10));

            // --- Inside (stage hold R30) ---
            t[(SurvivalMapKind.Inside, 5)] = Pressure(
                "Strafe Swarm!",
                Spec(EnemyMovementMode.Strafe, EnemyZombieKind.Inside, 20));
            t[(SurvivalMapKind.Inside, 10)] = Combo(
                "Orbit & Chase!",
                Spec(EnemyMovementMode.Orbit, EnemyZombieKind.Inside, 12),
                Spec(EnemyMovementMode.Chase, EnemyZombieKind.Outside, 12));
            t[(SurvivalMapKind.Inside, 15)] = Pressure(
                "Flying Swarm!",
                Spec(EnemyMovementMode.Fly, EnemyZombieKind.Inside, 24));
            t[(SurvivalMapKind.Inside, 20)] = Combo(
                "Charge + Kite!",
                Spec(EnemyMovementMode.Charge, EnemyZombieKind.Inside, 14),
                Spec(EnemyMovementMode.Kite, EnemyZombieKind.Inside, 10, ranged: true));
            t[(SurvivalMapKind.Inside, 25)] = Pressure(
                "Elite Charge!",
                Spec(EnemyMovementMode.Charge, EnemyZombieKind.Inside, 16, elite: true));

            // --- Dungeon (stage hold R40) ---
            t[(SurvivalMapKind.Dungeon, 5)] = Pressure(
                "Elite Sprint!",
                Spec(EnemyMovementMode.Sprint, EnemyZombieKind.InsideElite, 20));
            t[(SurvivalMapKind.Dungeon, 10)] = Combo(
                "Wall of Steel!",
                Spec(EnemyMovementMode.Chase, EnemyZombieKind.InsideElite, 16),
                Spec(EnemyMovementMode.Orbit, EnemyZombieKind.InsideElite, 10));
            t[(SurvivalMapKind.Dungeon, 15)] = Pressure(
                "Caster Barrage!",
                Spec(EnemyMovementMode.Kite, EnemyZombieKind.InsideElite, 22, ranged: true));
            t[(SurvivalMapKind.Dungeon, 20)] = Combo(
                "Fly & Ground!",
                Spec(EnemyMovementMode.Fly, EnemyZombieKind.InsideElite, 12),
                Spec(EnemyMovementMode.Chase, EnemyZombieKind.InsideElite, 14));
            t[(SurvivalMapKind.Dungeon, 25)] = Pressure(
                "Charge Line!",
                Spec(EnemyMovementMode.Charge, EnemyZombieKind.InsideElite, 24));
            t[(SurvivalMapKind.Dungeon, 30)] = Combo(
                "Ambush Triangle!",
                Spec(EnemyMovementMode.Charge, EnemyZombieKind.InsideElite, 10),
                Spec(EnemyMovementMode.Strafe, EnemyZombieKind.InsideElite, 10),
                Spec(EnemyMovementMode.Kite, EnemyZombieKind.InsideElite, 8, ranged: true));
            t[(SurvivalMapKind.Dungeon, 35)] = Pressure(
                "Orbit Cage!",
                Spec(EnemyMovementMode.Orbit, EnemyZombieKind.InsideElite, 26, elite: true));

            // --- Crypt (stage hold R50) ---
            t[(SurvivalMapKind.Crypt, 5)] = Pressure(
                "Crypt Sprint!",
                Spec(EnemyMovementMode.Sprint, EnemyZombieKind.InsideElite, 22));
            t[(SurvivalMapKind.Crypt, 10)] = Combo(
                "Bone Flank!",
                Spec(EnemyMovementMode.Strafe, EnemyZombieKind.InsideElite, 14),
                Spec(EnemyMovementMode.Chase, EnemyZombieKind.InsideElite, 12));
            t[(SurvivalMapKind.Crypt, 15)] = Pressure(
                "Winged Horde!",
                Spec(EnemyMovementMode.Fly, EnemyZombieKind.InsideElite, 26));
            t[(SurvivalMapKind.Crypt, 20)] = Combo(
                "Charge & Orbit!",
                Spec(EnemyMovementMode.Charge, EnemyZombieKind.InsideElite, 14),
                Spec(EnemyMovementMode.Orbit, EnemyZombieKind.InsideElite, 14));
            t[(SurvivalMapKind.Crypt, 25)] = Pressure(
                "Deadeye Pack!",
                Spec(EnemyMovementMode.Kite, EnemyZombieKind.InsideElite, 24, ranged: true));
            t[(SurvivalMapKind.Crypt, 30)] = Combo(
                "Crush Pattern!",
                Spec(EnemyMovementMode.Chase, EnemyZombieKind.InsideElite, 16, elite: true),
                Spec(EnemyMovementMode.Kite, EnemyZombieKind.InsideElite, 10, ranged: true));
            t[(SurvivalMapKind.Crypt, 35)] = Pressure(
                "Rampage Charge!",
                Spec(EnemyMovementMode.Charge, EnemyZombieKind.InsideElite, 28));
            t[(SurvivalMapKind.Crypt, 40)] = Combo(
                "Sky & Steel!",
                Spec(EnemyMovementMode.Fly, EnemyZombieKind.InsideElite, 14),
                Spec(EnemyMovementMode.Sprint, EnemyZombieKind.InsideElite, 14));
            t[(SurvivalMapKind.Crypt, 45)] = Pressure(
                "Final Pressure!",
                Spec(EnemyMovementMode.Strafe, EnemyZombieKind.InsideElite, 30, elite: true));

            // --- Unlimited: every 5 after R10; trash prelude on decade rounds too ---
            AddUnlimited(t, 15, Pressure(
                "Unlimited Sprint!",
                Spec(EnemyMovementMode.Sprint, EnemyZombieKind.Outside, 24)));
            AddUnlimited(t, 25, Combo(
                "Biome Flank!",
                Spec(EnemyMovementMode.Charge, EnemyZombieKind.Inside, 14),
                Spec(EnemyMovementMode.Kite, EnemyZombieKind.Inside, 12, ranged: true)));
            AddUnlimited(t, 35, Pressure(
                "Flying Tide!",
                Spec(EnemyMovementMode.Fly, EnemyZombieKind.Inside, 28)));
            AddUnlimited(t, 45, Combo(
                "Orbit Siege!",
                Spec(EnemyMovementMode.Orbit, EnemyZombieKind.InsideElite, 16),
                Spec(EnemyMovementMode.Chase, EnemyZombieKind.InsideElite, 14)));
            AddUnlimited(t, 55, Pressure(
                "Dungeon Charge!",
                Spec(EnemyMovementMode.Charge, EnemyZombieKind.InsideElite, 30)));
            AddUnlimited(t, 65, Combo(
                "Caster Wall!",
                Spec(EnemyMovementMode.Kite, EnemyZombieKind.InsideElite, 18, ranged: true),
                Spec(EnemyMovementMode.Strafe, EnemyZombieKind.InsideElite, 14)));
            AddUnlimited(t, 75, Pressure(
                "Elite Orbit!",
                Spec(EnemyMovementMode.Orbit, EnemyZombieKind.InsideElite, 32, elite: true)));
            AddUnlimited(t, 85, Combo(
                "Endgame Ambush!",
                Spec(EnemyMovementMode.Charge, EnemyZombieKind.InsideElite, 16),
                Spec(EnemyMovementMode.Fly, EnemyZombieKind.InsideElite, 12),
                Spec(EnemyMovementMode.Kite, EnemyZombieKind.InsideElite, 10, ranged: true)));
            AddUnlimited(t, 95, Pressure(
                "Last Push!",
                Spec(EnemyMovementMode.Sprint, EnemyZombieKind.InsideElite, 34, elite: true)));

            return t;
        }

        static void AddUnlimited(
            Dictionary<(SurvivalMapKind, int), ChallengeRoundDef> t,
            int round,
            ChallengeRoundDef def)
            => t[(SurvivalMapKind.Unlimited, round)] = def;

        static ChallengeRoundDef Pressure(string banner, params ChallengeSpawnSpec[] specs)
            => new(banner, ChallengeFlavor.Pressure, specs);

        static ChallengeRoundDef Combo(string banner, params ChallengeSpawnSpec[] specs)
            => new(banner, ChallengeFlavor.Combo, specs);

        static ChallengeSpawnSpec Spec(
            EnemyMovementMode mode,
            EnemyZombieKind kind,
            int count,
            bool ranged = false,
            bool elite = false)
            => new(mode, kind, count, ranged, elite);
    }
}
