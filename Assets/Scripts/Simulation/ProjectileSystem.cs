using System.Collections.Generic;
using UnityEngine;

namespace MCFight
{
    /// <summary> 纯逻辑投射物系统 </summary>
    public static class ProjectileSystem
    {
        /// <summary> 创建标准投射物 </summary>
        public static ProjectileData CreateDefault(
            int id, int team, float x, float y, float dirX, float dirY,
            int sourceId, string sourceMonsterId, float damage,
            float range, StatusEffectType[] statusOnHit = null)
        {
            return new ProjectileData
            {
                Id = id,
                Team = team,
                X = x, Y = y,
                DirX = dirX, DirY = dirY,
                Speed = BattleConstants.PROJECTILE_SPEED,
                RawDamage = damage,
                SourceId = sourceId,
                SourceMonsterId = sourceMonsterId,
                Kind = ProjectileKind.Default,
                MaxTravel = range * 1.15f,
                Traveled = 0,
                StatusOnHit = statusOnHit,
            };
        }

        /// <summary> 每帧 tick 所有投射物 </summary>
        public static void Tick(List<ProjectileData> projectiles, UnitList units, float dt)
        {
            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                var p = projectiles[i];

                // 直线飞行
                p.X += p.DirX * p.Speed * dt;
                p.Y += p.DirY * p.Speed * dt;
                p.Traveled += p.Speed * dt;

                // 追踪弹修正方向
                if (p.Kind == ProjectileKind.HarbHoming || p.Kind == ProjectileKind.ProwlerMissile)
                    SteerTowardTarget(ref p, units, dt);

                // 命中检测
                int hitIdx = FindHitTarget(p, units);
                if (hitIdx >= 0)
                {
                    ResolveHit(ref p, hitIdx, units);
                    // 穿透弹不消失
                    bool piercing = p.Kind == ProjectileKind.ForsakenSonic;
                    if (!piercing)
                    {
                        projectiles.RemoveAt(i);
                        continue;
                    }
                    else
                    {
                        projectiles[i] = p; // 更新穿透弹状态
                        continue;
                    }
                }

                // 写回修改
                projectiles[i] = p;

                // 超出范围
                if (p.MaxTravel > 0 && p.Traveled > p.MaxTravel || IsOffField(p))
                {
                    projectiles.RemoveAt(i);
                }
            }
        }

        static void SteerTowardTarget(ref ProjectileData p, UnitList units, float dt)
        {
            if (p.TargetId < 0) return;
            int idx = FindUnitById(units, p.TargetId);
            if (idx < 0) return;
            ref var target = ref units[idx];
            if (target.State == UnitStateEnum.Dead) return;

            float dx = target.X - p.X;
            float dy = target.Y - p.Y;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            if (d < 0.01f) return;

            float steer = p.HomingSteer > 0 ? p.HomingSteer : 4.5f * dt;
            float newDx = Mathf.Lerp(p.DirX, dx / d, steer);
            float newDy = Mathf.Lerp(p.DirY, dy / d, steer);
            float len = Mathf.Sqrt(newDx * newDx + newDy * newDy);
            if (len > 0.01f)
            {
                p.DirX = newDx / len;
                p.DirY = newDy / len;
            }
        }

        static int FindHitTarget(ProjectileData p, UnitList units)
        {
            for (int i = 0; i < units.Count; i++)
            {
                ref var u = ref units[i];
                if (u.State == UnitStateEnum.Dead) continue;
                if (u.Team == p.Team) continue;

                float dx = u.X - p.X;
                float dy = u.Y - p.Y;
                float hitRange = u.Radius + BattleConstants.PROJECTILE_HIT_PAD;

                // 穿透弹用半宽
                if (p.Kind == ProjectileKind.ForsakenSonic)
                {
                    hitRange = u.Radius + (p.PierceHalfWidth > 0 ? p.PierceHalfWidth : 12f);
                }

                if (dx * dx + dy * dy <= hitRange * hitRange)
                {
                    // 穿透弹检查是否已命中
                    if (p.HitEnemyIds != null && p.HitEnemyIds.Contains(u.Id))
                        continue;
                    return i;
                }
            }
            return -1;
        }

        static void ResolveHit(ref ProjectileData p, int targetIdx, UnitList units)
        {
            ref var target = ref units[targetIdx];
            // 找到攻击者
            int sourceIdx = FindUnitById(units, p.SourceId);
            ref var source = ref units[sourceIdx >= 0 ? sourceIdx : 0];

            if (p.ExplodeRadius > 0)
            {
                // 爆炸 AOE
                for (int i = 0; i < units.Count; i++)
                {
                    ref var u = ref units[i];
                    if (u.State == UnitStateEnum.Dead || u.Team == p.Team) continue;
                    float d = DamageSystem.Dist(p.X, p.Y, u.X, u.Y);
                    if (d <= p.ExplodeRadius + u.Radius)
                        DamageSystem.DealDamage(ref u, p.RawDamage, DamageCategory.Ranged, ref source, units);
                }
            }
            else
            {
                DamageSystem.DealDamage(ref target, p.RawDamage, DamageCategory.Ranged, ref source, units);
            }

            // 施加状态
            if (p.StatusOnHit != null)
                StatusEffectSystem.ApplyAll(ref target, p.StatusOnHit);

            // 飞行近战设置脆弱窗口
            if (source.MoveType == MoveType.Fly && source.AttackType == AttackType.Melee)
                source.VulnerableWindow = BattleConstants.FLY_MELEE_VULN_WINDOW;

            // 穿透弹记录已命中
            if (p.Kind == ProjectileKind.ForsakenSonic)
            {
                if (p.HitEnemyIds == null) p.HitEnemyIds = new List<int>();
                p.HitEnemyIds.Add(target.Id);
            }
        }

        static int FindUnitById(UnitList units, int id)
        {
            for (int i = 0; i < units.Count; i++)
                if (units[i].Id == id) return i;
            return -1;
        }

        static bool IsOffField(ProjectileData p)
        {
            const float margin = 24f;
            return p.X < -margin || p.X > BattleConstants.FIELD_WIDTH + margin ||
                   p.Y < -margin || p.Y > BattleConstants.FIELD_HEIGHT + margin;
        }
    }
}
