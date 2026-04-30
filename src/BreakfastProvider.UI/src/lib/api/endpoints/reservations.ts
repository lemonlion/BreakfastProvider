import { get, post, put, del, patch } from '../client';
import type { ReservationRequest, ReservationResponse } from '../types';

export function getReservations(): Promise<ReservationResponse[]> {
  return get<ReservationResponse[]>('/reservations');
}

export function getReservation(id: number): Promise<ReservationResponse> {
  return get<ReservationResponse>(`/reservations/${id}`);
}

export function createReservation(request: ReservationRequest): Promise<ReservationResponse> {
  return post<ReservationResponse>('/reservations', { body: request });
}

export function updateReservation(
  id: number,
  request: ReservationRequest,
): Promise<ReservationResponse> {
  return put<ReservationResponse>(`/reservations/${id}`, { body: request });
}

export function cancelReservation(id: number): Promise<ReservationResponse> {
  return patch<ReservationResponse>(`/reservations/${id}/cancel`);
}

export function deleteReservation(id: number): Promise<void> {
  return del(`/reservations/${id}`);
}
