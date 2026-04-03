# CONTEXT.md

## Python Sidecar Status

- Phase 1 已新增 `FitTrack.AgentHost.Python/` 作为 Python agent host。
- 当前形态是本地开发 sidecar，不是生产级独立服务。
- `FitTrack.Copilot` 新增了可切换的 Python chat provider，并保留进程内 `.NET` fallback。
- 业务真相源仍是 `.NET` + SQLite；Python trace 只用于调试和观测。
- Python 侧通过 `/internal/agent-tools/*` 调用 `FitTrack.Copilot` 的内部业务能力，而不是直接访问数据库。

## 当前项目状态

这是一个仍处于重构收口阶段的健身/饮食追踪仓库，目前有三条实现线:

1. `FitTrack`
   旧的 Blazor + Identity 基础版，保留为 legacy/reference。
2. `FitTrack.Copilot`
   `.NET 10` 的 AI 后端主线，负责 API、Identity、Agent 编排和数据库写入。
3. `FitTrack.Web`
   独立的 Next.js 前端工作台，承接主用户体验和线程式聊天。

## 已确认的现状

### `FitTrack`

- 技术栈: `.NET 9 + Razor Components + Interactive Server + Identity + EF Core Sqlite + MudBlazor`
- 当前真正落地的业务模型:
  - `Food`
  - `DailyFoodRecord`
- 当前 UI 现状:
  - 首页仍保留模板内容
  - 导航菜单仍有模板项
  - `/food` 页面能读取并展示食物列表
- 启动时自动迁移数据库，并在 `Foods` 为空时从 `wwwroot/foods.json` 导入默认食物库
- 认证路由已启用，页面默认通过 `AuthorizeRouteView` 受保护

### `FitTrack.Copilot`

- 技术栈:
  - `.NET 10`
  - OpenAPI / Swagger UI
  - NLog
  - Semantic Kernel
  - Microsoft.Extensions.AI
  - Microsoft.Agents.AI
  - Microsoft.Agents.AI.Workflows
  - USDA API 客户端
- 当前已落地的主能力:
  - JWT + refresh token 认证 API
  - `ConversationThread` / `ConversationMessage` / `NutritionSnapshot`
  - `UserProfile` / `RefreshToken`
  - Supervisor + 专家 agent 骨架
  - `/api/*` 聊天、档案、饮食、训练、进度端点
- `FitTrack.Copilot/Components/*` 仍保留部分旧 UI，但主交付路径已经切到 API + Agent Host。

### `FitTrack.Web`

- 技术栈: `Next.js 16 + React 19 + TypeScript + Tailwind + TanStack Query`
- 当前已落成页面:
  - `/login`
  - `/chat`
  - `/food-records`
  - `/workouts`
  - `/progress`
  - `/settings/profile`
- 当前直接调用 `FitTrack.Copilot` 的 `/api/*`，不再使用 Next 本地 API Route + Prisma
- 前端壳层已经承担会话检查、401 刷新和线程式聊天工作台

## 当前主要问题

### 1. 旧前端仍未完全退场

- `FitTrack.Copilot/Components/*` 仍在项目内编译
- MudBlazor/Blazor UI 还没有从工程层面完全移除
- 当前主交付物已经变成 `FitTrack.Web + FitTrack.Copilot API`

### 2. 主项目业务完成度偏低

- 主应用的数据模型已经开始偏向真实业务
- 但页面结构、导航、首页内容仍大量保留模板痕迹
- `DailyFoodRecord` 已存在，但主项目里还没有完整的录入和统计流程页面

### 3. Agent/依赖治理仍需继续

- AI 配置较多，但密钥全部留空
- `Microsoft.Agents.AI` 已升级到稳定 `1.0.0`
- `Microsoft.Agents.AI.Workflows` 当前仍停在公开 `1.0.0-rc4`
- 外部依赖增多后，仓库级运行说明、错误处理规范、测试策略仍需补齐

### 4. 测试和运维文档不足

- solution 中未看到测试项目
- 当前没有明确的:
  - 测试策略
  - 发布流程
  - 数据迁移规范
  - 配置分层说明

## 建议的短期方向

### 如果主线是稳定交付

- 以 `FitTrack` 为主
- 补齐食物记录录入、每日汇总、基础统计
- 清理模板页面和导航
- 把 Copilot 能力作为后续增量集成，而不是并行主产品

### 如果主线是 AI 产品探索

- 以 `FitTrack.Copilot` 为主
- 把主项目降级为 legacy/reference
- 统一数据模型、运行说明和配置策略
- 补测试与失败兜底，尤其是外部 AI/USDA 调用

## 本次分析边界

本文件基于当前仓库代码和配置得出，不包含以下未经代码证实的假设:

- 不假设项目已经进入生产
- 不假设 AI 配置在任何环境中已可用
- 不假设两个项目会自动共享数据库 schema
- 不假设主项目与 Copilot 项目未来一定合并
