import { z } from 'zod';

import { zodMsg } from '../../i18n/zodMessage';

/** Mirrors the API's AccommodationCommandBaseValidator so the form catches errors early. */
export const accommodationFormSchema = z.object({
  address: z
    .string()
    .trim()
    .min(1, { error: zodMsg('validation.required') })
    .max(512, { error: zodMsg('validation.maxLength', { max: 512 }) }),
  note: z
    .string()
    .trim()
    .max(2000, { error: zodMsg('validation.maxLength', { max: 2000 }) })
    .optional()
    .or(z.literal('')),
});

export type AccommodationFormValues = z.infer<typeof accommodationFormSchema>;
