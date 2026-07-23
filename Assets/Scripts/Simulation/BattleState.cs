using System.Collections.Generic;
using UnityEngine;

namespace MCFight
{
    /// <summary> 运行时单位状态（值类型，cache-friendly） </summary>
    public struct UnitState
    {
        // 身份
        public int Id;
        public int Team;
        public string MonsterId;

        // 位置与朝向
        public float X, Y;
        public float Facing;

        // 生命
        public float Hp;
        public float MaxHp;

        // 战斗属性
        public float Attack;
        public float Armor;
        public float ArmorToughness;
        public float MoveSpeed;
        public float AttackRange;
        public float AttackInterval;
        public float Radius;
        public MoveType MoveType;
        public AttackType AttackType;

        // 状态
        public UnitStateEnum State;
        public float AttackCooldown;
        public float AttackAnimTimer;
        public int TargetId;

        // 基础值
        public float BaseMoveSpeed;
        public float BaseAttackInterval;

        // 飞行近战脆弱窗口
        public float VulnerableWindow;

        // 状态效果
        public StatusEffectList StatusEffects;

        // 技能冷却
        public float SkillCooldown;

        // 技能状态数据
        public SkillStateMap SkillState;

        // 骑乘/下马
        public int RiderUnitId;
        public int MountUnitId;

        // 游走
        public float DriftAngle;
        public float DriftTimer;

        // 重选目标计时
        public float RetargetTimer;

        // 标签缓存
        public string[] Tags;

        public bool HasTag(string tag)
        {
            if (Tags == null) return false;
            for (int i = 0; i < Tags.Length; i++)
                if (Tags[i] == tag) return true;
            return false;
        }

        public float VisualHalfExtent
        {
            get
            {
                if (HasTag("giant")) return BattleConstants.SIZE_GIANT * 0.5f;
                if (HasTag("boss")) return BattleConstants.SIZE_BOSS * 0.5f;
                if (MoveType == MoveType.Fly) return BattleConstants.SIZE_FLY * 0.5f;
                return BattleConstants.SIZE_NORMAL * 0.5f;
            }
        }

        public float FieldHalfExtent => Mathf.Max(Radius, VisualHalfExtent);
    }

    /// <summary> 部署信息 </summary>
    public struct DeployedUnit
    {
        public string MonsterId;
        public int Team;
        public float X, Y;
    }

    /// <summary> 投射物数据 </summary>
    public struct ProjectileData
    {
        public int Id;
        public int Team;
        public float X, Y;
        public float DirX, DirY;
        public float Speed;
        public float RawDamage;
        public int SourceId;
        public string SourceMonsterId;
        public ProjectileKind Kind;
        public float ExplodeRadius;
        public StatusEffectType[] StatusOnHit;
        public float MaxTravel;
        public float Traveled;
        public List<int> HitEnemyIds;
        public float PierceHalfWidth;
        public float ArcRadius;
        public float ArcHalfRad;
        public int TargetId;
        public float HomingSteer;
    }

    /// <summary> 区域效果数据 </summary>
    public struct AreaEffectData
    {
        public int Id;
        public AreaEffectType Type;
        public int Team;
        public int SourceId;
        public float X, Y;
        public float DirX, DirY;
        public float Radius;
        public float Remaining;
        public float Duration;
        public float Damage;
        public DamageCategory DamageCategory;
        public StatusEffectType[] StatusOnTick;
        public float Angle;
        public float Length;
        public float HalfWidth;
        public float DotTimer;
        public float Speed;
        public List<int> HitEnemyIds;
        public float OrbitRadius;
        public float AngularSpeed;
        public float OrbitAngle;
        public int RingIndex;
        public int RingCount;
        public float RingInterval;
        public float RingTimer;
        public float FallDuration;
        public float FallProgress;
        public float PctMaxHpDamage;
    }

    /// <summary> 光束数据 </summary>
    public struct ActiveBeamData
    {
        public int Id;
        public int Team;
        public int SourceId;
        public int TargetId;
        public float OriginX, OriginY;
        public float DirX, DirY;
        public float Length;
        public float HalfWidth;
        public float Remaining;
        public float TickAccumulator;
        public int TicksRemaining;
        public float DamagePerTick;
        public float PctMaxHpPerTick;
        public string SourceMonsterId;
        public BeamKind Kind;
        public StatusEffectType[] StatusOnTick;
    }

    /// <summary> 单位列表（支持 ref 索引访问，避免 List 索引器限制） </summary>
    public class UnitList
    {
        public UnitState[] Items = new UnitState[256];
        public int Count = 0;
        private const int INITIAL_CAPACITY = 256;

        public ref UnitState this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                    return ref Items[0];
                return ref Items[index];
            }
        }

        public void Add(UnitState unit)
        {
            if (Count >= Items.Length)
                System.Array.Resize(ref Items, Items.Length * 2);
            Items[Count] = unit;
            Count++;
        }

        public void RemoveDead()
        {
            int j = 0;
            for (int i = 0; i < Count; i++)
                if (Items[i].State != UnitStateEnum.Dead)
                    Items[j++] = Items[i];
                else
                {
                    // 处理骑手下马
                    if (Items[i].RiderUnitId >= 0)
                    {
                        // 标记骑手下马（在外部处理）
                    }
                }
            Count = j;
        }

        public ref UnitState FindById(int id)
        {
            for (int i = 0; i < Count; i++)
                if (Items[i].Id == id) return ref Items[i];
            return ref Items[0];
        }

        public int FindIndexById(int id)
        {
            for (int i = 0; i < Count; i++)
                if (Items[i].Id == id) return i;
            return -1;
        }
    }

    /// <summary> 战斗状态快照 </summary>
    public class BattleState
    {
        public UnitList Units = new();
        public List<ProjectileData> Projectiles = new();
        public List<AreaEffectData> AreaEffects = new();
        public List<ActiveBeamData> ActiveBeams = new();
        public List<VFXEvent> VFXEvents = new();

        public int Tick;
        public int Winner = -1;
        public float ElapsedTime;
        public System.Random RNG;

        public int NextIdCounter = 1;
        public int NextId() => NextIdCounter++;
    }

    public struct VFXEvent
    {
        public int Id;
        public string Path;
        public float X, Y;
        public float Scale;
        public float Lifetime;
        public int Team;

        public static VFXEvent Create(string path, float x, float y, float scale = 0.5f, float lifetime = 1f, int team = -1)
            => new VFXEvent { Id = -1, Path = path, X = x, Y = y, Scale = scale, Lifetime = lifetime, Team = team };
    }
}
