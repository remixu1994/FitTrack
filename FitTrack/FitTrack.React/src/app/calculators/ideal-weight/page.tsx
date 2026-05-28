'use client'

import { useMemo, useState } from 'react'

import { AppShell } from '@/components/layout/app-shell'

export default function IdealWeightPage() {
  const [heightCm, setHeightCm] = useState('170')
  const [sex, setSex] = useState<'male' | 'female'>('male')

  const idealWeight = useMemo(() => {
    const cm = Number(heightCm)
    if (!Number.isFinite(cm) || cm <= 0) return null
    const inches = cm / 2.54
    const over5 = Math.max(inches - 60, 0)
    const lb = sex === 'male' ? 106 + 6 * over5 : 100 + 5 * over5
    return (lb * 0.45359237).toFixed(1)
  }, [heightCm, sex])

  return (
    <AppShell title="Ideal Weight Calculator" titleKey="calculators">
      <div className="grid gap-4 max-w-xl">
        <select value={sex} onChange={(e) => setSex(e.target.value as 'male' | 'female')} className="rounded-2xl border border-white/10 bg-white/5 px-4 py-3">
          <option value="male">Male</option>
          <option value="female">Female</option>
        </select>
        <input value={heightCm} onChange={(e) => setHeightCm(e.target.value)} className="rounded-2xl border border-white/10 bg-white/5 px-4 py-3" placeholder="Height (cm)" />
        <div className="rounded-2xl border border-cyan-300/30 bg-cyan-300/10 p-4 text-xl">Ideal Weight: {idealWeight ?? '--'} kg</div>
      </div>
    </AppShell>
  )
}
