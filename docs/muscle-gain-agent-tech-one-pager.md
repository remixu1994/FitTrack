# FitTrack 增肌 Agent 技术开发一页纸方案

## 1. 目标与边界

基于 `PRODUCT_PLAN.md`，本方案服务 6 周 MVP：面向增肌训练者，打通“建档 -> 周训练计划 -> 训练日驱动碳循环 -> 饮食/训练记录 -> 每日复盘 -> 次日调整”的最小闭环。

主交付链路保持：

`FitTrack.React -> FitTrack.Copilot API -> Agent orchestration -> SQLite`

本轮技术开发只落在 `FitTrack.React` 与 `FitTrack.Copilot`。`FitTrack` legacy 主应用不承接新能力；不重构 solution 结构；不删除或覆盖现有 SQLite 文件。

## 2. MVP 系统切分

### Frontend: `FitTrack.React`

1. `Today`
   - 展示今日训练日、碳循环日、宏量目标、已摄入/剩余预算、训练完成状态和下一步动作。
   - 作为默认每日控制台，而不是让用户先进入 thread。

2. `Chat`
   - 承载建档、计划生成、饮食纠偏、训练反馈、晚间复盘。
   - Agent 输出必须同步沉淀为结构化卡片或可保存记录。

3. `Food Records`
   - 从记录列表升级为当日宏量预算追踪。
   - 支持餐次维度：早餐、午餐、晚餐、加餐、训练前、训练后。

4. `Workouts`
   - 从 session 列表升级为训练计划与当日执行台。
   - 支持动作完成、重量/次数、RPE、跳过/替换、疲劳反馈。

5. `Progress`
   - 增加增肌相关趋势：体重、训练完成度、主项进步、蛋白达标率。

### Backend: `FitTrack.Copilot`

1. Profile / Baseline
   - 扩展用户画像：目标、训练经验、器械条件、训练频率、饮食偏好、是否启用碳循环。

2. Training Plan
   - 保存周训练计划、训练日、动作处方。
   - 支持力量增肌混合结构：主项力量 + 辅助容量。

3. Carb Cycle / Macro Targets
   - 根据训练日类型生成高/中/低碳日。
   - 保存每日热量、蛋白、碳水、脂肪目标。

4. Food / Workout Logging
   - 复用并扩展现有 food record、workout session 能力。
   - 记录来源区分：用户确认、Agent 估算、外部数据查询。

5. Daily Review
   - 汇总饮食偏差、训练完成度、RPE、疲劳。
   - 生成次日训练强度、碳日和饮食重点建议。

## 3. 核心数据模型输入

后端持久化设计至少覆盖以下概念：

1. `UserProfileBaseline`
   用户目标、训练经验、器械条件、训练频率、饮食限制。

2. `FitnessGoal`
   目标类型、目标体重/体脂、周期、开始/结束日期。

3. `WeeklyWorkoutPlan`
   周计划、训练日结构、计划状态、生成来源。

4. `WorkoutPrescription`
   动作、组数、次数、RPE、休息、替代动作。

5. `WorkoutSessionLog`
   实际完成动作、重量、次数、RPE、疲劳、跳过/替换原因。

6. `CarbCyclePlan`
   周高/中/低碳序列，与训练日绑定。

7. `DailyMacroTarget`
   每日热量、蛋白、碳水、脂肪、碳日类型、生成原因。

8. `FoodRecord`
   餐次、食物、热量、宏量、估算/确认状态。

9. `DailyReview`
   今日偏差、训练完成度、恢复状态、次日调整建议。

优先演进现有 `FitnessModels.cs`、`ConversationModels.cs`、Profile/Food/Workout/Progress 服务，避免另起一套平行 domain。

## 4. Agent 分工

1. `CoachSupervisorAgent`
   - 识别用户意图，协调建档、训练、营养、复盘子能力。
   - 负责输出结构化卡片和保存动作建议。

2. `TrainingAgent`
   - 生成周训练计划和当日训练。
   - 根据 workout log、RPE、疲劳给出进阶、维持、降量或替代建议。

3. `NutritionAgent`
   - 计算宏量目标和碳循环。
   - 根据食物记录和剩余预算给出下一餐纠偏。

4. `ReviewAgent`
   - 汇总每日训练、饮食、恢复偏差。
   - 生成次日训练强度、碳日和行动清单。

Agent 输出协议至少包含：`summary`、`structuredPayload`、`recommendedActions`、`confidence`、`sourcesOrReasons`。

## 5. API 与页面闭环

第一阶段优先补齐以下 HTTP 能力：

1. `GET/PUT /api/profile`
   - 支持增肌建档字段。

2. `GET/POST /api/workout-plans`
   - 获取/创建周训练计划。

3. `GET/POST /api/workout-sessions`
   - 保存训练执行和反馈。

4. `GET/POST /api/food-records`
   - 保存饮食记录并刷新宏量汇总。

5. `GET /api/progress/summary`
   - 聚合 Today 与 Progress 所需指标。

6. `POST /api/chat/messages`
   - 继续作为 Agent 执行入口，流式返回解释和结构化卡片。

后续如现有端点难以承载每日宏量目标和复盘，再新增专用端点，例如 `/api/daily-targets`、`/api/daily-reviews`。新增前必须先检查 `Program.cs` 服务注册、认证、数据库迁移和启动流程。

## 6. 六周开发顺序

1. Week 1：扩展 profile 建档字段，Today 显示基线和缺失项。
2. Week 2：训练计划模型、API、Workouts 计划视图、TrainingAgent 初版。
3. Week 3：碳循环计算迁移/复用，生成每日宏量目标，Today 展示碳日与原因。
4. Week 4：Food Records 接入宏量预算，Agent 支持下一餐纠偏。
5. Week 5：Workout session 记录 RPE/完成度/疲劳，Agent 支持训练调整。
6. Week 6：Daily Review 卡片、次日调整建议、Progress 汇总 MVP 指标。

## 7. 验收与校验

验收场景：

1. 新用户 5 分钟内完成建档，并生成第一周增肌训练计划。
2. Today 能展示今日训练主题、碳日类型、宏量目标和下一步动作。
3. 饮食记录后，当日热量/蛋白/碳水/脂肪预算实时变化。
4. 训练记录后，Agent 能基于完成度、RPE、疲劳给出下次调整。
5. 晚间复盘能生成次日训练强度、碳日和饮食重点。

工程校验：

1. 修改 `FitTrack.Copilot` `.cs` / `.csproj` / 配置时，至少运行对应 `dotnet build`。
2. 修改 `FitTrack.React` 前端代码时，至少运行 `npm run build`。
3. 同时改动前后端时，两边 build 都必须通过。
4. 涉及 Identity、迁移、种子数据、AI 配置时，同步检查 `Program.cs`。
5. 若外部 AI/USDA 配置缺失，测试需覆盖本地 fallback 或明确阻塞原因。
