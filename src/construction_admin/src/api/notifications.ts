import { request } from './client';
import { listParams } from './resource';
import type {
  AnnouncementInput,
  AudiencePreview,
  AudienceTarget,
  BroadcastDetail,
  BroadcastListItem,
  BroadcastListQuery,
  Notification,
  PagedList,
  SendBroadcastInput,
  SendBroadcastResult,
} from './types';

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

  /** Who a choice of targets would reach, before anything is sent. */
  previewAudience: (targets: AudienceTarget[]) =>
    request<AudiencePreview>({ method: 'POST', url: '/api/v1/notifications/audience-preview', data: { targets } }),

  /** Sends an announcement to everybody the targets name and keeps a record of it. */
  sendBroadcast: (input: SendBroadcastInput) =>
    request<SendBroadcastResult>({ method: 'POST', url: '/api/v1/notifications/broadcasts', data: input }),

  broadcasts: (query: BroadcastListQuery) =>
    request<PagedList<BroadcastListItem>>({
      method: 'GET',
      url: '/api/v1/notifications/broadcasts',
      params: listParams(query),
    }),

  broadcast: (id: string) =>
    request<BroadcastDetail>({ method: 'GET', url: `/api/v1/notifications/broadcasts/${id}` }),

  /** Returns how many people were reminded. */
  remind: (id: string) =>
    request<number>({ method: 'POST', url: `/api/v1/notifications/broadcasts/${id}/remind` }),

  /** Returns the number of people it reached. */
  announce: (input: AnnouncementInput) =>
    request<number>({
      method: 'POST',
      url: '/api/v1/notifications/announce',
      data: input,
    }),
};
