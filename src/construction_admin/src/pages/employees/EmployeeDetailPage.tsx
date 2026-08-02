import {
  AddOutlined,
  ApartmentOutlined,
  DeleteOutlined,
  EditOutlined,
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
import { useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';

import { toApiError } from '../../api/apiError';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { ErrorState } from '../../components/ErrorState';
import { StatusChip } from '../../components/StatusChip';
import {
  useAssignEmployeeToProject,
  useDeleteEmployee,
  useEmployeeQuery,
  useRemoveEmployeeFromProject,
} from '../../features/employees/useEmployees';
import { useAllProjectsQuery } from '../../features/projects/useProjects';
import { useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';
import { formatDate, initialsOf } from '../../utils/formatting';

export function EmployeeDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const t = useT();

  const { data: employee, isLoading, isError, error, refetch } = useEmployeeQuery(id);
  const { data: allProjects } = useAllProjectsQuery();

  const assign = useAssignEmployeeToProject(id ?? '');
  const remove = useRemoveEmployeeFromProject(id ?? '');
  const deleteEmployee = useDeleteEmployee();

  const [selectedProjectId, setSelectedProjectId] = useState('');
  const [confirmDelete, setConfirmDelete] = useState(false);
  const [removeTarget, setRemoveTarget] = useState<{ id: string; name: string } | null>(
    null,
  );

  const assignableProjects = useMemo(() => {
    const assignedIds = new Set(employee?.projects.map((p) => p.projectId));
    return (allProjects?.items ?? []).filter((project) => !assignedIds.has(project.id));
  }, [allProjects, employee]);

  if (isLoading) return null;
  if (isError || !employee) {
    return <ErrorState error={error} onRetry={() => void refetch()} />;
  }

  const handleAssign = async () => {
    if (!selectedProjectId) return;
    await assign.mutateAsync(selectedProjectId);
    setSelectedProjectId('');
  };

  const handleRemove = async () => {
    if (!removeTarget) return;
    await remove.mutateAsync(removeTarget.id);
    setRemoveTarget(null);
  };

  const handleDelete = async () => {
    await deleteEmployee.mutateAsync(employee.id);
    navigate(paths.employees, { replace: true });
  };

  return (
    <Box sx={{ maxWidth: 900 }}>
      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Stack
            direction={{ xs: 'column', sm: 'row' }}
            spacing={3}
            sx={{ alignItems: 'flex-start' }}
          >
            <Avatar sx={{ width: 64, height: 64, bgcolor: 'primary.main', fontSize: 24 }}>
              {initialsOf(employee.firstName, employee.lastName)}
            </Avatar>
            <Box sx={{ flex: 1 }}>
              <Typography variant="h5" sx={{ fontWeight: 700 }}>
                {employee.fullName}
              </Typography>
              <Typography color="text.secondary">{employee.position}</Typography>
              <Stack direction="row" spacing={1} sx={{ mt: 1.5, alignItems: 'center' }}>
                <StatusChip status={employee.status} kind="employeeStatus" />
                <Typography variant="body2" color="text.secondary">
                  · {employee.employeeNumber}
                </Typography>
              </Stack>
            </Box>
            <Stack direction="row" spacing={1}>
              <Button
                variant="outlined"
                startIcon={<EditOutlined />}
                onClick={() => navigate(paths.employeeEdit(employee.id))}
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
        </CardContent>
      </Card>

      <Grid container spacing={3}>
        <Grid size={{ xs: 12, sm: 6 }}>
          <Card>
            <CardContent>
              <Typography variant="subtitle1" gutterBottom sx={{ fontWeight: 700 }}>
                Contact
              </Typography>
              <Stack spacing={1.5} sx={{ mt: 1 }}>
                <InfoRow label={t('employees.phone')} value={employee.phone} />
                <InfoRow label={t('employees.email')} value={employee.email} />
                <InfoRow label={t('employees.address')} value={employee.address} />
              </Stack>
            </CardContent>
          </Card>
        </Grid>
        <Grid size={{ xs: 12, sm: 6 }}>
          <Card>
            <CardContent>
              <Typography variant="subtitle1" gutterBottom sx={{ fontWeight: 700 }}>
                Employment
              </Typography>
              <Stack spacing={1.5} sx={{ mt: 1 }}>
                <InfoRow label={t('employees.employedSince')} value={formatDate(employee.employmentDate)} />
                <InfoRow
                  label={t('employees.dateOfBirth')}
                  value={employee.dateOfBirth ? formatDate(employee.dateOfBirth) : null}
                />
                <InfoRow
                  label={t('employees.appAccount')}
                  value={employee.hasUserAccount ? 'Yes' : 'No'}
                />
              </Stack>
            </CardContent>
          </Card>
        </Grid>

        <Grid size={12}>
          <Card>
            <CardContent>
              <Typography variant="subtitle1" gutterBottom sx={{ fontWeight: 700 }}>
                Projects ({employee.projects.length})
              </Typography>

              {employee.projects.length === 0 ? (
                <Typography color="text.secondary" sx={{ py: 2 }}>
                  Not assigned to any project.
                </Typography>
              ) : (
                <List disablePadding>
                  {employee.projects.map((assignment) => (
                    <ListItem
                      key={assignment.projectId}
                      divider
                      sx={{ cursor: 'pointer' }}
                      onClick={() => navigate(paths.projectDetail(assignment.projectId))}
                      secondaryAction={
                        <Tooltip title={t('employees.removeFromProject')}>
                          <IconButton
                            edge="end"
                            onClick={(event) => {
                              event.stopPropagation();
                              setRemoveTarget({
                                id: assignment.projectId,
                                name: assignment.projectName,
                              });
                            }}
                          >
                            <DeleteOutlined fontSize="small" />
                          </IconButton>
                        </Tooltip>
                      }
                    >
                      <ListItemAvatar>
                        <Avatar>
                          <ApartmentOutlined />
                        </Avatar>
                      </ListItemAvatar>
                      <ListItemText
                        primary={assignment.projectName}
                        secondary={`Assigned ${formatDate(assignment.assignedAt)}`}
                      />
                      <StatusChip status={assignment.projectStatus} kind="projectStatus" />
                    </ListItem>
                  ))}
                </List>
              )}

              <Divider sx={{ my: 2 }} />

              <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
                <FormControl size="small" sx={{ minWidth: 260 }}>
                  <Select
                    displayEmpty
                    value={selectedProjectId}
                    onChange={(event) => setSelectedProjectId(event.target.value)}
                  >
                    <MenuItem value="">
                      <em>Select a project…</em>
                    </MenuItem>
                    {assignableProjects.map((project) => (
                      <MenuItem key={project.id} value={project.id}>
                        {project.name}
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
                <Button
                  variant="outlined"
                  startIcon={<AddOutlined />}
                  disabled={!selectedProjectId}
                  loading={assign.isPending}
                  onClick={handleAssign}
                >
                  Assign to project
                </Button>
              </Stack>

              {assign.isError && (
                <Alert severity="error" sx={{ mt: 2 }}>
                  {toApiError(assign.error).message}
                </Alert>
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      <ConfirmDialog
        open={!!removeTarget}
        title={t('employees.removeAssignmentTitle')}
        description={
          removeTarget
            ? `${employee.fullName} will no longer be assigned to ${removeTarget.name}.`
            : ''
        }
        confirmLabel={t('employees.removeFromProject')}
        destructive
        loading={remove.isPending}
        onConfirm={handleRemove}
        onCancel={() => setRemoveTarget(null)}
      />

      <ConfirmDialog
        open={confirmDelete}
        title={t('employees.deleteTitle')}
        description={`${employee.fullName} (${employee.employeeNumber}) will be removed from active records.`}
        confirmLabel={t('common.delete')}
        destructive
        loading={deleteEmployee.isPending}
        onConfirm={handleDelete}
        onCancel={() => setConfirmDelete(false)}
      />
    </Box>
  );
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
