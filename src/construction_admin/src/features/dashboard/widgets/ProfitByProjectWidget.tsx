import { Box, Table, TableBody, TableCell, TableHead, TableRow, TableSortLabel, Typography } from '@mui/material';
import { useState } from 'react';
import { Link as RouterLink } from 'react-router-dom';

import type { FinanceProjectRow } from '../../../api/finance';
import { useI18n } from '../../../i18n/useI18n';
import { paths } from '../../../routes/paths';
import { formatMoney } from '../../../utils/formatting';
import { useFinancePeriod } from '../../finance/PeriodContext';
import { formatPeriod } from '../../finance/periods';
import { useFinanceByProject } from '../../finance/useFinanceSeries';
import type { DashboardWidgetProps } from '../widgetTypes';
import { WidgetShell } from './WidgetShell';

const TOP = 10;

type SortKey = 'projectName' | 'revenue' | 'expense' | 'profit' | 'marginPercent';

/** A missing margin sorts below every real one, whichever way the column is sorted. */
function compare(a: FinanceProjectRow, b: FinanceProjectRow, key: SortKey): number {
  if (key === 'projectName') return a.projectName.localeCompare(b.projectName);
  const left = a[key];
  const right = b[key];
  if (left === null && right === null) return 0;
  if (left === null) return -1;
  if (right === null) return 1;
  return left - right;
}

/** Which projects earn and which lose money in the board's period, sortable by any column. */
export function ProfitByProjectWidget({ onRemove, onExpandWidth }: DashboardWidgetProps) {
  const { t, locale } = useI18n();
  const { period } = useFinancePeriod();
  const { data, isLoading, error } = useFinanceByProject(TOP);
  const [sort, setSort] = useState<{ key: SortKey; direction: 'asc' | 'desc' }>({ key: 'profit', direction: 'asc' });

  const rows = [...(data?.rows ?? [])].sort((a, b) => {
    const order = compare(a, b, sort.key);
    return sort.direction === 'asc' ? order : -order;
  });

  const header = (key: SortKey, label: string, numeric = true) => (
    <TableCell align={numeric ? 'right' : 'left'} sortDirection={sort.key === key ? sort.direction : false}>
      <TableSortLabel
        active={sort.key === key}
        direction={sort.key === key ? sort.direction : 'asc'}
        onClick={() =>
          setSort((current) => ({
            key,
            direction: current.key === key && current.direction === 'asc' ? 'desc' : 'asc',
          }))
        }
      >
        {label}
      </TableSortLabel>
    </TableCell>
  );

  return (
    <WidgetShell
      title={`${t('dashboard.widget.ProfitByProject')} — ${formatPeriod(period.from, period.to)}`}
      isLoading={isLoading}
      error={error}
      onRemove={onRemove}
      onExpandWidth={onExpandWidth}
    >
      {rows.length === 0 ? (
        <Typography color="text.secondary" variant="body2">
          {t('finance.noData')}
        </Typography>
      ) : (
        <Box sx={{ flex: 1, minHeight: 0, overflow: 'auto' }}>
          <Table size="small" stickyHeader>
            <TableHead>
              <TableRow>
                {header('projectName', t('finance.project'), false)}
                {header('revenue', t('finance.revenue'))}
                {header('expense', t('finance.expense'))}
                {header('profit', t('finance.profit'))}
                {header('marginPercent', t('finance.margin'))}
              </TableRow>
            </TableHead>
            <TableBody>
              {rows.map((row) => (
                <TableRow key={row.projectId} hover>
                  <TableCell>
                    <Box
                      component={RouterLink}
                      to={paths.projectDetail(row.projectId)}
                      sx={{ color: 'inherit', textDecoration: 'none', fontWeight: 600 }}
                    >
                      {row.projectName}
                    </Box>
                  </TableCell>
                  <TableCell align="right">{formatMoney(row.revenue, locale)}</TableCell>
                  <TableCell align="right">{formatMoney(row.expense, locale)}</TableCell>
                  <TableCell align="right" sx={{ color: row.profit < 0 ? 'error.main' : 'inherit', fontWeight: 700 }}>
                    {formatMoney(row.profit, locale)}
                  </TableCell>
                  <TableCell align="right">
                    {row.marginPercent === null ? '—' : `${row.marginPercent.toFixed(1)}%`}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Box>
      )}
    </WidgetShell>
  );
}
