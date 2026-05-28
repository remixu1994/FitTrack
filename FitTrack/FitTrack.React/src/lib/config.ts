const copilotPort = process.env.NEXT_PUBLIC_COPILOT_PORT?.trim()
const configuredApiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL?.trim() || undefined
const derivedLocalApiBaseUrl = copilotPort ? `http://localhost:${copilotPort}` : undefined

export const apiBaseUrl = (derivedLocalApiBaseUrl ?? configuredApiBaseUrl ?? 'http://localhost:5097').replace(/\/$/, '')
