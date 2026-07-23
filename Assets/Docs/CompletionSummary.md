# MC Fight Arena — 当前完成度总结

> 日期: 2026-07-16
> 状态: 核心功能完整，可玩

---

## 一、整体完成度

| 模块 | 完成度 | 说明 |
|---|---|---|
| 战斗模拟引擎 | 100% | 纯逻辑层，60Hz 固定步长，确定性模拟 |
| 怪物数据 | 100% | 84 个 ScriptableObject，78 个可购买 + 6 个召唤物 |
| 怪物精灵图 | 100% | 82 个已有贴图，2 个（vex/stradpole）用桌面图片 |
| 通用攻击模式 | 100% | Melee / AoeMelee / Ranged / Explosive |
| 特殊技能 | 100% | 48 个怪物有独立技能组件（42 个能力类） |
| UI 系统 | 90% | 主菜单/商店/部署/战斗/结算/图鉴 完整流程 |
| 战斗特效 | 60% | 投射物/光束/区域效果/伤害数字 有基础实现 |
| 联机对战 | 0% | 尚未开始 |
| AI 平衡 | 0% | 尚未开始 |

---

## 二、架构总览

```
GameManager (状态机)
  MainMenu → Shop → Deploy → Battle → Result
                 ↑ 图鉴独立入口

BattleBridge (渲染桥接)
  Update() → BattleSimulator.Tick() → SyncViews()
    ├── 单位渲染 (UnitView)
    ├── 投射物渲染 (ProjectileView)
    ├── 区域效果渲染 (EffectView)
    ├── 光束渲染 (BeamView)
    └── 伤害数字 (DamageNumberView)

BattleSimulator (纯逻辑)
  Tick(dt):
    Phase A: 全局效果更新 (AreaEffectSystem / ProjectileSystem)
    Phase B: 单位循环
      ├── StatusEffectSystem.Tick()  // DoT 结算
      ├── 冷却递减
      ├── Ability.IsBusy? → TickCast()
      ├── 恐惧 → 随机游走
      ├── TargetingSystem.PickTarget()
      ├── Ability.TryExecute()  // 技能释放
      ├── 标准战斗逻辑 (B.9)
      │     └── inRange + cooldown=0 → 攻击
      │     └── inRange + cooldown>0 → 待机
      │     └── outOfRange → 追击
      └── ClampToField
    Phase C: 碰撞分离
    Phase D: 胜负判定 (+ 120s 超时按 HP 比例判胜)
```

---

## 三、核心系统实现逻辑

### 3.1 伤害系统 (DamageSystem)

```
dealDamageToUnit(target, rawDamage, category):
  1. 无敌检查: 巨蟹埋地 → 返回 0
  2. 防御修正: 遗魂防御 → 近战×0.1, 非近战返回 0
  3. 格挡修正: 骸骨斩首者 → 远程×0.5
  4. 免疫修正: 食人妖 → Ranged/Beam 返回 0
  5. 护甲减伤: g = clamp(armor - 4*dmg/(toughness+8), armor/5, 20)
               final = dmg * (1 - g/25)
  6. 扣血 + 死亡检查
  7. 触发 DamageEvents.OnDamage（统计 + 伤害数字）
```

### 3.2 状态效果系统 (StatusEffectSystem)

| 效果 | DPS | 持续 | 速度倍率 | 攻击间隔 | 特殊 |
|---|---|---|---|---|---|
| Poison | 2/s | 5s | 1.0 | 1.0 | - |
| Burn | 1/s | 10s | 1.0 | 1.0 | 52px 范围传播 |
| Wither | 3/s | 4s | 1.0 | 1.0 | - |
| Slow | 0 | 5s | 0.7 | ×1/0.7 | - |
| Fear | 0 | 2s | 0.95 | 1.0 | 随机游走 |
| Freeze | 0 | 2s | 0 | ∞ | 不能攻击 |
| Stun | 0 | 30s | 0 | 1.0 | 变为地面，仍可攻击 |

DoT 每 1 秒结算一次，无视护甲，尊重无敌。

### 3.3 目标选择 (TargetingSystem)

```
pickTarget(unit):
  1. 获取所有存活敌方
  2. 计算交战半径 (技能组件或 attackRange)
  3. 对空规则:
     - 飞行单位所有攻击可对空
     - 地面远程可对空
     - 地面近战只能对地（除非飞行单位有脆弱窗口）
  4. Sticky 目标保持 (距离 < range + 30)
  5. pickNearestTarget (anti_arthropod 对飞行距离×0.75)
  6. 每 2.5s 强制重选
```

### 3.4 碰撞与移动 (MovementSystem)

```
separateAllUnits(units):
  对所有单位对:
    overlap = (minDist - d) / minDist
    盟友推力 ×1, 敌对推力 ×2.5
    位移 = overlap × SEPARATION_FORCE(180) × dt

ChaseTowardTarget:
  归一化方向 × moveSpeed × dt

待机: 随机游走 (速度×0.72), 碰壁反弹
跃击: 抛物线插值 ease = t×(2-t), hop = sin(t×π)×arcHeight
```

### 3.5 冷却机制 (关键修复)

所有技能冷却在 `TryExecute()` 中递减（每帧调用），而非 `TickCast()`（仅在施法中调用）。

```
TryExecute 每帧:
  if (cd > 0) cd -= dt;     // 冷却递减（无论是否施法）
  if (busy) return true;     // 施法中，跳过
  if (cd <= 0 && inRange)   // 冷却完毕且在范围内
    释放技能; cd = cooldown;
```

---

## 四、怪物技能系统

### 4.1 技能组件接口

```csharp
interface IAbilityComponent {
    void OnInit(ref UnitState);
    bool TryExecute(ref UnitState, int targetIdx, float dist, BattleState, float dt);
    void TickCast(ref UnitState, BattleState, float dt);
    float GetEngageRange(ref UnitState);
    bool IsBusy(ref UnitState);
    bool AllowAntiAir(ref UnitState);
}
```

### 4.2 技能分类

| 类别 | 数量 | 文件 | 说明 |
|---|---|---|---|
| 通用 | 4 | GenericAbilities.cs | Melee / AoeMelee / Ranged / Explosive |
| Batch1 | 9 | Batch1Abilities.cs | 升级怪物（远近双模式/蓄力/交替/锥形等） |
| Batch2 | 10 | Batch2Abilities.cs | 中等复杂度（女巫/祭司/蛛蜂/烈焰人等） |
| Batch3 | 23 | Batch3Abilities.cs | 全部 Boss（光束/弹幕/跃击/石阵等） |

### 4.3 全部 48 个特殊技能怪物

**Boss 级 (26 个):**

| 怪物 | 能力类 | 核心机制 |
|---|---|---|
| 撼地斯拉 | TremorzillaAbility | 超能射线(5s持续光束, 15tick×20=∠300) + AOE 践踏(92px/30伤害) |
| 暝煌龙 | LuxtructosaurusAbility | 跃击/甩尾/践踏交替 + 陨石被动(3s) + 熔岩区域(30s/5DPS) |
| 远古遗魂 | RemnantAbility | 5技能随机(咬/甩尾/沙暴/践踏/石碑弹幕7环) + %maxHp 伤害 |
| 先驱者 | HarbingerAbility | 模式切换(凋零导弹/激光) + 4技能循环(追踪弹/手雷/冲撞/死亡射线) + 回血 |
| 监守者 | WardenAbility | 近战30 + 远程声波(10伤害/冷却10s) |
| 骸骨斩首者 | KobolediatorAbility | 冲锋/三连斩/践踏 + 远程格挡50% + 无法对空 |
| 擎天龙 | AoeMeleeAbility | AOE 践踏(56px/8伤害) |
| 撼地龙 | TremorsaurusAbility | 恐吓怒吼(对所有非boss施加恐惧) + 极速近战(0.7s) |
| 末影傀儡 | EnderGolemAbility | 拳击/猛击/虚空符文(十字光束)随机 + 1s定身 |
| 诡异蚊鬼 | WarpedMoscoAbility | 地面阶段3技能 + HP<25%变身飞行远程 |
| 紫水晶巨蟹 | AmethystCrabAbility | 缩地(5s无敌)→破土(2s AOE)→横扫(3s AOE)循环 |
| 炽燃遗魂 | RevenantAbility | 旋转/吐息/骨弹随机 + 防御姿态(近战×0.1, 非近战免疫) |
| 珊瑚巨兽 | CoralLeapAbility | 跃击(1.6s/28px/11.5伤害) |
| 瓦吉特 | WadjetAbility | 横扫(2hit)/龙卷(穿透)交替 + 石碑弹幕(5环/冷却15s) |
| 独眼巨人 | CyclopsAbility | 吞噬(HP≤50秒杀) + 重击(AOE 17伤害) |
| 霜冻巨兽 | FrostmawAbility | 冰球/喷雾/爪击/猛砸(40伤害!) 随机 |
| 雪怪首领 | AlphaYetiAbility | 冰炸弹(爆炸+冰冻区域) + 狂暴(3tick×5) |
| 徘徊者 | ProwlerAbility | 横扫/切割/导弹/死亡射线(5+5%maxHp) 4技能循环 |
| 瞻远者 | FarseerAbility | 衰败射线(10%maxHp) + 爪击(6伤害) 模式切换 |
| 娜迦 | NagaAbility | 蛇形移动(3-8节) + 接触伤害(共享0.5s冷却) + 速度随HP变化 |
| 深潜者法师 | DeepOneMageAbility | 水弹/水波(击退80px)/甩动(3tick) 随机 |
| 核能苦力怕 | NucleeperAbility | 10s引信自爆(中心500→边缘100, 不分敌我) |
| 渊灵术士 | WarlockAbility | 标记→延迟→激光雨(7tick×14=98) 四阶段 |
| 悚怖尸巫 | DreadLichAbility | 远程(对空×2) + 召唤随从(10s) + 击杀转化 |
| 遗弃者 | ForsakenAbility | 咬/锤/声波/弧形声波 + 跃击 + 1HP/s回血 |
| 珊瑚傀儡 | CoralLeapAbility | 跃击(1.5s/20px/12.5伤害) |

**升级怪物 (22 个):**

| 怪物 | 能力类 | 核心机制 |
|---|---|---|
| 大象 | ChargeMeleeAbility | 3s蓄力冲锋(25伤害) + 20%概率长牙 |
| 渊灵蛮兵 | DualModeAbility | 远近双模式(>100远程10, ≤100近战14) |
| 炽燃狂魂 | BerserkerAbility | 挥砍(14×2)/旋转(AOE 11×2) 3s交替 |
| 食人妖 | TrollAbility | 重击(15s冷却27伤害) + 免疫击退+免疫远程 |
| 唤魔者 | EvokerAbility | 尖牙(近距AOE/远距直线) + 召唤恼鬼(2只) |
| 米诺菇 | ChargeMeleeAbility | 3s蓄力冲锋(23伤害) |
| 磁控机兵 | MagnetronAbility | 伤害=2+周围敌军数 + 20px击退 |
| 深潜者骑士 | DualModeAbility | 远近双模式 |
| 渊灵祭司 | PriestAbility | 3s定身施法(96px AOE 8/tick) + 3s冷却 |
| 洞穴蜈蚣 | MeleeAbility | 近战附带中毒 |
| 渊灵 | DualModeAbility | 远近双模式 |
| 轻语灵 | MurmurAbility | 头部投射体(独立攻击, 受伤×0.5, 共享血量) |
| 观测者 | RangedAbility | 远程附带燃烧 |
| 跨座兽 | StraddlerAbility | 远程投射 + 命中生成蝌蚪 + 10px击退 |
| 鸡蛇 | RangedAbility | 极速远程(0.4s) + 附带凋零 |
| 链锤哥布林 | GoblinAbility | 自身AOE(48px/8伤害) + 10px击退 |
| 女巫 | WitchAbility | 4药水随机(伤害/剧毒/迟缓/治疗) + 受伤无敌人优先治疗 |
| 寒冬狼 | ConeBreathAbility | 锥形冰雾(4tick×4=16) + 减速 |
| 沙漠蛛蜂 | TarantulaHawkAbility | 蛰晕节肢(30s无法移动+变地面) |
| 铜羽泽鹗 | StymphalianAbility | 双发×1 + 无视护甲 + 移速90 |
| 国王蜘蛛 | SpiderRiderAbility | 骑乘系统(蜘蛛近战+德鲁伊远程独立目标) + 下马机制 |
| 舐脑魔 | BrainiacAbility | 远近切换 + 废料桶(HP<30触发: 爆炸+污染区域30s) |

**其他特殊 (4 个):**

| 怪物 | 能力类 | 机制 |
|---|---|---|
| 烈焰人 | BlazeAbility | 5s三连发(3×5+燃烧) + 无限射程 + 随机偏移 |
| 娜迦(飞行) | FlyNagaAbility | 剧毒射击/俯冲交替 + 圆周盘旋 |
| 恼鬼 | VexAbility | 漫飞+近战(13伤害/5s冷却) |
| 喷火甲虫 | ConeBreathAbility | 锥形火焰(4tick×4=16) + 燃烧 |

---

## 五、游戏流程

### 5.1 状态机

```
MainMenu
  ├── 单人对战 → Shop (双方手动购买)
  ├── AI 对战 → Shop (玩家买蓝方, AI自动买红方)
  └── 怪物图鉴 → Codex → 返回主菜单

Shop → Deploy → Battle → Result
  ↑                          ↑
  └── 再来一局 ──────────────┘
  └── 返回主菜单 ───→ MainMenu
```

### 5.2 商店阶段
- 双方各 1000G 金币
- 怪物按价格降序排列（78 个可购买怪物）
- 支持单买(+1)和批量买(最大10个)
- 队伍切换按钮(蓝方/红方)
- 自动部署并开战按钮

### 5.3 部署阶段
- 点击战场半场放置单位（左半=蓝方, 右半=红方）
- 部署位置钳制在战场边界内
- 自动部署按钮（随机分配位置）
- PvAI 模式：AI 自动购买和部署红方

### 5.4 战斗阶段
- BattleSimulator 驱动，60Hz 固定步长
- 顶部 UI：蓝方存活数 vs 红方存活数 + 计时器
- 视觉特效：投射物、光束、区域效果、伤害数字
- 战斗结束自动进入结算

### 5.5 结算阶段
- 显示胜方（蓝方胜利/红方胜利/同归于尽）
- 战斗统计：每单位存活状态、造成伤害(近战/远程/光束/爆炸/DoT)、承受伤害、击杀数、施加buff次数、承受debuff次数
- 再来一局 / 返回主菜单

### 5.6 怪物图鉴
- 左侧：怪物列表（按价格降序，可滚动）
- 右侧：详情面板（图标+名称+价格+属性+标签+技能描述）
- 属性含护甲减伤示例
- 返回主菜单按钮

---

## 六、战斗特效系统

### 6.1 投射物 (ProjectileView)
- 8 种类型：Default / HarbWither / HarbHoming / HarbLaser / RevenantBone / ForsakenSonic / IceBomb / ProwlerMissile
- 每种不同颜色和拖尾效果
- 队伍颜色区分（蓝/红）
- 追踪弹有 TrailRenderer 拖尾

### 6.2 区域效果 (EffectView)
- 冲击波：扩散环动画 + 淡出
- 熔岩/冰冻/污染区域：径向渐变圆形 + 脉冲呼吸效果
- 龙卷风：旋转动画
- 最后 2 秒淡出

### 6.3 光束 (BeamView)
- 三层渲染：核心 + 外发光
- 3 种类型：Tremor(橙) / HarbingerDeath(红) / ProwlerRay(紫)
- 光束长度和方向每帧更新
- 最后 0.5s 淡出

### 6.4 伤害数字 (DamageNumberView)
- 向上飘移 + 淡出
- 按伤害类型着色：Melee白/Ranged黄/Beam橙/Explosion红/True紫
- 高伤害(≥50)放大字体

---

## 七、已知问题与待修复

| 问题 | 优先级 | 状态 |
|---|---|---|
| 商店滚动速度 | 已修复 | ScrollRect.scrollSensitivity=30 + 鼠标滚轮加速 |
| 战斗结束重新开始 | 已修复 | BattleBridge.StopBattle() 清理所有视图 |
| 冷却机制 | 已修复 | 所有冷却从 TickCast 移到 TryExecute |
| 巨型单位近战打不到 | 已修复 | Mathf.Max(attackRange, radius+target.radius) |
| 距离检查无半径补偿 | 已修复 | 所有 dist > X 改为 dist > X + target.Radius × 0.5 |
| 激光无可见光束 | 已修复 | TremorzillaAbility 创建 ActiveBeamData |
| 能力类型未注册 | 已修复 | MonsterDataGenerator 添加 abilityComponentType |
| 图鉴/结算文字溢出 | 已修复 | horizontalOverflow=Wrap, fontSize 增大 |
| 图鉴/结算文字遮挡 | 已修复 | 列表 25% 宽度, 详情面板 27% 起始 |
| 商店 UI 不隐藏 | 已修复 | StartBattle() 中添加 shopUI.Hide() |
| 批量文件编码损坏 | 已修复 | 用 write_file 重写（纯 ASCII 无中文注释） |
| 效果区域视觉单调 | 待优化 | 计划用 AI 生成特殊素材 |

---

## 八、文件结构

```
Assets/Scripts/
├── Core/
│   ├── Enums.cs              // 所有枚举类型
│   ├── Constants.cs           // 全局常量
│   └── MonsterDefSO.cs        // 怪物数据定义
├── Simulation/
│   ├── BattleSimulator.cs     // 核心模拟器
│   ├── BattleState.cs         // 运行时状态（UnitState/BattleState/UnitList）
│   ├── SkillStateMap.cs       // 技能状态 KV 存储
│   ├── DamageSystem.cs        // 伤害系统 + 事件总线
│   ├── StatusEffectSystem.cs  // 状态效果系统
│   ├── TargetingSystem.cs     // 目标选择系统
│   ├── MovementSystem.cs      // 移动与碰撞系统
│   ├── ProjectileSystem.cs    // 投射物系统
│   ├── AreaEffectSystem.cs    // 区域效果系统
│   ├── BattleStatsCollector.cs// 战斗统计收集器
│   └── Abilities/
│       ├── IAbilityComponent.cs    // 技能组件接口
│       ├── AbilityFactory.cs       // 技能工厂
│       ├── GenericAbilities.cs     // 通用技能（Melee/AoeMelee/Ranged/Explosive）
│       ├── Batch1Abilities.cs      // 9 个升级怪物技能
│       ├── Batch2Abilities.cs      // 10 个中等复杂度技能
│       └── Batch3Abilities.cs      // 23 个 Boss 技能
├── GameFlow/
│   └── GameManager.cs         // 游戏状态机
├── View/
│   ├── BattleBridge.cs        // 战斗渲染桥接器
│   ├── UnitView.cs            // 单位渲染
│   ├── BattleEffectViews.cs   // 特效渲染（投射物/区域/光束/伤害数字）
│   └── BattleFieldRenderer.cs // 战场背景渲染
├── UI/
│   ├── MainMenuUI.cs          // 主菜单
│   ├── ShopUI.cs              // 商店界面
│   ├── DeployUI.cs            // 部署界面
│   ├── BattleUI.cs            // 战斗界面
│   ├── ResultUI.cs            // 结算界面
│   └── CodexUI.cs             // 怪物图鉴
├── Data/
│   └── MonsterDatabase.cs     // 怪物数据库
├── Editor/
│   └── MonsterDataGenerator.cs// 怪物数据生成器
└── Docs/
    ├── DesignDocument.md       // 架构设计文档
    ├── MonsterDesign.md        // 怪物详细设计
    └── OptimizationPlan.md     // 优化计划
```