import { z } from 'zod';

import { zodMsg } from '../../i18n/zodMessage';
import { employeeStatuses, employeeTypes } from '../../api/types';

/** Mirrors the API's EmployeeCommandBaseValidator so the form catches errors early. */
export const employeeFormSchema = z
  .object({
    employeeNumber: z
      .string()
      .trim()
      .min(1, { error: zodMsg('validation.required') })
      .max(32, { error: zodMsg('validation.maxLength', { max: 32 }) }),
    firstName: z
      .string()
      .trim()
      .min(1, { error: zodMsg('validation.required') })
      .max(100, { error: zodMsg('validation.maxLength', { max: 100 }) }),
    lastName: z
      .string()
      .trim()
      .min(1, { error: zodMsg('validation.required') })
      .max(100, { error: zodMsg('validation.maxLength', { max: 100 }) }),
    phone: z
      .string()
      .trim()
      .max(32, { error: zodMsg('validation.maxLength', { max: 32 }) })
      .optional()
      .or(z.literal('')),
    email: z
      .string()
      .trim()
      .max(256, { error: zodMsg('validation.maxLength', { max: 256 }) })
      .optional()
      .or(z.literal(''))
      .refine((value) => !value || z.string().email().safeParse(value).success, {
        error: zodMsg('validation.emailInvalid'),
      }),
    address: z
      .string()
      .trim()
      .max(512, { error: zodMsg('validation.maxLength', { max: 512 }) })
      .optional()
      .or(z.literal('')),
    dateOfBirth: z.string().optional().or(z.literal('')),
    employmentDate: z.string().min(1, { error: zodMsg('validation.required') }),
    position: z
      .string()
      .trim()
      .min(1, { error: zodMsg('validation.required') })
      .max(128, { error: zodMsg('validation.maxLength', { max: 128 }) }),
    status: z.enum(employeeStatuses),
    type: z.enum(employeeTypes),
  })
  .refine(
    (values) => {
      if (!values.dateOfBirth) return true;
      return new Date(values.dateOfBirth) < new Date();
    },
    { error: zodMsg('validation.dateOfBirthPast'), path: ['dateOfBirth'] },
  )
  .refine(
    (values) => {
      if (!values.dateOfBirth || !values.employmentDate) return true;
      return new Date(values.dateOfBirth) < new Date(values.employmentDate);
    },
    {
      error: zodMsg('validation.dateOfBirthBeforeEmployment'),
      path: ['dateOfBirth'],
    },
  );

export type EmployeeFormValues = z.infer<typeof employeeFormSchema>;
