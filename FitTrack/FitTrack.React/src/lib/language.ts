export type AppLanguage = 'en' | 'zh-CN'

export const APP_LANGUAGE_STORAGE_KEY = 'fittrack.language'
export const DEFAULT_APP_LANGUAGE: AppLanguage = 'en'

export function normalizeAppLanguage(value?: string | null): AppLanguage {
  if (!value) return DEFAULT_APP_LANGUAGE
  return value.toLowerCase().startsWith('zh') ? 'zh-CN' : 'en'
}

export function getBrowserPreferredLanguage(): AppLanguage {
  if (typeof navigator === 'undefined') {
    return DEFAULT_APP_LANGUAGE
  }

  return normalizeAppLanguage(navigator.language)
}

export function getPersistedAppLanguage(): AppLanguage {
  if (typeof window === 'undefined') {
    return DEFAULT_APP_LANGUAGE
  }

  try {
    return normalizeAppLanguage(window.localStorage.getItem(APP_LANGUAGE_STORAGE_KEY))
  } catch {
    return DEFAULT_APP_LANGUAGE
  }
}

export function persistAppLanguage(language: AppLanguage) {
  if (typeof window === 'undefined') {
    return
  }

  try {
    window.localStorage.setItem(APP_LANGUAGE_STORAGE_KEY, language)
  } catch {
    // Ignore storage failures and keep the in-memory language only.
  }
}

export function getLocaleForLanguage(language: AppLanguage) {
  return language === 'zh-CN' ? 'zh-CN' : 'en-US'
}

