import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { getMenu, clearMenuCache } from '@/lib/api/endpoints';

export function useMenu() {
  return useQuery({
    queryKey: ['menu'],
    queryFn: getMenu,
    staleTime: 5 * 60 * 1000,
  });
}

export function useClearMenuCache() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: clearMenuCache,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['menu'] });
    },
  });
}
