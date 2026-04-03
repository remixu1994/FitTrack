from __future__ import annotations

from functools import lru_cache
from pathlib import Path

from pydantic import Field
from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_file=".env", env_file_encoding="utf-8", extra="ignore")

    azure_openai_endpoint: str = Field(alias="AZURE_OPENAI_ENDPOINT")
    azure_openai_api_key: str = Field(alias="AZURE_OPENAI_API_KEY")
    azure_openai_deployment: str = Field(alias="AZURE_OPENAI_DEPLOYMENT")
    azure_openai_api_version: str | None = Field(default=None, alias="AZURE_OPENAI_API_VERSION")
    fittrack_copilot_internal_base_url: str = Field(alias="FITTRACK_COPILOT_INTERNAL_BASE_URL")
    fittrack_agent_trace_dir: Path = Field(alias="FITTRACK_AGENT_TRACE_DIR")


@lru_cache(maxsize=1)
def get_settings() -> Settings:
    return Settings()
