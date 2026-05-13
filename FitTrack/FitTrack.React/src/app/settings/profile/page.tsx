'use client'

import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'

import { AppShell } from '@/components/layout/app-shell'
import { useLanguage } from '@/components/providers/language-provider'
import { apiFetch, getCurrentUser } from '@/lib/http'
import type { TenantModelConnectorOption, UserProfile } from '@/types/fittrack'

export default function ProfileSettingsPage() {
  const { language } = useLanguage()
  const isChinese = language === 'zh-CN'
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
    <AppShell title="Profile" titleKey="profile">
      <div className="rounded-[32px] border border-white/10 bg-slate-950/50 p-6">
        <p className="text-sm text-slate-400">
          {isChinese ? '这些基础信息会进入多轮教练上下文，影响饮食、训练和进度复盘建议。' : 'Your baseline stats guide the multi-turn coaching context for food, training, and progress reviews.'}
        </p>
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
          <Field name="displayName" label={isChinese ? '显示名称' : 'Display name'} defaultValue={profile?.displayName ?? ''} />
          <Field name="sex" label={isChinese ? '性别' : 'Sex'} defaultValue={profile?.sex ?? ''} />
          <Field name="age" label={isChinese ? '年龄' : 'Age'} defaultValue={profile?.age?.toString() ?? ''} />
          <Field name="heightCm" label={isChinese ? '身高（cm）' : 'Height (cm)'} defaultValue={profile?.heightCm?.toString() ?? ''} />
          <Field name="weightKg" label={isChinese ? '体重（kg）' : 'Weight (kg)'} defaultValue={profile?.weightKg?.toString() ?? ''} />
          <Field name="bodyFatPercent" label={isChinese ? '体脂（%）' : 'Body fat (%)'} defaultValue={profile?.bodyFatPercent?.toString() ?? ''} />
          <Field name="activityLevel" label={isChinese ? '活动水平' : 'Activity level'} defaultValue={profile?.activityLevel ?? ''} />
          <Field name="goal" label={isChinese ? '目标' : 'Goal'} defaultValue={profile?.goal ?? ''} />
          <SelectField
            name="preferredModelConnectorId"
            label={isChinese ? '偏好模型' : 'Preferred model'}
            defaultValue={profile?.preferredModelConnectorId ?? ''}
            options={connectors.map((connector) => ({
              value: connector.id,
              label: connector.isDefault
                ? `${connector.displayName} ${isChinese ? '（租户默认）' : '(Tenant default)'}`
                : connector.displayName,
            }))}
            emptyLabel={isChinese ? '使用租户默认' : 'Use tenant default'}
          />
          <label className="md:col-span-2">
            <span className="mb-2 block text-xs uppercase tracking-[0.3em] text-slate-400">{isChinese ? '偏好说明' : 'Preferences'}</span>
            <textarea
              name="preferences"
              defaultValue={profile?.preferences ?? ''}
              rows={4}
              className="w-full rounded-2xl border border-white/10 bg-white/5 px-4 py-3 text-sm outline-none focus:border-cyan-300"
            />
          </label>
          <div className="md:col-span-2 rounded-[24px] border border-white/10 bg-white/5 px-4 py-3 text-sm text-slate-400">
            {loading
              ? isChinese
                ? '正在加载可用租户模型...'
                : 'Loading available tenant models...'
              : isChinese
                ? '选择一个当前租户允许使用的模型连接器，或留空以跟随租户默认配置。'
                : 'Choose a tenant-approved model connector or leave this empty to follow the tenant default.'}
          </div>
          <button
            disabled={saving || loading}
            className="md:col-span-2 mt-2 rounded-full bg-cyan-300 px-5 py-3 text-sm font-semibold uppercase tracking-[0.3em] text-slate-950 disabled:opacity-60"
          >
            {saving ? (isChinese ? '保存中...' : 'Saving...') : isChinese ? '保存档案' : 'Save profile'}
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
  emptyLabel,
}: {
  name: string
  label: string
  defaultValue: string
  options: ReadonlyArray<{ value: string; label: string }>
  emptyLabel: string
}) {
  return (
    <label>
      <span className="mb-2 block text-xs uppercase tracking-[0.3em] text-slate-400">{label}</span>
      <select
        name={name}
        defaultValue={defaultValue}
        className="w-full rounded-2xl border border-white/10 bg-white/5 px-4 py-3 text-sm outline-none focus:border-cyan-300"
      >
        <option value="">{emptyLabel}</option>
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
