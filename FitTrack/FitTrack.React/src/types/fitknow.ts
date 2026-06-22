export type FitKnowGoal = 'fat-loss' | 'muscle-gain'

export type FitKnowSex = 'male' | 'female'

export type FitKnowSourceRef = {
  sheetIndex: number
  sheetTitle: string
  row?: number
  rowRange?: string
}

export type FitKnowDietPlan = {
  id: string
  sheetIndex: number
  title: string
  goal: FitKnowGoal
  timing: string
  summary: string
  inputs: FitKnowInputField[]
  trainingDayMeals?: FitKnowMealRow[]
  restDayMeals?: FitKnowMealRow[]
  mealTables?: FitKnowMealTable[]
  sourceRefs: FitKnowSourceRef[]
}

export type FitKnowInputField = {
  label: string
  detail: string
}

export type FitKnowMealRow = {
  row: number
  text: string
  sourceRefs: FitKnowSourceRef[]
}

export type FitKnowMealTable = {
  dayType: 'training' | 'rest' | 'daily'
  title: string
  meals: FitKnowMeal[]
}

export type FitKnowMeal = {
  order: string
  name: string
  mealTypeLabel: string
  carbPercent: number
  proteinPercent: number
  carbOptions: string[]
  proteinOptions: string[]
  fatNote: string
  produceNote: string
}

export type FitKnowFood = {
  id: string
  macroType: string
  group: string
  name: string
  rate: string
  giOrPosition: string
  explanation: string
  sourceRefs: FitKnowSourceRef[]
}

export type FitKnowQa = {
  id: string
  category: string
  question: string
  answer: string
  tags: string[]
  sourceRefs: FitKnowSourceRef[]
}

export type FitKnowPlannerProfile = {
  sex: FitKnowSex
  height: string
  weight: string
  age: string
  strengthCalories: string
  cardioCalories: string
}

export type FitKnowQuotaMatch = {
  trainingCarb?: number
  restCarb?: number
  dailyCarb?: number
  protein: number
  key: string
  sex: FitKnowSex
  noStrength: boolean
  matchedHeight: number
  matchedWeight: number
  isExact: boolean
}

export type FitKnowNutritionMetrics = {
  bmiText: string
  bmiLabel: string
  goal: FitKnowGoal
  sex: FitKnowSex
  height: number
  weight: number
  bmr: number
  restingExpenditure: number
  strengthCalories: number
  cardioCalories: number
  maintenanceCalories?: number
  trainingMaintenanceCalories?: number
  restMaintenanceCalories?: number
  targetCalories?: number
  trainingTargetCalories?: number
  restTargetCalories?: number
  primaryTargetCalories: number
  quotaMatch: FitKnowQuotaMatch | null
  advice: string
  targetWeightText: string
  tdee?: number
  protein?: number
  fat?: number
  carbs?: number
  carbsRate?: number
  restCarbsRate?: number
  proteinRate?: number
  fatRate?: number
  trainingCarbs?: number
  trainingProtein?: number
  trainingFat?: number
  restCarbs?: number
  restProtein?: number
  restFat?: number
}

export type FitKnowMealTargets = {
  carbTotal: number
  proteinTotal: number
  carb: number
  protein: number
}

