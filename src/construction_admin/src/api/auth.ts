import { anonymousRequest, cookieAuthHeaders, request } from './client';
import type { AuthResponse, User } from './types';

export const authApi = {
  login: (email: string, password: string) =>
    anonymousRequest<AuthResponse>({
      method: 'POST',
      url: '/api/v1/auth/login',
      data: { email: email.trim(), password },
      // Asks the API to put the refresh token in a cookie and leave it out of
      // the response body, so it never passes through anything script reads.
      headers: cookieAuthHeaders,
    }),

  // No token in the body: the API reads the cookie, and clears it. Anonymous,
  // because the cookie is the credential — an idle sign-out happens long after
  // the access token has died, and must still reach the API.
  logout: () =>
    anonymousRequest<void>({ method: 'POST', url: '/api/v1/auth/logout', data: {} }),

  currentUser: () => request<User>({ method: 'GET', url: '/api/v1/auth/me' }),

  changePassword: (currentPassword: string, newPassword: string) =>
    request<void>({
      method: 'POST',
      url: '/api/v1/auth/change-password',
      data: { currentPassword, newPassword },
    }),

  forgotPassword: (email: string) =>
    anonymousRequest<void>({
      method: 'POST',
      url: '/api/v1/auth/forgot-password',
      data: { email: email.trim() },
    }),

  resetPassword: (email: string, token: string, newPassword: string) =>
    anonymousRequest<void>({
      method: 'POST',
      url: '/api/v1/auth/reset-password',
      data: { email: email.trim(), token, newPassword },
    }),
};
