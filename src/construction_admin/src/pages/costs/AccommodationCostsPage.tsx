import { AddOutlined, ChevronRightOutlined, HomeWorkOutlined } from '@mui/icons-material';
import {
  Box,
  Button,
  Chip,
  CircularProgress,
  FormControlLabel,
  LinearProgress,
  Link,
  Paper,
  Stack,
  Switch,
  Typography,
  alpha,
} from '@mui/material';
import { useMemo, useState } from 'react';
import { Link as RouterLink } from 'react-router-dom';

import type { AccommodationRate } from '../../api/types';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { EmptyState } from '../../components/EmptyState';
import { ErrorState } from '../../components/ErrorState';
import { PageHeader } from '../../components/PageHeader';
import {
  useAccommodationRatesQuery,
  useAccommodationRatesSummaryQuery,
  useDeleteAccommodationRate,
} from '../../features/costs/useCosts';
import { useDeleteWithConfirm } from '../../hooks/useDeleteWithConfirm';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useI18n, useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';
import { dateOnlyOffset, formatDate, formatMoney } from '../../utils/formatting';
import { ChargeDialog, KIND_UNIT } from '../accommodations/AccommodationDetailPage';
import { ChargeDetailDialog, KIND_ICON } from './ChargeDetailDialog';

// A firm houses a few dozen places at most: one board, no page numbers.
const FULL_LIST = { pageNumber: 1, pageSize: 100 };

const numeric = { fontVariantNumeric: 'tabular-nums' } as const;

function Stat({ label, value }: { label: string; value: string }) {
  return (
    <Box sx={{ flex: '1 1 180px', minWidth: 0, py: 0.5, px: 2, borderLeft: 3, borderColor: 'primary.main' }}>
      <Typography variant="caption" color="text.secondary">
        {label}
      </Typography>
      <Typography variant="h5" sx={{ fontWeight: 800, letterSpacing: -0.3, ...numeric }}>
        {value}
      </Typography>
    </Box>
  );
}

function daysBetween(from: string, to: string) {
  return (new Date(`${to}T00:00`).getTime() - new Date(`${from}T00:00`).getTime()) / 86_400_000;
}

/**
 * What housing costs the firm: every charge, grouped by the place it belongs
 * to. Opening a charge shows everything about it, including how it has run so
 * far. The places themselves and who lives in them are kept in the register.
 */
export function AccommodationCostsPage() {
  const t = useT();
  const { locale } = useI18n();
  const enumLabel = useEnumLabel();
  const [inForceOnly, setInForceOnly] = useState(true);
  const [adding, setAdding] = useState(false);
  const [opened, setOpened] = useState<AccommodationRate | null>(null);
  const [editing, setEditing] = useState<AccommodationRate | null>(null);
  const today = dateOnlyOffset(0);

  const query = useMemo(
    () => ({
      ...FULL_LIST,
      sortBy: 'startDate',
      sortDescending: true,
      currentOnly: inForceOnly || undefined,
    }),
    [inForceOnly],
  );

  const { data, isLoading, isError, error, refetch } = useAccommodationRatesQuery(query);
  const { data: summary } = useAccommodationRatesSummaryQuery(query);
  const remove = useDeleteWithConfirm<AccommodationRate>(useDeleteAccommodationRate());
  const rows = useMemo(() => data?.items ?? [], [data]);

  const groups = useMemo(() => {
    const byPlace = new Map<string, { id: string; address: string; charges: AccommodationRate[] }>();

    for (const row of rows) {
      const group = byPlace.get(row.accommodationId) ?? {
        id: row.accommodationId,
        address: row.accommodationAddress,
        charges: [],
      };
      group.charges.push(row);
      byPlace.set(row.accommodationId, group);
    }

    return [...byPlace.values()]
      .map((group) => ({
        ...group,
        monthly: group.charges
          .filter((c) => c.kind === 'Monthly' && c.startDate <= today && (!c.endDate || c.endDate >= today))
          .reduce((sum, c) => sum + c.amount, 0),
      }))
      .sort((a, b) => a.address.localeCompare(b.address));
  }, [rows, today]);

  const money = (value: number) => formatMoney(value, locale);

  return (
    <Box>
      <PageHeader
        title={t('accommodationCosts.title')}
        description={t('accommodationCosts.description')}
        action={{
          label: t('accommodations.addCharge'),
          icon: <AddOutlined />,
          onClick: () => setAdding(true),
        }}
      />

      <Paper variant="outlined" sx={{ p: 2, mb: 2 }}>
        <Stack direction="row" useFlexGap sx={{ flexWrap: 'wrap', gap: 2, alignItems: 'center' }}>
          <Stat label={t('accommodationCosts.monthlyTotalLabel')} value={money(summary?.totalMonthlyAmount ?? 0)} />
          <Stat label={t('accommodationCosts.recorded')} value={String(rows.length)} />
          <Stat label={t('accommodationCosts.places')} value={String(groups.length)} />
          <Box sx={{ flex: 1 }} />
          <Stack sx={{ alignItems: { xs: 'flex-start', sm: 'flex-end' } }}>
            <FormControlLabel
              control={<Switch checked={inForceOnly} onChange={(event) => setInForceOnly(event.target.checked)} />}
              label={t('accommodationCosts.inForceOnly')}
            />
            <Button
              size="small"
              component={RouterLink}
              to={paths.accommodations}
              startIcon={<HomeWorkOutlined />}
            >
              {t('accommodationCosts.toRegister')}
            </Button>
          </Stack>
        </Stack>
      </Paper>

      {isLoading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', p: 6 }}>
          <CircularProgress />
        </Box>
      ) : isError ? (
        <ErrorState error={error} onRetry={() => void refetch()} />
      ) : groups.length === 0 ? (
        <EmptyState message={t('accommodationCosts.empty')} />
      ) : (
        <Stack spacing={2}>
          {groups.map((group) => (
            <Paper key={group.id} variant="outlined" sx={{ overflow: 'hidden' }}>
              <Stack
                direction="row"
                spacing={2}
                sx={(theme) => ({
                  px: 2,
                  py: 1.25,
                  alignItems: 'center',
                  justifyContent: 'space-between',
                  bgcolor: alpha(theme.palette.text.primary, 0.03),
                  borderBottom: 1,
                  borderColor: 'divider',
                })}
              >
                <Stack direction="row" spacing={1.25} sx={{ alignItems: 'center', minWidth: 0 }}>
                  <HomeWorkOutlined fontSize="small" color="primary" />
                  <Link
                    component={RouterLink}
                    to={paths.accommodationDetail(group.id)}
                    underline="hover"
                    color="text.primary"
                    sx={{ fontWeight: 700 }}
                    noWrap
                  >
                    {group.address}
                  </Link>
                </Stack>
                {group.monthly > 0 && (
                  <Chip
                    size="small"
                    variant="outlined"
                    label={t('accommodationCosts.perMonth', { amount: money(group.monthly) })}
                    sx={numeric}
                  />
                )}
              </Stack>

              <Stack divider={<Box sx={{ borderTop: 1, borderColor: 'divider' }} />}>
                {group.charges.map((charge) => {
                  const closed = !!charge.endDate && charge.endDate < today;
                  const total = charge.endDate ? daysBetween(charge.startDate, charge.endDate) + 1 : null;
                  const elapsed = Math.max(0, daysBetween(charge.startDate, today) + 1);
                  const pct = total ? Math.min(100, Math.round((elapsed / total) * 100)) : null;

                  return (
                    <Box
                      key={charge.id}
                      role="button"
                      tabIndex={0}
                      aria-label={t('accommodationCosts.openDetails')}
                      onClick={() => setOpened(charge)}
                      onKeyDown={(event) => {
                        if (event.key === 'Enter' || event.key === ' ') {
                          event.preventDefault();
                          setOpened(charge);
                        }
                      }}
                      sx={{
                        px: 2,
                        py: 1.5,
                        cursor: 'pointer',
                        opacity: closed ? 0.62 : 1,
                        transition: 'background-color 120ms',
                        '&:hover, &:focus-visible': { bgcolor: 'action.hover', outline: 'none' },
                        '&:active': { bgcolor: 'action.selected' },
                      }}
                    >
                      <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center' }}>
                        <Box sx={{ color: 'text.secondary', display: 'flex' }}>{KIND_ICON[charge.kind]}</Box>

                        <Box sx={{ flex: 1, minWidth: 0 }}>
                          <Stack direction="row" spacing={1} useFlexGap sx={{ alignItems: 'center', flexWrap: 'wrap' }}>
                            <Typography variant="body2" sx={{ fontWeight: 600 }}>
                              {enumLabel('accommodationChargeKind', charge.kind)}
                            </Typography>
                            {charge.provider && (
                              <Typography variant="body2" color="text.secondary" noWrap>
                                · {charge.provider}
                              </Typography>
                            )}
                            {charge.kind !== 'OneOff' && (
                              <Chip
                                size="small"
                                variant="outlined"
                                color={closed ? 'default' : 'success'}
                                label={closed ? t('rates.ended') : t('rates.active')}
                              />
                            )}
                          </Stack>
                          <Typography variant="caption" color="text.secondary">
                            {charge.kind === 'OneOff'
                              ? formatDate(charge.startDate)
                              : `${formatDate(charge.startDate)} – ${charge.endDate ? formatDate(charge.endDate) : t('rates.open')}`}
                          </Typography>
                          {pct !== null && charge.kind !== 'OneOff' && !closed && charge.startDate <= today && (
                            <LinearProgress
                              variant="determinate"
                              value={pct}
                              sx={{ mt: 0.75, height: 4, borderRadius: 2, maxWidth: 260 }}
                            />
                          )}
                        </Box>

                        <Box sx={{ textAlign: 'right', flexShrink: 0 }}>
                          <Typography variant="body1" sx={{ fontWeight: 700, whiteSpace: 'nowrap', ...numeric }}>
                            {money(charge.amount)}
                          </Typography>
                          <Typography variant="caption" color="text.secondary">
                            {t(KIND_UNIT[charge.kind])}
                          </Typography>
                        </Box>

                        <ChevronRightOutlined fontSize="small" sx={{ color: 'text.disabled' }} />
                      </Stack>
                    </Box>
                  );
                })}
              </Stack>
            </Paper>
          ))}
        </Stack>
      )}

      <ChargeDetailDialog
        rate={opened}
        onClose={() => setOpened(null)}
        onEdit={(rate) => {
          setOpened(null);
          setEditing(rate);
        }}
        onDelete={(rate) => {
          setOpened(null);
          remove.request(rate);
        }}
      />

      <ChargeDialog open={adding} onClose={() => setAdding(false)} />
      <ChargeDialog
        open={!!editing}
        accommodationId={editing?.accommodationId}
        editingRate={editing}
        onClose={() => setEditing(null)}
      />

      <ConfirmDialog
        open={!!remove.pending}
        title={t('accommodations.deleteRateTitle')}
        description={t('accommodations.deleteRateBody')}
        confirmLabel={t('common.delete')}
        destructive
        loading={remove.isDeleting}
        onConfirm={remove.confirm}
        onCancel={remove.cancel}
      />
    </Box>
  );
}
