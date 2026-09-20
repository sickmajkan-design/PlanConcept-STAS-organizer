import {
  CalendarMonthOutlined,
  CloseOutlined,
  DeleteOutlined,
  EditOutlined,
  PersonOutlined,
  ReceiptLongOutlined,
} from '@mui/icons-material';
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  IconButton,
  LinearProgress,
  Link,
  Stack,
  Typography,
  alpha,
} from '@mui/material';
import type { ReactNode } from 'react';
import { Link as RouterLink } from 'react-router-dom';

import { toApiError } from '../../api/apiError';
import type { AccommodationChargeKind, AccommodationRate } from '../../api/types';
import { AttachmentList } from '../../components/AttachmentList';
import { useAccommodationChargeTrackingQuery } from '../../features/costs/useCosts';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useI18n, useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';
import { formatDate, formatMoney } from '../../utils/formatting';
import { KIND_UNIT } from '../accommodations/AccommodationDetailPage';

export const KIND_ICON: Record<AccommodationChargeKind, ReactNode> = {
  Monthly: <CalendarMonthOutlined fontSize="small" />,
  DailyPerPerson: <PersonOutlined fontSize="small" />,
  OneOff: <ReceiptLongOutlined fontSize="small" />,
};

const numeric = { fontVariantNumeric: 'tabular-nums' } as const;

function Tile({ label, value, hint, tone }: { label: string; value: string; hint?: string; tone?: 'warn' }) {
  return (
    <Box
      sx={(theme) => ({
        flex: '1 1 160px',
        minWidth: 0,
        p: 1.75,
        borderRadius: 2,
        border: 1,
        borderColor: tone === 'warn' ? alpha(theme.palette.warning.main, 0.5) : 'divider',
        bgcolor: tone === 'warn' ? alpha(theme.palette.warning.main, 0.06) : 'background.paper',
      })}
    >
      <Typography variant="caption" color="text.secondary" sx={{ letterSpacing: 0.2 }}>
        {label}
      </Typography>
      <Typography variant="h6" sx={{ fontWeight: 700, lineHeight: 1.25, ...numeric }}>
        {value}
      </Typography>
      {hint && (
        <Typography variant="caption" color="text.secondary">
          {hint}
        </Typography>
      )}
    </Box>
  );
}

function SectionTitle({ children }: { children: ReactNode }) {
  return (
    <Typography variant="overline" color="text.secondary" sx={{ fontWeight: 700, letterSpacing: 0.8 }}>
      {children}
    </Typography>
  );
}

function DetailRow({ label, value }: { label: string; value: ReactNode }) {
  return (
    <Stack direction="row" spacing={2} sx={{ py: 0.75, justifyContent: 'space-between' }}>
      <Typography variant="body2" color="text.secondary">
        {label}
      </Typography>
      <Typography variant="body2" sx={{ textAlign: 'right', fontWeight: 500 }}>
        {value}
      </Typography>
    </Stack>
  );
}

/**
 * Everything about one housing charge in one place: what it is, how far along
 * it has run, what it has cost so far and will cost, month by month, who it was
 * for, and the papers behind it.
 */
export function ChargeDetailDialog({
  rate,
  onClose,
  onEdit,
  onDelete,
}: {
  rate: AccommodationRate | null;
  onClose: () => void;
  onEdit: (rate: AccommodationRate) => void;
  onDelete: (rate: AccommodationRate) => void;
}) {
  const t = useT();
  const { locale } = useI18n();
  const enumLabel = useEnumLabel();
  const tracking = useAccommodationChargeTrackingQuery(rate?.id);
  const data = tracking.data;
  const money = (value: number) => formatMoney(value, locale);
  const monthName = (year: number, month: number) =>
    new Date(year, month - 1, 1).toLocaleDateString(locale === 'sr' ? 'sr-Latn' : 'en-GB', {
      month: 'long',
      year: 'numeric',
    });

  const progress =
    data && data.totalDays ? Math.min(100, Math.round((data.elapsedDays / data.totalDays) * 100)) : null;
  const peak = data ? Math.max(1, ...data.months.map((m) => m.total)) : 1;
  const employeeTotal = data ? data.byEmployee.reduce((sum, e) => sum + e.cost, 0) : 0;

  return (
    <Dialog open={!!rate} onClose={onClose} fullWidth maxWidth="md" scroll="paper">
      {rate && (
        <>
          <Box sx={{ px: 3, pt: 2.5, pb: 2, borderBottom: 1, borderColor: 'divider' }}>
            <Stack direction="row" spacing={2} sx={{ alignItems: 'flex-start', justifyContent: 'space-between' }}>
              <Box sx={{ minWidth: 0 }}>
                <Stack direction="row" spacing={1} useFlexGap sx={{ alignItems: 'center', flexWrap: 'wrap', mb: 0.5 }}>
                  <Chip
                    size="small"
                    icon={KIND_ICON[rate.kind] as React.ReactElement}
                    label={enumLabel('accommodationChargeKind', rate.kind)}
                    variant="outlined"
                  />
                  {rate.kind === 'OneOff' ? (
                    <Chip size="small" variant="outlined" label={t('accommodations.once')} />
                  ) : data?.notStarted ? (
                    <Chip size="small" color="info" variant="outlined" label={t('charge.notStarted')} />
                  ) : rate.endDate && data && data.asOf > rate.endDate ? (
                    <Chip size="small" variant="outlined" label={t('rates.ended')} />
                  ) : (
                    <Chip size="small" color="success" variant="outlined" label={t('rates.active')} />
                  )}
                </Stack>
                <Typography variant="h4" sx={{ fontWeight: 800, letterSpacing: -0.5, ...numeric }}>
                  {money(rate.amount)}{' '}
                  <Typography component="span" variant="body1" color="text.secondary">
                    {t(KIND_UNIT[rate.kind])}
                  </Typography>
                </Typography>
                <Link
                  component={RouterLink}
                  to={paths.accommodationDetail(rate.accommodationId)}
                  underline="hover"
                  variant="body2"
                  onClick={onClose}
                >
                  {rate.accommodationAddress}
                </Link>
              </Box>
              <IconButton onClick={onClose} aria-label={t('common.close')} sx={{ mt: -0.5, mr: -1 }}>
                <CloseOutlined />
              </IconButton>
            </Stack>
          </Box>

          <DialogContent sx={{ px: 3, py: 2.5 }}>
            {tracking.isLoading && (
              <Box sx={{ display: 'flex', justifyContent: 'center', p: 5 }}>
                <CircularProgress />
              </Box>
            )}

            {tracking.isError && <Alert severity="error">{toApiError(tracking.error).message}</Alert>}

            {data && (
              <Stack spacing={3}>
                {/* Period and progress */}
                {rate.kind !== 'OneOff' && (
                  <Box>
                    <Stack direction="row" sx={{ justifyContent: 'space-between', mb: 0.75 }}>
                      <Typography variant="body2" sx={{ fontWeight: 600 }}>
                        {formatDate(data.startDate)}
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        {data.notStarted
                          ? t('charge.startsIn')
                          : progress === null
                            ? t('charge.dayOpen', { days: data.elapsedDays })
                            : t('charge.dayOf', { day: data.elapsedDays, total: data.totalDays ?? 0 })}
                      </Typography>
                      <Typography variant="body2" sx={{ fontWeight: 600 }}>
                        {data.endDate ? formatDate(data.endDate) : t('rates.open')}
                      </Typography>
                    </Stack>
                    <LinearProgress
                      variant={progress === null ? 'indeterminate' : 'determinate'}
                      value={progress ?? 0}
                      sx={{ height: 8, borderRadius: 4 }}
                    />
                  </Box>
                )}

                {rate.kind === 'OneOff' && (
                  <Typography variant="body2" color="text.secondary">
                    {t('charge.oneOffOn', { date: formatDate(data.startDate) })}
                  </Typography>
                )}

                {/* Numbers */}
                {!data.notStarted && (
                  <Stack direction="row" useFlexGap sx={{ flexWrap: 'wrap', gap: 1.5 }}>
                    <Tile
                      label={t('charge.chargedToDate')}
                      value={money(data.chargedToDate)}
                      hint={t('charge.asOf', { date: formatDate(data.asOf) })}
                    />
                    {data.projectedTotal !== null && rate.kind !== 'OneOff' && (
                      <Tile
                        label={t('charge.projected')}
                        value={money(data.projectedTotal)}
                        hint={t('charge.projectedHint')}
                      />
                    )}
                    {rate.kind === 'Monthly' && (
                      <Tile
                        label={t('charge.vacancy')}
                        value={money(data.vacancyCost)}
                        hint={t('charge.vacantDays', { days: data.vacantDays })}
                        tone={data.vacancyCost > 0 ? 'warn' : undefined}
                      />
                    )}
                    {rate.kind !== 'OneOff' && (
                      <Tile
                        label={t('charge.personDays')}
                        value={String(data.personDays)}
                        hint={
                          data.personDays > 0
                            ? t('charge.perPersonDay', {
                                amount: money(data.chargedToDate / data.personDays),
                              })
                            : undefined
                        }
                      />
                    )}
                  </Stack>
                )}

                {/* Month by month */}
                {data.months.length > 0 && (
                  <Box>
                    <SectionTitle>{t('charge.monthByMonth')}</SectionTitle>
                    <Stack spacing={1} sx={{ mt: 1 }}>
                      {data.months.map((month) => {
                        const used = month.total - month.vacancyCost;

                        return (
                          <Stack
                            key={`${month.year}-${month.month}`}
                            direction="row"
                            spacing={1.5}
                            sx={{ alignItems: 'center' }}
                          >
                            <Typography variant="body2" sx={{ width: { xs: 96, sm: 130 }, flexShrink: 0 }} noWrap>
                              {monthName(month.year, month.month)}
                            </Typography>
                            <Box
                              sx={(theme) => ({
                                flex: 1,
                                height: 10,
                                borderRadius: 5,
                                bgcolor: alpha(theme.palette.text.primary, 0.06),
                                overflow: 'hidden',
                                display: 'flex',
                              })}
                            >
                              <Box
                                sx={{
                                  width: `${(used / peak) * 100}%`,
                                  bgcolor: 'primary.main',
                                }}
                              />
                              <Box
                                sx={(theme) => ({
                                  width: `${(month.vacancyCost / peak) * 100}%`,
                                  bgcolor: alpha(theme.palette.warning.main, 0.55),
                                })}
                              />
                            </Box>
                            <Typography variant="body2" sx={{ width: 92, textAlign: 'right', fontWeight: 600, ...numeric }}>
                              {money(month.total)}
                            </Typography>
                          </Stack>
                        );
                      })}
                    </Stack>
                    {data.months.some((m) => m.vacancyCost > 0) && (
                      <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 1 }}>
                        {t('charge.vacancyLegend')}
                      </Typography>
                    )}
                  </Box>
                )}

                {/* Who it was for */}
                {data.byEmployee.length > 0 && (
                  <Box>
                    <SectionTitle>{t('charge.whoFor')}</SectionTitle>
                    <Stack divider={<Box sx={{ borderTop: 1, borderColor: 'divider' }} />} sx={{ mt: 0.5 }}>
                      {data.byEmployee.map((person) => (
                        <Stack
                          key={person.employeeId}
                          direction="row"
                          spacing={2}
                          sx={{ py: 1, alignItems: 'center', justifyContent: 'space-between' }}
                        >
                          <Box sx={{ minWidth: 0 }}>
                            <Typography variant="body2" sx={{ fontWeight: 600 }} noWrap>
                              {person.employeeName}
                            </Typography>
                            <Typography variant="caption" color="text.secondary">
                              {t('charge.days', { days: person.personDays })}
                              {employeeTotal > 0 ? ` · ${Math.round((person.cost / employeeTotal) * 100)}%` : ''}
                            </Typography>
                          </Box>
                          <Typography variant="body2" sx={{ fontWeight: 600, ...numeric }}>
                            {money(person.cost)}
                          </Typography>
                        </Stack>
                      ))}
                    </Stack>
                  </Box>
                )}

                {data.byProject.length > 0 && (
                  <Box>
                    <SectionTitle>{t('charge.byProject')}</SectionTitle>
                    <Stack direction="row" useFlexGap sx={{ flexWrap: 'wrap', gap: 1, mt: 1 }}>
                      {data.byProject.map((share) => (
                        <Chip
                          key={share.projectId ?? 'none'}
                          variant="outlined"
                          label={`${share.projectName ?? t('charge.noProject')} · ${money(share.cost)}`}
                        />
                      ))}
                    </Stack>
                  </Box>
                )}

                {/* Facts */}
                <Box>
                  <SectionTitle>{t('charge.details')}</SectionTitle>
                  <Box sx={{ mt: 0.5 }}>
                    <DetailRow label={t('accommodations.provider')} value={rate.provider || '—'} />
                    <DetailRow label={t('charge.recordedBy')} value={rate.setByName || '—'} />
                    <DetailRow label={t('charge.recordedOn')} value={formatDate(rate.createdAt)} />
                    {rate.note && <DetailRow label={t('rates.note')} value={rate.note} />}
                  </Box>
                </Box>

                <Box>
                  <SectionTitle>{t('charge.documents')}</SectionTitle>
                  <Box sx={{ mt: 1 }}>
                    <AttachmentList
                      ownerType="AccommodationRate"
                      ownerId={rate.id}
                      categories={['Contract', 'Other']}
                    />
                  </Box>
                </Box>
              </Stack>
            )}
          </DialogContent>

          <DialogActions sx={{ px: 3, py: 1.5, borderTop: 1, borderColor: 'divider' }}>
            <Button color="error" startIcon={<DeleteOutlined />} onClick={() => onDelete(rate)}>
              {t('common.delete')}
            </Button>
            <Box sx={{ flex: 1 }} />
            <Button onClick={onClose}>{t('common.close')}</Button>
            <Button variant="contained" startIcon={<EditOutlined />} onClick={() => onEdit(rate)}>
              {t('common.edit')}
            </Button>
          </DialogActions>
        </>
      )}
    </Dialog>
  );
}
