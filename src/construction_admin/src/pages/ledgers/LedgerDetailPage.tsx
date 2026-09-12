import {
  AddOutlined,
  ArrowDownwardOutlined,
  ArrowUpwardOutlined,
  DeleteOutlined,
  EditOutlined,
  ExpandLessOutlined,
  ExpandMoreOutlined,
  HistoryOutlined,
  LinkOffOutlined,
  LockOutlined,
  ReceiptLongOutlined,
  WarningAmberOutlined,
} from '@mui/icons-material';
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Collapse,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Grid,
  IconButton,
  Link,
  List,
  ListItem,
  ListItemText,
  MenuItem,
  Paper,
  Popover,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  ToggleButton,
  ToggleButtonGroup,
  Tooltip,
  Typography,
} from '@mui/material';
import { useEffect, useMemo, useState } from 'react';
import { Link as RouterLink, useNavigate, useParams } from 'react-router-dom';

import { toApiError } from '../../api/apiError';
import {
  generalExpenseCategories,
  ledgerColumnDataTypes,
  ledgerColumnSourceMetrics,
  type Accommodation,
  type Employee,
  type GeneralExpenseCategory,
  type LedgerColumn,
  type LedgerPromotion,
  type LedgerColumnDataType,
  type LedgerColumnSourceMetric,
  type LedgerRow,
  type LedgerSection,
  type LedgerSummaryBox,
  type Material,
  type Project,
  type Tool,
  type Vehicle,
} from '../../api/types';
import { AuditHistoryCard } from '../../components/AuditHistoryCard';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { ErrorState } from '../../components/ErrorState';
import { useAllAccommodationsQuery } from '../../features/accommodations/useAccommodations';
import { useAllEmployeesQuery } from '../../features/employees/useEmployees';
import {
  useAddLedgerColumn,
  useAddLedgerRow,
  useAddLedgerSection,
  useAddLedgerSummaryBox,
  useDeleteLedgerColumn,
  useDeleteLedgerRow,
  useDeleteLedgerSection,
  useDeleteLedgerSummaryBox,
  useLedgerQuery,
  useLedgerSectionRowsQuery,
  useLedgerPromotionsQuery,
  useLedgerSummaryQuery,
  useLedgerUnlinkedRowsQuery,
  usePromoteLedgerRowToAccommodationRate,
  usePromoteLedgerRowToGeneralExpense,
  useReorderLedgerColumns,
  useReorderLedgerRows,
  useReorderLedgerSections,
  useReorderLedgerSummaryBoxes,
  useSetLedgerCell,
  useSetLedgerCellColor,
  useSetLedgerRowColor,
  useUpdateLedgerColumn,
  useUpdateLedgerRow,
  useUpdateLedgerSummaryBox,
} from '../../features/ledgers/useLedgers';
import { useAllMaterialsQuery } from '../../features/materials/useMaterials';
import { useAllProjectsQuery } from '../../features/projects/useProjects';
import { useAllToolsQuery } from '../../features/tools/useTools';
import { useAllVehiclesQuery } from '../../features/vehicles/useVehicles';
import { useEnumLabel } from '../../i18n/enumLabels';
import type { MessageKey } from '../../i18n/en';
import { useI18n, useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';
import { formatDate, formatMoney } from '../../utils/formatting';

const NUMERIC_TYPES: readonly LedgerColumnDataType[] = ['Number', 'Currency'];

function parseNumeric(value: string | null): number {
  if (!value) return 0;
  const cleaned = value.replace(',', '.').trim();
  const parsed = Number(cleaned);
  return Number.isNaN(parsed) ? 0 : parsed;
}

const amountFormatter = new Intl.NumberFormat('sr-RS', {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

function formatAmount(value: number): string {
  return amountFormatter.format(value);
}

const BOX_COLOR_PALETTE = [
  '#FBD98A', // yellow
  '#F2B27E', // orange
  '#B7E4A0', // green
  '#9ECBE8', // blue
  '#E8A0A0', // red
  '#CBB2E8', // purple
  '#D6D6D6', // grey
];

const DEFAULT_BOX_COLOR = BOX_COLOR_PALETTE[1];
const NET_BOX_COLOR = BOX_COLOR_PALETTE[2];

export function LedgerDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const t = useT();
  const enumLabel = useEnumLabel();

  const { data: ledger, isLoading, isError, error, refetch } = useLedgerQuery(id);
  const ledgerId = ledger?.id ?? '';

  // Every mutation the grid needs, called exactly once for the whole page —
  // not per row or per section. With over a hundred rows in a real month,
  // one hook instance per row (each carrying its own React Query
  // subscription) was what froze the tab; the fix is fewer, shared
  // instances whose `.mutate` calls simply carry the target id in their
  // variables instead.
  const addColumn = useAddLedgerColumn(ledgerId);
  const updateColumn = useUpdateLedgerColumn(ledgerId);
  const deleteColumn = useDeleteLedgerColumn(ledgerId);
  const reorderColumns = useReorderLedgerColumns(ledgerId);
  const addSection = useAddLedgerSection(ledgerId);
  const reorderSections = useReorderLedgerSections(ledgerId);
  const deleteSection = useDeleteLedgerSection(ledgerId);
  const addRow = useAddLedgerRow(ledgerId);
  const updateRow = useUpdateLedgerRow(ledgerId);
  const deleteRow = useDeleteLedgerRow(ledgerId);
  const reorderRows = useReorderLedgerRows(ledgerId);
  const setCell = useSetLedgerCell(ledgerId);
  const setRowColor = useSetLedgerRowColor(ledgerId);
  const setCellColor = useSetLedgerCellColor(ledgerId);
  const addSummaryBox = useAddLedgerSummaryBox(ledgerId);
  const updateSummaryBox = useUpdateLedgerSummaryBox(ledgerId);
  const deleteSummaryBox = useDeleteLedgerSummaryBox(ledgerId);
  const reorderSummaryBoxes = useReorderLedgerSummaryBoxes(ledgerId);
  const promoteToGeneralExpense = usePromoteLedgerRowToGeneralExpense(ledgerId);
  const promoteToAccommodationRate = usePromoteLedgerRowToAccommodationRate(ledgerId);

  const { data: summary } = useLedgerSummaryQuery(ledgerId);
  const { data: promotions } = useLedgerPromotionsQuery(ledgerId);

  const { data: projectsPage } = useAllProjectsQuery();
  const { data: employeesPage } = useAllEmployeesQuery();
  const { data: vehiclesPage } = useAllVehiclesQuery();
  const { data: toolsPage } = useAllToolsQuery();
  const { data: materialsPage } = useAllMaterialsQuery();
  const { data: accommodationsPage } = useAllAccommodationsQuery();
  const projects = projectsPage?.items ?? [];
  const employees = employeesPage?.items ?? [];
  const vehicles = vehiclesPage?.items ?? [];
  const tools = toolsPage?.items ?? [];
  const materials = materialsPage?.items ?? [];
  const accommodations = accommodationsPage?.items ?? [];

  const [addingColumn, setAddingColumn] = useState(false);
  const [editingColumn, setEditingColumn] = useState<LedgerColumn | null>(null);
  const [deletingColumn, setDeletingColumn] = useState<LedgerColumn | null>(null);
  const [addingSection, setAddingSection] = useState(false);
  const [deletingSection, setDeletingSection] = useState<LedgerSection | null>(null);
  const [addingRowToSectionId, setAddingRowToSectionId] = useState<string | null>(null);
  const [deletingRow, setDeletingRow] = useState<LedgerRow | null>(null);
  const [promotingRow, setPromotingRow] = useState<LedgerRow | null>(null);
  const [addingSummaryBox, setAddingSummaryBox] = useState(false);
  const [editingSummaryBox, setEditingSummaryBox] = useState<LedgerSummaryBox | null>(null);
  const [deletingSummaryBox, setDeletingSummaryBox] = useState<LedgerSummaryBox | null>(null);
  // Collapsed by default — a section's row table only mounts once opened, so
  // the page never has to render every cell of every section at once.
  const [expandedSectionIds, setExpandedSectionIds] = useState<Set<string>>(new Set());
  // Column management is occasional, not the thing read every visit — start
  // collapsed so the page reads Summary, then straight into the data.
  const [columnsExpanded, setColumnsExpanded] = useState(false);
  // The panel's own query is disabled until this is true — opening it is a
  // deliberate choice to pay for one small ledger-wide query, never an
  // accidental one.
  const [unlinkedPanelOpen, setUnlinkedPanelOpen] = useState(false);

  const sections = useMemo(
    () => (ledger ? [...ledger.sections].sort((a, b) => a.sortOrder - b.sortOrder) : []),
    [ledger],
  );
  const columns = useMemo(
    () => (ledger ? [...ledger.columns].sort((a, b) => a.sortOrder - b.sortOrder) : []),
    [ledger],
  );

  if (isLoading) return null;
  if (isError || !ledger) {
    return <ErrorState error={error} onRetry={() => void refetch()} />;
  }

  const toggleSection = (sectionId: string) => {
    setExpandedSectionIds((prev) => {
      const next = new Set(prev);
      if (next.has(sectionId)) {
        next.delete(sectionId);
      } else {
        next.add(sectionId);
      }
      return next;
    });
  };

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

  const moveSummaryBox = (index: number, direction: -1 | 1) => {
    const boxes = summary?.boxes ?? [];
    const target = index + direction;
    if (target < 0 || target >= boxes.length) return;
    const reordered = [...boxes];
    [reordered[index], reordered[target]] = [reordered[target], reordered[index]];
    reorderSummaryBoxes.mutate(reordered.map((b) => b.id));
  };

  const addingRowSection = sections.find((s) => s.id === addingRowToSectionId) ?? null;

  return (
    <Box sx={{ maxWidth: 1100, mx: 'auto' }}>
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

      <Paper variant="outlined" sx={{ p: 2, mb: 3 }}>
        <Stack direction="row" sx={{ alignItems: 'center', justifyContent: 'space-between', mb: 1 }}>
          <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
            {t('ledgers.summaryHeading')}
          </Typography>
          <Button size="small" startIcon={<AddOutlined />} onClick={() => setAddingSummaryBox(true)}>
            {t('ledgers.addSummaryBox')}
          </Button>
        </Stack>

        {!summary || summary.boxes.length === 0 ? (
          <Typography color="text.secondary" sx={{ textAlign: 'center', py: 2 }}>
            {t('ledgers.noSummaryBoxesSentence')}
          </Typography>
        ) : (
          <Box
            sx={{
              display: 'flex',
              flexWrap: 'wrap',
              justifyContent: 'center',
              gap: 1.5,
            }}
          >
            {summary.boxes.map((box, index) => (
              <SummaryBoxCard
                key={box.id}
                box={box}
                onEdit={() => setEditingSummaryBox(box)}
                onDelete={() => setDeletingSummaryBox(box)}
                onMoveUp={index > 0 ? () => moveSummaryBox(index, -1) : undefined}
                onMoveDown={
                  index < summary.boxes.length - 1 ? () => moveSummaryBox(index, 1) : undefined
                }
              />
            ))}
            <NetTotalCard value={summary.netTotal} />
          </Box>
        )}
      </Paper>

      {promotions && promotions.length > 0 && <PromotionsPanel promotions={promotions} />}

      <Paper variant="outlined" sx={{ p: 2, mb: 3 }}>
        <Stack
          direction="row"
          onClick={() => setColumnsExpanded((v) => !v)}
          sx={{ alignItems: 'center', justifyContent: 'space-between', cursor: 'pointer' }}
        >
          <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
            <IconButton
              size="small"
              onClick={(event) => {
                event.stopPropagation();
                setColumnsExpanded((v) => !v);
              }}
            >
              {columnsExpanded ? (
                <ExpandLessOutlined fontSize="small" />
              ) : (
                <ExpandMoreOutlined fontSize="small" />
              )}
            </IconButton>
            <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
              {t('ledgers.columnsHeading')}
            </Typography>
            <Chip size="small" variant="outlined" label={columns.length} />
          </Stack>
          <Button
            size="small"
            startIcon={<AddOutlined />}
            onClick={(event) => {
              event.stopPropagation();
              setAddingColumn(true);
            }}
          >
            {t('ledgers.addColumn')}
          </Button>
        </Stack>

        <Collapse in={columnsExpanded} timeout="auto" unmountOnExit>
        {columns.length === 0 ? (
          <Typography color="text.secondary" sx={{ mt: 1 }}>{t('ledgers.noColumnsSentence')}</Typography>
        ) : (
          <List dense disablePadding sx={{ mt: 1 }}>
            {columns.map((column, index) => (
              <ListItem
                key={column.id}
                disableGutters
                secondaryAction={
                  <Stack direction="row" spacing={0}>
                    {index > 0 && (
                      <IconButton size="small" onClick={() => moveColumn(index, -1)}>
                        <ArrowUpwardOutlined fontSize="small" />
                      </IconButton>
                    )}
                    {index < columns.length - 1 && (
                      <IconButton size="small" onClick={() => moveColumn(index, 1)}>
                        <ArrowDownwardOutlined fontSize="small" />
                      </IconButton>
                    )}
                    <IconButton size="small" onClick={() => setEditingColumn(column)}>
                      <EditOutlined fontSize="small" />
                    </IconButton>
                    <IconButton size="small" onClick={() => setDeletingColumn(column)}>
                      <DeleteOutlined fontSize="small" />
                    </IconButton>
                  </Stack>
                }
              >
                <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                  <Typography variant="body2">{column.name}</Typography>
                  <Chip size="small" variant="outlined" label={enumLabel('ledgerColumnDataType', column.dataType)} />
                </Stack>
              </ListItem>
            ))}
          </List>
        )}
        </Collapse>
      </Paper>

      <Stack direction="row" spacing={1} sx={{ mb: 2, alignItems: 'center' }}>
        <Button size="small" startIcon={<AddOutlined />} onClick={() => setAddingSection(true)}>
          {t('ledgers.addSection')}
        </Button>
        <Button
          size="small"
          variant={unlinkedPanelOpen ? 'contained' : 'text'}
          startIcon={<LinkOffOutlined />}
          onClick={() => setUnlinkedPanelOpen((v) => !v)}
        >
          {t('ledgers.showUnlinkedOnly')}
        </Button>
      </Stack>

      {unlinkedPanelOpen && (
        <UnlinkedRowsPanel
          ledgerId={ledgerId}
          onOpenSection={(sectionId) =>
            setExpandedSectionIds((prev) => new Set(prev).add(sectionId))
          }
        />
      )}

      {columns.length === 0 ? (
        <Alert severity="info">{t('ledgers.noColumnsSentence')}</Alert>
      ) : sections.length === 0 ? (
        <Alert severity="info">{t('ledgers.noSectionsSentence')}</Alert>
      ) : (
        <Stack spacing={1.5}>
          {sections.map((section, index) => (
            <SectionCard
              key={section.id}
              ledgerId={ledgerId}
              section={section}
              columns={columns}
              expanded={expandedSectionIds.has(section.id)}
              onToggle={() => toggleSection(section.id)}
              onMoveUp={index > 0 ? () => moveSection(index, -1) : undefined}
              onMoveDown={index < sections.length - 1 ? () => moveSection(index, 1) : undefined}
              onDeleteSection={() => setDeletingSection(section)}
              onAddRow={() => setAddingRowToSectionId(section.id)}
              onCommitLabel={(rowId, label) => updateRow.mutate({ rowId, input: { label } })}
              onDeleteRow={(row) => setDeletingRow(row)}
              onPromoteRow={(row) => setPromotingRow(row)}
              onSetCell={(rowId, columnId, value) =>
                setCell.mutate({ rowId, columnId, value: value || null })
              }
              onSetRowColor={(rowId, color) => setRowColor.mutate({ rowId, color })}
              onSetCellColor={(rowId, columnId, color) =>
                setCellColor.mutate({ rowId, columnId, color })
              }
              onReorderRows={(orderedRowIds) =>
                reorderRows.mutate({ sectionId: section.id, orderedRowIds })
              }
            />
          ))}
        </Stack>
      )}

      <SummaryBoxDialog
        open={addingSummaryBox}
        columns={columns}
        onClose={() => setAddingSummaryBox(false)}
        onSubmit={(input) =>
          addSummaryBox.mutate(input, { onSuccess: () => setAddingSummaryBox(false) })
        }
        loading={addSummaryBox.isPending}
        error={addSummaryBox.isError ? toApiError(addSummaryBox.error) : null}
      />

      <SummaryBoxDialog
        open={!!editingSummaryBox}
        box={editingSummaryBox}
        columns={columns}
        onClose={() => setEditingSummaryBox(null)}
        onSubmit={(input) => {
          if (!editingSummaryBox) return;
          updateSummaryBox.mutate(
            { boxId: editingSummaryBox.id, input },
            { onSuccess: () => setEditingSummaryBox(null) },
          );
        }}
        loading={updateSummaryBox.isPending}
        error={updateSummaryBox.isError ? toApiError(updateSummaryBox.error) : null}
      />

      <ConfirmDialog
        open={!!deletingSummaryBox}
        title={t('ledgers.deleteSummaryBoxTitle')}
        description={
          deletingSummaryBox ? t('ledgers.deleteSummaryBoxBody', { name: deletingSummaryBox.label }) : ''
        }
        confirmLabel={t('common.delete')}
        destructive
        loading={deleteSummaryBox.isPending}
        onConfirm={async () => {
          if (!deletingSummaryBox) return;
          await deleteSummaryBox.mutateAsync(deletingSummaryBox.id);
          setDeletingSummaryBox(null);
        }}
        onCancel={() => setDeletingSummaryBox(null)}
      />

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
        projects={projects}
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

      <RowDialog
        open={!!addingRowSection}
        employees={employees}
        vehicles={vehicles}
        tools={tools}
        materials={materials}
        onClose={() => setAddingRowToSectionId(null)}
        onSubmit={(input) => {
          if (!addingRowSection) return;
          addRow.mutate(
            { sectionId: addingRowSection.id, input },
            { onSuccess: () => setAddingRowToSectionId(null) },
          );
        }}
        loading={addRow.isPending}
        error={addRow.isError ? toApiError(addRow.error) : null}
      />

      <PromoteRowDialog
        open={!!promotingRow}
        row={promotingRow}
        columns={columns}
        projects={projects}
        employees={employees}
        accommodations={accommodations}
        onClose={() => setPromotingRow(null)}
        onSubmitGeneralExpense={(input) => {
          if (!promotingRow) return;
          promoteToGeneralExpense.mutate(
            { rowId: promotingRow.id, input },
            { onSuccess: () => setPromotingRow(null) },
          );
        }}
        onSubmitAccommodationRate={(input) => {
          if (!promotingRow) return;
          promoteToAccommodationRate.mutate(
            { rowId: promotingRow.id, input },
            { onSuccess: () => setPromotingRow(null) },
          );
        }}
        loading={promoteToGeneralExpense.isPending || promoteToAccommodationRate.isPending}
        error={
          promoteToGeneralExpense.isError
            ? toApiError(promoteToGeneralExpense.error)
            : promoteToAccommodationRate.isError
              ? toApiError(promoteToAccommodationRate.error)
              : null
        }
      />

      <ConfirmDialog
        open={!!deletingRow}
        title={t('ledgers.deleteRowTitle')}
        description={deletingRow ? t('ledgers.deleteRowBody', { name: deletingRow.label }) : ''}
        confirmLabel={t('common.delete')}
        destructive
        loading={deleteRow.isPending}
        onConfirm={async () => {
          if (!deletingRow) return;
          await deleteRow.mutateAsync(deletingRow.id);
          setDeletingRow(null);
        }}
        onCancel={() => setDeletingRow(null)}
      />
    </Box>
  );
}

/**
 * One section, collapsed by default. Its row table only exists in the DOM
 * while expanded — with well over a hundred rows in a real month, mounting
 * every section's table at once (thousands of input cells) is what froze
 * the tab; `unmountOnExit` is what stops that.
 */
function SectionCard({
  ledgerId,
  section,
  columns,
  expanded,
  onToggle,
  onMoveUp,
  onMoveDown,
  onDeleteSection,
  onAddRow,
  onCommitLabel,
  onDeleteRow,
  onPromoteRow,
  onSetCell,
  onSetRowColor,
  onSetCellColor,
  onReorderRows,
}: {
  ledgerId: string;
  section: LedgerSection;
  columns: LedgerColumn[];
  expanded: boolean;
  onToggle: () => void;
  onMoveUp?: () => void;
  onMoveDown?: () => void;
  onDeleteSection: () => void;
  onAddRow: () => void;
  onCommitLabel: (rowId: string, label: string) => void;
  onDeleteRow: (row: LedgerRow) => void;
  onPromoteRow: (row: LedgerRow) => void;
  onSetCell: (rowId: string, columnId: string, value: string) => void;
  onSetRowColor: (rowId: string, color: string | null) => void;
  onSetCellColor: (rowId: string, columnId: string, color: string | null) => void;
  onReorderRows: (orderedRowIds: string[]) => void;
}) {
  const t = useT();

  // Fetched only while this section is open — a real month's worth of rows
  // across every section at once is what used to freeze the page.
  const { data: loadedSection, isLoading } = useLedgerSectionRowsQuery(
    ledgerId,
    section.id,
    expanded,
  );

  const rows = useMemo(
    () => (loadedSection ? [...loadedSection.rows].sort((a, b) => a.sortOrder - b.sortOrder) : []),
    [loadedSection],
  );

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
    onReorderRows(reordered.map((r) => r.id));
  };

  return (
    <Card variant="outlined">
      <Stack
        direction="row"
        spacing={1}
        sx={{ alignItems: 'center', px: 2, py: 1, cursor: 'pointer' }}
        onClick={onToggle}
      >
        <IconButton size="small" onClick={(event) => { event.stopPropagation(); onToggle(); }}>
          {expanded ? <ExpandLessOutlined fontSize="small" /> : <ExpandMoreOutlined fontSize="small" />}
        </IconButton>
        <Typography variant="subtitle1" sx={{ fontWeight: 700, flex: 1 }}>
          {section.name}
          {section.projectName && (
            <Typography component="span" color="text.secondary" sx={{ ml: 1, fontWeight: 400 }}>
              ({section.projectName})
            </Typography>
          )}
        </Typography>
        <Chip size="small" variant="outlined" label={t('ledgers.rowCount', { count: section.rowCount })} />
        {onMoveUp && (
          <IconButton size="small" onClick={(event) => { event.stopPropagation(); onMoveUp(); }}>
            <ArrowUpwardOutlined fontSize="small" />
          </IconButton>
        )}
        {onMoveDown && (
          <IconButton size="small" onClick={(event) => { event.stopPropagation(); onMoveDown(); }}>
            <ArrowDownwardOutlined fontSize="small" />
          </IconButton>
        )}
        <Tooltip title={t('common.delete')}>
          <IconButton size="small" onClick={(event) => { event.stopPropagation(); onDeleteSection(); }}>
            <DeleteOutlined fontSize="small" />
          </IconButton>
        </Tooltip>
      </Stack>

      <Collapse in={expanded} timeout="auto" unmountOnExit>
        <CardContent sx={{ pt: 0 }}>
          {isLoading ? (
            <Typography color="text.secondary" sx={{ py: 2 }}>
              {t('common.loading')}
            </Typography>
          ) : (
          <TableContainer sx={{ overflowX: 'auto' }}>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell sx={{ minWidth: 180 }}>{t('ledgers.worker')}</TableCell>
                  {columns.map((column) => (
                    <TableCell key={column.id} align="right" sx={{ minWidth: 100 }}>
                      {column.name}
                    </TableCell>
                  ))}
                  <TableCell align="right" sx={{ width: 90 }} />
                </TableRow>
              </TableHead>
              <TableBody>
                {rows.map((row, index) => (
                  <RowLine
                    key={row.id}
                    row={row}
                    columns={columns}
                    onCommitLabel={(label) => onCommitLabel(row.id, label)}
                    onDelete={() => onDeleteRow(row)}
                    onPromote={() => onPromoteRow(row)}
                    onSetCell={(columnId, value) => onSetCell(row.id, columnId, value)}
                    onSetRowColor={(color) => onSetRowColor(row.id, color)}
                    onSetCellColor={(columnId, color) => onSetCellColor(row.id, columnId, color)}
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
          )}

          <Button size="small" startIcon={<AddOutlined />} sx={{ mt: 1 }} onClick={onAddRow}>
            {t('ledgers.addRow')}
          </Button>
        </CardContent>
      </Collapse>
    </Card>
  );
}

/** One colored box in the month-summary panel — a live number, not an input. */
function SummaryBoxCard({
  box,
  onEdit,
  onDelete,
  onMoveUp,
  onMoveDown,
}: {
  box: LedgerSummaryBox;
  onEdit: () => void;
  onDelete: () => void;
  onMoveUp?: () => void;
  onMoveDown?: () => void;
}) {
  const t = useT();

  return (
    <Paper
      elevation={0}
      sx={{
        position: 'relative',
        p: 1.75,
        pt: 2.25,
        width: 176,
        borderRadius: 2,
        textAlign: 'center',
        bgcolor: box.color ?? DEFAULT_BOX_COLOR,
        boxShadow: '0 1px 3px rgba(0,0,0,0.15)',
        transition: 'box-shadow 0.15s, transform 0.15s',
        '&:hover': { boxShadow: '0 4px 10px rgba(0,0,0,0.2)', transform: 'translateY(-1px)' },
        '&:hover .box-actions': { opacity: 1 },
      }}
    >
      <Stack
        direction="row"
        spacing={0}
        className="box-actions"
        sx={{
          position: 'absolute',
          top: 2,
          right: 2,
          opacity: 0,
          transition: 'opacity 0.15s',
          bgcolor: 'rgba(255,255,255,0.55)',
          borderRadius: 1,
        }}
      >
        {onMoveUp && (
          <IconButton size="small" onClick={onMoveUp} sx={{ p: 0.25 }}>
            <ArrowUpwardOutlined sx={{ fontSize: 14 }} />
          </IconButton>
        )}
        {onMoveDown && (
          <IconButton size="small" onClick={onMoveDown} sx={{ p: 0.25 }}>
            <ArrowDownwardOutlined sx={{ fontSize: 14 }} />
          </IconButton>
        )}
        <IconButton size="small" onClick={onEdit} sx={{ p: 0.25 }}>
          <EditOutlined sx={{ fontSize: 14 }} />
        </IconButton>
        <IconButton size="small" onClick={onDelete} sx={{ p: 0.25 }}>
          <DeleteOutlined sx={{ fontSize: 14 }} />
        </IconButton>
      </Stack>

      <Typography
        variant="caption"
        sx={{
          display: 'block',
          fontWeight: 700,
          color: 'rgba(0,0,0,0.7)',
          textTransform: 'uppercase',
          letterSpacing: 0.4,
        }}
      >
        {box.label}
      </Typography>
      <Typography variant="h6" sx={{ fontWeight: 700, color: 'rgba(0,0,0,0.87)', mt: 0.5 }}>
        {formatAmount(box.value)}
      </Typography>
      <Typography variant="caption" sx={{ display: 'block', color: 'rgba(0,0,0,0.6)' }}>
        {box.sourceColumnName
          ? t('ledgers.summaryBoxFromColumn', { column: box.sourceColumnName })
          : t('ledgers.summaryBoxManual')}
      </Typography>
    </Paper>
  );
}

/** The computed net total every box adds up to — not itself editable. */
function NetTotalCard({ value }: { value: number }) {
  const t = useT();

  return (
    <Paper
      elevation={0}
      sx={{
        p: 1.75,
        width: 176,
        borderRadius: 2,
        textAlign: 'center',
        bgcolor: NET_BOX_COLOR,
        border: '2px solid rgba(0,0,0,0.3)',
        boxShadow: '0 1px 3px rgba(0,0,0,0.15)',
      }}
    >
      <Typography
        variant="caption"
        sx={{
          display: 'block',
          fontWeight: 700,
          color: 'rgba(0,0,0,0.7)',
          textTransform: 'uppercase',
          letterSpacing: 0.4,
        }}
      >
        {t('ledgers.netTotal')}
      </Typography>
      <Typography variant="h6" sx={{ fontWeight: 700, color: 'rgba(0,0,0,0.87)', mt: 0.5 }}>
        {formatAmount(value)}
      </Typography>
    </Paper>
  );
}

/**
 * Every row with no Employee/Vehicle/Tool/Material link, read from one
 * lightweight ledger-wide query — not by opening every section at once,
 * which is heavy enough on a real month (dozens of sections, each doing its
 * own sourced-column computation) to freeze the page.
 */
function UnlinkedRowsPanel({
  ledgerId,
  onOpenSection,
}: {
  ledgerId: string;
  onOpenSection: (sectionId: string) => void;
}) {
  const t = useT();
  const { data, isLoading } = useLedgerUnlinkedRowsQuery(ledgerId, true);
  const rows = data ?? [];

  return (
    <Paper variant="outlined" sx={{ p: 2, mb: 3 }}>
      <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1 }}>
        {t('ledgers.unlinkedRowsHeading')}
      </Typography>

      {isLoading ? (
        <Typography color="text.secondary" variant="body2">
          {t('common.loading')}
        </Typography>
      ) : rows.length === 0 ? (
        <Typography color="text.secondary" variant="body2">
          {t('ledgers.noUnlinkedRowsSentence')}
        </Typography>
      ) : (
        <List dense disablePadding>
          {rows.map((row) => (
            <ListItem
              key={row.rowId}
              disableGutters
              secondaryAction={
                <Button size="small" onClick={() => onOpenSection(row.sectionId)}>
                  {t('ledgers.openSection')}
                </Button>
              }
            >
              <ListItemText primary={`${row.sectionName} — ${row.rowLabel}`} />
            </ListItem>
          ))}
        </List>
      )}
    </Paper>
  );
}

/**
 * "Promoted this month" at a glance — every row already pushed through to a
 * real record, in one place, instead of finding out one row at a time by
 * opening each section.
 */
function PromotionsPanel({ promotions }: { promotions: LedgerPromotion[] }) {
  const t = useT();
  const { locale } = useI18n();
  const [expanded, setExpanded] = useState(false);

  const generalExpenseCount = promotions.filter((p) => p.target === 'GeneralExpense').length;
  const accommodationCount = promotions.filter((p) => p.target === 'AccommodationRate').length;

  return (
    <Paper variant="outlined" sx={{ p: 2, mb: 3 }}>
      <Stack
        direction="row"
        onClick={() => setExpanded((v) => !v)}
        sx={{ alignItems: 'center', justifyContent: 'space-between', cursor: 'pointer' }}
      >
        <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
          <IconButton
            size="small"
            onClick={(event) => {
              event.stopPropagation();
              setExpanded((v) => !v);
            }}
          >
            {expanded ? (
              <ExpandLessOutlined fontSize="small" />
            ) : (
              <ExpandMoreOutlined fontSize="small" />
            )}
          </IconButton>
          <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
            {t('ledgers.promotionsHeading')}
          </Typography>
        </Stack>
        <Stack direction="row" spacing={1}>
          {generalExpenseCount > 0 && (
            <Chip
              size="small"
              label={t('ledgers.promotionsGeneralExpenseCount', { count: generalExpenseCount })}
            />
          )}
          {accommodationCount > 0 && (
            <Chip
              size="small"
              label={t('ledgers.promotionsAccommodationCount', { count: accommodationCount })}
            />
          )}
        </Stack>
      </Stack>

      <Collapse in={expanded} timeout="auto" unmountOnExit>
        <List dense disablePadding sx={{ mt: 1 }}>
          {promotions.map((promotion) => (
            <ListItem key={promotion.rowId} disableGutters>
              <ListItemText
                primary={`${promotion.sectionName} — ${promotion.rowLabel}`}
                secondary={`${formatMoney(promotion.amount, locale)} · ${formatDate(promotion.occurredOn)}`}
              />
              <Link
                component={RouterLink}
                to={
                  promotion.target === 'GeneralExpense'
                    ? paths.generalExpenses
                    : paths.accommodations
                }
                variant="body2"
              >
                {t('ledgers.viewRecord')}
              </Link>
            </ListItem>
          ))}
        </List>
      </Collapse>
    </Paper>
  );
}

function SummaryBoxDialog({
  open,
  box,
  columns,
  onClose,
  onSubmit,
  loading,
  error,
}: {
  open: boolean;
  box?: LedgerSummaryBox | null;
  columns: LedgerColumn[];
  onClose: () => void;
  onSubmit: (input: {
    label: string;
    sourceColumnId?: string | null;
    manualValue?: number | null;
    sign: 1 | -1;
    color?: string | null;
  }) => void;
  loading: boolean;
  error: { message: string } | null;
}) {
  const t = useT();
  const isEditing = !!box;
  const [label, setLabel] = useState('');
  const [useColumn, setUseColumn] = useState(true);
  const [sourceColumnId, setSourceColumnId] = useState('');
  const [manualValue, setManualValue] = useState('');
  const [sign, setSign] = useState<1 | -1>(-1);
  const [color, setColor] = useState<string>(DEFAULT_BOX_COLOR);

  useEffect(() => {
    if (!open) return;
    setLabel(box?.label ?? '');
    setUseColumn(box ? !!box.sourceColumnId : true);
    setSourceColumnId(box?.sourceColumnId ?? '');
    setManualValue(box?.manualValue != null ? String(box.manualValue) : '');
    setSign(box?.sign ?? -1);
    setColor(box?.color ?? DEFAULT_BOX_COLOR);
  }, [open, box]);

  const numericManualValue = manualValue.trim() === '' ? null : Number(manualValue.replace(',', '.'));
  const canSubmit =
    label.trim() !== '' && (useColumn ? sourceColumnId !== '' : numericManualValue !== null);

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="xs">
      <DialogTitle>{isEditing ? t('ledgers.editSummaryBoxTitle') : t('ledgers.addSummaryBox')}</DialogTitle>
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
              label={t('ledgers.summaryBoxLabel')}
              value={label}
              onChange={(event) => setLabel(event.target.value)}
            />
          </Grid>
          <Grid size={12}>
            <TextField
              select
              fullWidth
              label={t('ledgers.summaryBoxSource')}
              value={useColumn ? 'column' : 'manual'}
              onChange={(event) => setUseColumn(event.target.value === 'column')}
            >
              <MenuItem value="column">{t('ledgers.summaryBoxSourceColumn')}</MenuItem>
              <MenuItem value="manual">{t('ledgers.summaryBoxSourceManual')}</MenuItem>
            </TextField>
          </Grid>
          {useColumn ? (
            <Grid size={12}>
              <TextField
                select
                fullWidth
                label={t('ledgers.summaryBoxColumn')}
                value={sourceColumnId}
                onChange={(event) => setSourceColumnId(event.target.value)}
              >
                {columns.map((column) => (
                  <MenuItem key={column.id} value={column.id}>
                    {column.name}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>
          ) : (
            <Grid size={12}>
              <TextField
                fullWidth
                label={t('ledgers.summaryBoxManualValue')}
                value={manualValue}
                onChange={(event) => setManualValue(event.target.value)}
              />
            </Grid>
          )}
          <Grid size={12}>
            <TextField
              select
              fullWidth
              label={t('ledgers.summaryBoxSign')}
              value={sign}
              onChange={(event) => setSign(Number(event.target.value) as 1 | -1)}
            >
              <MenuItem value={1}>{t('ledgers.summaryBoxSignPlus')}</MenuItem>
              <MenuItem value={-1}>{t('ledgers.summaryBoxSignMinus')}</MenuItem>
            </TextField>
          </Grid>
          <Grid size={12}>
            <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 0.5 }}>
              {t('ledgers.summaryBoxColor')}
            </Typography>
            <Stack direction="row" spacing={1}>
              {BOX_COLOR_PALETTE.map((swatch) => (
                <Box
                  key={swatch}
                  onClick={() => setColor(swatch)}
                  sx={{
                    width: 28,
                    height: 28,
                    borderRadius: '50%',
                    bgcolor: swatch,
                    cursor: 'pointer',
                    border: swatch === color ? '2px solid #000' : '1px solid rgba(0,0,0,0.25)',
                  }}
                />
              ))}
            </Stack>
          </Grid>
        </Grid>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button
          variant="contained"
          disabled={!canSubmit}
          loading={loading}
          onClick={() =>
            onSubmit({
              label: label.trim(),
              sourceColumnId: useColumn ? sourceColumnId : null,
              manualValue: useColumn ? null : numericManualValue,
              sign,
              color,
            })
          }
        >
          {t('common.save')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

/** Purely presentational — every write is a callback into the page's single shared mutation. */
function RowLine({
  row,
  columns,
  onCommitLabel,
  onDelete,
  onPromote,
  onSetCell,
  onSetRowColor,
  onSetCellColor,
  onMoveUp,
  onMoveDown,
}: {
  row: LedgerRow;
  columns: LedgerColumn[];
  onCommitLabel: (label: string) => void;
  onDelete: () => void;
  onPromote: () => void;
  onSetCell: (columnId: string, value: string) => void;
  onSetRowColor: (color: string | null) => void;
  onSetCellColor: (columnId: string, color: string | null) => void;
  onMoveUp?: () => void;
  onMoveDown?: () => void;
}) {
  const t = useT();
  const subjectName = row.employeeName ?? row.vehicleName ?? row.toolName ?? row.materialName;
  const isPromoted = !!row.promotedGeneralExpenseId || !!row.promotedAccommodationRateId;

  // A heads-up, not a hard rule: this row can't know whether a manual
  // Currency/Number cell means the same real cost the automatic column
  // already covers — only that both are non-zero at once, which is worth a
  // second look before it silently doubles up in the ledger's totals.
  const columnById = new Map(columns.map((c) => [c.id, c]));
  const hasAutomaticAmount = row.cells.some(
    (cell) => cell.isComputed && parseNumeric(cell.value) > 0,
  );
  const hasManualAmount = row.cells.some((cell) => {
    if (cell.isComputed) return false;
    const column = columnById.get(cell.columnId);
    return column && NUMERIC_TYPES.includes(column.dataType) && parseNumeric(cell.value) > 0;
  });
  const possibleDuplicate = hasAutomaticAmount && hasManualAmount;

  return (
    <TableRow hover sx={{ bgcolor: row.colorTag ?? undefined }}>
      <TableCell>
        <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center' }}>
          <ColorPickerButton value={row.colorTag} onPick={onSetRowColor} />
          <Box sx={{ flex: 1, minWidth: 0 }}>
            <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center' }}>
              <InlineText value={row.label} onCommit={onCommitLabel} />
              {possibleDuplicate && (
                <Tooltip title={t('ledgers.possibleDuplicateHint')}>
                  <WarningAmberOutlined sx={{ fontSize: 16, color: 'warning.main', flexShrink: 0 }} />
                </Tooltip>
              )}
            </Stack>
            {subjectName && (
              <Typography variant="caption" color="text.secondary" noWrap sx={{ display: 'block' }}>
                {subjectName}
              </Typography>
            )}
            {isPromoted && (
              <Typography variant="caption" color="success.main" noWrap sx={{ display: 'block' }}>
                {row.promotedGeneralExpenseId
                  ? t('ledgers.promoteTargetGeneralExpense')
                  : t('ledgers.promoteTargetAccommodationRate')}
                {' ✓'}
              </Typography>
            )}
          </Box>
        </Stack>
      </TableCell>
      {columns.map((column) => {
        const cell = row.cells.find((c) => c.columnId === column.id);
        return (
          <TableCell
            key={column.id}
            align="right"
            sx={{ position: 'relative', bgcolor: cell?.colorTag ?? undefined, '&:hover .cell-color-btn': { opacity: 1 }, '&:hover .cell-history-btn': { opacity: 1 } }}
          >
            {!column.sourceMetric && (
              <Box className="cell-color-btn" sx={{ position: 'absolute', top: 0, left: 2, opacity: 0, transition: 'opacity 0.15s' }}>
                <ColorPickerButton
                  value={cell?.colorTag ?? null}
                  onPick={(color) => onSetCellColor(column.id, color)}
                />
              </Box>
            )}
            {cell?.id && (
              <Box className="cell-history-btn" sx={{ position: 'absolute', top: 0, right: 2, opacity: 0, transition: 'opacity 0.15s' }}>
                <CellHistoryButton cellId={cell.id} />
              </Box>
            )}
            <CellInput
              value={cell?.value ?? ''}
              dataType={column.dataType}
              sourceMetric={column.sourceMetric}
              onCommit={(value) => onSetCell(column.id, value)}
            />
          </TableCell>
        );
      })}
      <TableCell align="right">
        <Stack direction="row" spacing={0} sx={{ justifyContent: 'flex-end' }}>
          {!isPromoted && (
            <Tooltip title={t('ledgers.promoteRow')}>
              <IconButton size="small" onClick={onPromote}>
                <ReceiptLongOutlined sx={{ fontSize: 14 }} />
              </IconButton>
            </Tooltip>
          )}
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
          <IconButton size="small" onClick={onDelete}>
            <DeleteOutlined sx={{ fontSize: 14 }} />
          </IconButton>
        </Stack>
      </TableCell>
    </TableRow>
  );
}

/** One editable cell — local draft while typing, committed on blur so every keystroke isn't a network call. A sourced (computed) cell is read-only and never drafts. */
function CellInput({
  value,
  dataType,
  sourceMetric,
  onCommit,
}: {
  value: string;
  dataType: LedgerColumnDataType;
  sourceMetric: LedgerColumnSourceMetric | null;
  onCommit: (value: string) => void;
}) {
  const t = useT();
  const [draft, setDraft] = useState(value);

  useEffect(() => {
    setDraft(value);
  }, [value]);

  const inputType = dataType === 'Date' ? 'date' : 'text';

  if (sourceMetric) {
    const isEmpty = !value || parseNumeric(value) === 0;
    const emptyHintKey =
      sourceMetric === 'VehicleTotalCost'
        ? 'ledgers.computedEmptyVehicle'
        : sourceMetric === 'ToolTotalCost'
          ? 'ledgers.computedEmptyTool'
          : 'ledgers.computedEmptyMaterial';

    return (
      <Tooltip title={isEmpty ? t(emptyHintKey) : t('ledgers.computedCellTooltip')}>
        <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center', justifyContent: 'flex-end', minWidth: 90 }}>
          <Typography variant="body2" color="text.secondary">
            {value || '—'}
          </Typography>
          <LockOutlined sx={{ fontSize: 12, color: 'text.disabled' }} />
        </Stack>
      </Tooltip>
    );
  }

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
      slotProps={{
        htmlInput: { style: { textAlign: 'right', background: 'transparent' } },
        input: { disableUnderline: false, sx: { bgcolor: 'transparent' } },
      }}
      sx={{ minWidth: 90 }}
    />
  );
}

/** A small clock icon: click to see who changed this cell's value, and when. */
function CellHistoryButton({ cellId }: { cellId: string }) {
  const t = useT();
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);

  return (
    <>
      <Tooltip title={t('ledgers.cellHistory')}>
        <IconButton size="small" onClick={(event) => setAnchor(event.currentTarget)} sx={{ p: 0.25 }}>
          <HistoryOutlined sx={{ fontSize: 14 }} />
        </IconButton>
      </Tooltip>
      <Popover
        open={!!anchor}
        anchorEl={anchor}
        onClose={() => setAnchor(null)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
      >
        <Box sx={{ width: 320 }}>
          <AuditHistoryCard entityName="LedgerCell" entityId={cellId} />
        </Box>
      </Popover>
    </>
  );
}

const ROW_COLOR_PALETTE = [
  '#F6C6C6', // red
  '#F9DFA0', // yellow
  '#C7E8B9', // green
  '#B9DDF2', // blue
  '#DCC7EE', // purple
  '#E2E2E2', // grey
];

/**
 * A small dot: click to open a palette and tag a row or cell with a color —
 * the same free-form highlighting the client uses fill color for in the real
 * spreadsheet this feature replaces.
 */
function ColorPickerButton({
  value,
  onPick,
}: {
  value: string | null | undefined;
  onPick: (color: string | null) => void;
}) {
  const t = useT();
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);

  return (
    <>
      <Tooltip title={t('ledgers.pickColor')}>
        <IconButton
          size="small"
          onClick={(event) => setAnchor(event.currentTarget)}
          sx={{ p: 0.25 }}
        >
          <Box
            sx={{
              width: 14,
              height: 14,
              borderRadius: '50%',
              bgcolor: value ?? 'transparent',
              border: value ? '1px solid rgba(0,0,0,0.3)' : '1px dashed rgba(0,0,0,0.35)',
            }}
          />
        </IconButton>
      </Tooltip>
      <Popover
        open={!!anchor}
        anchorEl={anchor}
        onClose={() => setAnchor(null)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'left' }}
      >
        <Stack direction="row" spacing={1} sx={{ p: 1.25, alignItems: 'center' }}>
          <Tooltip title={t('ledgers.noColor')}>
            <IconButton
              size="small"
              onClick={() => {
                onPick(null);
                setAnchor(null);
              }}
            >
              <Box
                sx={{
                  width: 20,
                  height: 20,
                  borderRadius: '50%',
                  border: '1px solid rgba(0,0,0,0.3)',
                  position: 'relative',
                  '&::after': {
                    content: '""',
                    position: 'absolute',
                    inset: '9px -1px',
                    borderTop: '1px solid rgba(0,0,0,0.5)',
                    transform: 'rotate(45deg)',
                  },
                }}
              />
            </IconButton>
          </Tooltip>
          {ROW_COLOR_PALETTE.map((swatch) => (
            <Box
              key={swatch}
              onClick={() => {
                onPick(swatch);
                setAnchor(null);
              }}
              sx={{
                width: 22,
                height: 22,
                borderRadius: '50%',
                bgcolor: swatch,
                cursor: 'pointer',
                border: swatch === value ? '2px solid #000' : '1px solid rgba(0,0,0,0.2)',
              }}
            />
          ))}
        </Stack>
      </Popover>
    </>
  );
}

/** The row label, editable the same way a cell is. */
function InlineText({ value, onCommit }: { value: string; onCommit: (value: string) => void }) {
  const [draft, setDraft] = useState(value);

  useEffect(() => {
    setDraft(value);
  }, [value]);

  return (
    <TextField
      variant="standard"
      size="small"
      fullWidth
      value={draft}
      onChange={(event) => setDraft(event.target.value)}
      onBlur={() => {
        const trimmed = draft.trim();
        if (trimmed && trimmed !== value) onCommit(trimmed);
        else setDraft(value);
      }}
      slotProps={{ input: { disableUnderline: false } }}
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
  onSubmit: (input: {
    name: string;
    dataType: LedgerColumnDataType;
    sourceMetric?: LedgerColumnSourceMetric | null;
  }) => void;
  loading: boolean;
  error: { message: string } | null;
}) {
  const t = useT();
  const enumLabel = useEnumLabel();
  const isEditing = !!column;
  const [name, setName] = useState('');
  const [dataType, setDataType] = useState<LedgerColumnDataType>('Number');
  const [mode, setMode] = useState<'manual' | 'auto'>('manual');
  const [sourceMetric, setSourceMetric] = useState<LedgerColumnSourceMetric | ''>('');

  useEffect(() => {
    if (!open) return;
    setName(column?.name ?? '');
    setDataType(column?.dataType ?? 'Number');
    setMode(column?.sourceMetric ? 'auto' : 'manual');
    setSourceMetric(column?.sourceMetric ?? '');
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
        <Typography color="text.secondary" variant="body2" sx={{ mb: 2 }}>
          {t('ledgers.columnModeIntro')}
        </Typography>
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
            <ToggleButtonGroup
              fullWidth
              exclusive
              size="small"
              value={mode}
              onChange={(_event, next: 'manual' | 'auto' | null) => {
                if (!next) return;
                setMode(next);
                if (next === 'manual') setSourceMetric('');
                else if (!sourceMetric) setSourceMetric(ledgerColumnSourceMetrics[0]);
              }}
            >
              <ToggleButton value="manual">{t('ledgers.columnModeManual')}</ToggleButton>
              <ToggleButton value="auto">{t('ledgers.columnModeAuto')}</ToggleButton>
            </ToggleButtonGroup>
          </Grid>

          {mode === 'manual' ? (
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
          ) : (
            <Grid size={12}>
              <Stack spacing={1}>
                {ledgerColumnSourceMetrics.map((value) => (
                  <Paper
                    key={value}
                    variant="outlined"
                    onClick={() => setSourceMetric(value)}
                    sx={{
                      p: 1.5,
                      cursor: 'pointer',
                      borderColor: sourceMetric === value ? 'primary.main' : undefined,
                      borderWidth: sourceMetric === value ? 2 : 1,
                      bgcolor: sourceMetric === value ? 'action.selected' : undefined,
                    }}
                  >
                    <Typography variant="body2" sx={{ fontWeight: 600 }}>
                      {enumLabel('ledgerColumnSourceMetric', value)}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      {t(`ledgers.columnSourceExplain.${value}` as MessageKey)}
                    </Typography>
                  </Paper>
                ))}
                <Typography variant="caption" color="text.secondary">
                  {t('ledgers.columnSourceHint')}
                </Typography>
              </Stack>
            </Grid>
          )}
        </Grid>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button
          variant="contained"
          disabled={name.trim() === '' || (mode === 'auto' && !sourceMetric)}
          loading={loading}
          onClick={() =>
            onSubmit({
              name: name.trim(),
              dataType: mode === 'auto' ? 'Currency' : dataType,
              sourceMetric: mode === 'auto' ? sourceMetric || null : null,
            })
          }
        >
          {t('common.save')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

function SectionDialog({
  open,
  projects,
  onClose,
  onSubmit,
  loading,
  error,
}: {
  open: boolean;
  projects: Project[];
  onClose: () => void;
  onSubmit: (input: { name: string; projectId?: string | null }) => void;
  loading: boolean;
  error: { message: string } | null;
}) {
  const t = useT();
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
              {projects.map((project) => (
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

type RowSubjectType = 'none' | 'employee' | 'vehicle' | 'tool' | 'material';

function RowDialog({
  open,
  employees,
  vehicles,
  tools,
  materials,
  onClose,
  onSubmit,
  loading,
  error,
}: {
  open: boolean;
  employees: Employee[];
  vehicles: Vehicle[];
  tools: Tool[];
  materials: Material[];
  onClose: () => void;
  onSubmit: (input: {
    label: string;
    employeeId?: string | null;
    vehicleId?: string | null;
    toolId?: string | null;
    materialId?: string | null;
  }) => void;
  loading: boolean;
  error: { message: string } | null;
}) {
  const t = useT();
  const [label, setLabel] = useState('');
  const [subjectType, setSubjectType] = useState<Exclude<RowSubjectType, 'none'>>('employee');
  const [subjectId, setSubjectId] = useState('');

  useEffect(() => {
    if (!open) return;
    setLabel('');
    setSubjectType('employee');
    setSubjectId('');
  }, [open]);

  const subjectOptions =
    subjectType === 'employee'
      ? employees.map((e) => ({ id: e.id, label: `${e.firstName} ${e.lastName}` }))
      : subjectType === 'vehicle'
        ? vehicles.map((v) => ({ id: v.id, label: `${v.brand} ${v.model} (${v.registrationNumber})` }))
        : subjectType === 'tool'
          ? tools.map((tool) => ({ id: tool.id, label: tool.name }))
          : materials.map((m) => ({ id: m.id, label: m.name }));

  const subjectLabel =
    subjectType === 'employee'
      ? t('ledgers.linkedEmployee')
      : subjectType === 'vehicle'
        ? t('ledgers.linkedVehicle')
        : subjectType === 'tool'
          ? t('ledgers.linkedTool')
          : t('ledgers.linkedMaterial');

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="xs">
      <DialogTitle>{t('ledgers.addRow')}</DialogTitle>
      <DialogContent>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error.message}
          </Alert>
        )}
        <Typography color="text.secondary" variant="body2" sx={{ mb: 2 }}>
          {t('ledgers.rowSubjectIntro')}
        </Typography>
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
            <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 0.5 }}>
              {t('ledgers.rowSubjectType')}
            </Typography>
            <ToggleButtonGroup
              fullWidth
              exclusive
              size="small"
              value={subjectType}
              onChange={(_event, next: Exclude<RowSubjectType, 'none'> | null) => {
                if (!next) return;
                setSubjectType(next);
                setSubjectId('');
              }}
            >
              <ToggleButton value="employee">{t('ledgers.rowSubjectEmployee')}</ToggleButton>
              <ToggleButton value="vehicle">{t('ledgers.rowSubjectVehicle')}</ToggleButton>
              <ToggleButton value="tool">{t('ledgers.rowSubjectTool')}</ToggleButton>
              <ToggleButton value="material">{t('ledgers.rowSubjectMaterial')}</ToggleButton>
            </ToggleButtonGroup>
          </Grid>

          <Grid size={12}>
            <TextField
              select
              fullWidth
              label={subjectLabel}
              value={subjectId}
              onChange={(event) => setSubjectId(event.target.value)}
              helperText={t('generalExpenses.optionalHint')}
            >
              <MenuItem value="">
                <em>{t('common.none')}</em>
              </MenuItem>
              {subjectOptions.map((option) => (
                <MenuItem key={option.id} value={option.id}>
                  {option.label}
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
          onClick={() =>
            onSubmit({
              label: label.trim(),
              employeeId: subjectType === 'employee' ? subjectId || null : null,
              vehicleId: subjectType === 'vehicle' ? subjectId || null : null,
              toolId: subjectType === 'tool' ? subjectId || null : null,
              materialId: subjectType === 'material' ? subjectId || null : null,
            })
          }
        >
          {t('common.save')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

type PromoteTarget = 'generalExpense' | 'accommodationRate';

/**
 * Pushes a manually-typed row through the real form it represents — an
 * explicit, one-shot action, not a silent sync. The SuperAdmin picks which
 * of the row's own columns holds the amount, then the rest of the form is
 * the same one the Costs/Accommodations screens use.
 */
function PromoteRowDialog({
  open,
  row,
  columns,
  projects,
  employees,
  accommodations,
  onClose,
  onSubmitGeneralExpense,
  onSubmitAccommodationRate,
  loading,
  error,
}: {
  open: boolean;
  row: LedgerRow | null;
  columns: LedgerColumn[];
  projects: Project[];
  employees: Employee[];
  accommodations: Accommodation[];
  onClose: () => void;
  onSubmitGeneralExpense: (input: {
    category: GeneralExpenseCategory;
    amount: number;
    projectId?: string | null;
    employeeId?: string | null;
    note?: string | null;
  }) => void;
  onSubmitAccommodationRate: (input: { accommodationId: string; monthlyAmount: number }) => void;
  loading: boolean;
  error: { message: string } | null;
}) {
  const t = useT();
  const { locale } = useI18n();
  const enumLabel = useEnumLabel();
  const [target, setTarget] = useState<PromoteTarget>('generalExpense');
  const [amountColumnId, setAmountColumnId] = useState('');
  const [category, setCategory] = useState<GeneralExpenseCategory>('WorkerOther');
  const [projectId, setProjectId] = useState('');
  const [employeeId, setEmployeeId] = useState('');
  const [note, setNote] = useState('');
  const [accommodationId, setAccommodationId] = useState('');
  const [showMoreOptions, setShowMoreOptions] = useState(false);

  const amountColumns = columns.filter((c) => NUMERIC_TYPES.includes(c.dataType));

  useEffect(() => {
    if (!open || !row) return;
    setTarget('generalExpense');
    setAmountColumnId(amountColumns[0]?.id ?? '');
    setCategory('WorkerOther');
    setProjectId('');
    setEmployeeId(row.employeeId ?? '');
    setNote(row.label);
    setAccommodationId('');
    setShowMoreOptions(false);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, row]);

  if (!row) return null;

  const amount = parseNumeric(
    row.cells.find((c) => c.columnId === amountColumnId)?.value ?? null,
  );

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="xs">
      <DialogTitle>{t('ledgers.promoteDialogTitle')}</DialogTitle>
      <DialogContent>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error.message}
          </Alert>
        )}
        <Typography color="text.secondary" variant="body2" sx={{ mb: 2 }}>
          {t('ledgers.promoteIntro', { row: row.label })}
        </Typography>
        <Grid container spacing={2} sx={{ mt: 0 }}>
          <Grid size={12}>
            <ToggleButtonGroup
              fullWidth
              exclusive
              size="small"
              value={target}
              onChange={(_event, next: PromoteTarget | null) => next && setTarget(next)}
            >
              <ToggleButton value="generalExpense">
                {t('ledgers.promoteTargetGeneralExpense')}
              </ToggleButton>
              <ToggleButton value="accommodationRate">
                {t('ledgers.promoteTargetAccommodationRate')}
              </ToggleButton>
            </ToggleButtonGroup>
          </Grid>

          {amountColumns.length > 1 && (
            <Grid size={12}>
              <TextField
                select
                fullWidth
                label={t('ledgers.promoteAmountColumn')}
                value={amountColumnId}
                onChange={(event) => setAmountColumnId(event.target.value)}
              >
                {amountColumns.map((column) => (
                  <MenuItem key={column.id} value={column.id}>
                    {column.name}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>
          )}

          <Grid size={12}>
            <Paper variant="outlined" sx={{ p: 1.5, textAlign: 'center', bgcolor: 'action.hover' }}>
              <Typography variant="caption" color="text.secondary">
                {t('ledgers.promoteAmountLabel')}
              </Typography>
              <Typography variant="h6" sx={{ fontWeight: 700 }}>
                {formatMoney(amount, locale)}
              </Typography>
            </Paper>
          </Grid>

          {target === 'generalExpense' ? (
            <>
              <Grid size={12}>
                <TextField
                  select
                  fullWidth
                  label={t('generalExpenses.category')}
                  value={category}
                  onChange={(event) => setCategory(event.target.value as GeneralExpenseCategory)}
                >
                  {generalExpenseCategories.map((value) => (
                    <MenuItem key={value} value={value}>
                      {enumLabel('generalExpenseCategory', value)}
                    </MenuItem>
                  ))}
                </TextField>
              </Grid>

              {!showMoreOptions ? (
                <Grid size={12}>
                  <Button size="small" onClick={() => setShowMoreOptions(true)}>
                    {t('ledgers.promoteMoreOptions')}
                  </Button>
                </Grid>
              ) : (
                <>
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
                      {projects.map((project) => (
                        <MenuItem key={project.id} value={project.id}>
                          {project.name}
                        </MenuItem>
                      ))}
                    </TextField>
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
                      {employees.map((employee) => (
                        <MenuItem key={employee.id} value={employee.id}>
                          {employee.firstName} {employee.lastName}
                        </MenuItem>
                      ))}
                    </TextField>
                  </Grid>
                  <Grid size={12}>
                    <TextField
                      fullWidth
                      label={t('generalExpenses.note')}
                      value={note}
                      onChange={(event) => setNote(event.target.value)}
                    />
                  </Grid>
                </>
              )}
            </>
          ) : accommodations.length === 0 ? (
            <Grid size={12}>
              <Alert severity="info">{t('ledgers.noAccommodationsHint')}</Alert>
            </Grid>
          ) : (
            <Grid size={12}>
              <TextField
                select
                fullWidth
                label={t('accommodations.title')}
                value={accommodationId}
                onChange={(event) => setAccommodationId(event.target.value)}
              >
                {accommodations.map((accommodation) => (
                  <MenuItem key={accommodation.id} value={accommodation.id}>
                    {accommodation.address}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>
          )}
        </Grid>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button
          variant="contained"
          disabled={
            !amountColumnId ||
            amount <= 0 ||
            (target === 'accommodationRate' && !accommodationId)
          }
          loading={loading}
          onClick={() => {
            if (target === 'generalExpense') {
              onSubmitGeneralExpense({
                category,
                amount,
                projectId: projectId || null,
                employeeId: employeeId || null,
                note: note.trim() || null,
              });
            } else {
              onSubmitAccommodationRate({ accommodationId, monthlyAmount: amount });
            }
          }}
        >
          {t('common.save')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
