import { request } from './client';
import { createCrudApi } from './resource';
import type {
  ListQuery,
  WorkItem,
  WorkItemInput,
  WorkItemKind,
  WorkItemPriority,
  WorkItemStatus,
} from './types';

export interface WorkItemListQuery extends ListQuery {
  kind?: WorkItemKind;
  status?: WorkItemStatus;
  priority?: WorkItemPriority;
  projectId?: string;
  assignedEmployeeId?: string;
  openOnly?: boolean;
  overdueOnly?: boolean;
  unassignedOnly?: boolean;
}

export const workItemsApi = {
  ...createCrudApi<WorkItem, WorkItem, WorkItemInput, WorkItemListQuery>(
    '/api/v1/workitems',
  ),

  changeStatus: (id: string, status: WorkItemStatus) =>
    request<WorkItem>({
      method: 'POST',
      url: `/api/v1/workitems/${id}/status`,
      data: { status },
    }),
};
