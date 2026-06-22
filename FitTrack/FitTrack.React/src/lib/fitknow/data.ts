import fatLossData from '@/data/fitknow/fat-loss.json'
import foodsData from '@/data/fitknow/foods.json'
import muscleGainData from '@/data/fitknow/muscle-gain.json'
import qaData from '@/data/fitknow/qa.json'
import type { FitKnowDietPlan, FitKnowFood, FitKnowGoal, FitKnowQa } from '@/types/fitknow'

type DietPayload = {
  dietPlans: FitKnowDietPlan[]
}

type FoodsPayload = {
  foods: FitKnowFood[]
}

type QaPayload = {
  qa: FitKnowQa[]
}

const dietPlansByGoal: Record<FitKnowGoal, FitKnowDietPlan[]> = {
  'fat-loss': (fatLossData as unknown as DietPayload).dietPlans,
  'muscle-gain': (muscleGainData as unknown as DietPayload).dietPlans,
}

export const fitKnowFoods = (foodsData as unknown as FoodsPayload).foods

export const fitKnowQa = (qaData as unknown as QaPayload).qa

export function getFitKnowDietPlans(goal: FitKnowGoal) {
  return dietPlansByGoal[goal]
}

export function getFitKnowGoalLabel(goal: FitKnowGoal, isChinese: boolean) {
  if (goal === 'fat-loss') {
    return isChinese ? '减脂饮食' : 'Fat-loss nutrition'
  }

  return isChinese ? '增肌饮食' : 'Muscle-gain nutrition'
}

export function getFitKnowGoalCategory(goal: FitKnowGoal) {
  return goal === 'fat-loss' ? '减脂' : '增肌'
}

export function getRelatedFitKnowQa(goal: FitKnowGoal, query: string) {
  const category = getFitKnowGoalCategory(goal)
  return fitKnowQa.filter((item) => item.category === category && fitKnowMatches(item, query))
}

export function fitKnowMatches(item: unknown, query: string) {
  const text = query.trim().toLowerCase()
  return !text || JSON.stringify(item).toLowerCase().includes(text)
}

