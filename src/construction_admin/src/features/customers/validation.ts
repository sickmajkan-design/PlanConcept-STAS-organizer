import { z } from 'zod';

import { zodMsg } from '../../i18n/zodMessage';

const optionalEmailString = z
  .string()
  .trim()
  .max(256, { error: zodMsg('validation.maxLength', { max: 256 }) })
  .refine((value) => value === '' || /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value), {
    error: zodMsg('validation.emailInvalid'),
  });

/** Mirrors the API's CustomerCommandBaseValidator so the form catches errors early. */
export const customerFormSchema = z.object({
  name: z
    .string()
    .trim()
    .min(1, { error: zodMsg('validation.required') })
    .max(256, { error: zodMsg('validation.maxLength', { max: 256 }) }),
  contactPerson: z
    .string()
    .trim()
    .max(256, { error: zodMsg('validation.maxLength', { max: 256 }) })
    .optional()
    .or(z.literal('')),
  phone: z
    .string()
    .trim()
    .max(64, { error: zodMsg('validation.maxLength', { max: 64 }) })
    .optional()
    .or(z.literal('')),
  email: optionalEmailString,
  note: z
    .string()
    .trim()
    .max(2000, { error: zodMsg('validation.maxLength', { max: 2000 }) })
    .optional()
    .or(z.literal('')),
  taxId: z
    .string()
    .trim()
    .max(64, { error: zodMsg('validation.maxLength', { max: 64 }) })
    .optional()
    .or(z.literal('')),
  registrationNumber: z
    .string()
    .trim()
    .max(64, { error: zodMsg('validation.maxLength', { max: 64 }) })
    .optional()
    .or(z.literal('')),
  vatNumber: z
    .string()
    .trim()
    .max(64, { error: zodMsg('validation.maxLength', { max: 64 }) })
    .optional()
    .or(z.literal('')),
});

export type CustomerFormValues = z.infer<typeof customerFormSchema>;
