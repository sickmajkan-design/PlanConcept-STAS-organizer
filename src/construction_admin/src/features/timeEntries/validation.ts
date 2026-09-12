import { z } from 'zod';

import { liveT } from '../../i18n/liveT';
import { zodMsg } from '../../i18n/zodMessage';
import { workTypes } from '../../api/types';

/**
 * The longest and furthest-back shift the API will accept. Mirrored here so
 * the form says so before a round trip, and kept in one place so the two
 * limits are visible together rather than buried in two rules.
 */
export const MAX_SHIFT_HOURS = 16;
export const MAX_BACKDATING_DAYS = 31;

/**
 * `HH:mm`, 24-hour. The time half of `startedAt`/`endedAt` is typed into a
 * plain text field rather than a native `type="time"` input, because that
 * native control's displayed format follows the browser/OS locale — on an
 * en-US machine it silently becomes a 12-hour AM/PM picker regardless of
 * this app's own locale setting. A hand-validated 24-hour field is the only
 * way to guarantee what the platform actually shows.
 */
const TIME_PATTERN = /^([01]\d|2[0-3]):[0-5]\d$/;

/** True once both the date and time half of a combined `datetime-local`-shaped value are present and well-formed. */
function hasValidTimePart(value: string): boolean {
  const time = value.split('T')[1] ?? '';
  return TIME_PATTERN.test(time);
}

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
    employeeId: z.string().min(1, { error: zodMsg('validation.required') }),
    projectId: z.string().optional().or(z.literal('')),
    startedAt: z
      .string()
      .min(1, { error: zodMsg('validation.required') })
      .refine(hasValidTimePart, { error: zodMsg('validation.timeFormat') }),
    endedAt: z
      .string()
      .optional()
      .or(z.literal(''))
      .refine((value) => !value || hasValidTimePart(value), {
        error: zodMsg('validation.timeFormat'),
      }),
    breakMinutes: z
      .string()
      .refine((value) => value === '' || !Number.isNaN(Number(value)), {
        error: zodMsg('validation.mustBeNumber'),
      })
      .refine((value) => value === '' || Number(value) >= 0, {
        error: zodMsg('validation.breakNegative'),
      }),
    workType: z.enum(workTypes),
    note: z
      .string()
      .trim()
      .max(1000, { error: zodMsg('validation.maxLength', { max: 1000 }) })
      .optional()
      .or(z.literal('')),
  })
  .superRefine((values, ctx) => {
    const start = new Date(values.startedAt).getTime();

    if (!Number.isNaN(start)) {
      const backdatingLimit = Date.now() - MAX_BACKDATING_DAYS * 86_400_000;

      if (start > Date.now() + 5 * 60_000) {
        ctx.addIssue({
          code: 'custom',
          path: ['startedAt'],
          message: liveT('validation.shiftFuture'),
        });
      } else if (start < backdatingLimit) {
        ctx.addIssue({
          code: 'custom',
          path: ['startedAt'],
          message: liveT('validation.shiftTooOld', { days: MAX_BACKDATING_DAYS }),
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
        message: liveT('validation.shiftEndBeforeStart'),
      });
      return;
    }

    if (duration > MAX_SHIFT_HOURS * 60) {
      ctx.addIssue({
        code: 'custom',
        path: ['endedAt'],
        message: liveT('validation.shiftTooLong', { hours: MAX_SHIFT_HOURS }),
      });
      return;
    }

    if (Number(values.breakMinutes || 0) >= duration) {
      ctx.addIssue({
        code: 'custom',
        path: ['breakMinutes'],
        message: liveT('validation.breakAsLongAsShift'),
      });
    }
  });

export type TimeEntryFormValues = z.infer<typeof timeEntryFormSchema>;
