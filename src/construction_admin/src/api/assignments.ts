import { request } from './client';
import type { AssignmentBoard } from './types';

/**
 * The one read behind the drag-and-drop board. Assigning and removing reuse
 * `employeesApi.assignToProject` / `removeFromProject` — this module only
 * reads the combined picture those writes change.
 */
export const assignmentsApi = {
  board: () => request<AssignmentBoard>({ method: 'GET', url: '/api/v1/assignment-board' }),
};
