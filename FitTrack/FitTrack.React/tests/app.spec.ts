import { expect, test } from '@playwright/test'

import {
  adminUser,
  createClientErrorCollector,
  expectNoHydrationErrors,
  mockAdminModelConnectorEndpoints,
  mockChatWorkspace,
  mockCoreSession,
  mockFoodRecords,
  mockLogin,
  mockModelConnectorEndpoints,
  mockProfilePage,
  mockProgressPage,
  mockWorkoutPages,
  seedAuthenticatedSession,
} from './fixtures'

test('home page renders primary calls to action', async ({ page }) => {
  const errors = createClientErrorCollector(page)

  await page.goto('/')

  await expect(page.getByText('Next.js workspace for the .NET 10 coaching backend')).toBeVisible()
  await expect(page.getByRole('link', { name: 'Login' })).toBeVisible()
  await expect(page.getByRole('link', { name: 'Open Coach' })).toBeVisible()
  await expectNoHydrationErrors(errors)
})

test('login page renders without hydration mismatch', async ({ page }) => {
  const errors = createClientErrorCollector(page)

  await page.goto('/login')

  await expect(page.getByRole('heading', { name: 'Sign in to the coach' })).toBeVisible()
  await expect(page.getByText('Use your ASP.NET Identity account.')).toBeVisible()
  await expectNoHydrationErrors(errors)
})

test('login submits and redirects into the chat workspace', async ({ page }) => {
  const errors = createClientErrorCollector(page)

  await mockLogin(page)
  await mockCoreSession(page)
  await mockChatWorkspace(page)

  await page.goto('/login')

  await page.getByLabel('Email').fill('demo@fittrack.local')
  await page.getByLabel('Password').fill('StrongPass123!')
  await page.getByRole('button', { name: 'Login' }).click()

  await expect(page).toHaveURL(/\/chat$/)
  await expect(page.getByRole('heading', { name: 'Coach', exact: true })).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Weekly check-in' })).toBeVisible()
  await expect(page.getByText('Ready for your next training block.')).toBeVisible()
  await expectNoHydrationErrors(errors)
})

test('authenticated pages render mocked data', async ({ page }) => {
  const errors = createClientErrorCollector(page)

  await seedAuthenticatedSession(page)
  await mockCoreSession(page)
  await mockChatWorkspace(page)
  await mockFoodRecords(page)
  await mockWorkoutPages(page)
  await mockProgressPage(page)
  await mockProfilePage(page)
  await mockModelConnectorEndpoints(page)

  await page.goto('/food-records')
  await expect(page.getByRole('heading', { name: 'Food Records' })).toBeVisible()
  await expect(page.getByText('Chicken Rice Bowl')).toBeVisible()

  await page.getByRole('link', { name: 'Workouts' }).click()
  await expect(page.getByRole('heading', { name: 'Workouts' })).toBeVisible()
  await expect(page.getByText('Strength Base')).toBeVisible()
  await expect(page.getByText('50 min | 410 kcal')).toBeVisible()

  await page.getByRole('link', { name: 'Progress' }).click()
  await expect(page.getByRole('heading', { name: 'Progress' })).toBeVisible()
  await expect(page.getByRole('heading', { name: 'You are on pace for a strong recovery week.' })).toBeVisible()

  await page.getByRole('link', { name: 'Profile' }).click()
  await expect(page.getByRole('heading', { name: 'Profile' })).toBeVisible()
  await expect(page.getByLabel('Display name')).toHaveValue('Demo Athlete')
  await expect(page.getByLabel('Preferred model')).toHaveValue('connector-default-azure-openai')

  await expectNoHydrationErrors(errors)
})

test('admin can open the tenant model connector settings page', async ({ page }) => {
  const errors = createClientErrorCollector(page)

  await seedAuthenticatedSession(page, adminUser)
  await mockCoreSession(page, adminUser)
  await mockProfilePage(page, adminUser)
  await mockModelConnectorEndpoints(page)
  await mockAdminModelConnectorEndpoints(page)

  await page.goto('/settings/models')

  await expect(page.getByRole('heading', { name: 'Models' })).toBeVisible()
  await expect(page.getByText('Default Tenant')).toBeVisible()
  await expect(page.getByLabel('Display name')).toHaveValue('Azure OpenAI')
  await expect(page.getByLabel('Protocol')).toHaveValue('AzureOpenAI')
  await expect(page.getByRole('button', { name: 'Update connector' })).toBeVisible()

  await expectNoHydrationErrors(errors)
})
