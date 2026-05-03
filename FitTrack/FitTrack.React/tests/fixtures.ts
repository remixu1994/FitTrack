import { expect, type Page, type Route } from '@playwright/test'

type AuthenticatedUser = {
  id: string
  email: string
  displayName: string
}

const defaultUser: AuthenticatedUser = {
  id: 'user-1',
  email: 'demo@fittrack.local',
  displayName: 'Demo Athlete',
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
    preferredAIProvider: 'AzureOpenAI',
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
