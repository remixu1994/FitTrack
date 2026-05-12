'use client'

import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'

import { AppShell } from '@/components/layout/app-shell'
import { apiFetch, getCurrentUser } from '@/lib/http'
import type { TenantModelConnectorOption, UserProfile } from '@/types/fittrack'

export default function ProfileSettingsPage() {
  const profileQuery = useQuery({
    queryKey: ['profile'],
    queryFn: () => apiFetch<UserProfile>('/api/profile'),
  })
  const connectorQuery = useQuery({
    queryKey: ['model-connectors'],
    queryFn: () => apiFetch<TenantModelConnectorOption[]>('/api/model-connectors'),
  })

  const [saving, setSaving] = useState(false)
  const profile = profileQuery.data
  const connectors = connectorQuery.data ?? []
  const loading = profileQuery.isLoading || connectorQuery.isLoading

  return (
    <AppShell title="Profile">
      <div className="rounded-[32px] border border-white/10 bg-slate-950/50 p-6">
        <p className="text-sm text-slate-400">Your baseline stats guide the multi-turn coaching context for food, training, and progress reviews.</p>
        <form
          key={profile?.updatedAt ?? 'profile-loading'}
          className="mt-6 grid gap-4 md:grid-cols-2"
          onSubmit={async (event) => {
            event.preventDefault()
            const formData = new FormData(event.currentTarget)
            setSaving(true)
            try {
              await apiFetch<UserProfile>('/api/profile', {
                method: 'PUT',
                body: JSON.stringify({
                  displayName: formData.get('displayName') || null,
                  sex: formData.get('sex') || null,
                  age: toNullableNumber(formData.get('age')),
                  heightCm: toNullableNumber(formData.get('heightCm')),
                  weightKg: toNullableNumber(formData.get('weightKg')),
                  bodyFatPercent: toNullableNumber(formData.get('bodyFatPercent')),
                  activityLevel: formData.get('activityLevel') || null,
                  goal: formData.get('goal') || null,
                  preferences: formData.get('preferences') || null,
                  preferredModelConnectorId: toNullableString(formData.get('preferredModelConnectorId')),
                }),
              })
              await getCurrentUser()
              await Promise.all([profileQuery.refetch(), connectorQuery.refetch()])
            } finally {
              setSaving(false)
            }
          }}
        >
          <Field name="displayName" label="Display name" defaultValue={profile?.displayName ?? ''} />
          <Field name="sex" label="Sex" defaultValue={profile?.sex ?? ''} />
          <Field name="age" label="Age" defaultValue={profile?.age?.toString() ?? ''} />
          <Field name="heightCm" label="Height (cm)" defaultValue={profile?.heightCm?.toString() ?? ''} />
          <Field name="weightKg" label="Weight (kg)" defaultValue={profile?.weightKg?.toString() ?? ''} />
          <Field name="bodyFatPercent" label="Body fat (%)" defaultValue={profile?.bodyFatPercent?.toString() ?? ''} />
          <Field name="activityLevel" label="Activity level" defaultValue={profile?.activityLevel ?? ''} />
          <Field name="goal" label="Goal" defaultValue={profile?.goal ?? ''} />
          <SelectField
            name="preferredModelConnectorId"
            label="Preferred model"
            defaultValue={profile?.preferredModelConnectorId ?? ''}
            options={connectors.map((connector) => ({
              value: connector.id,
              label: connector.isDefault ? `${connector.displayName} (Tenant default)` : connector.displayName,
            }))}
          />
          <label className="md:col-span-2">
            <span className="mb-2 block text-xs uppercase tracking-[0.3em] text-slate-400">Preferences</span>
            <textarea
              name="preferences"
              defaultValue={profile?.preferences ?? ''}
              rows={4}
              className="w-full rounded-2xl border border-white/10 bg-white/5 px-4 py-3 text-sm outline-none focus:border-cyan-300"
            />
          </label>
          <div className="md:col-span-2 rounded-[24px] border border-white/10 bg-white/5 px-4 py-3 text-sm text-slate-400">
            {loading ? 'Loading available tenant models...' : 'Choose a tenant-approved model connector or leave this empty to follow the tenant default.'}
          </div>
          <button
            disabled={saving || loading}
            className="md:col-span-2 mt-2 rounded-full bg-cyan-300 px-5 py-3 text-sm font-semibold uppercase tracking-[0.3em] text-slate-950 disabled:opacity-60"
          >
            {saving ? 'Saving...' : 'Save profile'}
          </button>
        </form>
      </div>
    </AppShell>
  )
}

function Field({ name, label, defaultValue }: { name: string; label: string; defaultValue: string }) {
  return (
    <label>
      <span className="mb-2 block text-xs uppercase tracking-[0.3em] text-slate-400">{label}</span>
      <input name={name} defaultValue={defaultValue} className="w-full rounded-2xl border border-white/10 bg-white/5 px-4 py-3 text-sm outline-none focus:border-cyan-300" />
    </label>
  )
}

function SelectField({
  name,
  label,
  defaultValue,
  options,
}: {
  name: string
  label: string
  defaultValue: string
  options: ReadonlyArray<{ value: string; label: string }>
}) {
  return (
    <label>
      <span className="mb-2 block text-xs uppercase tracking-[0.3em] text-slate-400">{label}</span>
      <select
        name={name}
        defaultValue={defaultValue}
        className="w-full rounded-2xl border border-white/10 bg-white/5 px-4 py-3 text-sm outline-none focus:border-cyan-300"
      >
        <option value="">Use tenant default</option>
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
    </label>
  )
}

function toNullableNumber(value: FormDataEntryValue | null) {
  if (!value) return null
  const num = Number(value)
  return Number.isFinite(num) ? num : null
}

function toNullableString(value: FormDataEntryValue | null) {
  if (typeof value !== 'string') return null
  const trimmed = value.trim()
  return trimmed.length > 0 ? trimmed : null
}
