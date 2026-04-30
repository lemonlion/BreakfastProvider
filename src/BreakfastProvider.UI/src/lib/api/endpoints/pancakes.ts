import { post } from '../client';
import type { PancakeRequest, PancakeResponse } from '../types';

export function createPancake(request: PancakeRequest): Promise<PancakeResponse> {
  return post<PancakeResponse>('/pancakes', { body: request });
}
