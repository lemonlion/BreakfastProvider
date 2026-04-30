import { useQueries } from '@tanstack/react-query';
import { getMilk, getGoatMilk, getEggs, getFlour } from '@/lib/api/endpoints';

export function useIngredients() {
  return useQueries({
    queries: [
      {
        queryKey: ['ingredients', 'milk'],
        queryFn: getMilk,
      },
      {
        queryKey: ['ingredients', 'goat-milk'],
        queryFn: getGoatMilk,
        retry: false,
      },
      {
        queryKey: ['ingredients', 'eggs'],
        queryFn: getEggs,
      },
      {
        queryKey: ['ingredients', 'flour'],
        queryFn: getFlour,
      },
    ],
  });
}
