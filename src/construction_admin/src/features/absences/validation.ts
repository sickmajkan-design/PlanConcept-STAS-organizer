import { z } from 'zod';

import { liveT } from '../../i18n/liveT';
import { zodMsg } from '../../i18n/zodMessage';
import { absenceTypes } from '../../api/types';

/** Longest single absence. Mirrors AbsenceRules.MaxDays. */
export const maxAbsenceDays = 180;

/** Mirrors the API's RequestAbsenceCommandValidator. */
export const absenceFormSchema = z
  .object({
    employeeId: z.string().min(1, { error: zodMsg('validation.pickEmployee') }),
    type: z.enum(absenceTypes),
    startDate: z.string().min(1, { error: zodMsg('validation.required') }),
    endDate: z.string().min(1, { error: zodMsg('validation.required') }),
    reason: z
      .string()
      .trim()
      .max(1000, { error: zodMsg('validation.maxLength', { max: 1000 }) })
      .optional()
      .or(z.literal('')),
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
        message: liveT('validation.absenceEndBeforeStart'),
      });
      return;
    }

    // Both ends inclusive, matching DayCount on the server.
    const days = Math.round((end.getTime() - start.getTime()) / 86_400_000) + 1;

    if (days > maxAbsenceDays) {
      ctx.addIssue({
        code: 'custom',
        path: ['endDate'],
        message: liveT('validation.absenceTooLong'),
      });
    }
  });

export type AbsenceFormValues = z.infer<typeof absenceFormSchema>;
