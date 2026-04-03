from __future__ import annotations

from typing import Sequence

from agent_framework import Agent, Content, Message
from agent_framework_openai import OpenAIChatClient

from app.settings import Settings


class AgentRuntime:
    def __init__(self, settings: Settings) -> None:
        self._client = OpenAIChatClient(
            model=settings.azure_openai_deployment,
            api_key=settings.azure_openai_api_key,
            azure_endpoint=settings.azure_openai_endpoint,
            api_version=settings.azure_openai_api_version,
        )

    async def run_prompt(self, name: str, instructions: str, prompt: str) -> str:
        agent = Agent(self._client, instructions=instructions, name=name, id=name)
        response = await agent.run(prompt)
        return (response.text or "").strip()

    async def run_messages(self, name: str, instructions: str, messages: Sequence[Message]) -> str:
        agent = Agent(self._client, instructions=instructions, name=name, id=name)
        response = await agent.run(messages)
        return (response.text or "").strip()

    @staticmethod
    def text_message(role: str, text: str) -> Message:
        return Message(role=role, contents=[Content.from_text(text)])
