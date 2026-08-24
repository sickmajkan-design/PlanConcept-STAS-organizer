import { useQuery } from '@tanstack/react-query';

import { assignmentsApi } from '../../api/assignments';
import { employeesApi } from '../../api/employees';
import { employeeKeys } from '../employees/useEmployees';
import { projectKeys } from '../projects/useProjects';
import { toolKeys } from '../tools/useTools';
import { vehicleKeys } from '../vehicles/useVehicles';
import { useResourceMutation } from '../resourceQueries';

export const assignmentBoardKeys = {
  all: ['assignmentBoard'] as const,
};

export function useAssignmentBoardQuery() {
  return useQuery({
    queryKey: assignmentBoardKeys.all,
    queryFn: assignmentsApi.board,
  });
}

/**
 * A drop onto a lane and a chip's remove button both touch the same caches:
 * the board itself, the employee/project detail screens a person might have
 * open elsewhere, and Tools/Vehicles — the API moves an employee's held
 * equipment onto their new project as part of the same operation, so those
 * lists would otherwise show a stale project until something else refreshed
 * them.
 */
const boardCaches = [
  assignmentBoardKeys.all,
  employeeKeys.all,
  projectKeys.all,
  toolKeys.all,
  vehicleKeys.all,
];

export interface AssignOnBoardInput {
  employeeId: string;
  projectId: string;
  startDate?: string | null;
  endDate?: string | null;
}

export function useAssignOnBoard() {
  return useResourceMutation(
    ({ employeeId, projectId, startDate, endDate }: AssignOnBoardInput, key: string) =>
      employeesApi.assignToProject(employeeId, projectId, key, { startDate, endDate }),
    boardCaches,
  );
}

export function useRemoveOnBoard() {
  return useResourceMutation(
    ({ employeeId, projectId }: { employeeId: string; projectId: string }) =>
      employeesApi.removeFromProject(employeeId, projectId),
    boardCaches,
  );
}
