import { z } from 'zod';

import { roles } from '../../api/types';

/**
 * Mirrors the API's PasswordRules.StrongPassword, so the form rejects a weak
 * password before a round trip rather than after one.
 */
export const passwordSchema = z
  .string()
  .min(8, 'Password must be at least 8 characters long.')
  .max(128)
  .regex(/[A-Z]/, 'Password must contain at least one upper-case letter.')
  .regex(/[a-z]/, 'Password must contain at least one lower-case letter.')
  .regex(/[0-9]/, 'Password must contain at least one digit.');

const baseUserSchema = z.object({
  email: z
    .string()
    .trim()
    .min(1, 'Email is required.')
    .max(256)
    .refine((value) => z.string().email().safeParse(value).success, {
      message: 'Email is not a valid email address.',
    }),
  role: z.enum(roles, { message: 'Role is required.' }),
  // Empty string is what an unselected picker submits; it means "no employee".
  employeeId: z.string().optional().or(z.literal('')),
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
