import { useQuery } from '@tanstack/react-query';
import {
  getOrderSummaries,
  getRecipeReports,
  getIngredientUsage,
  getPopularRecipes,
} from '@/lib/api/endpoints';

export function useOrderSummaries(first = 10, after?: string, statusFilter?: string) {
  return useQuery({
    queryKey: ['reporting', 'order-summaries', { first, after, statusFilter }],
    queryFn: () => getOrderSummaries(first, after, statusFilter),
    select: (data) => ({
      items: data.edges.map((e) => e.node),
      pageInfo: data.pageInfo,
    }),
  });
}

export function useRecipeReports(first = 10, recipeTypeFilter?: string) {
  return useQuery({
    queryKey: ['reporting', 'recipe-reports', { first, recipeTypeFilter }],
    queryFn: () => getRecipeReports(first, recipeTypeFilter),
    select: (data) => data.edges.map((e) => e.node),
  });
}

export function useIngredientUsage() {
  return useQuery({
    queryKey: ['reporting', 'ingredient-usage'],
    queryFn: getIngredientUsage,
  });
}

export function usePopularRecipes() {
  return useQuery({
    queryKey: ['reporting', 'popular-recipes'],
    queryFn: getPopularRecipes,
  });
}
