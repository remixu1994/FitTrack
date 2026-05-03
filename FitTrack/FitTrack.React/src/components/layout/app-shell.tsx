'use client'

import Link from 'next/link'
import { usePathname, useRouter } from 'next/navigation'
import { useEffect, useState } from 'react'

import { authStorage, FITTRACK_USER_CHANGED_EVENT } from '@/lib/auth'
import { getCurrentUser, logout, refreshSession } from '@/lib/http'
import type { AuthenticatedUser } from '@/types/fittrack'

const navItems = [
  { href: '/chat', label: 'Coach' },
  { href: '/food-records', label: 'Food' },
  { href: '/workouts', label: 'Workouts' },
  { href: '/progress', label: 'Progress' },
  { href: '/settings/profile', label: 'Profile' },
]

type AppShellProps = {
  title: string
  children: React.ReactNode
  hideHeader?: boolean
  immersive?: boolean
}

export function AppShell({ title, children, hideHeader = false, immersive = false }: AppShellProps) {
  const pathname = usePathname()
  const router = useRouter()
  const [user, setUser] = useState<AuthenticatedUser | null>(null)
  const [ready, setReady] = useState(false)

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

  if (!ready) {
    return (
      <div
        suppressHydrationWarning
        className="flex min-h-screen items-center justify-center bg-[radial-gradient(circle_at_top_left,_rgba(37,99,235,0.24),_transparent_30%),radial-gradient(circle_at_top_right,_rgba(16,185,129,0.18),_transparent_25%),linear-gradient(180deg,_#07111f_0%,_#050b14_100%)] text-sm uppercase tracking-[0.35em] text-cyan-300"
      >
        Loading workspace
      </div>
    )
  }

  return (
    <div
      suppressHydrationWarning
      className={`${immersive ? 'min-h-screen xl:h-screen xl:overflow-hidden' : 'min-h-screen'} bg-[radial-gradient(circle_at_top_left,_rgba(37,99,235,0.24),_transparent_30%),radial-gradient(circle_at_top_right,_rgba(16,185,129,0.18),_transparent_25%),linear-gradient(180deg,_#07111f_0%,_#050b14_100%)] text-slate-50`}
    >
      <div className={`mx-auto grid min-h-screen max-w-[1600px] grid-cols-1 lg:grid-cols-[250px_minmax(0,1fr)] ${immersive ? 'xl:h-full' : ''}`}>
        <aside className={`border-b border-white/10 bg-slate-950/60 p-6 backdrop-blur lg:border-b-0 lg:border-r ${immersive ? 'xl:flex xl:min-h-0 xl:flex-col xl:overflow-y-auto' : ''}`}>
          <p className="text-xs uppercase tracking-[0.45em] text-cyan-300">FitTrack</p>
          <h1 className="mt-3 text-2xl font-semibold text-white">Performance Workspace</h1>
          <p className="mt-2 text-sm text-slate-400">Next.js frontend for coaching, food, training, and weekly check-ins.</p>
          <nav className="mt-8 space-y-2">
            {navItems.map((item) => {
              const active = pathname === item.href
              return (
                <Link
                  key={item.href}
                  href={item.href}
                  className={`block rounded-2xl px-4 py-3 text-sm transition ${
                    active ? 'bg-cyan-400/15 text-cyan-100' : 'text-slate-300 hover:bg-white/5 hover:text-white'
                  }`}
                >
                  {item.label}
                </Link>
              )
            })}
          </nav>
          <div className={`mt-8 rounded-3xl border border-white/10 bg-white/5 p-4 text-sm ${immersive ? 'xl:mt-auto' : ''}`}>
            <p className="text-slate-400">Signed in as</p>
            <p className="mt-1 font-medium text-white">{user?.displayName || user?.email || 'Unknown user'}</p>
            <button
              className="mt-4 rounded-full border border-white/10 px-4 py-2 text-xs uppercase tracking-[0.25em] text-slate-200 hover:bg-white/10"
              onClick={async () => {
                await logout()
                router.replace('/login')
              }}
            >
              Sign out
            </button>
          </div>
        </aside>
        <main className={`min-w-0 p-6 lg:p-10 ${immersive ? 'xl:flex xl:min-h-0 xl:flex-col xl:overflow-hidden' : ''}`}>
          {!hideHeader ? (
            <header className="mb-8 shrink-0">
              <p className="text-xs uppercase tracking-[0.35em] text-cyan-300">Workspace</p>
              <h2 className="mt-2 text-3xl font-semibold text-white">{title}</h2>
            </header>
          ) : null}
          <div className={immersive ? 'xl:min-h-0 xl:flex-1 xl:overflow-hidden' : ''}>{children}</div>
        </main>
      </div>
    </div>
  )
}
