import { z } from 'zod';

import { fuelTypes, vehicleStatuses } from '../../api/types';

/** Mirrors the API's VehicleCommandBaseValidator so the form catches errors early. */
export const vehicleFormSchema = z.object({
  brand: z.string().trim().min(1, 'Brand is required.').max(100),
  model: z.string().trim().min(1, 'Model is required.').max(100),
  registrationNumber: z
    .string()
    .trim()
    .min(1, 'Registration number is required.')
    .max(32),
  vin: z.string().trim().max(32).optional().or(z.literal('')),
  qrCode: z.string().trim().max(256).optional().or(z.literal('')),
  fuelType: z.enum(fuelTypes, { message: 'Fuel type is required.' }),
  status: z.enum(vehicleStatuses),
});

export type VehicleFormValues = z.infer<typeof vehicleFormSchema>;
