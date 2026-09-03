import { z } from 'zod';

/** Mirrors the API's AccommodationCommandBaseValidator so the form catches errors early. */
export const accommodationFormSchema = z.object({
  address: z.string().trim().min(1, 'An address is required.').max(512),
  note: z.string().trim().max(2000).optional().or(z.literal('')),
});

export type AccommodationFormValues = z.infer<typeof accommodationFormSchema>;
