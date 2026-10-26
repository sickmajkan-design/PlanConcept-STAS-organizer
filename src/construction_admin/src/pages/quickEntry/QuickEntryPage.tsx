import { Alert, Box, Button, MenuItem, Paper, Select, Stack, TextField, Typography } from '@mui/material';
import { DataGrid, type GridColDef, type GridRowSelectionModel } from '@mui/x-data-grid';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useMemo, useState } from 'react';

import { toApiError } from '../../api/apiError';
import { employeesApi } from '../../api/employees';
import { toolsApi } from '../../api/tools';
import type { Employee, EmployeeInput, PagedList, Tool, ToolInput, Vehicle, VehicleInput } from '../../api/types';
import { employeeStatuses, employeeTypes, fuelTypes, toolOwnershipTypes, toolStatuses, vehicleOwnershipTypes, vehicleStatuses } from '../../api/types';
import { vehiclesApi } from '../../api/vehicles';
import { PageHeader } from '../../components/PageHeader';
import { useAuth } from '../../auth/useAuth';
import { canViewFinance } from '../../auth/authHelpers';
import { employeeKeys } from '../../features/employees/useEmployees';
import { paths } from '../../routes/paths';
import { useNavigate } from 'react-router-dom';
import { fetchAllPages } from '../../features/dashboard/dashboardData';
import { useBranchFilter } from '../../features/branches/BranchContext';
import { toolKeys } from '../../features/tools/useTools';
import { vehicleKeys } from '../../features/vehicles/useVehicles';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useT } from '../../i18n/useI18n';

type Kind = 'vehicles' | 'tools' | 'employees';
type Row = Record<string, unknown> & { id: string };

const DATE = /^\d{4}-\d{2}-\d{2}$/;
const DATE_FIELDS = new Set([
  'registrationValidUntil',
  'technicalInspectionValidUntil',
  'insuranceValidUntil',
  'nextServiceDue',
  'rentedUntil',
  'employmentDate',
  'dateOfBirth',
]);

const text = (v: unknown) => (typeof v === 'string' && v.trim() !== '' ? v.trim() : null);

function vehicleInput(r: Vehicle): VehicleInput {
  return {
    branchId: r.branchId,
    brand: r.brand,
    model: r.model,
    registrationNumber: r.registrationNumber,
    tdNumber: text(r.tdNumber) ?? '',
    vin: text(r.vin),
    qrCode: text(r.qrCode),
    gpsProvider: text(r.gpsProvider),
    gpsTrackingUrl: text(r.gpsTrackingUrl),
    fuelType: r.fuelType,
    status: r.status,
    ownershipType: r.ownershipType,
    registrationValidUntil: text(r.registrationValidUntil),
    technicalInspectionValidUntil: text(r.technicalInspectionValidUntil),
    insuranceValidUntil: text(r.insuranceValidUntil),
    nextServiceDue: text(r.nextServiceDue),
    rentedUntil: text(r.rentedUntil),
  };
}

function toolInput(r: Tool): ToolInput {
  return {
    branchId: r.branchId,
    name: r.name,
    category: text(r.category),
    serialNumber: text(r.serialNumber),
    qrCode: text(r.qrCode),
    status: r.status,
    ownershipType: r.ownershipType,
  };
}

function employeeInput(r: Employee): EmployeeInput {
  return {
    employeeNumber: r.employeeNumber,
    firstName: r.firstName.trim(),
    lastName: r.lastName.trim(),
    phone: text(r.phone),
    email: text(r.email),
    address: text(r.address),
    dateOfBirth: text(r.dateOfBirth),
    employmentDate: r.employmentDate,
    position: r.position.trim(),
    status: r.status,
    type: r.type,
  };
}

/**
 * Every editable field of the vehicles, the tools or the employees in one table. A cell is changed in place, several rows can take
 * the same value at once, and only the rows that changed are sent when saving.
 */
export function QuickEntryPage({ kind }: { kind: Kind }) {
  const t = useT();
  const enumLabel = useEnumLabel();
  const queryClient = useQueryClient();
  const { branchId } = useBranchFilter();
  const isVehicles = kind === 'vehicles';
  const isEmployees = kind === 'employees';
  const navigate = useNavigate();
  const { user } = useAuth();

  const { data, isLoading } = useQuery({
    queryKey: ['quick-entry', kind, branchId ?? null],
    queryFn: async () => {
      const unit = branchId ? { branchId } : {};
      const load = (pageNumber: number) =>
        (isVehicles
          ? vehiclesApi.list({ pageNumber, pageSize: 100, sortBy: 'brand', ...unit })
          : isEmployees
            ? employeesApi.list({ pageNumber, pageSize: 100, ...unit })
            : toolsApi.list({ pageNumber, pageSize: 100, sortBy: 'name', ...unit })) as unknown as Promise<PagedList<Row>>;
      return (await fetchAllPages(load)).items;
    },
  });

  const [edits, setEdits] = useState<Record<string, Row>>({});
  const [selection, setSelection] = useState<GridRowSelectionModel>({ type: 'include', ids: new Set() });
  const [bulkField, setBulkField] = useState('');
  const [bulkValue, setBulkValue] = useState('');
  const [message, setMessage] = useState<{ severity: 'success' | 'error' | 'info'; text: string } | null>(null);
  const [saving, setSaving] = useState(false);

  const rows = useMemo(() => (data ?? []).map((r) => edits[r.id] ?? r), [data, edits]);

  const select = (field: string, header: string, values: readonly string[], enumKind: Parameters<typeof enumLabel>[0]): GridColDef => ({
    field,
    headerName: header,
    width: 140,
    editable: true,
    type: 'singleSelect',
    valueOptions: values.map((v) => ({ value: v, label: enumLabel(enumKind, v) })),
  });
  const plain = (field: string, header: string, width = 130): GridColDef => ({ field, headerName: header, width, editable: true });
  const date = (field: string, header: string): GridColDef => ({
    field,
    headerName: header,
    width: 150,
    editable: true,
    valueGetter: (v) => v ?? '',
    preProcessEditCellProps: (p) => ({ ...p.props, error: p.props.value !== '' && !DATE.test(String(p.props.value)) }),
  });

  const columns: GridColDef[] = isEmployees
    ? [
        plain('firstName', t('employees.firstName')),
        plain('lastName', t('employees.lastName')),
        plain('position', t('employees.position'), 160),
        select('status', t('employees.status'), employeeStatuses, 'employeeStatus'),
        select('type', t('employees.type'), employeeTypes, 'employeeType'),
        plain('phone', t('employees.phone'), 150),
        plain('email', t('employees.email'), 190),
        plain('address', t('employees.address'), 190),
        date('employmentDate', t('employees.employmentDate')),
        date('dateOfBirth', t('employees.dateOfBirth')),
      ]
    : isVehicles
    ? [
        plain('brand', t('vehicles.brand')),
        plain('model', t('vehicles.model')),
        plain('registrationNumber', t('vehicles.registrationNumber')),
        plain('tdNumber', t('vehicles.tdNumber'), 110),
        plain('vin', t('vehicles.vin'), 170),
        plain('qrCode', t('vehicles.qrCode')),
        select('fuelType', t('vehicles.fuelType'), fuelTypes, 'fuelType'),
        select('status', t('vehicles.status'), vehicleStatuses, 'vehicleStatus'),
        select('ownershipType', t('vehicles.ownershipType'), vehicleOwnershipTypes, 'vehicleOwnershipType'),
        date('registrationValidUntil', t('vehicles.registrationValidUntil')),
        date('technicalInspectionValidUntil', t('vehicles.technicalInspectionValidUntil')),
        date('insuranceValidUntil', t('vehicles.insuranceValidUntil')),
        date('nextServiceDue', t('vehicles.nextServiceDue')),
        plain('gpsProvider', t('vehicles.gpsProvider')),
      ]
    : [
        plain('name', t('tools.name'), 200),
        plain('category', t('tools.category')),
        plain('serialNumber', t('tools.serialNumber'), 160),
        plain('qrCode', t('tools.qrCode')),
        select('status', t('tools.status'), toolStatuses, 'toolStatus'),
        select('ownershipType', t('tools.ownershipType'), toolOwnershipTypes, 'toolOwnershipType'),
      ];

  const selectedIds = (): string[] =>
    selection.type === 'include'
      ? Array.from(selection.ids).map(String)
      : rows.map((r) => r.id).filter((id) => !selection.ids.has(id));

  const bulkColumn = columns.find((c) => c.field === bulkField);
  const bulkOptions = bulkColumn?.type === 'singleSelect' ? ((bulkColumn as { valueOptions: { value: string; label: string }[] }).valueOptions) : null;
  const bulkIsDate = DATE_FIELDS.has(bulkField);
  const bulkInvalid = bulkIsDate && bulkValue !== '' && !DATE.test(bulkValue);

  const applyBulk = () => {
    const ids = selectedIds();
    if (!bulkField || ids.length === 0 || bulkInvalid) return;
    setEdits((prev) => {
      const next = { ...prev };
      for (const r of data ?? []) {
        if (ids.includes(r.id)) next[r.id] = { ...(next[r.id] ?? r), [bulkField]: bulkValue };
      }
      return next;
    });
    setMessage({ severity: 'info', text: t('quickEntry.applied', { count: ids.length }) });
  };

  const save = async () => {
    const ids = Object.keys(edits);
    setSaving(true);
    let done = 0;
    const failed: string[] = [];
    for (const id of ids) {
      try {
        const row = edits[id];
        if (isVehicles) await vehiclesApi.update(id, vehicleInput(row as unknown as Vehicle));
        else if (isEmployees) await employeesApi.update(id, employeeInput(row as unknown as Employee));
        else await toolsApi.update(id, toolInput(row as unknown as Tool));
        done++;
        setEdits((prev) => {
          const { [id]: _saved, ...rest } = prev;
          return rest;
        });
      } catch (error) {
        const row = edits[id];
        failed.push(`${isVehicles ? row.registrationNumber : isEmployees ? row.fullName : row.name}: ${toApiError(error).message}`);
      }
    }
    setSaving(false);
    await queryClient.invalidateQueries({ queryKey: isVehicles ? vehicleKeys.all : isEmployees ? employeeKeys.all : toolKeys.all });
    await queryClient.invalidateQueries({ queryKey: ['quick-entry', kind] });
    setMessage(
      failed.length
        ? { severity: 'error', text: `${t('quickEntry.savedSome', { done, total: ids.length })} ${failed.join(' · ')}` }
        : { severity: 'success', text: t('quickEntry.saved', { count: done }) },
    );
  };

  const changed = Object.keys(edits).length;

  return (
    <Box>
      <PageHeader
        title={isVehicles ? t('quickEntry.vehiclesTitle') : isEmployees ? t('quickEntry.employeesTitle') : t('quickEntry.toolsTitle')}
        description={t('quickEntry.description')}
        secondaryActions={
          isEmployees && canViewFinance(user) ? (
            <Button onClick={() => navigate(paths.employeeRatesQuickEntry)}>{t('quickEntry.tabRates')}</Button>
          ) : undefined
        }
      />

      <Paper variant="outlined" sx={{ p: 1.5, mb: 2 }}>
        <Stack direction="row" spacing={1.5} useFlexGap sx={{ flexWrap: 'wrap', alignItems: 'center' }}>
          <Typography variant="body2" color="text.secondary">
            {t('quickEntry.bulkLabel')}
          </Typography>
          <Select
            size="small"
            displayEmpty
            value={bulkField}
            onChange={(e) => {
              setBulkField(e.target.value);
              setBulkValue('');
            }}
            sx={{ minWidth: 200 }}
            inputProps={{ 'aria-label': t('quickEntry.bulkField') }}
          >
            <MenuItem value="">{t('quickEntry.bulkField')}</MenuItem>
            {columns.map((c) => (
              <MenuItem key={c.field} value={c.field}>
                {c.headerName}
              </MenuItem>
            ))}
          </Select>
          {bulkOptions ? (
            <Select size="small" value={bulkValue} onChange={(e) => setBulkValue(e.target.value)} sx={{ minWidth: 160 }} displayEmpty>
              <MenuItem value="">—</MenuItem>
              {bulkOptions.map((o) => (
                <MenuItem key={o.value} value={o.value}>
                  {o.label}
                </MenuItem>
              ))}
            </Select>
          ) : (
            <TextField
              size="small"
              value={bulkValue}
              onChange={(e) => setBulkValue(e.target.value)}
              placeholder={bulkIsDate ? 'YYYY-MM-DD' : t('quickEntry.bulkValue')}
              error={bulkInvalid}
              disabled={!bulkField}
            />
          )}
          <Button variant="outlined" onClick={applyBulk} disabled={!bulkField || bulkInvalid || selectedIds().length === 0}>
            {t('quickEntry.apply', { count: selectedIds().length })}
          </Button>
        </Stack>
      </Paper>

      {message && (
        <Alert severity={message.severity} sx={{ mb: 2 }} onClose={() => setMessage(null)}>
          {message.text}
        </Alert>
      )}

      <Paper variant="outlined" sx={{ height: 560, mb: 2 }}>
        <DataGrid
          rows={rows}
          columns={columns}
          loading={isLoading}
          checkboxSelection
          disableRowSelectionOnClick
          rowSelectionModel={selection}
          onRowSelectionModelChange={setSelection}
          hideFooter
          processRowUpdate={(next: Row, old: Row) => {
            if (JSON.stringify(next) !== JSON.stringify(old)) setEdits((prev) => ({ ...prev, [next.id]: next }));
            return next;
          }}
          getCellClassName={(p) => (edits[String(p.id)] && edits[String(p.id)][p.field] !== (data ?? []).find((r) => r.id === p.id)?.[p.field] ? 'quick-entry-changed' : '')}
          sx={{ '& .quick-entry-changed': { outline: '2px solid', outlineColor: 'primary.main', outlineOffset: -2 } }}
        />
      </Paper>

      <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center' }}>
        <Button variant="contained" onClick={() => void save()} disabled={changed === 0 || saving}>
          {t('quickEntry.save', { count: changed })}
        </Button>
        <Button onClick={() => setEdits({})} disabled={changed === 0 || saving}>
          {t('quickEntry.undo')}
        </Button>
      </Stack>
    </Box>
  );
}
