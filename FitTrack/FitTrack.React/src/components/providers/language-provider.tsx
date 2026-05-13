'use client'

import { createContext, useContext, useEffect, useMemo, useState } from 'react'

import {
  DEFAULT_APP_LANGUAGE,
  getBrowserPreferredLanguage,
  getLocaleForLanguage,
  getPersistedAppLanguage,
  persistAppLanguage,
  type AppLanguage,
} from '@/lib/language'

type LanguageContextValue = {
  language: AppLanguage
  locale: string
  setLanguage: (language: AppLanguage) => void
}

const LanguageContext = createContext<LanguageContextValue | null>(null)

export function LanguageProvider({ children }: { children: React.ReactNode }) {
  const [language, setLanguageState] = useState<AppLanguage>(DEFAULT_APP_LANGUAGE)

  useEffect(() => {
    const initialLanguage = getPersistedAppLanguage() ?? getBrowserPreferredLanguage()
    setLanguageState(initialLanguage)
  }, [])

  useEffect(() => {
    document.documentElement.lang = language
    persistAppLanguage(language)
  }, [language])

  const value = useMemo<LanguageContextValue>(
    () => ({
      language,
      locale: getLocaleForLanguage(language),
      setLanguage: setLanguageState,
    }),
    [language],
  )

  return <LanguageContext.Provider value={value}>{children}</LanguageContext.Provider>
}

export function useLanguage() {
  const context = useContext(LanguageContext)
  if (!context) {
    throw new Error('useLanguage must be used within LanguageProvider.')
  }

  return context
}
