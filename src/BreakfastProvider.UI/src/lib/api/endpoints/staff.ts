import { get, post, put, del } from '../client';
import type { StaffMemberRequest, StaffMemberResponse } from '../types';

export function getStaff(): Promise<StaffMemberResponse[]> {
  return get<StaffMemberResponse[]>('/staff');
}

export function getStaffMember(id: number): Promise<StaffMemberResponse> {
  return get<StaffMemberResponse>(`/staff/${id}`);
}

export function createStaffMember(request: StaffMemberRequest): Promise<StaffMemberResponse> {
  return post<StaffMemberResponse>('/staff', { body: request });
}

export function updateStaffMember(
  id: number,
  request: StaffMemberRequest,
): Promise<StaffMemberResponse> {
  return put<StaffMemberResponse>(`/staff/${id}`, { body: request });
}

export function deleteStaffMember(id: number): Promise<void> {
  return del(`/staff/${id}`);
}
