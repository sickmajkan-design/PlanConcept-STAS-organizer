import { request } from './client';
import type { BulletinPost, BulletinPostInput, BulletinViewer } from './types';

export const bulletinApi = {
  list: () =>
    request<BulletinPost[]>({
      method: 'GET',
      url: '/api/v1/bulletin',
    }),

  create: (input: BulletinPostInput) =>
    request<BulletinPost>({
      method: 'POST',
      url: '/api/v1/bulletin',
      data: input,
    }),

  remove: (id: string) =>
    request<void>({ method: 'DELETE', url: `/api/v1/bulletin/${id}` }),

  markViewed: (id: string) =>
    request<void>({ method: 'POST', url: `/api/v1/bulletin/${id}/view` }),

  viewers: (id: string) =>
    request<BulletinViewer[]>({
      method: 'GET',
      url: `/api/v1/bulletin/${id}/viewers`,
    }),
};
