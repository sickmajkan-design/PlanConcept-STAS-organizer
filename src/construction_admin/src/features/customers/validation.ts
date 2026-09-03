import { z } from 'zod';

const optionalEmailString = z
  .string()
  .trim()
  .max(256)
  .refine((value) => value === '' || /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value), {
    message: 'Not a valid email address.',
  });

/** Mirrors the API's CustomerCommandBaseValidator so the form catches errors early. */
export const customerFormSchema = z.object({
  name: z.string().trim().min(1, 'Customer name is required.').max(256),
  contactPerson: z.string().trim().max(256).optional().or(z.literal('')),
  phone: z.string().trim().max(64).optional().or(z.literal('')),
  email: optionalEmailString,
  note: z.string().trim().max(2000).optional().or(z.literal('')),
});

export type CustomerFormValues = z.infer<typeof customerFormSchema>;
