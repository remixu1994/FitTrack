from __future__ import annotations

import tempfile
import unittest
from pathlib import Path

from app.models import ChatRequest
from app.orchestrator import CoachOrchestrator
from app.trace_store import TraceStore


class FakeRuntime:
    def __init__(self, text_map: dict[str, str] | None = None, message_map: dict[str, str] | None = None) -> None:
        self.text_map = text_map or {}
        self.message_map = message_map or {}

    async def run_prompt(self, name: str, instructions: str, prompt: str) -> str:
        return self.text_map.get(name, f"{name}:{prompt[:24]}")

    async def run_messages(self, name: str, instructions: str, messages) -> str:
        return self.message_map.get(name, '{"summary":"Detected meal.","items":[]}')


class FakeTools:
    async def get_progress_summary(self, user_id: str):
        return {
            "userId": user_id,
            "currentWeightKg": 80.0,
            "bodyFatPercent": 18.0,
            "foodRecordCountToday": 2,
            "workoutCountThisWeek": 3,
            "caloriesToday": 1800,
            "proteinToday": 140,
            "carbsToday": 150,
            "fatToday": 60,
            "headline": "Progress is steady.",
            "recommendations": ["Keep protein high."],
        }

    async def get_daily_snapshot(self, user_id: str):
        return {
            "dayType": "medium",
            "consumedCalories": 1600,
            "consumedProteinG": 130,
            "consumedCarbsG": 140,
            "consumedFatG": 55,
            "remainingCalories": 600,
            "remainingProteinG": 30,
            "remainingCarbsG": 80,
            "remainingFatG": 15,
            "nextSuggestions": {"protein": "Increase protein in the next meal."},
        }

    async def get_recent_workouts(self, user_id: str):
        return [{"id": 1, "duration": 45, "notes": "Leg day", "exercises": []}]

    async def analyze_text(self, user_id: str, text: str):
        return {
            "summary": "Estimated 620 kcal.",
            "items": [{"name": "Chicken bowl", "calories": 620}],
        }


class CoachOrchestratorTests(unittest.IsolatedAsyncioTestCase):
    async def asyncSetUp(self) -> None:
        self.temp_dir = tempfile.TemporaryDirectory()
        self.trace_store = TraceStore(Path(self.temp_dir.name))

    async def asyncTearDown(self) -> None:
        self.temp_dir.cleanup()

    async def test_routes_nutrition_queries_to_nutrition_agent(self):
        orchestrator = CoachOrchestrator(
            FakeRuntime(text_map={"nutrition_agent": "Nutrition answer"}),
            FakeTools(),
            self.trace_store,
        )
        request = ChatRequest(
            userId="user-1",
            threadId="thread-1",
            text="How many calories are in my meal?",
            mealPhotoDataUrl=None,
            recentMessages=[],
            requestTimestampUtc="2026-04-03T00:00:00Z",
        )

        response = await orchestrator.handle_chat(request)

        self.assertEqual(response.agent_name, "nutrition_agent")
        self.assertEqual(response.message, "Nutrition answer")
        self.assertIsNotNone(response.snapshot)
        self.assertIn("subagent:nutrition", response.tool_events)

    async def test_routes_workout_queries_to_workout_agent(self):
        orchestrator = CoachOrchestrator(
            FakeRuntime(text_map={"workout_agent": "Workout answer"}),
            FakeTools(),
            self.trace_store,
        )
        request = ChatRequest(
            userId="user-1",
            threadId="thread-1",
            text="Build me a workout plan",
            mealPhotoDataUrl=None,
            recentMessages=[],
            requestTimestampUtc="2026-04-03T00:00:00Z",
        )

        response = await orchestrator.handle_chat(request)

        self.assertEqual(response.agent_name, "workout_agent")
        self.assertIn("subagent:workout", response.tool_events)

    async def test_routes_progress_queries_to_progress_agent(self):
        orchestrator = CoachOrchestrator(
            FakeRuntime(text_map={"progress_agent": "Progress answer"}),
            FakeTools(),
            self.trace_store,
        )
        request = ChatRequest(
            userId="user-1",
            threadId="thread-1",
            text="Give me a weekly progress summary",
            mealPhotoDataUrl=None,
            recentMessages=[],
            requestTimestampUtc="2026-04-03T00:00:00Z",
        )

        response = await orchestrator.handle_chat(request)

        self.assertEqual(response.agent_name, "progress_agent")
        self.assertEqual(response.message, "Progress answer")
        self.assertIn("subagent:progress", response.tool_events)

    async def test_routes_vision_queries_to_vision_agent(self):
        orchestrator = CoachOrchestrator(
            FakeRuntime(
                message_map={
                    "vision_agent": '{"summary":"Detected 2 items.","items":[{"name":"Rice","calories":300,"proteinGrams":6,"carbsGrams":64,"fatGrams":1},{"name":"Chicken","calories":250,"proteinGrams":35,"carbsGrams":0,"fatGrams":8}]}'
                }
            ),
            FakeTools(),
            self.trace_store,
        )
        request = ChatRequest(
            userId="user-1",
            threadId="thread-1",
            text="What is this meal?",
            mealPhotoDataUrl="data:image/png;base64,ZmFrZQ==",
            recentMessages=[],
            requestTimestampUtc="2026-04-03T00:00:00Z",
        )

        response = await orchestrator.handle_chat(request)

        self.assertEqual(response.agent_name, "vision_agent")
        self.assertEqual(response.snapshot.consumed_calories, 550)
        self.assertEqual(len(response.structured_payload["items"]), 2)

    async def test_combines_workout_and_nutrition_in_sequence(self):
        orchestrator = CoachOrchestrator(
            FakeRuntime(
                text_map={
                    "workout_agent": "Workout block",
                    "nutrition_agent": "Nutrition block",
                }
            ),
            FakeTools(),
            self.trace_store,
        )
        request = ChatRequest(
            userId="user-1",
            threadId="thread-1",
            text="Give me a workout and nutrition plan for today",
            mealPhotoDataUrl=None,
            recentMessages=[],
            requestTimestampUtc="2026-04-03T00:00:00Z",
        )

        response = await orchestrator.handle_chat(request)

        self.assertEqual(response.agent_name, "coach_supervisor")
        self.assertIn("Workout block", response.message)
        self.assertIn("Nutrition block", response.message)
        self.assertIn("subagents", response.structured_payload)


if __name__ == "__main__":
    unittest.main()
