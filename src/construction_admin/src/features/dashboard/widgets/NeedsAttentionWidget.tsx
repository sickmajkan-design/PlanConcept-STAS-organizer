import { EventBusyOutlined, WarningAmberOutlined } from '@mui/icons-material';
import { Avatar, List, ListItem, ListItemAvatar, ListItemText, Stack, Typography } from '@mui/material';
import { Link } from 'react-router-dom';

import { useAbsencesQuery } from '../../absences/useAbsences';
import { useExpiringDocumentsQuery } from '../../attachments/useAttachments';
import { useEnumLabel } from '../../../i18n/enumLabels';
import { useT } from '../../../i18n/useI18n';
import { paths } from '../../../routes/paths';
import { formatDate } from '../../../utils/formatting';
import type { DashboardWidgetProps } from '../widgetTypes';
import { WidgetShell } from './WidgetShell';

const DOCUMENT_WINDOW_DAYS = 30;
const SHOWN = 6;

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
export function NeedsAttentionWidget({ instanceId: _instanceId, onRemove, onExpandWidth }: DashboardWidgetProps) {
  const t = useT();
  const enumLabel = useEnumLabel();

  // Same query hooks the nav badge and each module's own list page use —
  // not a separate "dashboard" cache — so resolving one of these anywhere on
  // the platform updates this widget the instant it succeeds too.
  const documentsQuery = useExpiringDocumentsQuery(DOCUMENT_WINDOW_DAYS);
  const absencesQuery = useAbsencesQuery({ pageNumber: 1, pageSize: SHOWN, status: 'Requested' });

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
      onRemove={onRemove} onExpandWidth={onExpandWidth}
    >
      {isEmpty ? (
        <Typography color="text.secondary" variant="body2">
          {t('dashboard.needsAttention.empty')}
        </Typography>
      ) : (
        <Stack spacing={0.5} sx={{ flex: 1, minHeight: 0, overflow: 'auto' }}>
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
                  <ListItemAvatar sx={{ minWidth: 44 }}>
                    <Avatar sx={{ width: 32, height: 32, bgcolor: 'warning.main' }}>
                      <WarningAmberOutlined fontSize="small" />
                    </Avatar>
                  </ListItemAvatar>
                  <ListItemText
                    primary={doc.fileName}
                    secondary={t('dashboard.needsAttention.documentExpires', { date: formatDate(doc.expiresAt) })}
                    slotProps={{ primary: { noWrap: true, sx: { fontWeight: 600 } } }}
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
                  <ListItemAvatar sx={{ minWidth: 44 }}>
                    <Avatar sx={{ width: 32, height: 32, bgcolor: 'warning.main' }}>
                      <EventBusyOutlined fontSize="small" />
                    </Avatar>
                  </ListItemAvatar>
                  <ListItemText
                    primary={absence.employeeName}
                    secondary={t('dashboard.needsAttention.absencePending', { type: enumLabel('absenceType', absence.type) })}
                    slotProps={{ primary: { sx: { fontWeight: 600 } } }}
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
