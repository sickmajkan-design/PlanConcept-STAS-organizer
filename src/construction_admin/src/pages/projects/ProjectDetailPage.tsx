import {
  AddOutlined,
  DeleteOutlined,
  EditOutlined,
  HandymanOutlined,
  LocalShippingOutlined,
} from '@mui/icons-material';
import {
  Alert,
  Avatar,
  Box,
  Button,
  Card,
  CardContent,
  Divider,
  FormControl,
  Grid,
  IconButton,
  List,
  ListItem,
  ListItemAvatar,
  ListItemText,
  MenuItem,
  Select,
  Stack,
  Tooltip,
  Typography,
} from '@mui/material';
import { useEffect, useMemo, useState } from 'react';
import { useLocation, useNavigate, useParams } from 'react-router-dom';

import { toApiError } from '../../api/apiError';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { ErrorState } from '../../components/ErrorState';
import { AttachmentList } from '../../components/AttachmentList';
import { StatusChip } from '../../components/StatusChip';
import {
  useAllEmployeesQuery,
  useAssignProjectEmployee,
  useRemoveProjectEmployee,
} from '../../features/employees/useEmployees';
import { useDeleteProject, useProjectQuery } from '../../features/projects/useProjects';
import {
  useAllToolsQuery,
  useAssignProjectTool,
  useToolsQuery,
  useUnassignProjectTool,
} from '../../features/tools/useTools';
import {
  useAllVehiclesQuery,
  useAssignProjectVehicle,
  useUnassignProjectVehicle,
  useVehiclesQuery,
} from '../../features/vehicles/useVehicles';
import { useI18n, useT } from '../../i18n/useI18n';
import { canAdministerAccounts } from '../../auth/authHelpers';
import { useAuth } from '../../auth/useAuth';
import { paths } from '../../routes/paths';
import { formatDate, initialsOf } from '../../utils/formatting';
import { postingRange, workedSummary } from '../../utils/postings';

/** Every vehicle/tool list on this page fits comfortably on one page-worth. */
const RESOURCE_PAGE: { pageNumber: number; pageSize: number } = { pageNumber: 1, pageSize: 100 };

export function ProjectDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const location = useLocation();
  const t = useT();
  const { locale } = useI18n();
  const { user } = useAuth();

  const { data: project, isLoading, isError, error, refetch } = useProjectQuery(id);
  const deleteProject = useDeleteProject();
  const [confirmDelete, setConfirmDelete] = useState(false);

  // The project page a fresh "Create" redirects to briefly highlights the
  // resources block below — a new project's very next step is staffing and
  // equipping it, and a page that looks finished otherwise gives no hint of
  // that. Faded rather than pinned: it only matters the first time.
  const [highlightResources, setHighlightResources] = useState(
    Boolean((location.state as { justCreated?: boolean } | null)?.justCreated),
  );

  useEffect(() => {
    if (!highlightResources) return;
    const timer = setTimeout(() => setHighlightResources(false), 2200);
    return () => clearTimeout(timer);
  }, [highlightResources]);

  const projectId = project?.id ?? '';

  const { data: allEmployees } = useAllEmployeesQuery();
  const assignEmployee = useAssignProjectEmployee(projectId);
  const removeEmployee = useRemoveProjectEmployee(projectId);
  const [selectedEmployeeId, setSelectedEmployeeId] = useState('');
  const [removeEmployeeTarget, setRemoveEmployeeTarget] = useState<
    { id: string; name: string } | null
  >(null);

  const { data: vehiclesOnProject } = useVehiclesQuery({
    ...RESOURCE_PAGE,
    assignedProjectId: projectId || undefined,
  });
  const { data: allVehicles } = useAllVehiclesQuery();
  const assignVehicle = useAssignProjectVehicle(projectId);
  const unassignVehicle = useUnassignProjectVehicle();
  const [selectedVehicleId, setSelectedVehicleId] = useState('');
  const [removeVehicleTarget, setRemoveVehicleTarget] = useState<
    { id: string; name: string } | null
  >(null);

  const { data: toolsOnProject } = useToolsQuery({
    ...RESOURCE_PAGE,
    assignedProjectId: projectId || undefined,
  });
  const { data: allTools } = useAllToolsQuery();
  const assignTool = useAssignProjectTool(projectId);
  const unassignTool = useUnassignProjectTool();
  const [selectedToolId, setSelectedToolId] = useState('');
  const [removeToolTarget, setRemoveToolTarget] = useState<{ id: string; name: string } | null>(
    null,
  );

  const assignableEmployees = useMemo(() => {
    const assignedIds = new Set(project?.employees.map((e) => e.employeeId));
    return (allEmployees?.items ?? []).filter((e) => !assignedIds.has(e.id));
  }, [allEmployees, project]);

  // Only vehicles/tools not already parked on some other project — assigning
  // one here would otherwise silently move it out from under whatever else
  // it was on, with no warning.
  const assignableVehicles = useMemo(
    () => (allVehicles?.items ?? []).filter((v) => !v.assignedProjectId),
    [allVehicles],
  );
  const assignableTools = useMemo(
    () => (allTools?.items ?? []).filter((tool) => !tool.assignedProjectId),
    [allTools],
  );

  if (isLoading) return null;
  if (isError || !project) {
    return <ErrorState error={error} onRetry={() => void refetch()} />;
  }

  const handleDelete = async () => {
    await deleteProject.mutateAsync(project.id);
    navigate(paths.projects, { replace: true });
  };

  const handleAssignEmployee = async () => {
    if (!selectedEmployeeId) return;
    await assignEmployee.mutateAsync(selectedEmployeeId);
    setSelectedEmployeeId('');
  };

  const handleRemoveEmployee = async () => {
    if (!removeEmployeeTarget) return;
    await removeEmployee.mutateAsync(removeEmployeeTarget.id);
    setRemoveEmployeeTarget(null);
  };

  const handleAssignVehicle = async () => {
    if (!selectedVehicleId) return;
    await assignVehicle.mutateAsync(selectedVehicleId);
    setSelectedVehicleId('');
  };

  const handleRemoveVehicle = async () => {
    if (!removeVehicleTarget) return;
    await unassignVehicle.mutateAsync(removeVehicleTarget.id);
    setRemoveVehicleTarget(null);
  };

  const handleAssignTool = async () => {
    if (!selectedToolId) return;
    await assignTool.mutateAsync(selectedToolId);
    setSelectedToolId('');
  };

  const handleRemoveTool = async () => {
    if (!removeToolTarget) return;
    await unassignTool.mutateAsync(removeToolTarget.id);
    setRemoveToolTarget(null);
  };

  const hasCoordinates = project.latitude !== null && project.longitude !== null;
  const vehicles = vehiclesOnProject?.items ?? [];
  const tools = toolsOnProject?.items ?? [];

  return (
    <Box sx={{ maxWidth: 900 }}>
      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Stack
            direction={{ xs: 'column', sm: 'row' }}
            spacing={2}
            sx={{
              justifyContent: 'space-between',
              alignItems: { xs: 'flex-start', sm: 'center' },
            }}
          >
            <Box>
              <Typography variant="h5" sx={{ fontWeight: 700 }}>
                {project.name}
              </Typography>
              <Stack direction="row" spacing={1} sx={{ mt: 1.5, alignItems: 'center' }}>
                <StatusChip status={project.status} kind="projectStatus" />
                <Typography variant="body2" color="text.secondary">
                  · {project.employeeCount} assigned
                </Typography>
              </Stack>
            </Box>
            <Stack direction="row" spacing={1}>
              <Button
                variant="outlined"
                startIcon={<EditOutlined />}
                onClick={() => navigate(paths.projectEdit(project.id))}
              >
                Edit
              </Button>
              <Button
                variant="outlined"
                color="error"
                startIcon={<DeleteOutlined />}
                onClick={() => setConfirmDelete(true)}
              >
                Delete
              </Button>
            </Stack>
          </Stack>

          {project.description && (
            <Typography sx={{ mt: 2 }} color="text.secondary">
              {project.description}
            </Typography>
          )}
        </CardContent>
      </Card>

      <Grid container spacing={3}>
        <Grid size={12}>
          <Card>
            <CardContent>
              <Typography variant="subtitle1" gutterBottom sx={{ fontWeight: 700 }}>
                Details
              </Typography>
              <Stack spacing={1.5} sx={{ mt: 1 }}>
                <InfoRow label={t('projects.client')} value={project.client} />
                <InfoRow label={t('projects.address')} value={project.address} />
                <InfoRow
                  label={t('projects.coordinates')}
                  value={
                    hasCoordinates
                      ? `${project.latitude!.toFixed(5)}, ${project.longitude!.toFixed(5)}`
                      : null
                  }
                />
                <InfoRow label={t('projects.startDate')} value={project.startDate ? formatDate(project.startDate) : null} />
                <InfoRow label={t('projects.endDate')} value={project.endDate ? formatDate(project.endDate) : null} />
              </Stack>
            </CardContent>
          </Card>
        </Grid>

        {/* Everything staffed and equipped on this project, grouped together
            so setting up a fresh project is one place to work through rather
            than three separate hunts across the Employees/Vehicles/Tools
            sections. */}
        <Grid size={12}>
          <Box
            sx={{
              borderRadius: 2,
              transition: 'background-color 1.5s ease',
              bgcolor: highlightResources ? 'action.hover' : 'transparent',
              p: highlightResources ? 1.5 : 0,
              m: highlightResources ? -1.5 : 0,
            }}
          >
            <Typography
              variant="subtitle2"
              color="text.secondary"
              sx={{ mb: 1.5, ml: highlightResources ? 1.5 : 0 }}
            >
              {t('projects.resources')}
            </Typography>

            <Grid container spacing={3}>
              <Grid size={{ xs: 12, sm: 6 }}>
                <Card sx={{ height: '100%' }}>
                  <CardContent>
                    <Typography variant="subtitle1" gutterBottom sx={{ fontWeight: 700 }}>
                      {t('projects.crew')} ({project.employees.length})
                    </Typography>

                    {project.employees.length === 0 ? (
                      <Typography color="text.secondary" sx={{ py: 2 }}>
                        {t('projects.noCrewSentence')}
                      </Typography>
                    ) : (
                      <List disablePadding>
                        {project.employees.map((member) => (
                          <ListItem
                            key={member.employeeId}
                            divider
                            sx={{ cursor: 'pointer', px: 0 }}
                            onClick={() => navigate(paths.employeeDetail(member.employeeId))}
                            secondaryAction={
                              <Tooltip title={t('projects.removeFromCrew')}>
                                <IconButton
                                  edge="end"
                                  size="small"
                                  onClick={(event) => {
                                    event.stopPropagation();
                                    setRemoveEmployeeTarget({
                                      id: member.employeeId,
                                      name: member.fullName,
                                    });
                                  }}
                                >
                                  <DeleteOutlined fontSize="small" />
                                </IconButton>
                              </Tooltip>
                            }
                          >
                            <ListItemAvatar>
                              <Avatar sx={{ bgcolor: 'secondary.main' }}>
                                {crewInitials(member.fullName)}
                              </Avatar>
                            </ListItemAvatar>
                            <ListItemText
                              primary={member.fullName}
                              secondary={
                                [
                                  `${member.position} · ${member.employeeNumber}`,
                                  postingRange(member, t),
                                  workedSummary(member, t, locale),
                                ]
                                  .filter(Boolean)
                                  .join(' · ')
                              }
                            />
                            <StatusChip status={member.status} kind="employeeStatus" />
                          </ListItem>
                        ))}
                      </List>
                    )}

                    <Divider sx={{ my: 2 }} />

                    <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
                      <FormControl size="small" fullWidth>
                        <Select
                          displayEmpty
                          value={selectedEmployeeId}
                          onChange={(event) => setSelectedEmployeeId(event.target.value)}
                        >
                          <MenuItem value="">
                            <em>{t('common.selectEmployee')}</em>
                          </MenuItem>
                          {assignableEmployees.map((employee) => (
                            <MenuItem key={employee.id} value={employee.id}>
                              {employee.firstName} {employee.lastName}
                            </MenuItem>
                          ))}
                        </Select>
                      </FormControl>
                      <Button
                        variant="outlined"
                        startIcon={<AddOutlined />}
                        disabled={!selectedEmployeeId}
                        loading={assignEmployee.isPending}
                        onClick={handleAssignEmployee}
                      >
                        {t('projects.addToCrew')}
                      </Button>
                    </Stack>

                    {(assignEmployee.isError || removeEmployee.isError) && (
                      <Alert severity="error" sx={{ mt: 2 }}>
                        {toApiError(assignEmployee.error ?? removeEmployee.error).message}
                      </Alert>
                    )}
                  </CardContent>
                </Card>
              </Grid>

              <Grid size={{ xs: 12, sm: 6 }}>
                <Card sx={{ height: '100%' }}>
                  <CardContent>
                    <Typography variant="subtitle1" gutterBottom sx={{ fontWeight: 700 }}>
                      {t('projects.vehicles')} ({vehicles.length})
                    </Typography>

                    {vehicles.length === 0 ? (
                      <Typography color="text.secondary" sx={{ py: 2 }}>
                        {t('projects.noVehiclesSentence')}
                      </Typography>
                    ) : (
                      <List disablePadding>
                        {vehicles.map((vehicle) => (
                          <ListItem
                            key={vehicle.id}
                            divider
                            sx={{ cursor: 'pointer', px: 0 }}
                            onClick={() => navigate(paths.vehicleDetail(vehicle.id))}
                            secondaryAction={
                              <Tooltip title={t('projects.removeVehicle')}>
                                <IconButton
                                  edge="end"
                                  size="small"
                                  onClick={(event) => {
                                    event.stopPropagation();
                                    setRemoveVehicleTarget({
                                      id: vehicle.id,
                                      name: `${vehicle.brand} ${vehicle.model}`,
                                    });
                                  }}
                                >
                                  <DeleteOutlined fontSize="small" />
                                </IconButton>
                              </Tooltip>
                            }
                          >
                            <ListItemAvatar>
                              <Avatar sx={{ bgcolor: 'action.selected' }}>
                                <LocalShippingOutlined fontSize="small" />
                              </Avatar>
                            </ListItemAvatar>
                            <ListItemText
                              primary={`${vehicle.brand} ${vehicle.model}`}
                              secondary={vehicle.registrationNumber}
                            />
                            <StatusChip status={vehicle.status} kind="vehicleStatus" />
                          </ListItem>
                        ))}
                      </List>
                    )}

                    <Divider sx={{ my: 2 }} />

                    <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
                      <FormControl size="small" fullWidth>
                        <Select
                          displayEmpty
                          value={selectedVehicleId}
                          onChange={(event) => setSelectedVehicleId(event.target.value)}
                        >
                          <MenuItem value="">
                            <em>{t('common.selectVehicle')}</em>
                          </MenuItem>
                          {assignableVehicles.map((vehicle) => (
                            <MenuItem key={vehicle.id} value={vehicle.id}>
                              {vehicle.brand} {vehicle.model} ({vehicle.registrationNumber})
                            </MenuItem>
                          ))}
                        </Select>
                      </FormControl>
                      <Button
                        variant="outlined"
                        startIcon={<AddOutlined />}
                        disabled={!selectedVehicleId}
                        loading={assignVehicle.isPending}
                        onClick={handleAssignVehicle}
                      >
                        {t('projects.addVehicle')}
                      </Button>
                    </Stack>

                    {(assignVehicle.isError || unassignVehicle.isError) && (
                      <Alert severity="error" sx={{ mt: 2 }}>
                        {toApiError(assignVehicle.error ?? unassignVehicle.error).message}
                      </Alert>
                    )}
                  </CardContent>
                </Card>
              </Grid>

              <Grid size={{ xs: 12, sm: 6 }}>
                <Card>
                  <CardContent>
                    <Typography variant="subtitle1" gutterBottom sx={{ fontWeight: 700 }}>
                      {t('projects.tools')} ({tools.length})
                    </Typography>

                    {tools.length === 0 ? (
                      <Typography color="text.secondary" sx={{ py: 2 }}>
                        {t('projects.noToolsSentence')}
                      </Typography>
                    ) : (
                      <List disablePadding>
                        {tools.map((tool) => (
                          <ListItem
                            key={tool.id}
                            divider
                            sx={{ cursor: 'pointer', px: 0 }}
                            onClick={() => navigate(paths.toolDetail(tool.id))}
                            secondaryAction={
                              <Tooltip title={t('projects.removeTool')}>
                                <IconButton
                                  edge="end"
                                  size="small"
                                  onClick={(event) => {
                                    event.stopPropagation();
                                    setRemoveToolTarget({ id: tool.id, name: tool.name });
                                  }}
                                >
                                  <DeleteOutlined fontSize="small" />
                                </IconButton>
                              </Tooltip>
                            }
                          >
                            <ListItemAvatar>
                              <Avatar sx={{ bgcolor: 'action.selected' }}>
                                <HandymanOutlined fontSize="small" />
                              </Avatar>
                            </ListItemAvatar>
                            <ListItemText primary={tool.name} secondary={tool.category} />
                            <StatusChip status={tool.status} kind="toolStatus" />
                          </ListItem>
                        ))}
                      </List>
                    )}

                    <Divider sx={{ my: 2 }} />

                    <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
                      <FormControl size="small" fullWidth>
                        <Select
                          displayEmpty
                          value={selectedToolId}
                          onChange={(event) => setSelectedToolId(event.target.value)}
                        >
                          <MenuItem value="">
                            <em>{t('common.selectTool')}</em>
                          </MenuItem>
                          {assignableTools.map((tool) => (
                            <MenuItem key={tool.id} value={tool.id}>
                              {tool.name}
                            </MenuItem>
                          ))}
                        </Select>
                      </FormControl>
                      <Button
                        variant="outlined"
                        startIcon={<AddOutlined />}
                        disabled={!selectedToolId}
                        loading={assignTool.isPending}
                        onClick={handleAssignTool}
                      >
                        {t('projects.addTool')}
                      </Button>
                    </Stack>

                    {(assignTool.isError || unassignTool.isError) && (
                      <Alert severity="error" sx={{ mt: 2 }}>
                        {toApiError(assignTool.error ?? unassignTool.error).message}
                      </Alert>
                    )}
                  </CardContent>
                </Card>
              </Grid>
            </Grid>
          </Box>
        </Grid>

        {project.pastEmployees.length > 0 && (
          <Grid size={12}>
            <Card>
              <CardContent>
                <Typography variant="subtitle1" gutterBottom sx={{ fontWeight: 700 }}>
                  {t('projects.crewHistory')}
                </Typography>
                <List disablePadding>
                  {project.pastEmployees.map((member, index) => (
                    <ListItem
                      key={`${member.employeeId}-${member.startDate}-${index}`}
                      divider={index < project.pastEmployees.length - 1}
                      sx={{ cursor: 'pointer', px: 0 }}
                      onClick={() => navigate(paths.employeeDetail(member.employeeId))}
                    >
                      <ListItemAvatar>
                        <Avatar sx={{ bgcolor: 'action.selected' }}>
                          {crewInitials(member.fullName)}
                        </Avatar>
                      </ListItemAvatar>
                      <ListItemText
                        primary={member.fullName}
                        secondary={
                          [
                            `${member.position} · ${postingRange(member, t)}`,
                            workedSummary(member, t, locale),
                          ]
                            .filter(Boolean)
                            .join(' · ')
                        }
                      />
                    </ListItem>
                  ))}
                </List>
              </CardContent>
            </Card>
          </Grid>
        )}

        <Grid size={12}>
          <Card>
            <CardContent>
              <AttachmentList
                ownerType="Project"
                ownerId={project.id}
                categories={['SiteDocument', 'Photo', 'Licence', 'Insurance', 'Other']}
                canDelete={canAdministerAccounts(user)}
              />
            </CardContent>
          </Card>
        </Grid>

      </Grid>

      <ConfirmDialog
        open={confirmDelete}
        title={t('projects.deleteTitle')}
        description={t('projects.deleteBody', { name: project.name })}
        confirmLabel={t('common.delete')}
        destructive
        loading={deleteProject.isPending}
        onConfirm={handleDelete}
        onCancel={() => setConfirmDelete(false)}
      />

      <ConfirmDialog
        open={!!removeEmployeeTarget}
        title={t('projects.removeCrewTitle')}
        description={
          removeEmployeeTarget
            ? t('projects.removeCrewBody', { name: removeEmployeeTarget.name })
            : ''
        }
        confirmLabel={t('common.delete')}
        destructive
        loading={removeEmployee.isPending}
        onConfirm={handleRemoveEmployee}
        onCancel={() => setRemoveEmployeeTarget(null)}
      />

      <ConfirmDialog
        open={!!removeVehicleTarget}
        title={t('projects.removeVehicleTitle')}
        description={
          removeVehicleTarget
            ? t('projects.removeVehicleBody', { name: removeVehicleTarget.name })
            : ''
        }
        confirmLabel={t('common.delete')}
        destructive
        loading={unassignVehicle.isPending}
        onConfirm={handleRemoveVehicle}
        onCancel={() => setRemoveVehicleTarget(null)}
      />

      <ConfirmDialog
        open={!!removeToolTarget}
        title={t('projects.removeToolTitle')}
        description={
          removeToolTarget ? t('projects.removeToolBody', { name: removeToolTarget.name }) : ''
        }
        confirmLabel={t('common.delete')}
        destructive
        loading={unassignTool.isPending}
        onConfirm={handleRemoveTool}
        onCancel={() => setRemoveToolTarget(null)}
      />
    </Box>
  );
}

function crewInitials(fullName: string): string {
  const [first, last] = fullName.trim().split(/\s+/);
  return initialsOf(first, last, fullName);
}

function InfoRow({ label, value }: { label: string; value: string | null | undefined }) {
  return (
    <Box>
      <Typography variant="caption" color="text.secondary">
        {label}
      </Typography>
      <Typography variant="body1">{value || '—'}</Typography>
    </Box>
  );
}
