# FitTrack.AgentHost.Python

Python sidecar agent host for `FitTrack.Copilot`.

## Run

```powershell
uv sync
uv run uvicorn app.main:app --host 127.0.0.1 --port 8010
```

## Required environment variables

- `AZURE_OPENAI_ENDPOINT`
- `AZURE_OPENAI_API_KEY`
- `AZURE_OPENAI_DEPLOYMENT`
- `FITTRACK_COPILOT_INTERNAL_BASE_URL`
- `FITTRACK_AGENT_TRACE_DIR`

Optional:

- `AZURE_OPENAI_API_VERSION`
