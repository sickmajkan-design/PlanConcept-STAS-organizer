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

export function useWorkItemsQuery(query: WorkItemListQuery) {
  return useResourceList(workItemKeys, workItemsApi.list, query);
}

export function useWorkItemQuery(id: string | undefined) {
  return useResourceDetail(workItemKeys, workItemsApi.get, id);
}

export function useCreateWorkItem() {
  return useResourceMutation(
    (input: WorkItemInput) => workItemsApi.create(input),
    [workItemKeys.all],
  );
}

export function useUpdateWorkItem(id: string) {
  return useResourceMutation(
    (input: WorkItemInput) => workItemsApi.update(id, input),
    [workItemKeys.all],
  );
}

export function useChangeWorkItemStatus(id: string) {
  return useResourceMutation(
    (status: WorkItemStatus) => workItemsApi.changeStatus(id, status),
    [workItemKeys.all],
  );
}

export function useDeleteWorkItem() {
  return useResourceMutation((id: string) => workItemsApi.remove(id), [
    workItemKeys.all,
  ]);
}
