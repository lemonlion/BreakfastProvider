import { get } from '../client';
import type { MilkResponse, GoatMilkResponse, EggsResponse, FlourResponse } from '../types';

export function getMilk(): Promise<MilkResponse> {
  return get<MilkResponse>('/milk');
}

export function getGoatMilk(): Promise<GoatMilkResponse> {
  return get<GoatMilkResponse>('/goat-milk');
}

export function getEggs(): Promise<EggsResponse> {
  return get<EggsResponse>('/eggs');
}

export function getFlour(): Promise<FlourResponse> {
  return get<FlourResponse>('/flour');
}
