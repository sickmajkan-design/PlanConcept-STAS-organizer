import {
  AddOutlined,
  ChevronLeftOutlined,
  ChevronRightOutlined,
  DeleteOutlined,
  EditOutlined,
  HomeWorkOutlined,
} from '@mui/icons-material';
import {
  Alert,
  Avatar,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  Grid,
  IconButton,
  LinearProgress,
  Link as MuiLink,
  MenuItem,
  Stack,
  Switch,
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
import { Link as RouterLink, useNavigate, useParams } from 'react-router-dom';

import { toApiError } from '../../api/apiError';
import {
  accommodationChargeKinds,
  type Accommodation,
  type AccommodationChargeKind,
  type AccommodationRate,
  type AccommodationStay,
} from '../../api/types';
import { canAdministerAccounts, canSeeSpending } from '../../auth/authHelpers';
import { useAuth } from '../../auth/useAuth';
import { AttachmentList } from '../../components/AttachmentList';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { ErrorState } from '../../components/ErrorState';
import {
  useAccommodationCostsQuery,
  useAccommodationQuery,
  useAllAccommodationsQuery,
  useAccommodationStaysQuery,
  useAddAccommodationStay,
  useDeleteAccommodation,
  useDeleteAccommodationStay,
  useUpdateAccommodationStay,
} from '../../features/accommodations/useAccommodations';
import {
  useAccommodationRatesQuery,
  useDeleteAccommodationRate,
  useSetAccommodationRate,
  useUpdateAccommodationRate,
} from '../../features/costs/useCosts';
import { useAllEmployeesQuery } from '../../features/employees/useEmployees';
import { useAllProjectsQuery } from '../../features/projects/useProjects';
import { useDeleteWithConfirm } from '../../hooks/useDeleteWithConfirm';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useI18n, useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';
import { dateOnlyOffset, formatDate, formatMoney } from '../../utils/formatting';

/** What a place is called: its name, or its address when nobody named it. */
export const accommodationTitle = (accommodation: Pick<Accommodation, 'name' | 'address'>) =>
  accommodation.name || accommodation.address;

const CONTRACT_WARNING_DAYS = 30;

export function AccommodationDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const t = useT();
  const enumLabel = useEnumLabel();
  const { locale } = useI18n();
  const { user } = useAuth();

  const { data: accommodation, isLoading, isError, error, refetch } = useAccommodationQuery(id);
  const deleteAccommodation = useDeleteAccommodation();
  const [confirmDelete, setConfirmDelete] = useState(false);

  if (isLoading) return null;
  if (isError || !accommodation) {
    return <ErrorState error={error} onRetry={() => void refetch()} />;
  }

  const handleDelete = async () => {
    await deleteAccommodation.mutateAsync(accommodation.id);
    navigate(paths.accommodations, { replace: true });
  };

  const beds = accommodation.beds;
  const over = beds !== null && accommodation.currentOccupants > beds;
  const contractEnds = accommodation.contractEnd;
  const daysToContractEnd = contractEnds
    ? Math.round(
        (new Date(`${contractEnds}T00:00`).getTime() - new Date(`${dateOnlyOffset(0)}T00:00`).getTime()) /
          86_400_000,
      )
    : null;

  return (
    <Box sx={{ maxWidth: 960 }}>
      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Stack
            direction={{ xs: 'column', md: 'row' }}
            spacing={3}
            sx={{ alignItems: { md: 'center' } }}
          >
            <Avatar sx={{ width: 64, height: 64, bgcolor: 'primary.main' }}>
              <HomeWorkOutlined />
            </Avatar>
            <Box sx={{ flex: 1, minWidth: 0 }}>
              <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', alignItems: 'center' }}>
                <Typography variant="h5" sx={{ fontWeight: 700, overflowWrap: 'anywhere' }}>
                  {accommodationTitle(accommodation)}
                </Typography>
                <Chip size="small" variant="outlined" label={enumLabel('accommodationType', accommodation.type)} />
                {!accommodation.isActive && (
                  <Chip size="small" color="default" label={t('accommodations.inactive')} />
                )}
              </Stack>
              <Typography color="text.secondary" sx={{ mt: 0.5 }}>
                {[accommodation.name ? accommodation.address : null, accommodation.city]
                  .filter(Boolean)
                  .join(', ')}
              </Typography>
              {accommodation.currentMonthlyAmount !== null && (
                <Typography color="text.secondary" sx={{ mt: 0.5 }}>
                  {t('accommodations.currentMonthlyAmount')}:{' '}
                  {formatMoney(accommodation.currentMonthlyAmount, locale)}
                  {accommodation.currentProvider ? ` · ${accommodation.currentProvider}` : ''}
                </Typography>
              )}
              <Box sx={{ mt: 1.5, maxWidth: 360 }}>
                <Typography variant="caption" color={over ? 'error' : 'text.secondary'}>
                  {beds === null
                    ? t('accommodations.occupancyNoBeds', { count: accommodation.currentOccupants })
                    : t('accommodations.occupancyOf', { count: accommodation.currentOccupants, beds })}
                  {over ? ` · ${t('accommodations.overCapacity')}` : ''}
                </Typography>
                {beds !== null && (
                  <LinearProgress
                    variant="determinate"
                    color={over ? 'error' : 'primary'}
                    value={Math.min(100, (accommodation.currentOccupants / beds) * 100)}
                    sx={{ mt: 0.5, height: 6, borderRadius: 3 }}
                  />
                )}
              </Box>
            </Box>
            <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap' }}>
              <Button
                variant="outlined"
                startIcon={<EditOutlined />}
                onClick={() => navigate(paths.accommodationEdit(accommodation.id))}
              >
                {t('common.edit')}
              </Button>
              <Button
                variant="outlined"
                color="error"
                startIcon={<DeleteOutlined />}
                onClick={() => setConfirmDelete(true)}
              >
                {t('common.delete')}
              </Button>
            </Stack>
          </Stack>
        </CardContent>
      </Card>

      <Grid container spacing={3}>
        <Grid size={{ xs: 12, md: 6 }}>
          <Card sx={{ height: '100%' }}>
            <CardContent>
              <Typography variant="subtitle1" gutterBottom sx={{ fontWeight: 700 }}>
                {t('accommodations.sectionPlace')}
              </Typography>
              <Box sx={{ mt: 1, display: 'grid', gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' }, gap: 2 }}>
                <InfoRow label={t('accommodations.floor')} value={accommodation.floor} />
                <InfoRow
                  label={t('accommodations.rooms')}
                  value={accommodation.rooms === null ? null : String(accommodation.rooms)}
                />
                <InfoRow
                  label={t('accommodations.beds')}
                  value={accommodation.beds === null ? null : String(accommodation.beds)}
                />
                <InfoRow
                  label={t('accommodations.area')}
                  value={accommodation.areaSquareMeters === null ? null : `${accommodation.areaSquareMeters} m²`}
                />
                {accommodation.utilitiesIncluded && (
                  <Chip size="small" variant="outlined" label={t('accommodations.utilitiesIncluded')} sx={{ alignSelf: 'flex-start' }} />
                )}
                {accommodation.note && (
                  <Box>
                    <Typography variant="caption" color="text.secondary">
                      {t('accommodations.note')}
                    </Typography>
                    <Typography sx={{ whiteSpace: 'pre-wrap' }}>{accommodation.note}</Typography>
                  </Box>
                )}
              </Box>
            </CardContent>
          </Card>
        </Grid>

        <Grid size={{ xs: 12, md: 6 }}>
          <Card sx={{ height: '100%' }}>
            <CardContent>
              <Typography variant="subtitle1" gutterBottom sx={{ fontWeight: 700 }}>
                {t('accommodations.sectionLandlord')}
              </Typography>
              <Box sx={{ mt: 1, display: 'grid', gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' }, gap: 2 }}>
                <InfoRow label={t('accommodations.landlordName')} value={accommodation.landlordName} />
                <Box>
                  <Typography variant="caption" color="text.secondary">
                    {t('accommodations.landlordPhone')}
                  </Typography>
                  <Typography>
                    {accommodation.landlordPhone ? (
                      <MuiLink href={`tel:${accommodation.landlordPhone}`}>{accommodation.landlordPhone}</MuiLink>
                    ) : (
                      '—'
                    )}
                  </Typography>
                </Box>
                <Box>
                  <Typography variant="caption" color="text.secondary">
                    {t('accommodations.landlordEmail')}
                  </Typography>
                  <Typography>
                    {accommodation.landlordEmail ? (
                      <MuiLink href={`mailto:${accommodation.landlordEmail}`}>{accommodation.landlordEmail}</MuiLink>
                    ) : (
                      '—'
                    )}
                  </Typography>
                </Box>
                <InfoRow label={t('accommodations.contractNumber')} value={accommodation.contractNumber} />
                <Box>
                  <Typography variant="caption" color="text.secondary">
                    {t('accommodations.contractEnd')}
                  </Typography>
                  <Stack direction="row" spacing={1} useFlexGap sx={{ alignItems: 'center', flexWrap: 'wrap' }}>
                    <Typography>
                      {[
                        accommodation.contractStart ? formatDate(accommodation.contractStart) : null,
                        accommodation.contractEnd ? formatDate(accommodation.contractEnd) : null,
                      ]
                        .filter(Boolean)
                        .join(' – ') || '—'}
                    </Typography>
                    {daysToContractEnd !== null && daysToContractEnd < 0 && (
                      <Chip
                        size="small"
                        color="error"
                        label={t('accommodations.contractEnded', { date: formatDate(contractEnds!) })}
                      />
                    )}
                    {daysToContractEnd !== null &&
                      daysToContractEnd >= 0 &&
                      daysToContractEnd <= CONTRACT_WARNING_DAYS && (
                        <Chip
                          size="small"
                          color="warning"
                          label={t('accommodations.contractEnding', { date: formatDate(contractEnds!) })}
                        />
                      )}
                  </Stack>
                </Box>
                <InfoRow
                  label={t('accommodations.deposit')}
                  value={accommodation.depositAmount === null ? null : formatMoney(accommodation.depositAmount, locale)}
                />
              </Box>
            </CardContent>
          </Card>
        </Grid>

        <Grid size={12}>
          <OccupantsCard accommodation={accommodation} />
        </Grid>

        <Grid size={12}>
          <ChargesCard accommodationId={accommodation.id} />
        </Grid>

        {canSeeSpending(user) && (
          <Grid size={12}>
            <CostSummaryCard accommodationId={accommodation.id} />
          </Grid>
        )}

        <Grid size={12}>
          <Card>
            <CardContent>
              <AttachmentList
                ownerType="Accommodation"
                ownerId={accommodation.id}
                categories={['Contract', 'Photo', 'Other']}
                canDelete={canAdministerAccounts(user)}
              />
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      <ConfirmDialog
        open={confirmDelete}
        title={t('accommodations.deleteTitle')}
        description={t('accommodations.deleteBody', { name: accommodationTitle(accommodation) })}
        confirmLabel={t('common.delete')}
        destructive
        loading={deleteAccommodation.isPending}
        onConfirm={handleDelete}
        onCancel={() => setConfirmDelete(false)}
      />
    </Box>
  );
}

function InfoRow({ label, value }: { label: string; value: string | null | undefined }) {
  return (
    <Box>
      <Typography variant="caption" color="text.secondary">
        {label}
      </Typography>
      <Typography>{value || '—'}</Typography>
    </Box>
  );
}

// ---- occupants ------------------------------------------------------------

function OccupantsCard({ accommodation }: { accommodation: Accommodation }) {
  const t = useT();
  const [showEnded, setShowEnded] = useState(false);
  const [adding, setAdding] = useState(false);
  const [editing, setEditing] = useState<AccommodationStay | null>(null);

  const query = useMemo(
    () => ({
      accommodationId: accommodation.id,
      pageNumber: 1,
      pageSize: 100,
      sortBy: 'startDate',
      sortDescending: true,
    }),
    [accommodation.id],
  );

  const { data } = useAccommodationStaysQuery(query);
  const remove = useDeleteWithConfirm<AccommodationStay>(useDeleteAccommodationStay());
  const today = dateOnlyOffset(0);

  const all = data?.items ?? [];
  const isCurrent = (stay: AccommodationStay) =>
    stay.startDate <= today && (stay.endDate === null || stay.endDate >= today);
  const rows = showEnded ? all : all.filter((stay) => stay.endDate === null || stay.endDate >= today);

  return (
    <Card>
      <CardContent>
        <Stack
          direction="row"
          useFlexGap
          sx={{ flexWrap: 'wrap', gap: 1, alignItems: 'center', justifyContent: 'space-between' }}
        >
          <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
            {t('accommodations.occupants')}
          </Typography>
          <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', alignItems: 'center' }}>
            <FormControlLabel
              control={<Switch size="small" checked={showEnded} onChange={(e) => setShowEnded(e.target.checked)} />}
              label={t('accommodations.showEnded')}
            />
            <Button size="small" startIcon={<AddOutlined />} onClick={() => setAdding(true)}>
              {t('accommodations.addOccupant')}
            </Button>
          </Stack>
        </Stack>

        {rows.length === 0 ? (
          <Typography color="text.secondary" sx={{ mt: 1 }}>
            {t('accommodations.noOccupants')}
          </Typography>
        ) : (
          <TableContainer sx={{ mt: 1 }}>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>{t('accommodations.person')}</TableCell>
                  <TableCell>{t('accommodations.since')}</TableCell>
                  <TableCell>{t('accommodations.until')}</TableCell>
                  <TableCell>{t('accommodations.chargedTo')}</TableCell>
                  <TableCell align="right" />
                </TableRow>
              </TableHead>
              <TableBody>
                {rows.map((stay) => (
                  <TableRow key={stay.id} hover sx={{ cursor: 'pointer' }} onDoubleClick={() => setEditing(stay)}>
                    <TableCell>
                      <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                        <MuiLink component={RouterLink} to={paths.employeeDetail(stay.employeeId)}>
                          {stay.employeeName}
                        </MuiLink>
                        {isCurrent(stay) && <Chip size="small" color="success" variant="outlined" label={t('rates.active')} />}
                      </Stack>
                    </TableCell>
                    <TableCell>{formatDate(stay.startDate)}</TableCell>
                    <TableCell>{stay.endDate ? formatDate(stay.endDate) : t('rates.open')}</TableCell>
                    <TableCell>{stay.projectName || '—'}</TableCell>
                    <TableCell align="right" sx={{ whiteSpace: 'nowrap' }}>
                      <Tooltip title={t('common.edit')}>
                        <IconButton size="small" onClick={() => setEditing(stay)}>
                          <EditOutlined fontSize="small" />
                        </IconButton>
                      </Tooltip>
                      <Tooltip title={t('common.delete')}>
                        <IconButton size="small" onClick={() => remove.request(stay)}>
                          <DeleteOutlined fontSize="small" />
                        </IconButton>
                      </Tooltip>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        )}
      </CardContent>

      <StayDialog open={adding} accommodationId={accommodation.id} onClose={() => setAdding(false)} />
      <StayDialog
        open={!!editing}
        accommodationId={accommodation.id}
        editingStay={editing}
        onClose={() => setEditing(null)}
      />

      <ConfirmDialog
        open={!!remove.pending}
        title={t('accommodations.deleteStayTitle')}
        description={t('accommodations.deleteStayBody')}
        confirmLabel={t('common.delete')}
        destructive
        loading={remove.isDeleting}
        onConfirm={remove.confirm}
        onCancel={remove.cancel}
      />
    </Card>
  );
}

/**
 * Put a person into a place, or correct one such stay. Opened from an
 * accommodation (the place is fixed, pick the person) and from an employee (the
 * person is fixed, pick the place).
 */
export function StayDialog({
  open,
  accommodationId,
  employeeId,
  editingStay,
  onClose,
}: {
  open: boolean;
  /** Fixed when opened from an accommodation. */
  accommodationId?: string;
  /** Fixed when opened from an employee. */
  employeeId?: string;
  editingStay?: AccommodationStay | null;
  onClose: () => void;
}) {
  const t = useT();
  const add = useAddAccommodationStay();
  const update = useUpdateAccommodationStay();
  const { data: employees } = useAllEmployeesQuery();
  const { data: projects } = useAllProjectsQuery();
  const { data: accommodations } = useAllAccommodationsQuery();
  const isEditing = !!editingStay;

  const [pickedAccommodation, setPickedAccommodation] = useState('');
  const [pickedEmployee, setPickedEmployee] = useState('');
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [projectId, setProjectId] = useState('');
  const [note, setNote] = useState('');

  const resetAdd = add.reset;
  const resetUpdate = update.reset;

  useEffect(() => {
    if (!open) return;

    resetAdd();
    resetUpdate();

    if (editingStay) {
      setPickedAccommodation(editingStay.accommodationId);
      setPickedEmployee(editingStay.employeeId);
      setStartDate(editingStay.startDate);
      setEndDate(editingStay.endDate ?? '');
      setProjectId(editingStay.projectId ?? '');
      setNote(editingStay.note ?? '');
    } else {
      setPickedAccommodation(accommodationId ?? '');
      setPickedEmployee(employeeId ?? '');
      setStartDate(dateOnlyOffset(0));
      setEndDate('');
      setProjectId('');
      setNote('');
    }
  }, [open, editingStay, accommodationId, employeeId, resetAdd, resetUpdate]);

  const datesAreValid = !startDate || !endDate || endDate >= startDate;
  const canSubmit =
    !!startDate && datesAreValid && (isEditing || (pickedAccommodation !== '' && pickedEmployee !== ''));

  const mutation = isEditing ? update : add;
  const error = mutation.isError ? toApiError(mutation.error) : null;

  const submit = () => {
    const shared = {
      startDate,
      endDate: endDate || null,
      projectId: projectId || null,
      note: note.trim() || null,
    };

    if (isEditing) {
      update.mutate({ id: editingStay.id, input: shared }, { onSuccess: onClose });
    } else {
      add.mutate(
        { accommodationId: pickedAccommodation, input: { ...shared, employeeId: pickedEmployee } },
        { onSuccess: onClose },
      );
    }
  };

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>{isEditing ? t('accommodations.stayEditTitle') : t('accommodations.stayTitle')}</DialogTitle>
      <DialogContent>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          {t('accommodations.stayHint')}
        </Typography>

        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error.message}
          </Alert>
        )}

        <Grid container spacing={2} sx={{ mt: 0 }}>
          {!accommodationId && !isEditing && (
            <Grid size={12}>
              <TextField
                select
                fullWidth
                label={t('accommodations.accommodation')}
                value={pickedAccommodation}
                onChange={(event) => setPickedAccommodation(event.target.value)}
              >
                {(accommodations?.items ?? []).map((item) => (
                  <MenuItem key={item.id} value={item.id}>
                    {accommodationTitle(item)}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>
          )}

          {!employeeId && !isEditing && (
            <Grid size={12}>
              <TextField
                select
                fullWidth
                label={t('accommodations.person')}
                value={pickedEmployee}
                onChange={(event) => setPickedEmployee(event.target.value)}
              >
                {(employees?.items ?? []).map((item) => (
                  <MenuItem key={item.id} value={item.id}>
                    {item.fullName}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>
          )}

          <Grid size={{ xs: 12, sm: 6 }}>
            <TextField
              type="date"
              fullWidth
              required
              label={t('accommodations.since')}
              value={startDate}
              onChange={(event) => setStartDate(event.target.value)}
              slotProps={{ inputLabel: { shrink: true } }}
            />
          </Grid>
          <Grid size={{ xs: 12, sm: 6 }}>
            <TextField
              type="date"
              fullWidth
              label={t('accommodations.until')}
              value={endDate}
              onChange={(event) => setEndDate(event.target.value)}
              slotProps={{ inputLabel: { shrink: true } }}
              error={!datesAreValid}
              helperText={!datesAreValid ? t('rates.endsBeforeStart') : undefined}
            />
          </Grid>

          <Grid size={12}>
            <TextField
              select
              fullWidth
              label={t('accommodations.chargedTo')}
              value={projectId}
              onChange={(event) => setProjectId(event.target.value)}
            >
              <MenuItem value="">{t('accommodations.noProject')}</MenuItem>
              {(projects?.items ?? []).map((project) => (
                <MenuItem key={project.id} value={project.id}>
                  {project.name}
                </MenuItem>
              ))}
            </TextField>
          </Grid>

          <Grid size={12}>
            <TextField
              fullWidth
              multiline
              minRows={2}
              label={t('rates.note')}
              value={note}
              onChange={(event) => setNote(event.target.value)}
            />
          </Grid>
        </Grid>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button variant="contained" disabled={!canSubmit} loading={mutation.isPending} onClick={submit}>
          {t('common.save')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

// ---- charges --------------------------------------------------------------

const KIND_HINT: Record<AccommodationChargeKind, 'accommodations.chargeMonthlyHint' | 'accommodations.chargeDailyHint' | 'accommodations.chargeOneOffHint'> = {
  Monthly: 'accommodations.chargeMonthlyHint',
  DailyPerPerson: 'accommodations.chargeDailyHint',
  OneOff: 'accommodations.chargeOneOffHint',
};

const KIND_UNIT: Record<AccommodationChargeKind, 'accommodations.perMonth' | 'accommodations.perPersonDay' | 'accommodations.once'> = {
  Monthly: 'accommodations.perMonth',
  DailyPerPerson: 'accommodations.perPersonDay',
  OneOff: 'accommodations.once',
};

/** Rent, per-person daily charges and one-off fees; each recurring kind keeps its own history. */
function ChargesCard({ accommodationId }: { accommodationId: string }) {
  const t = useT();
  const { locale } = useI18n();
  const enumLabel = useEnumLabel();
  const [adding, setAdding] = useState(false);
  const [editing, setEditing] = useState<AccommodationRate | null>(null);

  const query = useMemo(
    () => ({
      accommodationId,
      pageNumber: 1,
      pageSize: 50,
      sortBy: 'startDate',
      sortDescending: true,
    }),
    [accommodationId],
  );

  const { data } = useAccommodationRatesQuery(query);
  const remove = useDeleteWithConfirm<AccommodationRate>(useDeleteAccommodationRate());
  const rows = data?.items ?? [];

  return (
    <Card>
      <CardContent>
        <Stack
          direction="row"
          useFlexGap
          sx={{ flexWrap: 'wrap', gap: 1, alignItems: 'center', justifyContent: 'space-between' }}
        >
          <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
            {t('accommodations.charges')}
          </Typography>
          <Button size="small" startIcon={<AddOutlined />} onClick={() => setAdding(true)}>
            {t('accommodations.addCharge')}
          </Button>
        </Stack>

        {rows.length === 0 ? (
          <Typography color="text.secondary" sx={{ mt: 1 }}>
            {t('accommodations.noRatesSentence')}
          </Typography>
        ) : (
          <TableContainer sx={{ mt: 1 }}>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>{t('accommodations.chargeKind')}</TableCell>
                  <TableCell>{t('common.status')}</TableCell>
                  <TableCell>{t('accommodations.provider')}</TableCell>
                  <TableCell align="right">{t('accommodations.chargeAmount')}</TableCell>
                  <TableCell>{t('accommodations.startDate')}</TableCell>
                  <TableCell>{t('accommodations.endDate')}</TableCell>
                  <TableCell align="right" />
                </TableRow>
              </TableHead>
              <TableBody>
                {rows.map((row) => (
                  <TableRow key={row.id} hover sx={{ cursor: 'pointer' }} onDoubleClick={() => setEditing(row)}>
                    <TableCell>{enumLabel('accommodationChargeKind', row.kind)}</TableCell>
                    <TableCell>
                      {row.kind === 'OneOff' ? (
                        <Chip size="small" variant="outlined" label={t('accommodations.once')} />
                      ) : row.endDate ? (
                        <Chip size="small" variant="outlined" label={t('rates.ended')} />
                      ) : (
                        <Chip size="small" color="success" variant="outlined" label={t('rates.active')} />
                      )}
                    </TableCell>
                    <TableCell>{row.provider || '—'}</TableCell>
                    <TableCell align="right" sx={{ fontWeight: 600, whiteSpace: 'nowrap' }}>
                      {formatMoney(row.amount, locale)}{' '}
                      <Typography component="span" variant="caption" color="text.secondary">
                        {t(KIND_UNIT[row.kind])}
                      </Typography>
                    </TableCell>
                    <TableCell>{formatDate(row.startDate)}</TableCell>
                    <TableCell>
                      {row.kind === 'OneOff' ? '—' : row.endDate ? formatDate(row.endDate) : t('rates.open')}
                    </TableCell>
                    <TableCell align="right">
                      <Tooltip title={t('common.delete')}>
                        <IconButton
                          size="small"
                          onClick={(event) => {
                            event.stopPropagation();
                            remove.request(row);
                          }}
                        >
                          <DeleteOutlined fontSize="small" />
                        </IconButton>
                      </Tooltip>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        )}
      </CardContent>

      <ChargeDialog open={adding} accommodationId={accommodationId} onClose={() => setAdding(false)} />
      <ChargeDialog
        open={!!editing}
        accommodationId={accommodationId}
        editingRate={editing}
        onClose={() => setEditing(null)}
      />

      <ConfirmDialog
        open={!!remove.pending}
        title={t('accommodations.deleteRateTitle')}
        description={t('accommodations.deleteRateBody')}
        confirmLabel={t('common.delete')}
        destructive
        loading={remove.isDeleting}
        onConfirm={remove.confirm}
        onCancel={remove.cancel}
      />
    </Card>
  );
}

function ChargeDialog({
  open,
  accommodationId,
  editingRate,
  onClose,
}: {
  open: boolean;
  accommodationId: string;
  editingRate?: AccommodationRate | null;
  onClose: () => void;
}) {
  const t = useT();
  const enumLabel = useEnumLabel();
  const set = useSetAccommodationRate();
  const update = useUpdateAccommodationRate();
  const isEditing = !!editingRate;

  const [kind, setKind] = useState<AccommodationChargeKind>('Monthly');
  const [amount, setAmount] = useState('');
  const [provider, setProvider] = useState('');
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [note, setNote] = useState('');

  const resetSet = set.reset;
  const resetUpdate = update.reset;

  useEffect(() => {
    if (!open) return;

    resetSet();
    resetUpdate();

    if (editingRate) {
      setKind(editingRate.kind);
      setAmount(String(editingRate.amount));
      setProvider(editingRate.provider ?? '');
      setStartDate(editingRate.startDate);
      setEndDate(editingRate.endDate ?? '');
      setNote(editingRate.note ?? '');
    } else {
      setKind('Monthly');
      setAmount('');
      setProvider('');
      setStartDate('');
      setEndDate('');
      setNote('');
    }
  }, [open, editingRate, resetSet, resetUpdate]);

  const isOneOff = kind === 'OneOff';
  const parsedAmount = Number(amount);
  const amountIsValid = amount.trim() !== '' && !Number.isNaN(parsedAmount) && parsedAmount > 0;
  const datesAreValid = isOneOff || !startDate || !endDate || endDate >= startDate;
  const canSubmit = amountIsValid && datesAreValid && (!isOneOff || !!startDate || !isEditing);

  const mutation = isEditing ? update : set;
  const error = mutation.isError ? toApiError(mutation.error) : null;

  const submit = () => {
    const shared = {
      accommodationId,
      kind,
      amount: parsedAmount,
      provider: provider.trim() || null,
      note: note.trim() || null,
    };

    if (isEditing) {
      update.mutate(
        { id: editingRate.id, input: { ...shared, startDate, endDate: isOneOff ? null : endDate || null } },
        { onSuccess: onClose },
      );
    } else {
      set.mutate(
        { ...shared, startDate: startDate || null, endDate: isOneOff ? null : endDate || null },
        { onSuccess: onClose },
      );
    }
  };

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>
        {isEditing ? t('accommodations.editChargeTitle') : t('accommodations.addCharge')}
      </DialogTitle>
      <DialogContent>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error.message}
          </Alert>
        )}

        <Grid container spacing={2} sx={{ mt: 0 }}>
          <Grid size={12}>
            <TextField
              select
              fullWidth
              disabled={isEditing}
              label={t('accommodations.chargeKind')}
              value={kind}
              onChange={(event) => setKind(event.target.value as AccommodationChargeKind)}
              helperText={t(KIND_HINT[kind])}
            >
              {accommodationChargeKinds.map((value) => (
                <MenuItem key={value} value={value}>
                  {enumLabel('accommodationChargeKind', value)}
                </MenuItem>
              ))}
            </TextField>
          </Grid>

          <Grid size={{ xs: 12, sm: 6 }}>
            <TextField
              type="number"
              fullWidth
              label={`${t('accommodations.chargeAmount')} (${t(KIND_UNIT[kind])})`}
              value={amount}
              onChange={(event) => setAmount(event.target.value)}
              error={amount.trim() !== '' && !amountIsValid}
              helperText={
                amount.trim() !== '' && !amountIsValid ? t('accommodations.mustBePositive') : undefined
              }
            />
          </Grid>

          <Grid size={{ xs: 12, sm: 6 }}>
            <TextField
              fullWidth
              label={t('accommodations.provider')}
              value={provider}
              onChange={(event) => setProvider(event.target.value)}
            />
          </Grid>

          <Grid size={{ xs: 12, sm: isOneOff ? 12 : 6 }}>
            <TextField
              type="date"
              fullWidth
              label={isOneOff ? t('accommodations.chargeDate') : t('accommodations.startDate')}
              value={startDate}
              onChange={(event) => setStartDate(event.target.value)}
              slotProps={{ inputLabel: { shrink: true } }}
            />
          </Grid>

          {!isOneOff && (
            <Grid size={{ xs: 12, sm: 6 }}>
              <TextField
                type="date"
                fullWidth
                label={t('accommodations.endDate')}
                value={endDate}
                onChange={(event) => setEndDate(event.target.value)}
                slotProps={{ inputLabel: { shrink: true } }}
                error={!datesAreValid}
                helperText={!datesAreValid ? t('rates.endsBeforeStart') : undefined}
              />
            </Grid>
          )}

          <Grid size={12}>
            <TextField
              fullWidth
              multiline
              minRows={2}
              label={t('rates.note')}
              value={note}
              onChange={(event) => setNote(event.target.value)}
            />
          </Grid>

          {isEditing && (
            <Grid size={12}>
              <AttachmentList
                ownerType="AccommodationRate"
                ownerId={editingRate.id}
                categories={['Contract', 'Other']}
              />
            </Grid>
          )}
        </Grid>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button variant="contained" disabled={!canSubmit} loading={mutation.isPending} onClick={submit}>
          {t('common.save')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

// ---- cost summary ---------------------------------------------------------

/** First and last day of the month `offset` months from this one. */
function monthWindow(offset: number): { from: string; to: string; label: Date } {
  const now = new Date();
  const first = new Date(now.getFullYear(), now.getMonth() + offset, 1);
  const last = new Date(first.getFullYear(), first.getMonth() + 1, 0);
  const pad = (n: number) => String(n).padStart(2, '0');
  const iso = (d: Date) => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;

  return { from: iso(first), to: iso(last), label: first };
}

/** What the place cost in a month, and whose it was: the point of tracking who lives where. */
function CostSummaryCard({ accommodationId }: { accommodationId: string }) {
  const t = useT();
  const { locale } = useI18n();
  const [offset, setOffset] = useState(0);
  const window = useMemo(() => monthWindow(offset), [offset]);
  const { data } = useAccommodationCostsQuery(accommodationId, { from: window.from, to: window.to });

  const monthName = new Intl.DateTimeFormat(locale === 'sr' ? 'sr-Latn' : 'en-GB', {
    month: 'long',
    year: 'numeric',
  }).format(window.label);

  return (
    <Card>
      <CardContent>
        <Stack
          direction="row"
          useFlexGap
          sx={{ flexWrap: 'wrap', gap: 1, alignItems: 'center', justifyContent: 'space-between' }}
        >
          <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
            {t('accommodations.costs')}
          </Typography>
          <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center' }}>
            <IconButton size="small" aria-label={t('accommodations.previousMonth')} onClick={() => setOffset((o) => o - 1)}>
              <ChevronLeftOutlined />
            </IconButton>
            <Typography sx={{ minWidth: 130, textAlign: 'center', textTransform: 'capitalize' }}>
              {monthName}
            </Typography>
            <IconButton size="small" aria-label={t('accommodations.nextMonth')} onClick={() => setOffset((o) => o + 1)}>
              <ChevronRightOutlined />
            </IconButton>
          </Stack>
        </Stack>

        {data && (
          <>
            <Stack direction="row" spacing={4} useFlexGap sx={{ mt: 2, flexWrap: 'wrap' }}>
              <Box>
                <Typography variant="caption" color="text.secondary">
                  {t('accommodations.costTotal')}
                </Typography>
                <Typography variant="h5" sx={{ fontWeight: 700 }}>
                  {formatMoney(data.total, locale)}
                </Typography>
              </Box>
              {data.monthlyPortion > 0 && (
                <Box>
                  <Typography variant="caption" color="text.secondary">
                    {t('accommodations.costMonthly')}
                  </Typography>
                  <Typography variant="h6">{formatMoney(data.monthlyPortion, locale)}</Typography>
                </Box>
              )}
              {data.dailyPortion > 0 && (
                <Box>
                  <Typography variant="caption" color="text.secondary">
                    {t('accommodations.costDaily')}
                  </Typography>
                  <Typography variant="h6">{formatMoney(data.dailyPortion, locale)}</Typography>
                </Box>
              )}
              {data.oneOffPortion > 0 && (
                <Box>
                  <Typography variant="caption" color="text.secondary">
                    {t('accommodations.costOneOff')}
                  </Typography>
                  <Typography variant="h6">{formatMoney(data.oneOffPortion, locale)}</Typography>
                </Box>
              )}
            </Stack>

            {data.vacancyCost > 0 && (
              <Alert severity="warning" sx={{ mt: 2 }}>
                {t('accommodations.costVacancy')}: <strong>{formatMoney(data.vacancyCost, locale)}</strong> (
                {t('accommodations.vacancyDays', { days: data.vacantDays })})
              </Alert>
            )}

            <Grid container spacing={3} sx={{ mt: 0.5 }}>
              {data.byEmployee.length > 0 && (
                <Grid size={{ xs: 12, md: 7 }}>
                  <Typography variant="subtitle2" sx={{ mb: 0.5 }}>
                    {t('accommodations.byPerson')}
                  </Typography>
                  <TableContainer>
                    <Table size="small">
                      <TableHead>
                        <TableRow>
                          <TableCell>{t('accommodations.person')}</TableCell>
                          <TableCell align="right">{t('accommodations.personDays')}</TableCell>
                          <TableCell align="right">{t('accommodations.costTotal')}</TableCell>
                        </TableRow>
                      </TableHead>
                      <TableBody>
                        {data.byEmployee.map((row) => (
                          <TableRow key={row.employeeId}>
                            <TableCell>
                              <MuiLink component={RouterLink} to={paths.employeeDetail(row.employeeId)}>
                                {row.employeeName}
                              </MuiLink>
                            </TableCell>
                            <TableCell align="right">{row.personDays}</TableCell>
                            <TableCell align="right">{formatMoney(row.cost, locale)}</TableCell>
                          </TableRow>
                        ))}
                      </TableBody>
                    </Table>
                  </TableContainer>
                </Grid>
              )}
              {data.byProject.length > 0 && (
                <Grid size={{ xs: 12, md: 5 }}>
                  <Typography variant="subtitle2" sx={{ mb: 0.5 }}>
                    {t('accommodations.byProject')}
                  </Typography>
                  <TableContainer>
                    <Table size="small">
                      <TableBody>
                        {data.byProject.map((row) => (
                          <TableRow key={row.projectId ?? 'none'}>
                            <TableCell>{row.projectName ?? t('accommodations.noProject')}</TableCell>
                            <TableCell align="right">{formatMoney(row.cost, locale)}</TableCell>
                          </TableRow>
                        ))}
                      </TableBody>
                    </Table>
                  </TableContainer>
                </Grid>
              )}
            </Grid>
          </>
        )}
      </CardContent>
    </Card>
  );
}
