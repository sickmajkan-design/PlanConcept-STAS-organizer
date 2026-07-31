import { z } from 'zod';

const quantityString = z
  .string()
  .min(1, 'Quantity is required.')
  .refine((value) => !Number.isNaN(Number(value)), { message: 'Must be a number.' })
  .refine((value) => Number(value) >= 0, { message: 'Quantity must not be negative.' });

/** Mirrors the API's MaterialCommandBaseValidator so the form catches errors early. */
export const materialFormSchema = z.object({
  name: z.string().trim().min(1, 'Material name is required.').max(256),
  unit: z.string().trim().min(1, 'Unit of measure is required.').max(32),
  quantity: quantityString,
  warehouse: z.string().trim().max(256).optional().or(z.literal('')),
  projectId: z.string().optional().or(z.literal('')),
});

export type MaterialFormValues = z.infer<typeof materialFormSchema>;

/** Mirrors the API's AdjustMaterialQuantityCommandValidator. */
export const adjustMaterialSchema = z.object({
  change: z
    .string()
    .min(1, 'Change is required.')
    .refine((value) => !Number.isNaN(Number(value)), { message: 'Must be a number.' })
    .refine((value) => Number(value) !== 0, { message: 'Change must not be zero.' }),
  reason: z.string().trim().max(512).optional().or(z.literal('')),
});

export type AdjustMaterialFormValues = z.infer<typeof adjustMaterialSchema>;
