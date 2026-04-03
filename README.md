# FitTrack

FitTrack 是一个健身和饮食追踪工作区，当前由两个 .NET 后端和一个独立前端组成。

## 运行入口

`FitTrack` 旧主应用:

```powershell
dotnet run --project FitTrack
```

默认地址:

- `http://localhost:5265`
- `https://localhost:7146`

`FitTrack.Copilot` 主 API / Agent Host:

```powershell
dotnet run --project FitTrack.Copilot
```

默认地址:

- `http://localhost:5097`
- `https://localhost:7291`

`FitTrack.Web` Next.js 前端:

```powershell
cd FitTrack.Web
npm install
npm run dev
```

默认地址:

- `http://localhost:3000`

## 前端配置

`FitTrack.Web` 默认通过 `NEXT_PUBLIC_API_BASE_URL` 连接 Copilot API，未设置时使用 `https://localhost:7291`。

## 当前主线

- `FitTrack` 保留为 legacy/reference
- `FitTrack.Copilot` 承担 API、Identity、Agent 编排和数据写入
- `FitTrack.Web` 承担主用户体验、线程式聊天和前端壳层
