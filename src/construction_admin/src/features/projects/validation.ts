import { z } from 'zod';

import { zodMsg } from '../../i18n/zodMessage';
import { projectStatuses } from '../../api/types';

const optionalCoordinate = z
  .string()
  .optional()
  .or(z.literal(''))
  .refine((value) => !value || !Number.isNaN(Number(value)), {
    error: zodMsg('validation.mustBeNumber'),
  });

const optionalNonNegativeAmount = z
  .string()
  .optional()
  .or(z.literal(''))
  .refine((value) => !value || (!Number.isNaN(Number(value)) && Number(value) >= 0), {
    error: zodMsg('validation.contractValueNegative'),
  });

/** Mirrors the API's ProjectCommandBaseValidator so the form catches errors early. */
export const projectFormSchema = z
  .object({
    name: z
      .string()
      .trim()
      .min(1, { error: zodMsg('validation.required') })
      .max(256, { error: zodMsg('validation.maxLength', { max: 256 }) }),
    description: z
      .string()
      .trim()
      .max(4000, { error: zodMsg('validation.maxLength', { max: 4000 }) })
      .optional()
      .or(z.literal('')),
    customerId: z.string().optional().or(z.literal('')),
    parentProjectId: z.string().optional().or(z.literal('')),
    address: z
      .string()
      .trim()
      .max(512, { error: zodMsg('validation.maxLength', { max: 512 }) })
      .optional()
      .or(z.literal('')),
    countryCode: z.string().optional().or(z.literal('')),
    latitude: optionalCoordinate,
    longitude: optionalCoordinate,
    shiftStartTime: z.string().optional().or(z.literal('')),
    startDate: z.string().optional().or(z.literal('')),
    endDate: z.string().optional().or(z.literal('')),
    status: z.enum(projectStatuses),
    contractValue: optionalNonNegativeAmount,
  })
  .refine((values) => Boolean(values.latitude) === Boolean(values.longitude), {
    error: zodMsg('validation.latLngTogether'),
    path: ['latitude'],
  })
  .refine(
    (values) => {
      if (!values.latitude) return true;
      const lat = Number(values.latitude);
      return lat >= -90 && lat <= 90;
    },
    { error: zodMsg('validation.latitudeRange'), path: ['latitude'] },
  )
  .refine(
    (values) => {
      if (!values.longitude) return true;
      const lng = Number(values.longitude);
      return lng >= -180 && lng <= 180;
    },
    { error: zodMsg('validation.longitudeRange'), path: ['longitude'] },
  )
  .refine(
    (values) => {
      if (!values.startDate || !values.endDate) return true;
      return new Date(values.endDate) >= new Date(values.startDate);
    },
    { error: zodMsg('validation.endDateBeforeStart'), path: ['endDate'] },
  );

export type ProjectFormValues = z.infer<typeof projectFormSchema>;
