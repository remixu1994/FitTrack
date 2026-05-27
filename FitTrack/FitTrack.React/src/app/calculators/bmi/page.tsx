'use client'

import { useMemo, useState } from 'react'

import { AppShell } from '@/components/layout/app-shell'

export default function BmiPage() {
  const [heightCm, setHeightCm] = useState('170')
  const [weightKg, setWeightKg] = useState('70')

  const bmi = useMemo(() => {
    const h = Number(heightCm) / 100
    const w = Number(weightKg)
    if (!Number.isFinite(h) || !Number.isFinite(w) || h <= 0) return null
    return (w / (h * h)).toFixed(1)
  }, [heightCm, weightKg])

  return (
    <AppShell title="BMI Calculator" titleKey="calculators">
      <div className="grid gap-4 max-w-xl">
        <input value={heightCm} onChange={(e) => setHeightCm(e.target.value)} className="rounded-2xl border border-white/10 bg-white/5 px-4 py-3" placeholder="Height (cm)" />
        <input value={weightKg} onChange={(e) => setWeightKg(e.target.value)} className="rounded-2xl border border-white/10 bg-white/5 px-4 py-3" placeholder="Weight (kg)" />
        <div className="rounded-2xl border border-cyan-300/30 bg-cyan-300/10 p-4 text-xl">BMI: {bmi ?? '--'}</div>
      </div>
    </AppShell>
  )
}
