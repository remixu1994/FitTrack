import { defineConfig, devices } from '@playwright/test'

import { frontendBaseUrl, useManagedServers } from './playwright.e2e-env'

export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: 'list',
  use: {
    baseURL: frontendBaseUrl,
    trace: 'on-first-retry',
  },
  ...(useManagedServers
    ? {
        globalSetup: './playwright.global-setup.ts',
        globalTeardown: './playwright.global-teardown.ts',
      }
    : {}),
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
})
