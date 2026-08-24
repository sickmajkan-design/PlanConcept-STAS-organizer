import { createCrudApi } from './resource';
import type {
  ListQuery,
  NotificationGroup,
  NotificationGroupDetail,
  NotificationGroupInput,
} from './types';

export type NotificationGroupListQuery = ListQuery;

export const notificationGroupsApi = createCrudApi<
  NotificationGroup,
  NotificationGroupDetail,
  NotificationGroupInput,
  NotificationGroupListQuery
>('/api/v1/notificationgroups');
