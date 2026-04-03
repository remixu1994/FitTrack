from __future__ import annotations

from typing import Any

import httpx


class InternalToolsClient:
    def __init__(self, base_url: str, http_client: httpx.AsyncClient | None = None) -> None:
        self._owns_client = http_client is None
        self._client = http_client or httpx.AsyncClient(base_url=base_url.rstrip("/"), timeout=15.0)

    async def close(self) -> None:
        if self._owns_client:
            await self._client.aclose()

    async def get_progress_summary(self, user_id: str) -> dict[str, Any]:
        response = await self._client.get("/internal/agent-tools/progress/summary", params={"userId": user_id})
        response.raise_for_status()
        return response.json()

    async def get_daily_snapshot(self, user_id: str) -> dict[str, Any]:
        response = await self._client.get("/internal/agent-tools/nutrition/daily-snapshot", params={"userId": user_id})
        response.raise_for_status()
        return response.json()

    async def get_recent_workouts(self, user_id: str) -> list[dict[str, Any]]:
        response = await self._client.get("/internal/agent-tools/workouts/recent", params={"userId": user_id})
        response.raise_for_status()
        return response.json()

    async def analyze_text(self, user_id: str, text: str) -> dict[str, Any]:
        response = await self._client.post(
            "/internal/agent-tools/nutrition/analyze-text",
            json={"userId": user_id, "text": text},
        )
        response.raise_for_status()
        return response.json()
