import { CloseOutlined } from '@mui/icons-material';
import {
  Alert,
  Box,
  CircularProgress,
  Dialog,
  DialogContent,
  IconButton,
  Link,
  Stack,
  Typography,
} from '@mui/material';
import { Link as RouterLink } from 'react-router-dom';

import { toApiError } from '../../api/apiError';
import {
  CompositionBar,
  CompositionLegend,
  CostLine,
  CostSection,
  EmptyLine,
  Tile,
  numeric,
  useCostColors,
} from '../../components/costs/costUi';
import { useProjectCostBreakdownQuery } from '../../features/costs/useCosts';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useI18n, useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';
import { formatDate, formatMoney, formatQuantity } from '../../utils/formatting';
import { splitHours, type Period } from './monthWindow';

const hoursText = (minutes: number) => {
  const { hours, minutes: rest } = splitHours(minutes);
  return `${hours}:${String(rest).padStart(2, '0')} h`;
};

/**
 * Everything one site cost in the period, itemised: who worked and what they
 * cost, which material was issued, the other costs, the share of housing, and
 * the manual pay entries that sit beside the total.
 */
export function ProjectCostDialog({
  projectId,
  period,
  onClose,
}: {
  projectId: string | null;
  period: Period;
  onClose: () => void;
}) {
  const t = useT();
  const { locale } = useI18n();
  const enumLabel = useEnumLabel();
  const colors = useCostColors();
  const query = useProjectCostBreakdownQuery(projectId ?? undefined, period);
  const data = query.data;
  const money = (value: number) => formatMoney(value, locale);
  const total = data?.summary.total ?? 0;
  const share = (value: number) => (total > 0 ? (value / total) * 100 : 0);

  const segments = data
    ? [
        { key: 'labour', label: t('costs.labour'), value: data.summary.labourCost, color: colors.labour },
        { key: 'material', label: t('costs.material'), value: data.summary.materialCost, color: colors.material },
        { key: 'other', label: t('costs.generalExpense'), value: data.summary.generalExpenseCost, color: colors.other },
        { key: 'housing', label: t('costs.accommodation'), value: data.summary.accommodationCost, color: colors.housing },
      ]
    : [];

  return (
    <Dialog open={!!projectId} onClose={onClose} fullWidth maxWidth="md" scroll="paper">
      <Box sx={{ px: 3, pt: 2.5, pb: 2, borderBottom: 1, borderColor: 'divider' }}>
        <Stack direction="row" spacing={2} sx={{ alignItems: 'flex-start', justifyContent: 'space-between' }}>
          <Box sx={{ minWidth: 0 }}>
            <Typography variant="caption" color="text.secondary">
              {formatDate(period.from)} – {formatDate(period.to)}
            </Typography>
            <Typography variant="h5" sx={{ fontWeight: 800, letterSpacing: -0.3 }}>
              {data?.projectName ?? ''}
            </Typography>
            {data && (
              <Link
                component={RouterLink}
                to={paths.projectDetail(data.projectId)}
                underline="hover"
                variant="body2"
                onClick={onClose}
              >
                {t('costs.openProject')}
              </Link>
            )}
          </Box>
          <Stack direction="row" spacing={1} sx={{ alignItems: 'flex-start' }}>
            {data && (
              <Box sx={{ textAlign: 'right' }}>
                <Typography variant="caption" color="text.secondary">
                  {t('costs.total')}
                </Typography>
                <Typography variant="h4" sx={{ fontWeight: 800, letterSpacing: -0.5, lineHeight: 1.1, ...numeric }}>
                  {money(data.summary.total)}
                </Typography>
              </Box>
            )}
            <IconButton onClick={onClose} aria-label={t('common.close')} sx={{ mr: -1 }}>
              <CloseOutlined />
            </IconButton>
          </Stack>
        </Stack>
      </Box>

      <DialogContent sx={{ px: 3, py: 2.5 }}>
        {query.isLoading && (
          <Box sx={{ display: 'flex', justifyContent: 'center', p: 5 }}>
            <CircularProgress />
          </Box>
        )}

        {query.isError && <Alert severity="error">{toApiError(query.error).message}</Alert>}

        {data && (
          <Stack spacing={3.5}>
            <Stack spacing={1.25}>
              <CompositionBar segments={segments} height={12} />
              <CompositionLegend segments={segments} format={money} showShare />
            </Stack>

            {!data.includesLabour && <Alert severity="info">{t('costs.labourHidden')}</Alert>}

            {data.summary.unpricedMinutes > 0 && (
              <Alert severity="warning">
                {t('costs.unpricedWarning', { count: Math.round(data.summary.unpricedMinutes / 60) })}
              </Alert>
            )}

            <Stack direction="row" useFlexGap sx={{ flexWrap: 'wrap', gap: 1.5 }}>
              {data.includesLabour && (
                <Tile
                  label={t('costs.hours')}
                  value={hoursText(data.summary.labourMinutes)}
                  hint={t('costs.approvedOnly')}
                />
              )}
              <Tile
                label={t('costs.materialsOnSite')}
                value={money(data.summary.materialsOnSiteValue)}
                hint={t('costs.notInTotal')}
              />
              {data.includesLabour && (
                <Tile
                  label={t('costs.manualPay')}
                  value={money(data.summary.manualPayAmount)}
                  hint={t('costs.notInTotal')}
                />
              )}
            </Stack>

            {data.includesLabour && (
              <CostSection
                title={t('costs.labour')}
                color={colors.labour}
                total={money(data.summary.labourCost)}
                share={share(data.summary.labourCost)}
              >
                {data.labour.length === 0 ? (
                  <EmptyLine>{t('costs.noEntries')}</EmptyLine>
                ) : (
                  data.labour.map((line) => (
                    <CostLine
                      key={line.employeeId}
                      primary={line.employeeName}
                      secondary={
                        line.unpricedMinutes > 0
                          ? t('costs.unpricedLine', { hours: hoursText(line.unpricedMinutes) })
                          : undefined
                      }
                      amount={money(line.cost)}
                      meta={hoursText(line.minutes)}
                    />
                  ))
                )}
              </CostSection>
            )}

            <CostSection
              title={t('costs.material')}
              color={colors.material}
              total={money(data.summary.materialCost)}
              share={share(data.summary.materialCost)}
              note={t('costs.materialNote')}
            >
              {data.materials.length === 0 ? (
                <EmptyLine>{t('costs.noEntries')}</EmptyLine>
              ) : (
                data.materials.map((line) => (
                  <CostLine
                    key={line.id}
                    primary={line.materialName}
                    secondary={[formatDate(line.occurredOn), line.note].filter(Boolean).join(' · ')}
                    amount={money(line.total)}
                    meta={`${formatQuantity(line.quantity, locale)} ${line.unit} × ${money(line.unitPrice)}`}
                  />
                ))
              )}
            </CostSection>

            <CostSection
              title={t('costs.generalExpense')}
              color={colors.other}
              total={money(data.summary.generalExpenseCost)}
              share={share(data.summary.generalExpenseCost)}
            >
              {data.generalExpenses.length === 0 ? (
                <EmptyLine>{t('costs.noEntries')}</EmptyLine>
              ) : (
                data.generalExpenses.map((line) => (
                  <CostLine
                    key={line.id}
                    primary={enumLabel('generalExpenseCategory', line.category)}
                    secondary={[formatDate(line.occurredOn), line.supplier, line.employeeName, line.note]
                      .filter(Boolean)
                      .join(' · ')}
                    amount={money(line.amount)}
                  />
                ))
              )}
            </CostSection>

            <CostSection
              title={t('costs.accommodation')}
              color={colors.housing}
              total={money(data.summary.accommodationCost)}
              share={share(data.summary.accommodationCost)}
              note={t('costs.accommodationNote')}
            >
              {data.accommodation.length === 0 ? (
                <EmptyLine>{t('costs.noEntries')}</EmptyLine>
              ) : (
                data.accommodation.map((line) => (
                  <CostLine
                    key={line.accommodationId}
                    primary={
                      <Link
                        component={RouterLink}
                        to={paths.accommodationDetail(line.accommodationId)}
                        underline="hover"
                        color="inherit"
                        onClick={onClose}
                      >
                        {line.accommodationName}
                      </Link>
                    }
                    amount={money(line.cost)}
                  />
                ))
              )}
            </CostSection>

            {data.includesLabour && (
              <CostSection
                title={t('costs.manualPayEntries')}
                total={money(data.summary.manualPayAmount)}
                note={t('costs.manualPayHint')}
              >
                {data.manualPay.length === 0 ? (
                  <EmptyLine>{t('costs.noEntries')}</EmptyLine>
                ) : (
                  data.manualPay.map((line) => (
                    <CostLine
                      key={line.id}
                      primary={line.employeeName}
                      secondary={[
                        formatDate(line.occurredOn),
                        enumLabel('financeEntryKind', line.kind),
                        line.note,
                      ]
                        .filter(Boolean)
                        .join(' · ')}
                      amount={money(line.amount)}
                      meta={line.hoursWorked === null ? undefined : `${line.hoursWorked} h`}
                    />
                  ))
                )}
              </CostSection>
            )}
          </Stack>
        )}
      </DialogContent>
    </Dialog>
  );
}
