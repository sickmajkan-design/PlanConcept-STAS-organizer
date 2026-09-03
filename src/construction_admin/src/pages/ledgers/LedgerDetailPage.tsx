import {
  AddOutlined,
  ArrowDownwardOutlined,
  ArrowUpwardOutlined,
  DeleteOutlined,
  EditOutlined,
} from '@mui/icons-material';
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Grid,
  IconButton,
  MenuItem,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import { useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';

import { toApiError } from '../../api/apiError';
import {
  ledgerColumnDataTypes,
  type LedgerColumn,
  type LedgerColumnDataType,
  type LedgerRow,
  type LedgerSection,
} from '../../api/types';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { ErrorState } from '../../components/ErrorState';
import { useAllEmployeesQuery } from '../../features/employees/useEmployees';
import {
  useAddLedgerColumn,
  useAddLedgerRow,
  useAddLedgerSection,
  useDeleteLedgerColumn,
  useDeleteLedgerRow,
  useDeleteLedgerSection,
  useLedgerQuery,
  useReorderLedgerColumns,
  useReorderLedgerRows,
  useReorderLedgerSections,
  useSetLedgerCell,
  useUpdateLedgerColumn,
  useUpdateLedgerRow,
} from '../../features/ledgers/useLedgers';
import { useAllProjectsQuery } from '../../features/projects/useProjects';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';

const NUMERIC_TYPES: readonly LedgerColumnDataType[] = ['Number', 'Currency'];

function parseNumeric(value: string | null): number {
  if (!value) return 0;
  const cleaned = value.replace(',', '.').trim();
  const parsed = Number(cleaned);
  return Number.isNaN(parsed) ? 0 : parsed;
}

export function LedgerDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const t = useT();

  const { data: ledger, isLoading, isError, error, refetch } = useLedgerQuery(id);
  const ledgerId = ledger?.id ?? '';

  const addColumn = useAddLedgerColumn(ledgerId);
  const updateColumn = useUpdateLedgerColumn(ledgerId);
  const deleteColumn = useDeleteLedgerColumn(ledgerId);
  const reorderColumns = useReorderLedgerColumns(ledgerId);
  const addSection = useAddLedgerSection(ledgerId);
  const reorderSections = useReorderLedgerSections(ledgerId);
  const deleteSection = useDeleteLedgerSection(ledgerId);

  const [addingColumn, setAddingColumn] = useState(false);
  const [editingColumn, setEditingColumn] = useState<LedgerColumn | null>(null);
  const [deletingColumn, setDeletingColumn] = useState<LedgerColumn | null>(null);
  const [addingSection, setAddingSection] = useState(false);
  const [deletingSection, setDeletingSection] = useState<LedgerSection | null>(null);

  if (isLoading) return null;
  if (isError || !ledger) {
    return <ErrorState error={error} onRetry={() => void refetch()} />;
  }

  const sections = [...ledger.sections].sort((a, b) => a.sortOrder - b.sortOrder);
  const columns = [...ledger.columns].sort((a, b) => a.sortOrder - b.sortOrder);

  const moveSection = (index: number, direction: -1 | 1) => {
    const target = index + direction;
    if (target < 0 || target >= sections.length) return;
    const reordered = [...sections];
    [reordered[index], reordered[target]] = [reordered[target], reordered[index]];
    reorderSections.mutate(reordered.map((s) => s.id));
  };

  const moveColumn = (index: number, direction: -1 | 1) => {
    const target = index + direction;
    if (target < 0 || target >= columns.length) return;
    const reordered = [...columns];
    [reordered[index], reordered[target]] = [reordered[target], reordered[index]];
    reorderColumns.mutate(reordered.map((c) => c.id));
  };

  return (
    <Box>
      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        spacing={2}
        sx={{ mb: 3, alignItems: { sm: 'center' }, justifyContent: 'space-between' }}
      >
        <Box>
          <Typography variant="h5" sx={{ fontWeight: 700 }}>
            {ledger.name}
          </Typography>
          {ledger.note && (
            <Typography color="text.secondary" sx={{ mt: 0.5 }}>
              {ledger.note}
            </Typography>
          )}
        </Box>
        <Button onClick={() => navigate(paths.ledgers)}>{t('ledgers.backToList')}</Button>
      </Stack>

      <Stack direction="row" spacing={1} sx={{ mb: 2 }}>
        <Button size="small" startIcon={<AddOutlined />} onClick={() => setAddingColumn(true)}>
          {t('ledgers.addColumn')}
        </Button>
        <Button size="small" startIcon={<AddOutlined />} onClick={() => setAddingSection(true)}>
          {t('ledgers.addSection')}
        </Button>
      </Stack>

      {columns.length === 0 ? (
        <Alert severity="info" sx={{ mb: 2 }}>
          {t('ledgers.noColumnsSentence')}
        </Alert>
      ) : (
        <Stack spacing={3}>
          {sections.map((section, index) => (
            <SectionCard
              key={section.id}
              ledgerId={ledgerId}
              section={section}
              columns={columns}
              onMoveUp={index > 0 ? () => moveSection(index, -1) : undefined}
              onMoveDown={index < sections.length - 1 ? () => moveSection(index, 1) : undefined}
              onMoveColumn={moveColumn}
              onEditColumn={setEditingColumn}
              onDeleteColumn={setDeletingColumn}
              onDeleteSection={() => setDeletingSection(section)}
            />
          ))}
        </Stack>
      )}

      {sections.length === 0 && columns.length > 0 && (
        <Alert severity="info">{t('ledgers.noSectionsSentence')}</Alert>
      )}

      <ColumnDialog
        open={addingColumn}
        onClose={() => setAddingColumn(false)}
        onSubmit={(input) => addColumn.mutate(input, { onSuccess: () => setAddingColumn(false) })}
        loading={addColumn.isPending}
        error={addColumn.isError ? toApiError(addColumn.error) : null}
      />

      <ColumnDialog
        open={!!editingColumn}
        column={editingColumn}
        onClose={() => setEditingColumn(null)}
        onSubmit={(input) => {
          if (!editingColumn) return;
          updateColumn.mutate(
            { columnId: editingColumn.id, input },
            { onSuccess: () => setEditingColumn(null) },
          );
        }}
        loading={updateColumn.isPending}
        error={updateColumn.isError ? toApiError(updateColumn.error) : null}
      />

      <ConfirmDialog
        open={!!deletingColumn}
        title={t('ledgers.deleteColumnTitle')}
        description={t('ledgers.deleteColumnBody')}
        confirmLabel={t('common.delete')}
        destructive
        loading={deleteColumn.isPending}
        onConfirm={async () => {
          if (!deletingColumn) return;
          await deleteColumn.mutateAsync(deletingColumn.id);
          setDeletingColumn(null);
        }}
        onCancel={() => setDeletingColumn(null)}
      />

      <SectionDialog
        open={addingSection}
        onClose={() => setAddingSection(false)}
        onSubmit={(input) => addSection.mutate(input, { onSuccess: () => setAddingSection(false) })}
        loading={addSection.isPending}
        error={addSection.isError ? toApiError(addSection.error) : null}
      />

      <ConfirmDialog
        open={!!deletingSection}
        title={t('ledgers.deleteSectionTitle')}
        description={
          deletingSection ? t('ledgers.deleteSectionBody', { name: deletingSection.name }) : ''
        }
        confirmLabel={t('common.delete')}
        destructive
        loading={deleteSection.isPending}
        onConfirm={async () => {
          if (!deletingSection) return;
          await deleteSection.mutateAsync(deletingSection.id);
          setDeletingSection(null);
        }}
        onCancel={() => setDeletingSection(null)}
      />
    </Box>
  );
}

function SectionCard({
  ledgerId,
  section,
  columns,
  onMoveUp,
  onMoveDown,
  onMoveColumn,
  onEditColumn,
  onDeleteColumn,
  onDeleteSection,
}: {
  ledgerId: string;
  section: LedgerSection;
  columns: LedgerColumn[];
  onMoveUp?: () => void;
  onMoveDown?: () => void;
  onMoveColumn: (index: number, direction: -1 | 1) => void;
  onEditColumn: (column: LedgerColumn) => void;
  onDeleteColumn: (column: LedgerColumn) => void;
  onDeleteSection: () => void;
}) {
  const t = useT();
  const addRow = useAddLedgerRow(ledgerId);
  const reorderRows = useReorderLedgerRows(ledgerId);
  const [addingRow, setAddingRow] = useState(false);

  const rows = [...section.rows].sort((a, b) => a.sortOrder - b.sortOrder);

  const subtotals = useMemo(() => {
    const sums = new Map<string, number>();
    for (const column of columns) {
      if (!NUMERIC_TYPES.includes(column.dataType)) continue;
      let sum = 0;
      for (const row of rows) {
        const cell = row.cells.find((c) => c.columnId === column.id);
        sum += parseNumeric(cell?.value ?? null);
      }
      sums.set(column.id, sum);
    }
    return sums;
  }, [columns, rows]);

  const moveRow = (index: number, direction: -1 | 1) => {
    const target = index + direction;
    if (target < 0 || target >= rows.length) return;
    const reordered = [...rows];
    [reordered[index], reordered[target]] = [reordered[target], reordered[index]];
    reorderRows.mutate({ sectionId: section.id, orderedRowIds: reordered.map((r) => r.id) });
  };

  return (
    <Card variant="outlined">
      <CardContent>
        <Stack direction="row" spacing={1} sx={{ alignItems: 'center', mb: 1 }}>
          <Typography variant="subtitle1" sx={{ fontWeight: 700, flex: 1 }}>
            {section.name}
            {section.projectName && (
              <Typography component="span" color="text.secondary" sx={{ ml: 1, fontWeight: 400 }}>
                ({section.projectName})
              </Typography>
            )}
          </Typography>
          {onMoveUp && (
            <IconButton size="small" onClick={onMoveUp}>
              <ArrowUpwardOutlined fontSize="small" />
            </IconButton>
          )}
          {onMoveDown && (
            <IconButton size="small" onClick={onMoveDown}>
              <ArrowDownwardOutlined fontSize="small" />
            </IconButton>
          )}
          <Tooltip title={t('common.delete')}>
            <IconButton size="small" onClick={onDeleteSection}>
              <DeleteOutlined fontSize="small" />
            </IconButton>
          </Tooltip>
        </Stack>

        <TableContainer sx={{ overflowX: 'auto' }}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell sx={{ minWidth: 180 }}>{t('ledgers.worker')}</TableCell>
                {columns.map((column, columnIndex) => (
                  <TableCell key={column.id} align="right" sx={{ minWidth: 130 }}>
                    <Stack direction="row" spacing={0.25} sx={{ alignItems: 'center', justifyContent: 'flex-end' }}>
                      <span>{column.name}</span>
                      {columnIndex > 0 && (
                        <IconButton size="small" onClick={() => onMoveColumn(columnIndex, -1)}>
                          <ArrowUpwardOutlined sx={{ fontSize: 14 }} />
                        </IconButton>
                      )}
                      {columnIndex < columns.length - 1 && (
                        <IconButton size="small" onClick={() => onMoveColumn(columnIndex, 1)}>
                          <ArrowDownwardOutlined sx={{ fontSize: 14 }} />
                        </IconButton>
                      )}
                      <IconButton size="small" onClick={() => onEditColumn(column)}>
                        <EditOutlined sx={{ fontSize: 14 }} />
                      </IconButton>
                      <IconButton size="small" onClick={() => onDeleteColumn(column)}>
                        <DeleteOutlined sx={{ fontSize: 14 }} />
                      </IconButton>
                    </Stack>
                  </TableCell>
                ))}
                <TableCell align="right" sx={{ width: 90 }} />
              </TableRow>
            </TableHead>
            <TableBody>
              {rows.map((row, index) => (
                <RowLine
                  key={row.id}
                  ledgerId={ledgerId}
                  row={row}
                  columns={columns}
                  onMoveUp={index > 0 ? () => moveRow(index, -1) : undefined}
                  onMoveDown={index < rows.length - 1 ? () => moveRow(index, 1) : undefined}
                />
              ))}
              {rows.length === 0 && (
                <TableRow>
                  <TableCell colSpan={columns.length + 2}>
                    <Typography variant="body2" color="text.secondary">
                      {t('ledgers.noRowsSentence')}
                    </Typography>
                  </TableCell>
                </TableRow>
              )}
              {rows.length > 0 && (
                <TableRow>
                  <TableCell sx={{ fontWeight: 700 }}>{t('ledgers.subtotal')}</TableCell>
                  {columns.map((column) => (
                    <TableCell key={column.id} align="right" sx={{ fontWeight: 700 }}>
                      {subtotals.has(column.id) ? subtotals.get(column.id)!.toFixed(2) : ''}
                    </TableCell>
                  ))}
                  <TableCell />
                </TableRow>
              )}
            </TableBody>
          </Table>
        </TableContainer>

        <Button
          size="small"
          startIcon={<AddOutlined />}
          sx={{ mt: 1 }}
          onClick={() => setAddingRow(true)}
        >
          {t('ledgers.addRow')}
        </Button>
      </CardContent>

      <RowDialog
        open={addingRow}
        onClose={() => setAddingRow(false)}
        onSubmit={(input) =>
          addRow.mutate(
            { sectionId: section.id, input },
            { onSuccess: () => setAddingRow(false) },
          )
        }
        loading={addRow.isPending}
        error={addRow.isError ? toApiError(addRow.error) : null}
      />
    </Card>
  );
}

function RowLine({
  ledgerId,
  row,
  columns,
  onMoveUp,
  onMoveDown,
}: {
  ledgerId: string;
  row: LedgerRow;
  columns: LedgerColumn[];
  onMoveUp?: () => void;
  onMoveDown?: () => void;
}) {
  const t = useT();
  const updateRow = useUpdateLedgerRow(ledgerId);
  const deleteRow = useDeleteLedgerRow(ledgerId);
  const setCell = useSetLedgerCell(ledgerId);
  const [editingRow, setEditingRow] = useState(false);
  const [deleting, setDeleting] = useState(false);

  return (
    <TableRow hover>
      <TableCell>
        <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center' }}>
          <Box sx={{ flex: 1, minWidth: 0 }}>
            <Typography variant="body2" noWrap>
              {row.label}
            </Typography>
            {row.employeeName && (
              <Typography variant="caption" color="text.secondary" noWrap>
                {row.employeeName}
              </Typography>
            )}
          </Box>
          <Tooltip title={t('common.edit')}>
            <IconButton size="small" onClick={() => setEditingRow(true)}>
              <EditOutlined sx={{ fontSize: 14 }} />
            </IconButton>
          </Tooltip>
        </Stack>
      </TableCell>
      {columns.map((column) => (
        <TableCell key={column.id} align="right">
          <CellInput
            value={row.cells.find((c) => c.columnId === column.id)?.value ?? ''}
            dataType={column.dataType}
            onCommit={(value) =>
              setCell.mutate({ rowId: row.id, columnId: column.id, value: value || null })
            }
          />
        </TableCell>
      ))}
      <TableCell align="right">
        <Stack direction="row" spacing={0} sx={{ justifyContent: 'flex-end' }}>
          {onMoveUp && (
            <IconButton size="small" onClick={onMoveUp}>
              <ArrowUpwardOutlined sx={{ fontSize: 14 }} />
            </IconButton>
          )}
          {onMoveDown && (
            <IconButton size="small" onClick={onMoveDown}>
              <ArrowDownwardOutlined sx={{ fontSize: 14 }} />
            </IconButton>
          )}
          <IconButton size="small" onClick={() => setDeleting(true)}>
            <DeleteOutlined sx={{ fontSize: 14 }} />
          </IconButton>
        </Stack>
      </TableCell>

      <RowDialog
        open={editingRow}
        row={row}
        onClose={() => setEditingRow(false)}
        onSubmit={(input) =>
          updateRow.mutate({ rowId: row.id, input }, { onSuccess: () => setEditingRow(false) })
        }
        loading={updateRow.isPending}
        error={updateRow.isError ? toApiError(updateRow.error) : null}
      />

      <ConfirmDialog
        open={deleting}
        title={t('ledgers.deleteRowTitle')}
        description={t('ledgers.deleteRowBody', { name: row.label })}
        confirmLabel={t('common.delete')}
        destructive
        loading={deleteRow.isPending}
        onConfirm={async () => {
          await deleteRow.mutateAsync(row.id);
          setDeleting(false);
        }}
        onCancel={() => setDeleting(false)}
      />
    </TableRow>
  );
}

/** One editable cell — local draft while typing, committed on blur so every keystroke isn't a network call. */
function CellInput({
  value,
  dataType,
  onCommit,
}: {
  value: string;
  dataType: LedgerColumnDataType;
  onCommit: (value: string) => void;
}) {
  const [draft, setDraft] = useState(value);

  useEffect(() => {
    setDraft(value);
  }, [value]);

  const inputType = dataType === 'Number' || dataType === 'Currency' ? 'text' : dataType === 'Date' ? 'date' : 'text';

  return (
    <TextField
      variant="standard"
      size="small"
      type={inputType}
      value={draft}
      onChange={(event) => setDraft(event.target.value)}
      onBlur={() => {
        if (draft !== value) onCommit(draft);
      }}
      slotProps={{ htmlInput: { style: { textAlign: 'right' } } }}
      sx={{ minWidth: 90 }}
    />
  );
}

function ColumnDialog({
  open,
  column,
  onClose,
  onSubmit,
  loading,
  error,
}: {
  open: boolean;
  column?: LedgerColumn | null;
  onClose: () => void;
  onSubmit: (input: { name: string; dataType: LedgerColumnDataType }) => void;
  loading: boolean;
  error: { message: string } | null;
}) {
  const t = useT();
  const enumLabel = useEnumLabel();
  const isEditing = !!column;
  const [name, setName] = useState('');
  const [dataType, setDataType] = useState<LedgerColumnDataType>('Number');

  useEffect(() => {
    if (!open) return;
    setName(column?.name ?? '');
    setDataType(column?.dataType ?? 'Number');
  }, [open, column]);

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="xs">
      <DialogTitle>{isEditing ? t('ledgers.editColumnTitle') : t('ledgers.addColumn')}</DialogTitle>
      <DialogContent>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error.message}
          </Alert>
        )}
        <Grid container spacing={2} sx={{ mt: 0 }}>
          <Grid size={12}>
            <TextField
              fullWidth
              label={t('ledgers.columnName')}
              value={name}
              onChange={(event) => setName(event.target.value)}
            />
          </Grid>
          <Grid size={12}>
            <TextField
              select
              fullWidth
              label={t('ledgers.columnType')}
              value={dataType}
              onChange={(event) => setDataType(event.target.value as LedgerColumnDataType)}
            >
              {ledgerColumnDataTypes.map((value) => (
                <MenuItem key={value} value={value}>
                  {enumLabel('ledgerColumnDataType', value)}
                </MenuItem>
              ))}
            </TextField>
          </Grid>
        </Grid>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button
          variant="contained"
          disabled={name.trim() === ''}
          loading={loading}
          onClick={() => onSubmit({ name: name.trim(), dataType })}
        >
          {t('common.save')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

function SectionDialog({
  open,
  onClose,
  onSubmit,
  loading,
  error,
}: {
  open: boolean;
  onClose: () => void;
  onSubmit: (input: { name: string; projectId?: string | null }) => void;
  loading: boolean;
  error: { message: string } | null;
}) {
  const t = useT();
  const { data: projects } = useAllProjectsQuery();
  const [name, setName] = useState('');
  const [projectId, setProjectId] = useState('');

  useEffect(() => {
    if (!open) return;
    setName('');
    setProjectId('');
  }, [open]);

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="xs">
      <DialogTitle>{t('ledgers.addSection')}</DialogTitle>
      <DialogContent>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error.message}
          </Alert>
        )}
        <Grid container spacing={2} sx={{ mt: 0 }}>
          <Grid size={12}>
            <TextField
              fullWidth
              label={t('ledgers.sectionName')}
              value={name}
              onChange={(event) => setName(event.target.value)}
            />
          </Grid>
          <Grid size={12}>
            <TextField
              select
              fullWidth
              label={t('ledgers.linkedProject')}
              value={projectId}
              onChange={(event) => setProjectId(event.target.value)}
              helperText={t('generalExpenses.optionalHint')}
            >
              <MenuItem value="">
                <em>{t('common.none')}</em>
              </MenuItem>
              {(projects?.items ?? []).map((project) => (
                <MenuItem key={project.id} value={project.id}>
                  {project.name}
                </MenuItem>
              ))}
            </TextField>
          </Grid>
        </Grid>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button
          variant="contained"
          disabled={name.trim() === ''}
          loading={loading}
          onClick={() => onSubmit({ name: name.trim(), projectId: projectId || null })}
        >
          {t('common.create')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

function RowDialog({
  open,
  row,
  onClose,
  onSubmit,
  loading,
  error,
}: {
  open: boolean;
  row?: LedgerRow | null;
  onClose: () => void;
  onSubmit: (input: { label: string; employeeId?: string | null }) => void;
  loading: boolean;
  error: { message: string } | null;
}) {
  const t = useT();
  const isEditing = !!row;
  const { data: employees } = useAllEmployeesQuery();
  const [label, setLabel] = useState('');
  const [employeeId, setEmployeeId] = useState('');

  useEffect(() => {
    if (!open) return;
    setLabel(row?.label ?? '');
    setEmployeeId(row?.employeeId ?? '');
  }, [open, row]);

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="xs">
      <DialogTitle>{isEditing ? t('ledgers.editRowTitle') : t('ledgers.addRow')}</DialogTitle>
      <DialogContent>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error.message}
          </Alert>
        )}
        <Grid container spacing={2} sx={{ mt: 0 }}>
          <Grid size={12}>
            <TextField
              fullWidth
              label={t('ledgers.rowLabel')}
              value={label}
              onChange={(event) => setLabel(event.target.value)}
            />
          </Grid>
          <Grid size={12}>
            <TextField
              select
              fullWidth
              label={t('ledgers.linkedEmployee')}
              value={employeeId}
              onChange={(event) => setEmployeeId(event.target.value)}
              helperText={t('generalExpenses.optionalHint')}
            >
              <MenuItem value="">
                <em>{t('common.none')}</em>
              </MenuItem>
              {(employees?.items ?? []).map((employee) => (
                <MenuItem key={employee.id} value={employee.id}>
                  {employee.firstName} {employee.lastName}
                </MenuItem>
              ))}
            </TextField>
          </Grid>
        </Grid>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button
          variant="contained"
          disabled={label.trim() === ''}
          loading={loading}
          onClick={() => onSubmit({ label: label.trim(), employeeId: employeeId || null })}
        >
          {t('common.save')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
