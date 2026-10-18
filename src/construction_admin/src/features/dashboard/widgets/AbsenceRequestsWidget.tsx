import { CheckOutlined, CloseOutlined } from '@mui/icons-material';
import { Box, IconButton, Stack, Tooltip } from '@mui/material';
import { useState } from 'react';

import type { Absence } from '../../../api/types';
import { useAuth } from '../../../auth/useAuth';
import { ReasonDialog } from '../../../components/ReasonDialog';
import { useEnumLabel } from '../../../i18n/enumLabels';
import { useT } from '../../../i18n/useI18n';
import { paths } from '../../../routes/paths';
import { formatDate } from '../../../utils/formatting';
import { ApproveAbsenceDialog } from '../../absences/ApproveAbsenceDialog';
import { useAbsencesQuery, useReviewAbsence } from '../../absences/useAbsences';
import type { DashboardWidgetProps } from '../widgetTypes';
import { WidgetShell } from './WidgetShell';
import { InsetList, InsetRow, MoreLink } from './widgetUi';

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
      empty={items.length === 0 ? { text: t('dashboard.absenceRequests.empty') } : undefined}
    >
      {items.length > 0 && (
        <Box sx={{ flex: 1, minHeight: 0, overflow: 'auto' }}>
          <InsetList>
            {items.map((absence) => {
              const own = !!user?.employeeId && absence.employeeId === user.employeeId;

              return (
                <InsetRow
                  key={absence.id}
                  primary={absence.employeeName}
                  secondary={`${enumLabel('absenceType', absence.type)} · ${formatDate(absence.startDate)} - ${formatDate(absence.endDate)} · ${t('dashboard.absenceRequests.days', { count: absence.dayCount })}`}
                  action={
                    <Stack direction="row" spacing={0.5} sx={{ flexShrink: 0 }}>
                      <Tooltip title={own ? t('dashboard.absenceRequests.own') : t('absences.approve')}>
                        <span>
                          <IconButton
                            size="small"
                            color="success"
                            aria-label={t('absences.approve')}
                            disabled={own || review.isPending}
                            onClick={() => setGranting(absence)}
                            sx={{ bgcolor: 'action.hover' }}
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
                            sx={{ bgcolor: 'action.hover' }}
                          >
                            <CloseOutlined fontSize="small" />
                          </IconButton>
                        </span>
                      </Tooltip>
                    </Stack>
                  }
                />
              );
            })}
          </InsetList>
        </Box>
      )}

      <MoreLink to={paths.absences}>
        {total > items.length ? t('dashboard.absenceRequests.viewAll', { count: total }) : t('common.viewAll')}
      </MoreLink>

      <ApproveAbsenceDialog absence={granting} onClose={() => setGranting(null)} />

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
