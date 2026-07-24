# MC Fight UI 系统翻新计划

> 版本：v1.0  
> 最后更新：2026-07-23  
> 状态：计划稿  
> 优先级：**先于平衡实验室（Balance Lab）实施**

---

## 目录

1. [目标与范围](#1-目标与范围)
2. [字体方案评估](#2-字体方案评估)
3. [素材方案](#3-素材方案)
4. [现状诊断](#4-现状诊断)
5. [视觉设计规范](#5-视觉设计规范)
6. [共享 UI 组件库](#6-共享-ui-组件库)
7. [动画系统](#7-动画系统)
8. [分界面翻新规格](#8-分界面翻新规格)
9. [代码与架构改造](#9-代码与架构改造)
10. [实施分期与排期](#10-实施分期与排期)
11. [验收标准](#11-验收标准)
12. [与平衡实验室的衔接](#12-与平衡实验室的衔接)

---

## 1. 目标与范围

### 1.1 目标

将当前「功能可用但视觉简陋」的 UI，翻新为**统一风格、组件化、带动画反馈**的完整界面系统，为后续平衡实验室提供 UI 基础。

### 1.2 范围

| 包含 | 不包含（本阶段） |
|------|-----------------|
| 主菜单、商店、部署、战斗 HUD、结算、图鉴 | 平衡实验室 UI（仅预留扩展点） |
| Kenney UI Pack 2.0 完整导入与规范化 | 主菜单背景、战场背景（**保持现有 AI 生成图不动**） |
| 猫啃什锦黑 + Kenney Future 字体体系 | 怪物精灵图、战斗 VFX |
| 共享 Prefab 组件库 | 联机 UI |
| UI 动画与音效反馈 | |

### 1.3 设计基调

**「方块世界 × 轻竞技 × 清晰可读」**

- 背景：保留现有 AI 生成的沉浸式场景图
- 控件：Kenney UI Pack 2.0 的扁平/微立体游戏风格
- 中文：猫啃什锦黑（活泼、辨识度高）
- 数字/英文：Kenney Future（与 UI 包同源，偏像素游戏感）

---

## 2. 字体方案评估

### 2.1 猫啃什锦黑（MaokenAssortedSans）

**文件路径（待导入）：**  
`C:\Users\Administrator\Downloads\猫啃什锦黑MaokenAssortedSans_爱给网_aigei_com.ttf`

| 维度 | 评估 |
|------|------|
| **授权** | SIL OFL 1.1，允许免费商用、嵌入游戏/APP ✅ |
| **中文覆盖** | 简体为主，满足游戏 UI 中文需求 ✅ |
| **风格** | 马克笔手绘、活泼可爱 ⚠️ 与 Minecraft 方块风不完全一致，但可作「轻量趣味」差异化 |
| **字重** | 仅 1 个字重 ⚠️ 标题/正文需靠字号和大小区分层级 |
| **文件体积** | 完整字库约 20MB+ ⚠️ Unity 中必须用 TMP 做 **Font Asset 子集化** |
| **来源** | 爱给网镜像 ⚠️ 建议对照 [GitHub 官方 Release](https://github.com/Skr-ZERO/MaokenAssortedSans) 校验版本与完整性 |

### 2.2 推荐字体分工

| 用途 | 字体 | 字号参考 |
|------|------|---------|
| 大标题（胜利 Banner 等） | 猫啃什锦黑 | 48–72 |
| 面板标题、按钮文字 | 猫啃什锦黑 | 24–36 |
| 正文、说明、图鉴描述 | 猫啃什锦黑 | 18–22 |
| 数字（金币、HP、ATK、计时） | Kenney Future | 20–28 |
| 英文标签（HP/ATK/DPS） | Kenney Future Narrow | 14–18 |

### 2.3 结论

**可以使用**，作为中文 UI 主字体没有问题。注意：

1. 从 GitHub 官方源下载 v1.70 与现有文件比对，确保未被篡改
2. 导入 Unity 后立即创建 TMP Font Asset，**静态模式 + 常用汉字子集**（游戏内文案 + 怪物名），目标 Atlas 控制在 2048×2048 或 4096×4096
3. 若实测与 Kenney 控件视觉冲突过大，标题仍用什锦黑，按钮内短文本可改用 **思源黑体 Bold** 作为备选（本计划以什锦黑为主方案）

---

## 3. 素材方案

### 3.1 Kenney UI Pack 2.0（完整包）

**来源：** `C:\Users\Administrator\Downloads\kenney_ui-pack`

| 内容 | 数量 | 用途 |
|------|------|------|
| PNG（5 色 + Extra） | 870 | 全部 UI 控件 |
| 字体 | 2 | Kenney Future 系列 |
| 音效 | 6 | 按钮点击、切换 |

**导入目标路径：**

```
Assets/Sprites/UI/Kenney/
├── PNG/
│   ├── Blue/Default/      ← 主交互色（蓝方、确认、购买）
│   ├── Red/Default/       ← 红方、危险、取消
│   ├── Green/Default/     ← 成功、存活、开始战斗
│   ├── Yellow/Default/    ← 金币、高亮、警告
│   ├── Grey/Default/      ← 面板、禁用、中性底
│   └── Extra/Default/     ← 输入框、分割线、播放/暂停图标
├── Font/
└── Sounds/
```

**导入后必做：**

- 所有 `button_rectangle_*`、`input_*` 设置 **9-Slice**（Sprite Editor → Border，通常 8–12px）
- Texture Type：Sprite (2D and UI)，Filter Mode：Point（保持像素清晰）或 Bilinear（若觉得太锐）
- Pixels Per Unit：100（与现有素材一致）

### 3.2 保留不动的素材

| 文件 | 说明 |
|------|------|
| `Assets/Sprites/UI/mainmenu_bg.jpg` | 主菜单 AI 背景 ✅ 保留 |
| `Assets/Sprites/UI/battlefield_bg.jpg` | 战场 AI 背景 ✅ 保留 |
| `Assets/Resources/Sprites/UI/battlefield_bg.jpg` | 若与上重复，统一引用路径，避免双份 |

### 3.3 需删除/归档的旧素材

导入 Kenney 完整包后，旧的零散素材（`Assets/Sprites/UI/Buttons/` 下 26 张）可归档到 `Assets/Sprites/UI/_Legacy/`，确认无引用后删除，避免混淆。

---

## 4. 现状诊断

### 4.1 界面完成度

| 界面 | 脚本 | 主要问题 |
|------|------|---------|
| **MainMenuUI** | 4 按钮（背景图已含标题，无需额外 Title） | 无样式体系，纯 Unity 默认 Button/Text |
| **ShopUI** | 动态卡片 + 滚轮 | 卡片用 `Image.color` 区分稀有度，无 Kenney 框体；无搜索/过滤；顶栏简陋 |
| **DeployUI** | 点击放置 + 标记 | 无半场高亮；标记无动画；提示条纯文本 |
| **BattleUI** | 2 个 Text | 仅存活数+计时，无 HUD 框架、无速度控制 |
| **ResultUI** | 纯 Text 统计墙 | 数据用 `StringBuilder` _dump 到单个 Text，不可读 |
| **CodexUI** | 卡片 + 详情 Text | 与商店卡片重复逻辑；详情页排版差 |

### 4.2 架构问题

```
当前：6 个独立 MonoBehaviour，各自 Find 子节点，无共享组件
      ShopUI / CodexUI 卡片 Setup 逻辑 90% 重复
      无 UITheme / UISound / UIAnimator 统一入口
      颜色硬编码在各脚本中（如 GetRarityColor）
```

### 4.3 目标架构

```
Canvas
├── UISystem (DontDestroyOnLoad 可选)
│   ├── UITheme          ← 颜色/字体/Sprite 引用
│   ├── UISoundPlayer    ← Kenney 点击音
│   └── UIAnimator       ← 面板进出场
├── Screens/
│   ├── MainMenuPanel
│   ├── ShopPanel
│   ├── DeployPanel
│   ├── BattlePanel
│   ├── ResultPanel
│   └── CodexPanel
└── Shared/
    ├── Prefabs/         ← 按钮、卡片、面板、Toast
    └── Overlays/        ← 半透明遮罩、Loading
```

---

## 5. 视觉设计规范

### 5.1 色彩

```csharp
// UITheme.cs 中统一定义
public static class UIColors
{
    // 队伍
    public static readonly Color TeamBlue      = new(0.30f, 0.60f, 1.00f);
    public static readonly Color TeamRed       = new(1.00f, 0.40f, 0.30f);
    public static readonly Color TeamBlueDim   = new(0.20f, 0.40f, 0.70f, 0.6f);
    public static readonly Color TeamRedDim    = new(0.70f, 0.25f, 0.20f, 0.6f);

    // 稀有度（商店/图鉴卡片边框色，不再整块染色）
    public static readonly Color RarityCommon    = new(0.55f, 0.55f, 0.60f);   // 普通 <50
    public static readonly Color RarityRare      = new(0.15f, 0.55f, 0.25f);   // 罕见 50–119
    public static readonly Color RarityEpic      = new(0.55f, 0.25f, 0.70f);   // 史诗 120–599
    public static readonly Color RarityLegendary = new(0.85f, 0.45f, 0.05f);   // 传说 ≥600

    // 功能
    public static readonly Color Gold          = new(1.00f, 0.84f, 0.00f);
    public static readonly Color Success       = new(0.20f, 0.80f, 0.30f);
    public static readonly Color Danger          = new(0.90f, 0.25f, 0.20f);

    // 面板
    public static readonly Color PanelOverlay    = new(0, 0, 0, 0.55f);
    public static readonly Color PanelBg         = new(0.12f, 0.12f, 0.15f, 0.92f);
}
```

### 5.2 稀有度与价格档映射（改为边框/标签）

| 价格 | 档位 | 边框色 | Kenney 强调 |
|------|------|--------|------------|
| < 50 | 普通 | Grey | — |
| 50–119 | 罕见 | Green | — |
| 120–599 | 史诗 | Purple | `star` |
| ≥ 600 | 传说 | Yellow/Gold | `star` + 微光动画 |

```csharp
public enum RarityTier { Common, Rare, Epic, Legendary }

public static RarityTier GetRarityTier(int price)
{
    if (price >= 600) return RarityTier.Legendary;
    if (price >= 120) return RarityTier.Epic;
    if (price >= 50)  return RarityTier.Rare;
    return RarityTier.Common;
}
```

实现时统一替换 `ShopUI` / `CodexUI` / `MonsterCardView` 中的 `GetRarityColor`，避免各脚本阈值不一致。

### 5.3 间距与尺寸（8px 网格）

| Token | 值 | 用途 |
|-------|-----|------|
| `space-xs` | 4 | 图标与文字间距 |
| `space-sm` | 8 | 卡片内 padding |
| `space-md` | 16 | 面板内边距 |
| `space-lg` | 24 | 区块间距 |
| `space-xl` | 32 | 面板外边距 |
| `btn-height` | 48 | 标准按钮高度 |
| `card-width` | 200 | 怪物卡片宽度 |
| `card-height` | 260 | 怪物卡片高度 |
| `top-bar-height` | 64 | 顶栏高度 |

### 5.4 Kenney 精灵映射表（核心控件）

| UI 组件 | Kenney 精灵 | 色板 |
|---------|------------|------|
| 主按钮 | `button_rectangle_depth_gradient` | Blue |
| 次要按钮 | `button_rectangle_flat` | Grey |
| 危险/退出 | `button_rectangle_depth_gradient` | Red |
| 成功/开始 | `button_rectangle_depth_gradient` | Green |
| 金币/强调 | `button_rectangle_border` | Yellow |
| 面板底 | `button_rectangle_flat` 拉伸 | Grey（9-slice） |
| 弹窗底 | `button_rectangle_depth_flat` | Grey |
| 输入框 | `input_outline_rectangle` | Extra |
| 水平进度条 | `slide_horizontal_color` + `_section` | Blue |
| 滚动条滑块 | `slide_horizontal_grey_section` | Grey |
| 勾选 | `check_square_color` | Blue/Green |
| 关闭 | `icon_cross` on `button_round_flat` | Red |
| 返回 | `arrow_basic_w` + 文字 | Grey |

---

## 6. 共享 UI 组件库

### 6.1 Prefab 清单

路径：`Assets/Prefabs/UI/`

| Prefab | 说明 | 用于 |
|--------|------|------|
| `BtnPrimary` | 蓝渐变按钮 + 什锦黑文字 + hover 态 | 确认、购买、开始 |
| `BtnSecondary` | 灰 flat 按钮 | 返回、取消 |
| `BtnDanger` | 红渐变按钮 | 退出、清空 |
| `BtnIcon` | 圆按钮 + 图标 | 关闭、队伍切换 |
| `Panel` | 9-slice 面板 + 可选标题栏 | 所有 Screen 容器 |
| `PanelModal` | 面板 + 半透明全屏遮罩 | 图鉴详情、确认框 |
| `TopBar` | 左标题 + 右金币/队伍 | 商店、部署 |
| `GoldDisplay` | 金币图标 + 数字滚动 | 商店顶栏 |
| `TeamToggle` | 蓝/红切换 Tab | 商店、部署 |
| `MonsterCard` | 统一怪物卡片 | 商店、图鉴 |
| `StatRow` | 标签 + 数值 + 可选进度条 | 结算、图鉴 |
| `StatTable` | 可滚动统计表 | 结算 |
| `Toast` | 顶部滑入提示 | 购买成功、战斗击杀 |
| `ProgressBar` | 填充条 + 百分比 | 通用 |
| `ScrollViewStyled` | 带 Kenney 滚动条的 ScrollRect | 商店、图鉴、结算 |
| `Chip` | 小标签（标签/tag） | 图鉴、卡片 |
| `Divider` | `divider` 精灵 | 面板内分隔 |

### 6.2 MonsterCard 统一规格

**一个 Prefab 解决 Shop + Codex 重复代码。**

```
MonsterCard (200×260)
├── Border          ← Image, 稀有度颜色, 9-slice
├── Background      ← Kenney panel grey
├── ArtFrame        ← 圆形/圆角遮罩
│   └── Art         ← 怪物 idleSprite
├── NameBar         ← 什锦黑 displayName
├── CostChip        ← Kenney yellow + 金币 icon + price
├── StatsRow        ← Kenney Future: HP / ATK / ARM
├── TagRow          ← Chip × N (explosive, fly...)
├── CountBadge      ← 可选, "蓝2 红1"
└── ActionRow       ← 商店: BuyBtn + BulkBtn / 图鉴: 无或「详情」
```

**脚本：** `MonsterCardView.cs`

```csharp
public class MonsterCardView : MonoBehaviour
{
    public void Bind(MonsterDefSO def, MonsterCardMode mode, ShopContext? shopCtx);
    public event Action<MonsterDefSO> OnClicked;
}
```

---

## 7. 动画系统

### 7.1 技术选型

- **面板进出场 / 按钮反馈：** Unity Animator + Animation Clip（不引入 DOTween 依赖，保持零依赖）
- **数字滚动：** 协程 Lerp（金币变化、计时器）
- **可选增强：** `CanvasGroup` 控制 Fade

### 7.2 动画清单

| ID | 触发 | 效果 | 时长 |
|----|------|------|------|
| `panel-in` | Screen.Show() | Scale 0.92→1.0 + Alpha 0→1 | 220ms ease-out |
| `panel-out` | Screen.Hide() | Scale 1.0→0.96 + Alpha 1→0 | 150ms ease-in |
| `btn-hover` | PointerEnter | Sprite → hover + Scale 1.04 | 80ms |
| `btn-press` | PointerDown | Scale 0.96 | 60ms |
| `btn-release` | PointerUp | 回弹 1.0 | 80ms |
| `card-pop` |  Instantiate 卡片 | Scale 0→1 overshoot | 200ms |
| `gold-bump` | 购买成功 | 金币 Text Scale 1→1.2→1 | 300ms |
| `toast-in` | 事件触发 | 从 Top 滑入 | 250ms |
| `toast-out` | 2s 后 | 滑出 + Fade | 200ms |
| `deploy-pulse` | 放置单位 | 标记 Scale 脉冲 | 400ms |
| `victory-banner` | 结算 Show | 标题从上方弹入 + 星粒子 | 500ms |
| `stat-row-in` | 结算列表 | 逐行 FadeIn（stagger 50ms） | — |

### 7.3 UI 音效

| 事件 | 音效文件 |
|------|---------|
| 按钮点击 | `Sounds/click-a.ogg` |
| 切换/Tab | `Sounds/switch-a.ogg` |
| 购买/确认 | `Sounds/tap-a.ogg` |
| 返回/取消 | `Sounds/click-b.ogg` |

**脚本：** `UISoundPlayer.cs`，挂载 Canvas，各按钮 OnClick 统一调用。

---

## 8. 分界面翻新规格

### 8.1 主菜单（MainMenuPanel）

**背景：** 保持 `mainmenu_bg.jpg` 全屏 RawImage。**背景图已自带游戏标题，UI 层不再叠加 Title / Subtitle 文字。**

```
┌─────────────────────────────────────────────┐
│  [AI 背景 mainmenu_bg.jpg - 含标题，不动]       │
│                                             │
│         （无 UI 标题，露出背景图自带标题）        │
│                                             │
│         ┌──────────────────┐               │
│         │   双人对战 (PvP)   │               │  ← BtnPrimary Blue
│         └──────────────────┘               │
│         ┌──────────────────┐               │
│         │   单人挑战 (PvAI)  │               │
│         └──────────────────┘               │
│         ┌──────────────────┐               │
│         │      怪物图鉴      │               │  ← BtnSecondary
│         └──────────────────┘               │
│         ┌──────────────────┐               │
│         │      退出游戏      │               │  ← BtnDanger 小尺寸
│         └──────────────────┘               │
│                                             │
│  v0.x                          [⚗️实验室灰显] │  ← Balance Lab 入口预留，disabled
└─────────────────────────────────────────────┘
```

**布局要点：**
- 按钮组垂直居中偏下（约屏幕 55%–75% 高度），避免遮挡背景图标题区域
- 移除 `MainMenuUI` 中的 `titleText`、`subtitleText` 引用（若 Scene 中存在则删除节点）

**改动：**
- 新建 MainMenu 布局，绑定 Kenney 按钮 Sprite，**不含标题 Text**
- 按钮 stagger 进场动画（依次 panel-in 延迟 80ms）
- `MainMenuUI.cs` 增加 `CanvasGroup` 淡入，删除标题相关字段

---

### 8.2 商店（ShopPanel）

```
┌──────────────────────────────────────────────────────────┐
│ TopBar                                                    │
│ [MC Fight 商店]     蓝方 1000G 💰 | 红方 1000G 💰   [图鉴] │
│ TeamToggle:  (●蓝方) (○红方)                              │
├──────────────────────────────────────────────────────────┤
│ FilterBar (新增)                                          │
│ [搜索框 input_outline]  [价格▼] [类型▼] [标签▼]             │
├──────────────────────────────────────────────────────────┤
│ ScrollViewStyled                                          │
│ ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐                       │
│ │Monster│ │Monster│ │Monster│ │Monster│  ...               │
│ │ Card  │ │ Card  │ │ Card  │ │ Card  │                       │
│ └──────┘ └──────┘ └──────┘ └──────┘                       │
├──────────────────────────────────────────────────────────┤
│ [开始部署 →] BtnPrimary Green    [一键购买并开战] Yellow   │
└──────────────────────────────────────────────────────────┘
```

**改动：**
- 提取 `MonsterCardView`，删除 ShopUI 内重复 Setup 逻辑
- 顶栏 `GoldDisplay` 购买时 gold-bump 动画
- 搜索/过滤（本地过滤 `_sortedMonsters`，P1 必做）
- 卡片 Grid：GridLayoutGroup，cell 200×280，spacing 16
- 买不起时 BuyBtn 变 Grey disabled + 「金币不足」

---

### 8.3 部署（DeployPanel）

**背景：** 保持 `battlefield_bg.jpg`（战场区域）。

```
┌──────────────────────────────────────────────────────────┐
│ TopBar: 部署阶段 | 蓝待放: 5 | 红待放: 3 | TeamToggle      │
├──────────────────────────┬───────────────────────────────┤
│                          │ SidePanel (可选，窄)           │
│   BattlefieldArea        │ 待部署队列                     │
│   ┌────────┬────────┐   │ ┌──── icon  creeper ×2 ───┐  │
│   │ 蓝半场  │ 红半场  │   │ └─────────────────────────┘  │
│   │ 半透明  │ 半透明  │   │                              │
│   │ 蓝 tint │ 红 tint │   │                              │
│   └────────┴────────┘   │                              │
│   --- 中线 glow ---       │                              │
│   [单位标记 + 名称]         │                              │
├──────────────────────────┴───────────────────────────────┤
│ 提示: 点击半场放置单位 — 当前: 蓝方                         │
│ [自动部署] BtnSecondary    [开始战斗 →] BtnPrimary Green   │
└──────────────────────────────────────────────────────────┘
```

**改动：**
- 半场 Overlay：两个半透明 Image（Blue/Red，Alpha 0.15），仅当前队伍侧高亮（Alpha 0.25）
- 中线：1px 宽 + 可选 soft glow 条
- 单位标记 Prefab：Kenney `button_round_flat` 底 + 怪物图 + deploy-pulse
- 放置非法区域时 Toast 「只能放在己方半场」
- SidePanel 显示 `ShopEntries` 剩余队列（点击可高亮对应怪物）

---

### 8.4 战斗 HUD（BattlePanel）

**不遮挡战场中央，HUD 贴顶 + 贴底。**

```
┌──────────────────────────────────────────────────────────┐
│ ┌─ HUD Top ─────────────────────────────────────────────┐│
│ │ [蓝 ■■■ 5]    ⏱ 01:23    [■■■ 3 红] │ Speed: [1x][2x][4x] │
│ └───────────────────────────────────────────────────────┘│
│                                                          │
│              [ 战场 - BattleBridge 渲染 ]                  │
│                                                          │
│ ┌─ HUD Bottom (可选) ───────────────────────────────────┐│
│ │ 击杀提示 Toast 队列                                     ││
│ └───────────────────────────────────────────────────────┘│
└──────────────────────────────────────────────────────────┘
```

**改动：**
- `BattleUI.cs` 重构：存活数用图标块（Kenney `icon_square`）而非纯文字
- **战斗速度控制（新增）：** 1x / 2x / 4x，修改 `BattleBridge` tick 倍率
- 计时器 Kenney Future 字体，最后一分钟变 Yellow
- 击杀 Toast：「苦力怕 击杀了 骷髅！」滑入（可选 P1）
- 顶栏 HUD 背景：Grey panel 9-slice，Alpha 0.85

---

### 8.5 结算（ResultPanel）

**不再使用单一 Text 墙。**

```
┌──────────────────────────────────────────────────────────┐
│ [半透明遮罩 PanelOverlay]                                  │
│  ┌────────────────────────────────────────────────────┐  │
│  │  🏆 蓝方胜利！                    [star 动画]        │  │  ← victory-banner
│  │  战斗时长: 45.2s                                   │  │
│  ├────────────────────────────────────────────────────┤  │
│  │  Tab: [蓝方统计] [红方统计] [全部]                     │  │
│  ├────────────────────────────────────────────────────┤  │
│  │  ScrollViewStyled - StatTable                       │  │
│  │  ┌──────────────────────────────────────────────┐  │  │
│  │  │ MVP ★ 苦力怕  伤害 1250  击杀 3  存活 ✓       │  │  │
│  │  │ 骷髅      伤害 890   击杀 1  阵亡 ✗           │  │  │
│  │  │ ...                                          │  │  │
│  │  └──────────────────────────────────────────────┘  │  │
│  ├────────────────────────────────────────────────────┤  │
│  │  [再来一局] BtnPrimary    [返回主菜单] BtnSecondary  │  │
│  └────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────┘
```

**改动：**
- 新建 `ResultStatRow` Prefab：怪物头像 + 名称 + 伤害条 + 击杀 + 状态
- MVP：伤害最高单位，卡片加 Gold 边框 + star
- `ResultUI.cs` 拆分为数据绑定逻辑 + View 刷新
- 支持 Tab 切换蓝/红/全部
- stat-row-in  stagger 动画

---

### 8.6 图鉴（CodexPanel）

```
┌──────────────────────────────────────────────────────────┐
│ TopBar: 怪物图鉴                              [返回]     │
│ FilterBar: [搜索] [价格▼] [标签▼]                         │
├──────────────────────────────────────────────────────────┤
│ MonsterCard Grid (同商店，Mode=Codex，无购买按钮)          │
└──────────────────────────────────────────────────────────┘

详情 Modal:
┌─────────────────────────────────────────┐
│ [大图]  苦力怕                    [×]    │
│ 价格 20G   标签: explosive              │
│ ─────────────────────                   │
│ HP 20  ATK 49  ARM 0  SPD 42            │  ← StatRow
│ 技能: 接近目标后自爆...                  │
│ 定位: AOE 自爆 / 刺客                    │  ← 来自 CodexUI 描述
└─────────────────────────────────────────┘
```

**改动：**
- 复用 `MonsterCardView`（Codex 模式）
- 详情 Modal 用 `PanelModal` + 结构化 StatRow，弃用单 Text `_dump`
- FilterBar 与商店共享 `MonsterFilterBar.cs`

---

## 9. 代码与架构改造

### 9.1 新增脚本

```
Assets/Scripts/UI/
├── Core/
│   ├── UITheme.cs              // 颜色、Sprite、字体引用 ScriptableObject
│   ├── UISoundPlayer.cs
│   ├── UIAnimator.cs           // Show/Hide 面板
│   ├── UIButtonStyled.cs       // 统一 hover/press + 音效
│   └── ScreenBase.cs           // Show/Hide/CanvasGroup 基类
├── Components/
│   ├── MonsterCardView.cs
│   ├── MonsterFilterBar.cs
│   ├── GoldDisplay.cs
│   ├── TeamToggle.cs
│   ├── StatRow.cs
│   ├── StatTable.cs
│   ├── ToastQueue.cs
│   └── ProgressBarView.cs
├── Screens/                    // 可选：从原脚本迁移
│   ├── MainMenuUI.cs           // 重构
│   ├── ShopUI.cs
│   ├── DeployUI.cs
│   ├── BattleUI.cs
│   ├── ResultUI.cs
│   └── CodexUI.cs
```

### 9.2 UITheme ScriptableObject

```csharp
[CreateAssetMenu(menuName = "MC Fight/UI Theme")]
public class UITheme : ScriptableObject
{
    [Header("Fonts")]
    public TMP_FontAsset FontTitleChinese;   // 什锦黑
    public TMP_FontAsset FontBodyChinese;
    public TMP_FontAsset FontNumeric;        // Kenney Future

    [Header("Buttons - Blue")]
    public Sprite BtnPrimaryNormal;
    public Sprite BtnPrimaryHover;
    // ... 其他 Sprite 引用
}
```

所有 Prefab 引用 `UITheme` asset，换肤只改一处。

### 9.3 迁移策略

**不 rewrite GameManager 流程**，只改 View 层：

1. 各 UI 仍暴露 `Show()` / `Hide()`，GameManager 无感
2. 逐 Screen 替换 Prefab，脚本接口保持不变
3. ShopUI / CodexUI 合并卡片逻辑到 `MonsterCardView`

### 9.4 BattleBridge 速度控制

```csharp
// BattleBridge.cs 新增
public float SpeedMultiplier = 1f;

// Update 中
_accumulatedTime += Time.deltaTime * SpeedMultiplier;
while (_accumulatedTime >= BattleConstants.TICK_DT) { ... }
```

---

## 10. 实施分期与排期

### Phase UI-0：基础设施（1–2 天）

- [ ] 导入 Kenney 完整包 → `Assets/Sprites/UI/Kenney/`
- [ ] 导入猫啃什锦黑 → `Assets/Sprites/UI/Font/`，创建 TMP Font Asset（子集化）
- [ ] 导入 Kenney Future TMP Font Asset
- [ ] 导入 UI 音效 → `Assets/Audio/UI/`
- [ ] 创建 `UITheme.asset`
- [ ] 9-slice 批量设置（Editor 脚本或手动核心控件）
- [ ] 归档 `_Legacy` 旧 UI 素材

### Phase UI-1：共享组件（2–3 天）

- [ ] Prefab：`BtnPrimary/Secondary/Danger/Icon`
- [ ] Prefab：`Panel`、`PanelModal`、`TopBar`、`GoldDisplay`、`TeamToggle`
- [ ] Prefab：`MonsterCardView`、`StatRow`、`ScrollViewStyled`、`Toast`
- [ ] 脚本：`UITheme`、`UISoundPlayer`、`UIButtonStyled`、`ScreenBase`
- [ ] 脚本：`MonsterCardView`、`ToastQueue`

### Phase UI-2：主菜单 + 商店（2 天）

- [ ] 翻新 MainMenuPanel（保留 AI 背景）
- [ ] 翻新 ShopPanel + FilterBar
- [ ] 接入 MonsterCardView，删除旧卡片 Setup
- [ ] 动画：panel-in、gold-bump、card-pop

### Phase UI-3：部署 + 战斗 HUD（2 天）

- [ ] DeployPanel 半场高亮 + 标记 Prefab + SidePanel
- [ ] BattlePanel HUD 顶栏 + 速度控制
- [ ] BattleBridge SpeedMultiplier
- [ ] 可选：击杀 Toast

### Phase UI-4：结算 + 图鉴（2 天）

- [ ] ResultPanel StatTable + MVP + Tab
- [ ] CodexPanel 复用卡片 + Modal 详情
- [ ] victory-banner、stat-row-in 动画

### Phase UI-5：打磨与文档（1–2 天）

- [ ] 全流程 Play 测试：MainMenu → Shop → Deploy → Battle → Result → Codex
- [ ] 修复布局（1920×1080、1366×768）
- [ ] 更新 `CompletionSummary.md` UI 完成度
- [ ] 截图对比（before/after）存入 `screenshots/ui_refresh/`

**总计：约 10–13 天**

---

## 11. 验收标准

### 11.1 视觉

- [ ] 所有按钮使用 Kenney 精灵，无 Unity 默认 UI Skin
- [ ] 中文全部可读（什锦黑 TMP 子集覆盖所有怪物名 + UI 文案）
- [ ] 主菜单/战场背景仍为原有 AI 图，未替换
- [ ] 6 个界面色彩、间距遵循 UITheme，无脚本内硬编码颜色（除稀有度）

### 11.2 交互

- [ ] 所有按钮有 hover/press 反馈 + 点击音效
- [ ] 面板切换有进出场动画
- [ ] 商店购买有金币动画反馈
- [ ] 战斗支持 1x/2x/4x 速度

### 11.3 代码

- [ ] ShopUI 与 CodexUI 共用 MonsterCardView，无重复 Setup 逻辑
- [ ] UITheme 单点换肤
- [ ] GameManager 流程无需修改即可跑通

### 11.4 性能

- [ ] 什锦黑 TMP Atlas ≤ 4096×4096
- [ ] 商店 80+ 卡片 ScrollRect 滚动流畅（对象池可选，P2）

---

## 12. 与平衡实验室的衔接

UI 翻新完成后，Balance Lab 可直接复用：

| 实验室需求 | 复用组件 |
|-----------|---------|
| 需求对话框 | `Panel` + `input_outline` + `ScrollViewStyled` |
| 计划预览表格 | `StatTable` + `StatRow` |
| Case 编辑器 | `MonsterCardView` + `TeamToggle` + `BtnPrimary` |
| 执行进度 | `ProgressBarView` + `TopBar` |
| 报告查看 | `PanelModal` + `StatTable` + Tab |
| 历史列表 | `MonsterCard` 变体 → `SessionCard` |
| 聊天气泡 | 新建 `ChatBubble` Prefab（基于 Panel 9-slice，UI-5 后加） |

**主菜单预留：** 「平衡实验室」按钮 Phase UI-2 以 **disabled + 灰色** 占位，Balance Lab 开发完成后启用。

---

## 附录 A：字体导入 Checklist

1. 从 [GitHub Releases](https://github.com/Skr-ZERO/MaokenAssortedSans/releases) 下载官方 `MaokenAssortedSans.ttf`
2. 与 Downloads 中爱给网版本 **MD5 比对**（可选但推荐）
3. Unity → 拖入 `Assets/Sprites/UI/Font/`
4. Window → TextMeshPro → Font Asset Creator
   - Source: MaokenAssortedSans.ttf
   - Character Set: Custom Characters（粘贴常用 UI 字符 + 全部怪物 displayName）
   - Atlas: 4096×4096, SDF
5. 重复创建 `Kenney Future SDF` 用于数字

## 附录 B：当前文件索引

| 类型 | 路径 |
|------|------|
| UI 脚本 | `Assets/Scripts/UI/*.cs` |
| 旧 UI 素材 | `Assets/Sprites/UI/` |
| 背景（不动） | `mainmenu_bg.jpg`, `battlefield_bg.jpg` |
| 场景 | `Assets/Scenes/BattleScene.unity` |
| Kenney 源包 | `C:\Users\Administrator\Downloads\kenney_ui-pack` |
| 字体源 | `C:\Users\Administrator\Downloads\猫啃什锦黑MaokenAssortedSans_爱给网_aigei_com.ttf` |

## 附录 C：Before / After 对照（目标）

| 项目 | Before | After |
|------|--------|-------|
| 按钮 | Unity 默认 | Kenney 渐变 + 动画 + 音效 |
| 卡片 | Image.color 染色 | 9-slice 边框 + 稀有度色 + 统一 Prefab |
| 结算 | 纯 Text 墙 | 结构化表格 + MVP + Tab |
| 战斗 | 2 行 Text | 完整 HUD + 速度控制 |
| 字体 | 系统默认 | 什锦黑 + Kenney Future |
| 代码 | 6 份重复逻辑 | UITheme + 共享组件 |
