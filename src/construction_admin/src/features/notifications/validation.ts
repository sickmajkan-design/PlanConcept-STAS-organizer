import { z } from 'zod';

import { roles } from '../../api/types';

/** Mirrors the API's SendAnnouncementCommandValidator. */
export const announcementFormSchema = z.object({
  title: z.string().trim().min(1, 'A subject is required.').max(256),
  body: z.string().trim().min(1, 'Say something.').max(4000),
  /**
   * Empty string means "everyone" rather than a role, because a `<TextField
   * select>` cannot hold null. It is turned back into null on submit.
   */
  role: z.union([z.enum(roles), z.literal('')]),
  projectId: z.string(),
});

export type AnnouncementFormValues = z.infer<typeof announcementFormSchema>;
