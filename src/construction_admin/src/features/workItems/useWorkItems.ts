import {
  workItemsApi,
  type WorkItemListQuery,
} from '../../api/workItems';
import type { WorkItemInput, WorkItemStatus } from '../../api/types';
import { navBadgeKeys } from '../../layout/useNavBadgeCounts';
import {
  createResourceKeys,
  useResourceDetail,
  useResourceList,
  useResourceMutation,
} from '../resourceQueries';

export const workItemKeys = createResourceKeys<WorkItemListQuery>('workItems');

/**
 * Every work-item write also invalidates the nav badge: assigning it,
 * closing it, or deleting it all resolve the same "nobody is assigned to it
 * yet" backlog the badge on "Zadaci i nedostaci" counts, so the number drops
 * the instant one of those succeeds rather than on the badge's own poll.
 */
const workItemCaches = [workItemKeys.all, navBadgeKeys.workItemsUnassigned];

export function useWorkItemsQuery(query: WorkItemListQuery) {
  return useResourceList(workItemKeys, workItemsApi.list, query);
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
