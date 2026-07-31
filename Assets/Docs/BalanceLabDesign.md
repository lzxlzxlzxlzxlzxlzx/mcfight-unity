# MC Fight 平衡实验室 — 详细设计文档

> 版本：v0.3  
> 最后更新：2026-07-29  
> 状态：设计稿

---

## 目录

1. [设计愿景](#1-设计愿景)
2. [总览：七步主流程](#2-总览七步主流程)
3. [系统分层架构](#3-系统分层架构)
4. [核心概念与术语](#4-核心概念与术语)
5. [测试场景分类](#5-测试场景分类)
6. [步骤 1：用户提出测试需求](#6-步骤-1用户提出测试需求)
7. [步骤 2：AI 调研与规划](#7-步骤-2ai-调研与规划)
8. [步骤 3：计划预览与用户编辑](#8-步骤-3计划预览与用户编辑)
9. [步骤 4：用户确认](#9-步骤-4用户确认)
10. [步骤 5：执行测试](#10-步骤-5执行测试)
11. [步骤 6：测试结束 — AI 分析与存档](#11-步骤-6测试结束--ai-分析与存档)
12. [步骤 7：历史记录与对话引用](#12-步骤-7历史记录与对话引用)
13. [知识库设计](#13-知识库设计)
14. [LLM 调用架构](#14-llm-调用架构)
15. [UI 设计](#15-ui-设计)
16. [全局状态机](#16-全局状态机)
17. [文件结构](#17-文件结构)
18. [持久化结构](#18-持久化结构)
19. [与现有代码集成改造](#19-与现有代码集成改造)
20. [关键设计决策](#20-关键设计决策)
21. [用户请求示例库](#21-用户请求示例库)
22. [实施分期](#22-实施分期)

---

## 1. 设计愿景

### 1.1 要解决的问题

当前项目有 84 种怪物、47+ 技能，人工平衡几乎不可能覆盖所有组合。需要一个**游戏内实验室**，让 AI 代替人类做两件事：

| 方向 | 目标 | 产出 |
|------|------|------|
| **AI 策略训练** | 学会在 1000 金币约束下选最强阵容，理解单位定位与克制 | 阵容推荐、克制矩阵、单位角色标签 |
| **平衡性诊断** | 发现超模/弱势单位，给出数值调整建议 | 强度偏差报告、具体字段调整建议 |

### 1.2 核心体验

不是后台批量跑数据，而是：

```
你在对话框说：「帮我测试所有 20 金币的近战单位，1v1 对战，每种组合跑 5 次」
        ↓
AI 生成测试计划（共 45 场），展示给你确认
        ↓
你编辑计划（删/增/改阵容）后点击「确认」
        ↓
屏幕上一场场真实战斗，可随时暂停/停止/跳过
        ↓
全部完成后：AI 自动分析 → 保存报告 → 可随时查看与引用
```

### 1.3 设计原则

- **可视化优先**：战斗在屏幕上真实渲染，与正常对战一致
- **计划可编辑**：AI 生成计划后，用户拥有完全控制权
- **双轨知识积累**：策略知识与平衡知识跨会话持久化
- **LLM 做解读，代码做计算**：排名、胜率等由本地计算，LLM 负责规划与分析

---

## 2. 总览：七步主流程

```
步骤 1: 用户提出需求（自然语言 + 可选引用历史）
    ↓
步骤 2: AI 调研与规划（拉取知识库/单位数据 → 生成结构化 TestPlan）
    ↓
步骤 3: 计划预览与编辑（删/增/改 case、改阵容、改重复次数）
    ↓
步骤 4: 用户确认（校验 → 锁定计划 → 初始化执行队列）
    ↓
步骤 5: 执行测试（逐 Case 可视化战斗，可暂停/停止/跳过）
    ↓
步骤 6: AI 分析与存档（聚合 → LLM 分析 → 持久化 SessionReport）
    ↓
步骤 7: 历史记录与引用（浏览报告、在对话中 @引用历史测试）
```

| 步骤 | 用户动作 | 系统动作 | 产出 |
|------|---------|---------|------|
| 1 | 在对话框输入自然语言需求 | 解析意图、展示「规划中…」 | `RequirementDraft` |
| 2 | 等待 | 拉取知识库/单位数据 → LLM 深度分析 → 生成计划 | `TestPlan`（结构化） |
| 3 | 编辑计划（删/增/改阵容） | 本地校验、实时更新预估 | `TestPlan`（用户修订版） |
| 4 | 点击「确认开始」 | 锁定计划、初始化执行队列 | `ConfirmedTestPlan` |
| 5 | 观看战斗；可暂停/停止/跳过 | 逐 Case 执行、收集战报 | `ExecutionState` + `MatchReport[]` |
| 6 | 等待（自动） | 聚合数据 → LLM 分析 → 持久化 | `SessionReport` |
| 7 | 浏览历史、在对话中 @引用 | 加载存档、注入上下文 | 跨会话知识更新 |

---

## 3. 系统分层架构

```
┌─────────────────────────────────────────────────────────┐
│  交互层                                                  │
│  RequirementChat │ BalanceLabUI │ BattleBridge(战斗视图)  │
├─────────────────────────────────────────────────────────┤
│  规划层                                                  │
│  TestPlanner │ PlanValidator │ PlanEnricher              │
├─────────────────────────────────────────────────────────┤
│  执行层                                                  │
│  LabSessionController │ TestExecutor │ CaseGenerator     │
│  LineupGenerator │ DeployGenerator │ BattleRunner         │
├─────────────────────────────────────────────────────────┤
│  分析层                                                  │
│  MatchAnalyzer │ PhaseAnalyzer │ SessionAnalyzer         │
│  BalanceCalculator │ LLMAnalyzer                         │
├─────────────────────────────────────────────────────────┤
│  知识层                                                  │
│  KnowledgeBase │ StrategyKnowledge │ BalanceKnowledge    │
│  KnowledgePersistence                                    │
├─────────────────────────────────────────────────────────┤
│  基础设施                                                │
│  BattleSimulator │ BattleStatsCollector │ MonsterDatabase│
│  LLMClient                                               │
└─────────────────────────────────────────────────────────┘
```

---

## 4. 核心概念与术语

| 术语 | 定义 |
|------|------|
| **LabSession** | 一次完整的测试会话，包含用户请求、测试计划、所有战报 |
| **TestPlan** | AI 生成的结构化测试计划，由多个 TestPhase 组成 |
| **TestPhase** | 测试阶段，如「20 金币近战 1v1 矩阵」或「1000 金币实战模拟」 |
| **TestCase** | 最小测试单元 = 一场战斗的配置（双方阵容 + 部署 + 重复次数） |
| **TestScenario** | 测试场景类型枚举，决定如何生成 TestCase |
| **MatchReport** | 单场战斗的结构化战报 |
| **PhaseReport** | 一个 TestPhase 完成后的聚合报告 |
| **SessionReport** | 整个 LabSession 的最终报告 |

层级关系：

```
LabSession
  └── TestPlan
        └── TestPhase[] (有序)
              └── TestCase[] (有序)
                    └── MatchRun[] (同一 TestCase 可重复 N 次)
                          └── MatchReport
```

---

## 5. 测试场景分类

### 5.1 场景类型一览

| 场景 ID | 名称 | 用途 | 示例 |
|---------|------|------|------|
| `FullShopMatch` | 真实商店对战 | 模拟真实 1000G 选购 + 部署 + 对战 | 蓝方 AI 选阵 vs 红方 AI 选阵 |
| `MirrorDuel` | 镜像对决 | 同单位 vs 同单位，测基础强度 | creeper×10 vs creeper×10 |
| `PriceTierDuel` | 同价对决 | 同等价格不同单位 1v1 | 所有 20G 近战互打 |
| `UnitVsUnit` | 指定单位对决 | 两个特定单位对战 | warden vs tremorzilla |
| `TagMatchup` | 标签对抗 | 某标签群体 vs 另一标签 | explosive 单位 vs tank 单位 |
| `CounterProbe` | 克制探测 | A 单位 vs 含 B 的阵容 | frostmaw vs 全 blaze 阵容 |
| `CompositionDuel` | 阵容对决 | 两个固定阵容互打 | 「warden+creeper」vs「tremorzilla+blaze」 |
| `EconomyEfficiency` | 性价比测试 | 同金币不同组合的效率 | 1000G 全 creeper vs 1000G 全 warden |
| `DeploymentSensitivity` | 部署敏感性 | 同阵容不同站位 | 同一阵容，前排/后排/分散部署 |
| `BossStressTest` | Boss 压力测试 | 多个低费单位 vs 单个 Boss | 10×deep_one vs 1×tremorzilla |
| `FreeExplore` | 自由探索 | LLM 自主设计非常规测试 | 「测试飞行单位对地面单位的压制力」 |

### 5.2 部署策略

| 策略 | 行为 |
|------|------|
| `StandardSpread` | 随机分散在各自半场（现有 `AutoDeploy` 逻辑） |
| `FrontLine` | 近战前排、远程后排 |
| `ClusterCenter` | 聚集在中线附近（测试 AOE 效果） |
| `FlankBoth` | 两翼包夹 |
| `Fixed` | 指定每个单位的 (x, y) |
| `LLMOptimized` | LLM 根据单位类型决定站位 |

---

## 6. 步骤 1：用户提出测试需求

### 6.1 UI 设计

```
┌─ 需求对话 ─────────────────────────────────────────┐
│ 📎 引用历史: [session_20260722_001 ▼]  [清除引用]   │
├────────────────────────────────────────────────────┤
│ 🤖 欢迎使用平衡实验室。你可以描述测试需求，例如：    │
│    · 测试所有 20 金币近战谁最强                      │
│    · warden 在 1000G 阵容里表现如何                  │
│    · 对比上次测试中 creeper 和 blaze 的克制关系       │
├────────────────────────────────────────────────────┤
│ 👤 帮我测试所有 20 金币的近战单位，1v1 对战，        │
│    每种组合跑 5 次，然后和 35 金币近战冠军对比       │
│                                                    │
│ 🤖 收到，正在分析需求并制定测试计划…                 │
│    [████████░░] 正在查询单位数据…                    │
├────────────────────────────────────────────────────┤
│ [ 输入测试需求…                              ] [发送]│
│ ☐ 引用当前知识库  ☑ 引用历史记录 [选择…]             │
└────────────────────────────────────────────────────┘
```

### 6.2 输入数据结构

```csharp
public class RequirementDraft
{
    public string RawText;                          // 用户原文
    public List<string> ReferencedSessionIds;       // @引用的历史会话
    public bool IncludeKnowledgeBase;               // 是否注入知识库
    public LabSessionConfig UserConstraints;        // 用户约束（可选）
}

public class LabSessionConfig
{
    public int MaxTotalMatches = 200;               // 场次上限
    public int MaxDurationMinutes = 60;
    public int DefaultGoldBudget = 1000;
    public float PauseBetweenMatches = 2f;          // 局间暂停秒数
    public bool AutoContinue = true;
    public bool EnablePerMatchLLM = false;          // 是否每场 LLM 点评
}
```

### 6.3 意图预解析（本地，不调 LLM）

发送后先做轻量本地解析，给用户即时反馈：

```csharp
public class IntentPreview
{
    public List<string> DetectedKeywords;     // "20金币", "近战", "1v1"
    public List<string> MentionedUnitIds;     // 从文本中匹配到的 monsterId
    public TestScenario? SuggestedScenario;   // 推测场景类型
    public bool NeedsClarification;           // 信息不足时需追问
    public string ClarificationQuestion;      // "你说的'近战'是指 attackType=Melee 吗？"
}
```

**追问机制**：若检测到歧义（如「测试 warden」未说明场景），AI 在规划前先问 1–2 个澄清问题，用户回答后再进入步骤 2。

---

## 7. 步骤 2：AI 调研与规划

分 **调研（Research）** 和 **规划（Planning）** 两阶段。

### 7.1 调研阶段 — 信息收集

流程：

```
收到 RequirementDraft
    → 本地数据收集（MonsterCatalog + KnowledgeBase + 引用历史）
    → 按意图预筛选单位
    → 组装 ResearchContext
    → LLM 深度分析
    → 生成 TestPlan
```

#### ResearchContext（调研上下文包）

```csharp
public class ResearchContext
{
    public RequirementDraft Requirement;
    public IntentPreview IntentPreview;
    public LabSessionConfig Constraints;

    public MonsterCatalogSnapshot Catalog;
    public KnowledgeSnapshot Knowledge;
    public List<SessionSummary> ReferencedSessions;
    public UnitFilterResult FilteredUnits;
}

public class MonsterCatalogSnapshot
{
    public int TotalCount;
    public Dictionary<int, int> CountByPrice;
    public Dictionary<string, int> CountByAttackType;
    public Dictionary<string, int> CountByMoveType;
    public Dictionary<string, int> CountByTag;
    public List<MonsterDetail> RelevantUnits;   // 预筛选后的单位详情
}

public class MonsterDetail
{
    public string MonsterId;
    public string DisplayName;
    public int Price;
    public float Hp, Attack, Armor;
    public string AttackType, MoveType;
    public string[] Tags;
    public string AbilityType;
    public string Description;

    // 来自知识库（若有）
    public float? KnownWinRate;
    public float? KnownPowerScore;
    public string KnownRole;
    public BalanceStatus? KnownBalanceStatus;
}

public class KnowledgeSnapshot
{
    public List<UnitRanking> TopStrongUnits;        // top 10
    public List<UnitRanking> TopWeakUnits;          // bottom 10
    public List<CounterRelation> TopCounters;       // top 10 克制对
    public List<string> RecentFindings;             // 最近 10 条 LLM 发现
    public List<BalanceSuggestion> PendingSuggestions;
    public int TotalSessionsRun;
    public int TotalMatchesRun;
}

public class UnitFilterResult
{
    public List<MonsterDetail> MatchedUnits;
    public UnitFilterCriteria AppliedCriteria;
    public int TotalMatched;
    public string FilterDescription;
}

public class UnitFilterCriteria
{
    public int? PriceMin, PriceMax, TargetPrice;
    public AttackType? AttackType;
    public MoveType? MoveType;
    public string[] TagsInclude, TagsExclude;
    public string[] UnitIdsInclude;
}
```

#### UI 进度展示

```
🤖 正在制定测试计划…
   ✅ 解析用户意图: 同价对决 + 跨价对比
   ✅ 加载知识库 (已积累 12 次测试, 340 场战斗)
   ✅ 筛选单位: price=20, Melee → 6 个
   ✅ 加载历史引用: session_20260721_003
   ⏳ AI 正在生成测试计划…
```

### 7.2 规划阶段 — LLM 生成 TestPlan

> **输出格式**：LLM 输出的 TestPlan 经后处理（见 7.3 节）后，最终转换为标准的 `.balancetest.json` 文件（格式规格见 [7.4 节](#74-测试计划-json-文件格式规格)），该文件可直接导入游戏进行自动测试。

#### LLM Prompt 结构

```
[System]
你是 MC Fight 平衡实验室的测试规划 AI…
（系统规则、输出 JSON Schema 详见 BalanceLabTestPlan/v1 规范）

[Research Context]
## 用户需求
"帮我测试所有 20 金币的近战单位…"

## 筛选到的单位 (6个)
| id | name | price | hp | attack | tags |
...

## 知识库摘要
- creeper 历史胜率: 62%, 定位: AOE Assassin
...

## 引用历史 (session_20260721_003)
- 上次 20G 测试结论: creeper > stray > deep_one

## 约束
- 最大场次: 200
- 金币预算: 1000
- 输出格式：符合 BalanceLabTestPlan/v1 JSON Schema (每项测试含 id, label, team_red, team_blue, repeat_count 等字段)

[Task]
根据以上信息，生成结构化测试计划 JSON。输出 JSON 必须符合 BalanceLabTestPlan/v1 格式规范，可直接导入游戏。包含 version, generated_at, generated_by, metadata, tests 等顶层字段。
要求：
1. 分阶段（Phase），每阶段有明确目标
2. 每个 TestCase 必须指定双方阵容（具体 monsterId + count）
3. 估算总场次
4. 说明规划理由
```

#### TestPlan 完整数据结构

```csharp
public class TestPlan
{
    public string PlanId;
    public string Title;
    public string UserRequest;
    public string AIReasoning;
    public PlanStatus Status;               // Draft / Confirmed / Executing / Completed
    public DateTime CreatedAt;

    public LabSessionConfig Config;
    public List<TestPhase> Phases;
    public PlanSummary Summary;

    public int Revision;
    public List<PlanRevision> History;
}

public class PlanSummary
{
    public int TotalPhases;
    public int TotalCases;
    public int TotalMatches;
    public float EstimatedDurationMinutes;
    public List<string> InvolvedUnitIds;
}

public enum PlanStatus { Draft, Confirmed, Executing, Paused, Completed, Cancelled, Failed }

public class TestPhase
{
    public string PhaseId;
    public int Order;
    public string Name;
    public string Description;
    public TestScenario ScenarioType;
    public ScenarioConfig Config;

    public List<TestCase> Cases;
    public List<string> DependsOnPhaseIds;

    public PhaseExecutionState ExecutionState;
    public PhaseReport Report;
}

public class TestCase
{
    public string CaseId;
    public int Order;
    public string Label;
    public string Description;

    public TeamLineup Team0;
    public TeamLineup Team1;

    public int RepeatCount;
    public DeployStrategy DeployStrategy;
    public int SeedBase;

    public CaseOrigin Origin;               // AIGenerated / UserAdded / UserModified
    public string ModifiedFromCaseId;

    public CaseExecutionState ExecutionState;
    public List<MatchReport> MatchReports;
}

public class TeamLineup
{
    public List<LineupEntry> Units;
    public int TotalCost(MonsterDatabase db);
    public bool IsValid(MonsterDatabase db);
}

public class LineupEntry
{
    public string MonsterId;
    public int Count;
    public string DisplayName;
}

public enum CaseOrigin { AIGenerated, UserAdded, UserModified, UserCloned, SystemResolved }
```

#### LLM 输出示例

```json
{
  "title": "20G近战强度评估与跨价对比",
  "reasoning": "先用全组合矩阵建立 20G 近战 baseline 排名（6单位15配对×5次=75场），再让冠军与 35G 近战冠军对比验证价格梯度是否合理",
  "phases": [
    {
      "phase_id": "phase_1",
      "name": "20G近战 1v1 矩阵",
      "scenario_type": "PriceTierDuel",
      "cases": [
        {
          "case_id": "case_1_1",
          "label": "creeper vs deep_one",
          "team0": { "units": [{ "monster_id": "creeper", "count": 1 }] },
          "team1": { "units": [{ "monster_id": "deep_one", "count": 1 }] },
          "repeat_count": 5,
          "deploy_strategy": "StandardSpread"
        }
      ]
    },
    {
      "phase_id": "phase_2",
      "name": "跨价对比",
      "depends_on": ["phase_1"],
      "cases": [
        {
          "case_id": "case_2_1",
          "label": "20G冠军 vs 35G冠军",
          "description": "team0 使用 phase_1 排名第一的单位（执行时动态替换）",
          "team0": { "units": [{ "monster_id": "__DYNAMIC:phase_1_rank_1__", "count": 1 }] },
          "team1": { "units": [{ "monster_id": "warden", "count": 1 }] },
          "repeat_count": 10
        }
      ]
    }
  ]
}
```

### 7.3 计划后处理

LLM 输出后，本地必须执行：

| 步骤 | 操作 |
|------|------|
| **Validate** | 所有 monsterId 存在；repeat ≥ 1；总价 ≤ 预算（FullShopMatch） |
| **Enrich** | 填充 `DisplayName`、计算 `Summary`、生成缺失的 `CaseId` |
| **Resolve Dynamic** | `__DYNAMIC:phase_1_rank_1__` 标记为待运行时解析 |
| **Cap** | 若 totalMatches > max，按优先级裁剪并通知用户 |
| **Deduplicate** | 去除重复 case |

### 7.4 测试计划 JSON 文件格式规格

LLM 生成的测试计划最终以独立 `.json` 文件保存，该文件可直接导入游戏进行自动测试。

#### 7.4.1 文件约定

| 项目 | 规则 |
|------|------|
| **文件扩展名** | `.balancetest.json` |
| **存放目录** | `Assets/Resources/BalanceLab/Tests/` |
| **编码** | UTF-8 (无 BOM) |
| **换行** | LF (`\n`) |
| **缩进** | 2 空格 |
| **文件名** | `{项目简述}_{日期}.balancetest.json`，如 `20g_melee_matrix_2026-07-29.balancetest.json` |

#### 7.4.2 JSON Schema

```json
{
  "$schema": "BalanceLabTestPlan/v1",
  "version": "1.0",
  "generated_at": "2026-07-29T14:00:00Z",
  "generated_by": "LLM:claude-4.5 (prompt: 20G近战强度评估)",
  "metadata": {
    "title": "测试计划名称",
    "description": "人类可读的描述",
    "estimated_duration_minutes": 28,
    "total_matches": 85
  },
  "tests": [
    {
      "id": "unique_test_id",
      "label": "简短标签（用于UI显示）",
      "category": "1v1 | mirror | team | phase",
      "description": "测试目的描述",
      "deploy_strategy": "StandardSpread",

      "team_red": {
        "monsters": [
          { "monster_id": "creeper", "count": 2 },
          { "monster_id": "zombie",  "count": 1 }
        ]
      },
      "team_blue": {
        "monsters": [
          { "monster_id": "blaze", "count": 1 }
        ]
      },

      "repeat_count": 5,
      "matches_per_repeat": 1,
      "battle_mode": "AutoPlay",
      "terrain": "DefaultFlat",
      "enable_recording": true,

      "success_criteria": {
        "team": "blue",
        "type": "win_rate",
        "min": 0.5,
        "max": 0.8
      },

      "tags": ["melee", "20g", "baseline"],
      "priority": 1
    }
  ]
}
```

#### 7.4.3 字段定义

**顶层字段**：

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `version` | string | ✅ | 固定 `"1.0"` |
| `generated_at` | string(ISO8601) | ✅ | 生成时间 |
| `generated_by` | string | ✅ | 生成来源标识 |
| `metadata.title` | string | ✅ | 计划名称 |
| `metadata.description` | string | 否 | 计划描述 |
| `metadata.estimated_duration_minutes` | number | 否 | 预计总耗时（分钟） |
| `metadata.total_matches` | number | 否 | 预计总对局数 |
| `tests` | array | ✅ | 测试项目列表，至少 1 项 |

**测试项目字段 (`tests[]`)**：

| 字段 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| `id` | string | ✅ | - | 唯一标识，如 `"creeper_vs_blaze_001"` |
| `label` | string | ✅ | - | UI 显示的简短名称 |
| `category` | string | 否 | `"wildcard"` | 分类：`1v1`, `mirror`, `team`, `phase` 等 |
| `description` | string | 否 | `""` | 测试目的说明 |
| `deploy_strategy` | string | 否 | `"StandardSpread"` | 部署策略名 |
| `team_red.monsters` | array | ✅ | - | 红方单位列表 |
| `team_blue.monsters` | array | ✅ | - | 蓝方单位列表 |
| `repeat_count` | number | ✅ | - | 每场重复次数（≥1） |
| `matches_per_repeat` | number | 否 | `1` | 每次重复中的对局数（用于位交换等） |
| `battle_mode` | string | 否 | `"AutoPlay"` | 战斗模式 |
| `terrain` | string | 否 | `"DefaultFlat"` | 地形选择 |
| `enable_recording` | boolean | 否 | `true` | 是否录制回放 |
| `success_criteria` | object | 否 | 见下方 | 胜负判定条件 |
| `tags` | string[] | 否 | `[]` | 标签列表 |
| `priority` | number | 否 | `1` | 优先级（1~5，1 最高） |

**单位定义 (`monsters[]`)**：

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `monster_id` | string | ✅ | 怪物 ID，如 `"creeper"`, `"blaze"` |
| `count` | number | ✅ | 数量（≥1） |

**胜负判定条件 (`success_criteria`)**：

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `team` | string | ✅ | 判定方：`"red"`, `"blue"`, `"either"` |
| `type` | string | ✅ | 判定类型：`"win_rate"`, `"rounds_won"`, `"avg_damage"` |
| `min` | number | 否 | 最小阈值（闭区间） |
| `max` | number | 否 | 最大阈值（闭区间） |

#### 7.4.4 完整示例

```json
{
  "$schema": "BalanceLabTestPlan/v1",
  "version": "1.0",
  "generated_at": "2026-07-29T14:00:00Z",
  "generated_by": "LLM:claude-4.5 (prompt: 20G近战强度评估)",
  "metadata": {
    "title": "20G 近战 1v1 强度评估",
    "description": "评估所有 20G 价位近战单位之间的 1v1 对战强度，建立基线排名",
    "estimated_duration_minutes": 25,
    "total_matches": 120
  },
  "tests": [
    {
      "id": "phase1_creeper_vs_zombie",
      "label": "苦力怕 vs 僵尸",
      "category": "1v1",
      "description": "爆炸 vs 近战耐久",
      "deploy_strategy": "StandardSpread",
      "team_red": {
        "monsters": [{ "monster_id": "creeper", "count": 1 }]
      },
      "team_blue": {
        "monsters": [{ "monster_id": "zombie", "count": 1 }]
      },
      "repeat_count": 10,
      "battle_mode": "AutoPlay",
      "terrain": "DefaultFlat",
      "enable_recording": true,
      "success_criteria": {
        "team": "blue",
        "type": "win_rate",
        "min": 0.3,
        "max": 0.7
      },
      "tags": ["melee", "20g", "baseline"],
      "priority": 1
    },
    {
      "id": "phase2_creeper_squad",
      "label": "苦力怕小队 vs 烈焰人",
      "category": "team",
      "description": "3 苦力怕 vs 1 烈焰人，测试多对少场景",
      "deploy_strategy": "StandardSpread",
      "team_red": {
        "monsters": [{ "monster_id": "creeper", "count": 3 }]
      },
      "team_blue": {
        "monsters": [{ "monster_id": "blaze", "count": 1 }]
      },
      "repeat_count": 10,
      "battle_mode": "AutoPlay",
      "terrain": "DefaultFlat",
      "enable_recording": true,
      "tags": ["multi", "20g", "cross-phase"],
      "priority": 2
    }
  ]
}
```

#### 7.4.5 导入与校准流程

```
1. 用户将 .balancetest.json 文件放入 Tests/ 目录
       ↓
2. 进入 BalanceLab → 点击"导入测试计划"
       ↓
3. 系统解析 JSON，校验：
   · 所有 monster_id 在数据库中存在
   · repeat_count ≥ 1
   · count ≥ 1
   · 每个测试项目 ID 唯一
       ↓
4. 显示校验结果 + 预览（同步骤3的预览UI）
       ↓
5. 用户确认后 → 加入执行队列 → 自动运行
       ↓
6. 完成后生成结果报告
```

#### 7.4.6 校验规则

| 规则 | 错误级别 | 处理 |
|------|----------|------|
| `monster_id` 不存在 | ❌ Error | 拒绝导入，提示缺失的 ID |
| `repeat_count` < 1 | ❌ Error | 拒绝导入 |
| `count` < 1 | ❌ Error | 拒绝导入 |
| `team_red.monsters` 或 `team_blue.monsters` 为空 | ❌ Error | 拒绝导入 |
| 测试 ID 重复 | ⚠️ Warning | 自动去重 + 警告 |
| `version` 不支持 | ⚠️ Warning | 尝试兼容解析 + 警告 |
| `success_criteria` 阈值不合理 (min > max) | ⚠️ Warning | 使用默认值 + 警告 |
| `terrain` 未知 | ℹ️ Info | 回退到 DefaultFlat |

#### 7.4.7 与 LLM TestPlan 的映射

LLM 输出的语义化 TestPlan（见 7.2 节）在本地后处理时转换为此 JSON 格式：

| LLM TestPlan 字段 | → | JSON 文件字段 |
|---|---|---|
| `case.case_id` | → | `tests[i].id` |
| `case.label` | → | `tests[i].label` |
| `case.description` | → | `tests[i].description` |
| `case.team0.units` | → | `tests[i].team_red.monsters` |
| `case.team1.units` | → | `tests[i].team_blue.monsters` |
| `case.repeat_count` | → | `tests[i].repeat_count` |
| `phase.phase_id` | → | `tests[i].phase_id` (保存到 tags) |
| `plan.plan_id + plan.name` | → | `metadata.title` / `generated_by` |
| `plan.strategy.budget` | → | `metadata` 扩展字段 `budget` |

`__DYNAMIC:` 占位符在 **运行时** 由前序 phase 的测试结果代入后解析为具体 monster_id。

---


## 8. 步骤 3：计划预览与用户编辑

### 8.1 计划预览 UI

```
┌─ 测试计划预览 ──────────────────────────────────────────────────────┐
│ 📋 20G近战强度评估与跨价对比                                          │
│ AI 规划理由: 先用全组合矩阵建立 baseline…                              │
│ 共 2 阶段 · 16 用例 · 85 场 · 预计 ~28 分钟                           │
├─────────────────────────────────────────────────────────────────────┤
│ ▼ 阶段 1: 20G近战 1v1 矩阵 (15 用例 · 75 场)                        │
│   ┌────┬──────────────────┬──────────────┬────┬──────┬───────────┐  │
│   │ #  │ 对战              │ 蓝方          │ 红方  │ 重复  │ 操作       │  │
│   ├────┼──────────────────┼──────────────┼────┼──────┼───────────┤  │
│   │ 1  │ creeper vs       │ creeper ×1   │ deep │ ×5   │ ✏️ 🗑️     │  │
│   │    │ deep_one         │              │ _one │      │           │  │
│   │ 2  │ creeper vs stray │ creeper ×1   │ stray│ ×5   │ ✏️ 🗑️     │  │
│   │ …  │                  │              │      │      │           │  │
│   └────┴──────────────────┴──────────────┴────┴──────┴───────────┘  │
│   [ + 新增用例到此阶段 ]                                              │
│ ▼ 阶段 2: 跨价对比 (1 用例 · 10 场)                                   │
│   [ + 新增用例到此阶段 ]                                              │
│ [ + 新增阶段 ]                                                       │
├─────────────────────────────────────────────────────────────────────┤
│ ⚙️ 执行设置: 局间暂停 [2.0]s  ☑ 自动继续  场次上限 [200]              │
├─────────────────────────────────────────────────────────────────────┤
│ [ ← 返回修改需求 ]  [ 💾 保存草稿 ]  [ ▶ 确认并开始测试 ]              │
└─────────────────────────────────────────────────────────────────────┘
```

### 8.2 编辑操作

| 操作 | 触发 | 系统行为 |
|------|------|---------|
| **删除用例** | 点击 🗑️ | 从列表移除，重算 Summary |
| **编辑用例** | 点击 ✏️ | 打开 CaseEditor 面板 |
| **新增用例** | 点击「+ 新增用例」 | 打开 CaseEditor（空白） |
| **克隆用例** | 右键 → 克隆 | 复制 case，Origin=UserCloned |
| **调整顺序** | 拖拽 | 更新 Order |
| **删除阶段** | 阶段标题旁 🗑️ | 删除整个 Phase |
| **新增阶段** | 「+ 新增阶段」 | 空白 Phase |
| **修改重复次数** | 行内编辑 | 即时重算 Summary |
| **修改执行设置** | 底部设置区 | 更新 Config |
| **撤销** | 工具栏 | 回到上一 Revision |

### 8.3 CaseEditor 面板

```
┌─ 编辑用例 ──────────────────────────────────────────┐
│ 用例名称: [ creeper vs blaze 对比测试          ]      │
│ 说明:     [ 测试自爆单位 vs 远程单位的克制关系    ]      │
│ 重复次数: [ 5 ]                                       │
│ 部署策略: [ StandardSpread ▼ ]                       │
├─────────────────────────────────────────────────────┤
│ 🔵 蓝方 (Team 0)                                      │
│ ┌──────────────┬───────┬───────┬──────┐               │
│ │ 单位          │ 单价   │ 数量   │ 小计  │ 操作       │
│ ├──────────────┼───────┼───────┼──────┤               │
│ │ creeper 苦力怕│  20G  │ [ 3 ] │  60G │ 🗑️          │
│ │ warden 监守者 │ 200G  │ [ 1 ] │ 200G │ 🗑️          │
│ └──────────────┴───────┴───────┴──────┘               │
│ 蓝方总价: 260G / 1000G                                 │
│ [ + 添加单位 ]  ← 打开单位选择器                        │
├─────────────────────────────────────────────────────┤
│ 🔴 红方 (Team 1)                                      │
│ ┌──────────────┬───────┬───────┬──────┐               │
│ │ blaze 烈焰人  │  35G  │ [ 5 ] │ 175G │ 🗑️          │
│ └──────────────┴───────┴───────┴──────┘               │
│ 红方总价: 175G / 1000G                                 │
│ [ + 添加单位 ]                                        │
├─────────────────────────────────────────────────────┤
│ ⚠️ 校验: ✅ 双方至少 1 个单位  ✅ 所有 ID 有效           │
│ [ 取消 ]  [ 保存用例 ]                                  │
└─────────────────────────────────────────────────────┘
```

### 8.4 单位选择器

```
┌─ 选择单位 ──────────────────────────────────────────┐
│ 搜索: [ cree                    ] 🔍               │
│ 筛选: 价格 [全部▼] 类型 [全部▼] 标签 [全部▼]         │
├─────────────────────────────────────────────────────┤
│ ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐       │
│ │[sprite]│ │[sprite]│ │[sprite]│ │[sprite]│       │
│ │ creeper│ │ deep   │ │ stray  │ │ blaze  │       │
│ │ 20G    │ │ 10G    │ │ 20G    │ │ 35G    │       │
│ │ ⭐62%  │ │ ⚠️28%  │ │  55%   │ │  71%   │       │
│ └────────┘ └────────┘ └────────┘ └────────┘       │
│ ⭐=知识库胜率  ⚠️=知识库标记弱势                       │
├─────────────────────────────────────────────────────┤
│ 已选: creeper × [ 3 ]              [ 确认添加 ]      │
└─────────────────────────────────────────────────────┘
```

### 8.5 编辑校验规则

**硬错误（阻止保存）：**

- 双方不能都为空
- 所有 monsterId 必须存在于 Database
- 每个单位 count ≥ 1
- repeat_count ≥ 1

**软警告（允许保存但提示）：**

- 单方总价超过 GoldBudget
- 双方是同一单位（镜像对决）
- 总场次超过 MaxTotalMatches

### 8.6 计划版本管理

```csharp
public class PlanRevision
{
    public int Revision;
    public DateTime Timestamp;
    public string ChangeDescription;
    public TestPlan Snapshot;
}
```

每次用户编辑保存 → `Revision++`，记录变更摘要。支持「撤销到上一版本」。

---

## 9. 步骤 4：用户确认

### 9.1 确认流程

```
用户点击「确认并开始测试」
    ↓
PlanValidator 全量校验（所有 case）
    ↓
有错误 → 阻止，高亮错误 case
有警告 → 弹窗确认
    ↓
用户确认
    ↓
TestPlan.Status = Confirmed
生成 ConfirmedTestPlan（不可再编辑，除非「中止并重编」）
初始化 ExecutionQueue
进入步骤 5
```

### 9.2 确认弹窗

```
┌─ 确认开始测试 ──────────────────────────────┐
│ 📋 20G近战强度评估与跨价对比                  │
│ 2 阶段 · 16 用例 · 85 场战斗                 │
│ 预计耗时: ~28 分钟                           │
│                                             │
│ ⚠️ 1 条警告:                                │
│  · case_2_1 使用了动态占位符，将在阶段1        │
│    完成后自动替换为排名第一的单位               │
│                                             │
│ 涉及单位: creeper, deep_one, stray,         │
│ pillager, vindicator, skeleton, warden      │
│                                             │
│ [ 取消 ]  [ ▶ 确认并开始 ]                   │
└─────────────────────────────────────────────┘
```

### 9.3 ConfirmedTestPlan

```csharp
public class ConfirmedTestPlan
{
    public TestPlan Plan;
    public ExecutionQueue Queue;
    public DateTime ConfirmedAt;
}

public class ExecutionQueue
{
    public List<QueuedCase> Items;
    public int TotalMatches;
}

public class QueuedCase
{
    public string PhaseId;
    public TestCase Case;
    public int GlobalIndex;
    public int MatchOffset;
}
```

---

## 10. 步骤 5：执行测试

### 10.1 执行状态机

```
Ready → Running → Battling → MatchComplete
    → CaseComplete → PhaseComplete → ResolvingDynamic → AllComplete

Running ↔ Paused（用户暂停/继续）
Running → Skipping（跳过当前 Case/Match）
Running/Paused → Stopping → Aborted
```

### 10.2 执行控制器

```csharp
public class LabSessionController : MonoBehaviour
{
    public LabExecutionState State;

    public void StartExecution(ConfirmedTestPlan plan);
    public void Pause();
    public void Resume();
    public void Stop();
    public void SkipCurrentCase();
    public void SkipCurrentMatch();

    public event Action<MatchReport> OnMatchStarted;
    public event Action<MatchReport> OnMatchCompleted;
    public event Action<TestCase> OnCaseCompleted;
    public event Action<TestCase> OnCaseSkipped;
    public event Action<TestPhase> OnPhaseCompleted;
    public event Action<LabExecutionState> OnStateChanged;
}

public class LabExecutionState
{
    public PlanStatus Status;
    public int CurrentPhaseIndex;
    public int CurrentCaseIndex;
    public int CurrentMatchIndex;
    public int GlobalMatchIndex;
    public int TotalMatches;
    public float ProgressPercent;
    public TestCase CurrentCase;
    public string CurrentMatchLabel;
    public float ElapsedTime;
    public int CompletedMatches;
    public int SkippedCases;
    public int SkippedMatches;
    public bool IsPaused;
    public bool SkipRequested;
}
```

### 10.3 执行 UI

```
┌─ 执行中 ──────────────────────────────────────────────────────────┐
│ 📋 20G近战强度评估  |  阶段 1/2  |  用例 3/15  |  场次 11/85       │
│ ████████░░░░░░░░░░░░ 13%  |  已耗时 3:42  |  预计剩余 25:18         │
├────────────────────────────────────────────────────────────────────┤
│                    🎮 战斗视图 (BattleBridge)                        │
│                    creeper (蓝) vs pillager (红)                    │
│                    第 1/5 场                                       │
├────────────────────────────────────────────────────────────────────┤
│ 当前: case_1_3  creeper ×1 vs pillager ×1  (1v1 · 标准分散)          │
│ [ ⏸ 暂停 ]  [ ⏭ 跳过此用例 ]  [ ⏩ 跳过此场 ]  [ ⏹ 停止测试 ]       │
├────────────────────────────────────────────────────────────────────┤
│ 📊 实时统计 (本阶段)                                                 │
│ creeper: 2胜0负 | pillager: 0胜1负(进行中) | deep_one: 0胜2负         │
└────────────────────────────────────────────────────────────────────┘
```

### 10.4 暂停 / 停止 / 跳过 语义

| 操作 | 行为 | 数据影响 |
|------|------|---------|
| **暂停** | 当前战斗打完后再暂停 | 不丢数据 |
| **继续** | 从下一个 match 继续 | — |
| **跳过此场** | 当前 match 标记 `Skipped`，不跑战斗 | 该 match 无 MatchReport |
| **跳过此用例** | 当前 case 所有剩余 match 标记 `Skipped` | 该 case 部分/无 MatchReport |
| **停止测试** | 当前战斗打完 → 终止 | 部分 SessionReport |

```csharp
public enum MatchStatus { Pending, Running, Completed, Skipped, Failed }

public class MatchReport
{
    public string MatchId;
    public int PhaseId;
    public int CaseIndex;
    public int RunIndex;
    public TestScenario Scenario;
    public LineupSnapshot Team0;
    public LineupSnapshot Team1;
    public int Winner;
    public float Duration;
    public string EndReason;
    public MatchStatus Status;
    public string SkipReason;
    public Dictionary<string, AggregatedUnitStats> UnitStats;
    public string LLMComment;
}
```

### 10.5 动态占位符解析

阶段 2 的 `__DYNAMIC:phase_1_rank_1__` 在阶段 1 完成后解析：

```csharp
public void ResolveDynamicCases(TestPhase upcomingPhase, PhaseReport completedPhaseReport)
{
    foreach (var testCase in upcomingPhase.Cases)
    {
        foreach (var entry in testCase.Team0.Units)
        {
            if (entry.MonsterId.StartsWith("__DYNAMIC:"))
            {
                entry.MonsterId = DynamicResolver.Resolve(entry.MonsterId, completedPhaseReport);
                entry.Origin = CaseOrigin.SystemResolved;
            }
        }
        testCase.Label = RegenerateLabel(testCase);
    }
}
```

UI 提示：

```
✅ 阶段 1 完成！creeper 排名第一 (胜率 72%)
🔄 阶段 2 用例已更新:
   case_2_1: creeper ×1 vs warden ×1 (原: [动态] vs warden ×1)
[ 继续执行阶段 2 ]
```

### 10.6 单场战斗执行流程

```csharp
async Task<MatchReport> ExecuteSingleMatch(TestCase testCase, int runIndex)
{
    // 1. 生成部署
    var deployments = DeployGenerator.Generate(
        testCase.Team0, testCase.Team1,
        testCase.DeployStrategy,
        seed: testCase.SeedBase + runIndex);

    // 2. 注入统计
    var stats = new BattleStatsCollector();

    // 3. 启动战斗（屏幕可见）
    _battleBridge.StartBattle(deployments, _database, stats);

    // 4. 等待结束（每帧检查暂停/跳过）
    while (!_battleBridge.Simulator.IsFinished)
    {
        if (State.SkipRequested) break;
        if (State.IsPaused) { await WaitUntilResumed(); }
        await Task.Yield();
    }

    // 5. 收集战报
    stats.UpdateFinalStats(_battleBridge.Simulator.State.Units,
                           _battleBridge.Simulator.ElapsedTime);
    var report = MatchAnalyzer.BuildReport(testCase, runIndex, stats, _simulator);

    // 6. 增量更新知识库
    _knowledgeBase.UpdateFromMatch(report);

    // 7. 清理战斗
    _battleBridge.StopBattle();

    return report;
}
```

---

## 11. 步骤 6：测试结束 — AI 分析与存档

### 11.1 触发时机

| 情况 | 行为 |
|------|------|
| 全部 Phase 正常完成 | 自动进入分析 |
| 用户停止（部分完成） | 弹窗：「已完成的 11/85 场是否仍生成报告？」 |
| 执行出错 | 标记 Failed，已完成的仍分析 |

### 11.2 分析流水线

```
执行结束
    → 本地数据聚合 (PhaseAnalyzer)
    → 生成各 PhaseReport
    → BalanceCalculator 计算指标
    → LLM 逐 Phase 分析
    → LLM Session 总结
    → 更新 KnowledgeBase
    → 持久化存档
    → 通知 UI
```

### 11.3 SessionReport 最终结构

```csharp
public class SessionReport
{
    public string SessionId;
    public string PlanTitle;
    public string UserRequest;
    public DateTime StartTime, EndTime;
    public float TotalDuration;

    public ExecutionSummary Execution;
    public List<PhaseReport> PhaseReports;

    public StrategyReport Strategy;
    public BalanceReport Balance;

    public string LLMNarrative;
    public List<string> KeyFindings;
    public List<string> FollowUpSuggestions;

    public string PlanSnapshotPath;
    public string RawMatchDataPath;
}

public class PhaseReport
{
    public int PhaseId;
    public string PhaseName;
    public TestScenario Scenario;
    public int TotalMatches;
    public Dictionary<string, UnitPhaseStats> UnitAggregates;
    public List<MatchupResult> MatchupMatrix;
    public List<UnitRanking> Rankings;
    public string LLMSummary;
    public List<string> KeyFindings;
    public List<string> TacticalNotes;
}

public class StrategyReport
{
    public List<UnitRanking> PowerRankings;
    public Dictionary<string, string> UnitRoles;
    public List<CounterRelation> CounterMatrix;
    public List<LineupRecommendation> TopLineups;
    public string LLMNarrative;
}

public class BalanceReport
{
    public List<BalanceMetric> AllUnits;
    public List<BalanceMetric> Overpowered;
    public List<BalanceMetric> Underpowered;
    public List<BalanceSuggestion> Suggestions;
    public string LLMNarrative;
}

public class BalanceSuggestion
{
    public string MonsterId;
    public string Field;           // "hp", "attack", "price", "attackInterval"
    public float CurrentValue;
    public float SuggestedValue;
    public float ChangePercent;
    public string Reason;
    public float Confidence;
    public int SampleSize;
}
```

### 11.4 平衡指标计算公式

```
ExpectedPower = f(price)
  简单版: ExpectedPower = price / avg_price * avg_win_rate

ActualPower = weighted_avg(win_rate_when_used, dps_per_gold, damage_share, survival_rate)

PowerDelta = ActualPower - ExpectedPower

Status:
  PowerDelta > +0.15  → Overpowered
  PowerDelta < -0.15  → Underpowered
  else                → Balanced

Confidence = min(1.0, sample_count / 30)
```

---

## 12. 步骤 7：历史记录与对话引用

### 12.1 历史记录浏览 UI

```
┌─ 测试历史 ──────────────────────────────────────────────────────┐
│ 搜索: [ creeper          ] 🔍   筛选: [全部▼] [已完成▼]          │
├─────────────────────────────────────────────────────────────────┤
│ 📋 20G近战强度评估与跨价对比                                       │
│ 2026-07-22 15:30 | 85/85 场 | ✅ 完成                            │
│ 发现: creeper 72% 胜率, warden 对 creeper 80% 胜率              │
│ [ 查看报告 ]  [ 在对话中引用 ]  [ 导出 ]  [ 删除 ]                │
├─────────────────────────────────────────────────────────────────┤
│ 📋 1000G 实战模拟 #3                                              │
│ 2026-07-21 20:15 | 42/50 场 | ⚠️ 用户中止                         │
│ [ 查看报告 ]  [ 在对话中引用 ]  [ 导出 ]  [ 删除 ]                │
└─────────────────────────────────────────────────────────────────┘
```

### 12.2 报告查看 UI

```
┌─ 测试报告: 20G近战强度评估 ──────────────────────────────────────┐
│ [ 概览 ]  [ 阶段详情 ]  [ 原始数据 ]  [ 平衡建议 ]                  │
├─────────────────────────────────────────────────────────────────┤
│ 🤖 AI 总结: "Creeper 以 72% 胜率稳居第一…"                         │
│ 🏆 排名                    ⚔️ 克制关系                            │
│ 1. creeper (72%)           warden > creeper (80%)               │
│ ⚖️ 平衡建议                                                      │
│ deep_one attack 3→5 (+67%, 置信度 0.7)                            │
└─────────────────────────────────────────────────────────────────┘
```

### 12.3 对话引用机制

用户在对话中引用历史记录，AI 规划时可访问该记录的完整上下文。

**引用 UI：**

```
📎 已引用: session_20260722_153000
   "20G近战强度评估" · 85场 · creeper 72%
   [ 查看摘要 ]  [ 移除引用 ]
```

**引用数据结构：**

```csharp
public class SessionReference
{
    public string SessionId;
    public SessionSummary Summary;
    public SessionReport Report;            // 按需加载
    public TestPlan Plan;
    public List<PhaseReport> PhaseReports;
}
```

**引用注入 LLM 的方式：**

```
[Referenced Session: session_20260722_153000]
标题: 20G近战强度评估与跨价对比
关键发现:
- creeper 72% 胜率, 20G 近战第一
- warden > creeper (80%)
平衡建议:
- deep_one attack 3→5 (+67%)

用户现在说: "基于上次测试结果，帮我设计一个验证 creeper 调价的测试"
→ 你应引用上述数据，设计针对性的新测试计划
```

**自然语言自动识别引用：**

```csharp
// 匹配 "上次" / "之前" / session_id / 标题关键词 / "20G测试"
public List<string> DetectSessionReferences(string text, List<SessionSummary> history);
```

---

## 13. 知识库设计

### 13.1 双轨结构

**轨道 A：策略知识（StrategyKnowledge）**

- 单位强度排名（综合 win rate + DPS/gold）
- 单位定位标签（Tank / DPS / AOE / Assassin / Support）
- 克制关系矩阵
- 历史最佳阵容
- LLM 归纳的战术笔记

**轨道 B：平衡性知识（BalanceKnowledge）**

- 单位表现 vs 价格预期
- 超模/弱势列表
- 调整建议（含置信度）

### 13.2 知识更新规则

每场比赛结束后增量更新：

```csharp
public void UpdateFromMatch(MatchReport report)
{
    // 策略轨: 记录出场、胜负、伤害、克制
    // 平衡轨: 记录 winRate, dpsPerGold, damageShare vs 价格预期
}
```

### 13.3 持久化

```
Assets/Resources/LabArchive/
├── index.json
├── knowledge/
│   ├── strategy_knowledge.json
│   └── balance_knowledge.json
└── sessions/
    └── session_YYYYMMDD_HHMMSS/
        ├── meta.json
        ├── plan.json
        ├── report.json
        ├── report.md
        └── matches/
```

---

## 14. LLM 调用架构

### 14.1 五个调用点

| 调用点 | 时机 | 输入 | 输出 |
|--------|------|------|------|
| **PlanGeneration** | 用户提交需求后 | 需求 + 怪物目录 + 知识库摘要 | TestPlan JSON |
| **LineupSelection** | FullShopMatch 场景 | 金币 + 知识库 + 对手信息 | 阵容 JSON |
| **MatchAnalysis** | 每场战斗后（可选） | 战报 + 场景上下文 | 一句话点评 |
| **PhaseAnalysis** | 每个阶段完成后 | PhaseReport 聚合数据 | 排名 + 发现 + 建议 |
| **SessionSummary** | 全部完成后 | 所有 PhaseReport | 最终双轨报告 |

### 14.2 成本控制

| 策略 | 做法 |
|------|------|
| 单局分析可选 | 默认关闭，仅 Phase/Session 级别调用 LLM |
| 知识库摘要 | 只传 top-20 强/弱 + 最近 5 条发现 |
| 本地预分析 | 排名、win rate 由代码算好，LLM 只做解读 |
| 批量合并 | 75 场矩阵完成后一次性分析 |

---

## 15. UI 设计

### 15.1 实验室入口

主菜单新增按钮：**「⚗️ 平衡实验室」**，进入独立 UI 层（不干扰正常游戏流程）。

### 15.2 整体布局

```
┌──────────────────────────────────────────────────────────────────┐
│  ⚗️ MC Fight 平衡性实验室                          [返回主菜单]   │
├────────────────────────────┬─────────────────────────────────────┤
│  💬 需求对话               │  🎮 战斗视图 / 📋 计划预览 / 📊 报告    │
│  (左侧面板，可切换)         │  (右侧主区域，根据阶段切换)             │
├────────────────────────────┼─────────────────────────────────────┤
│  📊 实时统计 / 📝 最新分析  │  控制栏: [▶][⏸][⏭][⏹]              │
└────────────────────────────┴─────────────────────────────────────┘
```

### 15.3 局间过渡

```
┌─────────────────────────────────────────┐
│  ⚔️ 第 12/75 场 结束                      │
│  creeper (蓝) 胜 deep_one (红)           │
│  耗时: 8.3s | creeper DPS: 6.1           │
│  💬 "creeper 自爆在 1v1 中几乎瞬杀"       │
│  下一场: creeper vs stray                 │
│  [ ▶ 继续 (2s) ]  [ ⏭ 跳过暂停 ]         │
└─────────────────────────────────────────┘
```

---

## 16. 全局状态机

```csharp
public enum LabSessionPhase
{
    Idle,
    RequirementInput,
    Clarifying,
    Researching,
    PlanDraft,
    PlanEditing,
    Confirming,
    Executing,
    Paused,
    Analyzing,
    ReportReady,
    Aborted,
    HistoryBrowsing,
    ReportViewing,
}
```

状态流转：

```
Idle → RequirementInput → Clarifying ↔ RequirementInput
     → Researching → PlanDraft ↔ PlanEditing
     → Confirming → Executing ↔ Paused
     → Analyzing → ReportReady → Idle

Executing → Aborted → Analyzing（可选）/ Idle

Idle → HistoryBrowsing → ReportViewing → RequirementInput（引用）
```

---

## 17. 文件结构

```
Assets/Scripts/BalanceLab/
├── Core/
│   ├── LabSession.cs
│   ├── LabSessionController.cs
│   ├── LabSessionConfig.cs
│   ├── LabSessionPhase.cs
│   └── LabEvents.cs
│
├── Planning/
│   ├── TestPlanner.cs
│   ├── PlanValidator.cs
│   ├── PlanEnricher.cs
│   ├── IntentPreview.cs
│   ├── ResearchContext.cs
│   ├── TestPlan.cs
│   ├── TestPhase.cs
│   ├── TestCase.cs
│   └── ScenarioConfigs/
│       ├── FullShopMatchConfig.cs
│       ├── PriceTierDuelConfig.cs
│       ├── UnitVsUnitConfig.cs
│       └── ...
│
├── Execution/
│   ├── TestExecutor.cs
│   ├── CaseGenerator.cs
│   ├── LineupGenerator.cs
│   ├── DeployGenerator.cs
│   ├── BattleRunner.cs
│   ├── DynamicResolver.cs
│   └── ExecutionQueue.cs
│
├── Analysis/
│   ├── MatchAnalyzer.cs
│   ├── PhaseAnalyzer.cs
│   ├── SessionAnalyzer.cs
│   ├── BalanceCalculator.cs
│   └── Reports/
│       ├── MatchReport.cs
│       ├── PhaseReport.cs
│       └── SessionReport.cs
│
├── Knowledge/
│   ├── KnowledgeBase.cs
│   ├── StrategyKnowledge.cs
│   ├── BalanceKnowledge.cs
│   └── KnowledgePersistence.cs
│
├── LLM/
│   ├── LLMClient.cs
│   ├── PromptTemplates.cs
│   └── LLMResponseParser.cs
│
└── UI/
    ├── BalanceLabUI.cs
    ├── RequirementChatUI.cs
    ├── PlanPreviewUI.cs
    ├── CaseEditorUI.cs
    ├── UnitPickerUI.cs
    ├── LiveStatsUI.cs
    ├── MatchTransitionUI.cs
    ├── ReportViewerUI.cs
    └── HistoryBrowserUI.cs
```

---

## 18. 持久化结构

### 18.1 index.json

```json
{
  "sessions": [
    {
      "session_id": "session_20260722_153000",
      "title": "20G近战强度评估与跨价对比",
      "user_request": "测试20G近战 + 和35G对比",
      "created_at": "2026-07-22T15:30:00",
      "status": "Completed",
      "total_matches": 85,
      "completed_matches": 85,
      "key_findings": ["creeper 在 20G 近战中最强", "warden 对 creeper 有显著克制"],
      "tags": ["price_tier", "melee", "creeper", "warden"]
    }
  ]
}
```

### 18.2 单次 Session 目录

```
sessions/session_20260722_153000/
├── meta.json          # SessionReport（不含原始 match）
├── plan.json          # 最终 TestPlan 快照
├── report.json        # 完整 SessionReport
├── report.md          # LLM 叙事（Markdown）
└── matches/
    ├── phase_1/
    │   ├── case_1_1_run_0.json
    │   └── ...
    └── phase_2/
        └── ...
```

---

## 19. 与现有代码集成改造

### 19.1 必须改造

| 文件 | 改造内容 |
|------|---------|
| `BattleSimulator.cs` | `InjectedStatsCollector` 替代 `GameManager.Instance?.StatsCollector` |
| `BattleBridge.cs` | `StartBattle` 接受注入 stats + `OnBattleFinished` 回调 |
| `GameManager.cs` | 新增入口或独立 `LabSessionController`，不干扰正常流程 |

### 19.2 直接复用

| 组件 | 用途 |
|------|------|
| `BattleSimulator` | 战斗逻辑 |
| `BattleBridge` | 屏幕渲染 |
| `BattleStatsCollector` | 赛后统计 |
| `MonsterDatabase` + `monster_config.json` | 单位数据 |
| `GameManager.AIBuyTeam` / `AIDeployTeam` | LLM 不可用时的 fallback |
| `DeployGenerator`（基于现有 AutoDeploy 逻辑） | 部署位置生成 |

---

## 20. 关键设计决策

| # | 决策点 | 建议 |
|---|--------|------|
| 1 | LLM 提供商 | 先做 DeepSeek 或 OpenAI，JSON mode 稳定 |
| 2 | API Key 存储 | UI 设置面板输入，存 PlayerPrefs |
| 3 | 知识库范围 | **跨会话积累**，Planner 越用越准 |
| 4 | 单局 LLM 分析 | **仅 Phase 级**，节省成本 |
| 5 | 失败处理 | API 失败重试 3 次，然后暂停等用户 |
| 6 | 独立场景 vs 叠加 | **现有 BattleScene 叠加 UI 层** |
| 7 | 部署可视化 | 测试模式**跳过部署 UI**，直接生成位置开战 |
| 8 | 调整建议执行 | 先**仅建议 + 导出**，不自动改数值 |

---

## 21. 用户请求示例库

| 用户说法 | 解析为 |
|---------|--------|
| "测试所有 20 金币近战" | PriceTierDuel, price=20, melee, matrix |
| "warden 强不强" | BossStressTest + FullShopMatch 含 warden |
| "帮我找最强阵容" | FullShopMatch × 20, LLM 选阵, 进化 |
| "creeper 和 blaze 谁克制谁" | UnitVsUnit, creeper vs blaze × 10 |
| "飞行单位是不是太强了" | TagMatchup, move=Fly vs move=Ground |
| "全面平衡性检查" | 多 Phase：各价格档矩阵 + 跨档 + 1000G 实战 |
| "新怪物 xxx 强度如何" | UnitVsUnit(xxx, 同价对手) + 加入 FullShopMatch |
| "基于上次20G测试结果，给 creeper 涨价后再测" | 引用历史 + 修改 monster_config + 重跑矩阵 |

---

## 22. 实施分期

| 阶段 | 内容 | 核心交付 | 预估 |
|------|------|---------|------|
| **P0 骨架** | LabSession 状态机 + BattleStats 解耦 + 硬编码计划跑通 3 场 | 能看到战斗 + 暂停/停止 | 2-3 天 |
| **P1 计划系统** | TestPlan 数据结构 + PlanEditor UI + CaseEditor + 校验器 | 用户能编辑/增删 case | 2-3 天 |
| **P2 调研+规划** | ResearchContext 组装 + LLM PlanGeneration + 需求对话框 | 自然语言 → 结构化计划 | 2-3 天 |
| **P3 完整执行** | ExecutionQueue + 跳过/暂停 + 动态占位符 + 进度 UI | 完整执行控制 | 2-3 天 |
| **P4 分析+存档** | PhaseAnalyzer + LLM 分析 + 持久化 + 报告 UI | 自动生成报告并保存 | 2-3 天 |
| **P5 历史+引用** | 历史浏览 + 对话引用 + 跨会话知识库 | 引用历史设计新测试 | 2-3 天 |

---

## 附录 A：数据流总图

```
用户输入需求
    → IntentPreview (本地)
    → ResearchContext 组装 (本地)
    → LLM PlanGeneration
    → PlanValidator + Enricher
    → TestPlan Draft
    → 用户编辑 (CaseEditor)
    → 用户确认
    → ConfirmedTestPlan + ExecutionQueue
    → ExecuteSingleMatch × N (BattleBridge 可视化)
    → MatchReport × N
    → PhaseAnalyzer (本地聚合)
    → LLM PhaseAnalysis
    → BalanceCalculator
    → LLM SessionSummary
    → SessionReport
    → 持久化 + 更新 KnowledgeBase
    → 用户查看 / 引用到下次对话
```

---

## 附录 B：相关现有代码

| 文件 | 说明 |
|------|------|
| `Assets/Scripts/Simulation/BattleSimulator.cs` | 纯逻辑战斗模拟器 |
| `Assets/Scripts/View/BattleBridge.cs` | 战斗桥接与渲染 |
| `Assets/Scripts/Simulation/BattleStatsCollector.cs` | 战斗统计收集 |
| `Assets/Scripts/GameFlow/GameManager.cs` | 游戏流程（Shop/Deploy/Battle） |
| `Assets/Scripts/Data/MonsterDatabase.cs` | 怪物数据库 |
| `Assets/Resources/monster_config.json` | 怪物数值配置 |
| `Assets/Scenes/BattleScene.unity` | 主战斗场景 |

---

## 修订记录

| 版本 | 日期 | 变更内容 |
|------|------|---------|
| v0.1 | 2026-07-21 | 初始设计稿 |
| v0.2 | 2026-07-22 | 完善 LLM 规划流程、TestPlan 数据结构、UI 设计、分期计划 |
| **v0.3** | **2026-07-29** | **新增 §7.4「测试计划 JSON 文件格式规格」：固定 JSON Schema (BalanceLabTestPlan/v1)，定义文件命名、字段、校验规则、导入流程；更新 §7.2 LLM Prompt 引用新格式；更新 §7.3 后处理映射表** |
