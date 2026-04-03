import { authStorage } from '@/lib/auth'
import { apiBaseUrl } from '@/lib/config'
import type { ApiResponse, AuthResponse, AuthenticatedUser } from '@/types/fittrack'

type FetchOptions = RequestInit & {
  authenticated?: boolean
  retryOnAuthFailure?: boolean
}

function buildHeaders(headers: HeadersInit | undefined, body: RequestInit['body']) {
  const result = new Headers(headers)
  if (body && !(body instanceof FormData) && !result.has('Content-Type')) {
    result.set('Content-Type', 'application/json')
  }

  return result
}

async function performRequest(path: string, options: FetchOptions = {}): Promise<Response> {
  const { authenticated = true, retryOnAuthFailure = true, headers, ...init } = options
  const token = authenticated ? authStorage.getToken() : null
  const requestHeaders = buildHeaders(headers, init.body)
  if (token) {
    requestHeaders.set('Authorization', `Bearer ${token}`)
  }

  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...init,
    headers: requestHeaders,
    credentials: 'include',
  })

  if (response.status === 401 && authenticated && retryOnAuthFailure) {
    const refreshed = await refreshSession()
    if (refreshed) {
      return performRequest(path, { ...options, retryOnAuthFailure: false })
    }
  }

  return response
}

export async function apiFetchRaw(path: string, options: FetchOptions = {}): Promise<Response> {
  const response = await performRequest(path, options)

  if (!response.ok) {
    try {
      const payload = (await response.json()) as ApiResponse<never>
      throw new Error(payload.error?.message ?? 'Request failed')
    } catch (error) {
      if (error instanceof Error) {
        throw error
      }
      throw new Error('Request failed')
    }
  }

  return response
}

export async function apiFetch<T>(path: string, options: FetchOptions = {}): Promise<T> {
  const response = await apiFetchRaw(path, options)
  const payload = (await response.json()) as ApiResponse<T>
  if (!response.ok || !payload.success || payload.data === undefined) {
    throw new Error(payload.error?.message ?? 'Request failed')
  }

  return payload.data
}

export async function fetchBlobUrl(path: string) {
  const response = await apiFetchRaw(path)
  const blob = await response.blob()
  return URL.createObjectURL(blob)
}

export async function login(email: string, password: string) {
  const result = await apiFetch<AuthResponse>('/api/auth/login', {
    method: 'POST',
    authenticated: false,
    body: JSON.stringify({ email, password }),
  })

  authStorage.setToken(result.accessToken)
  authStorage.setUser(result.user)
  return result
}

export async function getCurrentUser() {
  const user = await apiFetch<AuthenticatedUser>('/api/auth/me')
  authStorage.setUser(user)
  return user
}

export async function refreshSession() {
  try {
    const result = await apiFetch<AuthResponse>('/api/auth/refresh', {
      method: 'POST',
      authenticated: false,
      retryOnAuthFailure: false,
    })
    authStorage.setToken(result.accessToken)
    authStorage.setUser(result.user)
    return true
  } catch {
    authStorage.clearToken()
    authStorage.clearUser()
    return false
  }
}

export async function logout() {
  try {
    await apiFetch('/api/auth/logout', { method: 'POST' })
  } finally {
    authStorage.clearToken()
    authStorage.clearUser()
  }
}
