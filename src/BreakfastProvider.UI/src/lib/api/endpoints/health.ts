import { get } from '../client';
import type { HealthCheckResponse } from '../types';

export function getHeartbeat(): Promise<{ status: string }> {
  return get<{ status: string }>('/');
}

export function getHealth(): Promise<HealthCheckResponse> {
  return get<HealthCheckResponse>('/health');
}
