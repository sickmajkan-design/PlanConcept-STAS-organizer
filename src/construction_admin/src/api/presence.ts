import { request } from './client';

export interface OnlineUser {
  userId: string;
  name: string;
  email: string;
  role: string;
  /** "web" for the panel, "app" for the mobile app. */
  client: 'web' | 'app';
  screen: string | null;
  lastSeenAt: string;
  signedInAt: string | null;
}

export interface UserSession {
  id: string;
  userId: string;
  name: string;
  email: string;
  role: string;
  ipAddress: string | null;
  client: 'web' | 'app';
  startedAt: string;
  lastSeenAt: string;
}

export const presenceApi = {
  heartbeat: (screen: string) =>
    request<void>({ method: 'POST', url: '/api/v1/presence/heartbeat', data: { screen } }),
  online: () => request<OnlineUser[]>({ method: 'GET', url: '/api/v1/presence/online' }),
  sessions: (days: number) =>
    request<UserSession[]>({ method: 'GET', url: '/api/v1/presence/sessions', params: { days } }),
};
