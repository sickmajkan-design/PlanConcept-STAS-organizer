import {
  ChevronLeftOutlined,
  ChevronRightOutlined,
  FileDownloadOutlined,
  LockOutlined,
  LockOpenOutlined,
} from '@mui/icons-material';
import {
  Alert,
  Box,
  Button,
  Checkbox,
  Chip,
  FormControlLabel,
  IconButton,
  MenuItem,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useEffect, useState } from 'react';
import { Link as RouterLink } from 'react-router-dom';

import { toApiError } from '../../api/apiError';
import { bookApi, type SettlementSite } from '../../api/book';
import { exportsApi } from '../../api/exports';
import { ledgersApi } from '../../api/ledgers';
import type { LedgerCheck } from '../../api/types';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { PageHeader } from '../../components/PageHeader';
import { ReasonDialog } from '../../components/ReasonDialog';
import { useBranchFilter } from '../../features/branches/BranchContext';
import { HowItWorks, HowLine } from '../../features/book/HowItWorks';
import type { MessageKey } from '../../i18n/en';
import { useI18n } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';
import { formatDate, formatMoney } from '../../utils/formatting';

type BillingMode = 'Hourly' | 'FlatRate' | 'Measured';

const modes: BillingMode[] = ['Hourly', 'FlatRate', 'Measured'];

function describe(
  t: ReturnType<typeof useI18n>['t'],
  check: LedgerCheck,
): string {
  return t(`ledgers.check.${check.kind}` as MessageKey, {
    row: check.rowLabel ?? '',
    section: check.sectionName,
    column: check.columnName ?? '',
    hours: check.amount ?? 0,
    appHours: check.referenceAmount ?? 0,
    sections: [check.sectionName, ...check.otherSections].join(', '),
    unit: check.otherBranchName ? ` — ${check.otherBranchName}` : '',
  });
}

function storageKey(ledgerId: string): string {
  return `book.settlement.confirmed.${ledgerId}`;
}

function readConfirmed(ledgerId: string | null): string[] {
  if (!ledgerId) return [];
  try {
    const raw = window.localStorage.getItem(storageKey(ledgerId));
    return raw ? (JSON.parse(raw) as string[]) : [];
  } catch {
    return [];
  }
}

function writeConfirmed(ledgerId: string, value: string[]) {
  try {
    window.localStorage.setItem(storageKey(ledgerId), JSON.stringify(value));
  } catch {
    // Remembering is a convenience.
  }
}

/**
 * The month site by site, as the mockup lays it out: how each site is billed, hours, billing, what the workers
 * earned, margin, costs and result, down to what the firm earned after its fixed costs; then what to look at
 * before the month is closed, and closing it. Every figure is the settlement's own; nothing is recomputed here.
 */
export function BookSettlementPage() {
  const { t, locale } = useI18n();
  const queryClient = useQueryClient();
  const { branchId } = useBranchFilter();

  const now = new Date();
  const [year, setYear] = useState(now.getFullYear());
  const [month, setMonth] = useState(now.getMonth() + 1);
  const [confirmingLock, setConfirmingLock] = useState(false);
  const [reopening, setReopening] = useState(false);
  const [confirmed, setConfirmed] = useState<string[]>([]);
  const [resolved, setResolved] = useState<string[]>([]);

  const summary = useQuery({
    queryKey: ['book', 'settlement', year, month, branchId ?? null] as const,
    queryFn: () => bookApi.settlement({ year, month, branchId }),
  });

  const data = summary.data;
  const ledgerId = data?.ledgerId ?? null;
  const locked = data?.lockedAt != null;

  useEffect(() => {
    setConfirmed(readConfirmed(ledgerId));
    setResolved(readConfirmed(ledgerId ? `${ledgerId}.checks` : null));
  }, [ledgerId]);

  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: ['book'] });
    void queryClient.invalidateQueries({ queryKey: ['ledgers'] });
  };

  const updateSite = useMutation({
    mutationFn: (variables: { site: SettlementSite; mode: BillingMode; rate: number | null }) =>
      bookApi.updateSiteBilling(variables.site.projectId!, {
        billingMode: variables.mode,
        clientHourlyRate: variables.rate,
      }),
    onSuccess: refresh,
  });

  const lock = useMutation({
    mutationFn: () => ledgersApi.lock(ledgerId!),
    onSuccess: () => {
      setConfirmingLock(false);
      refresh();
    },
  });

  const unlock = useMutation({
    mutationFn: (reason: string) => ledgersApi.unlock(ledgerId!, reason),
    onSuccess: () => {
      setReopening(false);
      refresh();
    },
  });

  const shift = (delta: number) => {
    const date = new Date(year, month - 1 + delta, 1);
    setYear(date.getFullYear());
    setMonth(date.getMonth() + 1);
  };

  const monthLabel = new Date(year, month - 1, 1).toLocaleDateString(locale === 'sr' ? 'sr-Latn' : 'en-GB', {
    month: 'long',
    year: 'numeric',
  });

  const confirmations = [
    { key: 'hours', label: t('book.settlement.confirm.hours') },
    { key: 'imports', label: t('book.settlement.confirm.imports') },
    { key: 'export', label: t('book.settlement.confirm.export') },
  ];

  const checks = data?.checks ?? [];
  const checkKey = (c: LedgerCheck, index: number) => `${c.kind}:${c.rowId ?? index}:${c.columnName ?? ''}`;
  const openChecks = checks.filter((c, index) => !resolved.includes(checkKey(c, index)));
  const allConfirmed = confirmations.every((c) => confirmed.includes(c.key));
  const canLock = data?.hasSettlement === true && !locked && allConfirmed && openChecks.length === 0;

  const toggleConfirmed = (key: string) => {
    if (!ledgerId) return;
    const next = confirmed.includes(key) ? confirmed.filter((k) => k !== key) : [...confirmed, key];
    setConfirmed(next);
    writeConfirmed(ledgerId, next);
  };

  const toggleResolved = (key: string) => {
    if (!ledgerId) return;
    const next = resolved.includes(key) ? resolved.filter((k) => k !== key) : [...resolved, key];
    setResolved(next);
    writeConfirmed(`${ledgerId}.checks`, next);
  };

  const money = (value: number) => formatMoney(value, locale);
  const maxResult = Math.max(1, ...(data?.sites ?? []).map((s) => Math.abs(s.result)));

  const error = summary.error ?? updateSite.error ?? lock.error ?? unlock.error;

  return (
    <Stack spacing={2}>
      <PageHeader
        title={t('book.settlement.title')}
        description={monthLabel}
        secondaryActions={
          <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
            <IconButton size="small" onClick={() => shift(-1)} aria-label={t('book.overview.previous')}>
              <ChevronLeftOutlined />
            </IconButton>
            <Chip
              size="small"
              variant="outlined"
              color={locked ? 'default' : 'warning'}
              icon={locked ? <LockOutlined /> : undefined}
              label={locked ? t('book.settlement.locked') : t('book.settlement.open')}
            />
            <IconButton size="small" onClick={() => shift(1)} aria-label={t('book.overview.next')}>
              <ChevronRightOutlined />
            </IconButton>
            {ledgerId && (
              <>
                <Button startIcon={<FileDownloadOutlined />} onClick={() => void exportsApi.ledger(ledgerId)}>
                  {t('book.settlement.export')}
                </Button>
                <Button component={RouterLink} to={`${paths.ledgers}/${ledgerId}`}>
                  {t('book.settlement.sheet')}
                </Button>
              </>
            )}
          </Stack>
        }
      />

      {error && <Alert severity="error">{toApiError(error).message}</Alert>}

      {locked && (
        <Alert severity="info" icon={<LockOutlined />}>
          {t('book.settlement.lockedBanner')}
        </Alert>
      )}

      <HowItWorks id="settlement">
        <HowLine>{t('book.settlement.how1')}</HowLine>
        <HowLine>{t('book.settlement.how2')}</HowLine>
        <HowLine>{t('book.settlement.how3')}</HowLine>
        <HowLine>{t('book.settlement.how4')}</HowLine>
        <HowLine>{t('book.settlement.how5')}</HowLine>
      </HowItWorks>

      {data && !data.hasSettlement && <Alert severity="info">{t('book.settlement.none')}</Alert>}

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
          {t('book.settlement.bySite')}
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
          {t('book.settlement.bySiteHelp')}
        </Typography>
        <Box sx={{ overflowX: 'auto' }}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>{t('book.col.project')}</TableCell>
                <TableCell>{t('book.settlement.col.mode')}</TableCell>
                <TableCell align="right">{t('book.settlement.col.hours')}</TableCell>
                <TableCell align="right">{t('book.settlement.col.rate')}</TableCell>
                <TableCell align="right">{t('book.settlement.col.billing')}</TableCell>
                <TableCell align="right">{t('book.settlement.col.wage')}</TableCell>
                <TableCell align="right">{t('book.settlement.col.margin')}</TableCell>
                <TableCell align="right">{t('book.settlement.col.costs')}</TableCell>
                <TableCell align="right">{t('book.settlement.col.result')}</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {(data?.sites ?? []).map((site) => {
                const mode = (site.billingMode ?? 'Hourly') as BillingMode;
                const byInvoice = mode !== 'Hourly';

                return (
                  <TableRow key={site.sectionId} hover>
                    <TableCell>{site.name}</TableCell>
                    <TableCell>
                      {site.projectId ? (
                        <TextField
                          select
                          size="small"
                          value={mode}
                          disabled={locked || updateSite.isPending}
                          onChange={(event) =>
                            updateSite.mutate({
                              site,
                              mode: event.target.value as BillingMode,
                              rate: site.clientRate,
                            })
                          }
                          sx={{ minWidth: 130 }}
                        >
                          {modes.map((option) => (
                            <MenuItem key={option} value={option}>
                              {t(`book.settlement.mode.${option}` as MessageKey)}
                            </MenuItem>
                          ))}
                        </TextField>
                      ) : (
                        '—'
                      )}
                    </TableCell>
                    <TableCell align="right">{site.hours.toLocaleString(locale)}</TableCell>
                    <TableCell align="right">
                      {byInvoice ? (
                        <Typography variant="body2">{money(site.invoiced)}</Typography>
                      ) : site.projectId ? (
                        <RateField
                          value={site.clientRate}
                          disabled={locked}
                          onCommit={(rate) => updateSite.mutate({ site, mode, rate })}
                        />
                      ) : (
                        '—'
                      )}
                    </TableCell>
                    <TableCell align="right">{money(site.billing)}</TableCell>
                    <TableCell align="right">{money(site.wage)}</TableCell>
                    <TableCell align="right">{money(site.margin)}</TableCell>
                    <TableCell align="right">{money(site.costs)}</TableCell>
                    <TableCell align="right" sx={{ fontWeight: 600, color: site.result < 0 ? 'error.main' : undefined }}>
                      {money(site.result)}
                    </TableCell>
                  </TableRow>
                );
              })}
              {data && data.hasSettlement && (
                <TableRow sx={{ '& td': { fontWeight: 700 } }}>
                  <TableCell>{t('book.settlement.total')}</TableCell>
                  <TableCell />
                  <TableCell align="right">{data.totalHours.toLocaleString(locale)}</TableCell>
                  <TableCell />
                  <TableCell align="right">{money(data.totalBilling)}</TableCell>
                  <TableCell align="right">{money(data.totalWage)}</TableCell>
                  <TableCell align="right">{money(data.totalMargin)}</TableCell>
                  <TableCell align="right">{money(data.totalCosts)}</TableCell>
                  <TableCell align="right">{money(data.totalResult)}</TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </Box>

        <Stack spacing={0.5} sx={{ mt: 1.5 }}>
          {(data?.sites ?? [])
            .filter((s) => s.byHours != null && s.billingMode !== 'Hourly')
            .map((s) => (
              <Typography key={s.sectionId} variant="body2" color="text.secondary">
                {t('book.settlement.compare', {
                  name: s.name,
                  byHours: money(s.byHours ?? 0),
                  billed: money(s.billing),
                })}
              </Typography>
            ))}
        </Stack>
      </Paper>

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1 }}>
          {t('book.settlement.toFirm')}
        </Typography>
        <Stack spacing={0.75}>
          <Line label={t('book.settlement.allSites')} value={money(data?.totalResult ?? 0)} bold />
          {(data?.fixedLines ?? []).map((line, index) => (
            <Line key={index} label={line.name} value={`− ${money(line.amount)}`} />
          ))}
          <Line label={t('book.settlement.firmEarned')} value={money(data?.firmResult ?? 0)} bold />
        </Stack>
        <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 1 }}>
          {t('book.settlement.toFirmNote')}
        </Typography>
      </Paper>

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
          {t('book.settlement.checks')} {openChecks.length > 0 && `(${openChecks.length})`}
        </Typography>
        <Stack spacing={0.5} sx={{ mt: 1 }}>
          {checks.map((check, index) => {
            const key = checkKey(check, index);
            const done = resolved.includes(key);

            return (
              <FormControlLabel
                key={key}
                control={
                  <Checkbox checked={done} disabled={locked} onChange={() => toggleResolved(key)} size="small" />
                }
                label={
                  <Typography variant="body2" sx={{ textDecoration: done ? 'line-through' : undefined }}>
                    {describe(t, check)}
                  </Typography>
                }
              />
            );
          })}
          {(data?.costsWithoutDocumentCount ?? 0) > 0 && (
            <Alert
              severity="warning"
              action={
                <Button color="inherit" size="small" component={RouterLink} to={paths.bookEntries}>
                  {t('book.settlement.noDocs.open')}
                </Button>
              }
            >
              {t('book.settlement.noDocs', {
                count: data?.costsWithoutDocumentCount ?? 0,
                amount: money(data?.costsWithoutDocumentAmount ?? 0),
              })}
            </Alert>
          )}
          {checks.length === 0 && data?.hasSettlement && (
            <Typography variant="body2" color="text.secondary">
              {t('ledgers.checksNone')}
            </Typography>
          )}
        </Stack>
        <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 1 }}>
          {t('book.settlement.checksNote')} {t('book.settlement.noDocs.note')}
        </Typography>
      </Paper>

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1 }}>
          {t('book.settlement.close')}
        </Typography>
        <Stack>
          {confirmations.map((item) => (
            <FormControlLabel
              key={item.key}
              control={
                <Checkbox
                  checked={confirmed.includes(item.key)}
                  disabled={locked || !data?.hasSettlement}
                  onChange={() => toggleConfirmed(item.key)}
                  size="small"
                />
              }
              label={item.label}
            />
          ))}
          <FormControlLabel
            control={<Checkbox checked={openChecks.length === 0 && !!data?.hasSettlement} disabled size="small" />}
            label={t('book.settlement.confirm.checks')}
          />
        </Stack>
        <Stack direction="row" spacing={2} sx={{ mt: 1, alignItems: 'center' }}>
          {locked ? (
            <Button startIcon={<LockOpenOutlined />} onClick={() => setReopening(true)}>
              {t('book.settlement.unlock')}
            </Button>
          ) : (
            <Button
              variant="contained"
              startIcon={<LockOutlined />}
              disabled={!canLock}
              onClick={() => setConfirmingLock(true)}
            >
              {t('book.settlement.lock')}
            </Button>
          )}
          <Typography variant="body2" color="text.secondary">
            {locked
              ? t('book.settlement.lockedOn', { date: formatDate(data!.lockedAt!) })
              : canLock
                ? t('book.settlement.readyToLock')
                : t('book.settlement.notReady')}
          </Typography>
        </Stack>
        <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 1 }}>
          {t('book.settlement.closeNote')}
        </Typography>
      </Paper>

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1 }}>
          {t('book.settlement.resultBySite')}
        </Typography>
        <Stack spacing={1}>
          {(data?.sites ?? []).map((site) => (
            <Box key={site.sectionId}>
              <Stack direction="row" sx={{ justifyContent: 'space-between' }}>
                <Typography variant="body2">{site.name}</Typography>
                <Typography variant="body2" sx={{ fontWeight: 600 }}>
                  {money(site.result)}
                </Typography>
              </Stack>
              <Box sx={{ height: 8, bgcolor: 'action.hover', borderRadius: 1, mt: 0.5 }}>
                <Box
                  sx={{
                    height: '100%',
                    width: `${(Math.abs(site.result) / maxResult) * 100}%`,
                    bgcolor: site.result < 0 ? 'error.main' : 'success.main',
                    borderRadius: 1,
                  }}
                />
              </Box>
            </Box>
          ))}
        </Stack>
      </Paper>

      <ConfirmDialog
        open={confirmingLock}
        title={t('ledgers.lockTitle')}
        description={t('ledgers.lockBody')}
        confirmLabel={t('ledgers.lock')}
        loading={lock.isPending}
        onConfirm={async () => {
          await lock.mutateAsync();
        }}
        onCancel={() => setConfirmingLock(false)}
      />

      <ReasonDialog
        open={reopening}
        title={t('ledgers.reopenTitle')}
        hint={t('ledgers.reopenHint')}
        label={t('ledgers.reopenReason')}
        submitLabel={t('ledgers.reopen')}
        onSubmit={async (reason) => {
          await unlock.mutateAsync(reason);
        }}
        onClose={() => setReopening(false)}
      />
    </Stack>
  );
}

function Line({ label, value, bold }: { label: string; value: string; bold?: boolean }) {
  return (
    <Stack direction="row" sx={{ justifyContent: 'space-between' }}>
      <Typography variant="body2" sx={{ fontWeight: bold ? 700 : 400 }}>
        {label}
      </Typography>
      <Typography variant="body2" sx={{ fontWeight: bold ? 700 : 400 }}>
        {value}
      </Typography>
    </Stack>
  );
}

/** The client's price per hour of a site: typed, and saved when the field is left. */
function RateField({
  value,
  disabled,
  onCommit,
}: {
  value: number | null;
  disabled: boolean;
  onCommit: (rate: number | null) => void;
}) {
  const [text, setText] = useState(value === null ? '' : String(value));

  useEffect(() => {
    setText(value === null ? '' : String(value));
  }, [value]);

  return (
    <TextField
      size="small"
      value={text}
      disabled={disabled}
      onChange={(event) => setText(event.target.value)}
      onBlur={() => {
        const parsed = text.trim() === '' ? null : Number(text.replace(',', '.'));

        if (parsed !== null && !Number.isFinite(parsed)) {
          setText(value === null ? '' : String(value));
          return;
        }

        if (parsed !== value) onCommit(parsed);
      }}
      slotProps={{ htmlInput: { inputMode: 'decimal', style: { textAlign: 'right' } } }}
      sx={{ maxWidth: 110 }}
    />
  );
}
