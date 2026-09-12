import { z } from 'zod';

import { zodMsg } from '../../i18n/zodMessage';
import { roles } from '../../api/types';

/** Mirrors the API's SendAnnouncementCommandValidator. */
export const announcementFormSchema = z.object({
  title: z
    .string()
    .trim()
    .min(1, { error: zodMsg('validation.required') })
    .max(256, { error: zodMsg('validation.maxLength', { max: 256 }) }),
  body: z
    .string()
    .trim()
    .min(1, { error: zodMsg('validation.saySomething') })
    .max(4000, { error: zodMsg('validation.maxLength', { max: 4000 }) }),
  /**
   * Empty string means "everyone" rather than a role, because a `<TextField
   * select>` cannot hold null. It is turned back into null on submit.
   */
  role: z.union([z.enum(roles), z.literal('')]),
  projectId: z.string(),
  groupId: z.string(),
  requiresAcknowledgment: z.boolean(),
});

export type AnnouncementFormValues = z.infer<typeof announcementFormSchema>;
