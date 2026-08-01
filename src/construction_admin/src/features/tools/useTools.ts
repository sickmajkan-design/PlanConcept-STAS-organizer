import { toolsApi, type ToolListQuery } from '../../api/tools';
import type { Tool, ToolInput } from '../../api/types';
import {
  createResourceKeys,
  useResourceDetail,
  useResourceList,
  useResourceMutation,
} from '../resourceQueries';

export const toolKeys = createResourceKeys<ToolListQuery>('tools');

export function useToolsQuery(query: ToolListQuery) {
  return useResourceList(toolKeys, toolsApi.list, query);
}

export function useToolQuery(id: string | undefined) {
  return useResourceDetail(toolKeys, toolsApi.get, id);
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
    (employeeId: string) => toolsApi.assignEmployee(id, employeeId),
    [toolKeys.all],
  );
}

// `void` is explicit: the callback takes no argument, so there is nothing for
// the variables type to be inferred from, and the call site invokes `mutate()`.
export function useUnassignToolEmployee(id: string) {
  return useResourceMutation<void, Tool>(() => toolsApi.unassignEmployee(id), [
    toolKeys.all,
  ]);
}

export function useAssignToolProject(id: string) {
  return useResourceMutation(
    (projectId: string) => toolsApi.assignProject(id, projectId),
    [toolKeys.all],
  );
}

export function useUnassignToolProject(id: string) {
  return useResourceMutation<void, Tool>(() => toolsApi.unassignProject(id), [
    toolKeys.all,
  ]);
}
