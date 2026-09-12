import { EventBusyOutlined, WarningAmberOutlined } from '@mui/icons-material';
import { List, ListItem, ListItemIcon, ListItemText, Stack, Typography } from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';

import { absencesApi } from '../../../api/absences';
import { attachmentsApi } from '../../../api/attachments';
import { useT } from '../../../i18n/useI18n';
import { paths } from '../../../routes/paths';
import { formatDate } from '../../../utils/formatting';
import type { DashboardWidgetProps } from '../widgetTypes';
import { WidgetShell } from './WidgetShell';

const DOCUMENT_WINDOW_DAYS = 30;
const SHOWN = 5;

/**
 * A single actionable list gathering what the small red badges on the nav
 * rail only hint at (expiring documents, pending absence requests) — so
 * "something needs attention" has one place to click through from, instead
 * of visiting each section to find out what.
 *
 * Each source is queried and rendered independently: a viewer without one
 * permission (e.g. no `AdminAndAbove` for documents) still sees the other
 * section rather than the whole widget falling over on one 403.
 */
export function NeedsAttentionWidget({
  instanceId: _instanceId,
  dragHandleProps,
  onRemove,
}: DashboardWidgetProps) {
  const t = useT();

  const documentsQuery = useQuery({
    queryKey: ['dashboard', 'needs-attention', 'documents'] as const,
    queryFn: () => attachmentsApi.expiring(DOCUMENT_WINDOW_DAYS),
  });

  const absencesQuery = useQuery({
    queryKey: ['dashboard', 'needs-attention', 'absences'] as const,
    queryFn: () => absencesApi.list({ pageNumber: 1, pageSize: SHOWN, status: 'Requested' }),
  });

  const documents = Array.isArray(documentsQuery.data) ? documentsQuery.data.slice(0, SHOWN) : [];
  const absences = absencesQuery.data?.items ?? [];
  const isLoading = documentsQuery.isLoading || absencesQuery.isLoading;
  const bothFailed = !!documentsQuery.error && !!absencesQuery.error;
  const isEmpty = !isLoading && !bothFailed && documents.length === 0 && absences.length === 0;

  return (
    <WidgetShell
      title={t('dashboard.widget.NeedsAttention')}
      isLoading={isLoading}
      error={bothFailed ? documentsQuery.error : undefined}
      onRemove={onRemove}
      dragHandleProps={dragHandleProps}
    >
      {isEmpty ? (
        <Typography color="text.secondary" variant="body2">
          {t('dashboard.needsAttention.empty')}
        </Typography>
      ) : (
        <Stack spacing={0.5}>
          {!documentsQuery.error && documents.length > 0 && (
            <List dense disablePadding>
              {documents.map((doc) => (
                <ListItem
                  key={doc.id}
                  disableGutters
                  component={Link}
                  to={paths.expiringDocuments}
                  sx={{ color: 'inherit', textDecoration: 'none' }}
                >
                  <ListItemIcon sx={{ minWidth: 32 }}>
                    <WarningAmberOutlined fontSize="small" color="warning" />
                  </ListItemIcon>
                  <ListItemText
                    primary={doc.fileName}
                    secondary={t('dashboard.needsAttention.documentExpires', {
                      date: formatDate(doc.expiresAt),
                    })}
                  />
                </ListItem>
              ))}
            </List>
          )}
          {!absencesQuery.error && absences.length > 0 && (
            <List dense disablePadding>
              {absences.map((absence) => (
                <ListItem
                  key={absence.id}
                  disableGutters
                  component={Link}
                  to={paths.absences}
                  sx={{ color: 'inherit', textDecoration: 'none' }}
                >
                  <ListItemIcon sx={{ minWidth: 32 }}>
                    <EventBusyOutlined fontSize="small" color="warning" />
                  </ListItemIcon>
                  <ListItemText
                    primary={absence.employeeName}
                    secondary={t('dashboard.needsAttention.absencePending', {
                      type: absence.type,
                    })}
                  />
                </ListItem>
              ))}
            </List>
          )}
        </Stack>
      )}
    </WidgetShell>
  );
}
