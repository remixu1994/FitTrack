from __future__ import annotations

from typing import Any

from pydantic import BaseModel, ConfigDict, Field


def to_camel(value: str) -> str:
    head, *tail = value.split("_")
    return head + "".join(part.capitalize() for part in tail)


class CamelModel(BaseModel):
    model_config = ConfigDict(populate_by_name=True, alias_generator=to_camel)


class RecentMessage(CamelModel):
    role: str
    kind: str
    content_text: str | None = None
    content_json: Any | None = None


class ChatRequest(CamelModel):
    user_id: str
    thread_id: str
    text: str | None = None
    meal_photo_data_url: str | None = None
    recent_messages: list[RecentMessage] = Field(default_factory=list)
    request_timestamp_utc: str


class NutritionSnapshot(CamelModel):
    training_type: str | None = None
    day_type: str | None = None
    target_calories: int | None = None
    target_protein_g: int | None = None
    target_carbs_g: int | None = None
    target_fat_g: int | None = None
    consumed_calories: int | None = None
    consumed_protein_g: float | None = None
    consumed_carbs_g: float | None = None
    consumed_fat_g: float | None = None
    remaining_calories: int | None = None
    remaining_protein_g: float | None = None
    remaining_carbs_g: float | None = None
    remaining_fat_g: float | None = None
    next_suggestions: dict[str, Any] | None = None


class ChatResponse(CamelModel):
    agent_name: str
    message: str
    structured_payload: dict[str, Any] | None = None
    snapshot: NutritionSnapshot | None = None
    tool_events: list[str] = Field(default_factory=list)
    trace_id: str


class VisionItem(CamelModel):
    name: str
    calories: float
    protein_grams: float = 0
    carbs_grams: float = 0
    fat_grams: float = 0
    confidence: float | None = None
    serving_hint: str | None = None


class VisionStructuredResult(CamelModel):
    summary: str
    items: list[VisionItem] = Field(default_factory=list)
