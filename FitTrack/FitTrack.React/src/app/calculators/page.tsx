'use client'

import Link from 'next/link'

import { AppShell } from '@/components/layout/app-shell'
import { useLanguage } from '@/components/providers/language-provider'

export default function CalculatorsPage() {
  const { language } = useLanguage()
  const isChinese = language === 'zh-CN'

  const items = [
    { href: '/calculators/bmi', en: 'BMI', zh: 'BMI' },
    { href: '/calculators/calorie', en: 'Calorie', zh: '热量' },
    { href: '/calculators/body-fat', en: 'Body Fat', zh: '体脂' },
    { href: '/calculators/ideal-weight', en: 'Ideal Weight', zh: '理想体重' },
  ]

  return (
    <AppShell title="Health Calculators" titleKey="calculators">
      <div className="grid gap-4 md:grid-cols-2">
        {items.map((item) => (
          <Link key={item.href} href={item.href} className="rounded-[24px] border border-white/10 bg-white/5 p-5 hover:border-cyan-300/35">
            <p className="text-xl font-semibold text-white">{isChinese ? item.zh : item.en}</p>
          </Link>
        ))}
      </div>
    </AppShell>
  )
}
