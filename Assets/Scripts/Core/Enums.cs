namespace MCFight
{
    /// <summary> 移动类型 </summary>
    public enum MoveType { Ground, Fly }

    /// <summary> 攻击类型 </summary>
    public enum AttackType { Melee, Ranged }

    /// <summary> 单位状态枚举 </summary>
    public enum UnitStateEnum { Idle, Chase, Attack, Dead }

    /// <summary> 伤害类型 </summary>
    public enum DamageCategory { Melee, Ranged, Beam, Explosion, True }

    /// <summary> 状态效果类型 </summary>
    public enum StatusEffectType { Poison, Burn, Wither, Slow, Fear, Freeze, Stun }

    /// <summary> 投射物类型 </summary>
    public enum ProjectileKind
    {
        Default,
        HarbWither,
        HarbHoming,
        HarbLaser,
        RevenantBone,
        ForsakenSonic,
        IceBomb,
        ProwlerMissile
    }

    /// <summary> 区域效果类型 </summary>
    public enum AreaEffectType
    {
        LavaPatch,
        FrostZone,
        SandTornado,
        LinearTornado,
        VoidRune,
        Shockwave,
        Meteor,
        ObeliskBarrage,
        FallingObelisk,
        ConeStrike,
        ArcWave,
        PollutionZone,
    }

    /// <summary> 光束类型 </summary>
    public enum BeamKind { Tremor, HarbingerDeath, ProwlerRay }
}
