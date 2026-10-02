import { AddOutlined, PrintOutlined } from '@mui/icons-material';
import { Box, Button, MenuItem, Stack, TextField } from '@mui/material';
import type { GridColDef } from '@mui/x-data-grid';
import { useMemo, useState } from 'react';

import { invoiceStatuses, type Invoice, type InvoiceStatus } from '../../api/invoices';
import { PageHeader } from '../../components/PageHeader';
import { ReasonDialog } from '../../components/ReasonDialog';
import { ResourceDataGrid } from '../../components/ResourceDataGrid';
import { StatusChip } from '../../components/StatusChip';
import {
  useCancelInvoice,
  useInvoicesQuery,
  useMarkInvoicePaid,
} from '../../features/invoices/useInvoices';
import { useHighlightTarget } from '../../hooks/useHighlightTarget';
import { useListQueryState } from '../../hooks/useListQueryState';
import { useOpenOnParam } from '../../hooks/useOpenOnParam';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useI18n } from '../../i18n/useI18n';
import { formatDate, formatMoney } from '../../utils/formatting';
import { BranchDot } from '../../features/branches/BranchDot';
import { InvoiceDocumentDialog } from './InvoiceDocumentDialog';
import { NewInvoiceDialog } from './NewInvoiceDialog';

/**
 * The invoices the firm issued to its clients, recorded here. For a site billed by a fixed sum or
 * by measured work, what is invoiced in a month is what the payroll bills for it. An invoice can
 * go to one company of the client or be split among several. Laid out like every other list.
 */
export function InvoicesPage() {
  const { t, locale } = useI18n();
  const enumLabel = useEnumLabel();
  const list = useListQueryState('issueDate', 'desc');
  const { targetId, isHighlighted } = useHighlightTarget();
  const markPaid = useMarkInvoicePaid();
  const cancel = useCancelInvoice();

  const [status, setStatus] = useState<InvoiceStatus | ''>('');
  const [creating, setCreating] = useState(false);
  const [cancelling, setCancelling] = useState<Invoice | null>(null);
  const [viewing, setViewing] = useState<Invoice | null>(null);
  useOpenOnParam('new', () => setCreating(true));

  const query = useMemo(
    () => ({ ...list.query, status: status || undefined }),
    [list.query, status],
  );

  const { data, isLoading, isError, error, refetch } = useInvoicesQuery(query);

  const columns: GridColDef<Invoice>[] = useMemo(
    () => [
      { field: 'number', headerName: t('invoices.number'), width: 150 },
      {
        field: 'projectName',
        headerName: t('invoices.project'),
        flex: 1,
        minWidth: 180,
        sortable: false,
        valueGetter: (_value, row) => (row.customerName ? `${row.projectName} · ${row.customerName}` : row.projectName),
      },
      {
        field: 'branchName',
        headerName: t('branches.single'),
        width: 190,
        sortable: false,
        renderCell: (params) =>
          params.row.branchName ? (
            <Stack direction="row" spacing={1} sx={{ alignItems: 'center', height: '100%' }}>
              <BranchDot color={params.row.branchColor ?? '#999999'} />
              <span>{params.row.branchName}</span>
            </Stack>
          ) : (
            '—'
          ),
      },
      {
        field: 'companies',
        headerName: t('invoices.companies'),
        flex: 1,
        minWidth: 180,
        sortable: false,
        valueGetter: (_value, row) => {
          const named = row.shares.filter((share) => share.companyName);

          if (named.length === 0) return t('invoices.wholeClientShort');
          if (named.length === 1) return named[0]!.companyName!;

          return `${named[0]!.companyName} +${named.length - 1}`;
        },
      },
      {
        field: 'amount',
        headerName: t('invoices.amount'),
        width: 130,
        align: 'right',
        headerAlign: 'right',
        valueGetter: (_value, row) => formatMoney(row.amount, locale),
      },
      { field: 'issueDate', headerName: t('invoices.issueDate'), width: 130, valueGetter: (value) => formatDate(value) },
      {
        field: 'payrollMonth',
        headerName: t('invoices.payrollMonth'),
        width: 120,
        sortable: false,
        valueGetter: (_value, row) => `${String(row.payrollMonth).padStart(2, '0')}.${row.payrollYear}`,
      },
      {
        field: 'status',
        headerName: t('invoices.statusColumn'),
        width: 140,
        renderCell: (params) => <StatusChip status={params.row.status} kind="invoiceStatus" />,
      },
      {
        field: 'actions',
        headerName: '',
        width: 290,
        sortable: false,
        filterable: false,
        align: 'right',
        headerAlign: 'right',
        renderCell: (params) => {
          const invoice = params.row;

          return (
            <Stack direction="row" spacing={1} sx={{ alignItems: 'center', height: '100%' }}>
              <Button size="small" startIcon={<PrintOutlined />} onClick={() => setViewing(invoice)}>
                {t('invoices.document.open')}
              </Button>
              {invoice.status === 'Issued' && (
                <>
                  <Button size="small" disabled={markPaid.isPending} onClick={() => markPaid.mutate(invoice.id)}>
                    {t('invoices.markPaid')}
                  </Button>
                  <Button size="small" color="error" onClick={() => setCancelling(invoice)}>
                    {t('invoices.cancel')}
                  </Button>
                </>
              )}
            </Stack>
          );
        },
      },
    ],
    [t, locale, markPaid],
  );

  return (
    <Box>
      <PageHeader
        title={t('invoices.title')}
        subtitle={data ? t('common.total', { count: data.totalCount }) : undefined}
        description={t('invoices.description')}
        action={{ label: t('invoices.new'), icon: <AddOutlined />, onClick: () => setCreating(true) }}
      />

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} useFlexGap sx={{ flexWrap: 'wrap', mb: 2 }}>
        <TextField
          select
          size="small"
          label={t('invoices.statusColumn')}
          value={status}
          onChange={(event) => {
            setStatus(event.target.value as InvoiceStatus | '');
            list.resetToFirstPage();
          }}
          sx={{ minWidth: 200 }}
        >
          <MenuItem value="">{t('common.all')}</MenuItem>
          {invoiceStatuses.map((value) => (
            <MenuItem key={value} value={value}>
              {enumLabel('invoiceStatus', value)}
            </MenuItem>
          ))}
        </TextField>
      </Stack>

      <ResourceDataGrid
        data={data}
        columns={columns}
        isLoading={isLoading}
        isError={isError}
        error={error}
        onRetry={() => void refetch()}
        paginationModel={list.paginationModel}
        onPaginationModelChange={list.setPaginationModel}
        sortModel={list.sortModel}
        onSortModelChange={list.setSortModel}
        highlightedId={targetId && isHighlighted(targetId) ? targetId : null}
      />

      <NewInvoiceDialog open={creating} onClose={() => setCreating(false)} />
      <InvoiceDocumentDialog invoice={viewing} onClose={() => setViewing(null)} />
      <ReasonDialog
        open={!!cancelling}
        title={t('invoices.cancelTitle')}
        hint={t('invoices.cancelHint')}
        label={t('invoices.cancelReason')}
        submitLabel={t('invoices.cancel')}
        onClose={() => setCancelling(null)}
        onSubmit={async (reason) => {
          if (!cancelling) return;
          await cancel.mutateAsync({ id: cancelling.id, reason });
          setCancelling(null);
        }}
      />
    </Box>
  );
}
