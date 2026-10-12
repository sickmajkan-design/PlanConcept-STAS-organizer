import { request } from './client';

export const recurringFrequencies = ['Monthly', 'Quarterly', 'Annual'] as const;
export type RecurringFrequency = (typeof recurringFrequencies)[number];

export interface RecurringExpense {
  id: string;
  name: string;
  categoryId: string;
  categoryName: string;
  amount: number;
  frequency: RecurringFrequency;
  /** The first day of the first month it applies to. */
  startMonth: string;
  dayOfMonth: number;
  projectId: string | null;
  projectName: string | null;
  countryCode: string | null;
  isActive: boolean;
  endDate: string | null;
  branchId: string | null;
  /** How many entries of the book it has made so far. */
  entriesMade: number;
}

export interface RecurringExpenseInput {
  name: string;
  categoryId: string;
  amount: number;
  frequency: RecurringFrequency;
  startMonth: string;
  dayOfMonth: number;
  projectId?: string | null;
  countryCode?: string | null;
  endDate?: string | null;
  branchId?: string | null;
}

export interface RecurringPreviewRow {
  recurringExpenseId: string;
  name: string;
  categoryName: string;
  amount: number;
  dueDate: string;
  entryId: string | null;
  alreadyMade: boolean;
}

export const recurringApi = {
  list: () => request<RecurringExpense[]>({ method: 'GET', url: '/api/v1/book/recurring' }),

  create: (input: RecurringExpenseInput) =>
    request<RecurringExpense>({ method: 'POST', url: '/api/v1/book/recurring', data: input }),

  update: (id: string, input: RecurringExpenseInput) =>
    request<RecurringExpense>({ method: 'PUT', url: `/api/v1/book/recurring/${id}`, data: input }),

  toggle: (id: string) =>
    request<RecurringExpense>({ method: 'POST', url: `/api/v1/book/recurring/${id}/toggle` }),

  preview: (year: number, month: number) =>
    request<RecurringPreviewRow[]>({
      method: 'GET',
      url: '/api/v1/book/recurring/preview',
      params: { year, month },
    }),

  generate: (year: number, month: number) =>
    request<number>({
      method: 'POST',
      url: '/api/v1/book/recurring/generate',
      params: { year, month },
    }),
};
