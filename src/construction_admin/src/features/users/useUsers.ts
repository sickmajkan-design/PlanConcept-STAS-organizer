import { usersApi, type UserListQuery } from '../../api/users';
import type { CreateUserAccountInput, UserAccount, UserAccountInput } from '../../api/types';
import {
  createResourceKeys,
  useResourceDetail,
  useResourceList,
  useResourceMutation,
} from '../resourceQueries';

export const userKeys = createResourceKeys<UserListQuery>('users');

export function useUsersQuery(query: UserListQuery) {
  return useResourceList(userKeys, usersApi.list, query);
}

export function useUserQuery(id: string | undefined) {
  return useResourceDetail(userKeys, usersApi.get, id);
}

export function useCreateUser() {
  return useResourceMutation(
    (input: CreateUserAccountInput) => usersApi.create(input),
    [userKeys.all],
  );
}

export function useUpdateUser(id: string) {
  return useResourceMutation(
    (input: UserAccountInput) => usersApi.update(id, input),
    [userKeys.all],
  );
}

// Deactivating changes what the employee directory shows about an account, so
// both caches are refreshed.
export function useDeactivateUser() {
  return useResourceMutation((id: string) => usersApi.deactivate(id), [
    userKeys.all,
    ['employees'],
  ]);
}

export function useActivateUser() {
  return useResourceMutation<string, UserAccount>(
    (id: string) => usersApi.activate(id),
    [userKeys.all, ['employees']],
  );
}

export function useSetUserPassword(id: string) {
  return useResourceMutation(
    (newPassword: string) => usersApi.setPassword(id, newPassword),
    [userKeys.all],
  );
}
