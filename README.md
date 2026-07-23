# MC Fight — Unity 迁移指南

## 已完成的工作

### 已创建的脚本文件

| 文件 | 作用 |
|---|---|
| `Scripts/Core/Enums.cs` | 枚举类型（MoveType, AttackType, UnitState, DamageCategory） |
| `Scripts/Core/MonsterDefSO.cs` | 怪物数据定义（ScriptableObject） |
| `Scripts/Core/SkillConfigSO.cs` | 技能配置基类 |
| `Scripts/Core/StatusEffectSystem.cs` | 状态效果系统（中毒/燃烧/凋零/减速/冰冻） |
| `Scripts/Core/DamageSystem.cs` | MC 风格护甲减伤公式 |
| `Scripts/Core/UnitBase.cs` | 单位战斗组件（挂到每个怪物 Prefab 上） |
| `Scripts/Core/BattleManager.cs` | 战斗管理器（驱动所有单位的 AI 和战斗循环） |
| `Scripts/Core/ISkillExecutor.cs` | 技能执行接口 |
| `Scripts/Core/Projectile.cs` | 投射物行为 |
| `Scripts/Core/UnitFactory.cs` | 单位工厂（从 MonsterDefSO 创建单位实例） |
| `Scripts/Core/TestSpawner.cs` | 测试生成器（快速生成两队怪物战斗） |
| `Scripts/Skills/CreeperSkill.cs` | 苦力怕技能示例（自爆） |
| `Scripts/Skills/NagaSkill.cs` | 娜迦技能示例（接触伤害+穿梭） |

### 已复制的资源

- 所有怪物精灵图已复制到 `Assets/Sprites/Monsters/` 目录

## 你需要在 Unity 中做的操作

### 第一步：让 Unity 编译脚本

1. 打开 Unity Hub → 打开你的项目
2. 等待 Unity 编译所有 C# 脚本（右下角会显示进度）
3. 如果编译有错误，根据错误提示修复

### 第二步：创建场景和基础设置

1. 在 `Scenes` 文件夹右键 → 创建场景，命名为 `BattleScene`
2. 双击打开这个场景
3. 在 Hierarchy 中右键 → Create Empty，命名为 `BattleManager`
4. 选中 `BattleManager`，在 Inspector 中点击 Add Component → 搜索 `BattleManager` 并添加

### 第三步：创建第一个怪物配置（ScriptableObject）

1. 在 `ScriptableObjects` 文件夹中右键 → Create → MC Fight → Monster Definition
2. 命名为 `Monster_Creeper`
3. 在 Inspector 中填写：
   - monsterId: `creeper`
   - displayName: `苦力怕`
   - price: `20`
   - hp: `20`, attack: `49`, armor: `0`
   - moveSpeed: `42`, attackRange: `42`, attackInterval: `99`
   - radius: `14`
   - tags: 点击 + 添加 `explosive`
   - idleSprite: 从 `Sprites/Monsters/creeper/` 拖入 idle.png

### 第四步：创建测试 Prefab

1. 在 `Prefabs` 文件夹右键 → Create → Sprite → 命名为 `Unit_Creeper`
2. 拖入一个精灵图作为占位
3. 添加组件：`CircleCollider2D`（isTrigger=true）、`Rigidbody2D`（gravityScale=0）、`UnitBase`、`CreeperSkill`
4. 把 `UnitBase` 的 def 字段拖入刚才创建的 `Monster_Creeper` 资产

### 第五步：创建第二个怪物（对面试方）

1. 重复第三步和第四步，创建一个简单的测试怪物（比如骷髅）
2. 或者复制一份苦力怕的配置，放到对面队伍

### 第六步：测试战斗

1. 在 Hierarchy 中创建一个空 GameObject，命名为 `TestSpawner`
2. 添加 `TestSpawner` 组件
3. 在 Inspector 中：
   - Team 0 Monsters: 数组大小设为 1，拖入 `Monster_Creeper`
   - Team 1 Monsters: 数组大小设为 1，拖入对面怪物的配置
4. 点击 Play 按钮 ▶️

### 第七步：验证结果

- 两个单位应该会互相靠近
- 进入范围后攻击
- 如果一方死亡，控制台会输出 "Team X wins!"
- 可以调整 `MonsterDefSO` 中的数值，改完立刻生效

## 后续扩展方向

### 添加更多怪物

1. 创建 `MonsterDefSO` 资产（填数值）
2. 创建对应的技能脚本，实现 `ISkillExecutor` 接口
3. 在 `UnitFactory.AddSkillComponent()` 中注册
4. 创建 Prefab，挂上组件

### 添加 UI（商店/部署）

1. 用 Unity 的 uGUI 或 UI Toolkit 创建界面
2. 读取 `MonsterDefSO` 列表显示在商店中
3. 点击部署时调用 `UnitFactory.CreateUnit()` 生成单位

### 配置编辑器工具

我写的 `MonsterDefSO` 使用了 `[CreateAssetMenu]`，你可以在 Unity 中右键快速创建。后续可以写一个 `MonsterConfigWindow` 编辑器窗口，像现在的 Web 页面一样批量调整数值。

## 注意事项

- 当前碰撞检测用的是 `Physics2D.OverlapCircle`，需要确保单位有 `Collider2D`
- 单位的 `Rigidbody2D` 的 `gravityScale` 必须为 0（俯视图，没有重力）
- 所有精灵图是 `Sprite Mode: Multiple` 的话需要在 Sprite Editor 中切分