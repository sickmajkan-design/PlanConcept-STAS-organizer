import {
  AddOutlined,
  DeleteOutlined,
  EditOutlined,
  LocalShippingOutlined,
  QrCodeScannerOutlined,
  VisibilityOutlined,
  WarningAmberOutlined,
} from '@mui/icons-material';
import {
  Box,
  Button,
  FormControl,
  FormControlLabel,
  IconButton,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  Switch,
  Tooltip,
  Typography,
} from '@mui/material';
import type { GridColDef, GridSortModel } from '@mui/x-data-grid';
import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';

import type { VehicleListQuery } from '../../api/vehicles';
import type { Vehicle, VehicleOwnershipType, VehicleStatus } from '../../api/types';
import { vehicleOwnershipTypes, vehicleStatuses } from '../../api/types';
import { exportsApi } from '../../api/exports';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { ExportButton } from '../../components/ExportButton';
import { FindByQrPhotoDialog } from '../../components/FindByQrPhotoDialog';
import { PageHeader } from '../../components/PageHeader';
import { ResourceDataGrid } from '../../components/ResourceDataGrid';
import { RowActions } from '../../components/RowActions';
import { RowPhotoCell } from '../../components/RowPhotoCell';
import { SavedViewsBar } from '../../components/SavedViewsBar';
import { SearchField } from '../../components/SearchField';
import { StatusChip } from '../../components/StatusChip';
import { StatusLegend } from '../../components/StatusLegend';
import { useVehicleRentalsOutSummaryQuery } from '../../features/costs/useCosts';
import { useDeleteVehicle, useVehiclesQuery } from '../../features/vehicles/useVehicles';
import { useDeleteWithConfirm } from '../../hooks/useDeleteWithConfirm';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useI18n, useT } from '../../i18n/useI18n';
import { useListQueryState } from '../../hooks/useListQueryState';
import { useSavedViews } from '../../hooks/useSavedViews';
import { paths } from '../../routes/paths';
import { formatDate, formatMoney, humanizeEnum } from '../../utils/formatting';

interface VehicleViewState {
  search: string;
  filter: VehicleStatus | '';
  sortModel: GridSortModel;
  ownershipFilter: VehicleOwnershipType | '';
  incompleteOnly: boolean;
}

export function VehiclesListPage() {
  const navigate = useNavigate();
  const t = useT();
  const { locale } = useI18n();
  const enumLabel = useEnumLabel();
  const list = useListQueryState<VehicleStatus>('brand');

  const [incompleteOnly, setIncompleteOnly] = useState(false);
  const [ownershipFilter, setOwnershipFilter] = useState<VehicleOwnershipType | ''>('');

  const savedViews = useSavedViews<VehicleViewState>('vehicles');

  const applyView = (state: VehicleViewState) => {
    list.setSearch(state.search);
    list.setFilter(state.filter);
    list.setSortModel(state.sortModel);
    setOwnershipFilter(state.ownershipFilter);
    setIncompleteOnly(state.incompleteOnly);
    list.resetToFirstPage();
  };

  const saveCurrentView = (name: string) => {
    savedViews.saveView(name, {
      search: list.search,
      filter: list.filter,
      sortModel: list.sortModel,
      ownershipFilter,
      incompleteOnly,
    });
  };

  const query: VehicleListQuery = useMemo(
    () => ({
      ...list.query,
      status: list.filter || undefined,
      ownershipType: ownershipFilter || undefined,
      incompleteOnly: incompleteOnly || undefined,
    }),
    [list.query, list.filter, ownershipFilter, incompleteOnly],
  );

  const { data, isLoading, isError, error, refetch } = useVehiclesQuery(query);
  const { data: rentalsOutSummary } = useVehicleRentalsOutSummaryQuery({});
  const remove = useDeleteWithConfirm<Vehicle>(useDeleteVehicle());
  const [qrPhotoOpen, setQrPhotoOpen] = useState(false);

  // Memoized: DataGrid treats a new columns array as a structural change on
  // every render, which is wasted work.
  const columns: GridColDef<Vehicle>[] = useMemo(
    () => [
      {
        field: 'photo',
        headerName: '',
        width: 56,
        sortable: false,
        filterable: false,
        renderCell: (params) => (
          <RowPhotoCell
            ownerType="Vehicle"
            ownerId={params.row.id}
            icon={<LocalShippingOutlined fontSize="small" />}
          />
        ),
      },
      {
        field: 'brand',
        headerName: t('vehicles.vehicle'),
        flex: 1,
        minWidth: 180,
        valueGetter: (_value, row) => `${row.brand} ${row.model}`,
        renderCell: (params) => (
          <Stack direction="row" spacing={0.75} sx={{ alignItems: 'center' }}>
            <span>{params.row.brand} {params.row.model}</span>
            {(!params.row.vin || !params.row.qrCode) && (
              <Tooltip title={t('vehicles.incompleteHint')}>
                <WarningAmberOutlined fontSize="small" color="warning" />
              </Tooltip>
            )}
          </Stack>
        ),
      },
      { field: 'registrationNumber', headerName: t('vehicles.registrationShort'), width: 140 },
      {
        field: 'fuelType',
        headerName: t('vehicles.fuelShort'),
        width: 110,
        valueFormatter: (value: string) => humanizeEnum(value),
      },
      {
        field: 'status',
        headerName: t('vehicles.status'),
        width: 130,
        renderCell: (params) => <StatusChip status={params.value as string} kind="vehicleStatus" />,
      },
      {
        field: 'ownershipType',
        headerName: t('vehicles.ownershipType'),
        width: 120,
        renderCell: (params) => (
          <StatusChip status={params.value as string} kind="vehicleOwnershipType" />
        ),
      },
      {
        field: 'assignedEmployeeName',
        headerName: t('vehicles.assignedTo'),
        flex: 1,
        minWidth: 160,
        // Falls back to the project the same way ToolsListPage's "held by"
        // column does — a vehicle placed directly on a project with nobody
        // holding it otherwise showed a bare dash.
        valueGetter: (_value, row) => row.assignedEmployeeName || row.assignedProjectName || '—',
      },
      {
        field: 'currentRentalOutRenterName',
        headerName: t('vehicles.rentedOutColumn'),
        flex: 1,
        minWidth: 170,
        renderCell: (params) => {
          if (params.row.status === 'RentedOut' && params.row.currentRentalOutRenterName) {
            return (
              <Tooltip
                title={t('vehicles.rentedOutHint', {
                  rate: formatMoney(params.row.currentRentalOutDailyRate, locale),
                  date: formatDate(params.row.currentRentalOutStartDate),
                })}
              >
                <span>{params.row.currentRentalOutRenterName}</span>
              </Tooltip>
            );
          }

          if (params.row.lastRentalOutRenterName) {
            return (
              <Tooltip
                title={t('vehicles.previouslyRentedHint', {
                  date: formatDate(params.row.lastRentalOutEndDate),
                })}
              >
                <Typography variant="caption" color="text.secondary">
                  {t('vehicles.previouslyRented', { name: params.row.lastRentalOutRenterName })}
                </Typography>
              </Tooltip>
            );
          }

          return '—';
        },
      },
      {
        field: 'actions',
        headerName: '',
        width: 130,
        sortable: false,
        filterable: false,
        align: 'right',
        headerAlign: 'right',
        renderCell: (params) => (
          <RowActions>
            <Tooltip title={t('common.view')}>
              <IconButton size="small" onClick={() => navigate(paths.vehicleDetail(params.row.id))}>
                <VisibilityOutlined fontSize="small" />
              </IconButton>
            </Tooltip>
            <Tooltip title={t('common.edit')}>
              <IconButton size="small" onClick={() => navigate(paths.vehicleEdit(params.row.id))}>
                <EditOutlined fontSize="small" />
              </IconButton>
            </Tooltip>
            <Tooltip title={t('common.delete')}>
              <IconButton size="small" onClick={() => remove.request(params.row)}>
                <DeleteOutlined fontSize="small" />
              </IconButton>
            </Tooltip>
          </RowActions>
        ),
      },
    ],
    [navigate, remove, t, locale],
  );

  return (
    <Box>
      <PageHeader
        title={t('vehicles.title')}
        subtitle={data ? t('common.total', { count: data.totalCount }) : undefined}
        action={{
          label: t('vehicles.add'),
          icon: <AddOutlined />,
          onClick: () => navigate(paths.vehicleNew),
        }}
      />

      {rentalsOutSummary && (
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2, mt: -1.5 }}>
          {t('vehicles.totalRentalRevenue')}: {formatMoney(rentalsOutSummary.totalValue, locale)}
        </Typography>
      )}

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ mb: 2 }}>
        <SearchField
          value={list.search}
          onChange={list.setSearch}
          placeholder={t('vehicles.searchPlaceholder')}
        />
        <FormControl size="small" sx={{ minWidth: 180 }}>
          <InputLabel id="vehicle-status-filter-label">{t('vehicles.status')}</InputLabel>
          <Select
            labelId="vehicle-status-filter-label"
            label={t('vehicles.status')}
            value={list.filter}
            onChange={(event) => list.setFilter(event.target.value as VehicleStatus | '')}
          >
            <MenuItem value="">
              <em>{t('common.all')}</em>
            </MenuItem>
            {vehicleStatuses.map((value) => (
              <MenuItem key={value} value={value}>
                {enumLabel('vehicleStatus', value)}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
        <StatusLegend kind="vehicleStatus" values={vehicleStatuses} />

        <FormControl size="small" sx={{ minWidth: 180 }}>
          <InputLabel id="vehicle-ownership-filter-label">{t('vehicles.ownershipType')}</InputLabel>
          <Select
            labelId="vehicle-ownership-filter-label"
            label={t('vehicles.ownershipType')}
            value={ownershipFilter}
            onChange={(event) => setOwnershipFilter(event.target.value as VehicleOwnershipType | '')}
          >
            <MenuItem value="">
              <em>{t('common.all')}</em>
            </MenuItem>
            {vehicleOwnershipTypes.map((value) => (
              <MenuItem key={value} value={value}>
                {enumLabel('vehicleOwnershipType', value)}
              </MenuItem>
            ))}
          </Select>
        </FormControl>

        <FormControlLabel
          control={
            <Switch
              checked={incompleteOnly}
              onChange={(event) => setIncompleteOnly(event.target.checked)}
            />
          }
          label={t('vehicles.incompleteOnly')}
        />

        <Button
          variant="outlined"
          startIcon={<QrCodeScannerOutlined />}
          onClick={() => setQrPhotoOpen(true)}
        >
          {t('qrPhoto.findAction')}
        </Button>

        <ExportButton
          onExport={(language) =>
            exportsApi.vehicles({ search: list.search, status: list.filter, language })
          }
        />
      </Stack>

      <Box sx={{ mb: 2 }}>
        <SavedViewsBar
          views={savedViews.views}
          onApply={applyView}
          onSave={saveCurrentView}
          onDelete={savedViews.deleteView}
        />
      </Box>

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
        onRowClick={(row) =>
          navigate(paths.vehicleDetail(row.id), {
            state: { siblingIds: data?.items.map((item) => item.id) ?? [] },
          })
        }
      />

      <ConfirmDialog
        open={!!remove.pending}
        title={t('vehicles.deleteTitle')}
        description={
          remove.pending
            ? t('vehicles.deleteBody', {
                name: `${remove.pending.brand} ${remove.pending.model} (${remove.pending.registrationNumber})`,
              })
            : ''
        }
        confirmLabel={t('common.delete')}
        destructive
        loading={remove.isDeleting}
        onConfirm={remove.confirm}
        onCancel={remove.cancel}
      />

      {remove.error && (
        <Box sx={{ mt: 1 }}>
          <Typography variant="body2" color="error">
            {remove.error.message}
          </Typography>
        </Box>
      )}

      <FindByQrPhotoDialog open={qrPhotoOpen} onClose={() => setQrPhotoOpen(false)} />
    </Box>
  );
}
