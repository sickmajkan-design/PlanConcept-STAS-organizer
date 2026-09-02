import { useQuery } from '@tanstack/react-query';

import { toolsApi, type ToolListQuery } from '../../api/tools';
import type { Tool, ToolInput } from '../../api/types';
import {
  createResourceKeys,
  useResourceDetail,
  useResourceList,
  useResourceMutation,
} from '../resourceQueries';

export const toolKeys = createResourceKeys<ToolListQuery>('tools');

// Written as literals rather than importing `employeeKeys`/`assignmentBoardKeys`
// from their own feature modules — those modules import `toolKeys` from here,
// and importing back would make them circular.
const employeeKey = ['employees'] as const;
const assignmentBoardKey = ['assignmentBoard'] as const;

// Assigning or unassigning a tool's employee changes what that employee's
// card shows on the Assignment Board (and, once a project owns the tool via
// EmployeeEquipmentSync, what their project crew list shows too).
const employeeAssignmentCaches = [employeeKey, assignmentBoardKey];

/** The largest page the API will serve, used by the picker query below. */
const PICKER_QUERY: ToolListQuery = {
  pageNumber: 1,
  pageSize: 100,
};

export function useToolsQuery(query: ToolListQuery) {
  return useResourceList(toolKeys, toolsApi.list, query);
}

export function useToolQuery(id: string | undefined) {
  return useResourceDetail(toolKeys, toolsApi.get, id);
}

/** All tools for the expense picker. Cached like the other pickers. */
export function useAllToolsQuery() {
  return useQuery({
    queryKey: toolKeys.list(PICKER_QUERY),
    queryFn: () => toolsApi.list(PICKER_QUERY),
    staleTime: 60_000,
  });
}

export function useCreateTool() {
  return useResourceMutation(
    (input: ToolInput) => toolsApi.create(input),
    [toolKeys.all],
  );
}

export function useUpdateTool(id: string) {
  return useResourceMutation(
    (input: ToolInput) => toolsApi.update(id, input),
    [toolKeys.all],
  );
}

export function useDeleteTool() {
  return useResourceMutation((id: string) => toolsApi.remove(id), [
    toolKeys.all,
  ]);
}

export function useAssignToolEmployee(id: string) {
  return useResourceMutation(
    (employeeId: string, key: string) => toolsApi.assignEmployee(id, employeeId, key),
    [toolKeys.all, ...employeeAssignmentCaches],
  );
}

// `void` is explicit: the callback takes no argument, so there is nothing for
// the variables type to be inferred from, and the call site invokes `mutate()`.
export function useUnassignToolEmployee(id: string) {
  return useResourceMutation<void, Tool>((_, key) => toolsApi.unassignEmployee(id, key), [
    toolKeys.all,
    ...employeeAssignmentCaches,
  ]);
}

export function useAssignToolProject(id: string) {
  return useResourceMutation(
    (projectId: string, key: string) => toolsApi.assignProject(id, projectId, key),
    [toolKeys.all, ...employeeAssignmentCaches],
  );
}

export function useUnassignToolProject(id: string) {
  return useResourceMutation<void, Tool>((_, key) => toolsApi.unassignProject(id, key), [
    toolKeys.all,
    ...employeeAssignmentCaches,
  ]);
}

// Same relationship, initiated from the project's side: the project is fixed,
// the tool is chosen — the reverse of the two hooks above.
export function useAssignProjectTool(projectId: string) {
  return useResourceMutation(
    (toolId: string, key: string) => toolsApi.assignProject(toolId, projectId, key),
    [toolKeys.all, ...employeeAssignmentCaches],
  );
}

export function useUnassignProjectTool() {
  return useResourceMutation(
    (toolId: string, key: string) => toolsApi.unassignProject(toolId, key),
    [toolKeys.all, ...employeeAssignmentCaches],
  );
}
