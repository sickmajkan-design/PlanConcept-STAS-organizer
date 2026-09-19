import { z } from 'zod';

import { zodMsg } from '../../i18n/zodMessage';

const quantityString = z
  .string()
  .min(1, { error: zodMsg('validation.required') })
  .refine((value) => !Number.isNaN(Number(value)), { error: zodMsg('validation.mustBeNumber') })
  .refine((value) => Number(value) >= 0, { error: zodMsg('validation.numberPositive') });

const optionalPriceString = z
  .string()
  .refine((value) => value === '' || !Number.isNaN(Number(value)), {
    error: zodMsg('validation.mustBeNumber'),
  })
  .refine((value) => value === '' || Number(value) >= 0, {
    error: zodMsg('validation.numberPositive'),
  });

/** Mirrors the API's MaterialCommandBaseValidator so the form catches errors early. */
export const materialFormSchema = z.object({
  name: z
    .string()
    .trim()
    .min(1, { error: zodMsg('validation.required') })
    .max(256, { error: zodMsg('validation.maxLength', { max: 256 }) }),
  unit: z
    .string()
    .trim()
    .min(1, { error: zodMsg('validation.required') })
    .max(32, { error: zodMsg('validation.maxLength', { max: 32 }) }),
  quantity: quantityString,
  warehouse: z
    .string()
    .trim()
    .max(256, { error: zodMsg('validation.maxLength', { max: 256 }) })
    .optional()
    .or(z.literal('')),
  unitPrice: optionalPriceString,
  minimumQuantity: optionalPriceString,
  projectId: z.string().optional().or(z.literal('')),
  // The delivery the starting stock came in on (new material only).
  receivedOn: z.string().optional().or(z.literal('')),
  supplier: z
    .string()
    .trim()
    .max(200, { error: zodMsg('validation.maxLength', { max: 200 }) })
    .optional()
    .or(z.literal('')),
  invoiceNumber: z
    .string()
    .trim()
    .max(100, { error: zodMsg('validation.maxLength', { max: 100 }) })
    .optional()
    .or(z.literal('')),
  purchaseUnitPrice: optionalPriceString,
  receiptNote: z
    .string()
    .trim()
    .max(500, { error: zodMsg('validation.maxLength', { max: 500 }) })
    .optional()
    .or(z.literal('')),
})
  // Same rule the API applies: a supplier or a price means it was bought, and
  // something bought has an invoice or receipt behind it.
  .refine(
    (values) =>
      Number(values.quantity) <= 0 ||
      (!values.supplier && !values.purchaseUnitPrice) ||
      !!values.invoiceNumber,
    { path: ['invoiceNumber'], error: zodMsg('movements.needsInvoiceNumber') },
  );

export type MaterialFormValues = z.infer<typeof materialFormSchema>;

/** Mirrors the API's AdjustMaterialQuantityCommandValidator. */
export const adjustMaterialSchema = z.object({
  change: z
    .string()
    .min(1, { error: zodMsg('validation.required') })
    .refine((value) => !Number.isNaN(Number(value)), { error: zodMsg('validation.mustBeNumber') })
    .refine((value) => Number(value) !== 0, { error: zodMsg('validation.changeNotZero') }),
  reason: z
    .string()
    .trim()
    .max(512, { error: zodMsg('validation.maxLength', { max: 512 }) })
    .optional()
    .or(z.literal('')),
});

export type AdjustMaterialFormValues = z.infer<typeof adjustMaterialSchema>;
