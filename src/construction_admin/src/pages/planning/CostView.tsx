import { Alert, Box, CircularProgress, MenuItem, Paper, Stack, TextField, Typography } from '@mui/material';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { useState } from 'react';

import { planningApi } from '../../api/planning';
import { ErrorState } from '../../components/ErrorState';
import { useBranchFilter } from '../../features/branches/BranchContext';
import { useI18n } from '../../i18n/useI18n';
import { formatMoney } from '../../utils/formatting';

const HOURS = [6, 7, 8, 9, 10];

/** "2026-10" as "10/2026". */
const monthLabel = (month: string) => `${month.slice(5)}/${month.slice(0, 4)}`;

/**
 * What the schedule will cost in labour, per site and month, beside what the approved hours have cost so
 * far. An estimate: days posted on a site that works them, minus leave and holidays, at the rate in force
 * with an assumed working day. Only shown to whoever may see what people are paid.
 */
export function CostView({ range }: { range: { from: string; to: string } }) {
  const { t, locale } = useI18n();
  const { branchId } = useBranchFilter();
  const [hours, setHours] = useState(8);

  const query = useQuery({
    queryKey: ['planning', 'labourCost', branchId ?? null, range.from, range.to, hours],
    queryFn: () => planningApi.labourCost({ ...range, hoursPerDay: hours, branchId }),
    placeholderData: keepPreviousData,
  });

  if (query.error && !query.data) return <ErrorState error={query.error} onRetry={() => void query.refetch()} />;

  if (!query.data) {
    return (
      <Box sx={{ py: 6, display: 'flex', justifyContent: 'center' }}>
        <CircularProgress />
      </Box>
    );
  }

  const data = query.data;
  const money = (value: number) => (value === 0 ? '–' : formatMoney(value, locale));
  const total = (pick: (p: (typeof data.projects)[number]) => number) => data.projects.reduce((sum, p) => sum + pick(p), 0);

  return (
    <Stack spacing={2}>
      <Stack direction="row" spacing={2} useFlexGap sx={{ flexWrap: 'wrap', alignItems: 'center' }}>
        <TextField select size="small" label={t('planning.cost.hours')} value={hours} onChange={(e) => setHours(Number(e.target.value))} sx={{ minWidth: 190 }}>
          {HOURS.map((h) => (
            <MenuItem key={h} value={h}>
              {t('planning.cost.hoursOption', { hours: h })}
            </MenuItem>
          ))}
        </TextField>
        <Typography variant="body2" color="text.secondary" sx={{ maxWidth: '70ch' }}>
          {t('planning.cost.note')}
        </Typography>
      </Stack>

      {data.unpricedDays > 0 && (
        <Alert severity="warning">{t('planning.cost.unpriced', { count: data.unpricedDays })}</Alert>
      )}

      {data.projects.length === 0 ? (
        <Typography color="text.secondary">{t('planning.cost.empty')}</Typography>
      ) : (
        <Paper variant="outlined" sx={{ borderRadius: 2.5, overflowX: 'auto' }}>
          <Box
            component="table"
            sx={{
              borderCollapse: 'collapse',
              width: '100%',
              minWidth: 260 + data.months.length * 210,
              fontVariantNumeric: 'tabular-nums',
              '& td, & th': { p: 1.25, borderBottom: 1, borderColor: 'divider', textAlign: 'right' },
              '& th': { fontWeight: 500, fontSize: 12, color: 'text.secondary' },
              '& td:first-of-type, & th:first-of-type': { textAlign: 'left', position: 'sticky', left: 0, bgcolor: 'background.paper', minWidth: 200 },
              '& tr:last-of-type td': { borderBottom: 0 },
            }}
          >
            <thead>
              <tr>
                <th rowSpan={2}>{t('planning.n.project')}</th>
                {data.months.map((m) => (
                  <th key={m} colSpan={2} style={{ textAlign: 'center' }}>
                    {monthLabel(m)}
                  </th>
                ))}
                <th colSpan={2} style={{ textAlign: 'center' }}>
                  {t('planning.cost.total')}
                </th>
              </tr>
              <tr>
                {[...data.months, 'total'].flatMap((m) => [
                  <th key={`${m}-p`}>{t('planning.cost.planned')}</th>,
                  <th key={`${m}-a`}>{t('planning.cost.actual')}</th>,
                ])}
              </tr>
            </thead>
            <tbody>
              {data.projects.map((p) => (
                <tr key={p.projectId}>
                  <td>
                    <Typography variant="body2" sx={{ fontWeight: 600 }}>
                      {p.name}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      {t('planning.cost.days', { count: p.plannedDays })}
                    </Typography>
                  </td>
                  {p.months.flatMap((m) => [
                    <td key={`${m.month}-p`}>{money(m.plannedCost)}</td>,
                    <td key={`${m.month}-a`}>{money(m.actualCost)}</td>,
                  ])}
                  <td style={{ fontWeight: 700 }}>{money(p.plannedCost)}</td>
                  <td style={{ fontWeight: 700 }}>{money(p.actualCost)}</td>
                </tr>
              ))}
              <Box component="tr" sx={{ '& td': { bgcolor: 'action.hover', fontWeight: 700 } }}>
                <td>{t('planning.cost.total')}</td>
                {data.months.flatMap((m) => [
                  <td key={`${m}-p`}>{money(total((p) => p.months.find((x) => x.month === m)?.plannedCost ?? 0))}</td>,
                  <td key={`${m}-a`}>{money(total((p) => p.months.find((x) => x.month === m)?.actualCost ?? 0))}</td>,
                ])}
                <td>{money(total((p) => p.plannedCost))}</td>
                <td>{money(total((p) => p.actualCost))}</td>
              </Box>
            </tbody>
          </Box>
        </Paper>
      )}
    </Stack>
  );
}
