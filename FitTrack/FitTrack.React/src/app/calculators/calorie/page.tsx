'use client'

import { useMemo, useState } from 'react'

import { AppShell } from '@/components/layout/app-shell'

export default function CaloriePage() {
  const [age, setAge] = useState('30')
  const [heightCm, setHeightCm] = useState('170')
  const [weightKg, setWeightKg] = useState('70')
  const [sex, setSex] = useState<'male' | 'female'>('male')

  const bmr = useMemo(() => {
    const a = Number(age)
    const h = Number(heightCm)
    const w = Number(weightKg)
    if (![a, h, w].every(Number.isFinite)) return null
    const offset = sex === 'male' ? 5 : -161
    return Math.round(10 * w + 6.25 * h - 5 * a + offset)
  }, [age, heightCm, weightKg, sex])

  return (
    <AppShell title="Calorie Calculator" titleKey="calculators">
      <div className="grid gap-4 max-w-xl">
        <select value={sex} onChange={(e) => setSex(e.target.value as 'male' | 'female')} className="rounded-2xl border border-white/10 bg-white/5 px-4 py-3">
          <option value="male">Male</option>
          <option value="female">Female</option>
        </select>
        <input value={age} onChange={(e) => setAge(e.target.value)} className="rounded-2xl border border-white/10 bg-white/5 px-4 py-3" placeholder="Age" />
        <input value={heightCm} onChange={(e) => setHeightCm(e.target.value)} className="rounded-2xl border border-white/10 bg-white/5 px-4 py-3" placeholder="Height (cm)" />
        <input value={weightKg} onChange={(e) => setWeightKg(e.target.value)} className="rounded-2xl border border-white/10 bg-white/5 px-4 py-3" placeholder="Weight (kg)" />
        <div className="rounded-2xl border border-cyan-300/30 bg-cyan-300/10 p-4 text-xl">BMR: {bmr ?? '--'} kcal</div>
      </div>
    </AppShell>
  )
}
