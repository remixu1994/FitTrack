'use client'

import Link from 'next/link'
import { useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'

import { DailySummaryCard } from '@/components/cards/daily-summary-card'
import { AppShell } from '@/components/layout/app-shell'
import { apiFetch } from '@/lib/http'
import type { FoodRecord, ProgressSummary, WorkoutSession } from '@/types/fittrack'

const quickActions = [
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
]

export default function TodayPage() {
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

  return (
    <AppShell title="Today">
      <div className="space-y-6">
        <section className="grid gap-6 xl:grid-cols-[minmax(0,1.45fr)_420px]">
          <div className="rounded-[34px] border border-cyan-300/20 bg-[radial-gradient(circle_at_top_left,_rgba(34,211,238,0.18),_transparent_32%),radial-gradient(circle_at_right,_rgba(16,185,129,0.14),_transparent_28%),linear-gradient(180deg,_rgba(8,15,33,0.96),_rgba(2,6,23,0.82))] p-6 shadow-2xl shadow-cyan-950/20">
            <p className="text-xs uppercase tracking-[0.38em] text-cyan-300">Today Dashboard</p>
            <h3 className="mt-4 max-w-4xl text-3xl font-semibold leading-tight text-white">
              {summary?.headline ?? "Bring today's food, training, and recovery into one execution loop."}
            </h3>
            <p className="mt-4 max-w-3xl text-sm leading-7 text-slate-300">
              This view is the new operational front door for the coach. Start from a task, inspect today's status, then move into a guided
              conversation with context already set.
            </p>

            <div className="mt-6 grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
              <MetricTile label="Calories Today" value={`${summary?.caloriesToday?.toFixed(0) ?? '--'} kcal`} />
              <MetricTile label="Protein Today" value={`${summary?.proteinToday?.toFixed(0) ?? '--'} g`} />
              <MetricTile label="Food Logs" value={`${summary?.foodRecordCountToday ?? '--'}`} />
              <MetricTile label="Workouts This Week" value={`${summary?.workoutCountThisWeek ?? '--'}`} />
            </div>

            <div className="mt-6 flex flex-wrap gap-3">
              <Link
                href={`/chat?draft=${encodeURIComponent("Help me plan today's training and nutrition.")}`}
                className="rounded-full bg-cyan-300 px-5 py-3 text-xs font-semibold uppercase tracking-[0.3em] text-slate-950"
              >
                Start Today Plan
              </Link>
              <Link
                href="/chat"
                className="rounded-full border border-white/10 px-5 py-3 text-xs uppercase tracking-[0.3em] text-white transition hover:bg-white/5"
              >
                Open Coach Workspace
              </Link>
            </div>
          </div>

          <DailySummaryCard
            content={{
              readiness: {
                label: 'Calories',
                value: `${summary?.caloriesToday?.toFixed(0) ?? '--'} kcal`,
                delta: `${summary?.foodRecordCountToday ?? 0} meals logged`,
              },
              weight: {
                label: 'Protein',
                value: `${summary?.proteinToday?.toFixed(0) ?? '--'} g`,
                delta: summary?.currentWeightKg ? `${summary.currentWeightKg} kg body weight` : undefined,
              },
              sleep: {
                label: 'Training',
                value: `${summary?.workoutCountThisWeek ?? 0} this week`,
                delta: summary?.bodyFatPercent ? `${summary.bodyFatPercent}% body fat` : undefined,
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
              <p className="mt-5 text-xs uppercase tracking-[0.28em] text-slate-300">Open in Coach</p>
            </Link>
          ))}
        </section>

        <section className="grid gap-6 xl:grid-cols-[minmax(0,1.1fr)_minmax(0,0.9fr)]">
          <div className="rounded-[32px] border border-white/10 bg-slate-950/50 p-6">
            <div className="flex items-center justify-between gap-4">
              <div>
                <p className="text-xs uppercase tracking-[0.35em] text-cyan-300">Next Actions</p>
                <h3 className="mt-3 text-2xl font-semibold text-white">What the agent should help you decide next</h3>
              </div>
              <Link
                href={`/chat?draft=${encodeURIComponent("Given today's status, tell me the single most important adjustment to make next.")}`}
                className="rounded-full border border-cyan-300/30 bg-cyan-300/10 px-4 py-2 text-[11px] uppercase tracking-[0.25em] text-cyan-100"
              >
                Ask Coach
              </Link>
            </div>

            <div className="mt-6 grid gap-4 md:grid-cols-2">
              {(summary?.recommendations?.length ? summary.recommendations : defaultRecommendations).map((item, index) => (
                <div key={`${item}-${index}`} className="rounded-[24px] border border-white/10 bg-white/5 p-4">
                  <p className="text-[11px] uppercase tracking-[0.28em] text-slate-400">Action {index + 1}</p>
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
                  <h3 className="mt-3 text-xl font-semibold text-white">What today actually looks like</h3>
                </div>
                <Link href="/food-records" className="text-xs uppercase tracking-[0.28em] text-slate-300">
                  Open Log
                </Link>
              </div>

              <div className="mt-5 space-y-3">
                {latestMeals.map((meal) => (
                  <div key={meal.id} className="rounded-[24px] border border-white/10 bg-white/5 p-4">
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <p className="font-medium text-white">{meal.foodName}</p>
                        <p className="mt-1 text-sm text-slate-400">{formatDateTime(meal.consumptionDate)}</p>
                      </div>
                      <span className="rounded-full border border-white/10 px-3 py-1 text-[11px] uppercase tracking-[0.25em] text-cyan-200">
                        {meal.calories.toFixed(0)} kcal
                      </span>
                    </div>
                    <p className="mt-3 text-sm text-slate-300">
                      Protein {meal.protein} g | Carbs {meal.carbs} g | Fat {meal.fat} g
                    </p>
                  </div>
                ))}
                {latestMeals.length === 0 ? <EmptyState text="No meals logged yet. Start from a meal photo or a quick manual entry." /> : null}
              </div>
            </section>

            <section className="rounded-[32px] border border-white/10 bg-slate-950/50 p-6">
              <div className="flex items-center justify-between gap-4">
                <div>
                  <p className="text-xs uppercase tracking-[0.35em] text-violet-300">Recent Training</p>
                  <h3 className="mt-3 text-xl font-semibold text-white">Sessions that should shape the next decision</h3>
                </div>
                <Link href="/workouts" className="text-xs uppercase tracking-[0.28em] text-slate-300">
                  Open Workouts
                </Link>
              </div>

              <div className="mt-5 space-y-3">
                {latestSessions.map((session) => (
                  <div key={session.id} className="rounded-[24px] border border-white/10 bg-white/5 p-4">
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <p className="font-medium text-white">{formatDate(session.startTime)}</p>
                        <p className="mt-1 text-sm text-slate-400">
                          {session.duration ?? 0} min | {session.caloriesBurned ?? 0} kcal
                        </p>
                      </div>
                      <span className="rounded-full border border-white/10 px-3 py-1 text-[11px] uppercase tracking-[0.25em] text-violet-200">
                        Session
                      </span>
                    </div>
                    {session.notes ? <p className="mt-3 text-sm text-slate-300">{session.notes}</p> : null}
                  </div>
                ))}
                {latestSessions.length === 0 ? <EmptyState text="No workout sessions logged yet. Use the coach to turn rough notes into structured training records." /> : null}
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

function formatDateTime(value: string) {
  return new Date(value).toLocaleString([], {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

function formatDate(value: string) {
  return new Date(value).toLocaleDateString([], {
    month: 'short',
    day: 'numeric',
  })
}

const defaultRecommendations = [
  "Open the coach with today's context and confirm the main nutrition target before the next meal.",
  'Use meal analysis before dinner if calories or protein still look off.',
  'Close the loop tonight with a short recovery and adherence review.',
]
