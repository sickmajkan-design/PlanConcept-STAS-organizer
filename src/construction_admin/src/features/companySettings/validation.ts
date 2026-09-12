import { z } from 'zod';

import { zodMsg } from '../../i18n/zodMessage';

const optionalEmailString = z
  .string()
  .trim()
  .max(256, { error: zodMsg('validation.maxLength', { max: 256 }) })
  .refine((value) => value === '' || /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value), {
    error: zodMsg('validation.emailInvalid'),
  });

/** Mirrors the API's UpdateCompanySettingsCommandValidator so the form catches errors early. */
export const companySettingsFormSchema = z.object({
  name: z
    .string()
    .trim()
    .min(1, { error: zodMsg('validation.required') })
    .max(200, { error: zodMsg('validation.maxLength', { max: 200 }) }),
  address: z
    .string()
    .trim()
    .max(500, { error: zodMsg('validation.maxLength', { max: 500 }) })
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
  phone: z
    .string()
    .trim()
    .max(32, { error: zodMsg('validation.maxLength', { max: 32 }) })
    .optional()
    .or(z.literal('')),
  email: optionalEmailString,
  weeklyReportsForwardEmail: optionalEmailString,
});

export type CompanySettingsFormValues = z.infer<typeof companySettingsFormSchema>;
