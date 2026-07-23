# 从零到实战：Unity 初学者完全指南

> 以 MC Fight（Minecraft 主题自走棋）项目为教学案例

---

# 第一部分：Unity 基础入门

---

## 第 1 章：Unity 是什么

### 1.1 游戏引擎的概念

**游戏引擎**是一套预先编写好的工具和功能集合，让开发者不必从零开始实现渲染、物理、音频、输入等底层系统。你可以把它想象成一个"半成品的游戏框架"——引擎负责底层，你负责创意和逻辑。

Unity 是全球使用最广泛的游戏引擎之一，支持：
- **2D 游戏**（平台跳跃、自走棋、RPG、解谜等）
- **3D 游戏**（FPS、开放世界、赛车等）
- **非游戏应用**（建筑可视化、汽车仪表盘、AR 体验等）

### 1.2 为什么 MC Fight 选择 Unity 2D

MC Fight 是一个 2D 自走棋/塔防模拟游戏。选择 Unity 2D 的原因：
- 内置 2D 渲染管线、2D 物理、Tilemap 等工具
- 丰富的 UI 系统（UGUI）适合制作商店、部署界面
- 编辑器工具链成熟，方便制作怪物数据生成器
- 跨平台发布能力（PC、移动端、Web）

### 1.3 Unity 的核心架构

Unity 采用**组件-实体**架构（严格说是"组件化 GameObject"）：

```
GameObject（游戏对象）
  ├── Transform（位置/旋转/缩放）
  ├── SpriteRenderer（渲染精灵图）
  ├── MonoBehaviour 脚本（你的游戏逻辑）
  └── 其他组件...
```

每个 GameObject 本身只是一个"空壳"，真正赋予它功能的是挂在上面的**组件（Component）**。这种设计让你通过组合不同组件来创建各种游戏对象，而不是通过继承。

---

## 第 2 章：安装与项目创建

### 2.1 Unity Hub 安装

1. 前往 [unity.com](https://unity.com) 下载 **Unity Hub**
2. 安装 Hub 后，在 "Installs" 页面点击 "Install Editor"
3. 选择版本：MC Fight 使用的是 **2022.3.62f3c1**（LTS 长期支持版）
4. 勾选 **Microsoft Visual Studio Community** 作为 IDE

### 2.2 项目创建

1. 在 Hub 中点击 "New Project"
2. 模板选择 **2D (URP)** 或 **2D Core**
3. 设置项目名称和路径

### 2.3 项目目录结构

打开 MC Fight 项目后，你会看到这样的目录：

```
My project/
├── Assets/                  ← 所有游戏资源（代码、图片、场景等）
│   ├── Scripts/             ← C# 源代码
│   ├── Scenes/              ← 场景文件
│   ├── Resources/           ← 运行时动态加载的资源
│   ├── Prefabs/             ← 预制体
│   ├── ScriptableObjects/   ← ScriptableObject 数据资产
│   ├── Sprites/             ← 精灵图（怪物、UI、特效）
│   ├── UI Toolkit/          ← UI 工具箱资源
│   ├── JMO Assets/          ← 第三方插件（Cartoon FX Remaster）
│   └── Docs/                ← 项目文档
├── Packages/                ← Unity 包管理器配置
├── ProjectSettings/         ← 项目设置
├── Library/                 ← Unity 自动生成的缓存（不要手动修改）
└── Logs/                    ← 编辑器日志
```

**重要提示：**
- `Assets/` 是你唯一需要关心的目录
- `Library/` 是缓存，可以删除后重新导入（类似 node_modules）
- `ProjectSettings/ProjectSettings.asset` 存储项目全局配置

---

## 第 3 章：编辑器界面

Unity 编辑器由多个窗口组成，每个窗口负责不同职能。

### 3.1 核心窗口

| 窗口 | 快捷键 | 功能 |
|------|--------|------|
| **Scene** | - | 可视化编辑场景，拖拽摆放物体 |
| **Game** | Ctrl+G | 运行游戏时的视图 |
| **Hierarchy** | - | 当前场景中所有 GameObject 的树形列表 |
| **Inspector** | - | 选中对象后显示其所有组件和属性 |
| **Project** | - | 项目所有资源文件的浏览器 |
| **Console** | Ctrl+Shift+C | 查看 Debug.Log 输出和错误信息 |

### 3.2 以 MC Fight 为例

打开 `Assets/Scenes/BattleScene.unity`，在 Hierarchy 窗口中你会看到场景的根对象：

```
BattleScene
├── Main Camera          ← 摄像机（正交投影，俯视 2D）
├── GameManager          ← 游戏管理器（状态机）
├── BattleBridge         ← 战斗桥接器（连接模拟与渲染）
├── Canvas               ← UI 画布（所有 UI 面板的父容器）
│   ├── MainMenuPanel    ← 主菜单面板
│   ├── ShopPanel        ← 商店面板
│   ├── DeployPanel      ← 部署面板
│   ├── BattlePanel      ← 战斗信息面板
│   ├── ResultPanel      ← 结算面板
│   └── CodexPanel       ← 图鉴面板
└── BattleField          ← 战场背景渲染
```

选中 `GameManager`，在 Inspector 中可以看到它挂载的 `GameManager (Script)` 组件，以及所有 `public` 字段（如 `shopUI`、`deployUI` 等引用）。

### 3.3 Scene 视图操作

- **左键拖拽**：平移视图
- **滚轮**：缩放
- **Q 键**：平移工具
- **W 键**：移动工具（选中物体后拖拽箭头）
- **鼠标中键**：平移视图

---

## 第 4 章：场景与游戏对象

### 4.1 什么是 Scene

Scene（场景）是一个游戏关卡或界面的容器。它包含：
- 所有 **GameObject**（游戏对象）
- 每个对象的 **Component**（组件）配置
- 场景级别的设置（光照、环境等）

MC Fight 只有**一个主场景** `BattleScene.unity`，所有游戏阶段（主菜单→商店→部署→战斗→结算）都在这个场景内通过显示/隐藏 UI 面板来切换。

### 4.2 什么是 GameObject

GameObject（游戏对象）是 Unity 中最基本的实体。它本身只是一个**命名容器**，真正定义其行为的是挂载的组件。

```
GameObject: "GameManager"
  ├── Transform          ← 位置（在场景中的坐标）
  └── GameManager.cs     ← 你写的脚本（继承 MonoBehaviour）
```

### 4.3 父子关系

GameObject 可以形成父子层级：
- 移动父对象时，子对象跟随移动
- 禁用父对象时，子对象也一起禁用

在 MC Fight 的 UI 中：
```
Canvas
├── ShopPanel (Panel)
│   ├── Title (Text)
│   ├── MonsterGrid (GridLayoutGroup)
│   │   ├── MonsterCard_1
│   │   ├── MonsterCard_2
│   │   └── ...
│   └── BuyButton (Button)
```

### 4.4 创建 GameObject 的方式

1. **编辑器中**：Hierarchy 窗口右键 → Create Empty / UI / 2D Object
2. **代码中**：`new GameObject("名字")` 或 `Instantiate(prefab)`
3. **预制体实例化**：从 Project 窗口拖入 Scene，或代码 `Instantiate()`

---

## 第 5 章：组件系统

### 5.1 Component 是什么

组件（Component）是附加到 GameObject 上的功能模块。Unity 的哲学是：**不要用继承来组合功能，要用组件**。

```
GameObject: "怪物"
  ├── Transform              ← 位置/旋转/缩放
  ├── SpriteRenderer         ← 渲染精灵图
  ├── MonsterBehavior.cs     ← 你的自定义脚本
  └── CircleCollider2D       ← 圆形碰撞器
```

### 5.2 内置组件 vs 自定义组件

**内置组件**（Unity 提供）：
- `Transform`：所有对象必有
- `SpriteRenderer`：2D 精灵渲染
- `Camera`：摄像机
- `Canvas`：UI 画布
- `Button`、`Text`：UI 组件

**自定义组件**（你写的脚本）：
```csharp
public class GameManager : MonoBehaviour  // 继承 MonoBehaviour 就是一个组件
{
    public int gold = 1000;  // public 字段会自动显示在 Inspector
    void Update() { /* 每帧执行 */ }
}
```

### 5.3 Inspector 操作

在 Inspector 窗口中，你可以：
- **修改 public 字段的值**（如修改 gold 从 1000 改为 2000）
- **拖拽引用**（把 Hierarchy 中的对象拖到 public 字段上）
- **添加/移除组件**（Add Component 按钮）
- **启用/禁用组件**（组件名左边的复选框）

### 5.4 [Header] 和 [Tooltip]

MC Fight 中大量使用了这些特性来让 Inspector 更易读：

```csharp
[Header("基本信息")]
public string monsterId;
public string displayName;
public int price;

[Header("战斗属性")]
public float hp = 100;
public float attack = 10;

[Tooltip("Boss 的特殊技能组件类型名，留空则使用通用攻击模式")]
public string abilityComponentType;
```

在 Inspector 中，这会显示为分组标题和悬停提示。

---

## 第 6 章：Transform 组件

### 6.1 Transform 是什么

`Transform` 是**唯一一个不能移除的组件**。每个 GameObject 都有且只有一个 Transform，它定义了对象在空间中的：
- **Position**：位置（x, y, z）
- **Rotation**：旋转角度
- **Scale**：缩放比例

### 6.2 本地坐标 vs 世界坐标

```
世界坐标系（World Space）：整个场景共享的坐标系
  └── 父对象的 Transform
        └── 子对象的 Transform（本地坐标，相对于父对象）
```

- **世界坐标**：对象在场景中的绝对位置
- **本地坐标**：对象相对于父对象的位置

在 MC Fight 中，战场使用世界坐标系：
```csharp
// Constants.cs
public const float FIELD_WIDTH = 1280f;
public const float FIELD_HEIGHT = 720f;
public const float FIELD_MID_X = 640f;  // 中线 x=640
```

所有单位的位置都用世界坐标存储在 `UnitState` 中。

### 6.3 2D 坐标系

Unity 2D 中，x 轴向右，y 轴向上，z 轴朝向屏幕外：
```
        y (向上)
        ↑
        |
        |
        +----→ x (向右)
```

MC Fight 的战场范围是 `(0,0)` 到 `(1280,720)`，摄像机正交投影俯视整个战场。

---

## 第 7 章：生命周期

### 7.1 MonoBehaviour 回调顺序

Unity 会在特定时机自动调用 MonoBehaviour 的方法，这就是**生命周期**：

```
Awake()        ← 对象创建时立即调用（只调用一次）
  ↓
Start()        ← 第一帧 Update 之前调用（只调用一次）
  ↓
Update()       ← 每帧调用一次（~60fps = 每秒60次）
  ↓
FixedUpdate()  ← 每固定时间调用（默认 0.02s = 每秒50次）
  ↓
LateUpdate()   ← 每帧 Update 之后调用
  ↓
OnDestroy()    ← 对象被销毁时调用
```

### 7.2 MC Fight 中的生命周期实例

**GameManager.cs** 是理解生命周期的绝佳案例：

```csharp
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    void Awake()
    {
        // 第一步：设置单例引用
        Instance = this;
        // 第二步：加载怪物数据库（Resources.LoadAll）
        Database = new MonsterDatabase();
        Database.LoadAll();
    }

    void Start()
    {
        // 第三步：查找场景中的 UI 组件
        var canvas = FindObjectOfType<Canvas>();
        if (canvas != null) FindUIRecursive(canvas.transform);
        BattleBridge = FindObjectOfType<BattleBridge>();

        // 第四步：进入主菜单
        EnterMainMenu();
    }

    void Update()
    {
        // GameManager 本身不在 Update 中做太多事
        // 战斗模拟由 BattleBridge.Update 驱动
    }
}
```

**执行顺序分析：**
1. `Awake()` — 创建单例，加载怪物数据（此时 UI 可能还没找到）
2. `Start()` — 等所有 Awake 执行完后，才查找 UI 并进入主菜单
3. 这就是为什么数据库在 Awake 加载、UI 在 Start 查找——保证顺序正确

### 7.3 Update vs FixedUpdate

| 方法 | 调用频率 | 适用场景 |
|------|----------|----------|
| `Update()` | 每帧（不固定） | 输入检测、UI 更新、视觉同步 |
| `FixedUpdate()` | 固定 50fps | 物理计算 |

MC Fight 有一个特别之处：**战斗模拟不使用 FixedUpdate**，而是自己实现了固定步长：

```csharp
// BattleBridge.cs
void Update()
{
    _accumulatedTime += Time.deltaTime;
    int maxSteps = 3;

    while (_accumulatedTime >= BattleConstants.TICK_DT && maxSteps > 0)
    {
        Simulator.Tick(BattleConstants.TICK_DT);  // 1/60s 固定步长
        _accumulatedTime -= BattleConstants.TICK_DT;
        maxSteps--;
    }
}
```

这样做的好处：模拟逻辑完全不依赖 Unity 的物理系统，可以独立运行。

---

## 第 8 章：预制体

### 8.1 Prefab 是什么

**预制体（Prefab）**是一个 GameObject 的"模板"。它保存了对象及其所有组件、子对象、资源引用的完整配置。

```
Prefab: "MonsterCard"
  ├── RectTransform
  ├── Image (背景)
  ├── Text (名字)
  ├── Text (价格)
  └── Button (购买按钮)
```

### 8.2 Prefab Instance

从 Project 窗口把 Prefab 拖到 Scene 中，就创建了一个 **Prefab Instance**（预制体实例）。
- 实例默认继承 Prefab 的所有属性
- 可以 **Override**（覆盖）单个实例的属性
- 修改原始 Prefab 后，所有实例可以选择是否跟随更新

### 8.3 Resources 与动态加载

MC Fight 使用 `Resources/` 目录实现运行时动态加载：

```csharp
// MonsterDatabase.cs
public void LoadAll()
{
    // 从 Resources/Monsters/ 加载所有 MonsterDefSO 资产
    var loaded = Resources.LoadAll<MonsterDefSO>("Monsters");
    // ...
}
```

```csharp
// VFXSpawner.cs
// 从 Resources/VFX/ 加载粒子特效预制体
var prefab = Resources.Load<GameObject>("VFX/Hit/MeleeHit");
```

**Resources 目录的特点：**
- 放在这里的资源会被打包到最终构建中
- 可以用 `Resources.Load<T>(path)` 在运行时动态加载
- 适合小型项目；大型项目建议用 Addressables

---

## 第 9 章：资源管理

### 9.1 Project 窗口

Project 窗口就是你的文件浏览器。它对应磁盘上的 `Assets/` 目录。

**文件命名约定（Unity 特有）：**
```
Monsters/
├── Creeper.asset          ← 实际资源文件
├── Creeper.asset.meta     ← Unity 自动生成的元数据（GUID等）
```

`.meta` 文件非常重要——Unity 用它来追踪资源。**永远不要手动删除或重命名 .meta 文件！**

### 9.2 资源类型

| 扩展名 | 类型 | 示例 |
|--------|------|------|
| `.cs` | C# 脚本 | `GameManager.cs` |
| `.unity` | 场景文件 | `BattleScene.unity` |
| `.asset` | ScriptableObject | `Monster_Creeper.asset` |
| `.prefab` | 预制体 | `unitPrefab.prefab` |
| `.png/.jpg` | 纹理/精灵图 | 怪物图片 |
| `.mat` | 材质 | `BeamAdditive.mat` |
| `.shader` | 着色器 | `BeamAdditive.shader` |

### 9.3 资源导入设置

把图片拖入 Unity 后，Inspector 中可以设置：
- **Texture Type**：Sprite (2D and UI)、Default 等
- **Pixels Per Unit**：多少像素对应一个世界单位
- **Filter Mode**：Point (像素风)、Bilinear (平滑)
- **Compression**：None / Normal / High Quality

MC Fight 的怪物精灵图使用 Point 过滤（保持像素清晰），UI 素材可能用 Bilinear。

---

## 第 10 章：C# 在 Unity 中

### 10.1 MonoBehaviour 基础

所有 Unity 脚本都继承自 `MonoBehaviour`：

```csharp
using UnityEngine;

public class MyScript : MonoBehaviour
{
    // public 字段自动显示在 Inspector
    public float speed = 5f;
    public int health = 100;

    // private 字段不出现在 Inspector
    private bool isAlive = true;

    // [SerializeField] 让 private 字段也出现在 Inspector
    [SerializeField] private float attackRange = 42f;

    void Update()
    {
        if (isAlive)
            transform.Translate(Vector3.right * speed * Time.deltaTime);
    }
}
```

### 10.2 Inspector 序列化

Unity 会自动将 `public` 字段**序列化**到场景文件中：

```csharp
// 在 Inspector 中可以看到并编辑这些字段
public float hp = 100;           // 数字输入框
public string name = "Creeper";  // 文本输入框
public bool canFly = false;      // 复选框
public Sprite idleSprite;        // 拖拽槽
public string[] tags;            // 列表
```

### 10.3 [Header]、[Tooltip]、[TextArea]

```csharp
[Header("基本信息")]          // 在 Inspector 中显示分组标题
public string monsterId;

[Tooltip("怪物的显示名称")]   // 鼠标悬停时的提示文本
public string displayName;

[TextArea(3, 5)]              // 多行文本框（最小3行，最大5行）
public string description;
```

### 10.4 常用的 Unity API

```csharp
// 时间
Time.deltaTime          // 上一帧到这一帧的时间间隔
Time.time               // 游戏启动至今的总时间

// 数学
Mathf.Clamp(value, min, max)    // 限制值在范围内
Mathf.Max(a, b)                 // 取较大值
Vector2.Distance(a, b)          // 两点距离

// 输入
Input.GetMouseButtonDown(0)     // 鼠标左键按下
Input.mousePosition             // 鼠标屏幕坐标

// 对象查找
FindObjectOfType<T>()           // 查找场景中第一个 T 类型组件
GameObject.Find("名字")          // 按名字查找

// 实例化
Instantiate(prefab, position, rotation)  // 创建 Prefab 实例
Destroy(gameObject, delay)               // 延迟销毁
```

---

# 第二部分：核心系统深入

---

## 第 11 章：2D 物理系统

### 11.1 Rigidbody2D

`Rigidbody2D` 让对象受物理引擎驱动（重力、碰撞、力）。

**Body Type：**
- `Dynamic`：受力影响（角色、投射物）
- `Kinematic`：不受力，但可以移动（移动平台）
- `Static`：完全不动（地面、墙壁）

### 11.2 Collider2D

碰撞器定义对象的物理形状：
- `BoxCollider2D`：矩形
- `CircleCollider2D`：圆形
- `PolygonCollider2D`：多边形

**Is Trigger 模式：**
- 未勾选：物理碰撞（会被挡住）
- 勚选：触发器（可以穿过，但能检测重叠）

### 11.3 MC Fight 的特殊设计

MC Fight **没有使用 Unity 2D 物理系统**。碰撞分离完全在纯逻辑中实现：

```csharp
// MovementSystem.cs - 手动实现碰撞分离
public static void SeparateAllUnits(UnitList units, float dt)
{
    for (int i = 0; i < units.Count; i++)
    {
        for (int j = i + 1; j < units.Count; j++)
        {
            // 计算两单位距离
            float dx = units[j].X - units[i].X;
            float dy = units[j].Y - units[i].Y;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            float minDist = units[i].Radius + units[j].Radius;

            if (dist < minDist && dist > 0.01f)
            {
                // 推开彼此
                float pushForce = BattleConstants.SEPARATION_FORCE * dt;
                // 敌方之间推开力更大
                if (units[i].Team != units[j].Team)
                    pushForce *= BattleConstants.ENEMY_SEPARATION_MULT;
                // ...
            }
        }
    }
}
```

**为什么不用 Unity 物理？**
1. 模拟器需要完全脱离 Unity 运行（可测试、可网络同步）
2. 89 个字段的 UnitState 结构体比 MonoBehaviour 高效得多
3. 可以精确控制分离力的大小和行为

---

## 第 12 章：UGUI 界面系统

### 12.1 Canvas 是什么

`Canvas` 是所有 UI 元素的容器。它决定了 UI 的渲染方式：
- **Screen Space - Overlay**：UI 覆盖在游戏画面上（最常用）
- **Screen Space - Camera**：UI 通过特定摄像机渲染
- **World Space**：UI 在 3D 世界中（如角色头顶血条）

### 12.2 MC Fight 的 UI 结构

MC Fight 使用 **Screen Space - Overlay** 模式，所有 UI 面板都在同一个 Canvas 下：

```
Canvas (Screen Space - Overlay)
├── MainMenuPanel       ← 主菜单（PvP、PvAI、图鉴、退出）
├── ShopPanel           ← 商店（怪物卡片网格、购买按钮）
├── DeployPanel         ← 部署（点击战场放置单位）
├── BattlePanel         ← 战斗信息（计时器、存活数）
├── ResultPanel         ← 结算（详细统计数据）
└── CodexPanel          ← 图鉴（怪物百科）
```

### 12.3 面板切换机制

所有面板默认隐藏，通过 `SetActive(true/false)` 切换：

```csharp
// GameManager.cs
void HideAllUI()
{
    if (shopUI) shopUI.Hide();      // shopUI.gameObject.SetActive(false)
    if (deployUI) deployUI.Hide();
    if (battleUI) battleUI.Hide();
    if (resultUI) resultUI.Hide();
    if (mainMenuUI) mainMenuUI.Hide();
    if (codexUI) codexUI.Hide();
}

public void EnterMainMenu()
{
    Phase = GamePhase.MainMenu;
    HideAllUI();
    if (mainMenuUI != null) mainMenuUI.Show();  // mainMenuUI.gameObject.SetActive(true)
}
```

### 12.4 常用 UI 组件

| 组件 | 功能 | MC Fight 中的使用 |
|------|------|-------------------|
| `Panel` | 背景面板 | 所有 UI 面板的容器 |
| `Button` | 可点击按钮 | "购买"、"开始战斗"、"返回" |
| `Text` / `TextMeshPro` | 文本显示 | 怪物名字、价格、伤害数字 |
| `Image` | 图片显示 | 按钮背景、怪物卡片 |
| `ScrollRect` | 滚动区域 | 商店怪物列表 |
| `GridLayoutGroup` | 网格布局 | 商店卡片自动排列 |
| `HorizontalLayoutGroup` | 水平布局 | 按钮组 |

### 12.5 Layout 系统

`GridLayoutGroup` 让子对象自动排列成网格：

```csharp
// ShopUI 中的怪物卡片网格
// Inspector 设置：
// Cell Size: (120, 160)  ← 每张卡片大小
// Spacing: (10, 10)      ← 卡片间距
// Start Corner: Upper Left
// Start Axis: Horizontal
```

---

## 第 13 章：输入系统

### 13.1 传统 Input Manager

Unity 的传统输入系统使用 `Input` 类：

```csharp
// 检测鼠标点击
if (Input.GetMouseButtonDown(0))  // 0=左键, 1=右键, 2=中键
{
    Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    // 在世界坐标中处理点击
}
```

### 13.2 屏幕坐标 → 世界坐标

这是一个常见的初学者困惑点：

```
屏幕坐标系：左下角 (0,0)，右上角 (Screen.width, Screen.height)
                y (向上)
                ↑
                |
                +----→ x (向右)

世界坐标系：由摄像机决定
```

转换方法：
```csharp
Vector3 screenPos = Input.mousePosition;
Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
```

### 13.3 MC Fight 的部署点击

`DeployUI` 中的点击部署是输入系统的经典应用：

```csharp
// DeployUI.cs - 简化逻辑
void Update()
{
    if (Input.GetMouseButtonDown(0))
    {
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        GameManager.Instance.PlaceUnit(worldPos);
    }
}

// GameManager.cs
public void PlaceUnit(Vector2 worldPos)
{
    // 检查是否在合法区域
    bool onLeft = worldPos.x <= BattleConstants.FIELD_MID_X - 30f;
    bool onRight = worldPos.x >= BattleConstants.FIELD_MID_X + 30f;
    if (ActiveTeam == 0 && !onLeft) return;   // 蓝方只能放左边
    if (ActiveTeam == 1 && !onRight) return;  // 红方只能放右边

    // 从商店取出一个单位，记录部署位置
    int idx = ShopEntries.FindIndex(e => e.Team == ActiveTeam);
    var entry = ShopEntries[idx];
    ShopEntries.RemoveAt(idx);
    DeployedUnits.Add(new DeployedUnit { MonsterId = entry.MonsterId, Team = ActiveTeam, X = x, Y = y });
}
```

---

## 第 14 章：ScriptableObject

### 14.1 SO 是什么

`ScriptableObject` 是一种**纯数据容器**，专门用于存储配置数据。它与 MonoBehaviour 的区别：

| 特性 | MonoBehaviour | ScriptableObject |
|------|--------------|-----------------|
| 挂在 GameObject 上 | 是 | 否（作为独立 .asset 文件） |
| 有 Update 等生命周期 | 是 | 否 |
| 适合存储 | 运行时行为 | 静态配置数据 |
| 可被多个对象共享 | 否（每个对象一份） | 是（引用同一资产） |

### 14.2 MonsterDefSO：本项目的数据核心

MC Fight 的所有怪物定义都是 ScriptableObject：

```csharp
[CreateAssetMenu(fileName = "Monster_", menuName = "MC Fight/Monster Definition")]
public class MonsterDefSO : ScriptableObject
{
    [Header("基本信息")]
    public string monsterId;        // 唯一标识符
    public string displayName;      // 显示名称
    public int price;               // 购买价格
    [TextArea] public string description;

    [Header("战斗属性")]
    public float hp = 100;
    public float attack = 10;
    public float armor = 0;
    public float armorToughness = 0;
    public float moveSpeed = 58;
    public float attackRange = 42;
    public float attackInterval = 0.85f;
    public float radius = 18;
    public MoveType moveType = MoveType.Ground;
    public AttackType attackType = AttackType.Melee;

    [Header("标签")]
    public string[] tags;  // 如 "boss", "fly", "explosive"

    [Header("命中附带状态")]
    public StatusEffectType[] onHitEffects;

    [Header("技能配置")]
    public string abilityComponentType;  // 如 "WardenAbility"

    [Header("精灵图")]
    public Sprite idleSprite;
    public Sprite attackSprite;
    public Sprite deadSprite;
}
```

### 14.3 创建 SO 资产

**方法一：编辑器手动创建**
1. 在 Project 窗口右键 → Create → MC Fight → Monster Definition
2. 在 Inspector 中配置所有字段
3. 保存为 `Monster_Creeper.asset`

**方法二：代码批量生成**（MC Fight 的做法）

```csharp
// Editor/MonsterDataGenerator.cs
[MenuItem("MC Fight/Generate All Monster Data")]
static void GenerateAll()
{
    // 定义所有 84 个怪物的数据
    var monsters = new[]
    {
        new { id = "creeper", name = "苦力怕", price = 20, hp = 20f, attack = 40f, ... },
        new { id = "skeleton", name = "骷髅", price = 8, hp = 20f, attack = 4f, ... },
        // ... 84 个怪物
    };

    foreach (var m in monsters)
    {
        var so = ScriptableObject.CreateInstance<MonsterDefSO>();
        so.monsterId = m.id;
        so.displayName = m.name;
        // ... 设置所有字段
        AssetDatabase.CreateAsset(so, $"Assets/Resources/Monsters/Monster_{m.id}.asset");
    }
}
```

### 14.4 加载 SO 资产

```csharp
// 方法一：Resources.Load（按路径）
MonsterDefSO def = Resources.Load<MonsterDefSO>("Monsters/Monster_creeper");

// 方法二：Resources.LoadAll（加载整个目录）
MonsterDefSO[] allMonsters = Resources.LoadAll<MonsterDefSO>("Monsters");
```

---

## 第 15 章：动画系统

### 15.1 Animator 与 Animation Clip

Unity 的动画系统由两部分组成：
- **Animation Clip**：一段动画（如"挥剑"、"死亡"）
- **Animator Controller**：状态机，控制何时播放哪段动画

### 15.2 MC Fight 的简化动画

MC Fight 没有使用 Animator 状态机，而是在代码中直接控制精灵图切换和视觉效果：

```csharp
// UnitView.cs
public class UnitView : MonoBehaviour
{
    private SpriteRenderer _spriteRenderer;

    public void SyncFromState(ref UnitState state)
    {
        // 根据状态切换精灵图
        if (state.AttackAnimTimer > 0)
            _spriteRenderer.sprite = _attackSprite;  // 攻击态
        else
            _spriteRenderer.sprite = _idleSprite;    // 待机态

        // 根据朝向翻转
        _spriteRenderer.flipX = state.Facing < 0;
    }

    public void PlayDeath()
    {
        // 死亡动画：渐隐 + 下沉
        _spriteRenderer.sprite = _deadSprite;
        StartCoroutine(DeathAnimation());
    }

    System.Collections.IEnumerator DeathAnimation()
    {
        float duration = 0.5f;
        float elapsed = 0;
        Color originalColor = _spriteRenderer.color;
        Vector3 originalPos = transform.position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 渐隐
            _spriteRenderer.color = new Color(
                originalColor.r, originalColor.g, originalColor.b, 1 - t);

            // 下沉
            transform.position = originalPos + Vector3.down * t * 20f;

            yield return null;
        }

        Destroy(gameObject);
    }
}
```

### 15.3 协程（Coroutine）

`StartCoroutine()` 是 Unity 中处理异步/延时操作的方式：

```csharp
IEnumerator MyCoroutine()
{
    Debug.Log("开始");
    yield return new WaitForSeconds(1f);  // 等待1秒
    Debug.Log("1秒后");
    yield return null;                     // 等待一帧
    Debug.Log("下一帧");
}
```

---

## 第 16 章：粒子系统

### 16.1 ParticleSystem 基础

`ParticleSystem` 是 Unity 内置的粒子特效系统。它由多个模块组成：
- **Emission**：发射速率
- **Shape**：发射形状（圆形、锥形等）
- **Size over Lifetime**：大小随时间变化
- **Color over Lifetime**：颜色随时间变化
- **Renderer**：渲染方式（Billboard、Mesh等）

### 16.2 Cartoon FX Remaster

MC Fight 使用了 Asset Store 的 **Cartoon FX Remaster (CFXR)** 插件，提供了大量预制的粒子特效：
- 命中特效（Hit effects）
- 爆炸特效（Explosions）
- 冰/火/雷特效
- 天空光束（Sky rays）
- 漩涡（Vortex）

### 16.3 VFXSpawner：统一管理

MC Fight 创建了 `VFXSpawner` 静态类来统一管理特效生成：

```csharp
// VFXSpawner.cs - 简化示例
public static class VFXSpawner
{
    // 从 Resources 加载预制体（懒加载，首次使用时加载）
    private static GameObject meleeHitPrefab;
    private static GameObject explosionPrefab;

    static void EnsureLoaded()
    {
        if (meleeHitPrefab == null)
            meleeHitPrefab = Resources.Load<GameObject>("VFX/Hit/MeleeHit");
        if (explosionPrefab == null)
            explosionPrefab = Resources.Load<GameObject>("VFX/Explosion/Explosion");
    }

    public static void SpawnMeleeHit(Vector3 position, float damage)
    {
        EnsureLoaded();
        if (meleeHitPrefab == null) return;

        var go = Object.Instantiate(meleeHitPrefab, position, Quaternion.identity);
        // 2D 粒子需要旋转到 XY 平面
        go.transform.rotation = Quaternion.Euler(90, 0, 0);
        // 根据伤害调整粒子大小
        float scale = Mathf.Clamp(damage / 30f, 0.3f, 2f);
        go.transform.localScale = Vector3.one * scale;
        // 销毁残留粒子
        Object.Destroy(go, 2f);
    }
}
```

---

## 第 17 章：事件与消息系统

### 17.1 为什么需要事件

假设伤害系统需要通知 UI 显示伤害数字。最笨的做法是：

```csharp
// 不好的做法：硬编码依赖
void DealDamage(UnitState target, float damage)
{
    target.Hp -= damage;
    BattleUI.ShowDamageNumber(target.X, target.Y, damage);  // 直接引用 UI！
    VFXSpawner.SpawnMeleeHit(...);  // 直接引用 VFX！
}
```

问题：伤害系统直接依赖了 UI 和 VFX，耦合太紧。

### 17.2 C# 事件（Event）

MC Fight 使用 C# 原生的 `event` 和 `delegate` 实现事件总线：

```csharp
// DamageSystem.cs
public struct DamageEvent
{
    public int AttackerId;
    public int TargetId;
    public float Damage;
    public DamageCategory Category;
    public bool IsDot;
    public float X, Y;  // 伤害位置
}

public static class DamageEvents
{
    // 事件委托
    public delegate void DamageHandler(DamageEvent evt);
    // 事件声明
    public static event DamageHandler OnDamage;
    // 触发事件
    public static void Raise(DamageEvent evt) => OnDamage?.Invoke(evt);
}
```

### 17.3 事件的订阅与使用

```csharp
// BattleBridge.cs - 订阅伤害事件，生成伤害数字和特效
void OnEnable()
{
    DamageEvents.OnDamage += OnDamageNumber;
}

void OnDisable()
{
    DamageEvents.OnDamage -= OnDamageNumber;
}

void OnDamageNumber(DamageEvent evt)
{
    if (evt.Damage <= 0) return;
    // 生成伤害数字
    SpawnDamageNumber(evt.Damage, evt.Category, evt.X, evt.Y);
    // 播放特效
    VFXSpawner.SpawnMeleeHit(new Vector3(evt.X, evt.Y, 0), evt.Damage);
}

// BattleStatsCollector.cs - 订阅伤害事件，收集统计
void OnDamageDealt(int attackerId, int targetId, float damage, DamageCategory cat, bool isDot, UnitList units)
{
    // 记录伤害数据...
}
```

### 17.4 事件流图

```
DamageSystem.CalculateDamage()
    │
    ├── DamageEvents.Raise(evt)  ← 触发事件
    │       │
    │       ├── BattleBridge.OnDamageNumber()   → 生成伤害数字 + VFX
    │       │
    │       └── StatsCollector.OnDamageDealt()  → 记录统计数据
    │
    └── target.Hp -= damage
```

**关键点：** DamageSystem 不知道谁在监听，也不关心。这就是**松耦合**。

---

# 第三部分：MC Fight 项目实战解读

---

## 第 18 章：项目架构总览

### 18.1 分层架构

MC Fight 采用了清晰的分层设计：

```
┌─────────────────────────────────────────────┐
│                   UI 层                      │
│   ShopUI / DeployUI / BattleUI / ResultUI   │
├─────────────────────────────────────────────┤
│               渲染桥接层                      │
│              BattleBridge                    │
├─────────────────────────────────────────────┤
│               模拟层（纯 C#）                 │
│  BattleSimulator + 各种 System + Abilities  │
├─────────────────────────────────────────────┤
│               数据层                          │
│      MonsterDefSO + MonsterDatabase         │
└─────────────────────────────────────────────┘
```

### 18.2 核心设计原则

**1. 模拟与渲染完全分离**

`BattleSimulator` 是一个**纯 C# 类**，不继承 MonoBehaviour，不引用任何 Unity API（除了 `Mathf` 等数学工具）。这意味着：
- 可以在 Edit Mode 单元测试中运行
- 可以做无头批量平衡测试
- 可以轻松移植到服务器实现权威网络同步

**2. 数据驱动**

所有怪物数据存储在 ScriptableObject 中，通过字符串 key 关联技能组件：
```csharp
// MonsterDefSO.abilityComponentType = "WardenAbility"
// AbilityFactory 根据字符串创建对应的技能类
IAbilityComponent ability = AbilityFactory.Create("WardenAbility", def);
```

**3. 事件总线解耦**

伤害计算、统计收集、视觉反馈通过事件总线连接，互不直接依赖。

### 18.3 文件结构总览

```
Scripts/
├── Core/
│   ├── Enums.cs              ← 所有枚举定义
│   ├── MonsterDefSO.cs       ← 怪物数据定义（ScriptableObject）
│   └── Constants.cs          ← 全局常量
├── Data/
│   └── MonsterDatabase.cs    ← 运行时怪物数据库索引
├── GameFlow/
│   └── GameManager.cs        ← 游戏状态机
├── Simulation/
│   ├── BattleSimulator.cs    ← 战斗模拟核心
│   ├── BattleState.cs        ← 所有数据结构
│   ├── DamageSystem.cs       ← 伤害计算
│   ├── MovementSystem.cs     ← 移动与碰撞分离
│   ├── StatusEffectSystem.cs ← 状态效果
│   ├── TargetingSystem.cs    ← 目标选择
│   ├── ProjectileSystem.cs   ← 弹道系统
│   ├── AreaEffectSystem.cs   ← 区域效果
│   ├── SkillStateMap.cs      ← 手写 KV 存储
│   ├── BattleStatsCollector.cs ← 战斗统计
│   └── Abilities/            ← 50+ 技能实现
│       ├── IAbilityComponent.cs
│       ├── AbilityFactory.cs
│       ├── GenericAbilities.cs
│       ├── Batch1Abilities.cs
│       ├── Batch2Abilities.cs
│       └── Batch3Abilities.cs
├── UI/
│   ├── MainMenuUI.cs
│   ├── ShopUI.cs
│   ├── DeployUI.cs
│   ├── BattleUI.cs
│   ├── ResultUI.cs
│   └── CodexUI.cs
├── View/
│   ├── BattleBridge.cs       ← 渲染桥接器
│   ├── UnitView.cs           ← 单位视图
│   ├── BattleEffectViews.cs  ← 投射物/效果/光束/伤害数字视图
│   ├── BattleFieldRenderer.cs ← 战场背景渲染
│   ├── AttackRangeView.cs    ← 攻击范围指示器
│   └── VFXSpawner.cs         ← 粒子特效管理
└── Editor/
    └── MonsterDataGenerator.cs ← 编辑器工具：批量生成怪物数据
```

---

## 第 19 章：数据层 —— MonsterDefSO

### 19.1 字段设计思路

每个怪物需要定义的属性：

| 类别 | 字段 | 说明 |
|------|------|------|
| 身份 | `monsterId` | 唯一 ID（如 "creeper"） |
| 身份 | `displayName` | 显示名（如 "苦力怕"） |
| 身份 | `price` | 购买价格（5-1000G） |
| 身份 | `description` | 描述文本 |
| 战斗 | `hp` | 生命值 |
| 战斗 | `attack` | 攻击力 |
| 战斗 | `armor` | 护甲值 |
| 战斗 | `armorToughness` | 护甲韧性 |
| 战斗 | `moveSpeed` | 移动速度 |
| 战斗 | `attackRange` | 攻击范围 |
| 战斗 | `attackInterval` | 攻击间隔（秒） |
| 战斗 | `radius` | 碰撞半径 |
| 类型 | `moveType` | Ground / Fly |
| 类型 | `attackType` | Melee / Ranged |
| 标签 | `tags` | 字符串数组 |
| 状态 | `onHitEffects` | 命中附带的效果 |
| 技能 | `abilityComponentType` | 技能类名 |
| 视觉 | `idleSprite / attackSprite / deadSprite` | 精灵图 |

### 19.2 标签系统

`tags` 是一个灵活的标记系统，用于实现各种特殊行为：

```csharp
// 在代码中检查标签
if (unit.HasTag("explosive"))   // 苦力怕：死亡时爆炸
if (unit.HasTag("fly"))         // 飞行单位：地面近战无法直接攻击
if (unit.HasTag("boss"))        // Boss 单位：更大的显示尺寸
if (unit.HasTag("arthropod"))   // 节肢动物：对飞行节肢有额外伤害
if (unit.HasTag("fire_immune")) // 火焰免疫：不受燃烧伤害
if (unit.HasTag("giant"))       // 巨型单位：最大的显示尺寸
```

### 19.3 技能组件关联

`abilityComponentType` 字符串通过 `AbilityFactory` 映射到具体的技能类：

```csharp
// AbilityFactory.cs
public static IAbilityComponent Create(string typeName, MonsterDefSO def)
{
    return typeName switch
    {
        "MeleeAbility" => new MeleeAbility(),
        "RangedAbility" => new RangedAbility(),
        "ExplosiveAbility" => new ExplosiveAbility(...),
        "WardenAbility" => new WardenAbility(def),
        "TremorzillaAbility" => new TremorzillaAbility(def),
        // ... 50+ 映射
        _ => null
    };
}
```

---

## 第 20 章：怪物数据库

### 20.1 MonsterDatabase 的作用

`MonsterDatabase` 在运行时加载所有 `MonsterDefSO` 资产，提供快速查询：

```csharp
public class MonsterDatabase
{
    private Dictionary<string, MonsterDefSO> _byId = new();
    private MonsterDefSO[] _sortedByPrice;

    public int Count => _byId.Count;

    public void LoadAll()
    {
        // 从 Resources/Monsters/ 加载所有 SO
        var loaded = Resources.LoadAll<MonsterDefSO>("Monsters");
        foreach (var def in loaded)
            _byId[def.monsterId] = def;

        // 按价格排序（从高到低）
        _sortedByPrice = loaded.OrderByDescending(m => m.price).ToArray();
    }

    public MonsterDefSO GetById(string id)
    {
        _byId.TryGetValue(id, out var def);
        return def;
    }

    public MonsterDefSO[] GetAllSortedByPrice() => _sortedByPrice;
}
```

### 20.2 为什么用 Resources.LoadAll

- 不需要手动指定路径，自动加载目录下所有 SO
- 代码简洁，一行搞定
- 缺点：无法精细控制加载时机（全部一次加载）

---

## 第 21 章：游戏流程管理

### 21.1 状态机模式

GameManager 是一个经典的**有限状态机（FSM）**：

```
MainMenu ──→ Shop ──→ Deploy ──→ Battle ──→ Result
                ↑                              │
                └──────────────────────────────┘
                     (重新开始)
```

### 21.2 Phase 枚举

```csharp
public enum GamePhase { MainMenu, Shop, Deploy, Battle, Result, Codex }
```

每个 Phase 对应：
- 一个可见的 UI 面板
- 一组允许的操作
- 特定的游戏逻辑

### 21.3 状态转换

```csharp
// 从主菜单进入商店
public void StartPvP()
{
    Mode = GameMode.PvP;
    EnterShop();  // → Phase = Shop, 显示 ShopPanel
}

// 从商店进入部署
public void StartDeploy()
{
    if (!CanStartDeploy()) return;
    Phase = GamePhase.Deploy;
    HideAllUI();
    if (deployUI) deployUI.Show();
}

// 从部署进入战斗
public void StartBattle()
{
    if (DeployedUnits.Count == 0) return;
    Phase = GamePhase.Battle;
    HideAllUI();
    if (battleUI) battleUI.Show();
    BattleBridge.StartBattle(DeployedUnits, Database);
}

// 战斗结束进入结算
public void OnBattleEnd(int winner)
{
    Phase = GamePhase.Result;
    Winner = winner;
    StatsCollector.UpdateFinalStats(...);
    HideAllUI();
    if (resultUI != null) resultUI.Show(winner);
}
```

### 21.4 PvAI 模式

```csharp
public void StartPvAI()
{
    Mode = GameMode.PvAI;
    EnterShop();
}

// 进入部署时，AI 自动购买红方
public void StartDeploy()
{
    if (Mode == GameMode.PvAI && Gold[1] > 0)
        AIBuyTeam(1);  // AI 随机购买怪物
    // ...
}

void AIBuyTeam(int team)
{
    var rng = new System.Random(System.DateTime.Now.Millisecond);
    while (Gold[team] > 0)
    {
        var affordable = monsters.FindAll(m => m.price > 0 && m.price <= Gold[team]);
        if (affordable.Count == 0) break;
        var pick = affordable[rng.Next(affordable.Count)];
        Gold[team] -= pick.price;
        ShopEntries.Add(new ShopEntry { MonsterId = pick.monsterId, Team = team });
    }
}
```

---

## 第 22 章：战斗模拟器

### 22.1 为什么用纯 C# 类

```csharp
public class BattleSimulator  // 注意：没有继承 MonoBehaviour！
{
    private BattleState _state;

    public void Initialize(List<DeployedUnit> deployments, MonsterDatabase database, int seed)
    {
        // 初始化战斗状态...
    }

    public void Tick(float dt)
    {
        // 推进一帧模拟...
    }
}
```

**优势：**
1. 不依赖 Unity 运行时 → 可以在单元测试中直接调用
2. 没有 GameObject 的开销 → 更高效
3. 状态完全可序列化 → 可以保存/恢复/回放
4. 可以在编辑器模式下批量运行 → 快速平衡测试

### 22.2 5 阶段 Tick 循环

每一帧（1/60s）执行一次 `Tick()`，内部按 5 个阶段处理：

```
Phase A: 全局效果更新
  ├── AreaEffectSystem.Tick()   ← 地面区域效果（岩浆、冰冻等）
  └── ProjectileSystem.Tick()   ← 投射物飞行与碰撞

Phase B: 单位循环（对每个存活单位）
  ├── B.1 状态效果 tick        ← 中毒/燃烧/凋零/减速/恐惧/冰冻/眩晕
  ├── B.2 递减冷却             ← 攻击冷却、技能冷却、重选目标计时器
  ├── B.3 检查施法中           ← 如果正在施法，继续施法
  ├── B.4 恐惧 → 随机游走
  ├── B.5 重选目标             ← 寻找最近敌人
  ├── B.6 施法 tick
  ├── B.7 无目标 → 游走
  ├── B.8 尝试释放技能         ← 技能系统
  └── B.9 标准战斗逻辑         ← 追击/攻击/等待冷却

Phase C: 后处理
  ├── SeparateAllUnits()       ← 碰撞分离
  └── ClampToField()           ← 限制在战场范围内

Phase D: 胜负判定
  └── CheckWinner()            ← 检查是否一方全灭

Phase E: 时间推进
  └── ElapsedTime += dt        ← 120 秒超时判定
```

### 22.3 固定步长 vs 可变步长

```csharp
// BattleBridge.Update() - 外部驱动
void Update()
{
    _accumulatedTime += Time.deltaTime;
    int maxSteps = 3;  // 防止卡顿后追帧过多

    while (_accumulatedTime >= BattleConstants.TICK_DT && maxSteps > 0)
    {
        Simulator.Tick(BattleConstants.TICK_DT);  // 始终传入固定值 1/60s
        _accumulatedTime -= BattleConstants.TICK_DT;
        maxSteps--;
    }
}
```

**固定步长的好处：** 模拟结果与帧率无关，60fps 和 30fps 下战斗结果完全一致。

---

## 第 23 章：战斗状态数据

### 23.1 UnitState 结构体

`UnitState` 是整个模拟器中最核心的数据结构，包含 **89 个字段**：

```csharp
public struct UnitState  // 结构体，非类！
{
    // 身份
    public int Id;
    public int Team;          // 0=蓝方, 1=红方
    public string MonsterId;

    // 位置
    public float X, Y;
    public float Facing;      // 1=朝右, -1=朝左

    // 属性（运行时可被技能修改）
    public float Hp, MaxHp;
    public float Attack;
    public float Armor, ArmorToughness;
    public float MoveSpeed, BaseMoveSpeed;
    public float AttackRange;
    public float AttackInterval, BaseAttackInterval;
    public float Radius;

    // 状态
    public UnitStateEnum State;  // Idle/Chase/Attack/Dead
    public MoveType MoveType;
    public AttackType AttackType;

    // 冷却
    public float AttackCooldown;
    public float AttackAnimTimer;
    public float SkillCooldown;
    public float RetargetTimer;
    public float VulnerableWindow;  // 飞行单位的脆弱窗口

    // 目标
    public int TargetId;

    // 技能状态
    public SkillStateMap SkillState;
    public StatusEffectList StatusEffects;

    // 特殊
    public string[] Tags;
    public int RiderUnitId;    // 骑手（蜘蛛骑士）
    public int MountUnitId;    // 坐骑
    public float DriftAngle;   // 游走角度
    public float DriftTimer;
}
```

### 23.2 为什么用 struct 而不是 class

```csharp
public struct UnitState  // 值类型
{
    // ...
}
```

| 特性 | struct（值类型） | class（引用类型） |
|------|-----------------|-------------------|
| 内存分配 | 栈上（或数组内联） | 堆上 |
| GC 压力 | 无 | 有 |
| 复制行为 | 复制整个值 | 复制引用 |
| 适合场景 | 小型、频繁创建的数据 | 复杂对象、需要继承 |

UnitState 作为 struct 存储在 `UnitList` 的数组中，避免了大量堆分配和 GC 回收。

### 23.3 UnitList：自定义容器

```csharp
public struct UnitList
{
    private UnitState[] _data;
    public int Count;

    public UnitList(int capacity)
    {
        _data = new UnitState[capacity];  // 预分配 256 个槽位
        Count = 0;
    }

    // ref 返回：避免 struct 装箱，允许直接修改
    public ref UnitState this[int index] => ref _data[index];

    public void Add(UnitState unit)
    {
        _data[Count++] = unit;
    }
}
```

**`ref` 返回值**是关键优化——避免了每次索引时 struct 被复制到栈上。

### 23.4 SkillStateMap：手写 KV 存储

```csharp
// SkillStateMap.cs - 手动展开的 32 槽 KV 存储
// 避免 Dictionary 的 GC 分配
public struct SkillStateMap
{
    // 32 个显式字段
    public float SkillFloat0, SkillFloat1, ... SkillFloat15;
    public int SkillInt0, SkillInt1, ... SkillInt7;
    public bool SkillBool0, SkillBool1, ... SkillBool7;

    public float GetFloat(int key)
    {
        return key switch
        {
            0 => SkillFloat0,
            1 => SkillFloat1,
            2 => SkillFloat2,
            // ... 完全内联
            _ => 0f
        };
    }
}
```

---

## 第 24 章：伤害与护甲系统

### 24.1 MC 风格护甲公式

MC Fight 使用了 Minecraft 的护甲减伤公式：

```csharp
// DamageSystem.cs
public static float CalculateDamage(float rawDamage, float armor, float toughness, DamageCategory category)
{
    // 真实伤害无视护甲
    if (category == DamageCategory.True) return rawDamage;

    // MC 护甲公式
    float g = Mathf.Min(20f, Mathf.Max(
        armor / 5f,                          // 无韧性
        armor - 4f * rawDamage / (toughness + 8f)  // 有韧性
    ));

    return rawDamage * (1f - g / 25f);
}
```

**公式解读：**
- `armor/5`：低伤害时的减伤下限
- `armor - 4*damage/(toughness+8)`：高伤害时考虑韧性的减伤
- `g` 被限制在 0-20 之间
- 最终减伤比例 = `g/25`（最多 80% 减伤）

### 24.2 5 种伤害类型

| 类型 | 无视护甲 | 特殊规则 |
|------|---------|---------|
| `Melee` | 否 | 地面近战无法直接攻击飞行单位 |
| `Ranged` | 否 | 可以攻击飞行单位 |
| `Beam` | 否 | 光束攻击 |
| `Explosion` | 否 | 爆炸伤害，有半径衰减 |
| `True` | **是** | 无视护甲和韧性 |

### 24.3 特殊免疫机制

```csharp
// DamageSystem.cs
public static bool IsImmune(ref UnitState attacker, ref UnitState target, DamageCategory category)
{
    // 螃蟹钻地状态免疫
    if (target.SkillState.GetBool(SkillKeys.CrabBurrowed)) return true;

    // 亡魂防御状态免疫远程
    if (target.SkillState.GetBool(SkillKeys.RevenantDefending) && category == DamageCategory.Ranged)
        return true;

    // 巨魔免疫远程
    if (target.HasTag("troll") && category == DamageCategory.Ranged)
        return true;

    // 科博尔远程格挡
    if (target.SkillState.GetBool(SkillKeys.KoboBlock) && category == DamageCategory.Ranged)
        return true;

    return false;
}
```

---

## 第 25 章：状态效果系统

### 25.1 7 种状态效果

| 效果 | DPS | 持续 | 特殊 |
|------|-----|------|------|
| `Poison` 中毒 | 2 | 5s | - |
| `Burn` 燃烧 | 1 | 10s | 传播给 52 范围内的友军 |
| `Wither` 凋零 | 3 | 4s | - |
| `Slow` 减速 | - | 5s | 移速 ×0.7 |
| `Fear` 恐惧 | - | 2s | 随机游走 |
| `Freeze` 冰冻 | - | 2s | 移速=0，攻击间隔最大 |
| `Stun` 眩晕 | - | 30s | 移速=0，强制地面 |

### 25.2 StatusEffectList 数据结构

```csharp
public struct StatusEffectList
{
    // 固定 8 个槽位，最多同时 8 种效果
    private StatusEffectType[] _types;
    private float[] _timers;

    public void Apply(StatusEffectType type, float duration)
    {
        // 刷新已有效果的持续时间，或添加新效果
    }

    public bool Has(StatusEffectType type)
    {
        for (int i = 0; i < _types.Length; i++)
            if (_types[i] == type && _timers[i] > 0) return true;
        return false;
    }

    public void Tick(float dt)
    {
        // 递减所有效果计时器
    }
}
```

### 25.3 燃烧传播

```csharp
// StatusEffectSystem.cs
public static void SpreadBurn(int burnedIndex, UnitList units, float dt)
{
    ref var burned = ref units[burnedIndex];
    float bx = burned.X, by = burned.Y;

    for (int i = 0; i < units.Count; i++)
    {
        if (i == burnedIndex) continue;
        ref var other = ref units[i];
        if (other.Team != burned.Team) continue;  // 只传播给友军
        if (other.State == UnitStateEnum.Dead) continue;
        if (other.HasTag("fire_immune")) continue;

        float dist = Distance(bx, by, other.X, other.Y);
        if (dist <= BattleConstants.BURN_SPREAD_RADIUS)
        {
            other.StatusEffects.Apply(StatusEffectType.Burn, 10f);
        }
    }
}
```

---

## 第 26 章：技能系统

### 26.1 IAbilityComponent 接口

所有技能都实现同一个接口：

```csharp
public interface IAbilityComponent
{
    // 初始化（注册到单位时调用）
    void OnInit(ref UnitState unit);

    // 尝试执行技能（返回 true 表示已执行）
    bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt);

    // 施法中 tick（持续性技能）
    void TickCast(ref UnitState unit, BattleState state, float dt);

    // 交战范围（决定何时开始追击）
    float GetEngageRange(ref UnitState unit);

    // 是否正在施法
    bool IsBusy(ref UnitState unit);

    // 是否允许对空攻击
    bool AllowAntiAir(ref UnitState unit);
}
```

### 26.2 通用技能实现

**近战技能：**
```csharp
public class MeleeAbility : IAbilityComponent
{
    public void OnInit(ref UnitState unit) { }

    public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
    {
        if (unit.AttackCooldown > 0) return false;
        if (dist > unit.AttackRange) return false;

        // 执行近战攻击
        unit.AttackCooldown = unit.AttackInterval;
        unit.AttackAnimTimer = BattleConstants.MELEE_ANIM_TIME;

        DamageSystem.DealDamage(ref unit, ref state.Units[targetIdx],
            unit.Attack, DamageCategory.Melee, state);

        return true;
    }
    // ...
}
```

**爆炸技能（苦力怕）：**
```csharp
public class ExplosiveAbility : IAbilityComponent
{
    private float _radius;
    private float _fuseTime;
    private float _damage;

    public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
    {
        if (unit.AttackCooldown > 0) return false;
        if (dist > unit.AttackRange) return false;

        // 开始引信计时
        unit.SkillState.SetBool(SkillKeys.FuseActive, true);
        unit.SkillState.SetFloat(SkillKeys.FuseTimer, _fuseTime);
        unit.AttackCooldown = unit.AttackInterval;
        return true;
    }

    public void TickCast(ref UnitState unit, BattleState state, float dt)
    {
        float timer = unit.SkillState.GetFloat(SkillKeys.FuseTimer) - dt;
        unit.SkillState.SetFloat(SkillKeys.FuseTimer, timer);

        if (timer <= 0)
        {
            // 爆炸！对范围内所有敌人造成伤害
            for (int i = 0; i < state.Units.Count; i++)
            {
                ref var target = ref state.Units[i];
                if (target.Team == unit.Team || target.State == UnitStateEnum.Dead) continue;

                float dist = Distance(unit.X, unit.Y, target.X, target.Y);
                if (dist <= _radius)
                {
                    // 半径衰减：中心满伤害，边缘 25%
                    float falloff = 1f - (dist / _radius) * 0.75f;
                    DamageSystem.DealDamage(ref unit, ref target,
                        _damage * falloff, DamageCategory.Explosion, state);
                }
            }

            // 死亡
            unit.Hp = 0;
            unit.State = UnitStateEnum.Dead;
        }
    }
}
```

### 26.3 Boss 技能示例：监守者（Warden）

```csharp
public class WardenAbility : IAbilityComponent
{
    private float _attackTimer;
    private float _sonicCooldown;

    public bool TryExecute(ref UnitState unit, int targetIdx, float dist, BattleState state, float dt)
    {
        if (unit.AttackCooldown > 0) return false;

        // 近战攻击：30 伤害
        if (dist <= unit.AttackRange)
        {
            unit.AttackCooldown = unit.AttackInterval;
            unit.AttackAnimTimer = 0.3f;
            DamageSystem.DealDamage(ref unit, ref state.Units[targetIdx],
                30f, DamageCategory.Melee, state);
            return true;
        }

        // 声波冲击波（10秒冷却）
        if (_sonicCooldown <= 0 && dist <= 150f)
        {
            _sonicCooldown = 10f;
            // 创建冲击波，对范围内所有敌人造成 15 伤害
            AreaEffectSystem.CreateShockwave(unit.X, unit.Y, 120f, 15f, unit.Team, state);
            return true;
        }

        return false;
    }
}
```

---

## 第 27 章：弹道与区域效果

### 27.1 弹道系统

MC Fight 有 8 种弹道类型：

| 类型 | 行为 |
|------|------|
| `Default` | 直线飞行，命中后消失 |
| `HarbWither` | 直线飞行，命中施加凋零 |
| `HarbHoming` | 追踪目标 |
| `HarbLaser` | 光束，即时命中 |
| `RevenantBone` | 骨骼弹道 |
| `ForsakenSonic` | 穿透弹道（穿透多个目标） |
| `IceBomb` | 冰弹，命中创建冰冻区域 |
| `ProwlerMissile` | 追踪导弹 |

### 27.2 ProjectileData 结构

```csharp
public struct ProjectileData
{
    public int Id;
    public ProjectileKind Kind;
    public float X, Y;
    public float VX, VY;        // 速度分量
    public int TargetId;         // 追踪目标
    public int OwnerTeam;
    public float Damage;
    public StatusEffectType OnHitEffect;  // 命中附加效果
    public bool HasHit;          // 是否已命中
    public bool Pierce;          // 是否穿透
}
```

### 27.3 区域效果系统

11 种区域效果类型：

```csharp
public enum AreaEffectType
{
    LavaPatch,           // 岩浆地面：持续燃烧
    FrostZone,           // 冰冻区域：持续减速
    PollutionZone,       // 污染区域：中毒+减速
    SandTornado,         // 沙尘暴：击飞
    LinearTornado,       // 直线龙卷风
    VoidRune,            // 虚空符文
    Shockwave,           // 冲击波：即时范围伤害
    Meteor,              // 陨石：延迟范围伤害
    ObeliskBarrage,      // 方尖碑弹幕
    FallingObelisk,      // 坠落方尖碑
    ConeStrike,          // 锥形打击
    ArcWave,             // 弧形波
}
```

### 27.4 AreaEffectData 结构

```csharp
public struct AreaEffectData
{
    public int Id;
    public AreaEffectType Type;
    public float X, Y;
    public float Radius;
    public float Duration;      // 剩余持续时间
    public float DamagePerTick; // 每 tick 伤害
    public int OwnerTeam;
    public StatusEffectType AppliedEffect;  // 附加的状态效果
}
```

---

## 第 28 章：AI 与目标选择

### 28.1 TargetingSystem 的核心逻辑

```csharp
public static int PickTarget(ref UnitState unit, UnitList units, bool forceRetarget, IAbilityComponent ability)
{
    // 如果有当前目标且未到重选时间，保持粘性
    if (!forceRetarget && unit.TargetId >= 0)
    {
        int idx = GetTargetIndex(units, unit.TargetId);
        if (idx >= 0 && units[idx].State != UnitStateEnum.Dead)
        {
            float dist = Distance(unit.X, unit.Y, units[idx].X, units[idx].Y);
            // 在粘性范围内保持当前目标
            if (dist <= unit.AttackRange + BattleConstants.STICKY_RANGE_BONUS)
                return unit.TargetId;
        }
    }

    // 寻找最近的敌人
    int bestIdx = -1;
    float bestDist = float.MaxValue;

    for (int i = 0; i < units.Count; i++)
    {
        ref var other = ref units[i];
        if (other.Team == unit.Team) continue;       // 同队跳过
        if (other.State == UnitStateEnum.Dead) continue;  // 死亡跳过

        // 对空检查
        if (!CanTargetForAttack(ref unit, ref other, ability?.AllowAntiAir(ref unit) ?? false))
            continue;

        float dist = Distance(unit.X, unit.Y, other.X, other.Y);

        // 反节肢偏见：对飞行节肢类敌人，距离视为 0.75 倍
        if (unit.HasTag("arthropod") && other.MoveType == MoveType.Fly && other.HasTag("arthropod"))
            dist *= BattleConstants.ANTI_ARTHROPOD_BIAS;

        if (dist < bestDist)
        {
            bestDist = dist;
            bestIdx = i;
        }
    }

    return bestIdx >= 0 ? units[bestIdx].Id : -1;
}
```

### 28.2 粘性锁定

目标选择不是每帧重新计算，而是有**粘性**：
- 默认锁定 2.5 秒
- 在锁定期内，即使有更近的敌人也不切换
- 防止单位频繁切换目标导致"抖动"

### 28.3 反飞行逻辑

地面近战单位**不能直接攻击飞行单位**，除非飞行单位进入"脆弱窗口"：

```csharp
// 飞行单位俯冲时会短暂进入脆弱窗口
if (unit.MoveType == MoveType.Ground && target.MoveType == MoveType.Fly)
{
    if (target.VulnerableWindow <= 0)
        return false;  // 无法攻击飞行单位
    // 脆弱窗口内可以攻击
}
```

---

## 第 29 章：渲染桥接层

### 29.1 BattleBridge 的职责

`BattleBridge` 是连接模拟层和渲染层的**桥梁**：

```
BattleBridge.Update()
  │
  ├── 驱动 BattleSimulator.Tick()     ← 固定步长
  │
  └── SyncViews()                      ← 同步所有视图
        ├── 同步单位视图（创建/更新/销毁）
        ├── 同步投射物视图
        ├── 同步区域效果视图
        ├── 同步光束视图
        └── 同步 VFX 事件
```

### 29.2 单位视图同步

```csharp
void SyncViews()
{
    var units = Simulator.State.Units;

    for (int i = 0; i < units.Count; i++)
    {
        ref var u = ref units[i];

        if (u.State == UnitStateEnum.Dead)
        {
            // 播放死亡动画，然后销毁视图
            if (_unitViews.TryGetValue(u.Id, out var view))
            {
                view.PlayDeath();
                _unitViews.Remove(u.Id);
            }
            continue;
        }

        // 没有视图？创建一个
        if (!_unitViews.TryGetValue(u.Id, out var unitView))
        {
            var go = CreateUnitGameObject(u, def);
            unitView = go.AddComponent<UnitView>();
            _unitViews[u.Id] = unitView;
        }

        // 从模拟状态同步到视图
        unitView.SyncFromState(ref u);
    }
}
```

### 29.3 动态创建 GameObject

MC Fight 的单位 GameObject 是在运行时动态创建的，不是预制体：

```csharp
GameObject CreateUnitGameObject(UnitState u, MonsterDefSO def)
{
    var go = new GameObject($"Unit_{u.Id}_{u.MonsterId}");

    // 添加 SpriteRenderer
    var sr = go.AddComponent<SpriteRenderer>();
    sr.sprite = def.idleSprite;
    sr.sortingOrder = 100;

    // 根据标签调整大小
    float targetSize = u.HasTag("giant") ? 112f :
                        u.HasTag("boss") ? 56f :
                        u.MoveType == MoveType.Fly ? 40f : 40f;
    float scale = targetSize / def.idleSprite.rect.height;
    go.transform.localScale = new Vector3(scale, scale, 1);

    // 添加 HP 条
    var hpGo = new GameObject("HPBar");
    hpGo.transform.SetParent(go.transform, false);
    var hpSr = hpGo.AddComponent<SpriteRenderer>();
    hpSr.sortingOrder = 200;

    // 添加 UnitView 脚本
    var unitView = go.AddComponent<UnitView>();
    unitView.hpBarRenderer = hpSr;

    return go;
}
```

---

## 第 30 章：VFX 与编辑器扩展

### 30.1 VFXSpawner 架构

```csharp
public static class VFXSpawner
{
    // 懒加载的预制体缓存
    private static Dictionary<string, GameObject> _cache = new();

    public static void Spawn(string path, Vector3 position, float scale, float lifetime)
    {
        if (!_cache.TryGetValue(path, out var prefab))
        {
            prefab = Resources.Load<GameObject>(path);
            _cache[path] = prefab;
        }
        if (prefab == null) return;

        var go = Object.Instantiate(prefab, position, Quaternion.Euler(90, 0, 0));
        go.transform.localScale = Vector3.one * scale;
        Object.Destroy(go, lifetime);
    }

    // 便捷方法
    public static void SpawnMeleeHit(Vector3 pos, float dmg) => Spawn("VFX/Hit/MeleeHit", pos, ...);
    public static void SpawnExplosion(Vector3 pos, float scale) => Spawn("VFX/Explosion/Explosion", pos, ...);
    public static void SpawnLightHit(Vector3 pos, float scale) => Spawn("VFX/Hit/LightHit", pos, ...);
}
```

### 30.2 编辑器工具：MonsterDataGenerator

MC Fight 创建了一个编辑器菜单工具来批量生成怪物数据：

```csharp
// Editor/MonsterDataGenerator.cs
using UnityEditor;
using UnityEngine;

public class MonsterDataGenerator
{
    [MenuItem("MC Fight/Generate All Monster Data")]
    static void GenerateAll()
    {
        // 确保目录存在
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Monsters"))
            AssetDatabase.CreateFolder("Assets/Resources", "Monsters");

        // 定义所有怪物数据
        CreateMonster(new MonsterData {
            id = "creeper", name = "苦力怕", price = 20,
            hp = 20, attack = 40, armor = 0, moveSpeed = 58,
            moveType = MoveType.Ground, attackType = AttackType.Melee,
            tags = new[] { "explosive" },
            abilityType = ""
        });

        // ... 84 个怪物

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Generated all monster data!");
    }

    static void CreateMonster(MonsterData data)
    {
        var so = ScriptableObject.CreateInstance<MonsterDefSO>();
        so.monsterId = data.id;
        so.displayName = data.name;
        so.price = data.price;
        // ... 设置所有字段

        string path = $"Assets/Resources/Monsters/Monster_{data.id}.asset";
        AssetDatabase.CreateAsset(so, path);
    }
}
```

使用方式：Unity 菜单栏 → MC Fight → Generate All Monster Data

### 30.3 编辑器扩展的价值

- **批量操作**：84 个怪物手动创建太慢，一键生成
- **数据一致性**：所有怪物使用同一套数据模板
- **可重复执行**：修改数据后重新生成，更新所有资产
- **团队协作**：策划可以在 Excel 中维护数据，程序写生成器导入

---

# 附录

## A. 项目数据统计

| 指标 | 数量 |
|------|------|
| C# 源文件 | 34 |
| 怪物定义 | 84 |
| 独立技能实现 | 50+ |
| UI 面板 | 6 |
| 状态效果 | 7 |
| 伤害类型 | 5 |
| 弹道类型 | 8 |
| 区域效果类型 | 11 |

## B. 推荐学习路径

1. **第 1-10 章**：跟着做，安装 Unity 并打开项目
2. **第 11-17 章**：理解核心系统，在 Inspector 中实验
3. **第 18-21 章**：理解项目架构，阅读 GameManager 代码
4. **第 22-28 章**：深入模拟器，这是项目最核心的部分
5. **第 29-30 章**：理解渲染层和工具链

## C. 常见问题

**Q: 为什么 BattleSimulator 不用 MonoBehaviour？**
A: 纯 C# 类可以在没有 Unity 运行时的环境下执行，方便测试、网络同步、回放。

**Q: 为什么用 Resources.Load 而不是 Addressables？**
A: 项目规模较小（84个资产），Resources 足够。大型项目建议用 Addressables 实现按需加载。

**Q: SkillStateMap 为什么手写而不字典？**
A: 避免 Dictionary 的 GC 分配。手写 switch 语句在 JIT 编译后可能被优化为跳转表，性能更好。

**Q: 89 个字段的 UnitState 是否太多了？**
A: 这是性能与可维护性的权衡。用 struct 数组比 89 个独立字段或 Dictionary 更高效，但代码可读性确实下降了。
