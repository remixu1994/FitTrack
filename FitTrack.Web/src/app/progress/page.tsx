'use client'

import { useQuery } from '@tanstack/react-query'

import { DailySummaryCard } from '@/components/cards/daily-summary-card'
import { AppShell } from '@/components/layout/app-shell'
import { apiFetch } from '@/lib/http'
import type { ProgressSummary } from '@/types/fittrack'

export default function ProgressPage() {
  const query = useQuery({
    queryKey: ['progress-summary'],
    queryFn: () => apiFetch<ProgressSummary>('/api/progress/summary'),
  })

  const summary = query.data

  return (
    <AppShell title="Progress">
      <div className="grid gap-6 xl:grid-cols-[minmax(0,1.2fr)_420px]">
        <section className="rounded-[32px] border border-white/10 bg-slate-950/50 p-6">
          <p className="text-xs uppercase tracking-[0.35em] text-cyan-300">Status</p>
          <h3 className="mt-3 text-2xl font-semibold text-white">{summary?.headline ?? 'Loading progress...'}</h3>
          <div className="mt-6 grid gap-4 sm:grid-cols-2">
            <Stat label="Today Calories" value={`${summary?.caloriesToday?.toFixed(0) ?? '--'} kcal`} />
            <Stat label="Workouts This Week" value={`${summary?.workoutCountThisWeek ?? '--'}`} />
            <Stat label="Weight" value={summary?.currentWeightKg ? `${summary.currentWeightKg} kg` : '--'} />
            <Stat label="Body Fat" value={summary?.bodyFatPercent ? `${summary.bodyFatPercent}%` : '--'} />
          </div>
          <ul className="mt-6 space-y-2 text-sm text-slate-300">
            {summary?.recommendations.map((item) => <li key={item}>- {item}</li>)}
          </ul>
        </section>
        <DailySummaryCard
          content={{
            readiness: { label: 'Food Logs', value: `${summary?.foodRecordCountToday ?? 0}` },
            weight: { label: 'Weight', value: summary?.currentWeightKg ? `${summary.currentWeightKg} kg` : '--' },
            sleep: { label: 'Workouts', value: `${summary?.workoutCountThisWeek ?? 0} this week` },
            callout: summary?.headline,
            actions: summary?.recommendations,
          }}
        />
      </div>
    </AppShell>
  )
}

function Stat({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-2xl border border-white/10 bg-white/5 p-4">
      <p className="text-xs uppercase tracking-[0.35em] text-slate-500">{label}</p>
      <p className="mt-2 text-xl font-semibold text-white">{value}</p>
    </div>
  )
}
