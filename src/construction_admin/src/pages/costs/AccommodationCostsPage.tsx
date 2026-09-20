import { AddOutlined, DeleteOutlined, HomeWorkOutlined } from '@mui/icons-material';
import {
  Box,
  Button,
  Chip,
  CircularProgress,
  FormControlLabel,
  IconButton,
  Link,
  Paper,
  Stack,
  Switch,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Tooltip,
  Typography,
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
import { formatDate, formatMoney } from '../../utils/formatting';
import { ChargeDialog, KIND_UNIT } from '../accommodations/AccommodationDetailPage';

// A firm houses a few dozen places at most: one board, no page numbers.
const FULL_LIST = { pageNumber: 1, pageSize: 100 };

/**
 * What housing costs the firm, across every accommodation: rent, per-person
 * daily rates and one-off charges. Entering and editing the places and who
 * lives in them happens in the housing register, not here.
 */
export function AccommodationCostsPage() {
  const t = useT();
  const { locale } = useI18n();
  const enumLabel = useEnumLabel();
  const [inForceOnly, setInForceOnly] = useState(true);
  const [adding, setAdding] = useState(false);
  const [editing, setEditing] = useState<AccommodationRate | null>(null);

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
  const rows = data?.items ?? [];

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

      <Stack
        direction="row"
        spacing={2}
        useFlexGap
        sx={{ flexWrap: 'wrap', mb: 2, alignItems: 'center' }}
      >
        <FormControlLabel
          control={<Switch checked={inForceOnly} onChange={(event) => setInForceOnly(event.target.checked)} />}
          label={t('accommodationCosts.inForceOnly')}
        />
        {summary && (
          <Chip
            variant="outlined"
            label={t('accommodationCosts.monthlyTotal', {
              amount: formatMoney(summary.totalMonthlyAmount, locale),
            })}
          />
        )}
        <Box sx={{ flex: 1 }} />
        <Button component={RouterLink} to={paths.accommodations} startIcon={<HomeWorkOutlined />}>
          {t('accommodationCosts.toRegister')}
        </Button>
      </Stack>

      {isLoading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', p: 6 }}>
          <CircularProgress />
        </Box>
      ) : isError ? (
        <ErrorState error={error} onRetry={() => void refetch()} />
      ) : rows.length === 0 ? (
        <EmptyState message={t('accommodationCosts.empty')} />
      ) : (
        <Paper variant="outlined">
          <TableContainer>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>{t('accommodationCosts.accommodation')}</TableCell>
                  <TableCell>{t('accommodations.chargeKind')}</TableCell>
                  <TableCell>{t('common.status')}</TableCell>
                  <TableCell>{t('accommodations.provider')}</TableCell>
                  <TableCell align="right">{t('accommodations.chargeAmount')}</TableCell>
                  <TableCell>{t('accommodations.startDate')}</TableCell>
                  <TableCell>{t('accommodations.endDate')}</TableCell>
                  <TableCell align="right" />
                </TableRow>
              </TableHead>
              <TableBody>
                {rows.map((row) => (
                  <TableRow key={row.id} hover onDoubleClick={() => setEditing(row)}>
                    <TableCell>
                      <Link component={RouterLink} to={paths.accommodationDetail(row.accommodationId)}>
                        {row.accommodationAddress}
                      </Link>
                    </TableCell>
                    <TableCell>{enumLabel('accommodationChargeKind', row.kind)}</TableCell>
                    <TableCell>
                      {row.kind === 'OneOff' ? (
                        <Chip size="small" variant="outlined" label={t('accommodations.once')} />
                      ) : row.endDate ? (
                        <Chip size="small" variant="outlined" label={t('rates.ended')} />
                      ) : (
                        <Chip size="small" color="success" variant="outlined" label={t('rates.active')} />
                      )}
                    </TableCell>
                    <TableCell>{row.provider || '—'}</TableCell>
                    <TableCell align="right" sx={{ fontWeight: 600, whiteSpace: 'nowrap' }}>
                      {formatMoney(row.amount, locale)}{' '}
                      <Typography component="span" variant="caption" color="text.secondary">
                        {t(KIND_UNIT[row.kind])}
                      </Typography>
                    </TableCell>
                    <TableCell sx={{ whiteSpace: 'nowrap' }}>{formatDate(row.startDate)}</TableCell>
                    <TableCell sx={{ whiteSpace: 'nowrap' }}>
                      {row.kind === 'OneOff' ? '—' : row.endDate ? formatDate(row.endDate) : t('rates.open')}
                    </TableCell>
                    <TableCell align="right">
                      <Tooltip title={t('common.delete')}>
                        <IconButton size="small" onClick={() => remove.request(row)}>
                          <DeleteOutlined fontSize="small" />
                        </IconButton>
                      </Tooltip>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </Paper>
      )}

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
