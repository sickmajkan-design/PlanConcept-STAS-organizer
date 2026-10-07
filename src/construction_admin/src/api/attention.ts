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
  get: () => request<Attention>({ method: 'GET', url: '/api/v1/attention' }),
};
