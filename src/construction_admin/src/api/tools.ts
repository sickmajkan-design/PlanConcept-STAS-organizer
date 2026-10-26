import { request } from './client';
import { idempotencyHeaders } from './idempotency';
import { createCrudApi } from './resource';
import type { ListQuery, Tool, ToolInput, ToolStatus } from './types';

export const toolKitItemStates = ['InKit', 'Missing', 'Broken', 'BrokenHandedIn', 'Charged', 'WrittenOff'] as const;
export type ToolKitItemState = (typeof toolKitItemStates)[number];

export type ToolKitAction =
  | 'ReportMissing'
  | 'ReportBroken'
  | 'ReceiveBroken'
  | 'ChargeWorker'
  | 'WriteOff'
  | 'Restore'
  | 'ReplacementBought';

export type ToolKitPayoutMethod = 'WithPayroll' | 'Cash';

export interface ToolKitItem {
  id: string;
  name: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
  state: ToolKitItemState;
  sortOrder: number;
}

export interface ToolKitEvent {
  id: string;
  at: string;
  itemId: string | null;
  itemName: string;
  type: string;
  amount: number | null;
  employeeId: string | null;
  employeeName: string | null;
  note: string | null;
  /** When the charge was collected or the refund paid. Null while it is open. */
  settledAt: string | null;
  /** For a refund: with the pay or in cash. */
  payoutMethod: ToolKitPayoutMethod | null;
}

export interface ToolKit {
  toolId: string;
  name: string;
  items: ToolKitItem[];
  events: ToolKitEvent[];
  totalValue: number;
  missingValue: number;
  brokenValue: number;
  openCharges: number;
  openRefunds: number;
}

export interface ToolKitItemInput {
  name: string;
  quantity: number;
  unitPrice: number;
}

export interface ToolKitActionInput {
  action: ToolKitAction;
  amount?: number | null;
  employeeId?: string | null;
  note?: string | null;
  /** For a replacement the worker bought: false when the damage was theirs and nothing is refunded. */
  refund?: boolean;
  /** Required when a refund is owed. */
  payoutMethod?: ToolKitPayoutMethod | null;
}

export interface CopyToolInput {
  count: number;
  nameTemplate: string;
  startNumber: number;
  numberWidth: number;
  copyItems: boolean;
  copyPrices: boolean;
  copyBranch: boolean;
  qrCodes?: string[] | null;
}

export interface ToolListQuery extends ListQuery {
  branchId?: string;
  status?: ToolStatus | '';
  category?: string;
  assignedEmployeeId?: string;
  assignedProjectId?: string;
  unassigned?: boolean;
  incompleteOnly?: boolean;
}

export const toolsApi = {
  ...createCrudApi<Tool, Tool, ToolInput, ToolListQuery>('/api/v1/tools'),

  getByQrCode: (qrCode: string) =>
    request<Tool>({
      method: 'GET',
      url: `/api/v1/tools/by-qr/${encodeURIComponent(qrCode)}`,
    }),

  assignEmployee: (id: string, employeeId: string, idempotencyKey?: string) =>
    request<Tool>({
      method: 'POST',
      url: `/api/v1/tools/${id}/assign-employee/${employeeId}`,
      headers: idempotencyHeaders(idempotencyKey),
    }),

  unassignEmployee: (id: string, idempotencyKey?: string) =>
    request<Tool>({
      method: 'POST',
      url: `/api/v1/tools/${id}/unassign-employee`,
      headers: idempotencyHeaders(idempotencyKey),
    }),

  assignProject: (id: string, projectId: string, idempotencyKey?: string) =>
    request<Tool>({
      method: 'POST',
      url: `/api/v1/tools/${id}/assign-project/${projectId}`,
      headers: idempotencyHeaders(idempotencyKey),
    }),

  unassignProject: (id: string, idempotencyKey?: string) =>
    request<Tool>({
      method: 'POST',
      url: `/api/v1/tools/${id}/unassign-project`,
      headers: idempotencyHeaders(idempotencyKey),
    }),

  kit: (id: string) => request<ToolKit>({ method: 'GET', url: `/api/v1/tools/${id}/kit` }),

  addKitItem: (id: string, input: ToolKitItemInput) =>
    request<ToolKit>({ method: 'POST', url: `/api/v1/tools/${id}/kit/items`, data: input }),

  updateKitItem: (id: string, itemId: string, input: ToolKitItemInput) =>
    request<ToolKit>({ method: 'PUT', url: `/api/v1/tools/${id}/kit/items/${itemId}`, data: input }),

  removeKitItem: (id: string, itemId: string) =>
    request<ToolKit>({ method: 'DELETE', url: `/api/v1/tools/${id}/kit/items/${itemId}` }),

  actOnKitItem: (id: string, itemId: string, input: ToolKitActionInput, idempotencyKey?: string) =>
    request<ToolKit>({
      method: 'POST',
      url: `/api/v1/tools/${id}/kit/items/${itemId}/actions`,
      data: input,
      headers: idempotencyHeaders(idempotencyKey),
    }),

  settleKitEvent: (id: string, eventId: string, idempotencyKey?: string) =>
    request<ToolKit>({
      method: 'POST',
      url: `/api/v1/tools/${id}/kit/events/${eventId}/settle`,
      headers: idempotencyHeaders(idempotencyKey),
    }),

  copy: (id: string, input: CopyToolInput, idempotencyKey?: string) =>
    request<Tool[]>({
      method: 'POST',
      url: `/api/v1/tools/${id}/copy`,
      data: input,
      headers: idempotencyHeaders(idempotencyKey),
    }),
};
