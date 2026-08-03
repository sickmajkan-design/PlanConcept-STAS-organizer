import { z } from 'zod';

import { absenceTypes } from '../../api/types';

/** Longest single absence. Mirrors AbsenceRules.MaxDays. */
export const maxAbsenceDays = 180;

/** Mirrors the API's RequestAbsenceCommandValidator. */
export const absenceFormSchema = z
  .object({
    employeeId: z.string().min(1, 'Pick who this is for.'),
    type: z.enum(absenceTypes),
    startDate: z.string().min(1, 'A start date is required.'),
    endDate: z.string().min(1, 'An end date is required.'),
    reason: z.string().trim().max(1000).optional().or(z.literal('')),
    approve: z.boolean(),
  })
  .superRefine((values, ctx) => {
    if (!values.startDate || !values.endDate) {
      return;
    }

    const start = new Date(values.startDate);
    const end = new Date(values.endDate);

    if (end < start) {
      ctx.addIssue({
        code: 'custom',
        path: ['endDate'],
        message: 'The absence cannot end before it starts.',
      });
      return;
    }

    // Both ends inclusive, matching DayCount on the server.
    const days = Math.round((end.getTime() - start.getTime()) / 86_400_000) + 1;

    if (days > maxAbsenceDays) {
      ctx.addIssue({
        code: 'custom',
        path: ['endDate'],
        message: 'An absence that long is a change of employment, not leave.',
      });
    }
  });

export type AbsenceFormValues = z.infer<typeof absenceFormSchema>;
