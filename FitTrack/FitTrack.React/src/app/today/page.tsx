'use client'

import Link from 'next/link'
import { useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'

import { DailySummaryCard } from '@/components/cards/daily-summary-card'
import { AppShell } from '@/components/layout/app-shell'
import { useLanguage } from '@/components/providers/language-provider'
import { apiFetch } from '@/lib/http'
import type { FoodRecord, ProgressSummary, WorkoutSession } from '@/types/fittrack'

export default function TodayPage() {
  const { language, locale } = useLanguage()
  const isChinese = language === 'zh-CN'
  const progress = useQuery({
    queryKey: ['progress-summary'],
    queryFn: () => apiFetch<ProgressSummary>('/api/progress/summary'),
  })
  const foodRecords = useQuery({
    queryKey: ['food-records'],
    queryFn: () => apiFetch<FoodRecord[]>('/api/food-records'),
  })
  const workoutSessions = useQuery({
    queryKey: ['workout-sessions'],
    queryFn: () => apiFetch<WorkoutSession[]>('/api/workout-sessions'),
  })

  const summary = progress.data
  const latestMeals = useMemo(
    () =>
      [...(foodRecords.data ?? [])]
        .sort((left, right) => new Date(right.consumptionDate).getTime() - new Date(left.consumptionDate).getTime())
        .slice(0, 4),
    [foodRecords.data],
  )
  const latestSessions = useMemo(
    () =>
      [...(workoutSessions.data ?? [])]
        .sort((left, right) => new Date(right.startTime).getTime() - new Date(left.startTime).getTime())
        .slice(0, 3),
    [workoutSessions.data],
  )
  const quickActions = useMemo(
    () =>
      isChinese
        ? [
            {
              id: 'plan',
              eyebrow: '规划今天',
              title: '生成今天的训练和饮食计划',
              body: '一次拿到宏量目标、训练类型和执行重点。',
              prompt: '帮我规划今天的训练和饮食。',
            },
            {
              id: 'meal',
              eyebrow: '分析餐食',
              title: '给这顿饭打分并决定下一步调整',
              body: '适合在上传餐食图片后，或者准备吃饭前快速描述一遍。',
              prompt: '分析这顿饭，并告诉我该怎么调整。',
            },
            {
              id: 'workout',
              eyebrow: '记录训练',
              title: '把零散训练笔记整理成结构化记录',
              body: '适合把今天练了什么、练得怎么样交给教练整理。',
              prompt: '帮我记录今天的训练，并总结效果。',
            },
            {
              id: 'review',
              eyebrow: '晚间复盘',
              title: '结束一天，做恢复和执行度复盘',
              body: '当你想明确知道今天保留什么、明天修正什么时使用。',
              prompt: '帮我做今晚的恢复和营养复盘。',
            },
          ]
        : [
            {
              id: 'plan',
              eyebrow: 'Plan Today',
              title: "Build today's training and nutrition plan",
              body: 'Start the day with macros, training type, and execution priorities in one pass.',
              prompt: "Help me plan today's training and nutrition.",
            },
            {
              id: 'meal',
              eyebrow: 'Analyze Meal',
              title: 'Score a meal and decide the next adjustment',
              body: 'Use this after a meal photo or a quick description of what you are about to eat.',
              prompt: 'Analyze this meal and tell me what to change.',
            },
            {
              id: 'workout',
              eyebrow: 'Log Workout',
              title: 'Turn a rough training note into a structured session',
              body: 'Useful when you want the coach to clean up what you trained and what it means.',
              prompt: "Help me log today's workout and summarize how it went.",
            },
            {
              id: 'review',
              eyebrow: 'Evening Review',
              title: 'Close the day with recovery and adherence review',
              body: 'Use this when you want one clear answer on what to keep and what to fix tomorrow.',
              prompt: "Help me do tonight's recovery and nutrition review.",
            },
          ],
    [isChinese],
  )

  return (
    <AppShell title="Today" titleKey="today">
      <div className="space-y-6">
        <section className="grid gap-6 xl:grid-cols-[minmax(0,1.45fr)_420px]">
          <div className="rounded-[34px] border border-cyan-300/20 bg-[radial-gradient(circle_at_top_left,_rgba(34,211,238,0.18),_transparent_32%),radial-gradient(circle_at_right,_rgba(16,185,129,0.14),_transparent_28%),linear-gradient(180deg,_rgba(8,15,33,0.96),_rgba(2,6,23,0.82))] p-6 shadow-2xl shadow-cyan-950/20">
            <p className="text-xs uppercase tracking-[0.38em] text-cyan-300">{isChinese ? '今日看板' : 'Today Dashboard'}</p>
            <h3 className="mt-4 max-w-4xl text-3xl font-semibold leading-tight text-white">
              {summary?.headline ?? (isChinese ? '把今天的饮食、训练和恢复整合进一个执行闭环。' : "Bring today's food, training, and recovery into one execution loop.")}
            </h3>
            <p className="mt-4 max-w-3xl text-sm leading-7 text-slate-300">
              {isChinese
                ? '这是教练工作台的新入口。先从任务切入，查看今天状态，再进入一个已经带好上下文的引导式对话。'
                : "This view is the new operational front door for the coach. Start from a task, inspect today's status, then move into a guided conversation with context already set."}
            </p>

            <div className="mt-6 grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
              <MetricTile label={isChinese ? '今日热量' : 'Calories Today'} value={`${summary?.caloriesToday?.toFixed(0) ?? '--'} kcal`} />
              <MetricTile label={isChinese ? '今日蛋白质' : 'Protein Today'} value={`${summary?.proteinToday?.toFixed(0) ?? '--'} g`} />
              <MetricTile label={isChinese ? '饮食记录' : 'Food Logs'} value={`${summary?.foodRecordCountToday ?? '--'}`} />
              <MetricTile label={isChinese ? '本周训练' : 'Workouts This Week'} value={`${summary?.workoutCountThisWeek ?? '--'}`} />
            </div>

            <div className="mt-6 flex flex-wrap gap-3">
              <Link
                href={`/chat?draft=${encodeURIComponent(isChinese ? '帮我规划今天的训练和饮食。' : "Help me plan today's training and nutrition.")}`}
                className="rounded-full bg-cyan-300 px-5 py-3 text-xs font-semibold uppercase tracking-[0.3em] text-slate-950"
              >
                {isChinese ? '开始今日计划' : 'Start Today Plan'}
              </Link>
              <Link
                href="/chat"
                className="rounded-full border border-white/10 px-5 py-3 text-xs uppercase tracking-[0.3em] text-white transition hover:bg-white/5"
              >
                {isChinese ? '打开教练工作台' : 'Open Coach Workspace'}
              </Link>
            </div>
          </div>

          <DailySummaryCard
            content={{
              readiness: {
                label: isChinese ? '热量' : 'Calories',
                value: `${summary?.caloriesToday?.toFixed(0) ?? '--'} kcal`,
                delta: isChinese ? `已记录 ${summary?.foodRecordCountToday ?? 0} 餐` : `${summary?.foodRecordCountToday ?? 0} meals logged`,
              },
              weight: {
                label: isChinese ? '蛋白质' : 'Protein',
                value: `${summary?.proteinToday?.toFixed(0) ?? '--'} g`,
                delta: summary?.currentWeightKg ? (isChinese ? `体重 ${summary.currentWeightKg} kg` : `${summary.currentWeightKg} kg body weight`) : undefined,
              },
              sleep: {
                label: isChinese ? '训练' : 'Training',
                value: isChinese ? `本周 ${summary?.workoutCountThisWeek ?? 0} 次` : `${summary?.workoutCountThisWeek ?? 0} this week`,
                delta: summary?.bodyFatPercent ? (isChinese ? `体脂 ${summary.bodyFatPercent}%` : `${summary.bodyFatPercent}% body fat`) : undefined,
              },
              callout: summary?.headline,
              actions: summary?.recommendations,
            }}
          />
        </section>

        <section className="grid gap-4 xl:grid-cols-4">
          {quickActions.map((action) => (
            <Link
              key={action.id}
              href={`/chat?draft=${encodeURIComponent(action.prompt)}`}
              className="rounded-[28px] border border-white/10 bg-slate-950/55 p-5 transition hover:border-cyan-300/30 hover:bg-slate-950/70"
            >
              <p className="text-xs uppercase tracking-[0.35em] text-cyan-300">{action.eyebrow}</p>
              <h3 className="mt-3 text-lg font-semibold text-white">{action.title}</h3>
              <p className="mt-3 text-sm leading-6 text-slate-400">{action.body}</p>
              <p className="mt-5 text-xs uppercase tracking-[0.28em] text-slate-300">{isChinese ? '在教练中打开' : 'Open in Coach'}</p>
            </Link>
          ))}
        </section>

        <section className="grid gap-6 xl:grid-cols-[minmax(0,1.1fr)_minmax(0,0.9fr)]">
          <div className="rounded-[32px] border border-white/10 bg-slate-950/50 p-6">
            <div className="flex items-center justify-between gap-4">
              <div>
                <p className="text-xs uppercase tracking-[0.35em] text-cyan-300">Next Actions</p>
                <h3 className="mt-3 text-2xl font-semibold text-white">{isChinese ? '下一步最该让 Agent 帮你判断什么' : 'What the agent should help you decide next'}</h3>
              </div>
              <Link
                href={`/chat?draft=${encodeURIComponent(isChinese ? '基于我今天的状态，告诉我下一步最重要的一项调整。' : "Given today's status, tell me the single most important adjustment to make next.")}`}
                className="rounded-full border border-cyan-300/30 bg-cyan-300/10 px-4 py-2 text-[11px] uppercase tracking-[0.25em] text-cyan-100"
              >
                {isChinese ? '询问教练' : 'Ask Coach'}
              </Link>
            </div>

            <div className="mt-6 grid gap-4 md:grid-cols-2">
              {(summary?.recommendations?.length ? summary.recommendations : isChinese ? defaultRecommendationsZh : defaultRecommendations).map((item, index) => (
                <div key={`${item}-${index}`} className="rounded-[24px] border border-white/10 bg-white/5 p-4">
                  <p className="text-[11px] uppercase tracking-[0.28em] text-slate-400">{isChinese ? `行动 ${index + 1}` : `Action ${index + 1}`}</p>
                  <p className="mt-2 text-sm leading-6 text-white">{item}</p>
                </div>
              ))}
            </div>
          </div>

          <div className="space-y-6">
            <section className="rounded-[32px] border border-white/10 bg-slate-950/50 p-6">
              <div className="flex items-center justify-between gap-4">
                <div>
                  <p className="text-xs uppercase tracking-[0.35em] text-emerald-300">Recent Meals</p>
                  <h3 className="mt-3 text-xl font-semibold text-white">{isChinese ? '今天实际吃了什么' : 'What today actually looks like'}</h3>
                </div>
                <Link href="/food-records" className="text-xs uppercase tracking-[0.28em] text-slate-300">
                  {isChinese ? '查看记录' : 'Open Log'}
                </Link>
              </div>

              <div className="mt-5 space-y-3">
                {latestMeals.map((meal) => (
                  <div key={meal.id} className="rounded-[24px] border border-white/10 bg-white/5 p-4">
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <p className="font-medium text-white">{meal.foodName}</p>
                        <p className="mt-1 text-sm text-slate-400">{formatDateTime(meal.consumptionDate, locale)}</p>
                      </div>
                      <span className="rounded-full border border-white/10 px-3 py-1 text-[11px] uppercase tracking-[0.25em] text-cyan-200">
                        {meal.calories.toFixed(0)} kcal
                      </span>
                    </div>
                    <p className="mt-3 text-sm text-slate-300">
                      {isChinese ? `蛋白质 ${meal.protein} g | 碳水 ${meal.carbs} g | 脂肪 ${meal.fat} g` : `Protein ${meal.protein} g | Carbs ${meal.carbs} g | Fat ${meal.fat} g`}
                    </p>
                  </div>
                ))}
                {latestMeals.length === 0 ? <EmptyState text={isChinese ? '还没有饮食记录。可以从上传餐食图片或手动快速录入开始。' : 'No meals logged yet. Start from a meal photo or a quick manual entry.'} /> : null}
              </div>
            </section>

            <section className="rounded-[32px] border border-white/10 bg-slate-950/50 p-6">
              <div className="flex items-center justify-between gap-4">
                <div>
                  <p className="text-xs uppercase tracking-[0.35em] text-violet-300">Recent Training</p>
                  <h3 className="mt-3 text-xl font-semibold text-white">{isChinese ? '会影响下一步判断的最近训练' : 'Sessions that should shape the next decision'}</h3>
                </div>
                <Link href="/workouts" className="text-xs uppercase tracking-[0.28em] text-slate-300">
                  {isChinese ? '查看训练' : 'Open Workouts'}
                </Link>
              </div>

              <div className="mt-5 space-y-3">
                {latestSessions.map((session) => (
                  <div key={session.id} className="rounded-[24px] border border-white/10 bg-white/5 p-4">
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <p className="font-medium text-white">{formatDate(session.startTime, locale)}</p>
                        <p className="mt-1 text-sm text-slate-400">
                          {isChinese ? `${session.duration ?? 0} 分钟 | ${session.caloriesBurned ?? 0} 千卡` : `${session.duration ?? 0} min | ${session.caloriesBurned ?? 0} kcal`}
                        </p>
                      </div>
                      <span className="rounded-full border border-white/10 px-3 py-1 text-[11px] uppercase tracking-[0.25em] text-violet-200">
                        {isChinese ? '训练' : 'Session'}
                      </span>
                    </div>
                    {session.notes ? <p className="mt-3 text-sm text-slate-300">{session.notes}</p> : null}
                  </div>
                ))}
                {latestSessions.length === 0 ? <EmptyState text={isChinese ? '还没有训练记录。可以让教练把零散训练笔记整理成结构化记录。' : 'No workout sessions logged yet. Use the coach to turn rough notes into structured training records.'} /> : null}
              </div>
            </section>
          </div>
        </section>
      </div>
    </AppShell>
  )
}

function MetricTile({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-[24px] border border-white/10 bg-white/5 px-4 py-4">
      <p className="text-[11px] uppercase tracking-[0.3em] text-slate-400">{label}</p>
      <p className="mt-2 text-2xl font-semibold text-white">{value}</p>
    </div>
  )
}

function EmptyState({ text }: { text: string }) {
  return <p className="rounded-[24px] border border-dashed border-white/10 bg-white/[0.03] px-4 py-5 text-sm text-slate-400">{text}</p>
}

function formatDateTime(value: string, locale: string) {
  return new Date(value).toLocaleString(locale, {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

function formatDate(value: string, locale: string) {
  return new Date(value).toLocaleDateString(locale, {
    month: 'short',
    day: 'numeric',
  })
}

const defaultRecommendations = [
  "Open the coach with today's context and confirm the main nutrition target before the next meal.",
  'Use meal analysis before dinner if calories or protein still look off.',
  'Close the loop tonight with a short recovery and adherence review.',
]

const defaultRecommendationsZh = [
  '先带着今天的上下文打开教练，并在下一餐前确认核心营养目标。',
  '如果热量或蛋白还不够明确，晚餐前先做一次餐食分析。',
  '今晚用一次简短的恢复与执行度复盘把今天闭环。',
]
