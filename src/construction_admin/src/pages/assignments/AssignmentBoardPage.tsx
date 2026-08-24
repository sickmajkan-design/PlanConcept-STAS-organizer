import {
  CloseOutlined,
  DragIndicatorOutlined,
  ExpandLessOutlined,
  ExpandMoreOutlined,
  HandymanOutlined,
  LocalShippingOutlined,
} from '@mui/icons-material';
import {
  Avatar,
  Box,
  Button,
  Chip,
  CircularProgress,
  Collapse,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  FormControl,
  Grid,
  IconButton,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Snackbar,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TableSortLabel,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import {
  DndContext,
  DragOverlay,
  PointerSensor,
  useDraggable,
  useDroppable,
  useSensor,
  useSensors,
  type DragEndEvent,
  type DragStartEvent,
} from '@dnd-kit/core';
import { useEffect, useMemo, useState, type ReactElement } from 'react';

import { toApiError } from '../../api/apiError';
import type {
  AssignmentBoardEmployee,
  AssignmentBoardEquipment,
  AssignmentBoardPosting,
  AssignmentBoardProject,
} from '../../api/types';
import { ErrorState } from '../../components/ErrorState';
import { PageHeader } from '../../components/PageHeader';
import { SearchField } from '../../components/SearchField';
import { StatusChip } from '../../components/StatusChip';
import { useCoverPhoto } from '../../features/attachments/useAttachments';
import {
  useAssignOnBoard,
  useAssignmentBoardQuery,
  useRemoveOnBoard,
} from '../../features/assignments/useAssignmentBoard';
import { useT } from '../../i18n/useI18n';
import { dateOnlyOffset } from '../../utils/formatting';
import { postingRange } from '../../utils/postings';

// Module scope, not inside the component: `useSensor`/`useSensors` only
// return a stable sensor array when the options object they're given is
// referentially stable across renders. A literal created inline is a new
// object every render, so DndContext saw a "new" sensors array on every
// re-render — including the one right after a successful assign/remove —
// and re-subscribing its pointer listeners at exactly that moment is what
// left every drag and click unresponsive afterwards.
const POINTER_ACTIVATION_CONSTRAINT = { activationConstraint: { distance: 8 } };

interface PendingDrop {
  employeeId: string;
  employeeName: string;
  projectId: string;
  projectName: string;
  /** Set when the drag started from another site's row — a move, not just an addition. */
  sourceProjectId?: string;
}

type EmployeeFilter = 'all' | 'unassigned' | 'assigned';
type EmployeeSort = 'name' | 'siteCount';
type SiteSortField = 'name' | 'crew' | 'tools' | 'vehicles';
type SortDirection = 'asc' | 'desc';

export function AssignmentBoardPage() {
  const t = useT();
  const { data, isLoading, isError, error, refetch } = useAssignmentBoardQuery();
  const assign = useAssignOnBoard();
  const remove = useRemoveOnBoard();

  const [search, setSearch] = useState('');
  const [employeeFilter, setEmployeeFilter] = useState<EmployeeFilter>('all');
  const [employeeSort, setEmployeeSort] = useState<EmployeeSort>('name');
  const [activeEmployee, setActiveEmployee] = useState<AssignmentBoardEmployee | null>(null);
  const [conflict, setConflict] = useState<string | null>(null);
  const [pendingDrop, setPendingDrop] = useState<PendingDrop | null>(null);
  const [expandedProjectId, setExpandedProjectId] = useState<string | null>(null);
  const [siteSortBy, setSiteSortBy] = useState<SiteSortField>('name');
  const [siteSortDirection, setSiteSortDirection] = useState<SortDirection>('asc');

  // A plain click has to survive being wrapped in a draggable row — the
  // crew list's delete button sits inside one. Requiring a deliberate
  // few-pixel move before a drag activates is what tells the two apart;
  // without it, the sensor reads a click's own tiny jitter as a drag start
  // and the button never sees its click.
  const sensors = useSensors(useSensor(PointerSensor, POINTER_ACTIVATION_CONSTRAINT));

  const employeesById = useMemo(
    () => new Map((data?.employees ?? []).map((employee) => [employee.id, employee])),
    [data],
  );

  const projectsById = useMemo(
    () => new Map((data?.projects ?? []).map((project) => [project.id, project])),
    [data],
  );

  const filteredEmployees = useMemo(() => {
    let employees = data?.employees ?? [];

    if (employeeFilter === 'unassigned') {
      employees = employees.filter((employee) => employee.postings.length === 0);
    } else if (employeeFilter === 'assigned') {
      employees = employees.filter((employee) => employee.postings.length > 0);
    }

    const term = search.trim().toLowerCase();
    if (term) {
      employees = employees.filter(
        (employee) =>
          employee.fullName.toLowerCase().includes(term) ||
          employee.employeeNumber.toLowerCase().includes(term) ||
          employee.position.toLowerCase().includes(term) ||
          employee.assignedTools.some((tool) => tool.name.toLowerCase().includes(term)) ||
          employee.assignedVehicles.some((vehicle) =>
            vehicle.name.toLowerCase().includes(term),
          ),
      );
    }

    employees = [...employees];
    if (employeeSort === 'siteCount') {
      employees.sort((a, b) => b.postings.length - a.postings.length);
    } else {
      employees.sort((a, b) => a.fullName.localeCompare(b.fullName));
    }

    return employees;
  }, [data, search, employeeFilter, employeeSort]);

  const crewCountByProject = useMemo(() => {
    const counts = new Map<string, number>();

    for (const employee of data?.employees ?? []) {
      for (const posting of employee.postings) {
        counts.set(posting.projectId, (counts.get(posting.projectId) ?? 0) + 1);
      }
    }

    return counts;
  }, [data]);

  const sortedProjects = useMemo(() => {
    const projects = [...(data?.projects ?? [])];
    const direction = siteSortDirection === 'asc' ? 1 : -1;

    projects.sort((a, b) => {
      switch (siteSortBy) {
        case 'crew':
          return direction * ((crewCountByProject.get(a.id) ?? 0) - (crewCountByProject.get(b.id) ?? 0));
        case 'tools':
          return direction * (a.toolCount - b.toolCount);
        case 'vehicles':
          return direction * (a.vehicleCount - b.vehicleCount);
        default:
          return direction * a.name.localeCompare(b.name);
      }
    });

    return projects;
  }, [data, siteSortBy, siteSortDirection, crewCountByProject]);

  const toggleSiteSort = (field: SiteSortField) => {
    if (siteSortBy === field) {
      setSiteSortDirection((current) => (current === 'asc' ? 'desc' : 'asc'));
    } else {
      setSiteSortBy(field);
      setSiteSortDirection('asc');
    }
  };

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

    // Set only when the card was picked up from another site's own row
    // (see the crew list inside ProjectRow) rather than the workforce list —
    // that is what tells the confirm step to move them instead of just
    // adding a second posting.
    const sourceProjectId = dragEvent.active.data.current?.sourceProjectId as
      | string
      | undefined;

    setPendingDrop({
      employeeId,
      employeeName: employee.fullName,
      projectId,
      projectName: project.name,
      sourceProjectId,
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
        <DndContext sensors={sensors} onDragStart={handleDragStart} onDragEnd={handleDragEnd}>
          <Grid container spacing={2}>
            <Grid size={{ xs: 12, md: 3 }}>
              <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1 }}>
                {t('assignmentBoard.workforce')}
              </Typography>
              <Stack spacing={1}>
                <SearchField
                  value={search}
                  onChange={setSearch}
                  placeholder={t('assignmentBoard.searchPlaceholder')}
                />
                {/* Stacked, not side by side — two half-width selects in this
                    narrow column left no room for the label and the chosen
                    value at once. */}
                <FormControl size="small" fullWidth>
                  <InputLabel id="ab-filter-label">{t('assignmentBoard.filterLabel')}</InputLabel>
                  <Select
                    labelId="ab-filter-label"
                    label={t('assignmentBoard.filterLabel')}
                    value={employeeFilter}
                    onChange={(event) => setEmployeeFilter(event.target.value as EmployeeFilter)}
                  >
                    <MenuItem value="all">{t('assignmentBoard.filterAll')}</MenuItem>
                    <MenuItem value="unassigned">{t('assignmentBoard.filterUnassigned')}</MenuItem>
                    <MenuItem value="assigned">{t('assignmentBoard.filterAssigned')}</MenuItem>
                  </Select>
                </FormControl>
                <FormControl size="small" fullWidth>
                  <InputLabel id="ab-sort-label">{t('assignmentBoard.sortLabel')}</InputLabel>
                  <Select
                    labelId="ab-sort-label"
                    label={t('assignmentBoard.sortLabel')}
                    value={employeeSort}
                    onChange={(event) => setEmployeeSort(event.target.value as EmployeeSort)}
                  >
                    <MenuItem value="name">{t('assignmentBoard.sortByName')}</MenuItem>
                    <MenuItem value="siteCount">{t('assignmentBoard.sortBySites')}</MenuItem>
                  </Select>
                </FormControl>
              </Stack>
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
              {(data?.projects.length ?? 0) === 0 ? (
                <Typography color="text.secondary">{t('assignmentBoard.noOpenSites')}</Typography>
              ) : (
                <TableContainer component={Paper} variant="outlined" sx={{ maxHeight: '75vh' }}>
                  <Table stickyHeader size="small">
                    <TableHead>
                      <TableRow>
                        <TableCell padding="checkbox" />
                        <TableCell sortDirection={siteSortBy === 'name' ? siteSortDirection : false}>
                          <TableSortLabel
                            active={siteSortBy === 'name'}
                            direction={siteSortBy === 'name' ? siteSortDirection : 'asc'}
                            onClick={() => toggleSiteSort('name')}
                          >
                            {t('assignmentBoard.colSite')}
                          </TableSortLabel>
                        </TableCell>
                        <TableCell align="right" sortDirection={siteSortBy === 'crew' ? siteSortDirection : false}>
                          <TableSortLabel
                            active={siteSortBy === 'crew'}
                            direction={siteSortBy === 'crew' ? siteSortDirection : 'asc'}
                            onClick={() => toggleSiteSort('crew')}
                          >
                            {t('assignmentBoard.colCrew')}
                          </TableSortLabel>
                        </TableCell>
                        <TableCell align="right" sortDirection={siteSortBy === 'tools' ? siteSortDirection : false}>
                          <TableSortLabel
                            active={siteSortBy === 'tools'}
                            direction={siteSortBy === 'tools' ? siteSortDirection : 'asc'}
                            onClick={() => toggleSiteSort('tools')}
                          >
                            {t('assignmentBoard.colTools')}
                          </TableSortLabel>
                        </TableCell>
                        <TableCell
                          align="right"
                          sortDirection={siteSortBy === 'vehicles' ? siteSortDirection : false}
                        >
                          <TableSortLabel
                            active={siteSortBy === 'vehicles'}
                            direction={siteSortBy === 'vehicles' ? siteSortDirection : 'asc'}
                            onClick={() => toggleSiteSort('vehicles')}
                          >
                            {t('assignmentBoard.colVehicles')}
                          </TableSortLabel>
                        </TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {sortedProjects.map((project) => (
                        <ProjectRow
                          key={project.id}
                          project={project}
                          expanded={expandedProjectId === project.id}
                          onToggle={() =>
                            setExpandedProjectId((current) =>
                              current === project.id ? null : project.id,
                            )
                          }
                          employees={(data?.employees ?? []).filter((employee) =>
                            employee.postings.some((posting) => posting.projectId === project.id),
                          )}
                          postingFor={(employee) =>
                            employee.postings.find(
                              (posting) => posting.projectId === project.id,
                            )!
                          }
                          onRemove={(employeeId) =>
                            remove.mutate(
                              { employeeId, projectId: project.id },
                              { onError: (err) => setConflict(toApiError(err).message) },
                            )
                          }
                        />
                      ))}
                    </TableBody>
                  </Table>
                </TableContainer>
              )}
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
          const { employeeId, projectId, sourceProjectId } = pendingDrop;

          assign.mutate(
            { employeeId, projectId, startDate, endDate },
            {
              onSuccess: () => {
                setPendingDrop(null);

                // A move, not an addition — drop the old site now that the
                // new posting is in. Left alone on error: an employee
                // dropped by mistake mid-move on both sites is a smaller
                // problem than one dropped from both.
                if (sourceProjectId) {
                  remove.mutate(
                    { employeeId, projectId: sourceProjectId },
                    { onError: (err) => setConflict(toApiError(err).message) },
                  );
                }
              },
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

/** Small outlined chips for a short equipment list, collapsing past three with a tooltip for the rest. */
function EquipmentChips({ items, icon }: { items: AssignmentBoardEquipment[]; icon: ReactElement }) {
  if (items.length === 0) return null;

  const shown = items.slice(0, 3);
  const rest = items.slice(3);

  return (
    <Stack direction="row" spacing={0.5} sx={{ flexWrap: 'wrap', gap: 0.5, mt: 0.5 }}>
      {shown.map((item) => (
        <Chip key={item.id} size="small" variant="outlined" icon={icon} label={item.name} />
      ))}
      {rest.length > 0 && (
        <Tooltip title={rest.map((item) => item.name).join(', ')}>
          <Chip size="small" variant="outlined" label={`+${rest.length}`} />
        </Tooltip>
      )}
    </Stack>
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
        cursor: overlay ? 'grabbing' : 'grab',
        opacity: !overlay && isDragging ? 0.4 : 1,
        userSelect: 'none',
        ...(overlay && { boxShadow: 4 }),
      }}
    >
      <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
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
      </Stack>
      <EquipmentChips items={employee.assignedTools} icon={<HandymanOutlined />} />
      <EquipmentChips items={employee.assignedVehicles} icon={<LocalShippingOutlined />} />
    </Paper>
  );
}

function ProjectRow({
  project,
  employees,
  postingFor,
  onRemove,
  expanded,
  onToggle,
}: {
  project: AssignmentBoardProject;
  employees: AssignmentBoardEmployee[];
  postingFor: (employee: AssignmentBoardEmployee) => AssignmentBoardPosting;
  onRemove: (employeeId: string) => void;
  expanded: boolean;
  onToggle: () => void;
}) {
  const t = useT();
  const { setNodeRef, isOver } = useDroppable({ id: project.id });
  const coverPhoto = useCoverPhoto('Project', project.id);

  return (
    <>
      <TableRow
        ref={setNodeRef}
        hover
        sx={{
          cursor: 'pointer',
          bgcolor: isOver ? 'action.hover' : undefined,
          outline: isOver ? '2px solid' : undefined,
          outlineColor: isOver ? 'primary.main' : undefined,
          outlineOffset: -2,
          '& > td': { borderBottom: expanded ? 'none' : undefined },
        }}
        onClick={onToggle}
      >
        <TableCell padding="checkbox">
          <IconButton
            size="small"
            onClick={(event) => {
              event.stopPropagation();
              onToggle();
            }}
          >
            {expanded ? <ExpandLessOutlined fontSize="small" /> : <ExpandMoreOutlined fontSize="small" />}
          </IconButton>
        </TableCell>
        <TableCell>
          <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
            <Avatar src={coverPhoto ?? undefined} variant="rounded" sx={{ width: 28, height: 28 }}>
              {project.name.charAt(0)}
            </Avatar>
            <Typography variant="body2" sx={{ fontWeight: 600 }}>
              {project.name}
            </Typography>
            <StatusChip status={project.status} kind="projectStatus" size="small" />
          </Stack>
        </TableCell>
        <TableCell align="right">{employees.length || '—'}</TableCell>
        <TableCell align="right">{project.toolCount || '—'}</TableCell>
        <TableCell align="right">{project.vehicleCount || '—'}</TableCell>
      </TableRow>
      <TableRow>
        <TableCell colSpan={5} sx={{ py: 0, border: expanded ? undefined : 'none' }}>
          <Collapse in={expanded} timeout="auto" unmountOnExit>
            <Box sx={{ py: 1.5, px: 1 }}>
              {employees.length === 0 ? (
                <Typography variant="caption" color="text.secondary">
                  {t('assignmentBoard.dropHere')}
                </Typography>
              ) : (
                <Stack spacing={0.75}>
                  {employees.map((employee) => (
                    <CrewRow
                      key={employee.id}
                      employee={employee}
                      posting={postingFor(employee)}
                      sourceProjectId={project.id}
                      onRemove={() => onRemove(employee.id)}
                    />
                  ))}
                </Stack>
              )}
            </Box>
          </Collapse>
        </TableCell>
      </TableRow>
    </>
  );
}

/**
 * One employee inside a site's expanded crew list — draggable onto another
 * site's row, same as a card from the workforce list, except this one
 * carries where it started so dropping it elsewhere reads as a move rather
 * than a second posting.
 */
function CrewRow({
  employee,
  posting,
  sourceProjectId,
  onRemove,
}: {
  employee: AssignmentBoardEmployee;
  posting: AssignmentBoardPosting;
  sourceProjectId: string;
  onRemove: () => void;
}) {
  const t = useT();
  const { attributes, listeners, setNodeRef, isDragging } = useDraggable({
    id: employee.id,
    data: { sourceProjectId },
  });

  return (
    <Box
      ref={setNodeRef}
      {...listeners}
      {...attributes}
      sx={{
        px: 1,
        py: 0.5,
        borderRadius: 1,
        bgcolor: 'action.hover',
        cursor: 'grab',
        opacity: isDragging ? 0.4 : 1,
        touchAction: 'none',
        userSelect: 'none',
      }}
    >
      <Stack direction="row" spacing={1} sx={{ alignItems: 'center', justifyContent: 'space-between' }}>
        <Stack direction="row" spacing={0.75} sx={{ alignItems: 'center', minWidth: 0 }}>
          <DragIndicatorOutlined fontSize="small" color="disabled" />
          <Box sx={{ minWidth: 0 }}>
            <Typography variant="body2" noWrap>
              {employee.fullName}
            </Typography>
            <Typography variant="caption" color="text.secondary" noWrap>
              {postingRange(posting, t)}
            </Typography>
          </Box>
        </Stack>
        <Tooltip title={t('common.delete')}>
          <IconButton
            size="small"
            onClick={(event) => {
              event.stopPropagation();
              onRemove();
            }}
          >
            <CloseOutlined fontSize="small" />
          </IconButton>
        </Tooltip>
      </Stack>
      <EquipmentChips items={employee.assignedTools} icon={<HandymanOutlined />} />
      <EquipmentChips items={employee.assignedVehicles} icon={<LocalShippingOutlined />} />
    </Box>
  );
}
