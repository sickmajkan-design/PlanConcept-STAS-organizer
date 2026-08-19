import { CloseOutlined, DragIndicatorOutlined } from '@mui/icons-material';
import {
  Box,
  Button,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  Grid,
  IconButton,
  Paper,
  Snackbar,
  Stack,
  TextField,
  Tooltip,
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
import { useEffect, useMemo, useState } from 'react';

import { toApiError } from '../../api/apiError';
import type { AssignmentBoardEmployee, AssignmentBoardPosting, AssignmentBoardProject } from '../../api/types';
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
import { dateOnlyOffset, formatDate } from '../../utils/formatting';

interface PendingDrop {
  employeeId: string;
  employeeName: string;
  projectId: string;
  projectName: string;
}

export function AssignmentBoardPage() {
  const t = useT();
  const { data, isLoading, isError, error, refetch } = useAssignmentBoardQuery();
  const assign = useAssignOnBoard();
  const remove = useRemoveOnBoard();

  const [search, setSearch] = useState('');
  const [activeEmployee, setActiveEmployee] = useState<AssignmentBoardEmployee | null>(null);
  const [conflict, setConflict] = useState<string | null>(null);
  const [pendingDrop, setPendingDrop] = useState<PendingDrop | null>(null);

  const employeesById = useMemo(
    () => new Map((data?.employees ?? []).map((employee) => [employee.id, employee])),
    [data],
  );

  const projectsById = useMemo(
    () => new Map((data?.projects ?? []).map((project) => [project.id, project])),
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
    const project = projectsById.get(projectId);
    if (!employee || !project) return;

    // Already there — the drop is a no-op rather than a call the API would
    // refuse, so dragging someone onto a site they are already on never
    // shows an error for something that was never wrong.
    if (employee.postings.some((posting) => posting.projectId === projectId)) return;

    setPendingDrop({
      employeeId,
      employeeName: employee.fullName,
      projectId,
      projectName: project.name,
    });
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
                        employee.postings.some((posting) => posting.projectId === project.id),
                      )}
                      postingFor={(employee) =>
                        employee.postings.find((posting) => posting.projectId === project.id)!
                      }
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

      <AssignDatesDialog
        pending={pendingDrop}
        onClose={() => setPendingDrop(null)}
        onConfirm={(startDate, endDate) => {
          if (!pendingDrop) return;

          assign.mutate(
            { employeeId: pendingDrop.employeeId, projectId: pendingDrop.projectId, startDate, endDate },
            {
              onSuccess: () => setPendingDrop(null),
              onError: (err) => {
                setConflict(toApiError(err).message);
                setPendingDrop(null);
              },
            },
          );
        }}
        loading={assign.isPending}
      />

      <Snackbar
        open={!!conflict}
        autoHideDuration={5000}
        onClose={() => setConflict(null)}
        message={conflict}
      />
    </Box>
  );
}

/** The dates a dropped posting covers — asked for on every drop, since a plan made today often starts later. */
function AssignDatesDialog({
  pending,
  onClose,
  onConfirm,
  loading,
}: {
  pending: PendingDrop | null;
  onClose: () => void;
  onConfirm: (startDate: string, endDate: string | null) => void;
  loading: boolean;
}) {
  const t = useT();
  const [startDate, setStartDate] = useState(dateOnlyOffset(0));
  const [endDate, setEndDate] = useState('');

  useEffect(() => {
    if (pending) {
      setStartDate(dateOnlyOffset(0));
      setEndDate('');
    }
  }, [pending]);

  const datesAreValid = !endDate || endDate >= startDate;

  return (
    <Dialog open={!!pending} onClose={onClose} fullWidth maxWidth="xs">
      <DialogTitle>{t('assignmentBoard.assignTitle')}</DialogTitle>
      <DialogContent>
        <DialogContentText sx={{ mb: 2 }}>
          {pending
            ? t('assignmentBoard.assignBody', {
                employee: pending.employeeName,
                project: pending.projectName,
              })
            : ''}
        </DialogContentText>
        <Stack direction="row" spacing={2}>
          <TextField
            type="date"
            fullWidth
            label={t('assignmentBoard.startDate')}
            value={startDate}
            onChange={(event) => setStartDate(event.target.value)}
            slotProps={{ inputLabel: { shrink: true } }}
          />
          <TextField
            type="date"
            fullWidth
            label={t('assignmentBoard.endDate')}
            value={endDate}
            onChange={(event) => setEndDate(event.target.value)}
            slotProps={{ inputLabel: { shrink: true } }}
            error={!datesAreValid}
            helperText={!datesAreValid ? t('assignmentBoard.endsBeforeStart') : undefined}
          />
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button
          variant="contained"
          disabled={!startDate || !datesAreValid || loading}
          loading={loading}
          onClick={() => onConfirm(startDate, endDate || null)}
        >
          {t('assignmentBoard.assign')}
        </Button>
      </DialogActions>
    </Dialog>
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
      {employee.postings.length > 0 && (
        <Chip
          size="small"
          label={t('assignmentBoard.siteCount', { count: employee.postings.length })}
          variant="outlined"
        />
      )}
    </Paper>
  );
}

/** "Since 18.08.2026." or, once an end is set, "18.08.2026. – 22.08.2026.". */
function postingRange(posting: AssignmentBoardPosting, t: ReturnType<typeof useT>): string {
  return posting.endDate
    ? `${formatDate(posting.startDate)} – ${formatDate(posting.endDate)}`
    : `${t('assignmentBoard.since')} ${formatDate(posting.startDate)}`;
}

function ProjectLane({
  project,
  employees,
  postingFor,
  onRemove,
}: {
  project: AssignmentBoardProject;
  employees: AssignmentBoardEmployee[];
  postingFor: (employee: AssignmentBoardEmployee) => AssignmentBoardPosting;
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
          {employees.map((employee) => {
            const posting = postingFor(employee);

            return (
              <Stack
                key={employee.id}
                direction="row"
                spacing={1}
                sx={{
                  alignItems: 'center',
                  justifyContent: 'space-between',
                  px: 1,
                  py: 0.5,
                  borderRadius: 1,
                  bgcolor: 'action.hover',
                }}
              >
                <Box sx={{ minWidth: 0 }}>
                  <Typography variant="body2" noWrap>
                    {employee.fullName}
                  </Typography>
                  <Typography variant="caption" color="text.secondary" noWrap>
                    {postingRange(posting, t)}
                  </Typography>
                </Box>
                <Tooltip title={t('common.delete')}>
                  <IconButton size="small" onClick={() => onRemove(employee.id)}>
                    <CloseOutlined fontSize="small" />
                  </IconButton>
                </Tooltip>
              </Stack>
            );
          })}
        </Stack>
      )}
    </Paper>
  );
}
