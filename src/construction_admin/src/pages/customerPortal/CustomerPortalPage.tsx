import { LogoutOutlined } from '@mui/icons-material';
import {
  AppBar,
  Box,
  Card,
  CardContent,
  Chip,
  CircularProgress,
  Container,
  IconButton,
  LinearProgress,
  Stack,
  Toolbar,
  Tooltip,
  Typography,
} from '@mui/material';

import { useAuth } from '../../auth/useAuth';
import { EmptyState } from '../../components/EmptyState';
import { ErrorState } from '../../components/ErrorState';
import { useMyCustomerProjectsQuery } from '../../features/customerPortal/useCustomerPortal';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useT } from '../../i18n/useI18n';
import { formatDate } from '../../utils/formatting';

const STATUS_COLOR: Record<string, 'default' | 'info' | 'success' | 'warning'> = {
  Planned: 'default',
  Active: 'info',
  OnHold: 'warning',
  Completed: 'success',
  Cancelled: 'default',
};

/**
 * The one screen a customer login ever sees: their own project's status,
 * read-only. Deliberately its own top-level page, outside `AppLayout` — the
 * internal sidebar and every link on it lead somewhere this account may not
 * go, so it is never rendered here at all rather than merely hidden.
 */
export function CustomerPortalPage() {
  const t = useT();
  const enumLabel = useEnumLabel();
  const { user, signOut } = useAuth();
  const { data, isLoading, isError, error, refetch } = useMyCustomerProjectsQuery();

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
      <AppBar position="static" color="default" elevation={0} sx={{ borderBottom: 1, borderColor: 'divider' }}>
        <Toolbar>
          <Typography variant="h6" sx={{ flexGrow: 1, fontWeight: 700 }}>
            {t('customerPortal.title')}
          </Typography>
          {user && (
            <Typography variant="body2" color="text.secondary" sx={{ mr: 2 }}>
              {user.email}
            </Typography>
          )}
          <Tooltip title={t('common.signOut')}>
            <IconButton onClick={() => void signOut()} aria-label={t('common.signOut')}>
              <LogoutOutlined />
            </IconButton>
          </Tooltip>
        </Toolbar>
      </AppBar>

      <Container maxWidth="md" sx={{ py: 4 }}>
        <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
          {t('customerPortal.subtitle')}
        </Typography>

        {isLoading && (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 6 }}>
            <CircularProgress />
          </Box>
        )}

        {isError && <ErrorState error={error} onRetry={() => void refetch()} />}

        {!isLoading && !isError && (!data || data.length === 0) && (
          <EmptyState message={t('customerPortal.empty')} />
        )}

        <Stack spacing={2}>
          {data?.map((project) => (
            <Card key={project.projectId} variant="outlined">
              <CardContent>
                <Stack
                  direction="row"
                  sx={{ justifyContent: 'space-between', alignItems: 'flex-start', mb: 1 }}
                >
                  <Typography variant="h6" sx={{ fontWeight: 700 }}>
                    {project.projectName}
                  </Typography>
                  <Chip
                    label={enumLabel('projectStatus', project.status)}
                    color={STATUS_COLOR[project.status] ?? 'default'}
                    size="small"
                  />
                </Stack>

                {project.address && (
                  <Typography variant="body2" color="text.secondary">
                    {project.address}
                  </Typography>
                )}

                {(project.startDate || project.endDate) && (
                  <Typography variant="body2" color="text.secondary">
                    {project.startDate ? formatDate(project.startDate) : '—'}
                    {' – '}
                    {project.endDate ? formatDate(project.endDate) : t('customerPortal.ongoing')}
                  </Typography>
                )}

                {project.percentComplete !== null && (
                  <Box sx={{ mt: 2 }}>
                    <Stack direction="row" sx={{ justifyContent: 'space-between', mb: 0.5 }}>
                      <Typography variant="caption" color="text.secondary">
                        {t('customerPortal.progress')}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        {project.percentComplete}%
                      </Typography>
                    </Stack>
                    <LinearProgress
                      variant="determinate"
                      value={project.percentComplete}
                      sx={{ height: 8, borderRadius: 4 }}
                    />
                  </Box>
                )}
              </CardContent>
            </Card>
          ))}
        </Stack>
      </Container>
    </Box>
  );
}
