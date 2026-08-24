import { z } from 'zod';

import { workItemKinds, workItemPriorities } from '../../api/types';

/** Mirrors the API's WorkItemCommandBaseValidator. */
export const workItemFormSchema = z
  .object({
    kind: z.enum(workItemKinds),
    title: z.string().trim().min(1, 'A title is required.').max(256),
    description: z.string().trim().max(4000).optional().or(z.literal('')),
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
        message: 'A defect has to be raised against a site.',
      });
    }
  });

export type WorkItemFormValues = z.infer<typeof workItemFormSchema>;
