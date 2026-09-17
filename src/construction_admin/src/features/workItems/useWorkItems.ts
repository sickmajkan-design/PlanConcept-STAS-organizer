import {
  workItemsApi,
  type WorkItemListQuery,
} from '../../api/workItems';
import type { WorkItemInput, WorkItemStatus } from '../../api/types';
import {
  createResourceKeys,
  useResourceDetail,
  useResourceList,
  useResourceMutation,
} from '../resourceQueries';

export const workItemKeys = createResourceKeys<WorkItemListQuery>('workItems');

/**
 * `workItemKeys.all` is a prefix of every `useWorkItemsQuery` call, including
 * the nav badge's own two count queries — so assigning, closing, or deleting
 * a work item drops "Zadaci i nedostaci" the instant it succeeds, with
 * nothing extra to list here.
 */
const workItemCaches = [workItemKeys.all];

export function useWorkItemsQuery(query: WorkItemListQuery, enabled = true) {
  return useResourceList(workItemKeys, workItemsApi.list, query, { enabled });
}

export function useWorkItemQuery(id: string | undefined) {
  return useResourceDetail(workItemKeys, workItemsApi.get, id);
}

export function useCreateWorkItem() {
  return useResourceMutation(
    (input: WorkItemInput) => workItemsApi.create(input),
    workItemCaches,
  );
}

export function useUpdateWorkItem(id: string) {
  return useResourceMutation(
    (input: WorkItemInput) => workItemsApi.update(id, input),
    workItemCaches,
  );
}

export function useChangeWorkItemStatus(id: string) {
  return useResourceMutation(
    (status: WorkItemStatus) => workItemsApi.changeStatus(id, status),
    workItemCaches,
  );
}

export function useDeleteWorkItem() {
  return useResourceMutation((id: string) => workItemsApi.remove(id), workItemCaches);
}
