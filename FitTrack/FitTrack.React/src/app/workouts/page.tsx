'use client'

import { useQuery } from '@tanstack/react-query'

import { AppShell } from '@/components/layout/app-shell'
import { useLanguage } from '@/components/providers/language-provider'
import { apiFetch } from '@/lib/http'
import type { WorkoutPlan, WorkoutSession } from '@/types/fittrack'

export default function WorkoutsPage() {
  const { language, locale } = useLanguage()
  const isChinese = language === 'zh-CN'
  const plans = useQuery({
    queryKey: ['workout-plans'],
    queryFn: () => apiFetch<WorkoutPlan[]>('/api/workout-plans'),
  })
  const sessions = useQuery({
    queryKey: ['workout-sessions'],
    queryFn: () => apiFetch<WorkoutSession[]>('/api/workout-sessions'),
  })

  return (
    <AppShell title="Workouts" titleKey="workouts">
      <div className="grid gap-6 xl:grid-cols-2">
        <section className="rounded-[32px] border border-white/10 bg-slate-950/50 p-6">
          <p className="text-xs uppercase tracking-[0.35em] text-cyan-300">{isChinese ? '计划' : 'Plans'}</p>
          <div className="mt-5 space-y-4">
            {plans.data?.map((plan) => (
              <div key={plan.id} className="rounded-2xl border border-white/10 bg-white/5 p-4">
                <p className="font-medium text-white">{plan.planName}</p>
                <p className="mt-1 text-sm text-slate-400">{plan.description || (isChinese ? '暂无描述' : 'No description')}</p>
                <p className="mt-3 text-xs uppercase tracking-[0.25em] text-slate-500">
                  {isChinese ? `已安排 ${plan.workoutDays.length} 天` : `${plan.workoutDays.length} scheduled days`}
                </p>
              </div>
            ))}
            {plans.data?.length === 0 ? <p className="text-sm text-slate-400">{isChinese ? '还没有训练计划。' : 'No workout plans yet.'}</p> : null}
          </div>
        </section>

        <section className="rounded-[32px] border border-white/10 bg-slate-950/50 p-6">
          <p className="text-xs uppercase tracking-[0.35em] text-emerald-300">{isChinese ? '训练记录' : 'Sessions'}</p>
          <div className="mt-5 space-y-4">
            {sessions.data?.map((session) => (
              <div key={session.id} className="rounded-2xl border border-white/10 bg-white/5 p-4">
                <p className="font-medium text-white">{new Date(session.startTime).toLocaleDateString(locale)}</p>
                <p className="mt-1 text-sm text-slate-400">
                  {isChinese ? `${session.duration ?? 0} 分钟 | ${session.caloriesBurned ?? 0} 千卡` : `${session.duration ?? 0} min | ${session.caloriesBurned ?? 0} kcal`}
                </p>
                {session.notes ? <p className="mt-2 text-sm text-slate-300">{session.notes}</p> : null}
              </div>
            ))}
            {sessions.data?.length === 0 ? <p className="text-sm text-slate-400">{isChinese ? '还没有训练记录。' : 'No workout sessions logged.'}</p> : null}
          </div>
        </section>
      </div>
    </AppShell>
  )
}
