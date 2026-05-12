import { expect, type Page, type Route } from '@playwright/test'

type AuthenticatedUser = {
  id: string
  email: string
  displayName: string
  roles: string[]
}

const defaultUser: AuthenticatedUser = {
  id: 'user-1',
  email: 'demo@fittrack.local',
  displayName: 'Demo Athlete',
  roles: [],
}

export const adminUser: AuthenticatedUser = {
  id: 'admin-1',
  email: 'admin@fittrack.local',
  displayName: 'Admin Coach',
  roles: ['Admin'],
}

const frontendOrigin = 'http://localhost:3000'
const corsHeaders = {
  'access-control-allow-origin': frontendOrigin,
  'access-control-allow-credentials': 'true',
  'access-control-allow-headers': 'Content-Type, Authorization',
  'access-control-allow-methods': 'GET, POST, PUT, OPTIONS',
}

function ok<T>(data: T) {
  return {
    success: true,
    data,
  }
}

async function fulfillJson(route: Route, body: unknown, status = 200) {
  if (route.request().method() === 'OPTIONS') {
    await route.fulfill({
      status: 204,
      headers: corsHeaders,
    })
    return
  }

  await route.fulfill({
    status,
    headers: {
      ...corsHeaders,
      'content-type': 'application/json',
    },
    body: JSON.stringify(body),
  })
}

export function createClientErrorCollector(page: Page) {
  const errors: string[] = []

  page.on('console', (message) => {
    if (message.type() === 'error') {
      errors.push(`console: ${message.text()}`)
    }
  })

  page.on('pageerror', (error) => {
    errors.push(`pageerror: ${error.message}`)
  })

  return errors
}

export async function expectNoHydrationErrors(errors: string[]) {
  await expect(errors, errors.join('\n')).not.toContainEqual(expect.stringContaining('Hydration failed'))
}

export async function seedAuthenticatedSession(page: Page, user: AuthenticatedUser = defaultUser) {
  await page.addInitScript((payload) => {
    window.localStorage.setItem('fittrack.access-token', 'playwright-token')
    window.localStorage.setItem('fittrack.user', JSON.stringify(payload))
  }, user)
}

export async function mockCoreSession(page: Page, user: AuthenticatedUser = defaultUser) {
  await page.route('**/api/auth/me', async (route) => {
    await fulfillJson(route, ok(user))
  })

  await page.route('**/api/auth/refresh', async (route) => {
    await fulfillJson(route, {
        success: false,
        error: {
          code: 'REFRESH_TOKEN_MISSING',
          message: 'Refresh token cookie is missing.',
        },
      }, 401)
  })
}

export async function mockLogin(page: Page, user: AuthenticatedUser = defaultUser) {
  await page.route('**/api/auth/login', async (route) => {
    await fulfillJson(route, ok({
        accessToken: 'playwright-token',
        expiresAtUtc: '2035-01-01T00:00:00.000Z',
        user,
      }))
  })
}

export async function mockChatWorkspace(page: Page) {
  const thread = {
    id: 'thread-1',
    title: 'Weekly check-in',
    createdAt: '2026-05-01T09:00:00.000Z',
    updatedAt: '2026-05-01T09:00:00.000Z',
    archivedAt: null,
  }

  await page.route('**/api/chat/threads', async (route) => {
    if (route.request().method() === 'GET') {
      await fulfillJson(route, ok([thread]))
      return
    }

    await fulfillJson(route, ok(thread))
  })

  await page.route('**/api/chat/threads/thread-1', async (route) => {
    await fulfillJson(route, ok({
        ...thread,
        messages: [
          {
            id: 'message-1',
            threadId: 'thread-1',
            role: 'assistant',
            kind: 'text',
            contentText: 'Ready for your next training block.',
            contentJson: null,
            attachments: [],
            turnIndex: 1,
            createdAt: '2026-05-01T09:00:00.000Z',
          },
        ],
        snapshots: [],
      }))
  })
}

export async function mockFoodRecords(page: Page) {
  await page.route('**/api/food-records', async (route) => {
    await fulfillJson(route, ok([
        {
          id: 1,
          foodName: 'Chicken Rice Bowl',
          calories: 620,
          protein: 42,
          carbs: 55,
          fat: 18,
          servingSize: 1,
          servingUnit: 'bowl',
          consumptionDate: '2026-05-01T12:00:00.000Z',
          mealType: 'Lunch',
        },
      ]))
  })
}

export async function mockWorkoutPages(page: Page) {
  await page.route('**/api/workout-plans', async (route) => {
    await fulfillJson(route, ok([
        {
          id: 11,
          planName: 'Strength Base',
          description: 'Four-day split for rebuilding consistency.',
          fitnessLevel: 'Intermediate',
          createdAt: '2026-05-01T09:00:00.000Z',
          workoutDays: [
            {
              dayOfWeek: 'Monday',
              exercises: [],
            },
            {
              dayOfWeek: 'Thursday',
              exercises: [],
            },
          ],
        },
      ]))
  })

  await page.route('**/api/workout-sessions', async (route) => {
    await fulfillJson(route, ok([
        {
          id: 21,
          startTime: '2026-05-01T07:30:00.000Z',
          endTime: '2026-05-01T08:20:00.000Z',
          duration: 50,
          caloriesBurned: 410,
          notes: 'Kept tempo steady throughout the main sets.',
        },
      ]))
  })
}

export async function mockProgressPage(page: Page) {
  await page.route('**/api/progress/summary', async (route) => {
    await fulfillJson(route, ok({
        userId: 'user-1',
        currentWeightKg: 72.4,
        bodyFatPercent: 16.2,
        foodRecordCountToday: 3,
        workoutCountThisWeek: 4,
        caloriesToday: 1840,
        proteinToday: 146,
        carbsToday: 175,
        fatToday: 58,
        headline: 'You are on pace for a strong recovery week.',
        recommendations: ['Keep protein above 140 g', 'Schedule one lighter cardio day'],
      }))
  })
}

export async function mockProfilePage(page: Page, user: AuthenticatedUser = defaultUser) {
  const profile = {
    id: 'profile-1',
    userId: user.id,
    displayName: user.displayName,
    sex: 'Male',
    age: 30,
    heightCm: 178,
    weightKg: 72.4,
    bodyFatPercent: 16.2,
    activityLevel: 'Moderate',
    goal: 'Recomposition',
    preferences: 'Prefer concise plans.',
    preferredModelConnectorId: 'connector-default-azure-openai',
    createdAt: '2026-05-01T09:00:00.000Z',
    updatedAt: '2026-05-01T09:00:00.000Z',
  }

  await page.route('**/api/profile', async (route) => {
    if (route.request().method() === 'PUT') {
      const payload = route.request().postDataJSON() as Record<string, unknown>
      await fulfillJson(route, ok({
          ...profile,
          ...payload,
        }))
      return
    }

    await fulfillJson(route, ok(profile))
  })
}

export async function mockModelConnectorEndpoints(page: Page) {
  const connectors = [
    {
      id: 'connector-default-azure-openai',
      displayName: 'Azure OpenAI',
      providerPreset: 'azure-openai',
      protocol: 'AzureOpenAI',
      modelId: 'gpt-4.1',
      isDefault: true,
    },
    {
      id: 'connector-default-openai-codex',
      displayName: 'OpenAI Codex',
      providerPreset: 'openai-codex',
      protocol: 'OpenAICompatible',
      modelId: 'gpt-5-codex',
      isDefault: false,
    },
  ]

  await page.route('**/api/model-connectors', async (route) => {
    await fulfillJson(route, ok(connectors))
  })
}

export async function mockAdminModelConnectorEndpoints(page: Page) {
  const tenants = [
    {
      id: 'tenant-default',
      name: 'Default Tenant',
      slug: 'default',
      isSystemDefault: true,
      userCount: 2,
      connectorCount: 2,
    },
  ]

  const presets = [
    {
      key: 'azure-openai',
      displayName: 'Azure OpenAI',
      protocol: 'AzureOpenAI',
      baseUrl: 'https://example.openai.azure.com/',
      modelId: 'gpt-4.1',
    },
    {
      key: 'openai-codex',
      displayName: 'OpenAI/Codex',
      protocol: 'OpenAICompatible',
      baseUrl: 'https://api.openai.com/v1/',
      modelId: 'gpt-5-codex',
    },
  ]

  let connectors = [
    {
      id: 'connector-default-azure-openai',
      tenantId: 'tenant-default',
      displayName: 'Azure OpenAI',
      providerPreset: 'azure-openai',
      protocol: 'AzureOpenAI',
      baseUrl: 'https://example.openai.azure.com/',
      modelId: 'gpt-4.1',
      isDefault: true,
      isEnabled: true,
      hasApiKey: true,
      createdAt: '2026-05-01T09:00:00.000Z',
      updatedAt: '2026-05-01T09:00:00.000Z',
    },
  ]

  await page.route('**/api/admin/tenants', async (route) => {
    await fulfillJson(route, ok(tenants))
  })

  await page.route('**/api/admin/model-connector-presets', async (route) => {
    await fulfillJson(route, ok(presets))
  })

  await page.route('**/api/admin/tenants/tenant-default/model-connectors', async (route) => {
    const method = route.request().method()
    if (method === 'POST') {
      const payload = route.request().postDataJSON() as Record<string, unknown>
      const created = {
        id: 'connector-new',
        tenantId: 'tenant-default',
        displayName: String(payload.displayName ?? 'New connector'),
        providerPreset: String(payload.providerPreset ?? 'azure-openai'),
        protocol: String(payload.protocol ?? 'AzureOpenAI'),
        baseUrl: String(payload.baseUrl ?? ''),
        modelId: String(payload.modelId ?? ''),
        isDefault: Boolean(payload.isDefault),
        isEnabled: Boolean(payload.isEnabled),
        hasApiKey: Boolean(payload.apiKey),
        createdAt: '2026-05-01T09:00:00.000Z',
        updatedAt: '2026-05-01T09:00:00.000Z',
      }
      connectors = [created, ...connectors.map((item) => ({ ...item, isDefault: created.isDefault ? false : item.isDefault }))]
      await fulfillJson(route, ok(created))
      return
    }

    await fulfillJson(route, ok(connectors))
  })

  await page.route('**/api/admin/tenants/tenant-default/model-connectors/connector-default-azure-openai/default', async (route) => {
    connectors = connectors.map((item) => ({ ...item, isDefault: item.id === 'connector-default-azure-openai' }))
    await fulfillJson(route, ok(connectors[0]))
  })

  await page.route('**/api/admin/tenants/tenant-default/model-connectors/connector-default-azure-openai', async (route) => {
    const method = route.request().method()
    if (method === 'PUT') {
      const payload = route.request().postDataJSON() as Record<string, unknown>
      connectors = connectors.map((item) =>
        item.id === 'connector-default-azure-openai'
          ? {
              ...item,
              displayName: String(payload.displayName ?? item.displayName),
              providerPreset: String(payload.providerPreset ?? item.providerPreset),
              protocol: String(payload.protocol ?? item.protocol),
              baseUrl: String(payload.baseUrl ?? item.baseUrl),
              modelId: String(payload.modelId ?? item.modelId),
              isDefault: Boolean(payload.isDefault),
              isEnabled: Boolean(payload.isEnabled),
              hasApiKey: item.hasApiKey || Boolean(payload.apiKey),
              updatedAt: '2026-05-02T09:00:00.000Z',
            }
          : {
              ...item,
              isDefault: Boolean(payload.isDefault) ? false : item.isDefault,
            },
      )
      await fulfillJson(route, ok(connectors.find((item) => item.id === 'connector-default-azure-openai')))
      return
    }

    if (method === 'DELETE') {
      connectors = connectors.filter((item) => item.id !== 'connector-default-azure-openai')
      await fulfillJson(route, ok({ deleted: true }))
      return
    }

    await fulfillJson(route, ok(connectors.find((item) => item.id === 'connector-default-azure-openai')))
  })
}
