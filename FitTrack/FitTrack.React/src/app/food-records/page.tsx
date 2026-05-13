'use client'

import { useQuery } from '@tanstack/react-query'

import { AppShell } from '@/components/layout/app-shell'
import { useLanguage } from '@/components/providers/language-provider'
import { apiFetch } from '@/lib/http'
import type { FoodRecord } from '@/types/fittrack'

export default function FoodRecordsPage() {
  const { language, locale } = useLanguage()
  const isChinese = language === 'zh-CN'
  const query = useQuery({
    queryKey: ['food-records'],
    queryFn: () => apiFetch<FoodRecord[]>('/api/food-records'),
  })

  return (
    <AppShell title="Food Records" titleKey="food">
      <div className="rounded-[32px] border border-white/10 bg-slate-950/50 p-6">
        <p className="text-sm text-slate-400">{isChinese ? '来自 .NET API 的每日饮食记录。' : 'Daily food log from the .NET API.'}</p>
        <div className="mt-6 space-y-3">
          {query.data?.map((record) => (
            <div key={record.id} className="rounded-2xl border border-white/10 bg-white/5 p-4">
              <div className="flex items-center justify-between gap-4">
                <div>
                  <p className="font-medium text-white">{record.foodName}</p>
                  <p className="text-sm text-slate-400">{new Date(record.consumptionDate).toLocaleString(locale)}</p>
                </div>
                <p className="text-sm text-cyan-200">{record.calories.toFixed(0)} kcal</p>
              </div>
              <p className="mt-3 text-sm text-slate-300">
                {isChinese ? `蛋白质 ${record.protein} g | 碳水 ${record.carbs} g | 脂肪 ${record.fat} g` : `Protein ${record.protein} g | Carbs ${record.carbs} g | Fat ${record.fat} g`}
              </p>
            </div>
          ))}
          {query.data?.length === 0 ? <p className="text-sm text-slate-400">{isChinese ? '还没有饮食记录。' : 'No food records yet.'}</p> : null}
          {query.isLoading ? <p className="text-sm text-slate-400">{isChinese ? '正在加载饮食记录...' : 'Loading food records...'}</p> : null}
        </div>
      </div>
    </AppShell>
  )
}
