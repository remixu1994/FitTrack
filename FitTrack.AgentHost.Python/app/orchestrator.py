from __future__ import annotations

import json
from dataclasses import dataclass
from datetime import datetime, timezone
from typing import Any
from uuid import uuid4

from agent_framework import Content, Message

from app.agent_runtime import AgentRuntime
from app.internal_tools import InternalToolsClient
from app.models import ChatRequest, ChatResponse, NutritionSnapshot, VisionStructuredResult
from app.trace_store import TraceStore


NUTRITION_WORDS = ("meal", "food", "diet", "calorie", "protein", "carb", "macro", "nutrition")
WORKOUT_WORDS = ("workout", "training", "exercise", "plan", "gym", "session")
PROGRESS_WORDS = ("progress", "check-in", "summary", "weight", "weekly", "month")


@dataclass(slots=True)
class AgentRunResult:
    agent_name: str
    message: str
    structured_payload: dict[str, Any] | None = None
    snapshot: NutritionSnapshot | None = None
    tool_events: list[str] | None = None


class CoachOrchestrator:
    def __init__(
        self,
        runtime: AgentRuntime,
        tools: InternalToolsClient,
        trace_store: TraceStore,
    ) -> None:
        self._runtime = runtime
        self._tools = tools
        self._trace_store = trace_store

    async def handle_chat(self, request: ChatRequest) -> ChatResponse:
        trace_id = uuid4().hex
        trace: dict[str, Any] = {
            "traceId": trace_id,
            "createdAtUtc": datetime.now(timezone.utc).isoformat(),
            "userId": request.user_id,
            "threadId": request.thread_id,
            "text": request.text,
            "hasMealPhoto": bool(request.meal_photo_data_url),
            "recentMessageCount": len(request.recent_messages),
            "toolCalls": [],
            "agents": [],
        }

        result = await self._dispatch(request, trace)

        await self._trace_store.write(
            trace_id,
            {
                **trace,
                "finalAgent": result.agent_name,
                "finalMessage": result.message,
                "structuredPayload": result.structured_payload,
                "snapshot": result.snapshot.model_dump(by_alias=True, exclude_none=True) if result.snapshot else None,
                "toolEvents": result.tool_events or [],
            },
        )

        return ChatResponse(
            agent_name=result.agent_name,
            message=result.message,
            structured_payload=result.structured_payload,
            snapshot=result.snapshot,
            tool_events=result.tool_events or [],
            trace_id=trace_id,
        )

    async def _dispatch(self, request: ChatRequest, trace: dict[str, Any]) -> AgentRunResult:
        prompt = (request.text or "").strip()
        if request.meal_photo_data_url:
            return await self._run_vision(request, trace)

        wants_progress = self._contains_any(prompt, PROGRESS_WORDS)
        wants_workout = self._contains_any(prompt, WORKOUT_WORDS)
        wants_nutrition = self._contains_any(prompt, NUTRITION_WORDS)

        if wants_progress and not wants_workout and not wants_nutrition:
            return await self._run_progress(request, trace)

        if wants_workout and wants_nutrition:
            return await self._run_sequential(
                request,
                trace,
                (self._run_workout, self._run_nutrition),
            )

        if wants_workout:
            return await self._run_workout(request, trace)

        if wants_progress:
            return await self._run_progress(request, trace)

        normalized_request = request.model_copy(update={"text": prompt or "Help me with my diet today."})
        return await self._run_nutrition(normalized_request, trace)

    async def _run_nutrition(self, request: ChatRequest, trace: dict[str, Any]) -> AgentRunResult:
        snapshot_payload = await self._tools.get_daily_snapshot(request.user_id)
        analysis_payload = await self._tools.analyze_text(request.user_id, request.text or "")
        trace["toolCalls"].extend(
            [
                {"tool": "nutrition.daily_snapshot", "userId": request.user_id},
                {"tool": "nutrition.analyze_text", "userId": request.user_id},
            ]
        )

        snapshot = NutritionSnapshot.model_validate(snapshot_payload)
        items = analysis_payload.get("items", [])
        analysis_summary = analysis_payload.get("summary") or f"Detected {len(items)} nutrition item(s)."
        prompt = (
            "User request:\n"
            f"{request.text}\n\n"
            "Recent conversation:\n"
            f"{self._format_recent_messages(request)}\n\n"
            "Daily nutrition snapshot JSON:\n"
            f"{json.dumps(snapshot_payload, ensure_ascii=False)}\n\n"
            "Meal analysis JSON:\n"
            f"{json.dumps(analysis_payload, ensure_ascii=False)}\n\n"
            "Respond as FitTrack nutrition coach. Keep it concise, practical, and reference today's macro state."
        )
        message = await self._runtime.run_prompt("nutrition_agent", NUTRITION_INSTRUCTIONS, prompt)
        trace["agents"].append({"agent": "nutrition_agent", "analysisSummary": analysis_summary})
        return AgentRunResult(
            agent_name="nutrition_agent",
            message=message,
            structured_payload=snapshot.model_dump(by_alias=True, exclude_none=True),
            snapshot=snapshot,
            tool_events=["subagent:nutrition", "tool:nutrition.daily_snapshot", "tool:nutrition.analyze_text"],
        )

    async def _run_workout(self, request: ChatRequest, trace: dict[str, Any]) -> AgentRunResult:
        workouts = await self._tools.get_recent_workouts(request.user_id)
        trace["toolCalls"].append({"tool": "workouts.recent", "userId": request.user_id})
        prompt = (
            "User request:\n"
            f"{request.text}\n\n"
            "Recent conversation:\n"
            f"{self._format_recent_messages(request)}\n\n"
            "Recent workouts JSON:\n"
            f"{json.dumps(workouts, ensure_ascii=False)}\n\n"
            "Respond as FitTrack workout coach. Prefer concrete training guidance."
        )
        message = await self._runtime.run_prompt("workout_agent", WORKOUT_INSTRUCTIONS, prompt)
        trace["agents"].append({"agent": "workout_agent", "recentWorkoutCount": len(workouts)})
        return AgentRunResult(
            agent_name="workout_agent",
            message=message,
            structured_payload={"recentWorkouts": workouts},
            tool_events=["subagent:workout", "tool:workouts.recent"],
        )

    async def _run_progress(self, request: ChatRequest, trace: dict[str, Any]) -> AgentRunResult:
        progress = await self._tools.get_progress_summary(request.user_id)
        trace["toolCalls"].append({"tool": "progress.summary", "userId": request.user_id})
        snapshot = NutritionSnapshot(
            consumed_calories=round(progress.get("caloriesToday", 0)) if progress.get("caloriesToday") is not None else None,
            consumed_protein_g=progress.get("proteinToday"),
            consumed_carbs_g=progress.get("carbsToday"),
            consumed_fat_g=progress.get("fatToday"),
            next_suggestions={
                f"item_{index + 1}": recommendation
                for index, recommendation in enumerate(progress.get("recommendations", []))
            },
        )
        prompt = (
            "User request:\n"
            f"{request.text or 'Give me a progress summary.'}\n\n"
            "Recent conversation:\n"
            f"{self._format_recent_messages(request)}\n\n"
            "Progress summary JSON:\n"
            f"{json.dumps(progress, ensure_ascii=False)}\n\n"
            "Respond as FitTrack progress coach. Keep it direct and actionable."
        )
        message = await self._runtime.run_prompt("progress_agent", PROGRESS_INSTRUCTIONS, prompt)
        trace["agents"].append({"agent": "progress_agent"})
        return AgentRunResult(
            agent_name="progress_agent",
            message=message,
            structured_payload=progress,
            snapshot=snapshot,
            tool_events=["subagent:progress", "tool:progress.summary"],
        )

    async def _run_vision(self, request: ChatRequest, trace: dict[str, Any]) -> AgentRunResult:
        messages = [
            Message(
                role="user",
                contents=[
                    Content.from_text(
                        (
                            "Analyze this meal image for FitTrack. "
                            "Return strict JSON with keys summary and items. "
                            "Each item must include name, calories, proteinGrams, carbsGrams, fatGrams, confidence, servingHint. "
                            f"User hint: {request.text or 'None'}"
                        )
                    ),
                    Content.from_uri(
                        request.meal_photo_data_url or "",
                        media_type=self._infer_media_type(request.meal_photo_data_url or ""),
                    ),
                ],
            )
        ]
        raw = await self._runtime.run_messages("vision_agent", VISION_INSTRUCTIONS, messages)
        parsed = self._parse_json_payload(raw)
        if parsed:
            structured = VisionStructuredResult.model_validate(parsed)
            total_calories = round(sum(item.calories for item in structured.items))
            snapshot = NutritionSnapshot(
                consumed_calories=total_calories,
                consumed_protein_g=sum(item.protein_grams for item in structured.items),
                consumed_carbs_g=sum(item.carbs_grams for item in structured.items),
                consumed_fat_g=sum(item.fat_grams for item in structured.items),
                next_suggestions={
                    "vision": "Image-based estimates are approximate.",
                    "followUp": "Confirm portions if you need tighter macro targets.",
                },
            )
            message = structured.summary or f"Detected {len(structured.items)} food item(s), estimated {total_calories} kcal."
            trace["agents"].append({"agent": "vision_agent", "itemCount": len(structured.items)})
            return AgentRunResult(
                agent_name="vision_agent",
                message=message,
                structured_payload=structured.model_dump(by_alias=True, exclude_none=True),
                snapshot=snapshot,
                tool_events=["subagent:vision"],
            )

        trace["agents"].append({"agent": "vision_agent", "parseError": True})
        return AgentRunResult(
            agent_name="vision_agent",
            message=raw or "Unable to parse image analysis response.",
            tool_events=["subagent:vision"],
        )

    async def _run_sequential(
        self,
        request: ChatRequest,
        trace: dict[str, Any],
        steps: tuple,
    ) -> AgentRunResult:
        results = []
        for step in steps:
            results.append(await step(request, trace))

        final_message = "\n\n".join(result.message.strip() for result in results if result.message.strip()) or "No workflow response."
        payloads = {result.agent_name: result.structured_payload for result in results if result.structured_payload}
        tool_events = list(dict.fromkeys(event for result in results for event in (result.tool_events or [])))
        snapshot = next((result.snapshot for result in reversed(results) if result.snapshot is not None), None)

        return AgentRunResult(
            agent_name="coach_supervisor",
            message=final_message,
            structured_payload={"subagents": payloads} if payloads else None,
            snapshot=snapshot,
            tool_events=tool_events,
        )

    @staticmethod
    def _contains_any(prompt: str, words: tuple[str, ...]) -> bool:
        lowered = prompt.lower()
        return any(word in lowered for word in words)

    @staticmethod
    def _format_recent_messages(request: ChatRequest) -> str:
        if not request.recent_messages:
            return "(no recent history)"

        lines = []
        for item in request.recent_messages[-8:]:
            parts = []
            if item.content_text:
                parts.append(item.content_text)
            if item.content_json is not None:
                parts.append(json.dumps(item.content_json, ensure_ascii=False))
            content = " | ".join(parts) if parts else "(empty)"
            lines.append(f"{item.role}/{item.kind}: {content}")
        return "\n".join(lines)

    @staticmethod
    def _infer_media_type(data_url: str) -> str | None:
        if not data_url.startswith("data:"):
            return None
        header = data_url.split(",", 1)[0]
        metadata = header[5:]
        return metadata.split(";", 1)[0] or None

    @staticmethod
    def _parse_json_payload(raw: str) -> dict[str, Any] | None:
        text = raw.strip()
        if not text:
            return None

        if text.startswith("```"):
            lines = text.splitlines()
            if len(lines) >= 3:
                text = "\n".join(lines[1:-1]).strip()

        try:
            payload = json.loads(text)
        except json.JSONDecodeError:
            start = text.find("{")
            end = text.rfind("}")
            if start < 0 or end <= start:
                return None
            try:
                payload = json.loads(text[start : end + 1])
            except json.JSONDecodeError:
                return None

        return payload if isinstance(payload, dict) else None


NUTRITION_INSTRUCTIONS = """
You are FitTrack's nutrition specialist.
Use the provided JSON context as the factual source of truth.
Keep responses concise, practical, and coach-like.
Do not invent user data that is not present in the context.
"""

WORKOUT_INSTRUCTIONS = """
You are FitTrack's workout specialist.
Use the provided recent workout JSON when giving recommendations.
Keep the plan concrete and executable.
"""

PROGRESS_INSTRUCTIONS = """
You are FitTrack's progress specialist.
Summarize the user's current status and give the next best actions.
Stay concise and grounded in the supplied JSON.
"""

VISION_INSTRUCTIONS = """
You are FitTrack's meal photo specialist.
Always return valid JSON only.
The JSON must contain a summary string and an items array.
Do not wrap the JSON in markdown unless the model cannot avoid it.
"""
