import { keepPreviousData, useQuery } from '@tanstack/react-query';

import {
  notificationsApi,
  type NotificationListQuery,
} from '../../api/notifications';
import type {
  AnnouncementInput,
  AudienceTarget,
  BroadcastListQuery,
  SendBroadcastInput,
} from '../../api/types';
import { createResourceKeys, useResourceList, useResourceMutation } from '../resourceQueries';

export const notificationKeys = createResourceKeys<NotificationListQuery>('notifications');

/**
 * The badge count is its own cache entry rather than something derived from
 * the list: the badge is on screen everywhere, the list only on one page, and
 * counting unread rows out of one page would undercount as soon as there is a
 * second page.
 */
export const unreadCountKey = ['notifications', 'unread-count'] as const;

/** How often the badge asks again. */
const UNREAD_POLL_MS = 30_000;

export function useNotificationsQuery(query: NotificationListQuery) {
  return useResourceList(notificationKeys, notificationsApi.list, query);
}

/**
 * Polls, because nothing pushes to the browser.
 *
 * The mobile app gets these over FCM; the panel has no such channel, so a
 * minute's poll is the whole mechanism. It is one integer over the wire, and
 * the alternative — a count that only moves on a page reload — is a badge
 * nobody would trust.
 */
export function useUnreadCountQuery() {
  return useQuery({
    queryKey: unreadCountKey,
    queryFn: () => notificationsApi.unreadCount(),
    refetchInterval: UNREAD_POLL_MS,
    // A count that is a minute stale is fine; one that is wrong after the
    // laptop wakes up is not.
    refetchOnWindowFocus: true,
  });
}

/** Reading changes both the rows and the badge. */
const inboxCaches = [notificationKeys.all, unreadCountKey];

export function useMarkNotificationRead() {
  return useResourceMutation(
    (id: string) => notificationsApi.markRead(id),
    inboxCaches,
  );
}

export function useMarkAllNotificationsRead() {
  // `void` rather than an inferred parameter, so the call site is `mutate()`
  // and not `mutate(undefined)`.
  return useResourceMutation<void, number>(
    () => notificationsApi.markAllRead(),
    inboxCaches,
  );
}

/**
 * An announcement reaches the sender too when they match the audience, so it
 * invalidates the inbox as well as sending.
 */
export function useSendAnnouncement() {
  return useResourceMutation(
    (input: AnnouncementInput) => notificationsApi.announce(input),
    inboxCaches,
  );
}

const broadcastsKey = ['notifications', 'broadcasts'] as const;

/** Who the chosen targets would reach; asked again as the choices change, the last answer shown meanwhile. */
export function useAudiencePreviewQuery(targets: AudienceTarget[]) {
  return useQuery({
    queryKey: ['notifications', 'audience-preview', targets],
    queryFn: () => notificationsApi.previewAudience(targets),
    enabled: targets.length > 0,
    placeholderData: keepPreviousData,
  });
}

export function useSendBroadcast() {
  return useResourceMutation((input: SendBroadcastInput) => notificationsApi.sendBroadcast(input), [broadcastsKey]);
}

/** The sent announcements, newest first. */
export function useBroadcastsQuery(query: BroadcastListQuery) {
  return useQuery({
    queryKey: [...broadcastsKey, 'list', query],
    queryFn: () => notificationsApi.broadcasts(query),
    placeholderData: keepPreviousData,
  });
}

export function useBroadcastQuery(id: string | undefined) {
  return useQuery({
    queryKey: [...broadcastsKey, 'detail', id],
    queryFn: () => notificationsApi.broadcast(id!),
    enabled: !!id,
    // Reading happens while the page is open: people read it, the numbers move.
    refetchInterval: 30_000,
  });
}

export function useRemindBroadcast() {
  return useResourceMutation((id: string) => notificationsApi.remind(id), [broadcastsKey]);
}
