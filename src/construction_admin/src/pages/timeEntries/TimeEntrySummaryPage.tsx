import { KeyboardArrowDownOutlined, KeyboardArrowRightOutlined } from '@mui/icons-material';
import {
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Collapse,
  FormControlLabel,
  IconButton,
  Paper,
  Stack,
  Switch,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TableSortLabel,
  TextField,
  Typography,
} from '@mui/material';
import { useMemo, useState } from 'react';

import { exportsApi } from '../../api/exports';
import type { TimeEntrySummaryRow } from '../../api/types';
import { ErrorState } from '../../components/ErrorState';
import { ExportButton } from '../../components/ExportButton';
import { PageHeader } from '../../components/PageHeader';
import { useTimeEntrySummaryQuery } from '../../features/timeEntries/useTimeEntries';
import { useT } from '../../i18n/useI18n';
import { splitMinutes } from '../../utils/formatting';

type SortField = 'employeeName' | 'entryCount' | 'totalMinutes' | 'approvedMinutes' | 'pendingCount';
type SortDirection = 'asc' | 'desc';

/** One employee's rows collapsed into a single group, with per-project detail underneath. */
interface EmployeeGroup {
  employeeId: string;
  employeeName: string;
  projects: TimeEntrySummaryRow[];
  entryCount: number;
  totalMinutes: number;
  approvedMinutes: number;
  pendingCount: number;
}

/** `YYYY-MM-DD` for a date input, in local time rather than UTC. */
function toDateInput(date: Date): string {
  const pad = (value: number) => String(value).padStart(2, '0');

  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}

function startOfMonth(): string {
  const now = new Date();
  return toDateInput(new Date(now.getFullYear(), now.getMonth(), 1));
}

/** The instant just after the given local day ends. */
function endOfDay(date: string): string {
  const next = new Date(`${date}T00:00`);
  next.setDate(next.getDate() + 1);

  return next.toISOString();
}

export function TimeEntrySummaryPage() {
  const t = useT();

  const [from, setFrom] = useState(startOfMonth);
  const [to, setTo] = useState(() => toDateInput(new Date()));
  const [approvedOnly, setApprovedOnly] = useState(false);

  const query = useMemo(
    () => ({
      from: new Date(`${from}T00:00`).toISOString(),
      // The end date is inclusive to the person reading it: picking the 31st
      // has to include the 31st. The API's window is half-open, so send the
      // start of the following day — sending midnight of the 31st would drop
      // that whole day's hours without anything on screen saying so.
      to: endOfDay(to),
      approvedOnly: approvedOnly || undefined,
    }),
    [from, to, approvedOnly],
  );

  const valid = !!from && !!to && from <= to;
  const { data, isLoading, isError, error, refetch } = useTimeEntrySummaryQuery(
    query,
    valid,
  );

  const hours = (minutes: number) => t('timeEntries.hoursShort', splitMinutes(minutes));

  const [sortBy, setSortBy] = useState<SortField>('employeeName');
  const [sortDirection, setSortDirection] = useState<SortDirection>('asc');

  const toggleSort = (field: SortField) => {
    if (sortBy === field) {
      setSortDirection((prev) => (prev === 'asc' ? 'desc' : 'asc'));
    } else {
      setSortBy(field);
      setSortDirection('asc');
    }
  };

  // One row per employee AND project comes back from the API — grouped here
  // so a crew member split across two sites shows as one line with a
  // breakdown underneath, instead of their name repeated for every site.
  const groups = useMemo<EmployeeGroup[]>(() => {
    if (!data) return [];

    const byEmployee = new Map<string, EmployeeGroup>();

    for (const row of data.rows) {
      const existing = byEmployee.get(row.employeeId);
      if (existing) {
        existing.projects.push(row);
        existing.entryCount += row.entryCount;
        existing.totalMinutes += row.totalMinutes;
        existing.approvedMinutes += row.approvedMinutes;
        existing.pendingCount += row.pendingCount;
      } else {
        byEmployee.set(row.employeeId, {
          employeeId: row.employeeId,
          employeeName: row.employeeName,
          projects: [row],
          entryCount: row.entryCount,
          totalMinutes: row.totalMinutes,
          approvedMinutes: row.approvedMinutes,
          pendingCount: row.pendingCount,
        });
      }
    }

    return Array.from(byEmployee.values());
  }, [data]);

  const sortedGroups = useMemo(() => {
    const factor = sortDirection === 'asc' ? 1 : -1;

    const compare = (a: EmployeeGroup, b: EmployeeGroup): number => {
      switch (sortBy) {
        case 'employeeName':
          return a.employeeName.localeCompare(b.employeeName) * factor;
        case 'entryCount':
          return (a.entryCount - b.entryCount) * factor;
        case 'totalMinutes':
          return (a.totalMinutes - b.totalMinutes) * factor;
        case 'approvedMinutes':
          return (a.approvedMinutes - b.approvedMinutes) * factor;
        case 'pendingCount':
          return (a.pendingCount - b.pendingCount) * factor;
        default:
          return 0;
      }
    };

    return [...groups].sort(compare);
  }, [groups, sortBy, sortDirection]);

  const [expandedIds, setExpandedIds] = useState<Set<string>>(new Set());
  const toggleExpanded = (employeeId: string) => {
    setExpandedIds((prev) => {
      const next = new Set(prev);
      if (next.has(employeeId)) {
        next.delete(employeeId);
      } else {
        next.add(employeeId);
      }
      return next;
    });
  };
  const allExpanded = sortedGroups.length > 0 && sortedGroups.every((g) => expandedIds.has(g.employeeId));
  const toggleAllExpanded = () => {
    setExpandedIds(allExpanded ? new Set() : new Set(sortedGroups.map((g) => g.employeeId)));
  };

  return (
    <Box>
      <PageHeader title={t('timeEntries.summary')} />

      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        spacing={2}
        sx={{ mb: 2, alignItems: { sm: 'center' } }}
      >
        <TextField
          label={t('timeEntries.from')}
          type="date"
          size="small"
          value={from}
          onChange={(event) => setFrom(event.target.value)}
          slotProps={{ inputLabel: { shrink: true } }}
        />
        <TextField
          label={t('timeEntries.to')}
          type="date"
          size="small"
          value={to}
          onChange={(event) => setTo(event.target.value)}
          slotProps={{ inputLabel: { shrink: true } }}
          error={!valid}
        />
        <FormControlLabel
          control={
            <Switch
              checked={approvedOnly}
              onChange={(event) => setApprovedOnly(event.target.checked)}
            />
          }
          label={t('timeEntries.summaryApproved')}
        />

        {/* The row-by-row hours rather than this summary: payroll is run from
            the shifts, and a per-employee total is what the screen is for. */}
        <ExportButton
          disabled={!valid}
          onExport={(language) =>
            exportsApi.timeEntries({ from, to, approvedOnly, language })
          }
        />
      </Stack>

      {isError && <ErrorState error={error} onRetry={() => void refetch()} />}

      {data && (
        <>
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ mb: 2 }}>
            <SummaryTile
              label={t('timeEntries.summaryTotal')}
              value={hours(data.totalMinutes)}
            />
            <SummaryTile
              label={t('timeEntries.summaryApproved')}
              value={hours(data.approvedMinutes)}
            />
            <SummaryTile
              label={t('timeEntries.summaryPending')}
              value={String(data.pendingCount)}
            />
          </Stack>

          <Stack direction="row" sx={{ justifyContent: 'flex-end', mb: 1 }}>
            <Button size="small" onClick={toggleAllExpanded} disabled={sortedGroups.length === 0}>
              {allExpanded ? t('timeEntries.collapseAll') : t('timeEntries.expandAll')}
            </Button>
          </Stack>

          <Paper>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell width={40} />
                  <TableCell sortDirection={sortBy === 'employeeName' ? sortDirection : false}>
                    <TableSortLabel
                      active={sortBy === 'employeeName'}
                      direction={sortBy === 'employeeName' ? sortDirection : 'asc'}
                      onClick={() => toggleSort('employeeName')}
                    >
                      {t('timeEntries.employee')}
                    </TableSortLabel>
                  </TableCell>
                  <TableCell align="right" sortDirection={sortBy === 'entryCount' ? sortDirection : false}>
                    <TableSortLabel
                      active={sortBy === 'entryCount'}
                      direction={sortBy === 'entryCount' ? sortDirection : 'asc'}
                      onClick={() => toggleSort('entryCount')}
                    >
                      {t('timeEntries.summaryEntries')}
                    </TableSortLabel>
                  </TableCell>
                  <TableCell align="right" sortDirection={sortBy === 'totalMinutes' ? sortDirection : false}>
                    <TableSortLabel
                      active={sortBy === 'totalMinutes'}
                      direction={sortBy === 'totalMinutes' ? sortDirection : 'asc'}
                      onClick={() => toggleSort('totalMinutes')}
                    >
                      {t('timeEntries.summaryTotal')}
                    </TableSortLabel>
                  </TableCell>
                  <TableCell align="right" sortDirection={sortBy === 'approvedMinutes' ? sortDirection : false}>
                    <TableSortLabel
                      active={sortBy === 'approvedMinutes'}
                      direction={sortBy === 'approvedMinutes' ? sortDirection : 'asc'}
                      onClick={() => toggleSort('approvedMinutes')}
                    >
                      {t('timeEntries.summaryApproved')}
                    </TableSortLabel>
                  </TableCell>
                  <TableCell align="right" sortDirection={sortBy === 'pendingCount' ? sortDirection : false}>
                    <TableSortLabel
                      active={sortBy === 'pendingCount'}
                      direction={sortBy === 'pendingCount' ? sortDirection : 'asc'}
                      onClick={() => toggleSort('pendingCount')}
                    >
                      {t('timeEntries.summaryPending')}
                    </TableSortLabel>
                  </TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {sortedGroups.map((group) => (
                  <EmployeeGroupRow
                    key={group.employeeId}
                    group={group}
                    hours={hours}
                    expanded={expandedIds.has(group.employeeId)}
                    onToggle={() => toggleExpanded(group.employeeId)}
                  />
                ))}

                {sortedGroups.length === 0 && !isLoading && (
                  <TableRow>
                    <TableCell colSpan={6}>
                      <Typography variant="body2" color="text.secondary">
                        {t('timeEntries.summaryEmpty')}
                      </Typography>
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </Paper>
        </>
      )}
    </Box>
  );
}

/**
 * One employee's totals, expandable to the per-project rows behind them.
 * A crew member on a single site expands to nothing new, so the chevron is
 * hidden for single-project groups rather than opening an empty breakdown.
 */
function EmployeeGroupRow({
  group,
  hours,
  expanded,
  onToggle,
}: {
  group: EmployeeGroup;
  hours: (minutes: number) => string;
  expanded: boolean;
  onToggle: () => void;
}) {
  const t = useT();
  const hasBreakdown = group.projects.length > 1;

  return (
    <>
      <TableRow hover selected={expanded}>
        <TableCell>
          {hasBreakdown && (
            <IconButton size="small" onClick={onToggle}>
              {expanded ? (
                <KeyboardArrowDownOutlined fontSize="small" />
              ) : (
                <KeyboardArrowRightOutlined fontSize="small" />
              )}
            </IconButton>
          )}
        </TableCell>
        <TableCell>
          <Typography variant="body2">{group.employeeName}</Typography>
          {!hasBreakdown && (
            <Typography variant="caption" color="text.secondary">
              {group.projects[0]?.projectName ?? t('timeEntries.noProject')}
            </Typography>
          )}
        </TableCell>
        <TableCell align="right">{group.entryCount}</TableCell>
        <TableCell align="right">{hours(group.totalMinutes)}</TableCell>
        <TableCell align="right">{hours(group.approvedMinutes)}</TableCell>
        <TableCell align="right">
          {group.pendingCount > 0 ? (
            <Chip size="small" color="warning" label={group.pendingCount} />
          ) : (
            '—'
          )}
        </TableCell>
      </TableRow>
      {hasBreakdown && (
        <TableRow>
          <TableCell sx={{ py: 0, borderBottom: expanded ? undefined : 'none' }} colSpan={6}>
            <Collapse in={expanded} timeout="auto" unmountOnExit>
              <Table size="small" sx={{ bgcolor: 'action.hover' }}>
                <TableBody>
                  {group.projects.map((row) => (
                    <TableRow key={row.projectId ?? 'none'}>
                      <TableCell width={40} />
                      <TableCell>{row.projectName ?? t('timeEntries.noProject')}</TableCell>
                      <TableCell align="right">{row.entryCount}</TableCell>
                      <TableCell align="right">{hours(row.totalMinutes)}</TableCell>
                      <TableCell align="right">{hours(row.approvedMinutes)}</TableCell>
                      <TableCell align="right">
                        {row.pendingCount > 0 ? (
                          <Chip size="small" color="warning" variant="outlined" label={row.pendingCount} />
                        ) : (
                          '—'
                        )}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </Collapse>
          </TableCell>
        </TableRow>
      )}
    </>
  );
}

function SummaryTile({ label, value }: { label: string; value: string }) {
  return (
    <Card sx={{ flex: 1 }}>
      <CardContent>
        <Typography variant="body2" color="text.secondary">
          {label}
        </Typography>
        <Typography variant="h5" sx={{ fontWeight: 700 }}>
          {value}
        </Typography>
      </CardContent>
    </Card>
  );
}
