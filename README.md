# FitTrack
## 推荐一键启动方式

在仓库根目录运行：

```powershell
.\start-fittrack-dev.ps1
```

这个脚本会：

- 分别在两个独立 PowerShell 窗口中启动 `FitTrack.Copilot` 和 `FitTrack.React`
- 后端使用 `dotnet watch run`
- 前端使用 `npm run dev`
- 如果 `FitTrack/FitTrack.React/node_modules` 不存在，会先自动执行一次 `npm install`


FitTrack 目前由 `FitTrack.Copilot` 后端和 `FitTrack.React` 前端站点组成。

## 运行入口

`FitTrack.Copilot` API / Agent Host:

```powershell
dotnet run --project FitTrack/FitTrack.Copilot
```

默认地址:

- `http://localhost:5097`
- `https://localhost:7291`

`FitTrack.React` React 前端:

```powershell
cd FitTrack/FitTrack.React
npm install
npm run dev
```

默认地址:

- `http://localhost:3000`

## 前端配置

`FitTrack.React` 默认通过 `NEXT_PUBLIC_API_BASE_URL` 连接 Copilot API，未设置时使用 `https://localhost:7291`。

## 当前主线

- `FitTrack.Copilot` 承担 API、Identity、Agent 编排和数据读写。
- `FitTrack.React` 承担主用户体验、线程式聊天和前端壳层。
