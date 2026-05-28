# FitTrack 增肌 Agent 每周开发设计文档

## 1. 文档目的

本文档基于 `PRODUCT_PLAN.md` 和 `docs/muscle-gain-agent-tech-one-pager.md`，把 6 周 MVP 拆成可实施的开发设计。

目标闭环：

`建档 -> 周训练计划 -> 训练日驱动碳循环 -> 饮食记录 -> 训练记录 -> 每日复盘 -> 次日调整`

实施边界：

1. 主线只落在 `FitTrack.React` 与 `FitTrack.Copilot`。
2. `FitTrack` legacy 主应用不承接新能力。
3. 优先演进现有 Profile、Food、Workout、Progress、Chat 能力。
4. 不重构 solution 结构，不删除或覆盖现有 SQLite 文件。
5. 所有后端模型、迁移、服务注册、认证相关改动必须同步检查 `Program.cs`。

## 2. 总体技术策略

### 2.1 前端策略

`FitTrack.React` 继续作为主工作台：

1. `Today` 是默认执行首页，聚合训练、碳循环、宏量预算、下一步动作。
2. `Chat` 是 Agent 执行入口，负责生成计划、解释原因、处理复盘。
3. `Food Records` 从记录列表升级为当日宏量预算和餐次记录。
4. `Workouts` 从 session 列表升级为训练计划和当日执行台。
5. `Progress` 展示增肌相关趋势，不在 MVP 中做复杂分析。

### 2.2 后端策略

`FitTrack.Copilot` 继续作为 API + Agent Host：

1. 数据写入统一走 API 和 service，不让前端直连数据库。
2. Agent 输出解释与结构化 payload，结构化 payload 可沉淀为计划、记录、复盘卡片。
3. 训练、饮食、复盘优先使用明确 service，避免把业务规则只写在 prompt 中。
4. AI 不可用时，核心 CRUD 和 summary 页面仍可工作；Agent 建议可降级为空状态或模板建议。

### 2.3 最小新增概念

MVP 至少需要这些业务概念：

1. Profile baseline：建档字段和训练/饮食约束。
2. Weekly workout plan：一周训练计划。
3. Workout prescription：动作处方。
4. Carb cycle plan：训练日驱动的高/中/低碳序列。
5. Daily macro target：每日热量和宏量目标。
6. Food record：饮食记录。
7. Workout session log：训练执行记录。
8. Daily review：每日偏差与次日调整。

## 3. Week 1：建档、目标、训练条件、基线

### 3.1 目标

让用户在 5 分钟内完成增肌 Agent 所需的最小建档，并让系统判断是否足够生成第一周训练计划。

### 3.2 后端设计

扩展 `UserProfile` 或新增紧邻 profile 的 baseline 字段，覆盖：

1. 目标信息
   - `goal`: muscle_gain / recomposition / maintenance。
   - `targetWeightKg`。
   - `targetBodyFatPercent`。
   - `targetWeeks` 或 `targetDate`。

2. 训练条件
   - `trainingExperience`: beginner / intermediate / advanced。
   - `trainingLocation`: gym / home / mixed。
   - `availableEquipment`。
   - `weeklyTrainingDays`。
   - `sessionDurationMinutes`。
   - `mainLiftBaselines`：可先用 JSON 字符串或结构化子表，记录 squat / bench / deadlift / row / overhead press 的估算水平。

3. 饮食条件
   - `dietPreference`。
   - `allergies`。
   - `cookingAccess`。
   - `enableCarbCycling`。

4. 基线输出
   - `estimatedTdee`。
   - `initialCalorieTarget`。
   - `initialProteinTarget`。
   - `baselineCompletedAt`。

API：

1. 扩展 `GET /api/profile` 返回新字段。
2. 扩展 `PUT /api/profile` 保存新字段。
3. 可在 response 中增加 `profileCompleteness`，包含缺失字段和是否可生成计划。

服务：

1. `ProfileService` 负责读写 profile。
2. 新增轻量 `BaselineCalculator` 或 profile 内部 helper，计算 TDEE、蛋白目标、建档完整度。

### 3.3 前端设计

`settings/profile`：

1. 增加增肌目标区。
2. 增加训练条件区。
3. 增加饮食和碳循环偏好区。
4. 保存后显示“可生成第一周计划 / 还缺哪些字段”。

`Today`：

1. 如果 profile 不完整，显示建档提醒卡。
2. 如果 profile 完整但未生成训练计划，显示“生成第一周计划”入口。

`Chat`：

1. 推荐问题增加“帮我完成增肌建档”。
2. Agent 追问只围绕缺失字段。

### 3.4 Agent 设计

建档阶段由 `CoachSupervisorAgent` 调用 profile 上下文：

1. 判断缺失字段。
2. 只问一轮内能回答的关键问题。
3. 输出 `profile_baseline` structured payload。
4. 不直接生成训练计划，除非 `profileCompleteness.canCreateWorkoutPlan = true`。

结构化输出建议：

```json
{
  "type": "profile_baseline",
  "summary": "已完成增肌建档，可以生成第一周训练计划。",
  "missingFields": [],
  "baseline": {
    "goal": "muscle_gain",
    "weeklyTrainingDays": 4,
    "trainingLocation": "gym",
    "enableCarbCycling": true
  },
  "recommendedActions": ["生成第一周训练计划"]
}
```

### 3.5 验收与测试

验收：

1. 新用户能保存增肌建档字段。
2. Profile API 能返回完整度和缺失字段。
3. Today 能根据完整度显示下一步动作。

测试：

1. `dotnet build FitTrack.Copilot/FitTrack.Copilot.csproj`。
2. `npm run build` in `FitTrack/FitTrack.React`。
3. 手动验证 profile 保存、刷新、重新登录后字段仍存在。

## 4. Week 2：周训练计划与力量增肌混合结构

### 4.1 目标

基于 Week 1 建档信息，生成并保存一周力量增肌混合训练计划。

### 4.2 后端设计

优先扩展现有 `WorkoutPlan`、`WorkoutDay`、`Exercise`：

1. `WorkoutPlan`
   - `GoalType`。
   - `StartDate`。
   - `EndDate`。
   - `Status`: draft / active / archived。
   - `GeneratedBy`: user / agent / template。

2. `WorkoutDay`
   - `Date` 或 `DayIndex`。
   - `TrainingTheme`: upper / lower / push / pull / legs / full_body / rest。
   - `TrainingLoadType`: heavy / moderate / light / rest。
   - `EstimatedDurationMinutes`。

3. `Exercise`
   - `ExerciseRole`: main_lift / accessory / warmup / finisher。
   - `TargetMuscleGroup`。
   - `TargetRpe`。
   - `WeightPrescription`。
   - `AlternativeExercises`。

API：

1. `GET /api/workout-plans` 返回用户计划列表。
2. `POST /api/workout-plans` 创建计划，支持前端保存 Agent 生成结果。
3. `GET /api/workout-plans/{id}` 可选，若当前列表响应过大再拆。
4. `POST /api/chat/messages` 继续触发 Agent 生成计划。

服务：

1. `FitnessService` 负责计划 CRUD。
2. 新增 `WorkoutPlanGenerator`，把 profile baseline 转成确定性初稿。
3. Agent 可基于 generator 初稿做解释和微调，不把所有结构生成逻辑都放进 prompt。

### 4.3 前端设计

`Workouts`：

1. 顶部显示当前 active plan。
2. 周视图展示训练日主题、主项、时长、碳日占位。
3. 当日视图展示动作、组数、次数、RPE、休息。
4. 提供“设为当前计划”“重新生成计划”“进入今日训练”。

`Today`：

1. 显示今日训练主题。
2. 显示今日主项和目标容量。
3. 如果今天是休息日，显示恢复重点。

`Chat`：

1. 支持“根据我的资料生成第一周训练计划”。
2. 右栏或卡片输出 `WorkoutPlanCard`。

### 4.4 Agent 设计

`TrainingAgent` 初版职责：

1. 读取 profile baseline。
2. 判断每周训练天数和器械条件。
3. 生成 3-5 天训练结构。
4. 输出主项 + 辅助动作 + RPE/组次。
5. 给出计划原因和风险提醒。

结构化输出建议：

```json
{
  "type": "weekly_workout_plan",
  "planName": "4-Day Strength Hypertrophy Base",
  "days": [
    {
      "dayIndex": 1,
      "theme": "upper",
      "loadType": "heavy",
      "exercises": [
        {
          "name": "Bench Press",
          "role": "main_lift",
          "sets": 4,
          "reps": "4-6",
          "targetRpe": 8
        }
      ]
    }
  ],
  "recommendedActions": ["保存为当前计划", "生成碳循环"]
}
```

### 4.5 验收与测试

验收：

1. Profile 完整用户能生成第一周计划。
2. 计划可保存并在 Workouts 展示。
3. Today 能显示今日训练主题。

测试：

1. 后端 workout plan CRUD。
2. 前端 Workouts 计划渲染和空状态。
3. `dotnet build` 与 `npm run build`。

## 5. Week 3：训练日驱动碳循环与每日宏量目标

### 5.1 目标

根据训练计划自动生成高/中/低碳日，并保存每日宏量目标。

### 5.2 后端设计

新增或扩展模型：

1. `CarbCyclePlan`
   - `UserId`。
   - `WorkoutPlanId`。
   - `StartDate`。
   - `EndDate`。
   - `Status`。

2. `DailyMacroTarget`
   - `Date`。
   - `CarbDayType`: high / medium / low。
   - `Calories`。
   - `ProteinGrams`。
   - `CarbsGrams`。
   - `FatGrams`。
   - `Reason`。
   - `Source`: generated / adjusted / user_override。

业务规则：

1. heavy lower / full body high volume -> high。
2. normal upper / moderate session -> medium。
3. rest / recovery / light cardio -> low。
4. 蛋白稳定，优先调整碳水和脂肪。
5. 周平均热量保持增肌目标，不因低碳日整体吃太少。

服务：

1. 新增 `MacroTargetService` 或放入 Nutrition domain service。
2. 复用前端现有 `carb-cycle` 计算逻辑时，建议迁移一份后端实现作为业务真相源；前端只负责展示。

API：

1. `GET /api/daily-targets?date=yyyy-mm-dd`。
2. `GET /api/daily-targets/week?start=yyyy-mm-dd`。
3. `POST /api/workout-plans/{id}/carb-cycle` 生成或刷新碳循环。
4. 如果暂不新增端点，可先把今日目标合并进 `GET /api/progress/summary`，但不建议长期如此。

### 5.3 前端设计

`Today`：

1. 今日碳日类型徽标。
2. 宏量目标四项：热量、蛋白、碳水、脂肪。
3. 原因说明：例如“今天是高容量腿部训练，所以安排高碳日”。
4. 训练前后餐建议入口。

`Workouts`：

1. 周计划中显示每个训练日对应碳日。
2. 训练日变更后提示“需要重新计算碳循环”。

`Chat`：

1. 推荐问题增加“今天为什么是高碳日？”。
2. 支持“我今天不练了，饮食怎么调整？”。

### 5.4 Agent 设计

`NutritionAgent` 初版职责：

1. 读取 workout plan 和 profile。
2. 生成 carb cycle plan。
3. 解释每日碳水分配。
4. 针对临时训练变化给出调整建议。

结构化输出建议：

```json
{
  "type": "daily_macro_target",
  "date": "2026-05-28",
  "carbDayType": "high",
  "calories": 2800,
  "proteinGrams": 170,
  "carbsGrams": 360,
  "fatGrams": 70,
  "reason": "高容量腿部训练日，需要更高碳水支持表现和恢复。",
  "recommendedActions": ["安排训练前碳水", "晚间复盘摄入偏差"]
}
```

### 5.5 验收与测试

验收：

1. 训练计划生成后可生成一周碳循环。
2. Today 能展示今日宏量目标和碳日原因。
3. 修改训练日后能提示刷新碳循环。

测试：

1. 高/中/低碳映射单元测试或服务级测试。
2. 每日宏量目标 API。
3. `dotnet build` 与 `npm run build`。

## 6. Week 4：饮食记录与当日宏量预算追踪

### 6.1 目标

把每日宏量目标和 food record 打通，让用户记录后能看到剩余预算和下一餐纠偏。

### 6.2 后端设计

扩展 `FoodRecord`：

1. `MealType` 扩展支持 pre_workout / post_workout。
2. `Source`: user_confirmed / agent_estimated / usda_lookup。
3. `Confidence`。
4. `Notes` 可选。

新增 summary 逻辑：

1. `DailyNutritionBudget`
   - target calories/macros。
   - consumed calories/macros。
   - remaining calories/macros。
   - completion percentages。
   - deviations。

服务：

1. `FoodRecordService` 保存记录。
2. `MacroTargetService` 提供目标。
3. `ProgressService` 或新 summary service 聚合今日预算。

API：

1. `GET /api/food-records?date=yyyy-mm-dd`。
2. `POST /api/food-records`。
3. `GET /api/daily-nutrition-budget?date=yyyy-mm-dd`，或合并到 daily targets summary。

### 6.3 前端设计

`Food Records`：

1. 顶部显示今日预算：目标、已摄入、剩余、偏差。
2. 按餐次分组展示记录。
3. 快速新增记录表单。
4. 每条记录显示来源和置信度。

`Today`：

1. 显示热量、蛋白、碳水、脂肪进度。
2. 显示“下一餐重点”：补蛋白 / 控脂肪 / 补碳水。

`Chat`：

1. 支持“我午餐吃了...”并生成可保存记录。
2. Meal analysis card 增加保存动作。

### 6.4 Agent 设计

`NutritionAgent` 新增职责：

1. 将自然语言餐食转换为候选 food record。
2. 标注估算值和置信度。
3. 计算记录后预算变化。
4. 给出下一餐纠偏。

结构化输出建议：

```json
{
  "type": "meal_log_suggestion",
  "mealType": "lunch",
  "items": [
    {
      "foodName": "鸡胸肉米饭",
      "calories": 620,
      "protein": 45,
      "carbs": 78,
      "fat": 12,
      "confidence": 0.72
    }
  ],
  "budgetAfterSave": {
    "remainingProtein": 65,
    "remainingCarbs": 140,
    "remainingFat": 32
  },
  "recommendedActions": ["保存到饮食记录", "晚餐优先补蛋白"]
}
```

### 6.5 验收与测试

验收：

1. 用户能快速保存一餐。
2. Food Records 和 Today 预算实时更新。
3. Agent 能给出下一餐纠偏。

测试：

1. Food record 保存和按日期查询。
2. Daily budget 聚合计算。
3. 前端新增记录后 query invalidation。
4. `dotnet build` 与 `npm run build`。

## 7. Week 5：训练日志、完成度、RPE、疲劳反馈

### 7.1 目标

把训练计划转化为可记录 session，并让 Agent 能基于执行反馈判断下次是否进阶、维持或降量。

### 7.2 后端设计

扩展 `WorkoutSession` 与 `ExerciseSession`：

1. `WorkoutSession`
   - `WorkoutDayId`。
   - `CompletionRate`。
   - `OverallRpe`。
   - `FatigueLevel`。
   - `SorenessLevel`。
   - `EnergyLevel`。
   - `AdjustmentRecommendation`。

2. `ExerciseSession`
   - `PlannedExerciseId`。
   - `CompletedSets`。
   - `CompletedReps`。
   - `Weight`。
   - `ActualRpe`。
   - `Skipped`。
   - `SubstitutedExerciseName`。
   - `Notes`。

服务：

1. `WorkoutSessionService` 保存 session 和动作日志。
2. 新增 `TrainingAdjustmentService`，根据完成度、RPE、疲劳输出调整建议。

API：

1. `GET /api/workout-sessions?date=yyyy-mm-dd`。
2. `POST /api/workout-sessions` 支持动作明细和反馈。
3. `GET /api/workout-plans/current/today` 可选，返回今日训练处方。

### 7.3 前端设计

`Workouts`：

1. 今日训练执行台。
2. 每个动作支持填写重量、次数、RPE。
3. 支持标记跳过、替换、降重量。
4. 训练结束后填写整体疲劳、酸痛、精力。
5. 保存后显示 Agent 调整建议。

`Today`：

1. 显示今日训练是否完成。
2. 显示主项完成情况。
3. 显示复盘入口。

`Progress`：

1. 显示本周训练完成次数。
2. 可先用简化指标：训练完成率、最近主项表现。

### 7.4 Agent 设计

`TrainingAgent` 新增职责：

1. 解读训练日志。
2. 判断主项是否可进阶。
3. 判断疲劳是否需要降量。
4. 输出下次建议。

规则初版：

1. 完成率 >= 90%，主项 RPE <= 8：下次可小幅进阶。
2. 完成率 >= 75%，RPE 8-9：维持或微调。
3. 完成率 < 75% 或疲劳高：降量、替代动作或延后进阶。

结构化输出建议：

```json
{
  "type": "workout_adjustment",
  "sessionId": 123,
  "completionRate": 0.86,
  "overallRpe": 8,
  "fatigueLevel": 3,
  "recommendation": "下次卧推维持重量，辅助推举减少一组。",
  "reason": "主项完成但最后两组接近力竭，继续加重可能影响恢复。",
  "recommendedActions": ["保存训练复盘", "明天保持中碳日"]
}
```

### 7.5 验收与测试

验收：

1. 用户能按计划保存一次训练 session。
2. 保存后能看到完成度、RPE、疲劳摘要。
3. Agent 能输出下次训练调整。

测试：

1. Workout session 明细保存。
2. 调整规则边界值。
3. 前端执行台表单。
4. `dotnet build` 与 `npm run build`。

## 8. Week 6：每日复盘、偏差纠正、次日调整

### 8.1 目标

打通 MVP 闭环，让每天结束时系统能汇总饮食、训练、恢复偏差，并生成次日动作。

### 8.2 后端设计

新增 `DailyReview`：

1. `UserId`。
2. `Date`。
3. `WorkoutCompleted`。
4. `WorkoutCompletionRate`。
5. `CaloriesDeviation`。
6. `ProteinDeviation`。
7. `CarbsDeviation`。
8. `FatDeviation`。
9. `FatigueLevel`。
10. `RecoveryNote`。
11. `NextDayTrainingAdjustment`。
12. `NextDayCarbDayAdjustment`。
13. `NextDayNutritionFocus`。
14. `RecommendedActionsJson`。

服务：

1. `DailyReviewService` 汇总 food records、daily target、workout session。
2. `ReviewAgent` 生成解释和次日调整。
3. `ProgressService` 纳入 daily review 指标。

API：

1. `GET /api/daily-reviews?date=yyyy-mm-dd`。
2. `POST /api/daily-reviews` 生成或保存复盘。
3. `GET /api/progress/summary` 返回复盘相关摘要。

### 8.3 前端设计

`Today`：

1. 晚间复盘入口。
2. 复盘卡片展示：训练完成、宏量偏差、疲劳、明日重点。
3. 次日动作清单。

`Chat`：

1. 推荐问题增加“帮我做今晚复盘”。
2. 复盘完成后右侧或主区域展示 `DailyReviewCard`。

`Progress`：

1. 展示一周执行趋势。
2. MVP 可先展示卡片，不做复杂图表。

### 8.4 Agent 设计

`ReviewAgent` 职责：

1. 聚合当日目标、饮食记录、训练记录、疲劳反馈。
2. 识别关键偏差。
3. 输出明日训练强度建议。
4. 输出明日碳日是否保持或调整。
5. 输出 1-3 条明确行动。

结构化输出建议：

```json
{
  "type": "daily_review",
  "date": "2026-05-28",
  "summary": "今天训练完成度较高，但蛋白少 35g，高碳日碳水也偏低。",
  "deviations": {
    "calories": -280,
    "protein": -35,
    "carbs": -70,
    "fat": 8
  },
  "nextDayAdjustment": {
    "training": "明天维持原计划，但主项不要加重。",
    "carbDay": "保持中碳日。",
    "nutritionFocus": "早餐和训练后优先补蛋白与碳水。"
  },
  "recommendedActions": [
    "明早补一餐高蛋白早餐",
    "训练后记录主项 RPE",
    "晚餐前检查剩余碳水"
  ]
}
```

### 8.5 验收与测试

验收：

1. 用户能生成一次每日复盘。
2. 复盘卡片能展示偏差和次日建议。
3. 次日 Today 能引用复盘建议。

测试：

1. Daily review 聚合计算。
2. 缺训练记录、缺饮食记录、休息日三类边界状态。
3. 前端复盘入口和卡片展示。
4. `dotnet build` 与 `npm run build`。

## 9. 跨周依赖与风险

### 9.1 依赖顺序

1. Week 2 依赖 Week 1 profile baseline。
2. Week 3 依赖 Week 2 workout plan。
3. Week 4 依赖 Week 3 daily macro target。
4. Week 5 依赖 Week 2 workout plan。
5. Week 6 依赖 Week 3、Week 4、Week 5 的记录数据。

### 9.2 主要风险

1. 数据模型过早复杂化。
   - 处理方式：MVP 先支持当前计划、当日目标、当日复盘，不做多周期历史优化。

2. Agent 输出不稳定。
   - 处理方式：核心计算用 service，Agent 只负责解释、编排和建议。

3. 饮食估算不可信。
   - 处理方式：区分估算值和用户确认值，展示 confidence。

4. 训练调整建议过激。
   - 处理方式：默认保守进阶，RPE 高或疲劳高时优先维持/降量。

5. AI/USDA 外部依赖缺失。
   - 处理方式：CRUD、计划和预算页面可离线工作，AI 建议显示降级状态。

## 10. 最终 MVP 验收清单

1. 建档完成后，用户能生成第一周力量增肌计划。
2. 训练计划能生成对应碳循环和每日宏量目标。
3. Today 能展示训练日、碳日、宏量预算和下一步动作。
4. 用户能保存饮食记录，并看到预算偏差。
5. 用户能保存训练记录，并看到完成度/RPE/疲劳。
6. 用户能生成每日复盘，并得到次日训练和饮食建议。
7. Chat 输出的重要结果能沉淀为卡片、计划或记录。
8. `FitTrack.Copilot` build 通过。
9. `FitTrack.React` build 通过。
