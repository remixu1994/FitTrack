const copilotPort = process.env.NEXT_PUBLIC_COPILOT_PORT?.trim()
const derivedLocalApiBaseUrl = copilotPort ? `http://localhost:${copilotPort}` : undefined

export const apiBaseUrl = (derivedLocalApiBaseUrl ?? process.env.NEXT_PUBLIC_API_BASE_URL ?? 'http://localhost:5097').replace(/\/$/, '')
