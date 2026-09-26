import { CheckOutlined, CloseOutlined } from '@mui/icons-material';
import { Box, Button, Divider, IconButton, Stack, Tooltip, Typography } from '@mui/material';
import { useState } from 'react';
import { Link } from 'react-router-dom';

import type { Absence } from '../../../api/types';
import { useAuth } from '../../../auth/useAuth';
import { ConfirmDialog } from '../../../components/ConfirmDialog';
import { ReasonDialog } from '../../../components/ReasonDialog';
import { useEnumLabel } from '../../../i18n/enumLabels';
import { useT } from '../../../i18n/useI18n';
import { paths } from '../../../routes/paths';
import { formatDate } from '../../../utils/formatting';
import { useAbsencesQuery, useReviewAbsence } from '../../absences/useAbsences';
import type { DashboardWidgetProps } from '../widgetTypes';
import { WidgetShell } from './WidgetShell';

const SHOWN = 6;

/**
 * Time-off requests waiting for an answer, with the answer one press away.
 *
 * Uses the same query and the same review mutation as the absences page, so
 * an answer given here drops the nav badge, the Needs attention list and the
 * page's own list at once, and an answer given anywhere else drops this list.
 *
 * Nobody answers their own request; that is the API's rule, and the buttons
 * are switched off for those rows with the reason in a tooltip instead of
 * being left to fail.
 */
export function AbsenceRequestsWidget({ instanceId: _instanceId, onRemove, onExpandWidth }: DashboardWidgetProps) {
  const t = useT();
  const enumLabel = useEnumLabel();
  const { user } = useAuth();
  const review = useReviewAbsence();

  const query = useAbsencesQuery({ pageNumber: 1, pageSize: SHOWN, status: 'Requested' });
  const items = query.data?.items ?? [];
  const total = query.data?.totalCount ?? 0;

  const [granting, setGranting] = useState<Absence | null>(null);
  const [declining, setDeclining] = useState<Absence | null>(null);

  return (
    <WidgetShell
      title={t('dashboard.widget.AbsenceRequests')}
      isLoading={query.isLoading}
      error={query.error}
      onRemove={onRemove}
      onExpandWidth={onExpandWidth}
    >
      {items.length === 0 && !query.isLoading ? (
        <Typography color="text.secondary" variant="body2">
          {t('dashboard.absenceRequests.empty')}
        </Typography>
      ) : (
        <Stack spacing={1} sx={{ flex: 1, minHeight: 0, overflow: 'auto' }}>
          {items.map((absence, index) => {
            const own = !!user?.employeeId && absence.employeeId === user.employeeId;

            return (
              <Box key={absence.id}>
                {index > 0 && <Divider sx={{ mb: 1 }} />}
                <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                  <Box sx={{ flex: 1, minWidth: 0 }}>
                    <Typography variant="body2" sx={{ fontWeight: 700 }} noWrap>
                      {absence.employeeName}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      {enumLabel('absenceType', absence.type)} · {formatDate(absence.startDate)} - {formatDate(absence.endDate)} ·{' '}
                      {t('dashboard.absenceRequests.days', { count: absence.dayCount })}
                    </Typography>
                  </Box>
                  <Tooltip title={own ? t('dashboard.absenceRequests.own') : t('absences.approve')}>
                    <span>
                      <IconButton
                        size="small"
                        color="success"
                        aria-label={t('absences.approve')}
                        disabled={own || review.isPending}
                        onClick={() => setGranting(absence)}
                      >
                        <CheckOutlined fontSize="small" />
                      </IconButton>
                    </span>
                  </Tooltip>
                  <Tooltip title={own ? t('dashboard.absenceRequests.own') : t('absences.reject')}>
                    <span>
                      <IconButton
                        size="small"
                        color="error"
                        aria-label={t('absences.reject')}
                        disabled={own || review.isPending}
                        onClick={() => setDeclining(absence)}
                      >
                        <CloseOutlined fontSize="small" />
                      </IconButton>
                    </span>
                  </Tooltip>
                </Stack>
              </Box>
            );
          })}
        </Stack>
      )}

      <Typography variant="body2" sx={{ flexShrink: 0, mt: 1 }}>
        <Button component={Link} to={paths.absences} size="small">
          {total > items.length
            ? t('dashboard.absenceRequests.viewAll', { count: total })
            : t('common.viewAll')}
        </Button>
      </Typography>

      <ConfirmDialog
        open={!!granting}
        title={t('absences.approveTitle')}
        description={t('absences.approveBody')}
        confirmLabel={t('absences.approve')}
        onConfirm={async () => {
          if (!granting) return;

          // Awaited so a refusal (someone else answered first, a lost
          // connection) is thrown to the dialog and shown there.
          await review.mutateAsync({ id: granting.id, input: { approve: true } });
          setGranting(null);
        }}
        onCancel={() => setGranting(null)}
      />

      <ReasonDialog
        open={!!declining}
        title={t('absences.rejectTitle')}
        hint={t('absences.rejectHint')}
        label={t('absences.rejectReason')}
        submitLabel={t('absences.reject')}
        onClose={() => setDeclining(null)}
        onSubmit={async (note) => {
          if (!declining) return;

          await review.mutateAsync({ id: declining.id, input: { approve: false, note } });
          setDeclining(null);
        }}
      />
    </WidgetShell>
  );
}
