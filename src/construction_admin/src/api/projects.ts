import { createCrudApi } from './resource';
import type {
  ListQuery,
  Project,
  ProjectDetail,
  ProjectInput,
  ProjectStatus,
} from './types';

export interface ProjectListQuery extends ListQuery {
  status?: ProjectStatus | '';
  employeeId?: string;
}

export const projectsApi = createCrudApi<
  Project,
  ProjectDetail,
  ProjectInput,
  ProjectListQuery
>('/api/projects');
