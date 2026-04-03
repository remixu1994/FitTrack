# DECISIONS.md

## Python Agent Sidecar

### D-016 Python Agent Sidecar 作为 `FitTrack.Copilot` 的能力侧车

- 状态: 已落地
- 证据:
  - `FitTrack.AgentHost.Python/`
  - `FitTrack.Copilot/Service/PythonCoachChatService.cs`
- 含义:
  - 前端主入口保持 `FitTrack.Web -> FitTrack.Copilot`
  - Python 作为 sidecar 承接 agent orchestration，不直接成为前端 API 入口

### D-017 聊天主数据仍由 `.NET` 持久化

- 状态: 已落地
- 证据:
  - `FitTrack.Copilot/Endpoints/ChatEndpoints.cs`
  - `FitTrack.Copilot/Service/ConversationService.cs`
  - `FitTrack.Copilot/Service/PythonCoachChatService.cs`
- 含义:
  - thread / message / attachment / snapshot 仍由 `FitTrack.Copilot` 写 SQLite
  - Python trace 不是业务真相源

### D-018 Python 通过内部 tool API 获取业务数据

- 状态: 已落地
- 证据:
  - `FitTrack.Copilot/Endpoints/InternalAgentToolEndpoints.cs`
- 含义:
  - Phase 1 不让 Python 直连 SQLite
  - 内部 tool API 仅在 Development 下启用，并限制 loopback 请求

## 已落地技术决策

本文件只记录当前代码已经体现出的决策，不记录纯设想。

## D-001 使用双项目 solution，而不是单项目渐进式集成

- 状态: 已落地
- 证据:
  - `FitTrack.sln` 同时包含 `FitTrack` 与 `FitTrack.Copilot`
- 含义:
  - 基础能力与 AI 扩展能力被拆成两个可独立运行的 Web 应用
- 结果:
  - 研发试验更灵活
  - 但会带来重复配置、重复认证接线和模型分叉风险

## D-002 主应用采用 Blazor Server 风格的 Razor Components

- 状态: 已落地
- 证据:
  - `AddRazorComponents().AddInteractiveServerComponents()`
  - `MapRazorComponents<App>().AddInteractiveServerRenderMode()`
- 含义:
  - 选择服务器端交互模式，而不是 WASM 前端
- 原因推断:
  - 更适合快速搭建带认证和数据库访问的内部/原型系统

## D-003 使用 ASP.NET Core Identity 作为认证方案

- 状态: 已落地
- 证据:
  - `IdentityDbContext<ApplicationUser>`
  - `AddIdentityCore<ApplicationUser>()`
  - `AddIdentityCookies()`
  - `MapAdditionalIdentityEndpoints()`
- 含义:
  - 登录、注册、账户管理基于官方 Identity 体系
- 影响:
  - 开发速度快
  - 但账户模型扩展和 UI 定制需遵循模板结构

## D-004 使用 SQLite 作为默认数据库

- 状态: 已落地
- 证据:
  - 两个 .NET 项目都使用 `UseSqlite(connectionString)`
  - 默认连接串都指向 `Data\\app.db`
- 含义:
  - 当前优先本地部署和开发便利性，而非高并发或分布式部署
- 影响:
  - 上手简单
  - 但并发、迁移治理、备份恢复能力有限

## D-005 主应用通过启动初始化导入默认食物库

- 状态: 已落地
- 证据:
  - `DbInitializer.Initialize(context, env)`
  - 种子来源是 `wwwroot/foods.json`
- 含义:
  - 食物基础数据不依赖首次人工录入
- 影响:
  - 新环境启动体验更好
  - 但静态 JSON 会引入版本漂移和更新维护问题

## D-006 主应用的数据访问目前偏“页面直连 DbContext”

- 状态: 已落地
- 证据:
  - 主应用页面直接注入 `ApplicationDbContext`
- 含义:
  - 当前没有严格的应用服务层隔离
- 影响:
  - 早期开发快
  - 但后续复杂业务、测试、复用会受限

## D-007 Copilot 项目将 AI 能力放在独立服务层和 Semantic Kernel 目录中

- 状态: 已落地
- 证据:
  - `Service/*`
  - `SemanticKernel/*`
  - `AddCopilotServices(builder.Configuration)`
- 含义:
  - AI 调用没有散落在 Razor 页面中，而是经过集中注册和编排
- 影响:
  - 比主应用更接近可维护架构
  - 但抽象层次更多，排查问题成本更高

## D-008 Copilot 项目依赖外部 AI 与营养数据服务

- 状态: 已落地
- 证据:
  - `AI`
  - `TokenAI`
  - `USDA`
  - `Api/Usda/UsdaClient.cs`
- 含义:
  - Copilot 的核心价值依赖外部 API，而不是纯本地规则
- 影响:
  - 需要处理密钥、超时、失败重试、费用与可用性

## D-009 Copilot 暴露 OpenAPI/Swagger 用于接口调试

- 状态: 已落地
- 证据:
  - `AddOpenApi()`
  - `MapOpenApi()`
  - `UseSwaggerUI(...)`
- 含义:
  - 该项目不仅是 UI 应用，也在向 API 应用演进

## D-010 当前仓库尚未建立自动化测试基线

- 状态: 已落地
- 证据:
  - solution 中未发现测试项目
- 含义:
  - 目前主要依赖手动验证
- 风险:
  - 认证、迁移、AI 流程和页面回归容易漏检

## D-011 新主前端迁移为独立 Next.js 应用

- 状态: 已落地
- 证据:
  - `FitTrack.Web/`
  - `src/app/chat/page.tsx`
  - `src/components/chat/chat-view.tsx`
- 含义:
  - 主用户体验不再由 Blazor/MudBlazor 承担
  - 前端通过 HTTP 调用 `FitTrack.Copilot` 的 API
- 影响:
  - 前后端可以独立部署和演进
  - 但认证、CORS、refresh 和 cookie 策略需要按分离部署设计

## D-012 Copilot 主链路切换为 API + Agent Host

- 状态: 已落地
- 证据:
  - `FitTrack.Copilot/Program.cs`
  - `FitTrack.Copilot/Endpoints/*`
  - `FitTrack.Copilot/Agents/*`
- 含义:
  - `FitTrack.Copilot` 的主入口已不是 Razor Components
  - 当前以 JWT、线程式会话和结构化事件流承载前端交互

## D-013 Agent SDK 显式升级到 `Microsoft.Agents.AI 1.0.0`

- 状态: 已落地
- 证据:
  - `PackageReference Include="Microsoft.Agents.AI" Version="1.0.0"`
- 含义:
  - 主 agent SDK 已使用稳定版
- 补充:
  - `Microsoft.Agents.AI.Workflows` 由于公开稳定版尚不可用，当前仍保留 `1.0.0-rc4`

## D-014 前后端分离部署采用 bearer token + refresh cookie

- 状态: 已落地
- 证据:
  - `FitTrack.Web/src/lib/http.ts`
  - `FitTrack.Copilot/Endpoints/AuthEndpoints.cs`
- 含义:
  - 前端持有短期 access token
  - refresh token 由后端通过 HttpOnly cookie 管理
  - 浏览器通过 `credentials: 'include'` 完成刷新
- 影响:
  - 认证链路必须同时考虑 CORS、cookie SameSite 和 HTTPS

## D-015 `FitTrack.Web` 作为独立前端目录保留，不并入 `.sln`

- 状态: 已落地
- 证据:
  - 当前 `FitTrack.sln` 只包含两个 .NET 项目
  - `FitTrack.Web` 作为独立 Next.js 工作区存在
- 含义:
  - solution 继续只管理 .NET 运行时
  - 前端通过独立 Node/Next.js 工作流开发与启动

## 待确认但尚未写入决策的事项

以下事项重要，但当前代码还不足以把它们当作既定决策:

- 最终主线究竟是 `FitTrack` 还是 `FitTrack.Copilot`
- 生产环境是否继续使用 SQLite
- AI provider 最终统一到 Azure OpenAI、TokenAI，还是其他供应商
