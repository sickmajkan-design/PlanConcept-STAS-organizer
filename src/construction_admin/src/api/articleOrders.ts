import { request } from './client';
import { listParams } from './resource';
import type {
  ArticleOrder,
  ArticleOrderInput,
  ArticleOrderStatus,
  ListQuery,
  PagedList,
} from './types';

export interface ArticleOrderListQuery extends ListQuery {
  status?: ArticleOrderStatus;
  /** Requested, ordered and on their way: everything not yet in the requester's hands. */
  openOnly?: boolean;
  /** Only what the signed-in account asked for itself. */
  mine?: boolean;
}

export const articleOrdersApi = {
  list: (query: ArticleOrderListQuery) =>
    request<PagedList<ArticleOrder>>({
      method: 'GET',
      url: '/api/v1/articleorders',
      params: listParams(query),
    }),

  create: (input: ArticleOrderInput) =>
    request<ArticleOrder>({ method: 'POST', url: '/api/v1/articleorders', data: input }),

  setStatus: (id: string, status: ArticleOrderStatus, note?: string | null) =>
    request<ArticleOrder>({
      method: 'POST',
      url: `/api/v1/articleorders/${id}/status`,
      data: { status, note: note ?? null },
    }),
};
