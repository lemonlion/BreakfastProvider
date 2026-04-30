import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getStaff,
  createStaffMember,
  updateStaffMember,
  deleteStaffMember,
} from '@/lib/api/endpoints';
import type { StaffMemberRequest } from '@/lib/api/types';

export function useStaff() {
  return useQuery({ queryKey: ['staff'], queryFn: getStaff });
}

export function useCreateStaffMember() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (r: StaffMemberRequest) => createStaffMember(r),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['staff'] }),
  });
}

export function useUpdateStaffMember() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, request }: { id: number; request: StaffMemberRequest }) =>
      updateStaffMember(id, request),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['staff'] }),
  });
}

export function useDeleteStaffMember() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => deleteStaffMember(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['staff'] }),
  });
}
