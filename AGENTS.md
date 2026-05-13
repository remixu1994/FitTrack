# AGENTS.md

## 1. 仓库事实

- 解决方案文件: `FitTrack.sln`
- 当前包含两个 ASP.NET Core Web 项目:
  - `FitTrack/`: 旧主应用，`net9.0`
  - `FitTrack.Copilot/`: AI 扩展应用，`net10.0`
- 当前还有一个独立前端应用:
  - `FitTrack/FitTrack.React/`: `Next.js + React 19 + TypeScript + Tailwind`
- 现阶段主交付链路是 `FitTrack.React -> FitTrack.Copilot API`，不是 Blazor 主链路。
- `FitTrack` 仍保留为基础版/legacy 参考实现。
- `FitTrack.Copilot` 仍保留部分 Blazor/MudBlazor 代码，但主线已经转为 API + Agent Host。
- 当前仓库存在未提交改动，至少包括:
  - `FitTrack.Copilot/Components/Layout/MainLayout.razor`
  - `FitTrack.sln.DotSettings.user`

## 2. 当前主线判断

- `FitTrack` 是“基础版”饮食记录应用，核心模型仍以 `Food` 和 `DailyFoodRecord` 为主。
- `FitTrack.Copilot` 是“增强版/实验版”产品线，已经接入:
  - AI 对话
  - 图片识别食物
  - USDA 食物数据查询
  - Microsoft.Agents.AI / RAG 相关能力
- 当前新的实施主线是:
  - `FitTrack.Copilot` 作为 `.NET 10` API + Agent Host
  - `FitTrack.React` 作为主前端工作台
- `FitTrack.React` 不是 `.sln` 中的项目，但它是仓库级的一等公民。
- 两条 .NET 产品线与独立前端并行存在，不能默认合并或互相继承能力。

## 3. 协作约束

- 修改代码前先确认要动的是 `FitTrack` 还是 `FitTrack.Copilot`，避免把能力错加到另一条产品线上。
- 前端相关变更优先落在 `FitTrack.React`，不要再把新 UI 加回 `FitTrack.Copilot/Components/*`。
- 不要默认 `FitTrack.Copilot` 的设计已经在主项目落地；当前主项目仍明显接近模板起点。
- 不要删除或覆盖现有 SQLite 文件，尤其是:
  - `FitTrack/Data/app.db`
  - `FitTrack.Copilot/Data/app.db`
- 对 Identity、数据库迁移、种子数据、AI 配置这几类改动，必须同步检查启动流程 `Program.cs`。
- 未经明确确认，不要重构 solution 结构，也不要假设用户要合并两个项目。
- 每次任务完成前，必须执行与本次改动范围对应的 build / 编译校验，通过后才算完成任务:
  - 修改 `FitTrack` 或 `FitTrack.Copilot` 的 `.cs` / `.csproj` / 配置相关代码时，至少运行对应项目的 `dotnet build`
  - 修改 `FitTrack/FitTrack.React` 前端代码时，至少运行 `npm run build`
  - 同时改动前后端时，前后端的 build 都要通过
  - 若因运行中进程锁文件、环境缺失或外部依赖导致无法完成 build，必须在交付时明确说明阻塞原因，不能默认视为已完成

## 4. 本地运行记忆

### 主应用 `FitTrack`

- 启动命令: `dotnet run --project FitTrack`
- 本地地址:
  - `http://localhost:5265`
  - `https://localhost:7146`
- 默认数据库连接: `Data\\app.db;Cache=Shared`
- 首次启动会:
  - 执行 `context.Database.Migrate()`
  - 调用 `DbInitializer.Initialize(...)`
  - 从 `wwwroot/foods.json` 导入默认食物

### Copilot 应用 `FitTrack.Copilot`

- 启动命令: `dotnet run --project FitTrack.Copilot`
- 本地地址:
  - `http://localhost:5097`
  - `https://localhost:7291`
- 除本地 SQLite 外，还依赖配置中的外部能力:
  - `AI`
  - `TokenAI`
  - `USDA`
- 开发机需要 user-secrets 或本地配置提供 API Key，仓库中的 `appsettings.json` 仅是占位。

### 前端应用 `FitTrack.React`

- 启动命令: `cd FitTrack/FitTrack.React; npm install; npm run dev`
- 本地地址:
  - `http://localhost:3000`
- 优先通过环境变量 `NEXT_PUBLIC_COPILOT_PORT` 拼接 `FitTrack.Copilot` API 地址，默认端口是 `5097`
- 若需要指向非本地后端，可改用 `NEXT_PUBLIC_API_BASE_URL`
- 前端主链路是线程式聊天工作台、饮食记录、训练记录和进度看板

### 一键联调脚本

- 启动命令: `.\start-fittrack-dev.ps1`
- 运行位置: 仓库根目录
- 脚本行为:
  - 检查 `dotnet` 和 `npm` 是否可用
  - 若 `FitTrack/FitTrack.React/node_modules` 缺失，则先执行 `npm install`
  - 分别在两个独立 PowerShell 窗口中启动 `FitTrack.Copilot` 和 `FitTrack.React`
  - 后端默认启动在 `http://localhost:5097`，也可通过 `-CopilotPort` 覆盖
  - 通过 `NEXT_PUBLIC_COPILOT_PORT` 将后端端口传给前端
  - 后端使用 `dotnet watch run`，前端使用 `npm run dev`

## 5. 高风险区域

- `Program.cs`: 服务注册、认证、中间件、数据库初始化都在这里。
- `FitTrack/FitTrack.React/src/components/chat/chat-view.tsx`: 新前端主链路聊天工作台。
- `FitTrack/FitTrack.React/src/lib/http.ts`: 前端认证、refresh、401 重试都经过这里。
- `Data/`: EF Core 实体和迁移定义，改动会直接影响 SQLite schema。
- `DbInitializer`: 主项目依赖 `foods.json` 做初始导入。
- `FitTrack.Copilot/Service` 和 `SemanticKernel/`: AI 编排与业务能力耦合较高，改动前先确认调用链。
- `Components/Account/`: 基于 Identity 模板生成，容易因小改动引入登录流程回归。

## 6. 交付文档说明

本仓库新增了 4 个根文档，职责分别是:

- `AGENTS.md`: 记录稳定事实、协作边界和操作记忆
- `CONTEXT.md`: 记录当前项目状态、短板和下一步
- `ARCHITECTURE.md`: 记录结构、数据流和模块边界
- `DECISIONS.md`: 记录已在代码中落地的技术决策

后续如果仓库继续演进，优先更新这 4 个文件，而不是把信息散落到聊天记录里。

## 7. Chat Runtime（2026-05）

- `FitTrack.Copilot` 当前已经移除 Python chat provider、内部 tool API 映射和 sidecar 依赖。
- `FitTrack.Copilot` 当前已经移除对 `Microsoft.SemanticKernel` NuGet 包的运行时依赖，聊天主线以 `Microsoft.Agents.AI` 和 `Microsoft.Extensions.AI` 为主。
- 当前聊天主链路是 `FitTrack.React -> FitTrack.Copilot -> in-process CoachSupervisorAgent`。
- `FitTrack.Copilot` 负责:
  - JWT / refresh cookie
  - conversation thread / message / attachment / snapshot 持久化
  - 对前端输出 NDJSON 聊天流
  - 在进程内编排 `Nutrition / Workout / Vision / Progress` 子 Agent
- 文本聊天现在优先走 `.NET` 进程内流式输出，不再经过 Python fallback。
- 若仓库保留 `FitTrack.AgentHost.Python/` 目录，应视为独立实验资产，而不是 `FitTrack.Copilot` 的当前运行前提。

