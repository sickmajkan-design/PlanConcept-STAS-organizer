import { z } from 'zod';

import { projectStatuses } from '../../api/types';

const optionalCoordinate = z
  .string()
  .optional()
  .or(z.literal(''))
  .refine((value) => !value || !Number.isNaN(Number(value)), {
    message: 'Must be a number.',
  });

const optionalNonNegativeAmount = z
  .string()
  .optional()
  .or(z.literal(''))
  .refine((value) => !value || (!Number.isNaN(Number(value)) && Number(value) >= 0), {
    message: 'Contract value cannot be negative.',
  });

/** Mirrors the API's ProjectCommandBaseValidator so the form catches errors early. */
export const projectFormSchema = z
  .object({
    name: z.string().trim().min(1, 'Project name is required.').max(256),
    description: z.string().trim().max(4000).optional().or(z.literal('')),
    client: z.string().trim().max(256).optional().or(z.literal('')),
    address: z.string().trim().max(512).optional().or(z.literal('')),
    latitude: optionalCoordinate,
    longitude: optionalCoordinate,
    startDate: z.string().optional().or(z.literal('')),
    endDate: z.string().optional().or(z.literal('')),
    status: z.enum(projectStatuses),
    contractValue: optionalNonNegativeAmount,
  })
  .refine((values) => Boolean(values.latitude) === Boolean(values.longitude), {
    message: 'Latitude and longitude must be provided together.',
    path: ['latitude'],
  })
  .refine(
    (values) => {
      if (!values.latitude) return true;
      const lat = Number(values.latitude);
      return lat >= -90 && lat <= 90;
    },
    { message: 'Latitude must be between -90 and 90.', path: ['latitude'] },
  )
  .refine(
    (values) => {
      if (!values.longitude) return true;
      const lng = Number(values.longitude);
      return lng >= -180 && lng <= 180;
    },
    { message: 'Longitude must be between -180 and 180.', path: ['longitude'] },
  )
  .refine(
    (values) => {
      if (!values.startDate || !values.endDate) return true;
      return new Date(values.endDate) >= new Date(values.startDate);
    },
    { message: 'End date must not be before the start date.', path: ['endDate'] },
  );

export type ProjectFormValues = z.infer<typeof projectFormSchema>;
