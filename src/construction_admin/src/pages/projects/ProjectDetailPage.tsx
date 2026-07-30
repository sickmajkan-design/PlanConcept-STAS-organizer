import { DeleteOutlined, EditOutlined } from '@mui/icons-material';
import {
  Avatar,
  Box,
  Button,
  Card,
  CardContent,
  Grid,
  List,
  ListItem,
  ListItemAvatar,
  ListItemText,
  Stack,
  Typography,
} from '@mui/material';
import { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';

import { ConfirmDialog } from '../../components/ConfirmDialog';
import { ErrorState } from '../../components/ErrorState';
import { StatusChip } from '../../components/StatusChip';
import { useDeleteProject, useProjectQuery } from '../../features/projects/useProjects';
import { paths } from '../../routes/paths';
import { formatDate, initialsOf } from '../../utils/formatting';

export function ProjectDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const { data: project, isLoading, isError, error, refetch } = useProjectQuery(id);
  const deleteProject = useDeleteProject();
  const [confirmDelete, setConfirmDelete] = useState(false);

  if (isLoading) return null;
  if (isError || !project) {
    return <ErrorState error={error} onRetry={() => void refetch()} />;
  }

  const handleDelete = async () => {
    await deleteProject.mutateAsync(project.id);
    navigate(paths.projects, { replace: true });
  };

  const hasCoordinates = project.latitude !== null && project.longitude !== null;

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
                <StatusChip status={project.status} />
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
        <Grid size={{ xs: 12, sm: 6 }}>
          <Card>
            <CardContent>
              <Typography variant="subtitle1" gutterBottom sx={{ fontWeight: 700 }}>
                Details
              </Typography>
              <Stack spacing={1.5} sx={{ mt: 1 }}>
                <InfoRow label="Client" value={project.client} />
                <InfoRow label="Address" value={project.address} />
                <InfoRow
                  label="Coordinates"
                  value={
                    hasCoordinates
                      ? `${project.latitude!.toFixed(5)}, ${project.longitude!.toFixed(5)}`
                      : null
                  }
                />
                <InfoRow label="Start date" value={project.startDate ? formatDate(project.startDate) : null} />
                <InfoRow label="End date" value={project.endDate ? formatDate(project.endDate) : null} />
              </Stack>
            </CardContent>
          </Card>
        </Grid>

        <Grid size={{ xs: 12, sm: 6 }}>
          <Card sx={{ height: '100%' }}>
            <CardContent>
              <Typography variant="subtitle1" gutterBottom sx={{ fontWeight: 700 }}>
                Crew ({project.employees.length})
              </Typography>

              {project.employees.length === 0 ? (
                <Typography color="text.secondary" sx={{ py: 2 }}>
                  Nobody assigned yet. Assign employees from their profile page.
                </Typography>
              ) : (
                <List disablePadding>
                  {project.employees.map((member) => (
                    <ListItem
                      key={member.employeeId}
                      divider
                      sx={{ cursor: 'pointer', px: 0 }}
                      onClick={() => navigate(paths.employeeDetail(member.employeeId))}
                    >
                      <ListItemAvatar>
                        <Avatar sx={{ bgcolor: 'secondary.main' }}>
                          {crewInitials(member.fullName)}
                        </Avatar>
                      </ListItemAvatar>
                      <ListItemText
                        primary={member.fullName}
                        secondary={`${member.position} · ${member.employeeNumber}`}
                      />
                      <StatusChip status={member.status} />
                    </ListItem>
                  ))}
                </List>
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      <ConfirmDialog
        open={confirmDelete}
        title="Delete project?"
        description={`${project.name} will be removed from active records. Any tools assigned only to this project will be released.`}
        confirmLabel="Delete"
        destructive
        loading={deleteProject.isPending}
        onConfirm={handleDelete}
        onCancel={() => setConfirmDelete(false)}
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
