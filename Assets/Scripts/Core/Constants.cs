namespace MCFight
{
    /// <summary> 战斗全局常量 </summary>
    public static class BattleConstants
    {
        // 战场
        public const float FIELD_WIDTH = 1280f;
        public const float FIELD_HEIGHT = 720f;
        public const float FIELD_MID_X = 640f;

        // 模拟
        public const float TICK_DT = 1f / 60f;

        // 目标选择
        public const float STICKY_RANGE_BONUS = 30f;
        public const float TARGET_RETARGET_INTERVAL = 2.5f;
        public const float FLY_MELEE_VULN_WINDOW = 0.55f;
        public const float TARGET_RADIUS_PAD = 1.0f;
        public const float ANTI_ARTHROPOD_BIAS = 0.75f;

        // 碰撞分离
        public const float SEPARATION_FORCE = 180f;
        public const float ENEMY_SEPARATION_MULT = 2.5f;

        // 燃烧传播
        public const float BURN_SPREAD_RADIUS = 52f;

        // 投射物
        public const float PROJECTILE_SPEED = 280f;
        public const float PROJECTILE_HIT_PAD = 6f;

        // 动画时间
        public const float MELEE_ANIM_TIME = 0.25f;
        public const float AOE_ANIM_TIME = 0.4f;

        // 默认区域
        public const float DEFAULT_AOE_RADIUS = 64f;
        public const float DEFAULT_EXPLOSION_RADIUS = 90f;

        // 朝向
        public const float FACING_DEAD_ZONE = 4f;

        // 游走
        public const float DRIFT_SPEED_MUL = 0.72f;
        public const float FEAR_SPEED_MUL = 0.95f;

        // 商店
        public const int INITIAL_GOLD = 1000;
        public const int BULK_BUY_COUNT = 10;

        // 体量显示尺寸
        public const float SIZE_GIANT = 112f;
        public const float SIZE_BOSS = 56f;
        public const float SIZE_FLY = 40f;
        public const float SIZE_NORMAL = 40f;
    }
}
