import { z } from 'zod';

import { employeeStatuses } from '../../api/types';

/** Mirrors the API's EmployeeCommandBaseValidator so the form catches errors early. */
export const employeeFormSchema = z
  .object({
    employeeNumber: z.string().trim().min(1, 'Employee number is required.').max(32),
    firstName: z.string().trim().min(1, 'First name is required.').max(100),
    lastName: z.string().trim().min(1, 'Last name is required.').max(100),
    phone: z.string().trim().max(32).optional().or(z.literal('')),
    email: z
      .string()
      .trim()
      .max(256)
      .optional()
      .or(z.literal(''))
      .refine((value) => !value || z.string().email().safeParse(value).success, {
        message: 'Email is not a valid email address.',
      }),
    address: z.string().trim().max(512).optional().or(z.literal('')),
    dateOfBirth: z.string().optional().or(z.literal('')),
    employmentDate: z.string().min(1, 'Employment date is required.'),
    position: z.string().trim().min(1, 'Position is required.').max(128),
    status: z.enum(employeeStatuses),
    photoUrl: z.string().trim().max(1024).optional().or(z.literal('')),
  })
  .refine(
    (values) => {
      if (!values.dateOfBirth) return true;
      return new Date(values.dateOfBirth) < new Date();
    },
    { message: 'Date of birth must be in the past.', path: ['dateOfBirth'] },
  )
  .refine(
    (values) => {
      if (!values.dateOfBirth || !values.employmentDate) return true;
      return new Date(values.dateOfBirth) < new Date(values.employmentDate);
    },
    {
      message: 'Date of birth must be before the employment date.',
      path: ['dateOfBirth'],
    },
  );

export type EmployeeFormValues = z.infer<typeof employeeFormSchema>;
