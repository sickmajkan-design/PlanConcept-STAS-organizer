import { z } from 'zod';

import { zodMsg } from '../i18n/zodMessage';

/** Mirrors the API's password policy so the form gives instant feedback. */
export const strongPasswordSchema = z
  .string()
  .min(1, { error: zodMsg('validation.required') })
  .min(8, { error: zodMsg('validation.passwordMin') })
  .max(128, { error: zodMsg('validation.maxLength', { max: 128 }) })
  .regex(/[A-Z]/, { error: zodMsg('validation.passwordUpper') })
  .regex(/[a-z]/, { error: zodMsg('validation.passwordLower') })
  .regex(/[0-9]/, { error: zodMsg('validation.passwordDigit') });

export const emailSchema = z
  .string()
  .min(1, { error: zodMsg('validation.emailRequired') })
  .email({ error: zodMsg('validation.emailInvalid') });

export const loginSchema = z.object({
  email: emailSchema,
  password: z.string().min(1, { error: zodMsg('validation.required') }),
});

export type LoginFormValues = z.infer<typeof loginSchema>;

export const forgotPasswordSchema = z.object({
  email: emailSchema,
});

export type ForgotPasswordFormValues = z.infer<typeof forgotPasswordSchema>;

export const resetPasswordSchema = z
  .object({
    email: emailSchema,
    token: z.string().min(1, { error: zodMsg('validation.required') }),
    newPassword: strongPasswordSchema,
    confirmPassword: z.string().min(1, { error: zodMsg('validation.required') }),
  })
  .refine((values) => values.newPassword === values.confirmPassword, {
    error: zodMsg('validation.passwordsDiffer'),
    path: ['confirmPassword'],
  });

export type ResetPasswordFormValues = z.infer<typeof resetPasswordSchema>;

export const changePasswordSchema = z
  .object({
    currentPassword: z.string().min(1, { error: zodMsg('validation.required') }),
    newPassword: strongPasswordSchema,
    confirmPassword: z.string().min(1, { error: zodMsg('validation.required') }),
  })
  .refine((values) => values.newPassword !== values.currentPassword, {
    error: zodMsg('validation.passwordSameAsCurrent'),
    path: ['newPassword'],
  })
  .refine((values) => values.newPassword === values.confirmPassword, {
    error: zodMsg('validation.passwordsDiffer'),
    path: ['confirmPassword'],
  });

export type ChangePasswordFormValues = z.infer<typeof changePasswordSchema>;
