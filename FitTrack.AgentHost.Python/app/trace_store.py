from __future__ import annotations

import json
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


class TraceStore:
    def __init__(self, trace_dir: Path) -> None:
        self._trace_dir = trace_dir

    async def write(self, trace_id: str, payload: dict[str, Any]) -> Path:
        self._trace_dir.mkdir(parents=True, exist_ok=True)
        trace_path = self._trace_dir / f"{trace_id}.json"
        payload.setdefault("traceId", trace_id)
        payload.setdefault("writtenAtUtc", datetime.now(timezone.utc).isoformat())
        trace_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")
        return trace_path
