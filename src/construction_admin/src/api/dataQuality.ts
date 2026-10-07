import { request } from './client';

export interface DataQualityItem {
  id: string;
  label: string;
  detail: string | null;
}

export interface DataQualityGroup {
  key: string;
  /** The full number of records with this problem; `items` is only the first few. */
  count: number;
  items: DataQualityItem[];
}

export interface DataQuality {
  groups: DataQualityGroup[];
}

export const dataQualityApi = {
  get: () => request<DataQuality>({ method: 'GET', url: '/api/v1/data-quality' }),
};
