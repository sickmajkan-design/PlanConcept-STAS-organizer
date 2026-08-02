import { request } from './client';
import { createCrudApi } from './resource';
import type {
  CreateUserAccountInput,
  ListQuery,
  Role,
  UserAccount,
  UserAccountInput,
} from './types';

export interface UserListQuery extends ListQuery {
  role?: Role | '';
  /** Unset shows both, which is what "who still has access" needs. */
  isActive?: boolean;
}

export const usersApi = {
  ...createCrudApi<UserAccount, UserAccount, UserAccountInput, UserListQuery>(
    '/api/users',
  ),

  // Create takes a password, which the shared input type does not carry.
  create: (input: CreateUserAccountInput) =>
    request<UserAccount>({ method: 'POST', url: '/api/users', data: input }),

  /**
   * Offboards the account. The API revokes every session, invalidates any
   * outstanding reset link and drops the device registrations, so this is not
   * the same as editing an "active" field.
   */
  deactivate: (id: string) =>
    request<void>({ method: 'POST', url: `/api/users/${id}/deactivate` }),

  activate: (id: string) =>
    request<UserAccount>({ method: 'POST', url: `/api/users/${id}/activate` }),

  setPassword: (id: string, newPassword: string) =>
    request<void>({
      method: 'POST',
      url: `/api/users/${id}/password`,
      data: { newPassword },
    }),
};
