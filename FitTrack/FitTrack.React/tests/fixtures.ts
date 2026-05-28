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

  let connectors: Array<{
    id: string
    tenantId: string
    displayName: string
    providerPreset: string
    protocol: string
    baseUrl: string
    modelId: string
    isDefault: boolean
    isEnabled: boolean
    hasApiKey: boolean
    inputTokenPricePer1M: number | null
    outputTokenPricePer1M: number | null
    cacheReadTokenPricePer1M: number | null
    cacheWriteTokenPricePer1M: number | null
    createdAt: string
    updatedAt: string
  }> = [
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
      inputTokenPricePer1M: 2.4,
      outputTokenPricePer1M: 9.6,
      cacheReadTokenPricePer1M: 0.6,
      cacheWriteTokenPricePer1M: 1.2,
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
        inputTokenPricePer1M: Number(payload.inputTokenPricePer1M ?? 0) || null,
        outputTokenPricePer1M: Number(payload.outputTokenPricePer1M ?? 0) || null,
        cacheReadTokenPricePer1M: Number(payload.cacheReadTokenPricePer1M ?? 0) || null,
        cacheWriteTokenPricePer1M: Number(payload.cacheWriteTokenPricePer1M ?? 0) || null,
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
              inputTokenPricePer1M: payload.inputTokenPricePer1M === null || payload.inputTokenPricePer1M === undefined ? null : Number(payload.inputTokenPricePer1M),
              outputTokenPricePer1M: payload.outputTokenPricePer1M === null || payload.outputTokenPricePer1M === undefined ? null : Number(payload.outputTokenPricePer1M),
              cacheReadTokenPricePer1M: payload.cacheReadTokenPricePer1M === null || payload.cacheReadTokenPricePer1M === undefined ? null : Number(payload.cacheReadTokenPricePer1M),
              cacheWriteTokenPricePer1M: payload.cacheWriteTokenPricePer1M === null || payload.cacheWriteTokenPricePer1M === undefined ? null : Number(payload.cacheWriteTokenPricePer1M),
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

  await page.route('**/api/admin/tenants/tenant-default/model-usage/overview?*', async (route) => {
    await fulfillJson(route, ok({
        range: '24h',
        rangeStartUtc: '2026-05-01T00:00:00.000Z',
        rangeEndUtc: '2026-05-02T00:00:00.000Z',
        totalRequests: 18,
        inputTokens: 12450,
        outputTokens: 4380,
        cacheReadTokens: 1500,
        cacheWriteTokens: 420,
        totalTokens: 18750,
        totalCostUsd: 0.1542,
        hasUnpricedRequests: false,
        modelCount: 3,
        requestsPerMinute: 0.75,
        tokensPerMinute: 781.25,
      }))
  })

  await page.route('**/api/admin/tenants/tenant-default/model-usage/charts?*', async (route) => {
    const labels = ['05-01 00:00', '05-01 04:00', '05-01 08:00', '05-01 12:00', '05-01 16:00', '05-01 20:00']
    await fulfillJson(route, ok({
        requestCostBuckets: labels.map((label, index) => ({
          bucketStartUtc: `2026-05-01T${String(index * 4).padStart(2, '0')}:00:00.000Z`,
          label,
          requestCount: [1, 2, 4, 5, 3, 3][index],
          inputTokens: [240, 880, 2480, 3560, 2100, 3190][index],
          outputTokens: [80, 210, 640, 1020, 520, 910][index],
          cacheReadTokens: [0, 0, 200, 400, 300, 600][index],
          cacheWriteTokens: [0, 0, 50, 120, 100, 150][index],
          totalTokens: [320, 1090, 3320, 4700, 2920, 4850][index],
          totalCostUsd: [0.004, 0.009, 0.028, 0.043, 0.026, 0.044][index],
          hasUnpricedRequests: false,
        })),
        tokenBuckets: labels.map((label, index) => ({
          bucketStartUtc: `2026-05-01T${String(index * 4).padStart(2, '0')}:00:00.000Z`,
          label,
          requestCount: [1, 2, 4, 5, 3, 3][index],
          inputTokens: [240, 880, 2480, 3560, 2100, 3190][index],
          outputTokens: [80, 210, 640, 1020, 520, 910][index],
          cacheReadTokens: [0, 0, 200, 400, 300, 600][index],
          cacheWriteTokens: [0, 0, 50, 120, 100, 150][index],
          totalTokens: [320, 1090, 3320, 4700, 2920, 4850][index],
          totalCostUsd: [0.004, 0.009, 0.028, 0.043, 0.026, 0.044][index],
          hasUnpricedRequests: false,
        })),
        modelCostSeries: [
          {
            modelId: 'gpt-4.1',
            label: 'gpt-4.1 via Azure OpenAI',
            points: labels.map((label, index) => ({
              bucketStartUtc: `2026-05-01T${String(index * 4).padStart(2, '0')}:00:00.000Z`,
              label,
              value: [0.002, 0.006, 0.018, 0.025, 0.014, 0.022][index],
            })),
          },
          {
            modelId: 'gpt-5-codex',
            label: 'gpt-5-codex via OpenAI/Codex',
            points: labels.map((label, index) => ({
              bucketStartUtc: `2026-05-01T${String(index * 4).padStart(2, '0')}:00:00.000Z`,
              label,
              value: [0.001, 0.002, 0.008, 0.011, 0.009, 0.013][index],
            })),
          },
        ],
        modelRequestSeries: [
          {
            modelId: 'gpt-4.1',
            label: 'gpt-4.1 via Azure OpenAI',
            points: labels.map((label, index) => ({
              bucketStartUtc: `2026-05-01T${String(index * 4).padStart(2, '0')}:00:00.000Z`,
              label,
              value: [1, 1, 2, 3, 2, 2][index],
            })),
          },
          {
            modelId: 'gpt-5-codex',
            label: 'gpt-5-codex via OpenAI/Codex',
            points: labels.map((label, index) => ({
              bucketStartUtc: `2026-05-01T${String(index * 4).padStart(2, '0')}:00:00.000Z`,
              label,
              value: [0, 1, 2, 2, 1, 1][index],
            })),
          },
        ],
      }))
  })

  await page.route('**/api/admin/tenants/tenant-default/model-request-logs?*', async (route) => {
    await fulfillJson(route, ok({
        page: 1,
        pageSize: 20,
        totalCount: 2,
        items: [
          {
            id: 'log-1',
            requestTimeUtc: '2026-05-01T12:12:00.000Z',
            threadId: 'thread-1',
            conversationMessageId: 'message-1',
            connectorId: 'connector-default-azure-openai',
            connectorDisplayName: 'Azure OpenAI',
            providerPreset: 'azure-openai',
            protocol: 'AzureOpenAI',
            modelId: 'gpt-4.1',
            requestType: 'NutritionAgent',
            status: 'Succeeded',
            userAgent: 'Playwright',
            requestSummary: 'High protein lunch recommendation',
            toolEventsSummary: 'subagent:nutrition',
            durationMs: 1240,
            inputTokens: 880,
            outputTokens: 210,
            cacheReadTokens: 0,
            cacheWriteTokens: 0,
            totalTokens: 1090,
            costUsd: 0.009,
            errorCode: null,
            errorMessage: null,
          },
          {
            id: 'log-2',
            requestTimeUtc: '2026-05-01T16:35:00.000Z',
            threadId: 'thread-1',
            conversationMessageId: 'message-2',
            connectorId: 'connector-default-azure-openai',
            connectorDisplayName: 'Azure OpenAI',
            providerPreset: 'azure-openai',
            protocol: 'AzureOpenAI',
            modelId: 'gpt-4.1',
            requestType: 'WorkoutAgent',
            status: 'Failed',
            userAgent: 'Playwright',
            requestSummary: 'Deload plan refresh',
            toolEventsSummary: 'subagent:workout',
            durationMs: 900,
            inputTokens: 640,
            outputTokens: null,
            cacheReadTokens: null,
            cacheWriteTokens: null,
            totalTokens: null,
            costUsd: null,
            errorCode: 'TimeoutException',
            errorMessage: 'The upstream model request timed out.',
          },
        ],
      }))
  })

  await page.route('**/api/admin/tenants/tenant-default/model-request-logs/cleanup', async (route) => {
    await fulfillJson(route, ok({
        deletedCount: 4,
        cutoffUtc: '2026-02-01T00:00:00.000Z',
      }))
  })
}
