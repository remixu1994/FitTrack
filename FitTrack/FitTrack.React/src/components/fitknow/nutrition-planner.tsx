'use client'

import Link from 'next/link'
import { useCallback, useEffect, useMemo, useState, useId } from 'react'

import { AppShell } from '@/components/layout/app-shell'
import { useLanguage } from '@/components/providers/language-provider'
import {
  computeFitKnowNutrition,
  computeFoodMacro,
  computeMealTargets,
  defaultFitKnowProfiles,
  findDefaultFood,
  findFoodByQuery,
  foodInputUnit,
  foodOptionLabel,
  isNoStrengthPlan,
  reverseFoodAmount,
  suggestedFoodAmount,
  suggestedMacroAmount,
  targetCaloriesForTable,
} from '@/lib/fitknow/calculate'
import {
  fitKnowFoods,
  fitKnowMatches,
  getFitKnowDietPlans,
  getFitKnowGoalCategory,
  getFitKnowGoalLabel,
  getRelatedFitKnowQa,
} from '@/lib/fitknow/data'
import type {
  FitKnowDietPlan,
  FitKnowFood,
  FitKnowGoal,
  FitKnowMeal,
  FitKnowMealTable,
  FitKnowMealTargets,
  FitKnowNutritionMetrics,
  FitKnowPlannerProfile,
  FitKnowQa,
} from '@/types/fitknow'

type NutritionPlannerProps = {
  goal: FitKnowGoal
}

type FoodEntry = {
  id: string
  foodId: string
  foodQuery: string
  amount: string
  macro: string
}

export function NutritionPlanner({ goal }: NutritionPlannerProps) {
  const { language } = useLanguage()
  const isChinese = language === 'zh-CN'
  const plans = useMemo(() => getFitKnowDietPlans(goal), [goal])
  const [query, setQuery] = useState('')
  const [fatLossTrainingMode, setFatLossTrainingMode] = useState<'strength' | 'no-strength'>('strength')
  const strengthPlans = useMemo(() => plans.filter((plan) => !isNoStrengthPlan(plan)), [plans])
  const noStrengthPlan = useMemo(() => plans.find((plan) => isNoStrengthPlan(plan)), [plans])
  const selectablePlans = goal === 'fat-loss' && fatLossTrainingMode === 'no-strength'
    ? noStrengthPlan
      ? [noStrengthPlan]
      : []
    : strengthPlans.length
      ? strengthPlans
      : plans
  const [selectedId, setSelectedId] = useState(selectablePlans[0]?.id ?? plans[0]?.id ?? '')
  const selectedPlan = selectablePlans.find((plan) => plan.id === selectedId) ?? selectablePlans[0] ?? plans[0]
  const [profile, setProfile] = usePlannerProfile(goal)
  const metrics = useMemo(() => computeFitKnowNutrition(goal, profile, selectedPlan), [goal, profile, selectedPlan])
  const relatedQa = useMemo(() => getRelatedFitKnowQa(goal, query).slice(0, 10), [goal, query])
  const category = getFitKnowGoalCategory(goal)
  const title = getFitKnowGoalLabel(goal, isChinese)

  return (
    <AppShell title={title}>
      <div className="space-y-6">
        <section className="rounded-[32px] border border-cyan-300/20 bg-slate-950/60 p-5 shadow-2xl shadow-cyan-950/10 lg:p-6">
          <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
            <div className="max-w-4xl">
              <p className="text-xs uppercase tracking-[0.34em] text-cyan-300">FitKnow</p>
              <h3 className="mt-3 text-2xl font-semibold text-white">{title}</h3>
              <p className="mt-3 text-sm leading-7 text-slate-300">
                {isChinese
                  ? `从 ${plans.length} 条${category}饮食路径中选择训练时间，结合身高、体重和运动消耗计算每日热量、碳水和蛋白质目标。`
                  : `Choose from ${plans.length} ${goal === 'fat-loss' ? 'fat-loss' : 'muscle-gain'} paths, then calculate calories, carbs, and protein from your own inputs.`}
              </p>
            </div>
            <div className="flex flex-col gap-3 sm:flex-row">
              <Link href="/nutrition/fat-loss" className={goal === 'fat-loss' ? primaryTabClass : secondaryTabClass}>
                {isChinese ? '减脂' : 'Fat loss'}
              </Link>
              <Link href="/nutrition/muscle-gain" className={goal === 'muscle-gain' ? primaryTabClass : secondaryTabClass}>
                {isChinese ? '增肌' : 'Muscle gain'}
              </Link>
            </div>
          </div>

          <div className="mt-5 grid gap-3 lg:grid-cols-[minmax(0,1fr)_320px]">
            <div className="flex flex-wrap gap-2">
              {goal === 'fat-loss' ? (
                <SegmentedControl
                  items={[
                    { id: 'strength', label: isChinese ? '有力量训练' : 'Strength days' },
                    { id: 'no-strength', label: isChinese ? '无力量训练' : 'No strength training' },
                  ]}
                  selectedId={fatLossTrainingMode}
                  onSelect={(id) => setFatLossTrainingMode(id as 'strength' | 'no-strength')}
                />
              ) : null}
              <SegmentedControl
                items={selectablePlans.map((plan) => ({ id: plan.id, label: plan.timing }))}
                selectedId={selectedPlan.id}
                onSelect={setSelectedId}
                compact
              />
            </div>
            <label className="block">
              <span className="sr-only">{isChinese ? '搜索餐次、食物或问答' : 'Search meals, foods, or questions'}</span>
              <input
                value={query}
                onChange={(event) => setQuery(event.target.value)}
                placeholder={isChinese ? '搜索餐次 / 食物 / 问答' : 'Search meals / foods / QA'}
                className="h-12 w-full rounded-2xl border border-white/10 bg-white/5 px-4 text-sm text-white outline-none placeholder:text-slate-500 focus:border-cyan-300"
              />
            </label>
          </div>
        </section>

        <section className="grid gap-6 xl:grid-cols-[minmax(0,1.15fr)_minmax(360px,0.85fr)]">
          <div className="space-y-6">
            <PlannerForm profile={profile} setProfile={setProfile} metrics={metrics} goal={goal} plan={selectedPlan} />
            <StructuredMealTables plan={selectedPlan} metrics={metrics} query={query} />
          </div>

          <aside className="space-y-6">
            <ExecutionNotes plan={selectedPlan} query={query} />
            <QaPanel items={relatedQa} category={category} isChinese={isChinese} />
          </aside>
        </section>
      </div>
    </AppShell>
  )
}

export function NutritionLanding() {
  const { language } = useLanguage()
  const isChinese = language === 'zh-CN'
  const fatLossCount = getFitKnowDietPlans('fat-loss').length
  const muscleGainCount = getFitKnowDietPlans('muscle-gain').length

  return (
    <AppShell title={isChinese ? '营养规划' : 'Nutrition Planning'}>
      <div className="grid gap-5 xl:grid-cols-2">
        <GoalCard
          href="/nutrition/fat-loss"
          title={isChinese ? '减脂饮食路径' : 'Fat-loss nutrition paths'}
          count={fatLossCount}
          description={isChinese ? '按是否力量训练、训练时间和个人数据拆解热量、碳水、蛋白质与每餐食物重量。' : 'Plan calories, carb/protein quotas, and meal portions by training mode and timing.'}
          accent="cyan"
        />
        <GoalCard
          href="/nutrition/muscle-gain"
          title={isChinese ? '增肌饮食路径' : 'Muscle-gain nutrition paths'}
          count={muscleGainCount}
          description={isChinese ? '围绕训练日和休息日安排碳水、蛋白质和餐次，辅助稳定增肌。' : 'Structure training-day and rest-day carbs, protein, and meals for steady gaining.'}
          accent="emerald"
        />
      </div>
    </AppShell>
  )
}

function usePlannerProfile(goal: FitKnowGoal) {
  const storageKey = `fittrack-fitknow-${goal}-profile`
  const [profile, setProfile] = useState<FitKnowPlannerProfile>(() => {
    if (typeof window === 'undefined') return defaultFitKnowProfiles[goal]
    try {
      const stored = window.localStorage.getItem(storageKey)
      return { ...defaultFitKnowProfiles[goal], ...(stored ? JSON.parse(stored) : {}) }
    } catch {
      return defaultFitKnowProfiles[goal]
    }
  })

  useEffect(() => {
    window.localStorage.setItem(storageKey, JSON.stringify(profile))
  }, [profile, storageKey])

  return [profile, setProfile] as const
}

function PlannerForm({
  profile,
  setProfile,
  metrics,
  goal,
  plan,
}: {
  profile: FitKnowPlannerProfile
  setProfile: React.Dispatch<React.SetStateAction<FitKnowPlannerProfile>>
  metrics: FitKnowNutritionMetrics
  goal: FitKnowGoal
  plan: FitKnowDietPlan
}) {
  const { language } = useLanguage()
  const isChinese = language === 'zh-CN'
  const noStrength = goal === 'fat-loss' && isNoStrengthPlan(plan)
  const update = (field: keyof FitKnowPlannerProfile) => (value: string) => {
    setProfile((current) => ({ ...current, [field]: value }))
  }

  return (
    <section className="rounded-[32px] border border-white/10 bg-slate-950/50 p-5 lg:p-6">
      <div className="flex flex-col gap-4 xl:flex-row xl:items-start xl:justify-between">
        <div>
          <p className="text-xs uppercase tracking-[0.32em] text-cyan-300">{isChinese ? '自己输入' : 'Your inputs'}</p>
          <h3 className="mt-3 text-xl font-semibold text-white">{isChinese ? '目标计算器' : 'Goal calculator'}</h3>
          <p className="mt-2 max-w-3xl text-sm leading-6 text-slate-400">
            {isChinese ? '先估算基础代谢和无运动总消耗，再结合当前方案拆成训练日、休息日或每日目标。' : 'Estimate metabolism and activity, then split targets into training, rest, or daily plans.'}
          </p>
        </div>
        <div className="rounded-2xl border border-cyan-300/20 bg-cyan-300/10 px-4 py-3 text-sm text-cyan-100">
          {isChinese ? '当前路径：' : 'Current path: '}
          <span className="font-semibold">{plan.timing}</span>
        </div>
      </div>

      <div className="mt-5 grid gap-3 md:grid-cols-2 xl:grid-cols-3">
        <SelectField label={isChinese ? '性别' : 'Sex'} value={profile.sex} onChange={update('sex')} options={[{ value: 'male', label: isChinese ? '男' : 'Male' }, { value: 'female', label: isChinese ? '女' : 'Female' }]} />
        <InputField label={isChinese ? '身高 cm' : 'Height cm'} value={profile.height} onChange={update('height')} />
        <InputField label={isChinese ? '体重 kg' : 'Weight kg'} value={profile.weight} onChange={update('weight')} />
        <InputField label={isChinese ? '年龄' : 'Age'} value={profile.age} onChange={update('age')} />
        {!noStrength ? <InputField label={isChinese ? '力训消耗 kcal/天' : 'Strength kcal/day'} value={profile.strengthCalories} onChange={update('strengthCalories')} /> : null}
        <InputField label={isChinese ? '有氧消耗 kcal/天' : 'Cardio kcal/day'} value={profile.cardioCalories} onChange={update('cardioCalories')} />
      </div>

      <div className="mt-5 grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
        <MetricTile label={`BMI · ${metrics.bmiLabel}`} value={metrics.bmiText} />
        <MetricTile label={isChinese ? '基础代谢' : 'BMR'} value={`${metrics.bmr} kcal`} />
        <MetricTile label={isChinese ? '无运动总消耗' : 'Resting expenditure'} value={`${metrics.restingExpenditure} kcal`} />
        <MetricTile label={noStrength ? (isChinese ? '每日应吃热量' : 'Daily calories') : (isChinese ? '力训日应吃热量' : 'Training calories')} value={`${metrics.primaryTargetCalories} kcal`} />
      </div>

      <DietSummary metrics={metrics} plan={plan} />

      <div className="mt-5 rounded-[24px] border border-white/10 bg-white/5 p-4 text-sm leading-6 text-slate-300">
        <p className="font-semibold text-white">{isChinese ? '执行建议' : 'Guidance'}</p>
        <p className="mt-2">{metrics.advice}</p>
        <p className="mt-2">{metrics.targetWeightText}</p>
      </div>
    </section>
  )
}

function DietSummary({ metrics, plan }: { metrics: FitKnowNutritionMetrics; plan: FitKnowDietPlan }) {
  const { language } = useLanguage()
  const isChinese = language === 'zh-CN'
  const isFatLoss = metrics.goal === 'fat-loss'
  const noStrength = isNoStrengthPlan(plan)
  const quota = metrics.quotaMatch ?? {
    trainingCarb: metrics.carbsRate,
    restCarb: metrics.restCarbsRate,
    dailyCarb: undefined,
    protein: metrics.proteinRate,
    noStrength: false,
    key: '',
    sex: metrics.sex,
    matchedHeight: metrics.height,
    matchedWeight: metrics.weight,
    isExact: true,
  }

  return (
    <div className="mt-5 rounded-[24px] border border-white/10 bg-slate-900/70 p-4">
      <div className="grid gap-3 md:grid-cols-2">
        <MetricTile label={isChinese ? '力训日平衡热量' : 'Training maintenance'} value={`${metrics.trainingMaintenanceCalories ?? metrics.maintenanceCalories ?? '-'} kcal`} />
        <MetricTile label={isChinese ? '休息日平衡热量' : 'Rest maintenance'} value={`${metrics.restMaintenanceCalories ?? metrics.maintenanceCalories ?? '-'} kcal`} />
      </div>
      <div className="mt-4 grid gap-3 md:grid-cols-3">
        {noStrength ? (
          <>
            <MetricTile label={isChinese ? '每日碳水' : 'Daily carbs'} value={quota.dailyCarb ? `${Math.round(quota.dailyCarb * metrics.weight)} g` : '-'} />
            <MetricTile label={isChinese ? '每日蛋白质' : 'Daily protein'} value={quota.protein ? `${Math.round(quota.protein * metrics.weight)} g` : '-'} />
          </>
        ) : (
          <>
            <MetricTile label={isChinese ? '训练日碳水' : 'Training carbs'} value={quota.trainingCarb ? `${Math.round(quota.trainingCarb * metrics.weight)} g` : `${metrics.trainingCarbs ?? '-'} g`} />
            <MetricTile label={isChinese ? '休息日碳水' : 'Rest carbs'} value={quota.restCarb ? `${Math.round(quota.restCarb * metrics.weight)} g` : `${metrics.restCarbs ?? '-'} g`} />
            <MetricTile label={isChinese ? '每日蛋白质' : 'Daily protein'} value={quota.protein ? `${Math.round(quota.protein * metrics.weight)} g` : `${metrics.trainingProtein ?? '-'} g`} />
          </>
        )}
      </div>
      <p className="mt-3 text-xs leading-5 text-slate-500">
        {isFatLoss && metrics.quotaMatch
          ? isChinese
            ? `配额采用 ${metrics.quotaMatch.matchedHeight}cm / ${metrics.quotaMatch.matchedWeight}kg 档，单位为 g/kg 体重。`
            : `Quota uses the ${metrics.quotaMatch.matchedHeight}cm / ${metrics.quotaMatch.matchedWeight}kg row, in g/kg body weight.`
          : isChinese
            ? '增肌配额来自身高体重比例表，并按当前体重换算到每日目标克数。'
            : 'Muscle-gain quotas come from the height/weight ratio table and are converted to daily grams.'}
      </p>
    </div>
  )
}

function StructuredMealTables({ plan, metrics, query }: { plan: FitKnowDietPlan; metrics: FitKnowNutritionMetrics; query: string }) {
  const tables = plan.mealTables ?? []
  if (!tables.length) {
    return <LegacyMealRows plan={plan} query={query} />
  }

  return (
    <div className="space-y-6">
      {tables.map((table) => {
        const meals = table.meals.filter((meal) => fitKnowMatches({ table: table.title, meal }, query))
        if (!meals.length) return null
        const targets = meals.length ? computeMealTargets(metrics, table, meals[0]) : null
        const calories = targetCaloriesForTable(metrics, table.dayType)
        return (
          <section key={table.dayType} className="rounded-[32px] border border-white/10 bg-slate-950/50 p-5 lg:p-6">
            <div className="flex flex-col gap-4 xl:flex-row xl:items-start xl:justify-between">
              <div>
                <p className="text-xs uppercase tracking-[0.32em] text-emerald-300">{dayLabel(table.dayType)}</p>
                <h3 className="mt-3 text-xl font-semibold text-white">{table.title}</h3>
                <p className="mt-2 max-w-3xl text-sm leading-6 text-slate-400">按全天配额拆到每餐，再用食物营养率换算应该吃多少。</p>
              </div>
              <div className="grid grid-cols-3 gap-2 text-center">
                <MetricTile label="热量" value={calories ? `${calories} kcal` : '-'} />
                <MetricTile label="碳水" value={targets ? `${Math.round(targets.carbTotal)} g` : '-'} />
                <MetricTile label="蛋白质" value={targets ? `${Math.round(targets.proteinTotal)} g` : '-'} />
              </div>
            </div>

            <div className="mt-5 grid gap-4">
              {meals.map((meal, index) => (
                <MealCard key={`${table.dayType}-${meal.name}`} table={table} meal={meal} metrics={metrics} defaultOpen={index === 0} />
              ))}
            </div>
            <MealGuidance meals={meals} />
          </section>
        )
      })}
    </div>
  )
}

function MealCard({ table, meal, metrics, defaultOpen }: { table: FitKnowMealTable; meal: FitKnowMeal; metrics: FitKnowNutritionMetrics; defaultOpen: boolean }) {
  const [open, setOpen] = useState(defaultOpen)
  const targets = computeMealTargets(metrics, table, meal)

  return (
    <details open={open} onToggle={(event) => setOpen(event.currentTarget.open)} className="rounded-[24px] border border-white/10 bg-white/[0.04] p-4">
      <summary className="flex cursor-pointer items-start gap-3 text-left marker:content-['']">
        <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-2xl bg-cyan-300/15 text-sm font-semibold text-cyan-100">{meal.order}</span>
        <div className="min-w-0">
          <h4 className="font-semibold text-white">{meal.name.replace(/^[①②③④⑤]/, '')}</h4>
          <p className="mt-1 text-sm text-slate-400">{meal.mealTypeLabel} · 碳水 {meal.carbPercent}% · 蛋白质 {meal.proteinPercent}%</p>
        </div>
      </summary>
      {open ? (
        <div className="mt-4 grid gap-4 xl:grid-cols-2">
          <MealFoodCell macroType="碳水" target={targets?.carb} options={meal.carbOptions} />
          <MealFoodCell macroType="蛋白质" target={targets?.protein} options={meal.proteinOptions} disabled={meal.proteinPercent === 0} />
        </div>
      ) : null}
    </details>
  )
}

function MealFoodCell({ macroType, target, options, disabled = false }: { macroType: string; target?: number; options: string[]; disabled?: boolean }) {
  const listId = useId()
  const foods = useMemo(() => fitKnowFoods.filter((food) => food.macroType === macroType), [macroType])
  const defaultFood = useMemo(() => findDefaultFood(options, macroType, foods), [foods, macroType, options])
  const makeEntry = useCallback((food: FitKnowFood | undefined = defaultFood ?? foods[0]): FoodEntry => ({
    id: makeEntryId(),
    foodId: food?.id ?? '',
    foodQuery: food ? foodOptionLabel(food) : '',
    amount: suggestedFoodAmount(target, food),
    macro: typeof target === 'number' ? String(Math.round(target)) : suggestedMacroAmount(suggestedFoodAmount(target, food), food),
  }), [defaultFood, foods, target])
  const [entries, setEntries] = useState<FoodEntry[]>(() => [makeEntry()])

  useEffect(() => {
    setEntries([makeEntry()])
  }, [makeEntry])

  const updateEntry = (id: string, patch: Partial<FoodEntry>) => {
    setEntries((current) => current.map((entry) => (entry.id === id ? { ...entry, ...patch } : entry)))
  }
  const addEntry = () => setEntries((current) => [...current, { id: makeEntryId(), foodId: '', foodQuery: '', amount: '', macro: '' }])
  const removeEntry = (id: string) => setEntries((current) => (current.length > 1 ? current.filter((entry) => entry.id !== id) : current))
  const applyFoodQuery = (entry: FoodEntry, value: string, fuzzy = false) => {
    const food = findFoodByQuery(value, foods, fuzzy)
    if (!food) {
      updateEntry(entry.id, { foodId: '', foodQuery: value, macro: '' })
      return
    }

    const macroValue = Number.parseFloat(entry.macro)
    const amount = Number.isFinite(macroValue) && macroValue > 0 ? reverseFoodAmount(entry.macro, food) : entry.amount
    updateEntry(entry.id, {
      foodId: food.id,
      foodQuery: foodOptionLabel(food),
      amount,
      macro: suggestedMacroAmount(amount, food),
    })
  }
  const actualTotal = entries.reduce((sum, entry) => {
    const food = foods.find((item) => item.id === entry.foodId)
    return sum + computeFoodMacro(entry.amount, food)
  }, 0)
  const hasTarget = Number.isFinite(target)
  const gap = hasTarget ? Number(target) - actualTotal : null
  const gapText = !hasTarget ? '暂无配额' : Math.abs(Number(gap)) < 0.5 ? '已达标' : Number(gap) > 0 ? `还差 ${Math.round(Number(gap))} g` : `超出 ${Math.round(Math.abs(Number(gap)))} g`

  return (
    <div className="rounded-[22px] border border-white/10 bg-slate-950/55 p-4">
      <div className="flex flex-wrap items-start justify-between gap-2">
        <div>
          <p className="text-xs uppercase tracking-[0.26em] text-slate-400">{macroType}</p>
          <p className="mt-1 text-lg font-semibold text-white">{disabled ? '不用吃' : hasTarget ? `目标 ${Math.round(Number(target))} g` : '暂无配额'}</p>
        </div>
        {!disabled ? (
          <div className="text-right text-sm">
            <p className="font-semibold text-cyan-100">{Math.round(actualTotal)} g</p>
            <p className={gap != null && gap < -0.5 ? 'text-rose-300' : 'text-slate-400'}>{gapText}</p>
          </div>
        ) : null}
      </div>

      {!disabled ? (
        <>
          <datalist id={listId}>
            {foods.map((food) => (
              <option value={foodOptionLabel(food)} key={food.id} />
            ))}
          </datalist>
          <div className="mt-4 space-y-3">
            {entries.map((entry) => {
              const food = foods.find((item) => item.id === entry.foodId)
              const actual = computeFoodMacro(entry.amount, food)
              return (
                <div key={entry.id} className="grid gap-2 rounded-2xl border border-white/10 bg-white/[0.03] p-3 lg:grid-cols-[minmax(0,1.2fr)_120px_120px_60px_36px]">
                  <input
                    list={listId}
                    value={entry.foodQuery}
                    onChange={(event) => applyFoodQuery(entry, event.target.value)}
                    onBlur={(event) => applyFoodQuery(entry, event.target.value, true)}
                    placeholder="搜索食物"
                    className={inputClass}
                  />
                  <LabeledInlineInput label={food ? foodInputUnit(food) : '-'} value={entry.amount} onChange={(value) => updateEntry(entry.id, { amount: value, macro: suggestedMacroAmount(value, food) })} />
                  <LabeledInlineInput label="g" value={entry.macro} onChange={(value) => updateEntry(entry.id, { macro: value, amount: reverseFoodAmount(value, food) })} />
                  <span className="self-center text-sm text-slate-300">{Math.round(actual)}g</span>
                  <button type="button" onClick={() => removeEntry(entry.id)} className="h-10 rounded-xl border border-white/10 text-slate-300 hover:bg-white/10" aria-label="移除食物">
                    ×
                  </button>
                </div>
              )
            })}
          </div>
          <button type="button" onClick={addEntry} className="mt-3 rounded-full border border-white/10 px-4 py-2 text-xs uppercase tracking-[0.22em] text-slate-200 hover:bg-white/10">
            添加食物
          </button>
        </>
      ) : null}
      {options.length ? <p className="mt-3 text-xs leading-5 text-slate-500">{options.slice(0, 2).join('；')}</p> : null}
    </div>
  )
}

function MealGuidance({ meals }: { meals: FitKnowMeal[] }) {
  const fat = unique(meals.map((meal) => meal.fatNote))
  const produce = unique(meals.map((meal) => meal.produceNote))
  if (!fat.length && !produce.length) return null

  return (
    <div className="mt-5 grid gap-3 md:grid-cols-2">
      {fat.length ? <GuidanceBlock title="脂肪吃法" items={fat} /> : null}
      {produce.length ? <GuidanceBlock title="蔬菜/水果" items={produce} /> : null}
    </div>
  )
}

function GuidanceBlock({ title, items }: { title: string; items: string[] }) {
  return (
    <article className="rounded-[22px] border border-white/10 bg-white/[0.04] p-4">
      <p className="font-semibold text-white">{title}</p>
      {items.map((item) => (
        <p className="mt-2 text-sm leading-6 text-slate-400" key={item}>{item}</p>
      ))}
    </article>
  )
}

function LegacyMealRows({ plan, query }: { plan: FitKnowDietPlan; query: string }) {
  return (
    <section className="grid gap-4 xl:grid-cols-2">
      <MealList title="力训日饮食" rows={(plan.trainingDayMeals ?? []).filter((row) => fitKnowMatches(row, query))} />
      <MealList title="休息日饮食" rows={(plan.restDayMeals ?? []).filter((row) => fitKnowMatches(row, query))} />
    </section>
  )
}

function MealList({ title, rows }: { title: string; rows: { row: number; text: string }[] }) {
  return (
    <div className="rounded-[32px] border border-white/10 bg-slate-950/50 p-5">
      <h3 className="text-lg font-semibold text-white">{title}</h3>
      <div className="mt-4 space-y-2">
        {rows.map((row) => (
          <p key={row.row} className="rounded-2xl border border-white/10 bg-white/[0.04] p-3 text-sm leading-6 text-slate-300">
            <span className="mr-2 text-cyan-200">第 {row.row} 行</span>
            {row.text}
          </p>
        ))}
        {!rows.length ? <p className="text-sm text-slate-500">没有匹配的餐食行。</p> : null}
      </div>
    </div>
  )
}

function ExecutionNotes({ plan, query }: { plan: FitKnowDietPlan; query: string }) {
  const inputs = plan.inputs.filter((item) => fitKnowMatches(item, query))
  return (
    <section className="rounded-[32px] border border-white/10 bg-slate-950/50 p-5">
      <p className="text-xs uppercase tracking-[0.32em] text-cyan-300">Notes</p>
      <h3 className="mt-3 text-xl font-semibold text-white">执行说明</h3>
      <div className="mt-4 grid gap-3">
        {inputs.map((item, index) => (
          <article key={`${item.label}-${index}`} className="rounded-[22px] border border-white/10 bg-white/[0.04] p-4">
            <p className="font-semibold text-white">{item.label}</p>
            <p className="mt-2 text-sm leading-6 text-slate-400">{item.detail}</p>
          </article>
        ))}
      </div>
    </section>
  )
}

function QaPanel({ items, category, isChinese }: { items: FitKnowQa[]; category: string; isChinese: boolean }) {
  return (
    <section className="rounded-[32px] border border-white/10 bg-slate-950/50 p-5">
      <p className="text-xs uppercase tracking-[0.32em] text-emerald-300">QA</p>
      <h3 className="mt-3 text-xl font-semibold text-white">{isChinese ? `${category}常见问题` : `${category} QA`}</h3>
      <div className="mt-4 space-y-3">
        {items.map((item) => (
          <details key={item.id} className="rounded-[22px] border border-white/10 bg-white/[0.04] p-4">
            <summary className="cursor-pointer font-medium text-white">{item.question}</summary>
            <p className="mt-3 text-sm leading-6 text-slate-400">{item.answer || '暂无详细回答。'}</p>
          </details>
        ))}
        {!items.length ? <p className="text-sm text-slate-500">{isChinese ? '没有匹配的问答。' : 'No matching QA.'}</p> : null}
      </div>
    </section>
  )
}

function GoalCard({ href, title, count, description, accent }: { href: string; title: string; count: number; description: string; accent: 'cyan' | 'emerald' }) {
  const accentClass = accent === 'cyan' ? 'text-cyan-200 border-cyan-300/20 bg-cyan-300/10' : 'text-emerald-200 border-emerald-300/20 bg-emerald-300/10'
  return (
    <Link href={href} className="rounded-[32px] border border-white/10 bg-slate-950/50 p-6 transition hover:border-cyan-300/30 hover:bg-white/[0.07]">
      <span className={`inline-flex rounded-full border px-3 py-1 text-xs uppercase tracking-[0.28em] ${accentClass}`}>{count} paths</span>
      <h3 className="mt-5 text-2xl font-semibold text-white">{title}</h3>
      <p className="mt-3 text-sm leading-7 text-slate-400">{description}</p>
    </Link>
  )
}

function SegmentedControl({ items, selectedId, onSelect, compact = false }: { items: { id: string; label: string }[]; selectedId: string; onSelect: (id: string) => void; compact?: boolean }) {
  return (
    <div className="flex flex-wrap gap-2">
      {items.map((item) => (
        <button
          key={item.id}
          type="button"
          onClick={() => onSelect(item.id)}
          className={`${compact ? 'px-3 py-2 text-[11px]' : 'px-4 py-2 text-xs'} rounded-full border transition ${item.id === selectedId ? 'border-cyan-300/60 bg-cyan-300 text-slate-950' : 'border-white/10 bg-white/5 text-slate-300 hover:bg-white/10 hover:text-white'}`}
        >
          {item.label}
        </button>
      ))}
    </div>
  )
}

function InputField({ label, value, onChange }: { label: string; value: string; onChange: (value: string) => void }) {
  return (
    <label>
      <span className={fieldLabelClass}>{label}</span>
      <input value={value} inputMode="decimal" onChange={(event) => onChange(event.target.value)} className={inputClass} />
    </label>
  )
}

function SelectField({ label, value, onChange, options }: { label: string; value: string; onChange: (value: string) => void; options: { value: string; label: string }[] }) {
  return (
    <label>
      <span className={fieldLabelClass}>{label}</span>
      <select value={value} onChange={(event) => onChange(event.target.value)} className={inputClass}>
        {options.map((option) => (
          <option key={option.value} value={option.value}>{option.label}</option>
        ))}
      </select>
    </label>
  )
}

function LabeledInlineInput({ label, value, onChange }: { label: string; value: string; onChange: (value: string) => void }) {
  return (
    <label className="grid grid-cols-[minmax(0,1fr)_32px] items-center overflow-hidden rounded-xl border border-white/10 bg-slate-950/60">
      <input value={value} inputMode="decimal" onChange={(event) => onChange(event.target.value)} className="min-w-0 bg-transparent px-3 py-2 text-sm text-white outline-none" />
      <span className="border-l border-white/10 px-2 text-center text-xs text-slate-500">{label}</span>
    </label>
  )
}

function MetricTile({ label, value }: { label: string; value: string | number }) {
  return (
    <div className="rounded-[20px] border border-white/10 bg-white/[0.04] px-3 py-3">
      <p className="text-[10px] uppercase tracking-[0.22em] text-slate-500">{label}</p>
      <p className="mt-1 text-lg font-semibold text-white">{value}</p>
    </div>
  )
}

function dayLabel(dayType: FitKnowMealTable['dayType']) {
  if (dayType === 'training') return '力训日饮食'
  if (dayType === 'rest') return '休息日饮食'
  return '每日饮食'
}

function unique(items: string[]) {
  return [...new Set(items.map((item) => item.trim()).filter(Boolean))]
}

function makeEntryId() {
  return `${Date.now()}-${Math.random().toString(36).slice(2)}`
}

const primaryTabClass = 'rounded-full bg-cyan-300 px-5 py-3 text-xs font-semibold uppercase tracking-[0.28em] text-slate-950'
const secondaryTabClass = 'rounded-full border border-white/10 px-5 py-3 text-xs uppercase tracking-[0.28em] text-slate-200 hover:bg-white/10'
const fieldLabelClass = 'mb-2 block text-xs uppercase tracking-[0.26em] text-slate-400'
const inputClass = 'w-full rounded-2xl border border-white/10 bg-white/5 px-4 py-3 text-sm text-white outline-none focus:border-cyan-300'

