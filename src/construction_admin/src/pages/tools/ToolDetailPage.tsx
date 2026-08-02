import {
  ApartmentOutlined,
  DeleteOutlined,
  EditOutlined,
  HandymanOutlined,
  PersonOffOutlined,
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
  MenuItem,
  Select,
  Stack,
  Typography,
} from '@mui/material';
import { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';

import { toApiError } from '../../api/apiError';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { ErrorState } from '../../components/ErrorState';
import { StatusChip } from '../../components/StatusChip';
import { useAllEmployeesQuery } from '../../features/employees/useEmployees';
import { useAllProjectsQuery } from '../../features/projects/useProjects';
import {
  useAssignToolEmployee,
  useAssignToolProject,
  useDeleteTool,
  useToolQuery,
  useUnassignToolEmployee,
  useUnassignToolProject,
} from '../../features/tools/useTools';
import { useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';

export function ToolDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const t = useT();

  const { data: tool, isLoading, isError, error, refetch } = useToolQuery(id);
  const { data: allEmployees } = useAllEmployeesQuery();
  const { data: allProjects } = useAllProjectsQuery();

  const assignEmployee = useAssignToolEmployee(id ?? '');
  const unassignEmployee = useUnassignToolEmployee(id ?? '');
  const assignProject = useAssignToolProject(id ?? '');
  const unassignProject = useUnassignToolProject(id ?? '');
  const deleteTool = useDeleteTool();

  const [selectedEmployeeId, setSelectedEmployeeId] = useState('');
  const [selectedProjectId, setSelectedProjectId] = useState('');
  const [confirmDelete, setConfirmDelete] = useState(false);
  const [confirmUnassignEmployee, setConfirmUnassignEmployee] = useState(false);
  const [confirmUnassignProject, setConfirmUnassignProject] = useState(false);

  if (isLoading) return null;
  if (isError || !tool) {
    return <ErrorState error={error} onRetry={() => void refetch()} />;
  }

  const handleAssignEmployee = async () => {
    if (!selectedEmployeeId) return;
    await assignEmployee.mutateAsync(selectedEmployeeId);
    setSelectedEmployeeId('');
  };

  const handleUnassignEmployee = async () => {
    await unassignEmployee.mutateAsync();
    setConfirmUnassignEmployee(false);
  };

  const handleAssignProject = async () => {
    if (!selectedProjectId) return;
    await assignProject.mutateAsync(selectedProjectId);
    setSelectedProjectId('');
  };

  const handleUnassignProject = async () => {
    await unassignProject.mutateAsync();
    setConfirmUnassignProject(false);
  };

  const handleDelete = async () => {
    await deleteTool.mutateAsync(tool.id);
    navigate(paths.tools, { replace: true });
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
            <Avatar sx={{ width: 64, height: 64, bgcolor: 'primary.main' }}>
              <HandymanOutlined />
            </Avatar>
            <Box sx={{ flex: 1 }}>
              <Typography variant="h5" sx={{ fontWeight: 700 }}>
                {tool.name}
              </Typography>
              <Typography color="text.secondary">{tool.category ?? t('tools.uncategorised')}</Typography>
              <Stack direction="row" spacing={1} sx={{ mt: 1.5, alignItems: 'center' }}>
                <StatusChip status={tool.status} kind="toolStatus" />
              </Stack>
            </Box>
            <Stack direction="row" spacing={1}>
              <Button
                variant="outlined"
                startIcon={<EditOutlined />}
                onClick={() => navigate(paths.toolEdit(tool.id))}
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
                Tool
              </Typography>
              <Stack spacing={1.5} sx={{ mt: 1 }}>
                <InfoRow label={t('tools.serialNumber')} value={tool.serialNumber} />
                <InfoRow label={t('tools.qrCode')} value={tool.qrCode} />
              </Stack>
            </CardContent>
          </Card>
        </Grid>

        <Grid size={{ xs: 12, sm: 6 }}>
          <Card>
            <CardContent>
              <Typography variant="subtitle1" gutterBottom sx={{ fontWeight: 700 }}>
                Employee
              </Typography>

              {tool.assignedEmployeeId ? (
                <Stack spacing={1.5} sx={{ mt: 1 }}>
                  <Typography>
                    Held by <strong>{tool.assignedEmployeeName}</strong> (
                    {tool.assignedEmployeeNumber})
                  </Typography>
                  <Box>
                    <Button
                      size="small"
                      color="error"
                      variant="outlined"
                      startIcon={<PersonOffOutlined />}
                      onClick={() => setConfirmUnassignEmployee(true)}
                    >
                      Unassign
                    </Button>
                  </Box>
                </Stack>
              ) : (
                <Stack spacing={2} sx={{ mt: 1 }}>
                  <Typography color="text.secondary">{t('tools.notHeldSentence')}</Typography>
                  <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
                    <FormControl size="small" sx={{ minWidth: 220 }}>
                      <Select
                        displayEmpty
                        value={selectedEmployeeId}
                        onChange={(event) => setSelectedEmployeeId(event.target.value)}
                      >
                        <MenuItem value="">
                          <em>Select an employee…</em>
                        </MenuItem>
                        {(allEmployees?.items ?? []).map((employee) => (
                          <MenuItem key={employee.id} value={employee.id}>
                            {employee.fullName}
                          </MenuItem>
                        ))}
                      </Select>
                    </FormControl>
                    <Button
                      variant="outlined"
                      disabled={!selectedEmployeeId}
                      loading={assignEmployee.isPending}
                      onClick={handleAssignEmployee}
                    >
                      Assign
                    </Button>
                  </Stack>
                </Stack>
              )}

              {(assignEmployee.isError || unassignEmployee.isError) && (
                <Alert severity="error" sx={{ mt: 2 }}>
                  {toApiError(assignEmployee.error ?? unassignEmployee.error).message}
                </Alert>
              )}
            </CardContent>
          </Card>
        </Grid>

        <Grid size={12}>
          <Card>
            <CardContent>
              <Typography variant="subtitle1" gutterBottom sx={{ fontWeight: 700 }}>
                Project
              </Typography>

              {tool.assignedProjectId ? (
                <Stack
                  direction={{ xs: 'column', sm: 'row' }}
                  spacing={1.5}
                  sx={{ mt: 1, alignItems: { sm: 'center' } }}
                >
                  <Avatar sx={{ bgcolor: 'action.selected' }}>
                    <ApartmentOutlined />
                  </Avatar>
                  <Typography
                    sx={{ flex: 1, cursor: 'pointer' }}
                    onClick={() => navigate(paths.projectDetail(tool.assignedProjectId!))}
                  >
                    {tool.assignedProjectName}
                  </Typography>
                  <Button
                    size="small"
                    color="error"
                    variant="outlined"
                    onClick={() => setConfirmUnassignProject(true)}
                  >
                    Remove from project
                  </Button>
                </Stack>
              ) : (
                <Stack spacing={2} sx={{ mt: 1 }}>
                  <Typography color="text.secondary">{t('tools.notPlacedSentence')}</Typography>
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
                        {(allProjects?.items ?? []).map((project) => (
                          <MenuItem key={project.id} value={project.id}>
                            {project.name}
                          </MenuItem>
                        ))}
                      </Select>
                    </FormControl>
                    <Button
                      variant="outlined"
                      disabled={!selectedProjectId}
                      loading={assignProject.isPending}
                      onClick={handleAssignProject}
                    >
                      Place on project
                    </Button>
                  </Stack>
                </Stack>
              )}

              {(assignProject.isError || unassignProject.isError) && (
                <Alert severity="error" sx={{ mt: 2 }}>
                  {toApiError(assignProject.error ?? unassignProject.error).message}
                </Alert>
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      <Divider sx={{ my: 3 }} />

      <ConfirmDialog
        open={confirmUnassignEmployee}
        title={t('tools.unassignTitle')}
        description={`${tool.name} will no longer be held by ${tool.assignedEmployeeName ?? 'this employee'}.`}
        confirmLabel="Unassign"
        destructive
        loading={unassignEmployee.isPending}
        onConfirm={handleUnassignEmployee}
        onCancel={() => setConfirmUnassignEmployee(false)}
      />

      <ConfirmDialog
        open={confirmUnassignProject}
        title={t('tools.unassignProject')}
        description={`${tool.name} will no longer be placed at ${tool.assignedProjectName ?? 'this project'}.`}
        confirmLabel={t('employees.removeFromProject')}
        destructive
        loading={unassignProject.isPending}
        onConfirm={handleUnassignProject}
        onCancel={() => setConfirmUnassignProject(false)}
      />

      <ConfirmDialog
        open={confirmDelete}
        title={t('tools.deleteTitle')}
        description={`${tool.name} will be removed from active records.`}
        confirmLabel={t('common.delete')}
        destructive
        loading={deleteTool.isPending}
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
