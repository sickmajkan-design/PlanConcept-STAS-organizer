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
import { AttachmentList } from '../../components/AttachmentList';
import { StatusChip } from '../../components/StatusChip';
import { useDeleteProject, useProjectQuery } from '../../features/projects/useProjects';
import { useI18n, useT } from '../../i18n/useI18n';
import { canAdministerAccounts } from '../../auth/authHelpers';
import { useAuth } from '../../auth/useAuth';
import { paths } from '../../routes/paths';
import { formatDate, initialsOf } from '../../utils/formatting';
import { postingRange, workedSummary } from '../../utils/postings';

export function ProjectDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const t = useT();
  const { locale } = useI18n();
  const { user } = useAuth();

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
        <Grid size={{ xs: 12, sm: 6 }}>
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
            </CardContent>
          </Card>
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
