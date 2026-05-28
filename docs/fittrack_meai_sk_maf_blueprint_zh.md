# FitTrack AI Agent 技术蓝图（MEAI + SK + MAF）

## 1. 架构目标

构建模块化多智能体系统，职责清晰：

- `MAF` 负责 Agent 拓扑与编排流程。
- `MEAI` 负责模型与工具调用的统一抽象（`IChatClient`、`ITool`）。
- `Semantic Kernel` 负责可复用的领域插件、提示词模板与任务规划。

## 2. 分层职责

### 2.1 MAF（智能体编排层）

- 将用户意图路由到正确的领域 Agent。
- 基于图工作流执行任务，支持检查点与重试。
- 管理会话状态与跨 Agent 的上下文交接。

### 2.2 MEAI（模型抽象层）

- 提供统一聊天模型接口，便于切换模型供应商。
- 通过中间件链实现日志、安全过滤、性能监控。
- 为各 Agent 提供统一 Tool Calling 能力。

### 2.3 Semantic Kernel（领域智能层）

- 封装结构化领域插件：
  - 营养推理
  - 训练进阶逻辑
  - 行为引导策略
- 维护提示词模板与策略提示词。
- 在多步骤任务中可选启用 Planner。

## 3. 建议的 Agent 拓扑

- `OrchestratorAgent`
  - 意图识别
  - 路由任务
  - 聚合最终回复
- `NutritionAgent`
  - 餐食解析
  - 宏量营养估算
  - 餐次纠偏建议
- `WorkoutAgent`
  - 训练计划生成
  - 单次训练动态调整
  - 进阶更新
- `CheckInAgent`
  - 每日准备度打分
  - 触发临时调整规则
- `SafetyAgent`
  - 响应前后护栏检查
  - 风险请求拦截

## 4. Skill 与 Tool 设计

### 4.1 SK Plugins（业务逻辑）

- `ProfilePlugin`
- `GoalPlugin`
- `NutritionPlugin`
- `WorkoutPlugin`
- `ProgressPlugin`
- `SafetyPolicyPlugin`

要求：所有插件方法优先返回强类型 DTO，而非自由文本。

### 4.2 MEAI Tools（执行接口）

- 将 SK Plugin 包装为 MEAI Tool，供 Agent 统一调用。
- 外部服务也通过 Tool 接入：
  - USDA / OpenFoodFacts 查询
  - 图像识别服务
  - 可穿戴数据读取（后续）

### 4.3 MAF Skills（组合流程）

- `GenerateDailyPlanSkill`
- `AnalyzeMealFromTextSkill`
- `AnalyzeMealFromImageSkill`
- `AdjustPlanByCheckInSkill`
- `GenerateWeeklyReviewSkill`

要求：每个 Skill 具备幂等性、可追踪性。

## 5. 数据与记忆策略

- 短期记忆：会话上下文存储在 MAF 状态中。
- 长期记忆：
  - 用户画像、目标、历史记录存于 EF Core/SQLite（当前）和 Postgres（后续）。
  - 生成进展摘要，便于检索与复用。
- RAG 层：
  - 可信营养/训练知识库。
  - 可版本化的策略文档与提示词文档。

## 6. 端到端工作流

### 6.1 文本饮食记录

1. 用户输入餐食描述。
2. `OrchestratorAgent` 路由至 `NutritionAgent`。
3. `NutritionAgent` 调用 `AnalyzeMealFromTextSkill`。
4. Skill 调用 `NutritionPlugin` 与食物数据库工具。
5. 保存结构化记录并更新当日营养汇总。
6. 返回简洁反馈与下一步建议。

### 6.2 当日训练引导

1. 用户进入训练会话。
2. `WorkoutAgent` 读取当前计划与准备度信号。
3. 调用进阶 Skill 生成当天训练细节。
4. 训练中根据用户反馈动态调整动作或负重。
5. 会话结束后更新后续计划参数。

### 6.3 安全拦截流程

1. 所有请求先经过 `SafetyAgent` 预检。
2. 若识别到风险，阻断不安全输出路径。
3. 返回安全替代建议与必要的专业求助提示。
4. 记录事件（策略编码、置信度、上下文摘要）。

## 7. 建议项目结构（FitTrack.Copilot）

- `MAF/Agents`
  - `OrchestratorAgent.cs`
  - `NutritionAgent.cs`
  - `WorkoutAgent.cs`
  - `CheckInAgent.cs`
  - `SafetyAgent.cs`
- `MAF/Skills`
  - 组合业务流程
- `SemanticKernel/Plugins`
  - 领域插件（强类型输入输出）
- `AI`
  - ChatClient 工厂与模型配置
- `Middleware`
  - 日志、安全过滤、性能中间件
- `Service`
  - 数据持久化与领域服务
- `Data`
  - 实体、迁移、仓储

## 8. 实施路线图

### Step 1：基础建设（第 1-2 周）

- 定义统一 DTO：用户画像/餐食/训练/打卡。
- 统一模型输出 Schema。
- 完成 MEAI `IChatClient` 与中间件链路接入。

交付物：

- 具备 TraceId 与策略 Hook 的稳定聊天管道。

### Step 2：核心 Agent（第 3-4 周）

- 实现 `OrchestratorAgent`、`NutritionAgent`、`WorkoutAgent`。
- 先落地 3 个高价值 Skill：
  - 文本餐食分析
  - 当日训练生成
  - 每周复盘生成

交付物：

- MVP 主流程端到端可运行。

### Step 3：自适应闭环（第 5-6 周）

- 新增 `CheckInAgent` 与计划调整 Skill。
- 增加执行度打分与干预触发逻辑。

交付物：

- 支持按日动态个性化调整。

### Step 4：安全与质量（第 7-8 周）

- 新增 `SafetyAgent` 与策略规则引擎。
- 建立不安全提示词评测集与回归测试。
- 增加模型回退、超时与重试机制。

交付物：

- 生产可用的安全基线版本。

## 9. 可观测性与评估

- 核心指标：
  - Tool 调用成功率
  - 编排时延
  - 风险请求拦截率
  - 建议采纳率
- 评估套件：
  - 营养提取准确率
  - 训练建议适配度评分
  - 护栏精准率/召回率

## 10. 工程约束原则

- 确定性步骤放入 Tool/Plugin，LLM 输出严格边界化。
- 关键计算场景强制 JSON Schema 输出。
- 所有面向用户的建议必须附带置信度与理由。
- 分离“推理 Agent”和“执行 Tool”，提升可调试性与可维护性。
