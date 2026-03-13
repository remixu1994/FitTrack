# FitTrack AI Agent PRD (v1)

## 1. Product Vision

FitTrack AI Agent is a personal health copilot focused on two core outcomes:

- Better training quality and consistency.
- Better nutrition quality and long-term adherence.

The product should provide personalized, safe, and actionable guidance, then convert guidance into daily execution.

## 2. Target Users

- Beginner users who need clear plans and habit coaching.
- Intermediate users who need progression and plateau breaking.
- Fat-loss users who need calorie control with high adherence.
- Muscle-gain users who need progressive overload and protein strategy.
- Busy office users who need short, practical plans.

## 3. Product Goals and KPI

- 7-day retention: >= 35%
- 30-day retention: >= 20%
- Weekly workout completion rate: >= 60%
- Daily food logging completion rate: >= 50%
- Plan adherence score (diet + training): >= 70/100

## 4. Core Requirement Modules

## 4.1 User Profile and Baseline Assessment

- Capture demographics, height, weight, body-fat estimate, injuries, equipment access, dietary preference, allergies.
- Capture available training days/time windows and cooking constraints.
- Build baseline metrics: TDEE estimate, strength baseline, movement confidence.

Acceptance:

- User can complete onboarding in <= 5 minutes.
- Agent generates first weekly plan immediately after onboarding.

## 4.2 Goal Management

- Support goal types: fat loss, muscle gain, recomposition, maintenance.
- Support deadline and weekly pace recommendations.
- Auto-adjust goals based on progress trend and adherence.

Acceptance:

- Goal can be edited anytime.
- Agent explains tradeoffs when goal pace is too aggressive.

## 4.3 Workout Planning and Coaching

- Generate weekly split (home/gym variants).
- Generate daily workout with sets/reps/RPE/rest intervals.
- Offer exercise substitutions based on injury/equipment/time.
- Support progression logic (load, reps, volume, deload).

Acceptance:

- Daily workout can be completed step by step in chat.
- Plan updates after each session based on user feedback.

## 4.4 Nutrition Planning and Logging

- Generate calorie and macro targets from goal and profile.
- Build daily meal structure (3-meal/4-meal/IF modes).
- Parse food from text and image.
- Estimate macros and confidence level per entry.
- Suggest next-meal correction when user overshoots/undershoots.

Acceptance:

- User can complete one meal log in <= 20 seconds.
- Nutrition summary refreshes in real time after each log.

## 4.5 Daily Check-in and Adaptive Loop

- Daily check-in: sleep, energy, soreness, hunger, mood, stress.
- Adjust workout intensity and diet targets based on readiness.
- Detect non-adherence patterns and switch strategy.

Acceptance:

- Agent asks at most 3-5 key questions per check-in.
- Agent provides one concrete action list for the same day.

## 4.6 Safety and Compliance Guardrails

- Risk checks for injury, disordered eating signals, extreme deficits.
- Block unsafe guidance (medical diagnosis, dangerous protocols).
- Escalate to "seek professional help" templates when needed.

Acceptance:

- Unsafe requests always trigger safe refusal + alternatives.
- Guardrail events are logged for review.

## 4.7 Progress Analytics and Feedback

- Dashboard metrics: weight trend, waist trend, strength trend, adherence trend.
- Weekly review report: wins, bottlenecks, next-week adjustments.
- Explainable recommendations ("why this changed").

Acceptance:

- Weekly report generated automatically every 7 days.
- Each recommendation includes reason and expected impact.

## 4.8 Multi-Modal Interaction

- Text chat as primary interface.
- Image upload for food recognition.
- Optional voice input/output (v2).

Acceptance:

- Image flow supports at least JPG/PNG and handles low-confidence fallback.

## 5. Non-Functional Requirements

- P95 response latency:
  - text only <= 3s
  - image + nutrition <= 8s
- Availability >= 99.5%
- Observability: full request trace across agent/tool/model calls.
- Privacy:
  - PII encryption at rest.
  - user-level data isolation.
  - audit log for admin operations.

## 6. Scope by Phases

## Phase 1 (MVP, 6-8 weeks)

- Onboarding + goals + workout planner + text food logging.
- Single orchestrator + 2 domain agents (nutrition/workout).
- Weekly review report.

## Phase 2 (Growth, 8-12 weeks)

- Image food recognition and confidence scoring.
- Adaptive daily check-in loop.
- Enhanced progression logic and plateau handling.

## Phase 3 (Advanced)

- Wearable integration.
- Voice coach mode.
- Preventive health risk alerts (non-diagnostic).

## 7. Out of Scope (Current)

- Direct medical treatment advice.
- Real-time emergency response.
- Insurance/clinical workflow integration.

## 8. High-Risk Assumptions

- Users are willing to log food consistently.
- Image recognition quality is good enough for meal-level decisions.
- Initial prompt/guardrail design prevents unsafe coaching.

## 9. Validation Plan

- Run 2-week closed beta with 20 users.
- Measure retention, adherence, correction acceptance rate.
- Prioritize changes by highest impact on adherence and safety.
