import {
  articleOrdersApi,
  type ArticleOrderListQuery,
} from '../../api/articleOrders';
import type { ArticleOrderInput, ArticleOrderStatus } from '../../api/types';
import { createResourceKeys, useResourceList, useResourceMutation } from '../resourceQueries';

export const articleOrderKeys = createResourceKeys<ArticleOrderListQuery>('article-orders');

export function useArticleOrdersQuery(query: ArticleOrderListQuery, enabled = true) {
  return useResourceList(articleOrderKeys, articleOrdersApi.list, query, { enabled });
}

/** Every write drops every list, so the page, the widget and the badge agree at once. */
const caches = [articleOrderKeys.all];

export function useCreateArticleOrder() {
  return useResourceMutation(
    (input: ArticleOrderInput) => articleOrdersApi.create(input),
    caches,
  );
}

export function useSetArticleOrderStatus() {
  return useResourceMutation(
    ({ id, status, note }: { id: string; status: ArticleOrderStatus; note?: string | null }) =>
      articleOrdersApi.setStatus(id, status, note),
    caches,
  );
}
