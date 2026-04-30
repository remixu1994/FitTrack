# FitTrack

Fitness & diet tracking workspace with AI coaching capabilities.

## Project Structure

```
FitTrack.sln
├── FitTrack/                    # Legacy .NET 9 Blazor app (reference only)
├── FitTrack.Copilot/            # Main .NET 10 AI backend + API Host
├── FitTrack.Web/                # Next.js 16 frontend (not in .sln)
└── FitTrack.AgentHost.Python/   # Python agent sidecar (Phase 1)
```

## Primary Delivery Path

`FitTrack.Web → FitTrack.Copilot API` (NOT Blazor)

- **FitTrack**: Legacy/reference, no new features
- **FitTrack.Copilot**: Main backend — API, AI agents, database writes
- **FitTrack.Web**: Main frontend — chat, food records, workouts, progress pages

## Key Rules

- Frontend changes → `FitTrack.Web`, NOT `FitTrack.Copilot/Components/*`
- Before modifying code, confirm whether you're touching `FitTrack` or `FitTrack.Copilot`
- Do NOT delete or overwrite SQLite files: `FitTrack/Data/app.db`, `FitTrack.Copilot/Data/app.db`
- For Identity, migration, seed data, or AI config changes — always check `Program.cs`
- Do not refactor solution structure or assume projects will be merged
- Python sidecar is local dev only; business truth lives in .NET + SQLite

## Local Run Commands

| App | Command | URL |
|-----|---------|-----|
| FitTrack | `dotnet run --project FitTrack` | http://localhost:5265 |
| FitTrack.Copilot | `dotnet run --project FitTrack.Copilot` | https://localhost:7291 |
| FitTrack.Web | `cd FitTrack.Web && npm install && npm run dev` | http://localhost:3000 |

## Architecture

```
FitTrack.Web → FitTrack.Copilot → SQLite / USDA API / Azure OpenAI
                              ↓
                    Python Sidecar (optional)
```

- JWT + refresh cookie auth; frontend holds access token, backend manages HttpOnly refresh cookie
- Conversation threads/messages/attachments persisted by .NET, not Python
- Python sidecar accesses business data via `/internal/agent-tools/*` (Dev only, loopback-restricted)

## High-Risk Areas

- `Program.cs` — service registration, auth, middleware, DB init
- `FitTrack.Web/src/components/chat/chat-view.tsx` — main chat workbench
- `FitTrack.Web/src/lib/http.ts` — auth, refresh, 401 retry
- `Data/` — EF Core entities and migrations
- `FitTrack.Copilot/Service/` and `SemanticKernel/` — AI orchestration

## Current Status

- No automated tests in solution
- AI config keys are placeholders
- `FitTrack.Copilot/Components/*` still contains old Blazor UI (not fully cleaned)
- `Microsoft.Agents.AI.Workflows` still on `1.0.0-rc4`
