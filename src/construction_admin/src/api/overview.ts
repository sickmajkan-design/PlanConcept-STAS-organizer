import { request } from './client';

export interface OverviewMonth {
  year: number;
  month: number;
  hasSettlement: boolean;
  /** Every settlement of the month is closed, so the figures are the ones kept at closing. */
  locked: boolean;
  billing: number;
  otherIncome: number;
  workersEarned: number;
  subcontractorsEarned: number;
  officeEarned: number;
  siteCosts: number;
  fixedCosts: number;
  hours: number;
  /** The month's costs by kind; see the cost labels of the book. */
  costs: Record<string, number>;
  income: number;
  outflow: number;
  profit: number;
}

export type OverviewStepKey =
  | 'Billing'
  | 'OtherIncome'
  | 'Workers'
  | 'Subcontractors'
  | 'Office'
  | 'SiteCosts'
  | 'FixedCosts'
  | 'Profit';

export interface OverviewStep {
  key: OverviewStepKey;
  amount: number;
  /** Share of the period's income, in percent; null when there is no income. */
  sharePercent: number | null;
}

export interface Overview {
  from: string;
  to: string;
  /** False for an account that sees statistics only: shares and margins, amounts are zero. */
  amountsVisible: boolean;
  income: number;
  outflow: number;
  profit: number;
  hours: number;
  profitPerHour: number | null;
  marginPercent: number | null;
  previousProfit: number;
  previousIncome: number;
  steps: OverviewStep[];
  months: OverviewMonth[];
  bestMonth: OverviewMonth | null;
  worstMonth: OverviewMonth | null;
  /** Months of the period whose settlement has not been closed. */
  openMonths: number;
  averageMonthlyProfit: number;
  /** Where the money goes: costs by kind, biggest first. */
  costsByKind: { key: string; amount: number; sharePercent: number | null }[];
}

export interface OverviewAttention {
  kind: 'LateInvoice' | 'OpenMonth' | 'Checks';
  count: number;
  amount: number;
  reference: string | null;
  days: number;
}

export interface CashflowDay {
  date: string;
  inflow: number;
  outflow: number;
  balance: number;
}

export interface CashflowEvent {
  date: string;
  kind: 'Invoice' | 'Income' | 'Cost' | 'Payroll';
  what: string | null;
  who: string | null;
  /** Positive for money in, negative for money out. */
  amount: number;
  balanceAfter: number;
  /** The day is an assumption. */
  estimated: boolean;
}

export interface Cashflow {
  from: string;
  days: number;
  openingBalance: number;
  totalInflow: number;
  totalOutflow: number;
  closingBalance: number;
  events: CashflowEvent[];
  lowestBalance: number;
  lowestDate: string;
  belowZero: boolean;
  series: CashflowDay[];
}

export interface Forecast {
  hasData: boolean;
  monthsUsed: number;
  hours: number;
  averageHours: number;
  incomePerHour: number;
  costPerHour: number;
  workers: number;
  subcontractors: number;
  siteCosts: number;
  office: number;
  fixed: number;
  lastMonthProfit: number;
  income: number;
  outflow: number;
  profit: number;
  profitLow: number;
  profitHigh: number;
}

export const overviewApi = {
  get: (query: { from: string; to: string; branchId?: string }) =>
    request<Overview>({
      method: 'GET',
      url: '/api/v1/overview',
      params: { from: query.from, to: query.to, ...(query.branchId ? { branchId: query.branchId } : {}) },
    }),

  cashflow: (query: { days?: number; openingBalance?: number; lateDays?: number; branchId?: string }) =>
    request<Cashflow>({
      method: 'GET',
      url: '/api/v1/overview/cashflow',
      params: {
        days: query.days ?? 35,
        openingBalance: query.openingBalance ?? 0,
        lateDays: query.lateDays ?? 14,
        ...(query.branchId ? { branchId: query.branchId } : {}),
      },
    }),

  attention: (query: { branchId?: string } = {}) =>
    request<OverviewAttention[]>({
      method: 'GET',
      url: '/api/v1/overview/attention',
      params: query.branchId ? { branchId: query.branchId } : {},
    }),

  forecast: (query: { hours?: number; branchId?: string } = {}) =>
    request<Forecast>({
      method: 'GET',
      url: '/api/v1/overview/forecast',
      params: {
        ...(query.hours !== undefined ? { hours: query.hours } : {}),
        ...(query.branchId ? { branchId: query.branchId } : {}),
      },
    }),
};
