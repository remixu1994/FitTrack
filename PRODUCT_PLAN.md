# PRODUCT_PLAN.md

## 1. Product Direction

FitTrack 当前最有潜力的产品形态，不是“能聊天的健身助手”，而是“以 agent 驱动的每日健身执行系统”。

目标不是让用户和 AI 多聊，而是让用户每天更快完成 4 个关键动作：

1. 确认今天该怎么吃
2. 确认今天该怎么练
3. 看到当前偏差和风险
4. 明确下一步该执行什么

主交付链路继续保持：

`FitTrack.React -> FitTrack.Copilot API -> Agent orchestration`

## 2. Product Problem

当前主线已经有不错的聊天工作台，但仍有三个明显问题：

1. 聊天太强，闭环太弱
   用户可以得到建议，但很难把建议快速变成记录、计划和复盘。

2. 结构化页面太薄
   `food-records`、`workouts`、`progress` 目前更像数据展示页，不像执行工作台。

3. 信息架构仍偏“会话工具”
   thread 对系统友好，但对普通用户不天然；健身场景更适合按“今天 / 本周 / 趋势”组织。

## 3. Product Principles

后续设计和实现遵循以下原则：

1. Dashboard first, chat second
   先看到今日状态和动作，再进入对话。

2. Actionable outputs over descriptive outputs
   重要结果必须能执行、保存或复用，不能只停留在聊天气泡里。

3. Day-based workflow over thread-based workflow
   默认按“今天”组织，而不是让用户先理解 session/thread。

4. Deviation management over generic coaching
   重点展示偏差、剩余预算、补救动作，而不是泛泛建议。

5. Trust before sophistication
   比起更会聊天，更优先提升估算来源、置信度和可解释性。

## 4. Phased Delivery

### Phase 1: Make Daily Execution Real

目标：把现有 agent 从“建议生成器”推进到“每日执行工具”。

交付重点：

1. Today Dashboard
   汇总今日热量、蛋白、训练、最近记录和建议动作。

2. Quick Actions
   把常见任务做成结构化入口：
   - 规划今天
   - 分析这顿饭
   - 记录训练
   - 做晚间复盘

3. Chat-to-action bridging
   让 dashboard 和 chat 不再割裂，dashboard 的动作应能直接把用户带到对应任务上下文。

4. Insight rail strengthening
   让 `Daily Summary`、`Day Plan`、`Meal Analysis` 成为行动面板，而不是仅展示结果。

### Phase 2: Build Closed Loops

目标：把建议真正落到记录与复盘里。

交付重点：

1. Meal analysis -> save food record
2. Day plan -> save today plan
3. Evening review -> structured daily review card
4. Workout chat -> structured workout session logging

### Phase 3: Build Weekly Intelligence

目标：从单日工具升级为持续决策系统。

交付重点：

1. Weekly adherence summary
2. Trend-based adjustments
3. Pattern detection
4. Morning brief and event-triggered nudges

## 5. Workstreams

### A. Frontend Experience

1. 新增 Today Dashboard 页面
2. 调整导航优先级，突出 Today / Coach
3. 为 chat 增加从 dashboard 带入任务上下文的能力
4. 强化卡片的动作属性和状态表达

### B. Agent Interaction Model

1. 明确四类高频任务模板
2. 让响应更稳定地产出结构化卡片
3. 为 meal / day plan / review / workout 形成稳定输出协议

### C. Data and Persistence

1. 把 meal analysis 结果可回写 food record
2. 把 workout conversation 可回写 workout session
3. 为每日计划和每日复盘准备持久化模型

### D. Trust and Observability

1. 让关键建议提供来源和置信度
2. 区分“估算值”和“用户确认值”
3. 记录关键链路转化：
   - 从 quick action 到 chat
   - 从 chat 到 record
   - 从 chat 到 saved plan

## 6. Immediate Backlog

本轮先做以下内容：

1. 新增 `Today` 页面
2. 导航接入 `Today`
3. 增加 dashboard quick actions
4. 支持从 quick action 跳转到带上下文的 `Chat`

后续紧跟的两项：

1. Meal analysis card 增加 “Save to food log” 动作
2. Evening review card 标准化输出

## 7. Success Criteria

Phase 1 完成后，产品应满足以下判断标准：

1. 新用户进入后，不需要先理解 thread，也能知道今天该做什么
2. 用户能通过 dashboard 在 1 次点击内进入高频任务
3. chat 不再是孤立入口，而是 dashboard 的执行引擎
4. 重要结果开始从“聊天内容”转成“稳定卡片和动作”
