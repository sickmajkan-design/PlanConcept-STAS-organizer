import { request } from './client';
import { listParams } from './resource';
import type { AnnouncementInput, Notification, PagedList } from './types';

/**
 * The inbox is the signed-in user's own, so there is no employee or role
 * filter here: the API reads the owner from the token and never accepts one
 * from the caller.
 */
export interface NotificationListQuery {
  pageNumber: number;
  pageSize: number;
  unreadOnly?: boolean;
}

/**
 * Notifications are not a CRUD collection — nothing creates one from the panel
 * except an announcement, and nothing edits one. So this is written out rather
 * than built from `createCrudApi`.
 */
export const notificationsApi = {
  list: (query: NotificationListQuery) =>
    request<PagedList<Notification>>({
      method: 'GET',
      url: '/api/v1/notifications',
      params: listParams(query),
    }),

  unreadCount: () =>
    request<number>({ method: 'GET', url: '/api/v1/notifications/unread-count' }),

  markRead: (id: string) =>
    request<void>({ method: 'POST', url: `/api/v1/notifications/${id}/read` }),

  /** Returns how many were still unread. */
  markAllRead: () =>
    request<number>({ method: 'POST', url: '/api/v1/notifications/read-all' }),

  /** Returns the number of people it reached. */
  announce: (input: AnnouncementInput) =>
    request<number>({
      method: 'POST',
      url: '/api/v1/notifications/announce',
      data: input,
    }),
};
