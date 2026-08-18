import { useQuery } from '@tanstack/react-query';

import { assignmentsApi } from '../../api/assignments';
import { employeesApi } from '../../api/employees';
import { employeeKeys } from '../employees/useEmployees';
import { projectKeys } from '../projects/useProjects';
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
 * A drop onto a lane and a chip's remove button both touch the same three
 * caches: the board itself, and the employee/project detail screens a person
 * might have open elsewhere, which show the same postings a different way.
 */
const boardCaches = [assignmentBoardKeys.all, employeeKeys.all, projectKeys.all];

export function useAssignOnBoard() {
  return useResourceMutation(
    (
      { employeeId, projectId }: { employeeId: string; projectId: string },
      key: string,
    ) => employeesApi.assignToProject(employeeId, projectId, key),
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
