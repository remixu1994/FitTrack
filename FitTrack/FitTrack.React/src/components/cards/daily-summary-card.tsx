import { useLanguage } from '@/components/providers/language-provider'

export type SummaryMetric = {
  label: string
  value: string
  delta?: string
  intent?: string
}

export type DailySummaryCardContent = {
  readiness?: SummaryMetric
  weight?: SummaryMetric
  sleep?: SummaryMetric
  callout?: string
  actions?: string[]
}

const DAILY_SUMMARY_FALLBACK_EN: DailySummaryCardContent = {
  readiness: { label: 'Readiness', value: '+11 %', intent: 'Adjust carbs in the next meal' },
  weight: { label: 'Weight', value: '68.4 kg', delta: '-0.2 kg vs avg' },
  sleep: { label: 'Sleep', value: '6 h 10 m', delta: '-45 m vs goal' },
  callout: "System suggests a lighter dinner to keep tomorrow's plan stable.",
}

const DAILY_SUMMARY_FALLBACK_ZH: DailySummaryCardContent = {
  readiness: { label: '状态', value: '+11 %', intent: '下一餐适当调整碳水' },
  weight: { label: '体重', value: '68.4 kg', delta: '较平均值 -0.2 kg' },
  sleep: { label: '睡眠', value: '6 小时 10 分', delta: '较目标 -45 分' },
  callout: '系统建议晚餐适当降低脂肪摄入，以保持明天计划稳定。',
}

export function DailySummaryCard({ content }: { content?: DailySummaryCardContent | null }) {
  const { language } = useLanguage()
  const isChinese = language === 'zh-CN'
  const fallback = isChinese ? DAILY_SUMMARY_FALLBACK_ZH : DAILY_SUMMARY_FALLBACK_EN
  const data = content ?? fallback

  const metrics = [data.readiness, data.weight, data.sleep].filter(Boolean) as SummaryMetric[]
  const callout = data.callout ?? fallback.callout
  const actions = data.actions ?? []

  return (
    <div className="w-full rounded-3xl border border-amber-300/40 bg-gradient-to-br from-amber-400/15 to-orange-900/10 p-5 text-left shadow-lg shadow-amber-500/30">
      <p className="text-xs uppercase tracking-[0.35em] text-amber-200">{isChinese ? '每日摘要' : 'Daily Summary'}</p>
      <div className="mt-4 grid gap-3 sm:grid-cols-3">
        {metrics.map((metric) => (
          <div key={metric.label} className="rounded-2xl border border-white/10 bg-white/5 px-3 py-2">
            <p className="text-[10px] uppercase tracking-[0.35em] text-slate-300">{metric.label}</p>
            <p className="mt-1 text-lg font-semibold text-white">{metric.value}</p>
            {metric.delta && <p className="text-xs text-slate-400">{metric.delta}</p>}
            {metric.intent && <p className="text-xs text-amber-200">{metric.intent}</p>}
          </div>
        ))}
      </div>
      {callout && <p className="mt-4 text-sm text-slate-100">{callout}</p>}
      {actions.length > 0 && (
        <ul className="mt-3 space-y-1 text-xs text-slate-300">
          {actions.map((action) => (
            <li key={action}>- {action}</li>
          ))}
        </ul>
      )}
    </div>
  )
}
