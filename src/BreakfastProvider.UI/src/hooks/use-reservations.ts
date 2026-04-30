import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getReservations,
  createReservation,
  updateReservation,
  cancelReservation,
  deleteReservation,
} from '@/lib/api/endpoints';
import type { ReservationRequest } from '@/lib/api/types';

export function useReservations() {
  return useQuery({ queryKey: ['reservations'], queryFn: getReservations });
}

export function useCreateReservation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (r: ReservationRequest) => createReservation(r),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['reservations'] }),
  });
}

export function useUpdateReservation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, request }: { id: number; request: ReservationRequest }) =>
      updateReservation(id, request),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['reservations'] }),
  });
}

export function useCancelReservation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => cancelReservation(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['reservations'] }),
  });
}

export function useDeleteReservation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => deleteReservation(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['reservations'] }),
  });
}
