import { useQuery } from '@tanstack/react-query';

import {
  notificationGroupsApi,
  type NotificationGroupListQuery,
} from '../../api/notificationGroups';
import type { NotificationGroupInput } from '../../api/types';
import {
  createResourceKeys,
  useResourceDetail,
  useResourceList,
  useResourceMutation,
} from '../resourceQueries';

export const notificationGroupKeys =
  createResourceKeys<NotificationGroupListQuery>('notificationGroups');

const PICKER_QUERY: NotificationGroupListQuery = {
  pageNumber: 1,
  pageSize: 100,
  sortBy: 'name',
};

export function useNotificationGroupsQuery(query: NotificationGroupListQuery) {
  return useResourceList(notificationGroupKeys, notificationGroupsApi.list, query);
}

export function useNotificationGroupQuery(id: string | undefined) {
  return useResourceDetail(notificationGroupKeys, notificationGroupsApi.get, id);
}

/** All groups, for the picker in the announce dialog. */
export function useAllNotificationGroupsQuery() {
  return useQuery({
    queryKey: notificationGroupKeys.list(PICKER_QUERY),
    queryFn: () => notificationGroupsApi.list(PICKER_QUERY),
    staleTime: 60_000,
  });
}

export function useCreateNotificationGroup() {
  return useResourceMutation(
    (input: NotificationGroupInput) => notificationGroupsApi.create(input),
    [notificationGroupKeys.all],
  );
}

export function useUpdateNotificationGroup(id: string) {
  return useResourceMutation(
    (input: NotificationGroupInput) => notificationGroupsApi.update(id, input),
    [notificationGroupKeys.all],
  );
}

export function useDeleteNotificationGroup() {
  return useResourceMutation(
    (id: string) => notificationGroupsApi.remove(id),
    [notificationGroupKeys.all],
  );
}
