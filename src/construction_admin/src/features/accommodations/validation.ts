import { z } from 'zod';

import { zodMsg } from '../../i18n/zodMessage';

const optionalText = (max: number) =>
  z
    .string()
    .trim()
    .max(max, { error: zodMsg('validation.maxLength', { max }) })
    .optional()
    .or(z.literal(''));

const optionalPositiveNumber = z
  .string()
  .refine((value) => value === '' || !Number.isNaN(Number(value)), {
    error: zodMsg('validation.mustBeNumber'),
  })
  .refine((value) => value === '' || Number(value) > 0, {
    error: zodMsg('validation.numberPositive'),
  });

/** Mirrors the API's AccommodationCommandBaseValidator so the form catches errors early. */
export const accommodationFormSchema = z
  .object({
    address: z
      .string()
      .trim()
      .min(1, { error: zodMsg('validation.required') })
      .max(512, { error: zodMsg('validation.maxLength', { max: 512 }) }),
    name: optionalText(200),
    type: z.enum(['Apartment', 'House', 'Room', 'Hotel', 'Other']),
    city: optionalText(120),
    floor: optionalText(40),
    rooms: optionalPositiveNumber,
    beds: optionalPositiveNumber,
    areaSquareMeters: optionalPositiveNumber,
    landlordName: optionalText(200),
    landlordPhone: optionalText(60),
    landlordEmail: z
      .string()
      .trim()
      .max(200, { error: zodMsg('validation.maxLength', { max: 200 }) })
      .refine((value) => value === '' || /^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(value), {
        error: zodMsg('validation.emailInvalid'),
      })
      .optional()
      .or(z.literal('')),
    contractNumber: optionalText(100),
    contractStart: z.string().optional().or(z.literal('')),
    contractEnd: z.string().optional().or(z.literal('')),
    depositAmount: z
      .string()
      .refine((value) => value === '' || !Number.isNaN(Number(value)), {
        error: zodMsg('validation.mustBeNumber'),
      })
      .refine((value) => value === '' || Number(value) >= 0, {
        error: zodMsg('validation.numberPositive'),
      }),
    utilitiesIncluded: z.boolean(),
    isActive: z.boolean(),
    note: optionalText(2000),
  })
  .refine(
    (values) => !values.contractStart || !values.contractEnd || values.contractEnd >= values.contractStart,
    { path: ['contractEnd'], error: zodMsg('accommodations.contractEndsBeforeStart') },
  );

export type AccommodationFormValues = z.infer<typeof accommodationFormSchema>;
