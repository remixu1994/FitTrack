from __future__ import annotations

from contextlib import asynccontextmanager

import httpx
from fastapi import FastAPI

from app.agent_runtime import AgentRuntime
from app.internal_tools import InternalToolsClient
from app.models import ChatRequest, ChatResponse
from app.orchestrator import CoachOrchestrator
from app.settings import get_settings
from app.trace_store import TraceStore


@asynccontextmanager
async def lifespan(app: FastAPI):
    settings = get_settings()
    http_client = httpx.AsyncClient(base_url=settings.fittrack_copilot_internal_base_url.rstrip("/"), timeout=15.0)
    tools = InternalToolsClient(settings.fittrack_copilot_internal_base_url, http_client=http_client)
    runtime = AgentRuntime(settings)
    trace_store = TraceStore(settings.fittrack_agent_trace_dir)

    app.state.http_client = http_client
    app.state.orchestrator = CoachOrchestrator(runtime, tools, trace_store)

    try:
        yield
    finally:
        await http_client.aclose()


app = FastAPI(title="FitTrack Agent Host Python", version="0.1.0", lifespan=lifespan)


@app.get("/healthz")
async def healthz() -> dict[str, str]:
    return {"status": "ok"}


@app.post("/internal/agent/chat", response_model=ChatResponse)
async def internal_agent_chat(request: ChatRequest) -> ChatResponse:
    orchestrator: CoachOrchestrator = app.state.orchestrator
    return await orchestrator.handle_chat(request)
