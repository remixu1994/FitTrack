'use client'

import { useMemo, useState } from 'react'

import { AppShell } from '@/components/layout/app-shell'

export default function BodyFatPage() {
  const [heightCm, setHeightCm] = useState('175')
  const [neckCm, setNeckCm] = useState('38')
  const [waistCm, setWaistCm] = useState('85')

  const bodyFat = useMemo(() => {
    const h = Number(heightCm)
    const n = Number(neckCm)
    const w = Number(waistCm)
    if (![h, n, w].every(Number.isFinite)) return null
    if (w <= n || h <= 0) return null
    const value = 495 / (1.0324 - 0.19077 * Math.log10(w - n) + 0.15456 * Math.log10(h)) - 450
    return value.toFixed(1)
  }, [heightCm, neckCm, waistCm])

  return (
    <AppShell title="Body Fat Calculator" titleKey="calculators">
      <div className="grid gap-4 max-w-xl">
        <input value={heightCm} onChange={(e) => setHeightCm(e.target.value)} className="rounded-2xl border border-white/10 bg-white/5 px-4 py-3" placeholder="Height (cm)" />
        <input value={neckCm} onChange={(e) => setNeckCm(e.target.value)} className="rounded-2xl border border-white/10 bg-white/5 px-4 py-3" placeholder="Neck (cm)" />
        <input value={waistCm} onChange={(e) => setWaistCm(e.target.value)} className="rounded-2xl border border-white/10 bg-white/5 px-4 py-3" placeholder="Waist (cm)" />
        <div className="rounded-2xl border border-cyan-300/30 bg-cyan-300/10 p-4 text-xl">Body Fat: {bodyFat ?? '--'}%</div>
      </div>
    </AppShell>
  )
}
