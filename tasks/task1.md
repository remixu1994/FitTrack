# FitTrack 重构总方案：Next.js 前端 + .NET 10 Agent 后端

## Summary

以 `FitTrack.Copilot` 作为唯一主线后端，升级并收敛为 `.NET 10` 的 API + Agent Host；新增一个从 `D:\GitHub\CarbCycle` 迁移而来的 `Next.js + React` 前端主应用，彻底退出 Blazor/MudBlazor 在主链路中的职责。`FitTrack` 保留为 legacy/reference，不再承接新能力，但不删除、不覆盖现有 SQLite。

本次设计目标不是“把旧页面换皮”，而是建立一套新的双端架构：
- 前端负责会话体验、工作台、可视化卡片、认证态消费。
- 后端负责 Identity、业务数据、Agent 编排、流式响应、外部 AI/USDA 集成。
- Agent 采用 `Supervisor + 专家 Agent` 的多轮对话模式。
- `Microsoft.Agents.*` 按实施时官方最新可安装预览/RC 版本接入，不等待假设中的 GA `1.0.0`。

## Architecture

### 1. 仓库主线与目录形态

保留现有 solution，但新增独立前端应用，形成双运行时而不是双后端产品线：

- `FitTrack/`：legacy，不做新功能，只保留可运行与参考价值。
- `FitTrack/FitTrack.Copilot/`：重构为主后端，继续用现有 SQLite 与 Identity 基础。
- `FitTrack.Web/`：新建 Next.js 前端，迁移 `CarbCycle` 的 UI 结构、聊天体验和会话模型。
- 根文档 `AGENTS.md` / `CONTEXT.md` / `ARCHITECTURE.md` / `DECISIONS.md` 同步更新为新主线。

后端不再承担 Razor UI 责任。`FitTrack.Copilot` 的 `Components/*`、MudBlazor 和 Blazor Server 渲染链路进入淘汰路径；短期允许并存，最终主入口应只保留 API/Auth/Agent Host。

### 2. 后端目标形态：.NET 10 API + Agent Host

`FitTrack.Copilot` 重构为 ASP.NET Core 10 Web API，职责分 5 层：

- `Api`
  - Minimal API 或 Controllers 暴露认证、聊天、线程、档案、饮食、训练接口。
  - 聊天接口必须支持流式输出。
- `Application`
  - 用例编排层，承接前端请求，协调 Agent、领域服务、仓储和外部 API。
- `Domain`
  - 聚合 `UserProfile`、`ConversationThread`、`ChatMessage`、`NutritionSnapshot`、`FoodRecord`、`WorkoutPlan`、`WorkoutSession`。
- `Infrastructure`
  - EF Core SQLite、Identity、USDA、AI provider、文件存储、日志、缓存。
- `Agents`
  - Microsoft Agents Framework 的 agent、workflow、tool、memory、router。

`Program.cs` 需要收缩成标准 API Host：
- 保留 Identity、EF Core、user-secrets、OpenAPI、日志、CORS。
- 新增 JWT + refresh token 认证。
- 新增 ProblemDetails、Rate Limiting、Health Checks。
- 移除 Razor Components、MudBlazor、Blazor 相关注册。

### 3. Agent 设计：Supervisor + 专家 Agent

统一入口为 `CoachSupervisorAgent`，前端始终只和一个会话入口交互。Supervisor 根据意图、上下文和工具可用性路由到下列专家 agent：

- `NutritionAgent`
  - 饮食分析、宏量营养建议、USDA 查询、食物记录建议。
- `WorkoutAgent`
  - 训练计划、动作说明、训练日志总结。
- `VisionNutritionAgent`
  - 图像识别食物、估算热量、返回置信度和结构化营养项。
- `ProgressCheckInAgent`
  - 周/月复盘、体重与围度趋势、依从性提醒、下一步建议。

多轮对话约束：
- 所有消息落在线程级会话存储，不做“无状态单问单答”。
- Agent 每轮都读取最近消息摘要 + 当前用户档案 + 最近饮食/训练快照。
- 需要补参数时必须追问，不允许对关键体征、目标、单位进行默认臆测。
- 专家 agent 输出统一结构化 envelope，Supervisor 负责把文本响应和卡片载荷组合成前端事件流。

### 4. 前端目标形态：迁移 CarbCycle 的 UI 和会话模型

`FitTrack.Web` 采用：
- Next.js App Router
- React 19
- TypeScript
- Tailwind CSS
- Zod
- TanStack Query
- 原生流式读取 `fetch`/`ReadableStream` 处理聊天事件

迁移原则：
- 迁 `CarbCycle` 的聊天工作台、线程列表、消息流、结构化卡片交互。
- 不迁 Prisma、Next API Route 内部持久化逻辑；这些全部改为调用 .NET API。
- 视觉上保留“单主工作区 + 左侧线程 + 右侧上下文面板”的强工作台结构，但内容改成 FitTrack 的健身/饮食语义。
- 不继续使用 MudBlazor，也不做“组件库平移”；前端直接以 React 组件为主。

前端页面最少包含：
- `/login`
- `/chat`
- `/food-records`
- `/workouts`
- `/progress`
- `/settings/profile`

## Key Changes

### 1. 数据模型与迁移策略

后端新增或重构以下核心实体，并用 EF Core migration 增量落地，不删除现有 `Data/app.db`：

- `UserProfile`
  - 1:1 关联 `AspNetUsers`
  - 存储性别、年龄、身高、体重、体脂、活动水平、目标、偏好
- `ConversationThread`
  - 用户线程、标题、时间戳、归档状态
- `ChatMessage`
  - `role`、`kind`、`contentText`、`contentJson`、`turnIndex`
- `NutritionSnapshot`
  - 用于日计划、餐次分析、每日总结等结构化卡片
- 保留并升级现有：
  - `FoodRecord`
  - `WorkoutPlan`
  - `WorkoutSession`
  - `FitnessGoal`

迁移规则：
- 不覆盖现有表，优先新增新表与新列。
- 原 `ChatSession` / `ChatSessionMessage` 进入废弃路径，不再作为主链路模型。
- 需要数据迁移脚本时采用“一次性搬运到新会话表”的方式，而不是重命名数据库文件。

### 2. API 契约

后端公开以下稳定接口组：

- 认证
  - `POST /api/auth/login`
  - `POST /api/auth/refresh`
  - `POST /api/auth/logout`
  - `GET /api/auth/me`
- 用户档案
  - `GET /api/profile`
  - `PUT /api/profile`
- 聊天线程
  - `GET /api/chat/threads`
  - `POST /api/chat/threads`
  - `GET /api/chat/threads/{threadId}`
  - `DELETE /api/chat/threads/{threadId}`
- 消息流
  - `POST /api/chat/messages`
  - 返回 `application/x-ndjson` 或 SSE，事件类型固定为：
    - `user_message`
    - `assistant_message`
    - `snapshot`
    - `tool_event`
    - `error`
    - `done`
- 饮食
  - `GET /api/foods/search`
  - `POST /api/food-records`
  - `GET /api/food-records`
  - `POST /api/food/analyze`
- 训练
  - `GET /api/workout-plans`
  - `POST /api/workout-plans`
  - `POST /api/workout-sessions`
  - `GET /api/workout-sessions`
- 进度
  - `GET /api/progress/summary`

消息与卡片的公共 DTO 统一成：
- `ConversationThreadDto`
- `ChatMessageDto`
- `NutritionSnapshotDto`
- `AgentResponseEnvelope`
- `ApiResponse<T>`

### 3. Agent 与工具接口

每个专家 agent 只通过明确工具访问业务能力，不直接操作 DbContext：

- `INutritionTools`
- `IWorkoutTools`
- `IProgressTools`
- `IVisionTools`
- `IConversationMemory`

Supervisor 负责：
- 意图分类
- agent 选择
- 工具调用策略
- 失败降级
- 结构化卡片拼装

工具层负责：
- USDA 查询
- 食物识别 orchestration
- 训练计划读写
- 用户档案读取
- 历史快照汇总

### 4. 认证与安全

认证方案固定为：
- ASP.NET Core Identity 管用户
- JWT access token + refresh token 供前端消费
- Next.js 不持有数据库访问权
- CORS 仅允许前端域名
- 上传图片走后端受控接口，不让前端直接把 base64 永久落库

前端会话策略：
- access token 短时有效
- refresh token httpOnly
- 所有受保护页面依赖 `/api/auth/me` 校验

## Test Plan

### 1. 后端

- 单元测试
  - Supervisor 路由到正确专家 agent
  - tool 输入校验与失败回退
  - DTO/事件流序列化
- 集成测试
  - 登录、刷新、登出
  - 创建线程、发消息、流式返回
  - 图片分析请求的错误分支
  - USDA/AI provider 失败时返回可恢复错误
- 数据测试
  - migration 对现有 SQLite 可增量执行
  - 不删除现有 `app.db`
  - 旧会话表存在时新表仍能正常工作

### 2. 前端

- 组件测试
  - 线程列表切换
  - 消息流增量渲染
  - 卡片组件渲染 `NutritionSnapshotDto`
  - 登录态失效后的自动跳转
- 端到端测试
  - 登录 -> 创建线程 -> 连续多轮对话 -> 收到结构化卡片
  - 上传餐食图片 -> 返回识别结果
  - 记录训练 -> 刷新后进度面板更新
  - token 过期后 refresh 成功续会话

### 3. 验收场景

- 用户首次登录后填写身体档案，Agent 能在后续多轮对话中引用该档案。
- 用户连续 3 轮提问饮食与训练，线程上下文不丢失。
- 用户上传食物图片时，系统返回文本结论 + 卡片化营养项。
- 用户要求“按减脂目标做一周计划”时，Supervisor 能调起 Nutrition/Workout 两类能力并汇总成统一回复。
- 前端完全不依赖 MudBlazor/Blazor 页面即可完成主流程。

## Assumptions

- 主线后端是 `FitTrack.Copilot`，`FitTrack` 保留但不再扩展。
- `Microsoft.Agents.*` 以实施时官方最新可安装预发布版本为准，不阻塞于 GA 包名或最终 API 微差异。
- `CarbCycle` 迁移的是前端工作台与会话模型，不迁 Prisma 和 Next 侧持久化。
- `.NET 10` 是后端统一目标版本；前端使用当前 `CarbCycle` 所在 Next.js/React 主版本线。
- 数据库继续使用 SQLite 起步，但从现在开始按“可迁到更强数据库”的接口边界设计，不把前端或 agent 直接绑死在 SQLite 特性上。
