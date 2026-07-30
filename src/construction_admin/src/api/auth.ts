import { anonymousRequest, request } from './client';
import type { AuthResponse, User } from './types';

export const authApi = {
  login: (email: string, password: string) =>
    anonymousRequest<AuthResponse>({
      method: 'POST',
      url: '/api/auth/login',
      data: { email: email.trim(), password },
    }),

  logout: (refreshToken: string) =>
    request<void>({
      method: 'POST',
      url: '/api/auth/logout',
      data: { refreshToken },
    }),

  currentUser: () => request<User>({ method: 'GET', url: '/api/auth/me' }),

  changePassword: (currentPassword: string, newPassword: string) =>
    request<void>({
      method: 'POST',
      url: '/api/auth/change-password',
      data: { currentPassword, newPassword },
    }),

  forgotPassword: (email: string) =>
    anonymousRequest<void>({
      method: 'POST',
      url: '/api/auth/forgot-password',
      data: { email: email.trim() },
    }),

  resetPassword: (email: string, token: string, newPassword: string) =>
    anonymousRequest<void>({
      method: 'POST',
      url: '/api/auth/reset-password',
      data: { email: email.trim(), token, newPassword },
    }),
};
