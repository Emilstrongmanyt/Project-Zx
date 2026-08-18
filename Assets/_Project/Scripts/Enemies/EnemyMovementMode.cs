namespace ProjectZx.Enemies
{
    /// <summary>
    /// Locomotion role for trash (and light boss flavour). Attacks stay separate
    /// (melee / ranged / breath / bolts).
    /// </summary>
    public enum EnemyMovementMode
    {
        /// <summary>Straight chase — default melee.</summary>
        Chase = 0,
        /// <summary>Hold preferred band; used with ranged attacks.</summary>
        Kite = 1,
        /// <summary>Chase with periodic speed bursts (ground melee).</summary>
        Sprint = 2,
        /// <summary>Chase/kite with player speed cap + chill immune.</summary>
        Fly = 3,
        /// <summary>Wind-up then dash through the player line.</summary>
        Charge = 4,
        /// <summary>Circle at mid range; close briefly for melee.</summary>
        Orbit = 5,
        /// <summary>Zigzag approach while closing.</summary>
        Strafe = 6
    }
}
