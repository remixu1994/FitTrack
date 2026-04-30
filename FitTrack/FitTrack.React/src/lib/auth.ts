import type { AuthenticatedUser } from '@/types/fittrack'

const ACCESS_TOKEN_KEY = 'fittrack.access-token'
const USER_KEY = 'fittrack.user'
export const FITTRACK_USER_CHANGED_EVENT = 'fittrack:user-changed'

export const authStorage = {
  getToken() {
    if (typeof window === 'undefined') return null
    return window.localStorage.getItem(ACCESS_TOKEN_KEY)
  },
  setToken(token: string) {
    if (typeof window === 'undefined') return
    window.localStorage.setItem(ACCESS_TOKEN_KEY, token)
  },
  clearToken() {
    if (typeof window === 'undefined') return
    window.localStorage.removeItem(ACCESS_TOKEN_KEY)
  },
  getUser(): AuthenticatedUser | null {
    if (typeof window === 'undefined') return null
    const value = window.localStorage.getItem(USER_KEY)
    if (!value) return null
    try {
      return JSON.parse(value) as AuthenticatedUser
    } catch {
      return null
    }
  },
  setUser(user: AuthenticatedUser) {
    if (typeof window === 'undefined') return
    window.localStorage.setItem(USER_KEY, JSON.stringify(user))
    window.dispatchEvent(new CustomEvent(FITTRACK_USER_CHANGED_EVENT, { detail: user }))
  },
  clearUser() {
    if (typeof window === 'undefined') return
    window.localStorage.removeItem(USER_KEY)
    window.dispatchEvent(new CustomEvent(FITTRACK_USER_CHANGED_EVENT, { detail: null }))
  },
}
