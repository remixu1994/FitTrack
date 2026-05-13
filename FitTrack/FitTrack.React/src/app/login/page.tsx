'use client'

import { useState } from 'react'

import { useLanguage } from '@/components/providers/language-provider'
import { login } from '@/lib/http'

export default function LoginPage() {
  const { language, setLanguage } = useLanguage()
  const isChinese = language === 'zh-CN'
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  return (
    <div className="flex min-h-screen items-center justify-center bg-[radial-gradient(circle_at_top,_rgba(14,165,233,0.22),_transparent_28%),linear-gradient(180deg,_#07111f_0%,_#050b14_100%)] px-4 text-white">
      <div className="w-full max-w-md rounded-[32px] border border-white/10 bg-slate-950/60 p-8 shadow-2xl shadow-cyan-950/40 backdrop-blur">
        <div className="flex items-start justify-between gap-4">
          <div>
            <p className="text-xs uppercase tracking-[0.45em] text-cyan-300">FitTrack</p>
            <h1 className="mt-3 text-3xl font-semibold">{isChinese ? '登录教练工作台' : 'Sign in to the coach'}</h1>
            <p className="mt-2 text-sm text-slate-400">
              {isChinese
                ? '使用你的 ASP.NET Identity 账号登录。Access Token 保存在浏览器端，刷新依赖后端安全 Cookie。'
                : 'Use your ASP.NET Identity account. Access tokens stay client-side; refresh uses the secure cookie from the backend.'}
            </p>
          </div>
          <div className="flex items-center gap-2 rounded-full border border-white/10 bg-white/5 p-1">
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
        </div>
        <form
          className="mt-8 space-y-4"
          onSubmit={async (event) => {
            event.preventDefault()
            setLoading(true)
            setError(null)
            try {
              await login(email, password)
              window.location.replace('/chat')
            } catch (loginError) {
              setError(loginError instanceof Error ? loginError.message : isChinese ? '登录失败' : 'Login failed')
            } finally {
              setLoading(false)
            }
          }}
        >
          <label className="block">
            <span className="mb-2 block text-xs uppercase tracking-[0.3em] text-slate-400">{isChinese ? '邮箱' : 'Email'}</span>
            <input value={email} onChange={(event) => setEmail(event.target.value)} className="w-full rounded-2xl border border-white/10 bg-white/5 px-4 py-3 text-sm outline-none focus:border-cyan-300" />
          </label>
          <label className="block">
            <span className="mb-2 block text-xs uppercase tracking-[0.3em] text-slate-400">{isChinese ? '密码' : 'Password'}</span>
            <input type="password" value={password} onChange={(event) => setPassword(event.target.value)} className="w-full rounded-2xl border border-white/10 bg-white/5 px-4 py-3 text-sm outline-none focus:border-cyan-300" />
          </label>
          {error ? <p className="text-sm text-rose-300">{error}</p> : null}
          <button disabled={loading} className="w-full rounded-full bg-cyan-300 px-5 py-3 text-sm font-semibold uppercase tracking-[0.3em] text-slate-950 disabled:opacity-60">
            {loading ? (isChinese ? '登录中...' : 'Signing in...') : isChinese ? '登录' : 'Login'}
          </button>
        </form>
      </div>
    </div>
  )
}
