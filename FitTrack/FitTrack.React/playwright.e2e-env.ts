import { tmpdir } from 'node:os'
import { join } from 'node:path'

export const useManagedServers = process.env.PLAYWRIGHT_MANAGED_SERVERS === '1'

export const frontendPort = process.env.PLAYWRIGHT_FRONTEND_PORT ?? (useManagedServers ? '3100' : '3000')
export const backendPort = process.env.PLAYWRIGHT_BACKEND_PORT ?? (useManagedServers ? '5197' : '5097')

export const frontendBaseUrl = `http://localhost:${frontendPort}`
export const backendBaseUrl = `http://localhost:${backendPort}`

export const playwrightDbPath = join(tmpdir(), 'fittrack-copilot-playwright.db')
export const playwrightServerStatePath = join(tmpdir(), 'fittrack-playwright-servers.json')
