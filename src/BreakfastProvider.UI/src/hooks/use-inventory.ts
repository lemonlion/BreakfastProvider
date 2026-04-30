import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getInventory,
  getInventoryItem,
  createInventoryItem,
  updateInventoryItem,
  deleteInventoryItem,
} from '@/lib/api/endpoints';
import type { InventoryItemRequest } from '@/lib/api/types';

export function useInventory() {
  return useQuery({ queryKey: ['inventory'], queryFn: getInventory });
}

export function useInventoryItem(id: number) {
  return useQuery({
    queryKey: ['inventory', id],
    queryFn: () => getInventoryItem(id),
    enabled: id > 0,
  });
}

export function useCreateInventoryItem() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (r: InventoryItemRequest) => createInventoryItem(r),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['inventory'] }),
  });
}

export function useUpdateInventoryItem() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, request }: { id: number; request: InventoryItemRequest }) =>
      updateInventoryItem(id, request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['inventory'] }),
  });
}

export function useDeleteInventoryItem() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => deleteInventoryItem(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['inventory'] }),
  });
}
