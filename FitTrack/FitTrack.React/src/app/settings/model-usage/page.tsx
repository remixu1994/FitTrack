'use client'

import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useMemo, useState } from 'react'

import { AppShell } from '@/components/layout/app-shell'
import { authStorage, FITTRACK_USER_CHANGED_EVENT } from '@/lib/auth'
import { apiFetch } from '@/lib/http'
import type {
  AuthenticatedUser,
  ModelRequestLogCleanup,
  ModelRequestLogItem,
  ModelRequestLogList,
  ModelUsageCharts,
  ModelUsageModelSeries,
  ModelUsageOverview,
  TenantModelConnectorAdmin,
  TenantSummary,
} from '@/types/fittrack'

type RangeKey = '24h' | '7d' | '30d'

type LogFilters = {
  connectorId: string
  modelSearch: string
  status: string
  requestType: string
  page: number
  pageSize: number
}

const rangeOptions: Array<{ key: RangeKey; label: string }> = [
  { key: '24h', label: 'Last 24h' },
  { key: '7d', label: 'Last 7d' },
  { key: '30d', label: 'Last 30d' },
]

const chartPalette = ['#22d3ee', '#34d399', '#60a5fa', '#a78bfa', '#f59e0b', '#fb7185']

export default function ModelUsagePage() {
  const queryClient = useQueryClient()
  const [currentUser, setCurrentUser] = useState<AuthenticatedUser | null>(null)
  const [selectedTenantId, setSelectedTenantId] = useState('')
  const [range, setRange] = useState<RangeKey>('24h')
  const [statusMessage, setStatusMessage] = useState<string | null>(null)
  const [filters, setFilters] = useState<LogFilters>({
    connectorId: '',
    modelSearch: '',
    status: '',
    requestType: '',
    page: 1,
    pageSize: 20,
  })
  const isAdmin = currentUser?.roles.includes('Admin') ?? false

  useEffect(() => {
    setCurrentUser(authStorage.getUser())

    const handleUserChanged = (event: Event) => {
      const customEvent = event as CustomEvent<AuthenticatedUser | null>
      setCurrentUser(customEvent.detail ?? null)
    }

    window.addEventListener(FITTRACK_USER_CHANGED_EVENT, handleUserChanged as EventListener)
    return () => {
      window.removeEventListener(FITTRACK_USER_CHANGED_EVENT, handleUserChanged as EventListener)
    }
  }, [])

  const tenantsQuery = useQuery({
    queryKey: ['admin-tenants'],
    queryFn: () => apiFetch<TenantSummary[]>('/api/admin/tenants'),
    enabled: isAdmin,
  })

  const connectorsQuery = useQuery({
    queryKey: ['admin-tenant-connectors', selectedTenantId],
    queryFn: () => apiFetch<TenantModelConnectorAdmin[]>(`/api/admin/tenants/${selectedTenantId}/model-connectors`),
    enabled: isAdmin && selectedTenantId.length > 0,
  })

  const overviewQuery = useQuery({
    queryKey: ['admin-model-usage-overview', selectedTenantId, range],
    queryFn: () => apiFetch<ModelUsageOverview>(`/api/admin/tenants/${selectedTenantId}/model-usage/overview?range=${range}`),
    enabled: isAdmin && selectedTenantId.length > 0,
  })

  const chartsQuery = useQuery({
    queryKey: ['admin-model-usage-charts', selectedTenantId, range],
    queryFn: () => apiFetch<ModelUsageCharts>(`/api/admin/tenants/${selectedTenantId}/model-usage/charts?range=${range}`),
    enabled: isAdmin && selectedTenantId.length > 0,
  })

  const logQueryString = useMemo(() => {
    const params = new URLSearchParams({
      range,
      page: String(filters.page),
      pageSize: String(filters.pageSize),
    })

    if (filters.connectorId) params.set('connectorId', filters.connectorId)
    if (filters.modelSearch.trim()) params.set('modelSearch', filters.modelSearch.trim())
    if (filters.status) params.set('status', filters.status)
    if (filters.requestType) params.set('requestType', filters.requestType)

    return params.toString()
  }, [filters, range])

  const logsQuery = useQuery({
    queryKey: ['admin-model-request-logs', selectedTenantId, logQueryString],
    queryFn: () => apiFetch<ModelRequestLogList>(`/api/admin/tenants/${selectedTenantId}/model-request-logs?${logQueryString}`),
    enabled: isAdmin && selectedTenantId.length > 0,
  })

  useEffect(() => {
    if (!selectedTenantId && tenantsQuery.data?.length) {
      setSelectedTenantId(tenantsQuery.data[0]!.id)
    }
  }, [selectedTenantId, tenantsQuery.data])

  const connectors = connectorsQuery.data ?? []
  const overview = overviewQuery.data
  const charts = chartsQuery.data
  const logs = logsQuery.data
  const totalPages = logs ? Math.max(1, Math.ceil(logs.totalCount / logs.pageSize)) : 1

  if (!isAdmin) {
    return (
      <AppShell title="Usage">
        <div className="rounded-[32px] border border-amber-300/20 bg-amber-300/10 p-6 text-amber-100">
          This page is only available to global admins.
        </div>
      </AppShell>
    )
  }

  return (
    <AppShell title="Usage">
      <div className="space-y-6">
        <section className="rounded-[32px] border border-white/10 bg-slate-950/55 p-6">
          <div className="flex flex-wrap items-start justify-between gap-4">
            <div>
              <p className="text-xs uppercase tracking-[0.35em] text-cyan-300">Tenant Dashboard</p>
              <h3 className="mt-3 text-2xl font-semibold text-white">Model request analytics</h3>
              <p className="mt-2 max-w-3xl text-sm text-slate-400">
                Token usage, connector cost visibility, and per-request logs are isolated by tenant. The dashboard reflects real provider calls, not chat turns.
              </p>
            </div>
            <div className="flex flex-wrap items-center gap-3">
              <select
                aria-label="Tenant"
                value={selectedTenantId}
                onChange={(event) => {
                  setSelectedTenantId(event.target.value)
                  setFilters((current) => ({ ...current, connectorId: '', page: 1 }))
                  setStatusMessage(null)
                }}
                className="rounded-2xl border border-white/10 bg-white/5 px-4 py-3 text-sm text-white outline-none focus:border-cyan-300"
              >
                {(tenantsQuery.data ?? []).map((tenant) => (
                  <option key={tenant.id} value={tenant.id}>
                    {tenant.name}
                  </option>
                ))}
              </select>

              <button
                type="button"
                onClick={() => {
                  void Promise.all([
                    queryClient.invalidateQueries({ queryKey: ['admin-model-usage-overview', selectedTenantId] }),
                    queryClient.invalidateQueries({ queryKey: ['admin-model-usage-charts', selectedTenantId] }),
                    queryClient.invalidateQueries({ queryKey: ['admin-model-request-logs', selectedTenantId] }),
                  ])
                }}
                className="rounded-full border border-white/10 px-4 py-3 text-xs uppercase tracking-[0.25em] text-slate-200 hover:bg-white/10"
              >
                Refresh
              </button>

              <button
                type="button"
                onClick={async () => {
                  if (!selectedTenantId) return
                  const result = await apiFetch<ModelRequestLogCleanup>(`/api/admin/tenants/${selectedTenantId}/model-request-logs/cleanup`, {
                    method: 'POST',
                  })
                  setStatusMessage(`Removed ${result.deletedCount} logs older than 90 days.`)
                  await Promise.all([
                    queryClient.invalidateQueries({ queryKey: ['admin-model-usage-overview', selectedTenantId] }),
                    queryClient.invalidateQueries({ queryKey: ['admin-model-usage-charts', selectedTenantId] }),
                    queryClient.invalidateQueries({ queryKey: ['admin-model-request-logs', selectedTenantId] }),
                  ])
                }}
                className="rounded-full border border-cyan-300/25 bg-cyan-300/10 px-4 py-3 text-xs uppercase tracking-[0.25em] text-cyan-100 hover:bg-cyan-300/15"
              >
                Cleanup 90d+
              </button>
            </div>
          </div>

          <div className="mt-6 flex flex-wrap gap-3">
            {rangeOptions.map((option) => (
              <button
                key={option.key}
                type="button"
                onClick={() => {
                  setRange(option.key)
                  setFilters((current) => ({ ...current, page: 1 }))
                }}
                className={`rounded-full px-4 py-2 text-sm transition ${
                  range === option.key
                    ? 'bg-cyan-300 text-slate-950'
                    : 'border border-white/10 bg-white/5 text-slate-300 hover:bg-white/10'
                }`}
              >
                {option.label}
              </button>
            ))}
          </div>

          {statusMessage ? (
            <div className="mt-6 rounded-[24px] border border-emerald-300/20 bg-emerald-300/10 px-4 py-3 text-sm text-emerald-100">
              {statusMessage}
            </div>
          ) : null}

          <div className="mt-6 grid gap-4 md:grid-cols-2 xl:grid-cols-6">
            <MetricCard
              label="Total cost"
              value={overview ? formatCurrency(overview.totalCostUsd) : '--'}
              hint={overview?.hasUnpricedRequests ? 'Partial pricing' : rangeLabel(range)}
              accent="from-cyan-400/25 to-blue-400/25"
            />
            <MetricCard label="Requests" value={overview ? formatInteger(overview.totalRequests) : '--'} hint="Provider calls" />
            <MetricCard label="Tokens" value={overview ? formatInteger(overview.totalTokens) : '--'} hint="All token types" />
            <MetricCard label="Models" value={overview ? formatInteger(overview.modelCount) : '--'} hint="Distinct model IDs" />
            <MetricCard label="RPM" value={overview ? overview.requestsPerMinute.toFixed(2) : '--'} hint="Requests / minute" />
            <MetricCard label="TPM" value={overview ? overview.tokensPerMinute.toFixed(2) : '--'} hint="Tokens / minute" />
          </div>
        </section>

        <section className="grid gap-6 xl:grid-cols-2">
          <LineChartCard
            title="Cost trend"
            subtitle="Cost and request volume across the selected window"
            series={[
              { label: 'Cost (USD)', color: '#22d3ee', values: (charts?.requestCostBuckets ?? []).map((bucket) => ({ label: bucket.label, value: bucket.totalCostUsd ?? 0 })) },
              { label: 'Requests', color: '#34d399', values: (charts?.requestCostBuckets ?? []).map((bucket) => ({ label: bucket.label, value: bucket.requestCount })) },
            ]}
          />

          <LineChartCard
            title="Token trend"
            subtitle="Input, output, cache read, and cache write tokens"
            series={[
              { label: 'Input', color: '#60a5fa', values: (charts?.tokenBuckets ?? []).map((bucket) => ({ label: bucket.label, value: bucket.inputTokens })) },
              { label: 'Output', color: '#a78bfa', values: (charts?.tokenBuckets ?? []).map((bucket) => ({ label: bucket.label, value: bucket.outputTokens })) },
              { label: 'Cache read', color: '#34d399', values: (charts?.tokenBuckets ?? []).map((bucket) => ({ label: bucket.label, value: bucket.cacheReadTokens })) },
              { label: 'Cache write', color: '#f59e0b', values: (charts?.tokenBuckets ?? []).map((bucket) => ({ label: bucket.label, value: bucket.cacheWriteTokens })) },
            ]}
          />

          <SeriesChartCard
            title="Model cost"
            subtitle="Top model cost series for the active tenant"
            series={charts?.modelCostSeries ?? []}
          />

          <SeriesChartCard
            title="Model request volume"
            subtitle="Top model request counts for the active tenant"
            series={charts?.modelRequestSeries ?? []}
          />
        </section>

        <section className="rounded-[32px] border border-white/10 bg-slate-950/55 p-6">
          <div className="flex flex-wrap items-start justify-between gap-4">
            <div>
              <p className="text-xs uppercase tracking-[0.35em] text-cyan-300">Request Logs</p>
              <h3 className="mt-3 text-2xl font-semibold text-white">Structured model call history</h3>
              <p className="mt-2 text-sm text-slate-400">
                Filter by connector, model, status, and request type. Logs keep token and cost metadata only, without storing raw prompts or responses.
              </p>
            </div>
            <div className="rounded-[24px] border border-white/10 bg-white/5 px-4 py-3 text-sm text-slate-300">
              {logs ? `${formatInteger(logs.totalCount)} entries` : 'Loading logs'}
            </div>
          </div>

          <div className="mt-6 grid gap-4 lg:grid-cols-3 xl:grid-cols-6">
            <label className="xl:col-span-2">
              <span className="mb-2 block text-xs uppercase tracking-[0.3em] text-slate-400">Model name</span>
              <input
                value={filters.modelSearch}
                onChange={(event) => setFilters((current) => ({ ...current, modelSearch: event.target.value, page: 1 }))}
                placeholder="Search model or connector"
                className="w-full rounded-2xl border border-white/10 bg-white/5 px-4 py-3 text-sm text-white outline-none focus:border-cyan-300"
              />
            </label>

            <label>
              <span className="mb-2 block text-xs uppercase tracking-[0.3em] text-slate-400">Connector</span>
              <select
                value={filters.connectorId}
                onChange={(event) => setFilters((current) => ({ ...current, connectorId: event.target.value, page: 1 }))}
                className="w-full rounded-2xl border border-white/10 bg-white/5 px-4 py-3 text-sm text-white outline-none focus:border-cyan-300"
              >
                <option value="">All connectors</option>
                {connectors.map((connector) => (
                  <option key={connector.id} value={connector.id}>
                    {connector.displayName}
                  </option>
                ))}
              </select>
            </label>

            <label>
              <span className="mb-2 block text-xs uppercase tracking-[0.3em] text-slate-400">Status</span>
              <select
                value={filters.status}
                onChange={(event) => setFilters((current) => ({ ...current, status: event.target.value, page: 1 }))}
                className="w-full rounded-2xl border border-white/10 bg-white/5 px-4 py-3 text-sm text-white outline-none focus:border-cyan-300"
              >
                <option value="">All</option>
                <option value="Succeeded">Succeeded</option>
                <option value="Failed">Failed</option>
              </select>
            </label>

            <label>
              <span className="mb-2 block text-xs uppercase tracking-[0.3em] text-slate-400">Request type</span>
              <select
                value={filters.requestType}
                onChange={(event) => setFilters((current) => ({ ...current, requestType: event.target.value, page: 1 }))}
                className="w-full rounded-2xl border border-white/10 bg-white/5 px-4 py-3 text-sm text-white outline-none focus:border-cyan-300"
              >
                <option value="">All</option>
                <option value="Chat">Chat</option>
                <option value="VisionRecognition">VisionRecognition</option>
                <option value="TextRecognition">TextRecognition</option>
                <option value="NutritionAgent">NutritionAgent</option>
                <option value="WorkoutAgent">WorkoutAgent</option>
              </select>
            </label>

            <label>
              <span className="mb-2 block text-xs uppercase tracking-[0.3em] text-slate-400">Rows</span>
              <select
                value={String(filters.pageSize)}
                onChange={(event) => setFilters((current) => ({ ...current, pageSize: Number(event.target.value), page: 1 }))}
                className="w-full rounded-2xl border border-white/10 bg-white/5 px-4 py-3 text-sm text-white outline-none focus:border-cyan-300"
              >
                <option value="10">10 / page</option>
                <option value="20">20 / page</option>
                <option value="50">50 / page</option>
              </select>
            </label>
          </div>

          <div className="mt-6 overflow-x-auto rounded-[28px] border border-white/10">
            <table className="min-w-full divide-y divide-white/10 text-sm">
              <thead className="bg-white/5 text-left text-xs uppercase tracking-[0.25em] text-slate-400">
                <tr>
                  <th className="px-4 py-3">Time</th>
                  <th className="px-4 py-3">Model</th>
                  <th className="px-4 py-3">Connector</th>
                  <th className="px-4 py-3">Type</th>
                  <th className="px-4 py-3">Status</th>
                  <th className="px-4 py-3">Summary</th>
                  <th className="px-4 py-3">Duration</th>
                  <th className="px-4 py-3">Input</th>
                  <th className="px-4 py-3">Output</th>
                  <th className="px-4 py-3">Total</th>
                  <th className="px-4 py-3">Cost</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-white/5 text-slate-200">
                {(logs?.items ?? []).map((item) => (
                  <LogRow key={item.id} item={item} />
                ))}
                {logs && logs.items.length === 0 ? (
                  <tr>
                    <td colSpan={11} className="px-6 py-12 text-center text-slate-400">
                      No model requests matched the current filters.
                    </td>
                  </tr>
                ) : null}
              </tbody>
            </table>
          </div>

          <div className="mt-5 flex flex-wrap items-center justify-between gap-3 text-sm text-slate-400">
            <p>
              Page {logs?.page ?? 1} of {totalPages}
            </p>
            <div className="flex gap-3">
              <button
                type="button"
                disabled={(logs?.page ?? 1) <= 1}
                onClick={() => setFilters((current) => ({ ...current, page: Math.max(1, current.page - 1) }))}
                className="rounded-full border border-white/10 px-4 py-2 text-xs uppercase tracking-[0.25em] text-slate-200 disabled:opacity-50"
              >
                Previous
              </button>
              <button
                type="button"
                disabled={(logs?.page ?? 1) >= totalPages}
                onClick={() => setFilters((current) => ({ ...current, page: current.page + 1 }))}
                className="rounded-full border border-white/10 px-4 py-2 text-xs uppercase tracking-[0.25em] text-slate-200 disabled:opacity-50"
              >
                Next
              </button>
            </div>
          </div>
        </section>
      </div>
    </AppShell>
  )
}

function MetricCard({
  label,
  value,
  hint,
  accent,
}: {
  label: string
  value: string
  hint: string
  accent?: string
}) {
  return (
    <div className={`rounded-[28px] border border-white/10 p-5 ${accent ? `bg-gradient-to-br ${accent} to-white/5` : 'bg-white/5'}`}>
      <p className="text-xs uppercase tracking-[0.3em] text-slate-300">{label}</p>
      <p className="mt-4 text-3xl font-semibold text-white">{value}</p>
      <p className="mt-3 text-sm text-slate-400">{hint}</p>
    </div>
  )
}

function LineChartCard({
  title,
  subtitle,
  series,
}: {
  title: string
  subtitle: string
  series: Array<{ label: string; color: string; values: Array<{ label: string; value: number }> }>
}) {
  return (
    <section className="rounded-[32px] border border-white/10 bg-slate-950/55 p-6">
      <p className="text-xs uppercase tracking-[0.35em] text-cyan-300">{title}</p>
      <h3 className="mt-3 text-xl font-semibold text-white">{title}</h3>
      <p className="mt-2 text-sm text-slate-400">{subtitle}</p>
      <ChartSurface series={series} />
    </section>
  )
}

function SeriesChartCard({
  title,
  subtitle,
  series,
}: {
  title: string
  subtitle: string
  series: ModelUsageModelSeries[]
}) {
  return (
    <section className="rounded-[32px] border border-white/10 bg-slate-950/55 p-6">
      <p className="text-xs uppercase tracking-[0.35em] text-cyan-300">{title}</p>
      <h3 className="mt-3 text-xl font-semibold text-white">{title}</h3>
      <p className="mt-2 text-sm text-slate-400">{subtitle}</p>
      <ChartSurface
        series={series.map((item, index) => ({
          label: item.label,
          color: chartPalette[index % chartPalette.length]!,
          values: item.points.map((point) => ({ label: point.label, value: point.value })),
        }))}
      />
    </section>
  )
}

function ChartSurface({
  series,
}: {
  series: Array<{ label: string; color: string; values: Array<{ label: string; value: number }> }>
}) {
  const width = 920
  const height = 240
  const labels = series[0]?.values.map((value) => value.label) ?? []
  const maxValue = Math.max(1, ...series.flatMap((item) => item.values.map((value) => value.value)))
  const visibleTicks = labels.filter((_, index) => index === 0 || index === labels.length - 1 || index % Math.max(1, Math.floor(labels.length / 6)) === 0)

  return (
    <div className="mt-6 rounded-[28px] border border-white/10 bg-slate-950/50 p-4">
      <svg viewBox={`0 0 ${width} ${height}`} className="h-64 w-full">
        {[0.25, 0.5, 0.75, 1].map((ratio) => {
          const y = height - ratio * (height - 28) - 14
          return <line key={ratio} x1="0" y1={y} x2={width} y2={y} stroke="rgba(148,163,184,0.18)" strokeDasharray="5 5" />
        })}

        {series.map((item) => (
          <polyline
            key={item.label}
            fill="none"
            stroke={item.color}
            strokeWidth="3"
            points={buildPolylinePoints(item.values, maxValue, width, height)}
          />
        ))}
      </svg>

      <div className="mt-4 flex flex-wrap gap-3 text-xs text-slate-300">
        {series.map((item) => (
          <div key={item.label} className="flex items-center gap-2">
            <span className="h-2.5 w-2.5 rounded-full" style={{ backgroundColor: item.color }} />
            {item.label}
          </div>
        ))}
      </div>

      <div className="mt-4 flex flex-wrap justify-between gap-2 text-[11px] uppercase tracking-[0.2em] text-slate-500">
        {visibleTicks.map((label) => (
          <span key={label}>{label}</span>
        ))}
      </div>
    </div>
  )
}

function LogRow({ item }: { item: ModelRequestLogItem }) {
  return (
    <tr className="align-top">
      <td className="px-4 py-4 text-slate-300">{formatDateTime(item.requestTimeUtc)}</td>
      <td className="px-4 py-4">
        <p className="font-medium text-white">{item.modelId}</p>
        <p className="mt-1 text-xs text-slate-500">{item.protocol}</p>
      </td>
      <td className="px-4 py-4">
        <p className="text-white">{item.connectorDisplayName}</p>
        <p className="mt-1 text-xs text-slate-500">{item.providerPreset}</p>
      </td>
      <td className="px-4 py-4 text-slate-300">{item.requestType}</td>
      <td className="px-4 py-4">
        <span
          className={`inline-flex rounded-full px-3 py-1 text-xs uppercase tracking-[0.2em] ${
            item.status === 'Succeeded'
              ? 'bg-emerald-300/15 text-emerald-100'
              : 'bg-rose-300/15 text-rose-100'
          }`}
        >
          {item.status}
        </span>
      </td>
      <td className="max-w-[260px] px-4 py-4">
        <p className="text-white">{item.requestSummary || '--'}</p>
        {item.errorMessage ? <p className="mt-2 text-xs text-rose-200">{item.errorMessage}</p> : null}
        {item.toolEventsSummary ? <p className="mt-2 text-xs text-slate-500">{item.toolEventsSummary}</p> : null}
      </td>
      <td className="px-4 py-4 text-slate-300">{item.durationMs ? `${item.durationMs} ms` : '--'}</td>
      <td className="px-4 py-4 text-slate-300">{formatNullableInteger(item.inputTokens)}</td>
      <td className="px-4 py-4 text-slate-300">{formatNullableInteger(item.outputTokens)}</td>
      <td className="px-4 py-4 text-slate-300">{formatNullableInteger(item.totalTokens)}</td>
      <td className="px-4 py-4 text-slate-300">{formatCurrency(item.costUsd)}</td>
    </tr>
  )
}

function buildPolylinePoints(values: Array<{ value: number }>, maxValue: number, width: number, height: number) {
  if (values.length === 0) {
    return ''
  }

  const usableWidth = width - 24
  const usableHeight = height - 28
  const step = values.length === 1 ? 0 : usableWidth / (values.length - 1)

  return values
    .map((point, index) => {
      const x = 12 + index * step
      const y = height - 14 - (point.value / maxValue) * usableHeight
      return `${x},${Math.max(12, y)}`
    })
    .join(' ')
}

function formatCurrency(value?: number | null) {
  if (value === null || value === undefined) {
    return '--'
  }

  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 4,
    maximumFractionDigits: 6,
  }).format(value)
}

function formatInteger(value: number) {
  return new Intl.NumberFormat('en-US').format(value)
}

function formatNullableInteger(value?: number | null) {
  return value === null || value === undefined ? '--' : formatInteger(value)
}

function formatDateTime(value: string) {
  return new Intl.DateTimeFormat('en-US', {
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  }).format(new Date(value))
}

function rangeLabel(range: RangeKey) {
  return rangeOptions.find((item) => item.key === range)?.label ?? range
}
