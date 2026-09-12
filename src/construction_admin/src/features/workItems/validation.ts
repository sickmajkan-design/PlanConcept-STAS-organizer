import { z } from 'zod';

import { liveT } from '../../i18n/liveT';
import { zodMsg } from '../../i18n/zodMessage';
import { workItemKinds, workItemPriorities } from '../../api/types';

/** Mirrors the API's WorkItemCommandBaseValidator. */
export const workItemFormSchema = z
  .object({
    kind: z.enum(workItemKinds),
    title: z
      .string()
      .trim()
      .min(1, { error: zodMsg('validation.required') })
      .max(256, { error: zodMsg('validation.maxLength', { max: 256 }) }),
    description: z
      .string()
      .trim()
      .max(4000, { error: zodMsg('validation.maxLength', { max: 4000 }) })
      .optional()
      .or(z.literal('')),
    projectId: z.string().optional().or(z.literal('')),
    assignedEmployeeId: z.string().optional().or(z.literal('')),
    priority: z.enum(workItemPriorities),
    dueDate: z.string().optional().or(z.literal('')),
    requiresAcknowledgment: z.boolean(),
  })
  .superRefine((values, ctx) => {
    // Mirrors the database's check constraint, so the message lands on the
    // field rather than arriving as a constraint violation.
    if (values.kind === 'Defect' && !values.projectId) {
      ctx.addIssue({
        code: 'custom',
        path: ['projectId'],
        message: liveT('validation.defectNeedsProject'),
      });
    }
  });

export type WorkItemFormValues = z.infer<typeof workItemFormSchema>;
