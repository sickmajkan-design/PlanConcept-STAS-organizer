import { z } from 'zod';

import { zodMsg } from '../../i18n/zodMessage';
import { fuelTypes, vehicleOwnershipTypes, vehicleStatuses } from '../../api/types';

/** Mirrors the API's VehicleCommandBaseValidator so the form catches errors early. */
export const vehicleFormSchema = z.object({
  brand: z
    .string()
    .trim()
    .min(1, { error: zodMsg('validation.required') })
    .max(100, { error: zodMsg('validation.maxLength', { max: 100 }) }),
  model: z
    .string()
    .trim()
    .min(1, { error: zodMsg('validation.required') })
    .max(100, { error: zodMsg('validation.maxLength', { max: 100 }) }),
  registrationNumber: z
    .string()
    .trim()
    .min(1, { error: zodMsg('validation.required') })
    .max(32, { error: zodMsg('validation.maxLength', { max: 32 }) }),
  vin: z
    .string()
    .trim()
    .max(32, { error: zodMsg('validation.maxLength', { max: 32 }) })
    .optional()
    .or(z.literal('')),
  qrCode: z
    .string()
    .trim()
    .max(256, { error: zodMsg('validation.maxLength', { max: 256 }) })
    .optional()
    .or(z.literal('')),
  gpsProvider: z
    .string()
    .trim()
    .max(100, { error: zodMsg('validation.maxLength', { max: 100 }) })
    .optional()
    .or(z.literal('')),
  gpsTrackingUrl: z
    .string()
    .trim()
    .max(1000, { error: zodMsg('validation.maxLength', { max: 1000 }) })
    .url({ error: zodMsg('validation.urlInvalid') })
    .optional()
    .or(z.literal('')),
  fuelType: z.enum(fuelTypes, { error: zodMsg('validation.fuelTypeRequired') }),
  status: z.enum(vehicleStatuses),
  ownershipType: z.enum(vehicleOwnershipTypes),
});

export type VehicleFormValues = z.infer<typeof vehicleFormSchema>;
