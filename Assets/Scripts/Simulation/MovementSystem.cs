using System.Collections.Generic;
using UnityEngine;

namespace MCFight
{
    /// <summary> 纯逻辑移动与碰撞系统 </summary>
    public static class MovementSystem
    {
        /// <summary> 追击目标 </summary>
        public static void ChaseTowardTarget(ref UnitState unit, ref UnitState target, float dt)
        {
            unit.State = UnitStateEnum.Chase;
            float dx = target.X - unit.X;
            float dy = target.Y - unit.Y;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            if (d > 0.01f)
            {
                unit.X += (dx / d) * unit.MoveSpeed * dt;
                unit.Y += (dy / d) * unit.MoveSpeed * dt;
            }
        }

        /// <summary> 设置朝向（带死区，避免抖动） </summary>
        public static void SetFacing(ref UnitState unit, float targetX)
        {
            float dx = targetX - unit.X;
            if (Mathf.Abs(dx) > BattleConstants.FACING_DEAD_ZONE)
                unit.Facing = dx >= 0 ? 1f : -1f;
        }

        /// <summary> 全局碰撞分离：每帧对所有单位（包括敌对）施加推开力 </summary>
        public static void SeparateAllUnits(UnitList units, float dt)
        {
            for (int i = 0; i < units.Count; i++)
            {
                ref var unit = ref units[i];
                if (unit.State == UnitStateEnum.Dead) continue;

                // 食人妖免疫击退
                bool immuneKnockback = unit.HasTag("knockback_immune");

                float sx = 0, sy = 0;
                for (int j = 0; j < units.Count; j++)
                {
                    if (i == j) continue;
                    ref var other = ref units[j];
                    if (other.State == UnitStateEnum.Dead) continue;

                    float dx = unit.X - other.X;
                    float dy = unit.Y - other.Y;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float minDist = unit.Radius + other.Radius;

                    if (d > 0.01f && d < minDist)
                    {
                        float overlap = (minDist - d) / minDist;
                        float mult = unit.Team == other.Team ? 1f : BattleConstants.ENEMY_SEPARATION_MULT;
                        if (immuneKnockback) mult = 0f; // 免疫击退不受推力
                        sx += (dx / d) * overlap * mult;
                        sy += (dy / d) * overlap * mult;
                    }
                }

                if (!immuneKnockback)
                {
                    unit.X += sx * BattleConstants.SEPARATION_FORCE * dt;
                    unit.Y += sy * BattleConstants.SEPARATION_FORCE * dt;
                    DamageSystem.ClampToField(ref unit);
                }
            }
        }

        /// <summary> 随机游走（攻击间隔内） </summary>
        public static void IdleWander(ref UnitState unit, float dt, System.Random rng)
        {
            unit.State = UnitStateEnum.Idle;
            unit.DriftTimer -= dt;
            if (unit.DriftTimer <= 0)
            {
                unit.DriftAngle = (float)(rng.NextDouble() * Mathf.PI * 2);
                unit.DriftTimer = 0.35f + (float)(rng.NextDouble() * 0.85);
            }

            float speed = unit.MoveSpeed * BattleConstants.DRIFT_SPEED_MUL;
            float vx = Mathf.Cos(unit.DriftAngle) * speed;
            float vy = Mathf.Sin(unit.DriftAngle) * speed;

            float newX = unit.X + vx * dt;
            float newY = unit.Y + vy * dt;

            // 碰壁反弹
            float half = unit.FieldHalfExtent;
            if (newX < half || newX > BattleConstants.FIELD_WIDTH - half)
            {
                unit.DriftAngle = Mathf.PI - unit.DriftAngle;
                newX = Mathf.Clamp(newX, half, BattleConstants.FIELD_WIDTH - half);
            }
            if (newY < half || newY > BattleConstants.FIELD_HEIGHT - half)
            {
                unit.DriftAngle = -unit.DriftAngle;
                newY = Mathf.Clamp(newY, half, BattleConstants.FIELD_HEIGHT - half);
            }

            unit.X = newX;
            unit.Y = newY;
        }

        /// <summary> 跃击抛物线插值 </summary>
        public static void SetLeapArcPosition(
            ref UnitState unit,
            float fromX, float fromY,
            float toX, float toY,
            float t, float arcHeight)
        {
            float ease = t * (2 - t);  // ease-out
            unit.X = Mathf.Lerp(fromX, toX, ease);
            float baseY = Mathf.Lerp(fromY, toY, ease);
            float hop = Mathf.Sin(t * Mathf.PI) * arcHeight;
            unit.Y = baseY - hop;
            DamageSystem.ClampToField(ref unit);
        }
    }
}
