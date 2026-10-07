import type { Plan } from '../../features/planning/planningLogic';

/** What the views ask of the page. The page owns the actions so every one of them can be undone. */
export interface PlanningHandlers {
  openWorker: (personId: string, from: string, to: string) => void;
  openSite: (projectId: string, from: string, to: string, position?: string) => void;
  editNeeds: (projectId: string) => void;
}

export interface ViewProps extends PlanningHandlers {
  plan: Plan;
  /** Nothing can be moved: the views draw the same but their buttons do nothing. */
  readOnly?: boolean;
}
