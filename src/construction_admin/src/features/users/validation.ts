import { z } from 'zod';

import { zodMsg } from '../../i18n/zodMessage';
import { roles } from '../../api/types';

/**
 * Mirrors the API's PasswordRules.StrongPassword, so the form rejects a weak
 * password before a round trip rather than after one.
 */
export const passwordSchema = z
  .string()
  .min(8, { error: zodMsg('validation.passwordMin') })
  .max(128, { error: zodMsg('validation.maxLength', { max: 128 }) })
  .regex(/[A-Z]/, { error: zodMsg('validation.passwordUpper') })
  .regex(/[a-z]/, { error: zodMsg('validation.passwordLower') })
  .regex(/[0-9]/, { error: zodMsg('validation.passwordDigit') });

const baseUserSchema = z.object({
  email: z
    .string()
    .trim()
    .min(1, { error: zodMsg('validation.emailRequired') })
    .max(256, { error: zodMsg('validation.maxLength', { max: 256 }) })
    .refine((value) => z.string().email().safeParse(value).success, {
      error: zodMsg('validation.emailInvalid'),
    }),
  role: z.enum(roles, { error: zodMsg('validation.roleRequired') }),
  // Empty string is what an unselected picker submits; it means "no employee".
  employeeId: z.string().optional().or(z.literal('')),
  // Empty string means "use the system default" — the API's own
  // interpretation of a null value.
  documentExpiryReminderDays: z.string().optional().or(z.literal('')),
  // Only a SuperAdmin caller may actually change this; the API silently
  // ignores it from anyone else, so the field is harmless to always send.
  canViewCustomerTaxDetails: z.boolean().optional(),
});

export const createUserSchema = baseUserSchema.extend({
  password: passwordSchema,
});

export const editUserSchema = baseUserSchema;

export type CreateUserFormValues = z.infer<typeof createUserSchema>;
export type EditUserFormValues = z.infer<typeof editUserSchema>;

export const setPasswordSchema = z.object({
  newPassword: passwordSchema,
});

export type SetPasswordFormValues = z.infer<typeof setPasswordSchema>;
