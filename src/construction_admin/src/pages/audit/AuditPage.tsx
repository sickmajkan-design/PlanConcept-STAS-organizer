import { VisibilityOutlined } from '@mui/icons-material';
import {
  Box,
  Dialog,
  DialogContent,
  DialogTitle,
  Divider,
  IconButton,
  MenuItem,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import type { GridColDef, GridPaginationModel, GridSortModel } from '@mui/x-data-grid';
import { useMemo, useState } from 'react';

import type { AuditTrailQuery } from '../../api/audit';
import { auditActions, type AuditAction, type AuditEntry } from '../../api/types';
import { PageHeader } from '../../components/PageHeader';
import { ResourceDataGrid } from '../../components/ResourceDataGrid';
import { StatusChip } from '../../components/StatusChip';
import { useAuditListQuery } from '../../features/audit/useAudit';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useT } from '../../i18n/useI18n';
import { formatDateTime } from '../../utils/formatting';

/**
 * The data audit trail already collects, made visible.
 *
 * Read-only by design — see `AuditController` for why there is nothing here
 * to write or delete. No sorting either: the API always answers newest
 * first, so there is nothing a column header could usefully reorder.
 */
export function AuditPage() {
  const t = useT();
  const enumLabel = useEnumLabel();

  const [entityName, setEntityName] = useState('');
  const [action, setAction] = useState<AuditAction | ''>('');
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');
  const [paginationModel, setPaginationModel] = useState<GridPaginationModel>({
    page: 0,
    pageSize: 50,
  });
  const [viewing, setViewing] = useState<AuditEntry | null>(null);

  const query: AuditTrailQuery = useMemo(
    () => ({
      entityName: entityName.trim() || undefined,
      action: action || undefined,
      from: from || undefined,
      to: to || undefined,
      pageNumber: paginationModel.page + 1,
      pageSize: paginationModel.pageSize,
    }),
    [entityName, action, from, to, paginationModel],
  );

  const { data, isLoading, isError, error, refetch } = useAuditListQuery(query);

  const resetToFirstPage = () => setPaginationModel((prev) => ({ ...prev, page: 0 }));

  const columns: GridColDef<AuditEntry>[] = useMemo(
    () => [
      {
        field: 'occurredAt',
        headerName: t('audit.occurredAt'),
        width: 170,
        sortable: false,
        valueGetter: (value) => formatDateTime(value as string),
      },
      {
        field: 'action',
        headerName: t('audit.action'),
        width: 120,
        sortable: false,
        renderCell: (params) => <StatusChip status={params.value} kind="auditAction" />,
      },
      {
        field: 'entityName',
        headerName: t('audit.entityName'),
        width: 160,
        sortable: false,
        valueGetter: (value) => enumLabel('auditEntity', value as string),
      },
      {
        field: 'entityId',
        headerName: t('audit.entityId'),
        flex: 1,
        minWidth: 220,
        sortable: false,
        renderCell: (params) => (
          <Typography variant="body2" sx={{ fontFamily: 'monospace' }}>
            {params.value}
          </Typography>
        ),
      },
      {
        field: 'userEmail',
        headerName: t('audit.user'),
        flex: 1,
        minWidth: 180,
        sortable: false,
        valueGetter: (value) => value || t('audit.systemActor'),
      },
      {
        field: 'userRole',
        headerName: t('audit.userRole'),
        width: 130,
        sortable: false,
        valueGetter: (value) => enumLabel('role', value as string),
      },
      {
        field: 'ipAddress',
        headerName: t('audit.ipAddress'),
        width: 130,
        sortable: false,
        valueGetter: (value) => value || '—',
      },
      {
        field: 'changes',
        headerName: '',
        width: 60,
        sortable: false,
        filterable: false,
        align: 'right',
        headerAlign: 'right',
        renderCell: (params) =>
          Object.keys(params.value as object).length > 0 ? (
            <IconButton
              size="small"
              onClick={(event) => {
                event.stopPropagation();
                setViewing(params.row);
              }}
            >
              <VisibilityOutlined fontSize="small" />
            </IconButton>
          ) : null,
      },
    ],
    [t],
  );

  const changeLines = viewing
    ? Object.entries(viewing.changes).map(
        ([field, change]) => `${field}: ${change.from ?? '—'} → ${change.to ?? '—'}`,
      )
    : [];

  const noopSortModel: GridSortModel = [];

  return (
    <Box>
      <PageHeader
        title={t('audit.pageTitle')}
        description={t('audit.pageDescription')}
        subtitle={data ? t('common.total', { count: data.totalCount }) : undefined}
      />

      <Stack direction="row" spacing={2} sx={{ mb: 2, flexWrap: 'wrap' }}>
        <TextField
          size="small"
          label={t('audit.entityName')}
          value={entityName}
          onChange={(event) => {
            setEntityName(event.target.value);
            resetToFirstPage();
          }}
          sx={{ minWidth: 180 }}
        />
        <TextField
          select
          size="small"
          label={t('audit.action')}
          value={action}
          onChange={(event) => {
            setAction(event.target.value as AuditAction | '');
            resetToFirstPage();
          }}
          sx={{ minWidth: 160 }}
        >
          <MenuItem value="">{t('audit.allActions')}</MenuItem>
          {auditActions.map((value) => (
            <MenuItem key={value} value={value}>
              {enumLabel('auditAction', value)}
            </MenuItem>
          ))}
        </TextField>
        <TextField
          type="date"
          size="small"
          label={t('audit.from')}
          value={from}
          onChange={(event) => {
            setFrom(event.target.value);
            resetToFirstPage();
          }}
          slotProps={{ inputLabel: { shrink: true } }}
          sx={{ minWidth: 160 }}
        />
        <TextField
          type="date"
          size="small"
          label={t('audit.to')}
          value={to}
          onChange={(event) => {
            setTo(event.target.value);
            resetToFirstPage();
          }}
          slotProps={{ inputLabel: { shrink: true } }}
          sx={{ minWidth: 160 }}
        />
      </Stack>

      <ResourceDataGrid
        data={data}
        columns={columns}
        isLoading={isLoading}
        isError={isError}
        error={error}
        onRetry={() => void refetch()}
        paginationModel={paginationModel}
        onPaginationModelChange={setPaginationModel}
        sortModel={noopSortModel}
        onSortModelChange={() => {}}
      />

      <Dialog open={!!viewing} onClose={() => setViewing(null)} fullWidth maxWidth="sm">
        <DialogTitle>{t('audit.changesTitle')}</DialogTitle>
        <DialogContent>
          <Stack divider={<Divider />} spacing={1}>
            {changeLines.map((line) => (
              <Typography key={line} variant="body2">
                {line}
              </Typography>
            ))}
          </Stack>
        </DialogContent>
      </Dialog>
    </Box>
  );
}
