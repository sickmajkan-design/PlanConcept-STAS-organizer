import { z } from 'zod';

/** Mirrors the API's password policy so the form gives instant feedback. */
export const strongPasswordSchema = z
  .string()
  .min(1, 'Password is required.')
  .min(8, 'Password must be at least 8 characters long.')
  .max(128, 'Password must not exceed 128 characters.')
  .regex(/[A-Z]/, 'Password must contain an upper-case letter.')
  .regex(/[a-z]/, 'Password must contain a lower-case letter.')
  .regex(/[0-9]/, 'Password must contain a digit.');

export const emailSchema = z
  .string()
  .min(1, 'Email is required.')
  .email('Enter a valid email address.');

export const loginSchema = z.object({
  email: emailSchema,
  password: z.string().min(1, 'Password is required.'),
});

export type LoginFormValues = z.infer<typeof loginSchema>;

export const forgotPasswordSchema = z.object({
  email: emailSchema,
});

export type ForgotPasswordFormValues = z.infer<typeof forgotPasswordSchema>;

export const resetPasswordSchema = z
  .object({
    email: emailSchema,
    token: z.string().min(1, 'Reset token is required.'),
    newPassword: strongPasswordSchema,
    confirmPassword: z.string().min(1, 'Confirm the new password.'),
  })
  .refine((values) => values.newPassword === values.confirmPassword, {
    message: 'The passwords do not match.',
    path: ['confirmPassword'],
  });

export type ResetPasswordFormValues = z.infer<typeof resetPasswordSchema>;

export const changePasswordSchema = z
  .object({
    currentPassword: z.string().min(1, 'Current password is required.'),
    newPassword: strongPasswordSchema,
    confirmPassword: z.string().min(1, 'Confirm the new password.'),
  })
  .refine((values) => values.newPassword !== values.currentPassword, {
    message: 'New password must be different from the current password.',
    path: ['newPassword'],
  })
  .refine((values) => values.newPassword === values.confirmPassword, {
    message: 'The passwords do not match.',
    path: ['confirmPassword'],
  });

export type ChangePasswordFormValues = z.infer<typeof changePasswordSchema>;
