# FitTrack.Copilot.XiaomiSmokeTest

Minimal console app for directly testing the Xiaomi model configuration used by `FitTrack.Copilot`.

## Run

From `D:\GitHub\FitTrack\FitTrack`:

```powershell
$env:Xiaomi__ApiKey="your-key"
dotnet run --project .\FitTrack.Copilot.XiaomiSmokeTest -- "用中文介绍你自己"
```

You can override model and endpoint per run:

```powershell
dotnet run --project .\FitTrack.Copilot.XiaomiSmokeTest -- --model=mimo-v2-pro "给我一条减脂午餐建议"
```

It also supports shorthand environment variables:

```powershell
$env:XIAOMI_API_KEY="your-key"
$env:XIAOMI_ENDPOINT="https://token-plan-cn.xiaomimimo.com/v1"
$env:XIAOMI_MODEL_ID="mimo-v2-flash"
dotnet run --project .\FitTrack.Copilot.XiaomiSmokeTest -- "给我一条减脂早餐建议"
```

## Config resolution order

1. `FitTrack.Copilot/appsettings.json`
2. `FitTrack.Copilot/appsettings.Development.json`
3. same user-secrets as `FitTrack.Copilot`
4. standard environment variables like `Xiaomi__ApiKey`
5. shorthand environment variables like `XIAOMI_API_KEY`
