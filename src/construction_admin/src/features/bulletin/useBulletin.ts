import { useQuery } from '@tanstack/react-query';

import { bulletinApi } from '../../api/bulletin';
import type { BulletinPostInput } from '../../api/types';
import { createResourceKeys, useResourceMutation } from '../resourceQueries';

export const bulletinKeys = createResourceKeys<void>('bulletin');

export function useBulletinPostsQuery() {
  return useQuery({
    queryKey: bulletinKeys.list(undefined),
    queryFn: () => bulletinApi.list(),
  });
}

export function useCreateBulletinPost() {
  return useResourceMutation(
    (input: BulletinPostInput) => bulletinApi.create(input),
    [bulletinKeys.all],
  );
}

export function useDeleteBulletinPost() {
  return useResourceMutation((id: string) => bulletinApi.remove(id), [bulletinKeys.all]);
}

export function useMarkBulletinViewed() {
  return useResourceMutation((id: string) => bulletinApi.markViewed(id), [bulletinKeys.all]);
}

export function useBulletinViewersQuery(postId: string | null) {
  return useQuery({
    queryKey: bulletinKeys.detail(postId ?? ''),
    queryFn: () => bulletinApi.viewers(postId!),
    enabled: !!postId,
  });
}
