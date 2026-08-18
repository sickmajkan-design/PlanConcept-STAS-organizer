import { CloseOutlined, DragIndicatorOutlined } from '@mui/icons-material';
import {
  Box,
  Chip,
  CircularProgress,
  Grid,
  Paper,
  Snackbar,
  Stack,
  Typography,
} from '@mui/material';
import {
  DndContext,
  DragOverlay,
  useDraggable,
  useDroppable,
  type DragEndEvent,
  type DragStartEvent,
} from '@dnd-kit/core';
import { useMemo, useState } from 'react';

import { toApiError } from '../../api/apiError';
import type { AssignmentBoardEmployee, AssignmentBoardProject } from '../../api/types';
import { ErrorState } from '../../components/ErrorState';
import { PageHeader } from '../../components/PageHeader';
import { SearchField } from '../../components/SearchField';
import { StatusChip } from '../../components/StatusChip';
import {
  useAssignOnBoard,
  useAssignmentBoardQuery,
  useRemoveOnBoard,
} from '../../features/assignments/useAssignmentBoard';
import { useT } from '../../i18n/useI18n';

export function AssignmentBoardPage() {
  const t = useT();
  const { data, isLoading, isError, error, refetch } = useAssignmentBoardQuery();
  const assign = useAssignOnBoard();
  const remove = useRemoveOnBoard();

  const [search, setSearch] = useState('');
  const [activeEmployee, setActiveEmployee] = useState<AssignmentBoardEmployee | null>(null);
  const [conflict, setConflict] = useState<string | null>(null);

  const employeesById = useMemo(
    () => new Map((data?.employees ?? []).map((employee) => [employee.id, employee])),
    [data],
  );

  const filteredEmployees = useMemo(() => {
    const employees = data?.employees ?? [];
    const term = search.trim().toLowerCase();
    if (!term) return employees;

    return employees.filter(
      (employee) =>
        employee.fullName.toLowerCase().includes(term) ||
        employee.employeeNumber.toLowerCase().includes(term) ||
        employee.position.toLowerCase().includes(term),
    );
  }, [data, search]);

  const handleDragStart = (dragEvent: DragStartEvent) => {
    setActiveEmployee(employeesById.get(String(dragEvent.active.id)) ?? null);
  };

  const handleDragEnd = (dragEvent: DragEndEvent) => {
    setActiveEmployee(null);

    const employeeId = String(dragEvent.active.id);
    const projectId = dragEvent.over?.id ? String(dragEvent.over.id) : null;
    if (!projectId) return;

    const employee = employeesById.get(employeeId);
    if (!employee) return;

    // Already there — the drop is a no-op rather than a call the API would
    // refuse, so dragging someone onto a site they are already on never
    // shows an error for something that was never wrong.
    if (employee.projectIds.includes(projectId)) return;

    assign.mutate(
      { employeeId, projectId },
      {
        onError: (err) => setConflict(toApiError(err).message),
      },
    );
  };

  if (isError) return <ErrorState error={error} onRetry={() => void refetch()} />;

  return (
    <Box>
      <PageHeader title={t('assignmentBoard.title')} subtitle={t('assignmentBoard.subtitle')} />

      {isLoading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
          <CircularProgress />
        </Box>
      ) : (
        <DndContext onDragStart={handleDragStart} onDragEnd={handleDragEnd}>
          <Grid container spacing={2}>
            <Grid size={{ xs: 12, md: 3 }}>
              <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1 }}>
                {t('assignmentBoard.workforce')}
              </Typography>
              <SearchField
                value={search}
                onChange={setSearch}
                placeholder={t('assignmentBoard.searchPlaceholder')}
              />
              <Stack spacing={1} sx={{ mt: 1.5, maxHeight: '70vh', overflowY: 'auto', pr: 0.5 }}>
                {filteredEmployees.length === 0 && (
                  <Typography variant="body2" color="text.secondary" sx={{ py: 2 }}>
                    {t('common.noResults')}
                  </Typography>
                )}
                {filteredEmployees.map((employee) => (
                  <EmployeeCard key={employee.id} employee={employee} />
                ))}
              </Stack>
            </Grid>

            <Grid size={{ xs: 12, md: 9 }}>
              <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1 }}>
                {t('assignmentBoard.sites')}
              </Typography>
              <Grid container spacing={2}>
                {(data?.projects ?? []).map((project) => (
                  <Grid key={project.id} size={{ xs: 12, sm: 6, lg: 4 }}>
                    <ProjectLane
                      project={project}
                      employees={(data?.employees ?? []).filter((employee) =>
                        employee.projectIds.includes(project.id),
                      )}
                      onRemove={(employeeId) =>
                        remove.mutate(
                          { employeeId, projectId: project.id },
                          { onError: (err) => setConflict(toApiError(err).message) },
                        )
                      }
                    />
                  </Grid>
                ))}
                {(data?.projects.length ?? 0) === 0 && (
                  <Grid size={12}>
                    <Typography color="text.secondary">
                      {t('assignmentBoard.noOpenSites')}
                    </Typography>
                  </Grid>
                )}
              </Grid>
            </Grid>
          </Grid>

          <DragOverlay>
            {activeEmployee && <EmployeeCard employee={activeEmployee} overlay />}
          </DragOverlay>
        </DndContext>
      )}

      <Snackbar
        open={!!conflict}
        autoHideDuration={5000}
        onClose={() => setConflict(null)}
        message={conflict}
      />
    </Box>
  );
}

function EmployeeCard({
  employee,
  overlay = false,
}: {
  employee: AssignmentBoardEmployee;
  overlay?: boolean;
}) {
  const t = useT();
  const { attributes, listeners, setNodeRef, isDragging } = useDraggable({
    id: employee.id,
  });

  return (
    <Paper
      ref={overlay ? undefined : setNodeRef}
      {...(overlay ? {} : listeners)}
      {...(overlay ? {} : attributes)}
      variant="outlined"
      sx={{
        p: 1.25,
        display: 'flex',
        alignItems: 'center',
        gap: 1,
        cursor: overlay ? 'grabbing' : 'grab',
        opacity: !overlay && isDragging ? 0.4 : 1,
        userSelect: 'none',
        ...(overlay && { boxShadow: 4 }),
      }}
    >
      <DragIndicatorOutlined fontSize="small" color="disabled" />
      <Box sx={{ minWidth: 0, flex: 1 }}>
        <Typography variant="body2" sx={{ fontWeight: 600 }} noWrap>
          {employee.fullName}
        </Typography>
        <Typography variant="caption" color="text.secondary" noWrap>
          {employee.position}
        </Typography>
      </Box>
      {employee.projectIds.length > 0 && (
        <Chip
          size="small"
          label={t('assignmentBoard.siteCount', { count: employee.projectIds.length })}
          variant="outlined"
        />
      )}
    </Paper>
  );
}

function ProjectLane({
  project,
  employees,
  onRemove,
}: {
  project: AssignmentBoardProject;
  employees: AssignmentBoardEmployee[];
  onRemove: (employeeId: string) => void;
}) {
  const t = useT();
  const { setNodeRef, isOver } = useDroppable({ id: project.id });

  return (
    <Paper
      ref={setNodeRef}
      variant="outlined"
      sx={{
        p: 1.5,
        minHeight: 160,
        height: '100%',
        borderColor: isOver ? 'primary.main' : undefined,
        borderWidth: isOver ? 2 : 1,
        bgcolor: isOver ? 'action.hover' : undefined,
        transition: 'background-color 120ms, border-color 120ms',
      }}
    >
      <Stack direction="row" spacing={1} sx={{ alignItems: 'center', mb: 1 }}>
        <Typography variant="subtitle2" sx={{ fontWeight: 700, flex: 1 }} noWrap>
          {project.name}
        </Typography>
        <StatusChip status={project.status} kind="projectStatus" size="small" />
      </Stack>

      {employees.length === 0 ? (
        <Typography variant="caption" color="text.secondary">
          {t('assignmentBoard.dropHere')}
        </Typography>
      ) : (
        <Stack spacing={0.75}>
          {employees.map((employee) => (
            <Chip
              key={employee.id}
              label={employee.fullName}
              size="small"
              onDelete={() => onRemove(employee.id)}
              deleteIcon={<CloseOutlined fontSize="small" />}
              sx={{ justifyContent: 'space-between' }}
            />
          ))}
        </Stack>
      )}
    </Paper>
  );
}
