import { DeleteOutlined, EditOutlined } from '@mui/icons-material';
import {
  Alert,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
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
  Tooltip,
  Typography,
} from '@mui/material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';

import { toApiError } from '../../api/apiError';
import {
  companyRevenueSources,
  financeApi,
  type CompanyRevenue,
  type CompanyRevenueInput,
  type CompanyRevenueSource,
} from '../../api/finance';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { PageHeader } from '../../components/PageHeader';
import { useAllToolsQuery } from '../../features/tools/useTools';
import { useAllVehiclesQuery } from '../../features/vehicles/useVehicles';
import type { MessageKey } from '../../i18n/en';
import { useI18n } from '../../i18n/useI18n';
import { formatDate, formatMoney } from '../../utils/formatting';

const PAGE_SIZE = 50;
const listKey = ['finance', 'company-revenues'] as const;

/**
 * Money received that belongs to no project — renting a vehicle or a tool out,
 * or anything else. Together with what comes in against project contracts it
 * is the company's income on the finance widgets.
 */
export function CompanyRevenuesPage() {
  const { t, locale } = useI18n();
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState<CompanyRevenue | 'new' | null>(null);
  const [deleting, setDeleting] = useState<CompanyRevenue | null>(null);

  const { data, isLoading, error } = useQuery({
    queryKey: [...listKey, PAGE_SIZE] as const,
    queryFn: () => financeApi.companyRevenues.list({ pageNumber: 1, pageSize: PAGE_SIZE }),
  });

  // The widgets read the same money, so anything recorded here has to reach them.
  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: ['finance'] });
  };

  const remove = useMutation({
    mutationFn: (revenue: CompanyRevenue) => financeApi.companyRevenues.remove(revenue.id),
    onSuccess: refresh,
  });

  return (
    <Stack spacing={2}>
      <PageHeader
        title={t('finance.revenues.title')}
        description={t('finance.revenues.description')}
        action={{ label: t('finance.revenues.add'), onClick: () => setEditing('new') }}
      />

      {error && <Alert severity="error">{toApiError(error).message}</Alert>}

      <Paper variant="outlined" sx={{ overflowX: 'auto' }}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>{t('finance.revenues.date')}</TableCell>
              <TableCell>{t('finance.revenues.source')}</TableCell>
              <TableCell>{t('finance.revenues.asset')}</TableCell>
              <TableCell>{t('finance.revenues.note')}</TableCell>
              <TableCell align="right">{t('finance.revenues.amount')}</TableCell>
              <TableCell />
            </TableRow>
          </TableHead>
          <TableBody>
            {(data?.items ?? []).map((revenue) => (
              <TableRow key={revenue.id} hover>
                <TableCell>{formatDate(revenue.occurredOn)}</TableCell>
                <TableCell>{t(`finance.source.${revenue.source}` as MessageKey)}</TableCell>
                <TableCell>{revenue.vehicleName ?? revenue.toolName ?? '—'}</TableCell>
                <TableCell>{revenue.note ?? ''}</TableCell>
                <TableCell align="right">{formatMoney(revenue.amount, locale)}</TableCell>
                <TableCell align="right" sx={{ whiteSpace: 'nowrap' }}>
                  <Tooltip title={t('common.edit')}>
                    <IconButton size="small" onClick={() => setEditing(revenue)} aria-label={t('common.edit')}>
                      <EditOutlined fontSize="small" />
                    </IconButton>
                  </Tooltip>
                  <Tooltip title={t('common.delete')}>
                    <IconButton size="small" onClick={() => setDeleting(revenue)} aria-label={t('common.delete')}>
                      <DeleteOutlined fontSize="small" />
                    </IconButton>
                  </Tooltip>
                </TableCell>
              </TableRow>
            ))}
            {!isLoading && (data?.items.length ?? 0) === 0 && (
              <TableRow>
                <TableCell colSpan={6}>
                  <Typography color="text.secondary" sx={{ py: 2 }}>
                    {t('finance.revenues.empty')}
                  </Typography>
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </Paper>

      {editing && (
        <RevenueDialog
          revenue={editing === 'new' ? null : editing}
          onClose={() => setEditing(null)}
          onSaved={() => {
            setEditing(null);
            refresh();
          }}
        />
      )}

      <ConfirmDialog
        open={deleting !== null}
        title={t('finance.revenues.deleteTitle')}
        description={t('finance.revenues.deleteBody')}
        confirmLabel={t('common.delete')}
        destructive
        onConfirm={async () => {
          if (deleting) await remove.mutateAsync(deleting);
          setDeleting(null);
        }}
        onCancel={() => setDeleting(null)}
      />
    </Stack>
  );
}

function today(): string {
  const now = new Date();
  const month = String(now.getMonth() + 1).padStart(2, '0');
  const day = String(now.getDate()).padStart(2, '0');
  return `${now.getFullYear()}-${month}-${day}`;
}

function RevenueDialog({
  revenue,
  onClose,
  onSaved,
}: {
  revenue: CompanyRevenue | null;
  onClose: () => void;
  onSaved: () => void;
}) {
  const { t } = useI18n();
  const vehicles = useAllVehiclesQuery();
  const tools = useAllToolsQuery();

  const [amount, setAmount] = useState(revenue ? String(revenue.amount) : '');
  const [occurredOn, setOccurredOn] = useState(revenue?.occurredOn ?? today());
  const [source, setSource] = useState<CompanyRevenueSource>(revenue?.source ?? 'VehicleRental');
  const [vehicleId, setVehicleId] = useState(revenue?.vehicleId ?? '');
  const [toolId, setToolId] = useState(revenue?.toolId ?? '');
  const [note, setNote] = useState(revenue?.note ?? '');

  const parsed = Number(amount.replace(',', '.'));
  const valid = Number.isFinite(parsed) && parsed > 0 && occurredOn !== '';

  const save = useMutation({
    mutationFn: () => {
      const input: CompanyRevenueInput = {
        amount: parsed,
        occurredOn,
        source,
        // The asset only means something for its own kind of rental.
        vehicleId: source === 'VehicleRental' && vehicleId ? vehicleId : null,
        toolId: source === 'ToolRental' && toolId ? toolId : null,
        note: note.trim() || null,
      };
      return revenue ? financeApi.companyRevenues.update(revenue.id, input) : financeApi.companyRevenues.record(input);
    },
    onSuccess: onSaved,
  });

  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>{revenue ? t('finance.revenues.edit') : t('finance.revenues.add')}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ pt: 1 }}>
          <TextField
            select
            label={t('finance.revenues.source')}
            value={source}
            onChange={(event) => setSource(event.target.value as CompanyRevenueSource)}
          >
            {companyRevenueSources.map((option) => (
              <MenuItem key={option} value={option}>
                {t(`finance.source.${option}` as MessageKey)}
              </MenuItem>
            ))}
          </TextField>

          {source === 'VehicleRental' && (
            <TextField
              select
              label={t('finance.revenues.vehicle')}
              value={vehicleId}
              onChange={(event) => setVehicleId(event.target.value)}
            >
              <MenuItem value="">
                <em>{t('finance.revenues.noAsset')}</em>
              </MenuItem>
              {(vehicles.data?.items ?? []).map((vehicle) => (
                <MenuItem key={vehicle.id} value={vehicle.id}>
                  {vehicle.brand} {vehicle.model} ({vehicle.registrationNumber})
                </MenuItem>
              ))}
            </TextField>
          )}

          {source === 'ToolRental' && (
            <TextField
              select
              label={t('finance.revenues.tool')}
              value={toolId}
              onChange={(event) => setToolId(event.target.value)}
            >
              <MenuItem value="">
                <em>{t('finance.revenues.noAsset')}</em>
              </MenuItem>
              {(tools.data?.items ?? []).map((tool) => (
                <MenuItem key={tool.id} value={tool.id}>
                  {tool.name}
                </MenuItem>
              ))}
            </TextField>
          )}

          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
            <TextField
              label={t('finance.revenues.amount')}
              value={amount}
              onChange={(event) => setAmount(event.target.value)}
              slotProps={{ htmlInput: { inputMode: 'decimal' } }}
              error={amount !== '' && !valid}
              fullWidth
            />
            <TextField
              type="date"
              label={t('finance.revenues.date')}
              value={occurredOn}
              onChange={(event) => setOccurredOn(event.target.value)}
              slotProps={{ inputLabel: { shrink: true }, htmlInput: { max: today() } }}
              fullWidth
            />
          </Stack>

          <TextField
            label={t('finance.revenues.note')}
            value={note}
            onChange={(event) => setNote(event.target.value)}
            slotProps={{ htmlInput: { maxLength: 500 } }}
            multiline
            minRows={2}
          />

          {save.error && <Alert severity="error">{toApiError(save.error).message}</Alert>}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button variant="contained" disabled={!valid} loading={save.isPending} onClick={() => save.mutate()}>
          {t('common.save')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
