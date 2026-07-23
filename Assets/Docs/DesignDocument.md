# MC Fight Arena — Unity 重写设计文档

> **版本**: 1.1  
> **日期**: 2026-07-13  
> **状态**: 已完善，可开始实现

---

## 目录

1. [项目概述与目标](#1-项目概述与目标)
2. [架构总览](#2-架构总览)
3. [核心数据结构](#3-核心数据结构)
4. [战斗模拟引擎](#4-战斗模拟引擎)
5. [伤害与护甲系统](#5-伤害与护甲系统)
6. [状态效果系统](#6-状态效果系统)
7. [能力（技能）系统](#7-能力技能系统)
8. [怪物目录系统](#8-怪物目录系统)
9. [目标选择与 AI 行为](#9-目标选择与-ai-行为)
10. [碰撞与移动系统](#10-碰撞与移动系统)
11. [投射物与区域效果系统](#11-投射物与区域效果系统)
11b. [特殊机制系统](#11b-特殊机制系统)
12. [游戏流程（商店→部署→战斗→结算）](#12-游戏流程商店部署战斗结算)
13. [渲染与表现层](#13-渲染与表现层)
14. [配置系统](#14-配置系统)
15. [AI 自动平衡系统](#15-ai-自动平衡系统)
16. [网络联机对战](#16-网络联机对战)
17. [测试策略](#17-测试策略)
18. [目录结构](#18-目录结构)
19. [迁移计划](#19-迁移计划)

---

## 1. 项目概述与目标

### 1.1 项目背景

MC Fight Arena 是一个自动战斗游戏：双方玩家（蓝方/红方）用金币购买 Minecraft mod 中的怪物，部署在战场上，然后观看怪物自动交战，直到一方全灭。

当前 Web 版（TypeScript + React + Canvas）存在以下问题：
- **碰撞箱**：手动 O(n²) 圆形分离，缺乏物理引擎
- **攻击判定**：手动距离/线段/弧形计算，容易出 bug
- **行动逻辑**：所有 boss 状态写在 ~120 字段的大结构里，`battleEngine.ts` 是 1453 行的 if-else 链
- **边界判定**：手动钳制坐标
- **技能效果**：手动管理十几个 effect 数组
- **无联机功能**
- **无自动测试/平衡工具**

### 1.2 重写目标

| 目标 | 描述 |
|---|---|
| **可发布的游戏** | 完整的商店/部署/战斗/结算流程，有 UI、音效、特效 |
| **逻辑与表现分离** | 战斗模拟为纯 C# 逻辑层，可独立运行、可单元测试、可 headless 批量跑 |
| **组件化技能系统** | 用组合模式替代 if-else 链，每个 boss 的技能是独立组件 |
| **AI 自动平衡** | Headless 批量模拟 → 统计胜率 → 自动微调参数 → 生成报告 |
| **网络联机** | Server 权威架构，支持 1v1 在线对战 |
| **数据驱动** | 怪物数据用 ScriptableObject，可在 Inspector 中编辑，支持 JSON 导入导出 |

### 1.3 技术选型

| 领域 | 选择 | 理由 |
|---|---|---|
| 引擎 | Unity 2022 LTS+ | 成熟的 2D/3D 引擎，C# 生态 |
| 网络框架 | Netcode for GameObjects (NGO) | 官方方案，免费，与 Unity 集成最好 |
| UI 框架 | UI Toolkit (UIElements) 或 uGUI | 待定，倾向于 UI Toolkit（数据绑定更好） |
| 测试框架 | Unity Test Framework (NUnit) | 内置，支持 Play Mode + Edit Mode |
| 数据格式 | ScriptableObject + JSON | SO 用于编辑器，JSON 用于导入导出和配置覆盖 |
| 2D 物理 | Unity Physics2D (Box2D) | 内置，支持碰撞/触发/射线检测 |

---

## 2. 架构总览

### 2.1 分层架构

```
┌──────────────────────────────────────────────────────────────┐
│                       表现层 (Presentation)                    │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐        │
│  │ Shop UI  │ │Deploy UI │ │Battle UI │ │Result UI │        │
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘        │
│  SpriteRenderer / Animator / ParticleSystem / AudioSource     │
│  BattleView（读取 SimulationState，渲染到场景）                 │
├──────────────────────────────────────────────────────────────┤
│                       网络层 (Network)                         │
│  LobbyManager / Matchmaker / NetworkBattleServer              │
│  RPC 同步部署 → Server 跑 Simulation → 广播状态给 Client       │
├──────────────────────────────────────────────────────────────┤
│                    游戏流程层 (Game Flow)                       │
│  GameManager（状态机：Shop → Deploy → Battle → Result）        │
│  ShopSystem / DeploySystem / BattleOrchestrator              │
├──────────────────────────────────────────────────────────────┤
│                   战斗模拟引擎 (Simulation)                     │
│  ┌─────────────────────────────────────────────────────┐     │
│  │ BattleSimulator（纯 C#，不依赖 UnityEngine 渲染）     │     │
│  │  ├── UnitManager（单位生命周期）                      │     │
│  │  ├── CombatResolver（伤害结算）                      │     │
│  │  ├── TargetingSystem（目标选择）                      │     │
│  │  ├── MovementSystem（移动/碰撞）                      │     │
│  │  ├── AbilitySystem（技能调度）                        │     │
│  │  ├── StatusEffectSystem（状态效果）                   │     │
│  │  ├── ProjectileSystem（投射物）                       │     │
│  │  └── AreaEffectSystem（区域效果）                     │     │
│  └─────────────────────────────────────────────────────┘     │
│  ↑ 可独立运行（Edit Mode 测试 / Headless 批量平衡测试）        │
├──────────────────────────────────────────────────────────────┤
│                       数据层 (Data)                            │
│  ScriptableObject: MonsterDefSO / AbilityDefSO / StatusDefSO  │
│  BalanceConfigSO / ShopConfigSO                                │
│  MonsterDatabase（运行时索引，从 SO 加载为不可变数据）           │
├──────────────────────────────────────────────────────────────┤
│                    基础设施 (Infrastructure)                    │
│  Asset Management / Scene Mgmt / Input / Audio / Logging       │
└──────────────────────────────────────────────────────────────┘
```

### 2.2 核心设计原则

1. **逻辑与表现分离（最重要）**
   - `BattleSimulator` 是纯 C# 类，不继承 `MonoBehaviour`，不依赖 `UnityEngine` 渲染
   - 只使用 `UnityEngine.Vector2`（值类型，可在逻辑层安全使用）
   - 渲染层通过 `BattleView` 组件每帧读取 `SimulationState` 并更新 GameObject
   - 这样可以：单元测试、headless 批量模拟、服务器权威网络

2. **数据驱动**
   - 怪物属性 → `MonsterDefSO`
   - 技能参数 → `AbilityDefSO`（嵌套在 MonsterDefSO 或独立引用）
   - 全局平衡参数 → `BalanceConfigSO`
   - 运行时加载为不可变值类型 `struct`，避免装箱和引用修改

3. **组合优于继承**
   - 每个单位是 `GameObject` + 多个 `MonoBehaviour` 组件
   - 技能是独立组件，通过 `ISkillExecutor` 接口挂载
   - 不用继承体系（`BossUnit : Unit : Entity`），用组件组合

4. **确定性模拟**
   - 战斗模拟使用固定时间步长 `TICK_DT = 1/60f`（Unity 60fps，逻辑也 60Hz）
   - 所有随机数通过 `System.Random` + 固定种子，确保可复现
   - 网络模式下 server 跑模拟，client 只渲染

---

## 3. 核心数据结构

### 3.1 怪物定义（ScriptableObject）

```csharp
// Data/MonsterDefSO.cs
[CreateAssetMenu(fileName = "Monster_", menuName = "MC Fight/Monster Definition")]
public class MonsterDefSO : ScriptableObject
{
    [Header("身份")]
    public string monsterId;        // 如 "alexscaves_tremorzilla"
    public string displayName;      // 如 "撼地斯拉"
    public int price;               // 商店价格
    [TextArea] public string description;

    [Header("战斗属性")]
    public float hp = 100;
    public float attack = 10;
    public float armor = 0;
    public float armorToughness = 0;  // MC 护甲韧性
    public float moveSpeed = 58;     // px/s
    public float attackRange = 42;   // px
    public float attackInterval = 0.85f; // s
    public float radius = 18;        // 碰撞半径 px

    [Header("类型")]
    public MoveType moveType = MoveType.Ground;    // Ground / Fly
    public AttackType attackType = AttackType.Melee; // Melee / Ranged

    [Header("标签系统")]
    public string[] tags;           // boss, fly, arthropod, explosive, aoe_melee, fire_immune, ...

    [Header("命中附带状态")]
    public StatusEffectType[] onHitEffects;  // poison, burn, wither, slow, stun

    [Header("技能（可选）")]
    public AbilityDefSO[] abilities; // 该怪物的技能列表

    [Header("视觉资源")]
    public Sprite idleSprite;
    public Sprite attackSprite;
    public Sprite deadSprite;
}
```

### 3.2 运行时单位状态（值类型）

```csharp
// Simulation/UnitState.cs
public struct UnitState
{
    // 身份
    public int Id;
    public int Team;            // 0 或 1
    public string MonsterId;

    // 位置与朝向
    public float X, Y;
    public float Facing;        // 1 或 -1

    // 生命
    public float Hp;
    public float MaxHp;

    // 战斗属性（从 Def 复制，可被 buff/debuff 修改）
    public float Attack;
    public float Armor;
    public float MoveSpeed;
    public float AttackRange;
    public float AttackInterval;
    public float Radius;
    public MoveType MoveType;
    public AttackType AttackType;

    // 状态
    public UnitState State;    // Idle / Chase / Attack / Dead
    public float AttackCooldown;
    public float AttackAnimTimer;
    public int TargetId;       // -1 = 无目标

    // 基础值（用于减速恢复）
    public float BaseMoveSpeed;
    public float BaseAttackInterval;

    // 飞行近战脆弱窗口
    public float VulnerableWindow;

    // 状态效果（用列表，因为数量少）
    public StatusEffectList StatusEffects; // 自定义 struct，最多 8 个效果

    // 技能冷却
    public float SkillCooldown;

    // 技能状态数据（用 AnyValue 字典，每个 boss 自己管）
    // 这是一个可扩展的 KV 存储，替代 web 版的 120 个字段
    public SkillStateMap SkillState;
}
```

### 3.3 技能状态映射（替代 120 字段大结构）

Web 版的 `BattleUnit` 有 ~120 个字段，每个 boss 一组。Unity 版用 **KV 存储** 解决：

```csharp
// Simulation/SkillStateMap.cs
public struct SkillStateMap
{
    // 使用平行数组实现轻量 KV 存储（避免托管堆分配）
    // 最多 32 个键值对，足够任何单个 boss
    private int _count;
    private int[] _keys;    // 哈希值
    private float[] _floats;
    private int[] _ints;

    public void SetFloat(int keyHash, float value);
    public float GetFloat(int keyHash, float defaultValue = 0);
    public void SetInt(int keyHash, int value);
    public int GetInt(int keyHash, int defaultValue = 0);
    // ...
}

// 使用方式（在每个 boss 的 AbilityComponent 中）
public static class SkillKeys
{
    // 远古遗魂
    public static readonly int RemnantCastTimeLeft = "remnant_cast_time".GetHashCode();
    public static readonly int RemnantPendingSkill = "remnant_pending_skill".GetHashCode();
    public static readonly int RemnantQueuedSkill = "remnant_queued_skill".GetHashCode();
    public static readonly int RemnantObeliskCooldown = "remnant_obelisk_cd".GetHashCode();

    // 先驱者
    public static readonly int HarbAttackMode = "harb_attack_mode".GetHashCode();
    public static readonly int HarbModeTimer = "harb_mode_timer".GetHashCode();
    public static readonly int HarbChargeTimeLeft = "harb_charge_time".GetHashCode();
    // ...
}
```

### 3.4 战斗状态快照

```csharp
// Simulation/BattleState.cs
public class BattleState
{
    public List<UnitState> Units;
    public List<ProjectileData> Projectiles;
    public List<ShockwaveData> Shockwaves;
    public List<ActiveBeamData> ActiveBeams;
    public List<MeteorData> Meteors;
    public List<AreaEffectData> AreaEffects;  // 统一 lava/frost/tornado/obelisk 等
    public List<ConeStrikeData> ConeStrikes;
    public List<ArcWaveData> ArcWaves;

    public int Tick;
    public int Winner;  // -1 = 未结束, 0/1 = 胜方

    public float ElapsedTime;
    public Random RNG;  // 确定性随机数
}
```

### 3.5 投射物与效果数据

```csharp
public enum ProjectileKind
{
    Default, HarbWither, HarbHoming, HarbLaser,
    RevenantBone, ForsakenSonic, IceBomb, ProwlerMissile
}

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
    public List<int> HitEnemyIds;  // 穿透弹道已命中单位
    public float PierceHalfWidth;
    // 弧形声波
    public float ArcRadius;
    public float ArcHalfRad;
    public int TargetId;  // 追踪弹目标
}
```

---

## 4. 战斗模拟引擎

### 4.1 核心类

```csharp
// Simulation/BattleSimulator.cs
public class BattleSimulator
{
    private BattleState _state;
    private readonly TargetingSystem _targeting;
    private readonly MovementSystem _movement;
    private readonly CombatResolver _combat;
    private readonly AbilitySystem _abilities;
    private readonly StatusEffectSystem _statusEffects;
    private readonly ProjectileSystem _projectiles;
    private readonly AreaEffectSystem _areaEffects;

    public const float TICK_DT = 1f / 60f;  // 60Hz 逻辑帧
    public const float SEPARATION_FORCE = 180f;
    public const float STICKY_RANGE_BONUS = 30f;

    public BattleSimulator(BalanceConfigSO config, MonsterDatabase database) { ... }

    public void Initialize(List<DeployedUnit> deployments);
    public void Tick(float dt);           // 推进一帧
    public BattleState GetState();        // 获取当前状态（只读）
    public bool IsFinished { get; }
    public int Winner { get; }
}
```

### 4.2 每帧执行顺序

```
BattleSimulator.Tick(dt):
│
├─ Phase A: 全局效果更新（按顺序）
│   1. tickShockwaves(dt)
│   2. tickActiveBeams(dt)
│   3. tickHarbingerDeathBeams(dt)
│   4. tickMeteors(dt)
│   5. tickAreaEffects(dt)        // lava, frost zones
│   6. tickSandTornados(dt)
│   7. tickLinearSandTornados(dt)
│   8. tickArcWaves(dt)
│   9. tickVoidRunes(dt)
│   10. tickObeliskBarrages(dt)
│   11. tickFallingObelisks(dt)
│
├─ Phase B: 单位循环（每个存活单位）
│   for each unit in units:
│   │
│   ├─ B.1  tickStatusEffects(unit, dt)     // 状态效果 DoT
│   ├─ B.2  递减所有冷却计时器
│   ├─ B.3  检查引导/蓄力/跳跃状态 → 跳过
│   ├─ B.4  被动技能 tick（先驱者回血、暝煌龙陨石等）
│   ├─ B.5  恐惧状态 → 随机游走
│   ├─ B.6  娜迦特殊移动
│   ├─ B.7  强制重选目标计时
│   ├─ B.8  pickTarget(unit)              // 选择最近敌人
│   │
│   ├─ B.9  施法中 → tickCast(unit, dt)   // 继续施法
│   │
│   ├─ B.10 如果有技能组件：
│   │       abilitySystem.TryExecute(unit, target, dist)
│   │       如果技能释放 → continue
│   │
│   ├─ B.11 标准战斗逻辑：
│   │       if inMeleeRange && cooldown<=0 → meleeAttack
│   │       else if inRangedRange && cooldown<=0 → spawnProjectile
│   │       else if inRange → drift (攻击间隔游走)
│   │       else → chaseTowardTarget
│   │
│   └─ B.12 clampToField(unit)
│
├─ Phase C: 后处理
│   1. separateAllUnits(dt)           // 全局碰撞分离
│   2. updateProjectiles(dt)          // 投射物移动 + 命中检测
│   3. tickSpecialProjectiles(dt)     // 追踪弹/激光弹
│   4. tickDreadLichConversion()      // 尸巫击杀转化
│
├─ Phase D: 胜负判定
│   winner = checkWinner(units)
│
└─ Phase E: tick++, elapsedTime += dt
```

### 4.3 与 Web 版的关键差异

| 方面 | Web 版 | Unity 版 |
|---|---|---|
| 帧率 | 30Hz (setInterval) | 60Hz (Update) |
| 快照 | 每帧深拷贝所有数组（不可变） | 原地修改 + 仅在需要快照时复制 |
| 碰撞 | 手动 O(n²) 距离分离 | 逻辑层仍用手动分离（确定性），渲染层可用 Physics2D |
| 单位状态 | 120 字段大 struct | 值类型 struct + SkillStateMap KV 存储 |
| 技能调度 | tag if-else 链 | AbilityComponent 列表 + 接口分发 |
| 随机数 | Math.random()（不可复现） | System.Random + 种子（确定性） |

---

## 5. 伤害与护甲系统

### 5.1 MC 护甲公式（保持与 Web 版一致）

```csharp
public static class DamageSystem
{
    /// MC Java Edition CombatRules.getDamageAfterAbsorb
    /// g = clamp(armor - 4*dmg/(toughness+8), armor/5, 20)
    /// finalDamage = damage * (1 - g/25)
    /// 最大减伤 80%（g 上限 20）
    public static float GetDamageAfterArmor(float damage, float armor, float toughness = 0)
    {
        if (damage <= 0) return 0;
        if (armor <= 0) return damage;

        float g = Mathf.Min(20f,
            Mathf.Max(armor / 5f, armor - (4f * damage) / (toughness + 8f)));
        return damage * (1f - g / 25f);
    }
}
```

### 5.2 伤害结算管道

```
dealDamageToUnit(target, rawDamage, category):
│
├─ 1. 无敌检查
│   ├─ 紫水晶巨蟹埋地 → return 0
│   ├─ 炽燃遗魂防御中 → melee ×0.1, non-melee return 0
│
├─ 2. 格挡修正
│   └─ 骸骨斩首者: ranged ×0.5
│
├─ 3. 护甲减伤
│   └─ getDamageAfterArmor(dmg, target.armor, target.toughness)
│
├─ 4. 扣血
│   └─ target.hp -= finalDamage
│
└─ 5. 死亡检查
    └─ if hp <= 0 → state = Dead, trigger OnUnitDied
```

### 5.3 伤害类型

```csharp
public enum DamageCategory
{
    Melee,      // 标准近战
    Ranged,     // 投射物
    Beam,       // 光束（持续伤害）
    Explosion,  // 爆炸
    True        // 真实伤害（DoT，无视护甲但尊重无敌）
}
```

### 5.4 特殊伤害修正器

| 怪物 | 条件 | 修正 |
|---|---|---|
| 紫水晶巨蟹 | 埋地中 | 免疫一切伤害 |
| 炽燃遗魂 | 防御姿态 | 近战 ×0.1，非近战免疫 |
| 骸骨斩首者 | 远程格挡 tag | 远程 ×0.5 |
| 骸骨斩首者 | 对空 | 无法选择飞行单位为攻击目标 |
| 食人妖 | - | 免疫击退 + 免疫远程伤害（Ranged/Beam → 0） |
| 轻语灵 | 头部受伤 | 减半（简化为全局 ×0.5） |
| 独眼巨人 | - | 吞噬小型单位（maxHp ≤ 50）直接秒杀 |
| 铜羽泽鹗 | 攻击 | 无视护甲（护甲按 0 计算） |

---

## 6. 状态效果系统

### 6.1 效果定义

| 效果 | DPS | 持续 | 特殊机制 |
|---|---|---|---|
| Poison（中毒） | 2/s | 5s | - |
| Burn（燃烧） | 1/s | 10s | 范围传播（半径 52px） |
| Wither（凋零） | 3/s | 4s | - |
| Slow（减速） | - | 5s | 移速 ×0.7，攻击间隔 ×1/0.7 |
| Fear（恐惧） | - | 2s | 单位原地随机游走 |
| Freeze（冰冻） | - | 2s | 移速=0，攻击间隔=∞ |
| Stun（蛰晕） | - | 30s | 移速=0，移动类型变为 Ground（飞行单位被打落），仍可攻击 |

### 6.2 数据结构

```csharp
public struct StatusEffectInstance
{
    public StatusEffectType Type;
    public float Remaining;
    public float DotTimer;
    public MoveType OriginalMoveType; // 仅用于 Stun：记录蛰晕前的原始移动类型，效果结束时恢复
}

// 最多 8 个并发效果（用值类型数组，避免 GC）
public struct StatusEffectList
{
    private int _count;
    private StatusEffectInstance _effects0, _effects1, ... _effects7;

    public void Add(StatusEffectType type, float duration);
    public void Tick(UnitState unit, float dt);
    public bool Has(StatusEffectType type);
    public void Remove(StatusEffectType type);
}
```

### 6.3 DoT 结算

- DoT 每 1 秒结算一次（`dotTimer += dt; while (dotTimer >= 1) { dotTimer -= 1; dealTrueDamage(dps); }`）
- 真实伤害无视护甲，但尊重无敌（巨蟹埋地、遗魂防御）
- 燃烧效果在 DoT 结算时检查半径 52px 内的单位并传播

---

## 7. 能力（技能）系统

### 7.1 设计目标

Web 版的 boss 技能写在 `battleEngine.ts` 1453 行的 if-else 链中，极难维护。Unity 版用 **组件化能力系统** 替代。

### 7.2 架构

```
MonsterDefSO
  └── abilities: AbilityDefSO[]
        ├── AbilityDefSO (type=Beam, params...)
        ├── AbilityDefSO (type=ConeStrike, params...)
        └── AbilityDefSO (type=Summon, params...)

运行时：
UnitState.SkillState (KV 存储)
  ↑
AbilityComponent (MonoBehaviour，挂在 Prefab 上)
  ├── 尝试释放技能 → 返回 true/false
  ├── tick cast (施法中每帧调用)
  └── 读写 UnitState.SkillState 中的私有字段
```

### 7.3 技能定义（ScriptableObject）

```csharp
[CreateAssetMenu(fileName = "Ability_", menuName = "MC Fight/Ability Definition")]
public class AbilityDefSO : ScriptableObject
{
    public string abilityId;
    public string displayName;
    [TextArea] public string description;

    [Header("通用参数")]
    public float cooldown = 8f;
    public float castDuration = 0f;     // 施法时间（0=瞬发）
    public float engageRange = 200f;    // 释放此技能所需的最近距离
    public bool groundOnly = false;     // 只能对地面单位释放

    [Header("伤害")]
    public float baseDamage = 0f;
    public float pctMaxHpDamage = 0f;   // 百分比最大生命伤害
    public DamageCategory damageCategory = DamageCategory.Melee;

    [Header("附带状态")]
    public StatusEffectType[] statusOnHit;

    [Header("区域参数")]
    public float radius = 0f;           // AOE 半径
    public float angleDeg = 0f;         // 扇形角度
    public float length = 0f;           // 光束/锥形长度
    public float halfWidth = 0f;        // 光束半宽

    [Header] // 子类型特有参数用 SerializeField + 自定义 Inspector
    // ...
}
```

### 7.4 技能组件接口

```csharp
public interface IAbilityComponent
{
    /// 初始化单位技能状态（在单位创建时调用一次）
    void OnInit(ref UnitState unit, AbilityDefSO def);

    /// 每帧决策：尝试释放技能。返回 true 表示已释放（跳过普攻）
    bool TryExecute(ref UnitState unit, UnitState target, float dist,
                    BattleState state, float dt);

    /// 施法中每帧调用（如果 castDuration > 0）
    void TickCast(ref UnitState unit, BattleState state, float dt);

    /// 该单位的交战半径（替代全局 engageRange）
    float GetEngageRange(ref UnitState unit);

    /// 是否正在施法/引导/蓄力（阻止移动和普攻）
    bool IsBusy(ref UnitState unit);
}
```

### 7.5 技能类型枚举与基类

```csharp
public enum AbilityType
{
    // 通用型
    Melee,          // 标准近战
    AoeMelee,       // 范围近战
    Ranged,          // 远程投射物
    Beam,           // 持续光束

    // Boss 专用型（每个 boss 一个独立组件）
    LuxStomp,       // 暝煌龙践踏/甩尾
    LuxMeteor,      // 暝煌龙陨石雨
    LuxLeap,        // 暝煌龙跳跃
    TremorBeam,     // 撼地斯拉超能射线
    TremorRoar,     // 撼地龙恐吓怒吼
    RemnantSkill,   // 远古遗魂技能组
    HarbingerSkill, // 先驱者技能组
    WardenSonic,    // 监守者声波
    WadjetSkill,    // 瓦吉特技能组
    KoboCharge,     // 骸骨斩首者冲锋
    EnderTeleport,  // 末影傀儡传送
    MoscoTransform, // 诡异蚊鬼变身
    CrabBurrow,     // 紫水晶巨蟹埋地
    RevenantSkill,  // 炽燃遗魂技能组
    CoralLeap,      // 珊瑚跃击
    ForsakenSkill,  // 遗弃者技能组
    FrostmawSkill,  // 霜冻巨兽技能组
    YetiSkill,      // 雪怪首领技能组
    ProwlerSkill,   // 徘徊者技能组
    NagaContact,    // 娜迦接触伤害
    FarseerRay,     // 瞻远者激光
    DeepOneSkill,   // 深潜者法师技能组
    WarlockLaser,   // 渊灵术士激光雨
    NucleeperFuse,  // 核能苦力怕自爆
    DreadLichSummon,// 悚怖尸巫召唤
    CyclopsDevour,  // 独眼巨人吞噬

    // 升级怪物专用型（MonsterDesign.md 第 6 节）
    ElephantCharge,   // 大象蓄力冲锋
    DualMode,          // 远近双模式切换（渊灵蛮兵/深潜者骑士/渊灵/舐脑魔）
    BerserkerCombo,    // 炽燃狂魂挥砍/旋转交替
    TrollSmash,        // 食人妖重击
    EvokerFang,        // 唤魔者尖牙+召唤恼鬼
    VexWander,         // 恼鬼漫飞+近战
    MinoshroomCharge,  // 米诺菇蓄力冲锋
    MagnetronField,    // 磁控机兵递增伤害+击退
    PriestChannel,     // 渊灵祭司定身施法
    MurmurHead,        // 轻语灵头部投射+共享血量
    StraddlerSpawn,    // 跨座兽投射+生成蝌蚪
    CockatriceRapidFire,// 鸡蛇极速远程
    GoblinSweep,       // 链锤哥布林AOE+击退
    WitchPotion,       // 女巫4药水随机
    ConeBreath,         // 锥形持续喷射（寒冬狼/喷火甲虫）
    TarantulaSting,    // 沙漠蛛蜂蛰晕
    StymphalianShot,   // 铜羽泽鹗双发无视护甲
    SpiderRider,        // 国王蜘蛛+骷髅德鲁伊骑乘
    BlazeVolley,       // 烈焰人三连发
    FlyNagaCircle,     // 娜迦(飞行)盘旋+射击/俯冲
    SlowShot,           // 流浪者减速射击（通用附带效果，无需独立组件）
    WitherMelee,        // 凋零骷髅近战附带凋零（通用附带效果，无需独立组件）
    PoisonShot,         // 喷火甲虫/烈焰人等附带效果（通用，无需独立组件）
    BrainiacBucket,    // 舐脑魔废料桶投掷
}
```

### 7.6 技能组件示例（远古遗魂）

```csharp
public class RemnantAbilityComponent : IAbilityComponent
{
    private AbilityDefSO _def;
    private RemnantConfig _config;

    public void OnInit(ref UnitState unit, AbilityDefSO def)
    {
        _def = def;
        _config = def as RemnantAbilityDefSO?.Config ?? RemnantConfig.Default;
        // 初始化技能状态
        unit.SkillState.SetFloat(SkillKeys.RemnantCastTimeLeft, 0);
        unit.SkillState.SetInt(SkillKeys.RemnantPendingSkill, -1);
    }

    public bool TryExecute(ref UnitState unit, UnitState target, float dist, BattleState state, float dt)
    {
        if (IsBusy(ref unit)) return true;  // 正在施法

        if (unit.SkillCooldown > 0) return false;
        if (dist > GetEngageRange(ref unit)) return false;

        // 选择技能
        var skill = PickSkill(ref unit, ref target);
        if (skill == RemnantSkill.None) return false;

        // 开始施法
        StartCast(ref unit, skill);
        return true;
    }

    public void TickCast(ref UnitState unit, BattleState state, float dt)
    {
        float castTime = unit.SkillState.GetFloat(SkillKeys.RemnantCastTimeLeft);
        if (castTime <= 0) return;

        castTime -= dt;
        unit.SkillState.SetFloat(SkillKeys.RemnantCastTimeLeft, castTime);

        // 在施法特定时间点触发效果
        int skill = unit.SkillState.GetInt(SkillKeys.RemnantPendingSkill);
        float elapsed = _config.castDuration - castTime;

        switch ((RemnantSkill)skill)
        {
            case RemnantSkill.Bite:
                if (elapsed >= _config.biteHitTime)
                    // bite 伤害
                    break;
            case RemnantSkill.Sandstorm:
                if (elapsed >= _config.sandstormSpawnTime)
                    // 生成沙龙卷
                    break;
            // ...
        }

        if (castTime <= 0)
        {
            // 施法结束
            unit.SkillCooldown = _def.cooldown;
            unit.SkillState.SetFloat(SkillKeys.RemnantCastTimeLeft, 0);
        }
    }

    // ...
}
```

### 7.7 已实现 Boss 技能清单

| Boss | 技能数 | 核心机制 |
|---|---|---|
| **撼地斯拉** (tremorzilla) | 3 | 超能射线（持续光束）、辐照、范围践踏 |
| **暝煌龙** (luxtructosaurus) | 4 | 践踏/甩尾交替、陨石雨被动、跳跃践踏、范围攻击 |
| **远古遗魂** (ancient_remnant) | 5 | 咬、甩尾、沙暴（环绕龙卷）、践踏（锥形波）、方尖碑弹幕 |
| **先驱者** (harbinger) | 4 | 凋零导弹/激光模式切换、冲撞、死亡射线、被动回血 |
| **监守者** (warden) | 2 | 高伤近战、远程声波 |
| **瓦吉特** (wadjet) | 3 | 横扫（锥形）、线性龙卷、方尖碑弹幕 |
| **骸骨斩首者** (kobolediator) | 3 | 冲锋跳跃、三连斩、践踏 + 远程格挡 |
| **末影傀儡** (ender_golem) | 1 | 传送 + 超大范围攻击 |
| **诡异蚊鬼** (warped_mosco) | 2 | 吸血（地面阶段）、高机动远程（狂暴阶段，<20%血触发） |
| **紫水晶巨蟹** (amethyst_crab) | 2 | 埋地无敌、破土范围攻击 |
| **炽燃遗魂** (ignited_revenant) | 3 | 旋转火焰、火焰吐息、骨弹投射 + 防御格挡 |
| **珊瑚巨兽/珊瑚傀儡** (coral_golem/coralssus) | 1 | 跃击范围攻击（共享逻辑） |
| **遗弃者** (forsaken) | 4 | 咬、重锤、声波、远程声波弧波 + 被动回血 |
| **霜冻巨兽** (frostmaw) | 4 | 冰球、寒冰吐息、爪击、重击 + 冰冻区域 |
| **雪怪首领** (alpha_yeti) | 2 | 冰雪炸弹、狂暴 |
| **徘徊者** (prowler) | 4 | 电锯横扫、电锯切割、三连导弹、死亡射线 |
| **娜迦** (naga) | 1 | 接触穿刺伤害 + 蛇形移动 + 荆棘反伤 |
| **瞻远者** (farseer) | 2 | 百分比激光、爪击（模式切换） |
| **深潜者法师** (deep_one_mage) | 3 | 魔法弹、波浪攻击、横扫 |
| **渊灵术士** (deepling_warlock) | 1 | 标记→延迟→激光雨（7段伤害）→冷却 |
| **核能苦力怕** (nucleeper) | 1 | 延时自爆（中心1400伤害，不分敌我） |
| **悚怖尸巫** (dread_lich) | 1 | 远程 + 击杀转化召唤魔物 |
| **独眼巨人** (cyclops) | 1 | 吞噬小型单位秒杀 |

---

## 8. 怪物目录系统

### 8.1 数据库

```csharp
// Data/MonsterDatabase.cs
public class MonsterDatabase
{
    private Dictionary<string, MonsterDefSO> _byId;
    private List<MonsterDefSO> _sortedByPrice;  // 降序

    public void LoadAll()
    {
        // 从 Resources/Monsters/ 加载所有 MonsterDefSO
        var defs = Resources.LoadAll<MonsterDefSO>("Monsters");
        _byId = defs.ToDictionary(d => d.monsterId);
        _sortedByPrice = defs.OrderByDescending(d => d.price).ToList();
    }

    public MonsterDefSO GetById(string id);
    public IReadOnlyList<MonsterDefSO> GetAllSortedByPrice();
}
```

### 8.2 怪物分类

| 类别 | 数量 | 特点 |
|---|---|---|
| **Boss（有独立技能模块）** | 26 | 有 `IAbilityComponent`，独特的技能循环 |
| **普通怪物（数据驱动）** | ~50 | 只有基础属性 + 通用攻击模式（近战/远程/AOE/自爆） |

### 8.3 怪物属性推断规则（从 Web 版 infer.ts 迁移）

| 属性 | 推断规则 |
|---|---|
| moveType | 描述含"飞行" → Fly，否则 Ground |
| attackType | 描述含"远程/射击/投掷/射线/吐息" → Ranged，否则 Melee |
| attackRange | Ranged: boss=220, 普通=160; Melee: 42 |
| moveSpeed | Fly: 72, Boss: 48, 普通: 58 |
| attackInterval | Ranged: 1.1s, Melee: 0.85s |
| radius | Giant(3个): 56, Boss: 28, Fly: 16, 普通: 18 |
| tags | boss/fly/arthropod/explosive/aoe_melee/meteor_passive/fire_immune |
| onHitEffects | 中毒→poison, 火焰→burn, 凋零→wither, 减速→slow |

### 8.4 商店配置

```csharp
public static class ShopConfig
{
    public const int INITIAL_GOLD = 1000;
    public const int BULK_BUY_COUNT = 10;
}
```

---

## 9. 目标选择与 AI 行为

### 9.1 目标选择流程

```
pickTarget(unit):
│
├─ 1. 获取所有存活敌方单位 enemiesOf(unit)
│     └─ 如果为空 → 返回 null（单位待机）
│
├─ 2. 计算交战半径 engageRange(unit)
│     ├─ 有技能组件 → abilityComponent.GetEngageRange()
│     └─ 默认 → unit.attackRange
│
├─ 3. Sticky 目标保持
│     ├─ 如果 !forceRetarget 且有当前目标
│     │   └─ 如果 canPickEnemy 且 dist <= range + STICKY_RANGE_BONUS(30)
│     │       → 保持当前目标
│
├─ 4. pickNearestTarget(unit, enemies)
│     ├─ 对每个敌人计算距离
│     ├─ 如果有 anti_arthropod tag 且敌人是飞行单位 → 距离 ×0.75（优先攻击）
│     └─ 返回距离最近的
│
└─ 5. 每 TARGET_RETARGET_INTERVAL(2.5s) 强制重选
```

### 9.2 对空规则

**通用规则（详见 MonsterDesign.md 第 2.6 节）：**
- 飞行单位的所有攻击（近战 + 远程）均可对空
- 地面远程单位的大多数攻击可对空
- 地面近战单位不能攻击飞行单位（对空弱点）
- 例外会在怪物设计中明确标注（如"仅地面单位"的技能）

**飞行近战脆弱窗口：**
- 飞行近战单位攻击时设置 `VulnerableWindow = 0.55s`
- 在此窗口内，地面近战可以反击该飞行单位

```csharp
bool CanTargetForAttack(UnitState attacker, UnitState target, bool allowAntiAir)
{
    if (allowAntiAir) return true;
    // 飞行单位所有攻击均可对空
    if (attacker.MoveType == MoveType.Fly) return true;
    // 地面远程单位大多数可对空
    if (attacker.AttackType == AttackType.Ranged) return true;
    // 目标是地面单位 → 可攻击
    if (target.MoveType == MoveType.Ground) return true;
    // 地面近战 → 只能打有脆弱窗口的飞行单位
    return target.VulnerableWindow > 0;
}
```

**特殊对空限制（在怪物技能中标注）：**
- 骸骨斩首者：无法选择飞行单位为攻击目标（所有技能仅地面）
- 唤魔者尖牙：仅地面单位
- 部分地面技能标注"仅地面单位"时，对飞行目标自动跳过

### 9.3 标准战斗 AI（无技能的普通怪物）

```
if (inMeleeRange && attackCooldown <= 0):
    if (attackType == AoeMelee): aoeMeleeAttack(target, radius)
    else: meleeAttack(target)
    attackCooldown = attackInterval

elif (inRangedRange && attackCooldown <= 0):
    spawnProjectile(target)
    attackCooldown = attackInterval

elif (inRange && attackCooldown > 0):
    // 攻击间隔内随机游走
    drift(speedMul=0.72)

else:
    chaseTowardTarget(target)
```

---

## 10. 碰撞与移动系统

### 10.1 设计决策：逻辑层手动碰撞 vs Physics2D

**方案：逻辑层手动碰撞分离（确定性）**

理由：
- 自动平衡测试需要确定性（相同输入 → 相同输出）
- Physics2D 在不同机器上可能有浮点差异
- Headless 模式下 Physics2D 行为可能有差异
- Web 版的手动分离已经验证可行

但渲染层可以用 Physics2D 做视觉碰撞（投影阴影等非关键效果）。

### 10.2 碰撞分离算法

```csharp
public void SeparateAllUnits(List<UnitState> units, float dt)
{
    for (int i = 0; i < units.Count; i++)
    {
        if (units[i].State == UnitState.Dead) continue;
        float sx = 0, sy = 0;

        for (int j = 0; j < units.Count; j++)
        {
            if (i == j || units[j].State == UnitState.Dead) continue;

            float dx = units[i].X - units[j].X;
            float dy = units[i].Y - units[j].Y;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            float minDist = units[i].Radius + units[j].Radius;

            if (d > 0 && d < minDist)
            {
                float overlap = (minDist - d) / minDist;
                // 敌对推力 2.5×，盟友 1×
                float mult = units[i].Team == units[j].Team ? 1f : 2.5f;
                sx += (dx / d) * overlap * mult;
                sy += (dy / d) * overlap * mult;
            }
        }

        units[i].X += sx * SEPARATION_FORCE * dt;
        units[i].Y += sy * SEPARATION_FORCE * dt;
        ClampToField(ref units[i]);
    }
}
```

### 10.3 移动

```csharp
public void ChaseTowardTarget(ref UnitState unit, UnitState target, float dt)
{
    unit.State = UnitState.Chase;
    float dx = target.X - unit.X;
    float dy = target.Y - unit.Y;
    float d = Mathf.Sqrt(dx * dx + dy * dy);
    if (d > 0.01f)
    {
        unit.X += (dx / d) * unit.MoveSpeed * dt;
        unit.Y += (dy / d) * unit.MoveSpeed * dt;
    }
}

public void IdleWander(ref UnitState unit, float dt)
{
    unit.State = UnitState.Idle;
    // 随机角度游走，碰壁反弹
    // 速度 ×0.72
}
```

### 10.4 跃击（抛物线插值）

```csharp
public void SetLeapArcPosition(ref UnitState unit, float t, float arcHeight)
{
    float ease = t * (2 - t);  // ease-out
    unit.X = Mathf.Lerp(unit.LeapFromX, unit.LeapToX, ease);
    float baseY = Mathf.Lerp(unit.LeapFromY, unit.LeapToY, ease);
    float hop = Mathf.Sin(t * Mathf.PI) * arcHeight;
    unit.Y = baseY - hop;
    ClampToField(ref unit);
}
```

### 10.5 边界钳制

```csharp
public static void ClampToField(ref UnitState unit)
{
    float half = Mathf.Max(unit.Radius, GetUnitVisualHalfExtent(unit));
    unit.X = Mathf.Clamp(unit.X, half, BATTLE_FIELD_WIDTH - half);
    unit.Y = Mathf.Clamp(unit.Y, half, BATTLE_FIELD_HEIGHT - half);
}
```

---

## 11. 投射物与区域效果系统

### 11.1 投射物系统

```csharp
public class ProjectileSystem
{
    public void Tick(List<ProjectileData> projectiles, List<UnitState> units, float dt)
    {
        for (int i = projectiles.Count - 1; i >= 0; i--)
        {
            var p = projectiles[i];
            // 直线飞行
            p.X += p.DirX * p.Speed * dt;
            p.Y += p.DirY * p.Speed * dt;
            p.Traveled += p.Speed * dt;

            // 追踪弹修正方向
            if (p.Kind == ProjectileKind.HarbHoming)
                SteerTowardTarget(ref p, units, dt);

            // 命中检测
            var hit = FindHitTarget(p, units);
            if (hit != null)
            {
                ResolveHit(ref p, ref hit);
                if (p.Kind != PiercingKind)  // 穿透弹不消失
                    projectiles.RemoveAt(i);
            }
            else if (p.Traveled > p.MaxTravel || IsOffField(p))
            {
                projectiles.RemoveAt(i);
            }
        }
    }
}
```

### 11.2 投射物类型

| 类型 | 行为 | 特殊 |
|---|---|---|
| Default | 直线飞行，命中第一个敌人 | 可被挡枪 |
| HarbHoming | 追踪目标，3发散射 | 凋零效果 |
| HarbLaser | 直线高速 | - |
| RevenantBone | 直线 | - |
| ForsakenSonic | 弧形声波带 | 穿透，每敌只命中一次 |
| IceBomb | 抛物线 | 落地生成冰冻区域 |
| ProwlerMissile | 追踪，3连发 | 爆炸范围 |

### 11.3 区域效果统一管理

Web 版有十几个独立的 effect 数组。Unity 版统一为 `AreaEffectSystem`：

```csharp
public enum AreaEffectType
{
    LavaPatch,       // 熔岩（持续 DoT）
    FrostZone,      // 冰冻区域（减速 + DoT）
    SandTornado,    // 环绕龙卷风
    LinearTornado,  // 直线龙卷风
    VoidRune,       // 虚空符文
    Shockwave,      // 冲击波（扩散 + 伤害）
    Meteor,          // 陨石（下落 + 爆炸）
    ObeliskBarrage, // 方尖碑弹幕
    FallingObelisk, // 下落方尖碑
    ConeStrike,     // 锥形打击
    ArcWave,         // 弧形波
    PollutionZone,   // 污染区域（舐脑魔废料桶：DoT + 中毒 + 减速，仅地面）
}

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
    // 类型特有参数
    public float Angle;
    public float Length;
    public float HalfWidth;
    public List<int> HitEnemyIds;
}
```

### 11.4 光束系统

光束是持续伤害的线段，每帧重新计算起点和方向：

```csharp
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
    public BeamKind Kind;  // Tremor, HarbingerDeath, ProwlerRay
    public StatusEffectType[] StatusOnTick;
}
```

命中检测：点到线段距离 ≤ `halfWidth + target.radius`

---

## 11b. 特殊机制系统

### 11b.1 击退系统

部分怪物攻击附带击退效果，将目标推开一定距离。

```csharp
// 在 CombatResolver 中
public static void ApplyKnockback(ref UnitState target, float knockbackDist, float fromX, float fromY)
{
    // 食人妖免疫击退
    if (target.HasTag("knockback_immune")) return;
    if (target.StatusEffects.Has(StatusEffectType.Stun)) return; // 蛰晕单位不免疫击退

    float dx = target.X - fromX;
    float dy = target.Y - fromY;
    float d = Mathf.Sqrt(dx * dx + dy * dy);
    if (d > 0.01f)
    {
        target.X += (dx / d) * knockbackDist;
        target.Y += (dy / d) * knockbackDist;
    }
    ClampToField(ref target);
}
```

**击退参数来源：** 每个怪物的技能中定义 `knockback` 字段（单位 px）。瞬时施加，不是持续力。

**已使用击退的怪物：**

| 怪物 | 击退距离 | 触发条件 |
|---|---|---|
| 磁控机兵 | 20 px | 每次近战命中 |
| 跨座兽 | 10 px | 远程命中 |
| 链锤哥布林 | 10 px | AOE 命中 |
| 深潜者法师 | 80 px（水波）/ 30 px（甩动） | 对应技能命中 |
| 食人妖 | 免疫 | 自身不受击退影响 |

**免疫标签：** `knockback_immune`（食人妖拥有此标签）

### 11b.2 多体节系统（娜迦）

娜迦由头部 + 多个体节组成，体节仅是位置数据，不是独立单位。

**设计原则：体节不进入单位列表，不参与目标选择/投射物碰撞/碰撞分离。**

```csharp
// 在 NagaAbilityComponent 中管理
public class NagaAbilityComponent : IAbilityComponent
{
    private const int MAX_SEGMENTS = 8;
    private const float SEGMENT_SPACING = 16f; // 体节间距 px
    private const float SEGMENT_SIZE = 12f;    // 体节碰撞半径 px
    private const float CONTACT_COOLDOWN = 0.5f; // 共享接触冷却
    private const float CONTACT_RANGE_PAD = 4f;

    // 体节位置存储在 SkillStateMap 中：
    // seg_0_x, seg_0_y, seg_1_x, seg_1_y, ... seg_7_x, seg_7_y
    // contact_cd: 共享接触伤害冷却
    // seg_count: 当前体节数

    public void TickCast(ref UnitState unit, BattleState state, float dt)
    {
        // 1. 更新体节数量（随 HP 变化）
        int segCount = Mathf.Clamp(Mathf.RoundToInt(3 + 5 * unit.Hp / unit.MaxHp), 3, 8);
        unit.SkillState.SetInt(SkillKeys.NagaSegmentCount, segCount);

        // 2. 链式跟随：每节追随前一节位置
        // 体节[0] 追随头部
        // 体节[i] 追随体节[i-1]
        for (int i = 0; i < segCount; i++)
        {
            float prevX = (i == 0) ? unit.X : unit.SkillState.GetFloat(SkillKeys.NagaSegX(i - 1));
            float prevY = (i == 0) ? unit.Y : unit.SkillState.GetFloat(SkillKeys.NagaSegY(i - 1));
            float curX = unit.SkillState.GetFloat(SkillKeys.NagaSegX(i), prevX);
            float curY = unit.SkillState.GetFloat(SkillKeys.NagaSegY(i), prevY);

            float dx = curX - prevX, dy = curY - prevY;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            if (d > SEGMENT_SPACING)
            {
                // 保持间距
                float lerp = (d - SEGMENT_SPACING) / d;
                curX = prevX + dx * lerp;
                curY = prevY + dy * lerp;
            }
            unit.SkillState.SetFloat(SkillKeys.NagaSegX(i), curX);
            unit.SkillState.SetFloat(SkillKeys.NagaSegY(i), curY);
        }

        // 3. 接触伤害（共享冷却）
        float cd = unit.SkillState.GetFloat(SkillKeys.NagaContactCd, 0);
        cd -= dt;
        unit.SkillState.SetFloat(SkillKeys.NagaContactCd, cd);

        if (cd <= 0)
        {
            foreach (var enemy in state.Units)
            {
                if (enemy.Team == unit.Team || enemy.State == UnitState.Dead) continue;

                bool hit = false;
                // 检查头部
                if (DistSq(unit.X, unit.Y, enemy.X, enemy.Y) <= Mathf.Pow(SEGMENT_SIZE + enemy.Radius + CONTACT_RANGE_PAD, 2))
                    hit = true;

                // 检查每个体节
                if (!hit)
                {
                    for (int i = 0; i < segCount; i++)
                    {
                        float sx = unit.SkillState.GetFloat(SkillKeys.NagaSegX(i));
                        float sy = unit.SkillState.GetFloat(SkillKeys.NagaSegY(i));
                        if (DistSq(sx, sy, enemy.X, enemy.Y) <= Mathf.Pow(SEGMENT_SIZE + enemy.Radius + CONTACT_RANGE_PAD, 2))
                        {
                            hit = true;
                            break;
                        }
                    }
                }

                if (hit)
                {
                    CombatResolver.DealDamage(ref enemy, 6, DamageCategory.Melee, ref unit);
                    unit.SkillState.SetFloat(SkillKeys.NagaContactCd, CONTACT_COOLDOWN);
                    break; // 共享冷却，一次只命中一个敌人
                }
            }
        }
    }
}
```

**渲染：**
```csharp
// NagaUnitView : UnitView
// 额外创建 segCount 个 SpriteRenderer 作为子物体
// 每帧从 SkillStateMap 读取体节位置更新
```

### 11b.3 骑乘/下马系统（国王蜘蛛 + 骷髅德鲁伊）

**设计：两个独立单位 + 位置绑定 + 死亡触发下马**

```csharp
// 在 UnitState 中新增字段：
public int RiderUnitId;    // 骑乘者单位 ID（-1 = 无骑手）
public int MountUnitId;     // 坐骑单位 ID（-1 = 无坐骑，下马后独立）

// 在 BattleSimulator 的单位循环中，骑乘逻辑：
void TickRiderBinding(ref UnitState mount, ref UnitState rider, float dt)
{
    if (mount.State == UnitState.Dead || rider.State == UnitState.Dead) return;

    // 骑手位置 = 坐骑位置 + 偏移（骑在背上）
    rider.X = mount.X;
    rider.Y = mount.Y + mount.Radius * 0.5f; // 略偏上方

    // 骑手有自己的目标选择和攻击（独立 AI）
    // 但不能独立移动（位置绑定到坐骑）
}

// 坐骑死亡时触发下马：
void OnMountDeath(ref UnitState mount, List<UnitState> units)
{
    var rider = units.Find(u => u.Id == mount.RiderUnitId);
    if (rider != null && rider.State != UnitState.Dead)
    {
        rider.MountUnitId = -1; // 解除绑定
        rider.MoveSpeed = rider.BaseMoveSpeed; // 恢复自身移速
        // 骑手在坐骑位置变为独立单位
    }
}
```

**购买逻辑：** 购买国王蜘蛛时，部署阶段生成两个部署点（蜘蛛 + 德鲁伊），战斗初始化时设置绑定关系。

### 11b.4 共享血量机制（轻语灵）

轻语灵的头部投射体与本体共享一个 HP 池。

**设计：头部投射体不是独立单位，而是轻语灵的技能延伸。**

```csharp
// 在 MurmurAbilityComponent 中管理
// 头部位置存储在 SkillStateMap：head_x, head_y, head_active
// 头部受击时，伤害转发到本体：

public override void OnTakeDamage(ref UnitState unit, float damage, DamageCategory category)
{
    if (unit.SkillState.GetInt(SkillKeys.MurmurHeadActive) == 1)
    {
        // 头部投射体活跃时，头部受伤 ×0.5
        damage *= 0.5f;
    }
    // 伤害直接扣本体的 HP（头部不是独立单位，无需转发）
    unit.Hp -= damage;
}
```

**关键点：** 头部不是单位列表中的独立单位，而是轻语灵技能产生的"攻击源"。敌方的攻击目标始终是轻语灵本体。头部只是视觉效果 + 攻击发射点。头部受到的伤害通过伤害修正器减半后直接扣本体 HP。

### 11b.5 新状态效果：蛰晕（Stun）

```csharp
// StatusEffectType 枚举新增：
public enum StatusEffectType
{
    Poison, Burn, Wither, Slow, Fear, Freeze, Stun
}

// Stun 效果行为：
// - 移速 = 0（与 Freeze 类似）
// - 移动类型强制变为 Ground（飞行单位被打落）
// - 攻击间隔不变（仍可攻击，与 Freeze 不同）
// - 持续 30 秒（远长于其他效果）
// - 可被驱散（未来扩展）

// 在 tickStatusEffects 中：
if (effect.Type == StatusEffectType.Stun)
{
    unit.MoveSpeed = 0;
    // 注意：不修改 AttackInterval（蛰晕单位仍可攻击）
    // 注意：移动类型变为 Ground 需要在效果施加时立即修改
}

// 在 applyStatusEffect 中：
if (type == StatusEffectType.Stun)
{
    unit.MoveType = MoveType.Ground; // 飞行单位被打落
}
// 效果结束时是否恢复飞行？需要记录原始 MoveType
// 建议：在 StatusEffectInstance 中添加 OriginalMoveType 字段
```

**蛰晕来源：** 沙漠蛛蜂对 `arthropod` 标签的敌人施加。

### 12.1 状态机

```csharp
public enum GamePhase
{
    Shop,     // 选购怪物
    Deploy,   // 部署到战场
    Battle,   // 战斗模拟
    Result    // 结算
}

public class GameManager : MonoBehaviour
{
    public GamePhase Phase { get; private set; }
    public int[] Gold { get; private set; }  // [team0, team1]
    public List<ShopUnit> ShopUnits { get; private set; }
    public List<DeployedUnit> DeployedUnits { get; private set; }
    public BattleSimulator Simulator { get; private set; }
    public int Winner { get; private set; }

    // 状态转换
    public void BuyMonster(string monsterId, int team, int count);
    public void StartDeploy();
    public void PlaceUnit(string monsterId, int team, Vector2 position);
    public void StartBattle();
    public void AutoDeployAndStart();
    public void ResetToShop();
}
```

### 12.2 商店阶段

- 双方各有 `INITIAL_GOLD = 1000` 金币
- 怪物列表按价格降序排列
- 支持单个购买和批量购买（最多 10 个）
- 购买后单位进入待部署池
- 双方各至少 1 个单位才能进入部署阶段

### 12.3 部署阶段

- 点击战场半场放置单位（左半=蓝方，右半=红方，中线 30px 间隔）
- 部署位置钳制在战场边界内
- 支持自动部署（随机分配位置）
- 所有单位部署完毕后开始战斗

### 12.4 战斗阶段

- `BattleSimulator` 每帧 `Tick(Time.deltaTime)`
- `BattleView` 读取 `BattleState` 渲染
- 某方全灭 → 进入结算

### 12.5 结算阶段

- 显示胜方
- 可选：显示战斗统计（每方剩余单位、总伤害等）
- "再来一局" → 重置到商店阶段

### 12.6 AI 商店算法

保留 Web 版的 AI 购买算法：
1. 32 次随机贪心尝试，寻找恰好花完金币的组合
2. 如果没有恰好花完 → 使用 DP（无界背包）花掉剩余金币
3. 兜底：价格降序贪心

---

## 13. 渲染与表现层

### 13.1 BattleView（渲染适配器）

```csharp
public class BattleView : MonoBehaviour
{
    private BattleSimulator _simulator;
    private Dictionary<int, UnitView> _unitViews = new();
    private ObjectPool<ProjectileView> _projectilePool;
    private ObjectPool<EffectView> _effectPool;

    void Update()
    {
        if (_simulator == null || _simulator.IsFinished) return;

        var state = _simulator.GetState();

        // 1. 同步单位
        SyncUnits(state.Units);

        // 2. 同步投射物
        SyncProjectiles(state.Projectiles);

        // 3. 同步区域效果
        SyncAreaEffects(state.AreaEffects, state.Shockwaves, state.ActiveBeams, ...);

        // 4. 更新摄像机（可选：跟随焦点）
    }

    void SyncUnits(List<UnitState> units)
    {
        // 创建新单位的 View
        // 更新现有单位的 position/rotation/sprite/HP bar
        // 移除死亡单位的 View（延迟销毁播放死亡动画）
    }
}
```

### 13.2 渲染顺序（与 Web 版一致）

```
1. 战场背景
2. 中线
3. 地面区域效果（熔岩/冰冻）
4. 冲击波
5. 下落物体（方尖碑）
6. 龙卷风
7. 锥形打击
8. 陨石
9. 光束
10. 投射物
11. 单位（按 Y 排序，画家算法）
12. 单位上层效果（巨蟹埋地堆、遗魂防御环等）
13. UI 覆盖层（HP 条、状态图标）
```

### 13.3 单位视觉组件

```csharp
public class UnitView : MonoBehaviour
{
    public SpriteRenderer SpriteRenderer;
    public HealthBarView HealthBar;
    public StatusEffectIcons StatusIcons;

    public void SyncFromState(ref UnitState state)
    {
        transform.position = new Vector3(state.X, state.Y, 0);
        SpriteRenderer.flipX = state.Facing < 0;

        // HP bar
        HealthBar.SetFill(state.Hp / state.MaxHp);
        HealthBar.SetTeamColor(state.Team);

        // 状态效果图标
        StatusIcons.Sync(state.StatusEffects);

        // 特殊状态视觉
        if (state.State == UnitState.Dead)
            PlayDeathAnimation();
    }
}
```

### 13.4 战场尺寸

- Web 版：1280 × 720 px
- Unity 版：保持相同逻辑尺寸，摄像机 Orthographic Size = 360
- 1 Unity unit = 1 px（或缩放为 1 unit = 8px，与 MC 方块一致）

---

## 14. 配置系统

### 14.1 运行时配置

```csharp
// Config/ConfigManager.cs
public class ConfigManager
{
    // 基础属性覆盖（对所有怪物生效）
    private Dictionary<string, BaseStatOverride> _baseOverrides;

    // 技能参数覆盖（仅对有技能模块的怪物）
    private Dictionary<string, SkillConfigOverride> _skillOverrides;

    // 从文件加载
    public void LoadFromJson(string path);
    public void SaveToJson(string path);

    // 应用到 MonsterDefSO（运行时修改 SO 实例）
    public void ApplyAll(MonsterDatabase database);

    // 导入/导出
    public string ExportJson();
    public bool ImportJson(string json);
}
```

### 14.2 配置字段定义

```csharp
public struct ConfigFieldDef
{
    public string key;
    public string label;
    public string unit;      // "HP", "px", "px/s", "s", "°", "%"
    public string hint;
    public float step;
    public float min;
}

public struct ConfigGroupDef
{
    public string title;
    public ConfigFieldDef[] fields;
}
```

### 14.3 配置面板（Editor 工具 + 运行时面板）

在 Unity Editor 中提供 Editor Window 用于调参：
- 左侧怪物列表（按价格降序）
- 右侧编辑区：基础属性 + 技能分组
- 导入/导出 JSON
- 重置默认值

---

## 15. AI 自动平衡系统

### 15.1 系统架构

```
┌─────────────────────────────────────────────────────────┐
│                   BalanceRunner (Editor 工具)              │
│                                                          │
│  ┌──────────────┐    ┌──────────────┐    ┌────────────┐ │
│  │ MatchRunner  │───▶│ StatsCollector│───▶│BalanceTuner│ │
│  │ 跑 N 场对战   │    │ 收集统计数据   │    │ 微调参数    │ │
│  │ (headless)   │    │              │    │ 写回 SO/JSON│ │
│  └──────────────┘    └──────────────┘    └────────────┘ │
│                                                  │       │
│                                                  ▼       │
│                                          ┌────────────┐ │
│                                          │  Report    │ │
│                                          │  可视化报告 │ │
│                                          └────────────┘ │
└─────────────────────────────────────────────────────────┘
```

### 15.2 批量模拟

```csharp
public class MatchRunner
{
    /// 跑一场对战，返回结果
    public MatchResult RunSingleMatch(List<DeployedUnit> team0, List<DeployedUnit> team1);

    /// 跑批量对战
    public BatchResult RunBatch(BatchConfig config)
    {
        var results = new List<MatchResult>();

        for (int i = 0; i < config.MatchCount; i++)
        {
            // 随机生成双方阵容
            var (team0, team1) = GenerateRandomDeployment(config.Gold);

            // 确定性模拟（固定种子）
            var sim = new BattleSimulator(config.Balance, config.Database);
            sim.Initialize(...);
            while (!sim.IsFinished) sim.Tick(BattleSimulator.TICK_DT);

            results.Add(ExtractStats(sim));
        }

        return new BatchResult(results);
    }
}
```

### 15.3 统计采集

```csharp
public struct MonsterStats
{
    public string MonsterId;
    public int Appearances;      // 出场次数
    public int Wins;             // 胜场
    public float WinRate;        // 胜率
    public float AvgSurvivalTime; // 平均存活时间
    public float AvgDamageDealt; // 平均总伤害
    public float AvgKills;       // 平均击杀数
    public float AvgHpRemaining; // 胜利时平均剩余血量百分比
}

public struct BatchResult
{
    public List<MatchResult> Matches;
    public Dictionary<string, MonsterStats> MonsterStats;
    public int TotalMatches;
    public float AvgMatchDuration;
}
```

### 15.4 自动调参

```csharp
public class BalanceTuner
{
    public void Tune(Dictionary<string, MonsterStats> stats, MonsterDatabase database)
    {
        foreach (var (monsterId, stat) in stats)
        {
            if (stat.Appearances < 20) continue;  // 样本不足

            var def = database.GetById(monsterId);

            if (stat.WinRate > 0.55f)
            {
                // 过强：降低 HP 和攻击力
                def.hp *= 0.98f;
                def.attack *= 0.97f;
                LogChange(monsterId, "nerf", stat.WinRate);
            }
            else if (stat.WinRate < 0.45f)
            {
                // 过弱：提升 HP 和攻击力
                def.hp *= 1.02f;
                def.attack *= 1.03f;
                LogChange(monsterId, "buff", stat.WinRate);
            }
        }
    }
}
```

### 15.5 迭代流程

```
1. 记录当前所有怪物参数为 baseline
2. 跑 1000 场随机对战（headless，约 5 分钟）
3. 统计每只怪物的胜率/存活/伤害
4. 自动微调参数（胜率>55% nerf，<45% buff）
5. 生成报告（CSV/JSON + 可视化）
6. 人工审核报告
7. 如果不满意 → 回到步骤 2（用新参数再跑）
8. 满意 → 导出参数为 JSON → 应用到 SO
```

### 15.6 报告内容

- 每只怪物的胜率条形图
- 胜率散点图（X=价格, Y=胜率）
- 异常值标记（胜率 >65% 或 <35%）
- 参数调整记录（改了什么，改了多少）
- 对战时长分布

---

## 16. 网络联机对战

### 16.1 架构：Server 权威

```
┌──────────┐         ┌──────────────────┐         ┌──────────┐
│ Player A │         │   Game Server     │         │ Player B │
│ (Client) │         │  (Authority)      │         │ (Client) │
└────┬─────┘         └────────┬─────────┘         └────┬─────┘
     │                        │                        │
     │   1. 匹配/加入房间       │                        │
     │───────────────────────▶│                        │
     │                        │◀───────────────────────│
     │                        │                        │
     │   2. 选怪 & 部署        │                        │
     │──── (RPC) ────────────▶│◀──────── (RPC) ────────│
     │                        │                        │
     │                        │  3. Server 跑模拟        │
     │                        │  (BattleSimulator)      │
     │                        │                        │
     │   4. 广播状态快照       │                        │
     │◀── (NetworkMessage) ───│                        │
     │                        │─── (NetworkMessage) ───▶│
     │                        │                        │
     │   5. 广播胜负           │                        │
     │◀──────────────────────│───────────────────────▶│
```

### 16.2 为什么 Server 权威

- 自动战斗不需要实时输入（没有 APm/延迟问题）
- 防作弊：客户端不能修改战斗结果
- 服务器跑纯逻辑层 `BattleSimulator`，低频广播即可
- 客户端只做"部署 + 观看"

### 16.3 网络流程详细

#### 阶段 1：大厅与匹配

```csharp
public class LobbyManager : NetworkBehaviour
{
    // 客户端调用 ServerRpc 加入
    [ServerRpc]
    void JoinLobbyServerRpc(ulong clientId) { ... }

    // 两人到齐 → 开始选怪
    [ClientRpc]
    void StartShopPhaseClientRpc() { ... }
}
```

#### 阶段 2：选怪与部署

```csharp
// 客户端发送自己的部署信息给 Server
[ServerRpc]
void SubmitDeploymentServerRpc(int[] monsterIds, Vector2[] positions)
{
    // Server 验证金币（防作弊）
    // 收到双方部署后初始化 BattleSimulator
}

// Server 通知开始战斗
[ClientRpc]
void StartBattleClientRpc(DeployedUnit[] team0, DeployedUnit[] team1) { ... }
```

#### 阶段 3：战斗广播

```csharp
// Server 每帧/每几帧广播状态
void Update()
{
    if (IsServer && _simulator != null && !_simulator.IsFinished)
    {
        _simulator.Tick(Time.deltaTime);
        _broadcastTimer += Time.deltaTime;

        // 每 50ms 广播一次（20Hz），客户端插值
        if (_broadcastTimer >= 0.05f)
        {
            _broadcastTimer = 0;
            BroadcastStateClientRpc(SerializeState(_simulator.GetState()));
        }
    }
}

[ClientRpc]
void BroadcastStateClientRpc(byte[] stateData)
{
    // 反序列化 → 更新本地渲染状态
    // 客户端插值平滑显示
}
```

#### 阶段 4：结算

```csharp
[ClientRpc]
void BattleResultClientRpc(int winner) { ... }
```

### 16.4 状态序列化

为了减少网络带宽，只广播增量数据：

```csharp
public class BattleStateSerializer
{
    // 完整快照（每秒 1 次）
    public byte[] SerializeFull(BattleState state);

    // 增量快照（每 50ms）
    // 只包含：位置变化、HP 变化、新死亡单位、新投射物
    public byte[] SerializeDelta(BattleState state, BattleState lastState);
}
```

### 16.5 客户端插值

```csharp
public class NetworkedBattleView : MonoBehaviour
{
    private Queue<BattleState> _stateBuffer = new();
    private float _renderDelay = 0.1f; // 100ms 延迟缓冲

    void Update()
    {
        float targetTime = Time.time - _renderDelay;
        // 在两个快照之间插值
        var from = FindStateBefore(targetTime);
        var to = FindStateAfter(targetTime);
        float t = (targetTime - from.Timestamp) / (to.Timestamp - from.Timestamp);
        InterpolateAndRender(from, to, t);
    }
}
```

### 16.6 服务器部署选项

| 方案 | 描述 | 适合 |
|---|---|---|
| **主机模式** | 一个客户端同时是 Server | 开发测试/好友对战 |
| **专用服务器** | 独立服务器进程 | 正式发布 |
| **中继服务器** | 使用 Unity Relay | 快速上线，无需自建服务器 |

推荐先用主机模式开发，再接入 Unity Relay 发布。

---

## 17. 测试策略

### 17.1 测试分层

```
┌─────────────────────────────────────────┐
│           集成测试 (Play Mode)            │
│  完整游戏流程：选怪→部署→战斗→结算          │
├─────────────────────────────────────────┤
│           模拟测试 (Edit Mode)           │
│  BattleSimulator 独立运行，验证战斗逻辑     │
├─────────────────────────────────────────┤
│           单元测试 (Edit Mode)           │
│  伤害公式、状态效果、目标选择、碰撞分离      │
└─────────────────────────────────────────┘
```

### 17.2 单元测试示例

```csharp
[Test]
public void DamageSystem_ArmorReduction()
{
    // 0 护甲 → 满伤害
    Assert.AreEqual(10f, DamageSystem.GetDamageAfterArmor(10, 0));

    // 20 护甲, 0 韧性 → g = max(4, 20-5) = 15 → dmg = 10*(1-15/25) = 4
    Assert.AreEqual(4f, DamageSystem.GetDamageAfterArmor(10, 20), 0.001f);

    // 20 护甲 → 最大减伤 80%
    Assert.AreEqual(2f, DamageSystem.GetDamageAfterArmor(10, 20, 0), 0.001f);
}

[Test]
public void Targeting_GroundMeleeCannotHitFlyer()
{
    var attacker = CreateUnit(AttackType.Melee, MoveType.Ground);
    var flyer = CreateUnit(AttackType.Melee, MoveType.Fly);

    Assert.IsFalse(TargetingSystem.CanTargetForAttack(attacker, flyer, false));

    // 飞行单位攻击时设置脆弱窗口
    flyer.VulnerableWindow = 0.55f;
    Assert.IsTrue(TargetingSystem.CanTargetForAttack(attacker, flyer, false));
}

[Test]
public void Simulation_Deterministic()
{
    var seed = 12345;
    var sim1 = CreateSimulator(seed);
    var sim2 = CreateSimulator(seed);

    for (int i = 0; i < 1000; i++)
    {
        sim1.Tick(TICK_DT);
        sim2.Tick(TICK_DT);
    }

    Assert.AreEqual(sim1.GetState().Units[0].X, sim2.GetState().Units[0].X);
}
```

### 17.3 模拟测试

```csharp
[Test]
public void Simulation_CreeperExplodes()
{
    // 创建苦力怕和敌方单位
    var deployments = new List<DeployedUnit> {
        new("creeper", 0, new(100, 360)),
        new("skeleton", 1, new(200, 360)),
    };

    var sim = new BattleSimulator(config, database);
    sim.Initialize(deployments);

    // 跑到苦力怕自爆
    while (!sim.IsFinished && sim.ElapsedTime < 30f)
        sim.Tick(TICK_DT);

    // 验证骷髅受到爆炸伤害
    var skeleton = sim.GetState().Units.Find(u => u.MonsterId == "skeleton");
    Assert.IsTrue(skeleton.Hp < skeleton.MaxHp || skeleton.State == UnitState.Dead);
}
```

### 17.4 平衡回归测试

```csharp
[Test]
public void Balance_NoMonsterWinRateAbove65()
{
    var runner = new MatchRunner(config, database);
    var result = runner.RunBatch(new BatchConfig { MatchCount = 500, Gold = 1000 });

    foreach (var stat in result.MonsterStats.Values)
    {
        if (stat.Appearances < 10) continue;
        Assert.Less(stat.WinRate, 0.65f,
            $"{stat.MonsterId} win rate {stat.WinRate} is too high");
    }
}
```

---

## 18. 目录结构

```
Assets/
├── Docs/
│   └── DesignDocument.md          ← 本文件
│
├── Scripts/
│   ├── Core/                       ← 核心枚举与常量
│   │   ├── Enums.cs
│   │   └── Constants.cs
│   │
├── Scripts/Simulation/             ← 纯逻辑层（不依赖 UnityEngine 渲染）
│   ├── BattleSimulator.cs
│   ├── BattleState.cs
│   ├── UnitState.cs
│   ├── SkillStateMap.cs
│   ├── TargetingSystem.cs
│   ├── MovementSystem.cs
│   ├── CombatResolver.cs
│   ├── DamageSystem.cs
│   ├── StatusEffectSystem.cs
│   ├── ProjectileSystem.cs
│   ├── AreaEffectSystem.cs
│   └── Abilities/
│       ├── IAbilityComponent.cs
│       ├── AbilitySystem.cs
│       ├── Bosses/
│       │   ├── TremorzillaAbility.cs
│       │   ├── LuxtructosaurusAbility.cs
│       │   ├── AncientRemnantAbility.cs
│       │   ├── HarbingerAbility.cs
│       │   ├── WardenAbility.cs
│       │   ├── WadjetAbility.cs
│       │   ├── KobolediatorAbility.cs
│       │   ├── EnderGolemAbility.cs
│       │   ├── WarpedMoscoAbility.cs
│       │   ├── AmethystCrabAbility.cs
│       │   ├── IgnitedRevenantAbility.cs
│       │   ├── CoralLeapAbility.cs
│       │   ├── ForsakenAbility.cs
│       │   ├── FrostmawAbility.cs
│       │   ├── AlphaYetiAbility.cs
│       │   ├── ProwlerAbility.cs
│       │   ├── NagaAbility.cs
│       │   ├── FarseerAbility.cs
│       │   ├── DeepOneMageAbility.cs
│       │   ├── DeeplingWarlockAbility.cs
│       │   ├── NucleeperAbility.cs
│       │   ├── DreadLichAbility.cs
│       │   └── CyclopsAbility.cs
│       └── Generic/
│           ├── MeleeAbility.cs
│           ├── AoeMeleeAbility.cs
│           ├── RangedAbility.cs
│           └── ExplosiveAbility.cs
│
├── Scripts/Data/                   ← 数据定义
│   ├── MonsterDefSO.cs
│   ├── AbilityDefSO.cs
│   ├── BalanceConfigSO.cs
│   ├── MonsterDatabase.cs
│   └── MonsterInference.cs         ← 从 Web 版数据自动生成 SO
│
├── Scripts/GameFlow/               ← 游戏流程
│   ├── GameManager.cs
│   ├── ShopSystem.cs
│   ├── DeploySystem.cs
│   └── BattleOrchestrator.cs
│
├── Scripts/View/                   ← 渲染表现层
│   ├── BattleView.cs
│   ├── UnitView.cs
│   ├── ProjectileView.cs
│   ├── EffectView.cs
│   ├── HealthBarView.cs
│   └── CameraController.cs
│
├── Scripts/UI/                      ← UI 面板
│   ├── ShopUI.cs
│   ├── DeployUI.cs
│   ├── BattleUI.cs
│   ├── ResultUI.cs
│   └── ConfigPanelUI.cs
│
├── Scripts/Network/                 ← 网络联机
│   ├── LobbyManager.cs
│   ├── NetworkBattleServer.cs
│   ├── NetworkedBattleView.cs
│   └── BattleStateSerializer.cs
│
├── Scripts/Balance/                 ← AI 自动平衡
│   ├── BalanceRunner.cs
│   ├── MatchRunner.cs
│   ├── StatsCollector.cs
│   ├── BalanceTuner.cs
│   └── BalanceReport.cs
│
├── Scripts/Editor/                  ← Editor 工具
│   ├── MonsterConfigWindow.cs       ← 数值配置面板
│   ├── BalanceRunnerWindow.cs       ← 平衡测试工具
│   └── MonsterDataImporter.cs       ← 从 JSON 导入怪物数据
│
├── Tests/
│   ├── EditMode/
│   │   ├── DamageSystemTests.cs
│   │   ├── TargetingSystemTests.cs
│   │   ├── StatusEffectTests.cs
│   │   ├── SimulationTests.cs
│   │   └── BalanceRegressionTests.cs
│   └── PlayMode/
│       ├── GameFlowTests.cs
│       └── NetworkTests.cs
│
├── ScriptableObjects/
│   ├── Monsters/                    ← 每个怪物一个 SO 文件
│   │   ├── Monster_Tremorzilla.asset
│   │   ├── Monster_Luxtructosaurus.asset
│   │   └── ...
│   ├── Abilities/                   ← 技能定义
│   │   ├── Ability_Beam.asset
│   │   └── ...
│   └── Balance/
│       └── BalanceConfig.asset      ← 全局平衡参数
│
├── Prefabs/
│   ├── Units/                       ← 单位预制体
│   ├── Projectiles/                 ← 投射物预制体
│   └── Effects/                     ← 特效预制体
│
├── Sprites/
│   ├── Monsters/                    ← 怪物精灵图
│   ├── UI/                          ← UI 图标
│   └── Effects/                     ← 特效贴图
│
└── Scenes/
    ├── MainMenu.unity
    ├── Battle.unity
    └── BalanceTest.unity             ← 平衡测试专用场景
```

---

## 19. 迁移计划

### 19.1 阶段划分

| 阶段 | 内容 | 预计工作量 | 产出 |
|---|---|---|---|
| **0. 设计文档** | 本文档 | ✅ | 设计文档 |
| **1. 核心框架** | 纯逻辑战斗引擎 + 数据定义 + 单元测试 | 2-3 周 | 可跑的 headless 战斗 |
| **2. 基础渲染** | BattleView + UnitView + 基础特效 | 1-2 周 | 可看的战斗 |
| **3. 普通怪物迁移** | ~50 个普通怪物数据 + 通用攻击模式 | 3-5 天 | 基础可玩 |
| **4. Boss 技能迁移** | 26 个 boss 的技能组件 | 4-6 周 | 完整战斗 |
| **5. UI 系统** | 商店/部署/战斗/结果 UI | 1-2 周 | 可发布的单机版 |
| **6. AI 平衡** | 批量模拟 + 自动调参 + 报告 | 1-2 周 | 平衡工具 |
| **7. 网络联机** | 匹配 + 同步 + 大厅 | 2-3 周 | 可联机对战 |
| **8. 打磨发布** | 音效/特效/动画/性能优化/打包 | 2-4 周 | 可发布游戏 |

### 19.2 阶段 1 详细（核心框架）

1. **枚举与常量** (`Core/Enums.cs`, `Core/Constants.cs`)
   - MoveType, AttackType, UnitState, DamageCategory, StatusEffectType
   - TICK_DT, SEPARATION_FORCE, STICKY_RANGE_BONUS, BATTLE_FIELD 尺寸等

2. **数据定义** (`Data/MonsterDefSO.cs`)
   - 从 Web 版 `怪物图鉴.md` 导入所有怪物数据为 SO
   - 编写 `MonsterDataImporter` Editor 工具自动生成 SO

3. **伤害系统** (`Simulation/DamageSystem.cs`)
   - 迁移 `getDamageAfterArmor` 公式
   - 实现 `dealDamageToUnit` 管道（无敌/格挡/护甲/扣血/死亡）

4. **状态效果** (`Simulation/StatusEffectSystem.cs`)
   - 6 种效果 + DoT 结算 + 燃烧传播 + 减速/冰冻属性修正

5. **目标选择** (`Simulation/TargetingSystem.cs`)
   - pickTarget + sticky + retarget + 对空规则 + anti_arthropod

6. **移动与碰撞** (`Simulation/MovementSystem.cs`)
   - chase + separate + clamp + drift + leap arc

7. **战斗模拟器** (`Simulation/BattleSimulator.cs`)
   - 完整的 Tick 循环 + 胜负判定
   - 确定性随机数

8. **单元测试** (`Tests/EditMode/`)
   - 伤害公式、目标选择、碰撞分离、状态效果、确定性验证

### 19.3 数据迁移策略

从 Web 版 `怪物图鉴.md` 表格 + `infer.ts` 规则自动生成 Unity SO：

```csharp
// Editor/MonsterDataImporter.cs
public class MonsterDataImporter : EditorWindow
{
    [MenuItem("MC Fight/Import Monster Data")]
    static void Import()
    {
        // 1. 读取 怪物图鉴.md
        // 2. 解析表格行
        // 3. 对每行运行 infer 逻辑（inferMoveType, inferAttackType, ...）
        // 4. 生成 MonsterDefSO.asset 文件
        // 5. 保存到 Assets/ScriptableObjects/Monsters/
    }
}
```

### 19.4 当前 Unity 项目状态

已有基础框架（需要重构）：

| 文件 | 状态 | 需要的改动 |
|---|---|---|
| `Enums.cs` | ✅ 基本可用 | 添加 StatusEffectType（含 Stun）、RiderUnitId/MountUnitId 字段 |
| `MonsterDefSO.cs` | ✅ 基本可用 | 添加 armorToughness, onHitEffects, abilities |
| `UnitBase.cs` | ⚠️ 需重构 | 拆分为 UnitState(逻辑) + UnitView(渲染) |
| `BattleManager.cs` | ⚠️ 需重构 | 提取为 BattleSimulator(逻辑) + BattleView(渲染) |
| `DamageSystem.cs` | ✅ 可用 | 添加 dealDamageToUnit 管道 |
| `StatusEffectSystem.cs` | ⚠️ 需重构 | 改为值类型，避免 GC |
| `SkillConfigSO.cs` | ⚠️ 需重构 | 改为 AbilityDefSO + IAbilityComponent |
| `ISkillExecutor.cs` | ⚠️ 需重构 | 改为 IAbilityComponent（纯逻辑，不依赖 MonoBehaviour） |
| `UnitFactory.cs` | ⚠️ 需重构 | 拆分逻辑工厂 + 渲染工厂 |
| `Projectile.cs` | ⚠️ 需重构 | 改为 ProjectileData(逻辑) + ProjectileView(渲染) |

### 19.5 重构优先级

1. **第一步**：建立纯逻辑层 `BattleSimulator`，与现有 MonoBehaviour 代码并存
2. **第二步**：让 `BattleView` 从 `BattleSimulator.GetState()` 读取状态驱动渲染
3. **第三步**：逐步将 boss 技能从 if-else 迁移到 `IAbilityComponent`
4. **第四步**：移除旧的 `BattleManager.TickUnit` 逻辑，全部走 `BattleSimulator`

---

## 附录 A：Web 版关键常量汇总

| 常量 | 值 | 用途 |
|---|---|---|
| `TICK_DT` | 1/30 (Web) → 1/60 (Unity) | 逻辑帧间隔 |
| `STICKY_RANGE_BONUS` | 30 | 目标保持额外距离 |
| `SEPARATION_FORCE` | 180 | 碰撞分离力 |
| `TARGET_RETARGET_INTERVAL` | 2.5s | 强制重选目标间隔 |
| `FLY_MELEE_VULN_WINDOW` | 0.55s | 飞行近战脆弱窗口 |
| `BURN_SPREAD_RADIUS` | 52px | 燃烧传播半径 |
| `BATTLE_FIELD` | 1280×720, midX=640 | 战场尺寸 |
| `INITIAL_GOLD` | 1000 | 初始金币 |
| `BULK_BUY_COUNT` | 10 | 批量购买上限 |
| Projectile speed | 280 px/s | 投射物速度 |
| Projectile maxTravel | range × 1.15 | 投射物最大飞行距离 |
| Melee animTimer | 0.25s | 近战动画时间 |
| AOE animTimer | 0.4s | AOE 动画时间 |
| Default AOE radius | 64px | 默认范围攻击半径 |
| Default explosion radius | 90px | 默认爆炸半径 |
| Facing dead-zone | 4px | 朝向切换死区 |
| Drift speedMul | 0.72 | 游走速度倍率 |
| Feared speedMul | 0.95 | 恐惧游走速度 |
| Enemy separation mult | 2.5× | 敌对推力倍数 |
| anti_arthropod bias | 0.75× | 对飞行距离评分 |

## 附录 B：Web 版状态效果配置

| 效果 | DPS | 持续(s) | 速度倍率 | 特殊 |
|---|---|---|---|---|
| poison | 2 | 5 | 1.0 | - |
| burn | 1 | 10 | 1.0 | 范围传播 52px |
| wither | 3 | 4 | 1.0 | - |
| slow | 0 | 5 | 0.7 | 攻击间隔 ×1/0.7 |
| fear | 0 | 2 | - | 原地游走 ×0.95 |
| freeze | 0 | 2 | 0.0 | 攻击间隔=∞ |

## 附录 C：怪物体型尺寸

| 类别 | 显示尺寸(px) | 碰撞半径(px) | 例子 |
|---|---|---|---|
| Giant | 112 | 56 | 撼地斯拉、暝煌龙、远古遗魂 |
| Boss | 56 | 28 | 其他所有 boss |
| Fly | 40 | 16 | 飞行单位 |
| Normal | 40 | 18 | 普通地面单位 |
