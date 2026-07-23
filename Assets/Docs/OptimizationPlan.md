# MC Fight Arena — 优化与扩展计划

> **版本**: 2.0  
> **日期**: 2026-07-15  
> **状态**: 规划中

---

## 当前完成情况

| Phase | 状态 | 内容 |
|---|---|---|
| Phase 1 | ✅ 完成 | 纯逻辑战斗引擎 + 数据定义 |
| Phase 2 | ✅ 完成 | 基础渲染（BattleView/UnitView/BattleFieldRenderer） |
| Phase 3 | ✅ 完成 | 84 个怪物 ScriptableObject + 精灵图导入 |
| Phase 4 | ✅ 完成 | 全部 56 个技能组件实现 |
| Phase 5 | ✅ 完成 | UI 系统（商店/部署/战斗/结算） |

**当前可玩**：商店购买 → 部署 → 自动战斗 → 胜负判定 → 重新开始

---

## 优化计划

### 优先级 P0（影响基本可玩性）

#### 1. 战斗视觉特效系统
#### 2. 战斗结束重新开始功能
#### 3. 商店滚动速度修复

### 优先级 P1（体验提升）

#### 4. UI 美化升级
#### 5. 怪物图鉴

### 优先级 P2（核心功能扩展）

#### 6. AI 自动平衡系统
#### 7. 管理员数值配置面板

### 优先级 P3（发布级功能）

#### 8. 联机对战

---

## 1. 战斗视觉特效系统（P0）

### 1.1 需要展示的视觉元素

| 类别 | 具体内容 | 实现方式 |
|---|---|---|
| **投射物** | 骷髅箭矢、烈焰人火球、先驱者飞弹、骸骨投射物、深渊弹幕 | 程序化生成（有色圆形/三角形 + 拖尾） |
| **范围攻击指示** | AOE 圈、锥形范围、光束线、冲击波环 | 程序化生成（半透明圆/扇形/线条） |
| **伤害数字** | 命中时弹出数字 | 程序化生成（Text 飘字 + 淡出） |
| **特殊素材** | 远古石碑、龙卷风、陨石、凋零飞弹、激光束 | AI 生成素材 + 程序化组合 |
| **状态效果** | 中毒/燃烧/冰冻/恐惧/蛰晕的视觉提示 | 程序化生成（图标 + 颜色） |
| **死亡效果** | 单位消失动画 | 已有（淡出+下沉），需增强 |

### 1.2 实现方案

#### 投射物渲染（ProjectileView）
```
- 从 BattleState.Projectiles 读取投射物列表
- 每个投射物创建/更新一个 GameObject
- 根据 ProjectileKind 使用不同视觉：
  - Default: 小箭矢（三角形）
  - HarbHoming: 紫色追踪弹（圆形+拖尾）
  - HarbLaser: 红色激光弹（细长矩形）
  - RevenantBone: 骨头形状
  - ForsakenSonic: 弧形声波带
  - IceBomb: 蓝色冰球
  - ProwlerMissile: 导弹（三角+尾焰）
- 队伍颜色区分（蓝/红）
```

#### 区域效果渲染（EffectView）
```
- 从 BattleState.AreaEffects 读取区域效果列表
- 根据类型渲染：
  - Shockwave: 扩散圆环（白/蓝/红/沙色）
  - LavaPatch: 红橙色半透明圆
  - FrostZone: 蓝白色半透明圆
  - SandTornado: 旋转的沙色螺旋
  - ConeStrike: 扇形半透明区域
  - Meteor: 从上方下落的火球
  - ObeliskBarrage: 多个方尖碑从天而降
  - PollutionZone: 绿色半透明圆
```

#### 光束渲染（BeamView）
```
- 从 BattleState.ActiveBeams 读取光束列表
- 渲染为发光线段：
  - Tremor: 橙色粗光束
  - HarbingerDeath: 红色死亡射线
  - ProwlerRay: 紫色射线
- 核心 + 外发光 + 边缘 三层叠加
```

#### 伤害数字（DamageNumberView）
```
- 每次伤害结算时生成
- 从命中位置向上飘移 + 淡出
- 不同伤害类型不同颜色：
  - Melee: 白色
  - Ranged: 黄色
  - Beam: 橙色
  - Explosion: 红色
  - True (DoT): 紫色
- 暴击/高伤害放大字体
```

### 1.3 实现步骤

1. 创建 `ProjectileView.cs` — 投射物渲染
2. 创建 `EffectView.cs` — 区域效果渲染
3. 创建 `BeamView.cs` — 光束渲染
4. 创建 `DamageNumberView.cs` — 伤害数字
5. 更新 `BattleBridge.cs` — 在 SyncViews 中同步所有效果
6. 在 DamageSystem 中添加伤害事件回调
7. 用 AI 生成特殊素材（龙卷风、石碑、陨石等）

### 1.4 AI 生成素材清单

| 素材 | 用途 | Prompt 方向 |
|---|---|---|
| 龙卷风贴图 | 远古遗魂沙暴/瓦吉特龙卷 | 旋转的沙色螺旋，透明背景 |
| 方尖碑贴图 | 远古遗魂/瓦吉特石碑弹幕 | 深色石柱，顶部发光 |
| 陨石贴图 | 暝煌龙陨石雨 | 燃烧的岩石，橙红色 |
| 凋零飞弹 | 先驱者追踪弹 | 紫黑色弹丸 |
| 激光束贴图 | 先驱者/徘徊者射线 | 发光线段，可平铺 |
| 冰球贴图 | 霜冻巨兽/雪怪冰弹 | 蓝白色冰晶球 |

---

## 2. 战斗结束重新开始（P0）

### 当前问题
战斗结束后显示结算界面，点"再来一局"回到商店，但：
- 旧战斗的单位 GameObject 可能残留
- BattleBridge 的 Simulator 没有被清理
- 需要完全重置状态

### 实现方案

1. GameManager.ResetToShop() 中：
   - 调用 BattleBridge.StopBattle() 清理模拟器和单位视图
   - 清理所有 ProjectileView/EffectView/BeamView 残留
   - 重置金币和购买列表
   - 显示商店 UI

2. BattleBridge 添加 StopBattle() 方法：
   - Simulator = null
   - 清理 _unitViews 中所有 GameObject
   - 清理投射物/效果视图池

3. 结算界面添加"再来一局"按钮（已有但需确保功能正确）

---

## 3. 商店滚动速度修复（P0）

### 当前问题
ScrollRect 的滚动灵敏度默认很低，鼠标滚轮几乎无法滚动。

### 实现方案

1. 在 ShopUI 中添加 ScrollRect 滚轮加速：
```csharp
void Update()
{
    float scroll = Input.mouseScrollDelta.y;
    if (scroll != 0 && scrollRect != null)
    {
        // 放大滚动量
        float speed = 50f;
        scrollRect.verticalNormalizedPosition -= scroll * speed * Time.deltaTime;
        scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition);
    }
}
```

2. 或者设置 ScrollRect 的 scrollSensitivity：
```csharp
scrollRect.scrollSensitivity = 50f; // 默认是 10
```

---

## 4. UI 美化升级（P1）

### 4.1 商店界面
- 怪物卡片：圆角 + 阴影 + 悬停高亮
- 图标：圆形遮罩
- 购买按钮：进度条样式（金币/价格比例）
- 队伍指示器：更明显的蓝/红区分
- 金币显示：带图标
- 搜索/过滤功能

### 4.2 部署界面
- 战场背景：棋盘格纹理
- 单位放置：拖拽 + 预览
- 部署区域：半透明高亮（蓝/红）
- 中线：发光效果

### 4.3 战斗界面
- 顶部 HUD：双方剩余单位 + 金币
- 底部：战斗速度控制（1x/2x/4x）
- 死亡回放/击杀提示
- 战斗统计面板

### 4.4 结算界面
- 胜方放大展示
- 战斗统计（总伤害/击杀/存活时间）
- MVP 单位展示

---

## 5. 怪物图鉴（P1）

### 功能
- 商店界面添加"图鉴"按钮
- 点击怪物卡片查看详情
- 详情页包含：
  - 大图
  - 名称/价格/属性
  - 标签
  - 技能描述
  - 对空/对地能力
  - 特殊机制说明

### 实现
- MonsterCodex UI 面板
- 从 MonsterDefSO 读取数据
- 技能描述从 MonsterDesign.md 提取

---

## 6. AI 自动平衡系统（P2）

### 架构
```
BalanceRunnerWindow (Editor 工具)
  ├── 配置：模拟场次 / 金币预算 / 种子
  ├── 运行：headless 批量模拟
  ├── 统计：胜率/存活/伤害
  ├── 调参：自动微调 ±2-3%
  └── 报告：CSV/JSON + 可视化
```

### 实现
1. MatchRunner — 跑 N 场对战
2. StatsCollector — 收集统计数据
3. BalanceTuner — 自动调参
4. BalanceReport — 生成报告
5. EditorWindow — 可视化界面

### 关键
- 利用已有的纯逻辑 BattleSimulator
- headless 模式运行（不创建 GameObject）
- 确定性种子确保可复现

---

## 7. 管理员数值配置面板（P2）

### 功能
- Editor Window 或运行时面板
- 左侧怪物列表
- 右侧数值编辑：
  - 基础属性：HP/攻击/护甲/移速/攻距/间隔/半径
  - 技能参数：各技能的冷却/伤害/范围等
- 导入/导出 JSON
- 实时预览（修改后可立即测试一场）

### 实现
1. ConfigWindow (EditorWindow)
2. 从 MonsterDefSO 读写
3. 技能参数从 AbilityDefSO 读写（需要创建）
4. 持久化到 JSON

---

## 8. 联机对战（P3）

### 架构选择
- **Netcode for GameObjects (NGO)** + Unity Relay
- Server 权威：Server 跑 BattleSimulator，Client 只渲染
- 部署阶段用 RPC 同步，战斗阶段用 NetworkVariable 广播状态

### 实现步骤
1. 安装 NGO + Relay 包
2. LobbyManager — 大厅/匹配
3. NetworkBattleServer — Server 端逻辑
4. BattleStateSerializer — 状态序列化
5. NetworkedBattleView — Client 端插值渲染
6. 打包测试

### 发布方式
- 开发期：主机模式（一个客户端同时是 Server）
- 发布期：Unity Relay（无需自建服务器，朋友间直接联机）
- 未来：专用服务器

---

## 实施顺序

| 顺序 | 任务 | 优先级 | 预计工作量 |
|---|---|---|---|
| 1 | 商店滚动速度修复 | P0 | 10 分钟 |
| 2 | 战斗结束重新开始修复 | P0 | 30 分钟 |
| 3 | 投射物渲染 | P0 | 2 小时 |
| 4 | 区域效果渲染 | P0 | 3 小时 |
| 5 | 光束渲染 | P0 | 1 小时 |
| 6 | 伤害数字 | P0 | 1 小时 |
| 7 | AI 生成特殊素材 | P0 | 1 小时 |
| 8 | UI 美化（商店/部署/战斗/结算） | P1 | 1-2 天 |
| 9 | 怪物图鉴 | P1 | 半天 |
| 10 | AI 自动平衡系统 | P2 | 2-3 天 |
| 11 | 管理员配置面板 | P2 | 1-2 天 |
| 12 | 联机对战 | P3 | 1-2 周 |

---

## 技术债务清单

| 项目 | 当前状态 | 需要修复 |
|---|---|---|
| ShopUI 卡片布局 | ContentSizeFitter 已移除，手动设高度 | 需要更健壮的布局方案 |
| UnitView HP 条 | 用 1x1 纹理缩放 | 需要用 9-slice sprite |
| BattleBridge 单位创建 | 每次新建 GameObject | 需要对象池 |
| 怪物精灵图 PPU | 已设为 1 | 需要确认所有图都正确 |
| 技能组件警告 | 3 个未使用字段警告 | 清理 |
| GameManager 字段设置 | 用反射在场景设置中赋值 | 需要更可靠的引用方式 |
