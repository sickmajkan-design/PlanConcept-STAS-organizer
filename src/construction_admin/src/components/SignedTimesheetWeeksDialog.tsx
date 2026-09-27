import { CheckCircleOutlined, ExpandLessOutlined, ExpandMoreOutlined } from '@mui/icons-material';
import {
  Alert,
  Box,
  Chip,
  Dialog,
  DialogContent,
  DialogTitle,
  IconButton,
  List,
  ListItemButton,
  Stack,
  Typography,
} from '@mui/material';
import { useState } from 'react';

import type { SignedTimesheetWeek } from '../api/types';
import { useGetOrCreateSignedTimesheet, useSignedTimesheetWeeksQuery } from '../features/signedTimesheets/useSignedTimesheets';
import { useT } from '../i18n/useI18n';
import { formatDate } from '../utils/formatting';
import { AttachmentList } from './AttachmentList';
import { ErrorState } from './ErrorState';

/**
 * The client's signed timesheet, per calendar week, for one project — A3 in the
 * customer's answers. A week's row is created lazily, the first time somebody
 * opens it to attach a scan; until then it only exists as a date range.
 */
export function SignedTimesheetWeeksDialog({
  open,
  onClose,
  projectId,
  projectName,
  year,
  month,
}: {
  open: boolean;
  onClose: () => void;
  projectId: string;
  projectName: string;
  year: number;
  month: number;
}) {
  const t = useT();
  const { data: weeks, isError, error, refetch } = useSignedTimesheetWeeksQuery(
    { projectId, year, month },
    open,
  );
  const getOrCreate = useGetOrCreateSignedTimesheet();

  const [expanded, setExpanded] = useState<number | null>(null);
  // Week -> resolved row id, filled in the first time a week is opened.
  const [rowIds, setRowIds] = useState<Record<number, string>>({});

  const toggle = async (week: SignedTimesheetWeek) => {
    if (expanded === week.isoWeek) {
      setExpanded(null);
      return;
    }

    if (!(week.isoWeek in rowIds)) {
      const id = week.signedTimesheetId ?? (await getOrCreate.mutateAsync({
        projectId,
        year: week.isoYear,
        isoWeek: week.isoWeek,
      }));

      setRowIds((prev) => ({ ...prev, [week.isoWeek]: id }));
    }

    setExpanded(week.isoWeek);
  };

  const close = () => {
    setExpanded(null);
    onClose();
  };

  return (
    <Dialog open={open} onClose={close} fullWidth maxWidth="sm">
      <DialogTitle>{t('signedTimesheets.title', { project: projectName })}</DialogTitle>
      <DialogContent>
        {isError && <ErrorState error={error} onRetry={() => void refetch()} />}

        {!isError && !weeks && (
          <Typography color="text.secondary">{t('common.loading')}</Typography>
        )}

        {!isError && weeks && weeks.length === 0 && (
          <Alert severity="info">{t('signedTimesheets.noWeeks')}</Alert>
        )}

        {!isError && weeks && weeks.length > 0 && (
          <List dense disablePadding>
            {weeks.map((week) => (
              <Box key={week.isoWeek}>
                <ListItemButton
                  divider
                  onClick={() => void toggle(week)}
                  sx={{ px: 1 }}
                >
                  <Stack
                    direction="row"
                    spacing={1}
                    sx={{ alignItems: 'center', flex: 1 }}
                  >
                    <Typography variant="body2" sx={{ fontWeight: 600, minWidth: 60 }}>
                      KW{week.isoWeek}
                    </Typography>
                    <Typography variant="body2" color="text.secondary" sx={{ flex: 1 }}>
                      {formatDate(week.from)} – {formatDate(week.to)}
                    </Typography>
                    {week.hasAttachment ? (
                      <Chip
                        size="small"
                        color="success"
                        icon={<CheckCircleOutlined fontSize="small" />}
                        label={t('signedTimesheets.filed')}
                      />
                    ) : (
                      <Chip size="small" variant="outlined" label={t('signedTimesheets.notFiled')} />
                    )}
                  </Stack>
                  <IconButton size="small">
                    {expanded === week.isoWeek ? (
                      <ExpandLessOutlined fontSize="small" />
                    ) : (
                      <ExpandMoreOutlined fontSize="small" />
                    )}
                  </IconButton>
                </ListItemButton>

                {expanded === week.isoWeek && rowIds[week.isoWeek] && (
                  <Box sx={{ px: 1, py: 2 }}>
                    <AttachmentList
                      ownerType="SignedTimesheet"
                      ownerId={rowIds[week.isoWeek]!}
                      categories={['Other']}
                      canDelete
                    />
                  </Box>
                )}
              </Box>
            ))}
          </List>
        )}
      </DialogContent>
    </Dialog>
  );
}
