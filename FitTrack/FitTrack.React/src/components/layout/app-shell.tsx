'use client'

import Link from 'next/link'
import { usePathname, useRouter } from 'next/navigation'
import { useEffect, useState } from 'react'

import { useLanguage } from '@/components/providers/language-provider'
import { authStorage, FITTRACK_USER_CHANGED_EVENT } from '@/lib/auth'
import { getCurrentUser, logout, refreshSession } from '@/lib/http'
import type { AuthenticatedUser } from '@/types/fittrack'

type NavItem = {
  href: string
  label: { en: string; zh: string }
  children?: ReadonlyArray<{ href: string; label: { en: string; zh: string } }>
}

const baseNavItems: ReadonlyArray<NavItem> = [
  { href: '/today', label: { en: 'Today', zh: '今日' } },
  { href: '/chat', label: { en: 'Planning', zh: '计划中心' } },
  {
    href: '/calculators',
    label: { en: 'Calculators', zh: '计算器' },
    children: [
      { href: '/calculators/bmi', label: { en: 'BMI', zh: 'BMI' } },
      { href: '/calculators/calorie', label: { en: 'Calorie', zh: '热量' } },
      { href: '/calculators/body-fat', label: { en: 'Body Fat', zh: '体脂' } },
      { href: '/calculators/ideal-weight', label: { en: 'Ideal Weight', zh: '理想体重' } },
    ],
  },
  { href: '/food-records', label: { en: 'Food', zh: '饮食' } },
  { href: '/workouts', label: { en: 'Workouts', zh: '训练' } },
  { href: '/progress', label: { en: 'Progress', zh: '进度' } },
  { href: '/settings/profile', label: { en: 'Profile', zh: '档案' } },
]

type AppShellProps = {
  title: string
  titleKey?: 'today' | 'chat' | 'calculators' | 'food' | 'workouts' | 'progress' | 'profile' | 'models' | 'usage'
  children: React.ReactNode
  hideHeader?: boolean
  immersive?: boolean
}

export function AppShell({ title, titleKey, children, hideHeader = false, immersive = false }: AppShellProps) {
  const pathname = usePathname()
  const router = useRouter()
  const { language, setLanguage } = useLanguage()
  const isChinese = language === 'zh-CN'
  const [user, setUser] = useState<AuthenticatedUser | null>(null)
  const [ready, setReady] = useState(false)

  const navItems = user?.roles.includes('Admin')
    ? [...baseNavItems, { href: '/settings/models', label: { en: 'Models', zh: '模型' } }, { href: '/settings/model-usage', label: { en: 'Usage', zh: '用量' } }]
    : baseNavItems

  const translatedTitle = titleKey
    ? {
        today: isChinese ? '今日' : 'Today',
        chat: isChinese ? '计划中心' : 'Planning',
        calculators: isChinese ? '健康计算器' : 'Health Calculators',
        food: isChinese ? '饮食记录' : 'Food Records',
        workouts: isChinese ? '训练记录' : 'Workouts',
        progress: isChinese ? '进度' : 'Progress',
        profile: isChinese ? '个人档案' : 'Profile',
        models: isChinese ? '模型' : 'Models',
        usage: isChinese ? '用量' : 'Usage',
      }[titleKey]
    : title

  useEffect(() => {
    setUser(authStorage.getUser())
  }, [])

  useEffect(() => {
    if (pathname === '/login') {
      setReady(true)
      return
    }

    let cancelled = false

    async function ensureSession() {
      const token = authStorage.getToken()
      if (!token) {
        const refreshed = await refreshSession()
        if (!refreshed) {
          if (!cancelled) router.replace('/login')
          return
        }
      }

      try {
        const currentUser = await getCurrentUser()
        if (!cancelled) {
          setUser(currentUser)
          setReady(true)
        }
      } catch {
        if (!cancelled) {
          authStorage.clearToken()
          authStorage.clearUser()
          router.replace('/login')
        }
      }
    }

    void ensureSession()

    return () => {
      cancelled = true
    }
  }, [pathname, router])

  useEffect(() => {
    const handleUserChanged = (event: Event) => {
      const customEvent = event as CustomEvent<AuthenticatedUser | null>
      setUser(customEvent.detail ?? null)
    }

    window.addEventListener(FITTRACK_USER_CHANGED_EVENT, handleUserChanged as EventListener)
    return () => {
      window.removeEventListener(FITTRACK_USER_CHANGED_EVENT, handleUserChanged as EventListener)
    }
  }, [])

  return (
    <div className={`${immersive ? 'min-h-screen xl:h-screen xl:overflow-hidden' : 'min-h-screen'} bg-[radial-gradient(circle_at_top_left,_rgba(37,99,235,0.24),_transparent_30%),radial-gradient(circle_at_top_right,_rgba(16,185,129,0.18),_transparent_25%),linear-gradient(180deg,_#07111f_0%,_#050b14_100%)] text-slate-50`}>
      {!ready ? (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-[radial-gradient(circle_at_top_left,_rgba(37,99,235,0.24),_transparent_30%),radial-gradient(circle_at_top_right,_rgba(16,185,129,0.18),_transparent_25%),linear-gradient(180deg,_rgba(7,17,31,0.96)_0%,_rgba(5,11,20,0.98)_100%)] text-sm uppercase tracking-[0.35em] text-cyan-300">
          {isChinese ? '正在加载' : 'Loading'}
        </div>
      ) : null}

      <div
        aria-hidden={!ready}
        className={`mx-auto grid min-h-screen max-w-[1600px] grid-cols-1 transition-opacity duration-200 lg:grid-cols-[250px_minmax(0,1fr)] ${immersive ? 'xl:h-full' : ''} ${ready ? 'opacity-100' : 'pointer-events-none opacity-0'}`}
      >
        <aside className={`border-b border-white/10 bg-slate-950/60 p-6 backdrop-blur lg:border-b-0 lg:border-r ${immersive ? 'xl:flex xl:min-h-0 xl:flex-col xl:overflow-y-auto' : ''}`}>
          <p className="text-xs uppercase tracking-[0.45em] text-cyan-300">FitTrack</p>
          <h1 className="mt-3 text-2xl font-semibold text-white">{isChinese ? '训练与营养' : 'Training & Nutrition'}</h1>
          <p className="mt-2 text-sm text-slate-400">{isChinese ? '记录饮食、训练与进度的产品工作台。' : 'Product workspace for food, workouts, and progress.'}</p>

          <div className="mt-5 flex items-center gap-2 rounded-full border border-white/10 bg-white/5 p-1">
            <button
              type="button"
              className={`rounded-full px-3 py-1.5 text-xs uppercase tracking-[0.25em] transition ${language === 'en' ? 'bg-cyan-300 text-slate-950' : 'text-slate-300 hover:text-white'}`}
              onClick={() => setLanguage('en')}
            >
              EN
            </button>
            <button
              type="button"
              className={`rounded-full px-3 py-1.5 text-xs transition ${language === 'zh-CN' ? 'bg-cyan-300 text-slate-950' : 'text-slate-300 hover:text-white'}`}
              onClick={() => setLanguage('zh-CN')}
            >
              中文
            </button>
          </div>

          <nav className="mt-8 space-y-2">
            {navItems.map((item) => {
              const active = pathname === item.href || pathname.startsWith(`${item.href}/`)
              return (
                <div key={item.href}>
                  <Link
                    href={item.href}
                    className={`block rounded-2xl px-4 py-3 text-sm transition ${active ? 'bg-cyan-400/15 text-cyan-100' : 'text-slate-300 hover:bg-white/5 hover:text-white'}`}
                  >
                    {isChinese ? item.label.zh : item.label.en}
                  </Link>
                  {item.children && active ? (
                    <div className="mt-2 space-y-1 pl-3">
                      {item.children.map((child) => {
                        const childActive = pathname === child.href
                        return (
                          <Link
                            key={child.href}
                            href={child.href}
                            className={`block rounded-xl px-3 py-2 text-xs uppercase tracking-[0.18em] transition ${childActive ? 'bg-cyan-300/15 text-cyan-100' : 'text-slate-400 hover:bg-white/5 hover:text-white'}`}
                          >
                            {isChinese ? child.label.zh : child.label.en}
                          </Link>
                        )
                      })}
                    </div>
                  ) : null}
                </div>
              )
            })}
          </nav>

          <div className={`mt-8 rounded-3xl border border-white/10 bg-white/5 p-4 text-sm ${immersive ? 'xl:mt-auto' : ''}`}>
            <p className="text-slate-400">{isChinese ? '当前用户' : 'Signed in as'}</p>
            <p className="mt-1 font-medium text-white">{user?.displayName || user?.email || 'Unknown user'}</p>
            <button
              className="mt-4 rounded-full border border-white/10 px-4 py-2 text-xs uppercase tracking-[0.25em] text-slate-200 hover:bg-white/10"
              onClick={async () => {
                await logout()
                router.replace('/login')
              }}
            >
              {isChinese ? '退出登录' : 'Sign out'}
            </button>
          </div>
        </aside>

        <main className={`min-w-0 p-6 lg:p-10 ${immersive ? 'xl:flex xl:min-h-0 xl:flex-col xl:overflow-hidden' : ''}`}>
          {!hideHeader ? (
            <header className="mb-8 shrink-0">
              <p className="text-xs uppercase tracking-[0.35em] text-cyan-300">{isChinese ? '工作区' : 'Workspace'}</p>
              <h2 className="mt-2 text-3xl font-semibold text-white">{translatedTitle}</h2>
            </header>
          ) : null}
          <div className={immersive ? 'xl:min-h-0 xl:flex-1 xl:overflow-hidden' : ''}>{children}</div>
        </main>
      </div>
    </div>
  )
}
