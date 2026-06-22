import {
  femaleMuscleGainRatios,
  lookupRatio,
  maleFatLossRatios,
  maleMuscleGainRatios,
} from '@/lib/fitknow/fitness-ratios'
import type {
  FitKnowDietPlan,
  FitKnowFood,
  FitKnowGoal,
  FitKnowMeal,
  FitKnowMealTable,
  FitKnowMealTargets,
  FitKnowNutritionMetrics,
  FitKnowPlannerProfile,
  FitKnowQuotaMatch,
  FitKnowSex,
} from '@/types/fitknow'

type FatLossQuotaEntry = {
  trainingCarb?: number
  restCarb?: number
  dailyCarb?: number
  protein: number
}

type FatLossQuotaTable = Record<FitKnowSex, Record<string, FatLossQuotaEntry>>

const fatLossQuotaTables: Record<'training' | 'noStrength', FatLossQuotaTable> = {
  training: {
    male: {
      '160-60': { trainingCarb: 2.6, restCarb: 2.0, protein: 1.4 },
      '160-65': { trainingCarb: 2.6, restCarb: 1.9, protein: 1.4 },
      '165-65': { trainingCarb: 2.6, restCarb: 2.0, protein: 1.4 },
      '170-70': { trainingCarb: 2.6, restCarb: 2.0, protein: 1.4 },
      '175-70': { trainingCarb: 2.7, restCarb: 2.1, protein: 1.4 },
      '170-75': { trainingCarb: 2.5, restCarb: 2.0, protein: 1.4 },
      '175-75': { trainingCarb: 2.6, restCarb: 2.1, protein: 1.4 },
      '180-75': { trainingCarb: 2.7, restCarb: 2.1, protein: 1.4 },
      '175-80': { trainingCarb: 2.5, restCarb: 2.0, protein: 1.4 },
      '180-80': { trainingCarb: 2.6, restCarb: 2.1, protein: 1.4 },
      '185-80': { trainingCarb: 2.6, restCarb: 2.1, protein: 1.4 },
      '180-85': { trainingCarb: 2.5, restCarb: 2.0, protein: 1.4 },
      '185-85': { trainingCarb: 2.6, restCarb: 2.1, protein: 1.4 },
      '190-85': { trainingCarb: 2.6, restCarb: 2.2, protein: 1.4 },
      '175-90': { trainingCarb: 2.4, restCarb: 2.0, protein: 1.3 },
      '180-90': { trainingCarb: 2.5, restCarb: 2.0, protein: 1.3 },
      '185-90': { trainingCarb: 2.5, restCarb: 2.1, protein: 1.4 },
      '190-90': { trainingCarb: 2.6, restCarb: 2.1, protein: 1.4 },
    },
    female: {
      '150-45': { trainingCarb: 2.4, restCarb: 1.8, protein: 1.3 },
      '150-50': { trainingCarb: 2.3, restCarb: 1.8, protein: 1.2 },
      '155-50': { trainingCarb: 2.4, restCarb: 1.9, protein: 1.3 },
      '160-50': { trainingCarb: 2.5, restCarb: 2.0, protein: 1.3 },
      '150-55': { trainingCarb: 2.2, restCarb: 1.8, protein: 1.2 },
      '155-55': { trainingCarb: 2.3, restCarb: 1.9, protein: 1.2 },
      '160-55': { trainingCarb: 2.4, restCarb: 1.9, protein: 1.3 },
      '165-55': { trainingCarb: 2.5, restCarb: 2.0, protein: 1.3 },
      '160-60': { trainingCarb: 2.3, restCarb: 1.9, protein: 1.2 },
      '165-60': { trainingCarb: 2.4, restCarb: 2.0, protein: 1.3 },
      '170-60': { trainingCarb: 2.5, restCarb: 2.1, protein: 1.3 },
      '170-65': { trainingCarb: 2.4, restCarb: 2.0, protein: 1.3 },
      '175-65': { trainingCarb: 2.5, restCarb: 2.1, protein: 1.3 },
      '170-70': { trainingCarb: 2.3, restCarb: 2.0, protein: 1.2 },
      '175-70': { trainingCarb: 2.4, restCarb: 2.0, protein: 1.3 },
      '180-70': { trainingCarb: 2.4, restCarb: 2.1, protein: 1.3 },
    },
  },
  noStrength: {
    male: {
      '160-60': { dailyCarb: 2.4, protein: 1.0 },
      '160-65': { dailyCarb: 2.3, protein: 1.0 },
      '165-65': { dailyCarb: 2.4, protein: 1.0 },
      '160-70': { dailyCarb: 2.3, protein: 1.0 },
      '165-70': { dailyCarb: 2.3, protein: 1.0 },
      '170-70': { dailyCarb: 2.4, protein: 1.0 },
      '175-70': { dailyCarb: 2.5, protein: 1.1 },
      '170-75': { dailyCarb: 2.4, protein: 1.0 },
      '175-75': { dailyCarb: 2.4, protein: 1.0 },
      '180-75': { dailyCarb: 2.5, protein: 1.1 },
      '175-80': { dailyCarb: 2.4, protein: 1.0 },
      '180-80': { dailyCarb: 2.4, protein: 1.0 },
      '185-80': { dailyCarb: 2.5, protein: 1.1 },
    },
    female: {
      '150-45': { dailyCarb: 2.2, protein: 1.1 },
      '150-50': { dailyCarb: 2.1, protein: 1.1 },
      '155-50': { dailyCarb: 2.1, protein: 0.9 },
      '160-50': { dailyCarb: 2.1, protein: 1.2 },
      '150-55': { dailyCarb: 2.1, protein: 1.0 },
      '155-55': { dailyCarb: 2.0, protein: 0.9 },
      '160-55': { dailyCarb: 2.1, protein: 1.1 },
      '165-55': { dailyCarb: 2.2, protein: 1.2 },
      '155-60': { dailyCarb: 2.0, protein: 0.9 },
      '160-60': { dailyCarb: 2.0, protein: 1.1 },
      '165-60': { dailyCarb: 2.1, protein: 1.1 },
      '170-60': { dailyCarb: 2.2, protein: 1.2 },
      '165-65': { dailyCarb: 2.1, protein: 1.1 },
      '170-65': { dailyCarb: 2.1, protein: 1.2 },
      '175-65': { dailyCarb: 2.2, protein: 1.2 },
      '180-65': { dailyCarb: 2.3, protein: 1.2 },
    },
  },
}

export const defaultFitKnowProfiles: Record<FitKnowGoal, FitKnowPlannerProfile> = {
  'fat-loss': { sex: 'male', height: '175', weight: '75', age: '28', strengthCalories: '200', cardioCalories: '0' },
  'muscle-gain': { sex: 'male', height: '175', weight: '65', age: '28', strengthCalories: '200', cardioCalories: '0' },
}

export function isNoStrengthPlan(plan?: Pick<FitKnowDietPlan, 'id' | 'title' | 'timing'>) {
  if (!plan) return false
  return `${plan.id} ${plan.title} ${plan.timing}`.includes('无力训')
}

export function clampNumber(value: string | number | undefined, min: number, max: number, fallback: number) {
  const number = Number.parseFloat(String(value ?? ''))
  if (!Number.isFinite(number)) return fallback
  return Math.min(Math.max(number, min), max)
}

export function findFatLossQuota(sex: FitKnowSex, height: number, weight: number, noStrength: boolean): FitKnowQuotaMatch | null {
  const tableGroup = noStrength ? fatLossQuotaTables.noStrength : fatLossQuotaTables.training
  const heightKey = String(Math.round(height))
  const weightValue = Math.round(weight)
  const entries = Object.entries(tableGroup[sex] ?? {})
    .map(([key, entry]) => {
      const [entryHeight, entryWeight] = key.split('-').map(Number)
      return { key, entry, height: entryHeight, weight: entryWeight }
    })
    .filter((item) => String(item.height) === heightKey)
    .sort((left, right) => left.weight - right.weight)

  if (!entries.length) return null

  const exact = entries.find((item) => item.weight === weightValue)
  const lowerOrEqual = entries.filter((item) => item.weight <= weightValue).at(-1)
  const match = exact ?? lowerOrEqual ?? entries[0]

  return {
    ...match.entry,
    key: match.key,
    sex,
    noStrength,
    matchedHeight: match.height,
    matchedWeight: match.weight,
    isExact: exact != null,
  }
}

export function computeFitKnowNutrition(
  goal: FitKnowGoal,
  profile: FitKnowPlannerProfile,
  plan: FitKnowDietPlan,
): FitKnowNutritionMetrics {
  const sex: FitKnowSex = profile.sex === 'female' ? 'female' : 'male'
  const height = clampNumber(profile.height, 120, 230, 175)
  const weight = clampNumber(profile.weight, 35, 180, 70)
  const age = clampNumber(profile.age, 12, 80, 28)
  const heightM = height / 100
  const bmi = weight / (heightM * heightM)
  const bmr = Math.round(10 * weight + 6.25 * height - 5 * age + (sex === 'male' ? 5 : -161))
  const bmiLabel = bmi < 18.5 ? '偏低' : bmi < 24 ? '正常' : bmi < 28 ? '超重' : '肥胖'
  const targetWeight = Math.round((goal === 'fat-loss' ? 22 : 23) * heightM * heightM)

  if (goal === 'fat-loss') {
    const noStrength = isNoStrengthPlan(plan)
    const strengthCalories = noStrength ? 0 : clampNumber(profile.strengthCalories, 0, 800, sex === 'male' ? 200 : 150)
    const cardioCalories = clampNumber(profile.cardioCalories, 0, 1200, 0)
    const restingExpenditure = Math.round(bmr / 0.7)
    const maintenanceCalories = Math.round(restingExpenditure + cardioCalories)
    const trainingMaintenanceCalories = Math.round(restingExpenditure + strengthCalories + cardioCalories)
    const restMaintenanceCalories = maintenanceCalories
    const targetCalories = Math.round(maintenanceCalories * 0.64)
    const trainingTargetCalories = Math.round(trainingMaintenanceCalories * 0.64)
    const restTargetCalories = Math.round(restMaintenanceCalories * 0.64)
    const quotaMatch = findFatLossQuota(sex, height, weight, noStrength)

    return {
      bmiText: Number.isFinite(bmi) ? bmi.toFixed(1) : '-',
      bmiLabel,
      goal,
      sex,
      height,
      weight,
      bmr,
      restingExpenditure,
      strengthCalories,
      cardioCalories,
      maintenanceCalories,
      targetCalories,
      trainingMaintenanceCalories,
      restMaintenanceCalories,
      trainingTargetCalories,
      restTargetCalories,
      primaryTargetCalories: noStrength ? targetCalories : trainingTargetCalories,
      quotaMatch,
      advice: noStrength
        ? `每日应吃热量 = (${restingExpenditure} + ${cardioCalories}) x 0.64 ≈ ${targetCalories} kcal。有氧消耗请填平均到每天的数值。`
        : `力训日应吃热量 = (${restingExpenditure} + ${strengthCalories} + ${cardioCalories}) x 0.64 ≈ ${trainingTargetCalories} kcal；休息日应吃热量 = (${restingExpenditure} + ${cardioCalories}) x 0.64 ≈ ${restTargetCalories} kcal。`,
      targetWeightText: `以 BMI 约 22 估算，阶段目标体重可先看 ${targetWeight} kg 附近；若已有伤病或代谢问题，优先咨询专业人士。`,
    }
  }

  const strengthCalories = clampNumber(profile.strengthCalories, 0, 800, sex === 'male' ? 200 : 150)
  const cardioCalories = clampNumber(profile.cardioCalories, 0, 1200, 0)
  const restingExpenditure = Math.round(bmr / 0.7)
  const tdee = Math.round(restingExpenditure + strengthCalories + cardioCalories)
  const restMaintenanceCalories = Math.round(restingExpenditure + cardioCalories)
  const trainingMaintenanceCalories = tdee
  const restTargetCalories = Math.round(restMaintenanceCalories * 0.84)
  const trainingTargetCalories = Math.round(trainingMaintenanceCalories * 0.84)
  const minimumCalories = sex === 'male' ? 1500 : 1200
  const targetCalories = Math.max(trainingTargetCalories, minimumCalories)
  const [carbsRate, restCarbsRate, proteinRate] = lookupRatio(
    sex === 'female' ? femaleMuscleGainRatios : maleMuscleGainRatios,
    weight,
    height,
  )
  const fatRate = 0.9
  const trainingCarbs = Math.round(weight * carbsRate)
  const trainingProtein = Math.round(weight * proteinRate)
  const trainingFat = Math.round(weight * fatRate)
  const restCarbs = Math.round(weight * restCarbsRate)
  const restProtein = trainingProtein
  const restFat = trainingFat

  return {
    bmiText: Number.isFinite(bmi) ? bmi.toFixed(1) : '-',
    bmiLabel,
    goal,
    sex,
    height,
    weight,
    bmr,
    restingExpenditure,
    strengthCalories,
    cardioCalories,
    tdee,
    trainingMaintenanceCalories,
    restMaintenanceCalories,
    trainingTargetCalories,
    restTargetCalories,
    primaryTargetCalories: trainingTargetCalories,
    targetCalories,
    protein: trainingProtein,
    fat: trainingFat,
    carbs: trainingCarbs,
    carbsRate: roundRate(carbsRate),
    restCarbsRate: roundRate(restCarbsRate),
    proteinRate: roundRate(proteinRate),
    fatRate,
    trainingCarbs,
    trainingProtein,
    trainingFat,
    restCarbs,
    restProtein,
    restFat,
    quotaMatch: null,
    advice: `力训日应吃热量 = (${restingExpenditure} + ${strengthCalories} + ${cardioCalories}) x 0.84 ≈ ${trainingTargetCalories} kcal；休息日应吃热量 = (${restingExpenditure} + ${cardioCalories}) x 0.84 ≈ ${restTargetCalories} kcal。执行 2-3 周后看体重、围度和训练表现，若体重不上升，再按问答建议增加 100-150 kcal。`,
    targetWeightText: `以 BMI 约 23 估算，长期体重上限可先参考 ${targetWeight} kg 附近，优先保证围度和力量质量。`,
  }
}

export function computeMealTargets(
  metrics: FitKnowNutritionMetrics,
  table: FitKnowMealTable,
  meal: FitKnowMeal,
): FitKnowMealTargets | null {
  const quota = metrics.quotaMatch
  const carbRate = table.dayType === 'training' ? quota?.trainingCarb : table.dayType === 'rest' ? quota?.restCarb : quota?.dailyCarb
  const carbTotal = quota
    ? Number(carbRate) * metrics.weight
    : table.dayType === 'rest'
      ? metrics.restCarbs
      : metrics.trainingCarbs
  const proteinTotal = quota
    ? quota.protein * metrics.weight
    : table.dayType === 'rest'
      ? metrics.restProtein
      : metrics.trainingProtein

  if (!Number.isFinite(carbTotal) || !Number.isFinite(proteinTotal)) return null

  return {
    carbTotal: carbTotal ?? 0,
    proteinTotal: proteinTotal ?? 0,
    carb: ((carbTotal ?? 0) * meal.carbPercent) / 100,
    protein: ((proteinTotal ?? 0) * meal.proteinPercent) / 100,
  }
}

export function targetCaloriesForTable(metrics: FitKnowNutritionMetrics, dayType: FitKnowMealTable['dayType']) {
  if (dayType === 'training') return metrics.trainingTargetCalories
  if (dayType === 'rest') return metrics.restTargetCalories
  return metrics.targetCalories
}

export function parseFoodRate(food?: Pick<FitKnowFood, 'rate'>) {
  if (!food?.rate) return null
  const rateText = String(food.rate)
  const unitMatch = rateText.match(/^(\d+(?:\.\d+)?)g\/(.+)$/)
  if (unitMatch) {
    const rate = Number.parseFloat(unitMatch[1])
    return rate ? { mode: 'unit' as const, rate, unit: unitMatch[2] } : null
  }

  const rate = Number.parseFloat(rateText)
  return rate ? { mode: 'gram' as const, rate, unit: 'g' } : null
}

export function convertFoodWeight(targetGrams: number, food?: FitKnowFood) {
  if (!Number.isFinite(targetGrams) || !food?.rate) return null
  const parsed = parseFoodRate(food)
  if (!parsed) return null
  if (parsed.mode === 'unit') {
    const count = targetGrams / parsed.rate
    return {
      value: count < 10 ? Math.round(count * 10) / 10 : Math.round(count),
      unit: parsed.unit,
    }
  }

  return {
    value: Math.round(targetGrams / parsed.rate),
    unit: 'g',
  }
}

export function computeFoodMacro(amount: string, food?: FitKnowFood) {
  const value = Number.parseFloat(amount)
  const parsed = parseFoodRate(food)
  if (!Number.isFinite(value) || value <= 0 || !parsed) return 0
  return value * parsed.rate
}

export function foodInputUnit(food?: FitKnowFood) {
  return parseFoodRate(food)?.unit ?? 'g'
}

export function suggestedFoodAmount(target: number | undefined, food?: FitKnowFood) {
  if (typeof target !== 'number') return ''
  const amount = convertFoodWeight(target, food)
  return amount ? String(amount.value) : ''
}

export function suggestedMacroAmount(amount: string, food?: FitKnowFood) {
  const macro = computeFoodMacro(amount, food)
  if (!macro) return ''
  return macro < 10 ? String(Math.round(macro * 10) / 10) : String(Math.round(macro))
}

export function reverseFoodAmount(macro: string, food?: FitKnowFood) {
  const value = Number.parseFloat(macro)
  const amount = convertFoodWeight(value, food)
  return amount ? String(amount.value) : ''
}

export function foodOptionLabel(food: FitKnowFood) {
  return `${food.name} · ${food.rate}`
}

export function findFoodByQuery(query: string, foods: FitKnowFood[], fuzzy = false) {
  const text = query.trim()
  if (!text) return null
  const compactText = text.replace(/\s+/g, '')
  const exact = foods.find((food) => foodOptionLabel(food) === text || food.name === text)
  if (exact || !fuzzy) return exact ?? null

  return (
    foods.find((food) => {
      const name = food.name.replace(/\s+/g, '')
      const label = foodOptionLabel(food).replace(/\s+/g, '')
      return name.includes(compactText) || label.includes(compactText)
    }) ?? null
  )
}

export function findDefaultFood(options: string[] = [], macroType: string, foods: FitKnowFood[]) {
  const macroFoods = foods.filter((food) => food.macroType === macroType)
  const optionMatch = macroFoods.find((food) => options.some((option) => foodMatchesOption(food, option)))
  if (optionMatch) return optionMatch

  const fallbackNames = macroType === '碳水'
    ? ['米饭（很软）', '米饭（一般）']
    : ['鸡蛋', '鸡胸肉', '瘦猪肉', '瘦牛肉']
  return macroFoods.find((food) => fallbackNames.some((name) => food.name.includes(name))) ?? macroFoods[0]
}

function foodMatchesOption(food: FitKnowFood, option: string) {
  const text = option.replace(/\s+/g, '')
  const name = food.name.replace(/\s+/g, '')
  return text.length > 0 && (text.includes(name) || name.includes(text.slice(0, Math.min(4, text.length))))
}

function roundRate(value: number) {
  return Math.round(value * 100) / 100
}

export { lookupRatio, maleFatLossRatios, maleMuscleGainRatios, femaleMuscleGainRatios }

