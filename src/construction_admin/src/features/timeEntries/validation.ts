import { z } from 'zod';

import { workTypes } from '../../api/types';

/**
 * The longest and furthest-back shift the API will accept. Mirrored here so
 * the form says so before a round trip, and kept in one place so the two
 * limits are visible together rather than buried in two rules.
 */
export const MAX_SHIFT_HOURS = 16;
export const MAX_BACKDATING_DAYS = 31;

/** Minutes between two `datetime-local` values, or null while one is missing. */
function minutesBetween(startedAt: string, endedAt: string): number | null {
  if (!startedAt || !endedAt) return null;

  const start = new Date(startedAt).getTime();
  const end = new Date(endedAt).getTime();

  if (Number.isNaN(start) || Number.isNaN(end)) return null;

  // Truncated the same way the API and the entity truncate it, so the form
  // and the server never disagree about a break that leaves nothing worked.
  return Math.trunc((end - start) / 60_000);
}

/**
 * Mirrors the API's TimeEntryCommandBaseValidator.
 *
 * The cross-field rules are attached to the field a user can fix rather than
 * to the object, so the message lands on an input instead of at the top of the
 * form where it reads as unrelated.
 */
export const timeEntryFormSchema = z
  .object({
    employeeId: z.string().min(1, 'Employee is required.'),
    projectId: z.string().optional().or(z.literal('')),
    startedAt: z.string().min(1, 'Start time is required.'),
    endedAt: z.string().optional().or(z.literal('')),
    breakMinutes: z
      .string()
      .refine((value) => value === '' || !Number.isNaN(Number(value)), {
        message: 'Must be a number.',
      })
      .refine((value) => value === '' || Number(value) >= 0, {
        message: 'Break must not be negative.',
      }),
    workType: z.enum(workTypes),
    note: z.string().trim().max(1000).optional().or(z.literal('')),
  })
  .superRefine((values, ctx) => {
    const start = new Date(values.startedAt).getTime();

    if (!Number.isNaN(start)) {
      const backdatingLimit = Date.now() - MAX_BACKDATING_DAYS * 86_400_000;

      if (start > Date.now() + 5 * 60_000) {
        ctx.addIssue({
          code: 'custom',
          path: ['startedAt'],
          message: 'A shift cannot start in the future.',
        });
      } else if (start < backdatingLimit) {
        ctx.addIssue({
          code: 'custom',
          path: ['startedAt'],
          message: `A shift cannot be recorded more than ${MAX_BACKDATING_DAYS} days back.`,
        });
      }
    }

    // Everything below needs both ends; a running shift has neither a duration
    // nor a break to check against one.
    const duration = minutesBetween(values.startedAt, values.endedAt ?? '');

    if (duration === null) return;

    if (duration <= 0) {
      ctx.addIssue({
        code: 'custom',
        path: ['endedAt'],
        message: 'The shift must end after it starts.',
      });
      return;
    }

    if (duration > MAX_SHIFT_HOURS * 60) {
      ctx.addIssue({
        code: 'custom',
        path: ['endedAt'],
        message: `A shift cannot be longer than ${MAX_SHIFT_HOURS} hours.`,
      });
      return;
    }

    if (Number(values.breakMinutes || 0) >= duration) {
      ctx.addIssue({
        code: 'custom',
        path: ['breakMinutes'],
        message: 'The break is as long as the shift, which would leave no time worked.',
      });
    }
  });

export type TimeEntryFormValues = z.infer<typeof timeEntryFormSchema>;
