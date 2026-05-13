export type ApiError = {
  code: string
  message: string
  details?: unknown
}

export type ApiResponse<T> = {
  success: boolean
  data?: T
  error?: ApiError
}

export type AuthenticatedUser = {
  id: string
  email: string
  displayName?: string | null
  roles: string[]
}

export type AuthResponse = {
  accessToken: string
  expiresAtUtc: string
  user: AuthenticatedUser
}

export type ConversationThread = {
  id: string
  title: string
  createdAt: string
  updatedAt: string
  archivedAt: string | null
}

export type ChatAttachment = {
  id: string
  kind: string
  fileName?: string | null
  mimeType?: string | null
  fileSize?: number | null
  downloadPath: string
  createdAt: string
}

export type ChatMessage = {
  id: string
  threadId: string
  role: 'user' | 'assistant' | 'system'
  kind: string
  contentText: string | null
  contentJson: Record<string, unknown> | null
  attachments: ChatAttachment[]
  turnIndex: number
  createdAt: string
}

export type NutritionSnapshot = {
  id: string
  threadId: string
  messageId: string
  trainingType: string | null
  dayType: string | null
  targetCalories: number | null
  targetProteinG: number | null
  targetCarbsG: number | null
  targetFatG: number | null
  consumedCalories: number | null
  consumedProteinG: number | null
  consumedCarbsG: number | null
  consumedFatG: number | null
  remainingCalories: number | null
  remainingProteinG: number | null
  remainingCarbsG: number | null
  remainingFatG: number | null
  nextSuggestions: Record<string, unknown> | null
  createdAt: string
}

export type ThreadDetail = ConversationThread & {
  messages: ChatMessage[]
  snapshots: NutritionSnapshot[]
}

export type UserProfile = {
  id: string
  userId: string
  displayName?: string | null
  sex?: string | null
  age?: number | null
  heightCm?: number | null
  weightKg?: number | null
  bodyFatPercent?: number | null
  activityLevel?: string | null
  goal?: string | null
  preferences?: string | null
  preferredModelConnectorId?: string | null
  createdAt: string
  updatedAt: string
}

export type TenantModelProtocol = 'OpenAICompatible' | 'AzureOpenAI' | 'Anthropic'

export type TenantSummary = {
  id: string
  name: string
  slug: string
  isSystemDefault: boolean
  userCount: number
  connectorCount: number
}

export type TenantModelConnectorPreset = {
  key: string
  displayName: string
  protocol: TenantModelProtocol
  baseUrl: string
  modelId: string
}

export type TenantModelConnectorOption = {
  id: string
  displayName: string
  providerPreset: string
  protocol: TenantModelProtocol
  modelId: string
  isDefault: boolean
}

export type TenantModelConnectorAdmin = {
  id: string
  tenantId: string
  displayName: string
  providerPreset: string
  protocol: TenantModelProtocol
  baseUrl: string
  modelId: string
  isDefault: boolean
  isEnabled: boolean
  hasApiKey: boolean
  inputTokenPricePer1M?: number | null
  outputTokenPricePer1M?: number | null
  cacheReadTokenPricePer1M?: number | null
  cacheWriteTokenPricePer1M?: number | null
  createdAt: string
  updatedAt: string
}

export type ModelUsageOverview = {
  range: '24h' | '7d' | '30d'
  rangeStartUtc: string
  rangeEndUtc: string
  totalRequests: number
  inputTokens: number
  outputTokens: number
  cacheReadTokens: number
  cacheWriteTokens: number
  totalTokens: number
  totalCostUsd?: number | null
  hasUnpricedRequests: boolean
  modelCount: number
  requestsPerMinute: number
  tokensPerMinute: number
}

export type ModelUsageTimeBucket = {
  bucketStartUtc: string
  label: string
  requestCount: number
  inputTokens: number
  outputTokens: number
  cacheReadTokens: number
  cacheWriteTokens: number
  totalTokens: number
  totalCostUsd?: number | null
  hasUnpricedRequests: boolean
}

export type ModelUsageSeriesPoint = {
  bucketStartUtc: string
  label: string
  value: number
}

export type ModelUsageModelSeries = {
  modelId: string
  label: string
  points: ModelUsageSeriesPoint[]
}

export type ModelUsageCharts = {
  requestCostBuckets: ModelUsageTimeBucket[]
  tokenBuckets: ModelUsageTimeBucket[]
  modelCostSeries: ModelUsageModelSeries[]
  modelRequestSeries: ModelUsageModelSeries[]
}

export type ModelRequestLogItem = {
  id: string
  requestTimeUtc: string
  threadId?: string | null
  conversationMessageId?: string | null
  connectorId: string
  connectorDisplayName: string
  providerPreset: string
  protocol: TenantModelProtocol
  modelId: string
  requestType: string
  status: string
  userAgent?: string | null
  requestSummary?: string | null
  toolEventsSummary?: string | null
  durationMs?: number | null
  inputTokens?: number | null
  outputTokens?: number | null
  cacheReadTokens?: number | null
  cacheWriteTokens?: number | null
  totalTokens?: number | null
  costUsd?: number | null
  errorCode?: string | null
  errorMessage?: string | null
}

export type ModelRequestLogList = {
  page: number
  pageSize: number
  totalCount: number
  items: ModelRequestLogItem[]
}

export type ModelRequestLogCleanup = {
  deletedCount: number
  cutoffUtc: string
}

export type FoodRecord = {
  id: number
  foodName: string
  calories: number
  protein: number
  carbs: number
  fat: number
  servingSize?: number | null
  servingUnit?: string | null
  consumptionDate: string
  mealType?: string | null
}

export type WorkoutSession = {
  id: number
  startTime: string
  endTime: string
  duration?: number | null
  caloriesBurned?: number | null
  notes?: string | null
}

export type WorkoutPlan = {
  id: number
  planName: string
  description?: string | null
  fitnessLevel?: string | null
  createdAt: string
  workoutDays: Array<{
    dayOfWeek: string
    exercises: Array<{
      name: string
      description?: string | null
      sets?: number | null
      reps?: number | null
      duration?: number | null
      restTime?: number | null
    }>
  }>
}

export type ProgressSummary = {
  userId: string
  currentWeightKg?: number | null
  bodyFatPercent?: number | null
  foodRecordCountToday: number
  workoutCountThisWeek: number
  caloriesToday: number
  proteinToday: number
  carbsToday: number
  fatToday: number
  headline: string
  recommendations: string[]
}

export type StreamEvent =
  | { type: 'user_message'; message: ChatMessage }
  | { type: 'assistant_message'; message: ChatMessage }
  | { type: 'snapshot'; snapshot: NutritionSnapshot }
  | { type: 'tool_event'; value: string }
  | { type: 'token'; value: string }
  | { type: 'error'; error?: string }
  | { type: 'done' }
