'use client'

import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useMemo, useState } from 'react'

import { AppShell } from '@/components/layout/app-shell'
import { authStorage, FITTRACK_USER_CHANGED_EVENT } from '@/lib/auth'
import { apiFetch } from '@/lib/http'
import type {
  AuthenticatedUser,
  TenantModelConnectorAdmin,
  TenantModelConnectorPreset,
  TenantModelProtocol,
  TenantSummary,
} from '@/types/fittrack'

type ConnectorFormState = {
  id: string | null
  displayName: string
  providerPreset: string
  protocol: TenantModelProtocol
  baseUrl: string
  modelId: string
  apiKey: string
  inputTokenPricePer1M: string
  outputTokenPricePer1M: string
  cacheReadTokenPricePer1M: string
  cacheWriteTokenPricePer1M: string
  isDefault: boolean
  isEnabled: boolean
}

const protocolOptions: TenantModelProtocol[] = ['OpenAICompatible', 'AzureOpenAI', 'Anthropic']

export default function ModelSettingsPage() {
  const queryClient = useQueryClient()
  const [currentUser, setCurrentUser] = useState<AuthenticatedUser | null>(null)
  const [selectedTenantId, setSelectedTenantId] = useState<string>('')
  const [status, setStatus] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)
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
  const presetsQuery = useQuery({
    queryKey: ['admin-model-connector-presets'],
    queryFn: () => apiFetch<TenantModelConnectorPreset[]>('/api/admin/model-connector-presets'),
    enabled: isAdmin,
  })
  const connectorsQuery = useQuery({
    queryKey: ['admin-tenant-connectors', selectedTenantId],
    queryFn: () => apiFetch<TenantModelConnectorAdmin[]>(`/api/admin/tenants/${selectedTenantId}/model-connectors`),
    enabled: isAdmin && selectedTenantId.length > 0,
  })

  const presets = presetsQuery.data ?? []
  const connectors = connectorsQuery.data ?? []
  const initialForm = useMemo(() => buildInitialForm(presets), [presets])
  const [form, setForm] = useState<ConnectorFormState>(initialForm)

  useEffect(() => {
    if (!selectedTenantId && tenantsQuery.data?.length) {
      setSelectedTenantId(tenantsQuery.data[0]!.id)
    }
  }, [selectedTenantId, tenantsQuery.data])

  useEffect(() => {
    if (!form.id) {
      setForm((current) => (current.providerPreset ? current : buildInitialForm(presets)))
    }
  }, [form.id, presets])

  if (!isAdmin) {
    return (
      <AppShell title="Models">
        <div className="rounded-[32px] border border-amber-300/20 bg-amber-300/10 p-6 text-amber-100">
          This page is only available to global admins.
        </div>
      </AppShell>
    )
  }

  return (
    <AppShell title="Models">
      <div className="grid gap-6 xl:grid-cols-[320px_minmax(0,1fr)]">
        <section className="rounded-[32px] border border-white/10 bg-slate-950/55 p-6">
          <p className="text-xs uppercase tracking-[0.35em] text-cyan-300">Tenant Scope</p>
          <h3 className="mt-3 text-xl font-semibold text-white">Choose a tenant</h3>
          <p className="mt-2 text-sm text-slate-400">Each tenant can have multiple connectors, one default connector, and tenant-scoped user preferences.</p>

          <label className="mt-5 block">
            <span className="mb-2 block text-xs uppercase tracking-[0.3em] text-slate-400">Tenant</span>
            <select
              value={selectedTenantId}
              onChange={(event) => {
                setSelectedTenantId(event.target.value)
                setStatus(null)
                setForm(buildInitialForm(presets))
              }}
              className="w-full rounded-2xl border border-white/10 bg-white/5 px-4 py-3 text-sm outline-none focus:border-cyan-300"
            >
              {(tenantsQuery.data ?? []).map((tenant) => (
                <option key={tenant.id} value={tenant.id}>
                  {tenant.name}
                </option>
              ))}
            </select>
          </label>

          <div className="mt-5 space-y-3">
            {connectors.map((connector) => (
              <button
                key={connector.id}
                type="button"
                onClick={() => {
                  setForm(toEditForm(connector))
                  setStatus(null)
                }}
                className={`w-full rounded-[24px] border px-4 py-4 text-left transition ${
                  form.id === connector.id
                    ? 'border-cyan-300/35 bg-cyan-300/10'
                    : 'border-white/10 bg-white/5 hover:border-white/20 hover:bg-white/10'
                }`}
              >
                <div className="flex items-start justify-between gap-3">
                  <div>
                    <p className="font-medium text-white">{connector.displayName}</p>
                    <p className="mt-1 text-xs text-slate-400">
                      {connector.protocol} · {connector.modelId}
                    </p>
                    <p className="mt-1 text-xs text-slate-500">{connector.id}</p>
                    <p className="mt-2 text-xs text-slate-500">
                      In ${formatPrice(connector.inputTokenPricePer1M)}/1M | Out ${formatPrice(connector.outputTokenPricePer1M)}/1M
                    </p>
                  </div>
                  <div className="text-right text-[11px] uppercase tracking-[0.25em] text-slate-400">
                    {connector.isDefault ? <p>Default</p> : null}
                    {connector.isCurrentUserActive ? <p className="text-cyan-200">Active For You</p> : null}
                    <p>{connector.isEnabled ? 'Enabled' : 'Disabled'}</p>
                  </div>
                </div>
              </button>
            ))}
            {selectedTenantId && !connectors.length && !connectorsQuery.isLoading ? (
              <div className="rounded-[24px] border border-dashed border-white/10 bg-white/5 px-4 py-5 text-sm text-slate-400">
                No connectors yet for this tenant.
              </div>
            ) : null}
          </div>
        </section>

        <section className="rounded-[32px] border border-white/10 bg-slate-950/55 p-6">
          <div className="flex flex-wrap items-start justify-between gap-3">
            <div>
              <p className="text-xs uppercase tracking-[0.35em] text-cyan-300">Connector Editor</p>
              <h3 className="mt-3 text-xl font-semibold text-white">{form.id ? 'Edit connector' : 'Create connector'}</h3>
              <p className="mt-2 text-sm text-slate-400">Use a preset to prefill common vendors, then adjust protocol, base URL, model ID, and API key for the target tenant.</p>
            </div>
            <button
              type="button"
              onClick={() => {
                setForm(buildInitialForm(presets))
                setStatus(null)
              }}
              className="rounded-full border border-white/10 px-4 py-2 text-xs uppercase tracking-[0.25em] text-slate-300 hover:bg-white/10"
            >
              New connector
            </button>
          </div>

          <form
            className="mt-6 grid gap-4 md:grid-cols-2"
            onSubmit={async (event) => {
              event.preventDefault()
              if (!selectedTenantId) return

              setSubmitting(true)
              setStatus(null)
              try {
                const payload = {
                  displayName: form.displayName.trim(),
                  providerPreset: form.providerPreset,
                  protocol: form.protocol,
                  baseUrl: form.baseUrl.trim(),
                  modelId: form.modelId.trim(),
                  apiKey: form.apiKey.trim() || null,
                  inputTokenPricePer1M: parseNullableNumber(form.inputTokenPricePer1M),
                  outputTokenPricePer1M: parseNullableNumber(form.outputTokenPricePer1M),
                  cacheReadTokenPricePer1M: parseNullableNumber(form.cacheReadTokenPricePer1M),
                  cacheWriteTokenPricePer1M: parseNullableNumber(form.cacheWriteTokenPricePer1M),
                  isDefault: form.isDefault,
                  isEnabled: form.isEnabled,
                }

                if (form.id) {
                  await apiFetch<TenantModelConnectorAdmin>(`/api/admin/tenants/${selectedTenantId}/model-connectors/${form.id}`, {
                    method: 'PUT',
                    body: JSON.stringify(payload),
                  })
                  setStatus('Connector updated.')
                } else {
                  await apiFetch<TenantModelConnectorAdmin>(`/api/admin/tenants/${selectedTenantId}/model-connectors`, {
                    method: 'POST',
                    body: JSON.stringify(payload),
                  })
                  setStatus('Connector created.')
                  setForm(buildInitialForm(presets))
                }

                await queryClient.invalidateQueries({ queryKey: ['admin-tenant-connectors', selectedTenantId] })
              } finally {
                setSubmitting(false)
              }
            }}
          >
            <label>
              <span className="mb-2 block text-xs uppercase tracking-[0.3em] text-slate-400">Preset</span>
              <select
                value={form.providerPreset}
                onChange={(event) => {
                  const preset = presets.find((item) => item.key === event.target.value)
                  if (!preset) return
                  setForm((current) => ({
                    ...current,
                    providerPreset: preset.key,
                    displayName: current.id ? current.displayName : preset.displayName,
                    protocol: preset.protocol,
                    baseUrl: preset.baseUrl,
                    modelId: preset.modelId,
                  }))
                }}
                className="w-full rounded-2xl border border-white/10 bg-white/5 px-4 py-3 text-sm outline-none focus:border-cyan-300"
              >
                {presets.map((preset) => (
                  <option key={preset.key} value={preset.key}>
                    {preset.displayName}
                  </option>
                ))}
              </select>
            </label>

            <label>
              <span className="mb-2 block text-xs uppercase tracking-[0.3em] text-slate-400">Protocol</span>
              <select
                value={form.protocol}
                onChange={(event) => setForm((current) => ({ ...current, protocol: event.target.value as TenantModelProtocol }))}
                className="w-full rounded-2xl border border-white/10 bg-white/5 px-4 py-3 text-sm outline-none focus:border-cyan-300"
              >
                {protocolOptions.map((protocol) => (
                  <option key={protocol} value={protocol}>
                    {protocol}
                  </option>
                ))}
              </select>
            </label>

            <label>
              <span className="mb-2 block text-xs uppercase tracking-[0.3em] text-slate-400">Display name</span>
              <input
                value={form.displayName}
                onChange={(event) => setForm((current) => ({ ...current, displayName: event.target.value }))}
                className="w-full rounded-2xl border border-white/10 bg-white/5 px-4 py-3 text-sm outline-none focus:border-cyan-300"
              />
            </label>

            <label>
              <span className="mb-2 block text-xs uppercase tracking-[0.3em] text-slate-400">Model ID</span>
              <input
                value={form.modelId}
                onChange={(event) => setForm((current) => ({ ...current, modelId: event.target.value }))}
                className="w-full rounded-2xl border border-white/10 bg-white/5 px-4 py-3 text-sm outline-none focus:border-cyan-300"
              />
            </label>

            <label className="md:col-span-2">
              <span className="mb-2 block text-xs uppercase tracking-[0.3em] text-slate-400">Base URL</span>
              <input
                value={form.baseUrl}
                onChange={(event) => setForm((current) => ({ ...current, baseUrl: event.target.value }))}
                className="w-full rounded-2xl border border-white/10 bg-white/5 px-4 py-3 text-sm outline-none focus:border-cyan-300"
              />
            </label>

            <label className="md:col-span-2">
              <span className="mb-2 block text-xs uppercase tracking-[0.3em] text-slate-400">API key</span>
              <input
                type="password"
                value={form.apiKey}
                onChange={(event) => setForm((current) => ({ ...current, apiKey: event.target.value }))}
                placeholder={form.id ? 'Leave empty to keep the existing secret' : 'Enter the connector secret'}
                className="w-full rounded-2xl border border-white/10 bg-white/5 px-4 py-3 text-sm outline-none focus:border-cyan-300"
              />
              <p className="mt-2 text-xs text-slate-500">
                Secrets are write-only. {form.id ? 'This connector already has a stored key.' : 'A key is required before the connector can be used.'}
              </p>
            </label>

            <label>
              <span className="mb-2 block text-xs uppercase tracking-[0.3em] text-slate-400">Input price / 1M</span>
              <input
                type="number"
                min="0"
                step="0.000001"
                value={form.inputTokenPricePer1M}
                onChange={(event) => setForm((current) => ({ ...current, inputTokenPricePer1M: event.target.value }))}
                placeholder="0.150000"
                className="w-full rounded-2xl border border-white/10 bg-white/5 px-4 py-3 text-sm outline-none focus:border-cyan-300"
              />
            </label>

            <label>
              <span className="mb-2 block text-xs uppercase tracking-[0.3em] text-slate-400">Output price / 1M</span>
              <input
                type="number"
                min="0"
                step="0.000001"
                value={form.outputTokenPricePer1M}
                onChange={(event) => setForm((current) => ({ ...current, outputTokenPricePer1M: event.target.value }))}
                placeholder="0.600000"
                className="w-full rounded-2xl border border-white/10 bg-white/5 px-4 py-3 text-sm outline-none focus:border-cyan-300"
              />
            </label>

            <label>
              <span className="mb-2 block text-xs uppercase tracking-[0.3em] text-slate-400">Cache read / 1M</span>
              <input
                type="number"
                min="0"
                step="0.000001"
                value={form.cacheReadTokenPricePer1M}
                onChange={(event) => setForm((current) => ({ ...current, cacheReadTokenPricePer1M: event.target.value }))}
                placeholder="Optional"
                className="w-full rounded-2xl border border-white/10 bg-white/5 px-4 py-3 text-sm outline-none focus:border-cyan-300"
              />
            </label>

            <label>
              <span className="mb-2 block text-xs uppercase tracking-[0.3em] text-slate-400">Cache write / 1M</span>
              <input
                type="number"
                min="0"
                step="0.000001"
                value={form.cacheWriteTokenPricePer1M}
                onChange={(event) => setForm((current) => ({ ...current, cacheWriteTokenPricePer1M: event.target.value }))}
                placeholder="Optional"
                className="w-full rounded-2xl border border-white/10 bg-white/5 px-4 py-3 text-sm outline-none focus:border-cyan-300"
              />
            </label>

            <label className="flex items-center gap-3 rounded-2xl border border-white/10 bg-white/5 px-4 py-3 text-sm text-slate-200">
              <input
                type="checkbox"
                checked={form.isEnabled}
                onChange={(event) => setForm((current) => ({ ...current, isEnabled: event.target.checked }))}
              />
              Enabled
            </label>

            <label className="flex items-center gap-3 rounded-2xl border border-white/10 bg-white/5 px-4 py-3 text-sm text-slate-200">
              <input
                type="checkbox"
                checked={form.isDefault}
                onChange={(event) => setForm((current) => ({ ...current, isDefault: event.target.checked }))}
              />
              Set as tenant default
            </label>

            {status ? (
              <div className="md:col-span-2 rounded-[24px] border border-emerald-300/20 bg-emerald-300/10 px-4 py-3 text-sm text-emerald-100">
                {status}
              </div>
            ) : null}

            <div className="md:col-span-2 flex flex-wrap gap-3">
              <button
                type="submit"
                disabled={submitting || !selectedTenantId}
                className="rounded-full bg-cyan-300 px-5 py-3 text-sm font-semibold uppercase tracking-[0.3em] text-slate-950 disabled:opacity-60"
              >
                {submitting ? 'Saving...' : form.id ? 'Update connector' : 'Create connector'}
              </button>

              {form.id ? (
                <>
                  <button
                    type="button"
                    disabled={submitting}
                    onClick={async () => {
                      if (!selectedTenantId || !form.id) return
                      setSubmitting(true)
                      setStatus(null)
                      try {
                        await apiFetch<TenantModelConnectorAdmin>(`/api/admin/tenants/${selectedTenantId}/model-connectors/${form.id}/default`, {
                          method: 'POST',
                        })
                        await queryClient.invalidateQueries({ queryKey: ['admin-tenant-connectors', selectedTenantId] })
                        setStatus('Tenant default updated.')
                      } finally {
                        setSubmitting(false)
                      }
                    }}
                    className="rounded-full border border-cyan-300/30 bg-cyan-300/10 px-4 py-3 text-xs uppercase tracking-[0.25em] text-cyan-100 disabled:opacity-60"
                  >
                    Make default
                  </button>
                  <button
                    type="button"
                    disabled={submitting}
                    onClick={async () => {
                      if (!selectedTenantId || !form.id) return
                      setSubmitting(true)
                      setStatus(null)
                      try {
                        await apiFetch(`/api/admin/tenants/${selectedTenantId}/model-connectors/${form.id}`, {
                          method: 'DELETE',
                        })
                        setForm(buildInitialForm(presets))
                        await queryClient.invalidateQueries({ queryKey: ['admin-tenant-connectors', selectedTenantId] })
                        setStatus('Connector deleted.')
                      } finally {
                        setSubmitting(false)
                      }
                    }}
                    className="rounded-full border border-rose-300/30 bg-rose-300/10 px-4 py-3 text-xs uppercase tracking-[0.25em] text-rose-100 disabled:opacity-60"
                  >
                    Delete connector
                  </button>
                </>
              ) : null}
            </div>
          </form>
        </section>
      </div>
    </AppShell>
  )
}

function buildInitialForm(presets: TenantModelConnectorPreset[]): ConnectorFormState {
  const preset = presets[0]

  return {
    id: null,
    displayName: preset?.displayName ?? '',
    providerPreset: preset?.key ?? '',
    protocol: preset?.protocol ?? 'OpenAICompatible',
    baseUrl: preset?.baseUrl ?? '',
    modelId: preset?.modelId ?? '',
    apiKey: '',
    inputTokenPricePer1M: '',
    outputTokenPricePer1M: '',
    cacheReadTokenPricePer1M: '',
    cacheWriteTokenPricePer1M: '',
    isDefault: false,
    isEnabled: true,
  }
}

function toEditForm(connector: TenantModelConnectorAdmin): ConnectorFormState {
  return {
    id: connector.id,
    displayName: connector.displayName,
    providerPreset: connector.providerPreset,
    protocol: connector.protocol,
    baseUrl: connector.baseUrl,
    modelId: connector.modelId,
    apiKey: '',
    inputTokenPricePer1M: toNumberInput(connector.inputTokenPricePer1M),
    outputTokenPricePer1M: toNumberInput(connector.outputTokenPricePer1M),
    cacheReadTokenPricePer1M: toNumberInput(connector.cacheReadTokenPricePer1M),
    cacheWriteTokenPricePer1M: toNumberInput(connector.cacheWriteTokenPricePer1M),
    isDefault: connector.isDefault,
    isEnabled: connector.isEnabled,
  }
}

function parseNullableNumber(value: string) {
  if (!value.trim()) {
    return null
  }

  const parsed = Number(value)
  return Number.isFinite(parsed) ? parsed : null
}

function toNumberInput(value?: number | null) {
  return value === null || value === undefined ? '' : String(value)
}

function formatPrice(value?: number | null) {
  return value === null || value === undefined ? '--' : value.toFixed(6)
}
