import { request } from './client';

export interface AttentionItem {
  label: string;
  /** What it is about where a word is not enough: for a vehicle, which date. */
  kind: string | null;
  date: string | null;
}

export interface AttentionGroup {
  key: string;
  count: number;
  items: AttentionItem[];
}

export interface Attention {
  groups: AttentionGroup[];
}

export const attentionApi = {
  /** Narrowed to one business unit when given; `basis` says whose hours count for it. */
  get: (unit: { branchId?: string; basis?: 'Employer' } = {}) =>
    request<Attention>({ method: 'GET', url: '/api/v1/attention', params: unit }),
};
