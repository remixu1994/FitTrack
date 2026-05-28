# FitTrack AI Agent Technical Blueprint (MEAI + SK + MAF)

## 1. Architecture Objective

Build a modular multi-agent system where:

- `MAF` handles agent topology and orchestration.
- `MEAI` standardizes model/tool interaction (`IChatClient`, `ITool`).
- `Semantic Kernel` provides reusable domain plugins, prompts, and planners.

## 2. Layer Responsibilities

## 2.1 MAF (Agent Orchestration Layer)

- Route user intent to the right domain agent.
- Execute graph workflow with checkpoints and retries.
- Manage conversation/session state and cross-agent handoff.

## 2.2 MEAI (Model Abstraction Layer)

- Unified chat model interface for provider switching.
- Middleware pipeline for logging, safety filtering, and performance metrics.
- Unified tool calling contract across agents.

## 2.3 Semantic Kernel (Domain Intelligence Layer)

- Implement structured domain plugins:
  - nutrition reasoning
  - workout progression logic
  - behavior coaching snippets
- Maintain prompt templates and policy prompts.
- Optional planner use for multi-step domain tasks.

## 3. Proposed Agent Topology

- `OrchestratorAgent`
  - intent classification
  - route and compose final response
- `NutritionAgent`
  - meal parsing
  - macro estimation
  - meal correction suggestions
- `WorkoutAgent`
  - plan generation
  - session adaptation
  - progression updates
- `CheckInAgent`
  - daily readiness scoring
  - trigger temporary adjustment rules
- `SafetyAgent`
  - pre/post response guardrail checks
  - unsafe intent interception

## 4. Skill and Tool Design

## 4.1 SK Plugins (Business Logic)

- `ProfilePlugin`
- `GoalPlugin`
- `NutritionPlugin`
- `WorkoutPlugin`
- `ProgressPlugin`
- `SafetyPolicyPlugin`

All plugin methods should return strongly typed DTOs, not free-form strings.

## 4.2 MEAI Tools (Execution Surface)

- Wrap SK plugin calls as MEAI tools for agent tool-calling.
- Wrap external services as tools:
  - USDA/OpenFoodFacts lookup
  - image recognition endpoint
  - wearable data fetch (future)

## 4.3 MAF Skills (Composed Operations)

- `GenerateDailyPlanSkill`
- `AnalyzeMealFromTextSkill`
- `AnalyzeMealFromImageSkill`
- `AdjustPlanByCheckInSkill`
- `GenerateWeeklyReviewSkill`

Each skill should be idempotent and traceable.

## 5. Data and Memory Strategy

- Short-term memory: current chat/session context in MAF state store.
- Long-term memory:
  - profile, goals, history in EF Core/SQLite (current), Postgres (later).
  - summarized progress snapshots for retrieval.
- RAG layer:
  - trusted nutrition/workout knowledge base.
  - versioned prompt/policy docs.

## 6. End-to-End Workflows

## 6.1 Meal Logging (Text)

1. User inputs meal text.
2. `OrchestratorAgent` routes to `NutritionAgent`.
3. `NutritionAgent` calls `AnalyzeMealFromTextSkill`.
4. Skill uses `NutritionPlugin` + food DB tools.
5. Save structured record and update daily totals.
6. Return concise feedback + next action.

## 6.2 Workout Day Guidance

1. User opens workout session.
2. `WorkoutAgent` loads active plan and readiness signals.
3. Agent calls progression skill to generate session details.
4. During session, user reports performance.
5. Agent adjusts next exercises/loads in real time.

## 6.3 Safety Interception

1. Any user request passes `SafetyAgent` pre-check.
2. If risk detected, block unsafe output path.
3. Return safe alternatives and escalation suggestion.
4. Log event with policy code and confidence.

## 7. Suggested Project Structure (FitTrack.Copilot)

- `MAF/Agents`
  - `OrchestratorAgent.cs`
  - `NutritionAgent.cs`
  - `WorkoutAgent.cs`
  - `CheckInAgent.cs`
  - `SafetyAgent.cs`
- `MAF/Skills`
  - composed business flows
- `SemanticKernel/Plugins`
  - typed SK plugins per domain
- `AI`
  - chat client factory and provider config
- `Middleware`
  - logging, safety filter, performance middleware
- `Service`
  - data persistence and domain services
- `Data`
  - entities, migrations, repositories

## 8. Implementation Plan

## Step 1: Foundation (Week 1-2)

- Define canonical DTOs for profile/meal/workout/check-in.
- Standardize model response schema.
- Wire MEAI `IChatClient` + middleware chain.

Deliverable:

- stable chat pipeline with trace IDs and policy hooks.

## Step 2: Core Agents (Week 3-4)

- Implement `OrchestratorAgent`, `NutritionAgent`, `WorkoutAgent`.
- Implement 3 high-value skills:
  - meal text analysis
  - daily workout generation
  - weekly review

Deliverable:

- MVP flows executable end-to-end.

## Step 3: Adaptive Loop (Week 5-6)

- Add `CheckInAgent` and plan-adjustment skill.
- Add adherence scoring and intervention triggers.

Deliverable:

- personalized day-by-day adaptation.

## Step 4: Safety and Quality (Week 7-8)

- Add `SafetyAgent` and policy rule engine.
- Add eval set and regression tests for unsafe prompts.
- Add fallback model and timeout/retry strategy.

Deliverable:

- production-grade safety baseline.

## 9. Observability and Evaluation

- Metrics:
  - tool success rate
  - orchestration latency
  - unsafe request interception rate
  - recommendation acceptance rate
- Evaluation suites:
  - nutrition extraction accuracy
  - workout appropriateness scoring
  - guardrail precision/recall

## 10. Key Engineering Rules

- Keep deterministic steps in tools/plugins; keep LLM outputs bounded.
- Enforce JSON schema output for critical calculations.
- Any user-visible recommendation must include confidence + rationale.
- Separate "reasoning agent" and "execution tools" to simplify debugging.
