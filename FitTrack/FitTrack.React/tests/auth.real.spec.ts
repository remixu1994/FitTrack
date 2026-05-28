import { expect, test } from '@playwright/test'

import { backendBaseUrl } from '../playwright.e2e-env'

const adminEmail = process.env.PLAYWRIGHT_E2E_EMAIL ?? 'admin@fittrack.local'
const adminPassword = process.env.PLAYWRIGHT_E2E_PASSWORD ?? 'FitTrack123!'

type AuthPayload = {
  success: boolean
  data?: {
    accessToken?: string
    expiresAtUtc?: string
    user?: {
      id?: string
      email?: string
      displayName?: string | null
    }
  }
}

type MePayload = {
  success: boolean
  data?: {
    id?: string
    email?: string
    displayName?: string | null
  }
}

test.describe('real backend authentication', () => {
  test('login succeeds against FitTrack.Copilot without mocks', async ({ request }) => {
    const loginResponse = await request.post(`${backendBaseUrl}/api/auth/login`, {
      data: {
        email: adminEmail,
        password: adminPassword,
      },
    })

    expect(loginResponse.status()).toBe(200)

    const loginPayload = (await loginResponse.json()) as AuthPayload
    expect(loginPayload.success).toBe(true)
    expect(loginPayload.data?.accessToken).toBeTruthy()
    expect(loginPayload.data?.expiresAtUtc).toBeTruthy()
    expect(loginPayload.data?.user?.email).toBe(adminEmail)

    const storageState = await request.storageState()
    expect(storageState.cookies.some((cookie) => cookie.name === 'fittrack_rt')).toBe(true)

    const meResponse = await request.get(`${backendBaseUrl}/api/auth/me`, {
      headers: {
        Authorization: `Bearer ${loginPayload.data?.accessToken}`,
      },
    })

    expect(meResponse.status()).toBe(200)

    const mePayload = (await meResponse.json()) as MePayload
    expect(mePayload.success).toBe(true)
    expect(mePayload.data?.email).toBe(adminEmail)
    expect(mePayload.data?.id).toBeTruthy()

    const refreshResponse = await request.post(`${backendBaseUrl}/api/auth/refresh`)
    expect(refreshResponse.status()).toBe(200)

    const refreshPayload = (await refreshResponse.json()) as AuthPayload
    expect(refreshPayload.success).toBe(true)
    expect(refreshPayload.data?.accessToken).toBeTruthy()
    expect(refreshPayload.data?.user?.email).toBe(adminEmail)
  })
})
