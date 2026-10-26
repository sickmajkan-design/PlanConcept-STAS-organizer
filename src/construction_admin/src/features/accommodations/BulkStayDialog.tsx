import { DeleteOutlined } from '@mui/icons-material';
import {
  Alert,
  Autocomplete,
  Box,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  LinearProgress,
  MenuItem,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material';
import { useState } from 'react';

import { toApiError } from '../../api/apiError';
import type { AccommodationStayRowResult, AccommodationStaysResult } from '../../api/accommodations';
import { employeesApi } from '../../api/employees';
import { useT } from '../../i18n/useI18n';
import type { MessageKey } from '../../i18n/en';
import { dateOnlyOffset, formatDate } from '../../utils/formatting';
import { useAllEmployeesQuery } from '../employees/useEmployees';
import { useAllProjectsQuery } from '../projects/useProjects';
import { usePreviewAccommodationStays, useSaveAccommodationStays } from './useAccommodations';

interface Row {
  employeeId: string;
  name: string;
  startDate: string;
  endDate: string;
  projectId: string;
  note: string;
}

type Step = 'choose' | 'check' | 'done';

const TONE: Record<string, 'success' | 'warning' | 'error' | 'default'> = {
  Ok: 'success',
  OverCapacity: 'warning',
  Conflict: 'error',
  Invalid: 'error',
};

/**
 * Several people into one accommodation at once, each with their own dates and project: choose them, look at what
 * would happen to each, then write. What is in order is written and the rest is reported with the reason.
 */
export function BulkStayDialog({
  open,
  accommodationId,
  title,
  onClose,
}: {
  open: boolean;
  accommodationId: string;
  title: string;
  onClose: () => void;
}) {
  const t = useT();
  const { data: employees } = useAllEmployeesQuery();
  const { data: projects } = useAllProjectsQuery();
  const preview = usePreviewAccommodationStays();
  const save = useSaveAccommodationStays();

  const [step, setStep] = useState<Step>('choose');
  const [rows, setRows] = useState<Row[]>([]);
  const [commonStart, setCommonStart] = useState(dateOnlyOffset(0));
  const [commonEnd, setCommonEnd] = useState('');
  const [result, setResult] = useState<AccommodationStaysResult | null>(null);
  const [loadingTeam, setLoadingTeam] = useState(false);

  const projectList = projects?.items ?? [];
  const taken = new Set(rows.map((r) => r.employeeId));

  const reset = () => {
    setStep('choose');
    setRows([]);
    setResult(null);
    setCommonStart(dateOnlyOffset(0));
    setCommonEnd('');
    preview.reset();
    save.reset();
  };

  const close = () => {
    reset();
    onClose();
  };

  /** The project a person is posted to now, when it is one and the same by name; left empty otherwise. */
  const defaultProject = (names: string[]) => {
    const matches = projectList.filter((p) => names.includes(p.name));
    return matches.length === 1 ? matches[0].id : '';
  };

  const addPeople = (people: { id: string; name: string; projectId: string }[]) =>
    setRows((current) => [
      ...current,
      ...people
        .filter((p) => !current.some((r) => r.employeeId === p.id))
        .map((p) => ({
          employeeId: p.id,
          name: p.name,
          startDate: commonStart,
          endDate: commonEnd,
          projectId: p.projectId,
          note: '',
        })),
    ]);

  const addTeam = async (projectId: string) => {
    setLoadingTeam(true);
    try {
      const team = await employeesApi.list({ pageNumber: 1, pageSize: 100, status: 'Active', projectId });
      addPeople(team.items.map((e) => ({ id: e.id, name: e.fullName, projectId })));
    } finally {
      setLoadingTeam(false);
    }
  };

  const change = (employeeId: string, patch: Partial<Row>) =>
    setRows((current) => current.map((r) => (r.employeeId === employeeId ? { ...r, ...patch } : r)));

  const payload = (preview: boolean) => ({
    preview,
    stays: rows.map((r) => ({
      employeeId: r.employeeId,
      startDate: r.startDate || null,
      endDate: r.endDate || null,
      projectId: r.projectId || null,
      note: r.note.trim() || null,
    })),
  });

  const datesOk = rows.every((r) => !r.endDate || !r.startDate || r.endDate >= r.startDate);
  const canContinue = rows.length > 0 && datesOk;

  const check = () =>
    preview.mutate(
      { accommodationId, input: payload(true) },
      {
        onSuccess: (data) => {
          setResult(data);
          setStep('check');
        },
      },
    );

  const write = () =>
    save.mutate(
      { accommodationId, input: payload(false) },
      {
        onSuccess: (data) => {
          setResult(data);
          setStep('done');
        },
      },
    );

  /** Back to the choice with only the people who were not written, to fix and send again. */
  const retryFailed = () => {
    const failed = new Set((result?.rows ?? []).filter((r) => !r.stayId).map((r) => r.employeeId));
    setRows((current) => current.filter((r) => failed.has(r.employeeId)));
    setResult(null);
    save.reset();
    preview.reset();
    setStep('choose');
  };

  const reasonText = (row: AccommodationStayRowResult) =>
    row.reason
      ? t(`accommodations.bulk.reason.${row.reason}` as MessageKey, {
          place: row.place ?? '',
          until: row.placeUntil ? formatDate(row.placeUntil) : '…',
        })
      : '';

  const error = preview.isError ? toApiError(preview.error) : save.isError ? toApiError(save.error) : null;
  const writable = (result?.rows ?? []).filter((r) => r.status === 'Ok' || r.status === 'OverCapacity').length;
  const beds = result?.beds ?? null;

  return (
    <Dialog open={open} onClose={close} fullWidth maxWidth="lg">
      <DialogTitle>
        {t('accommodations.bulk.title')}
        <Typography variant="body2" color="text.secondary">
          {title}
        </Typography>
      </DialogTitle>

      <DialogContent>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error.message}
          </Alert>
        )}

        {step === 'choose' && (
          <Stack spacing={2} sx={{ mt: 0.5 }}>
            <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
              <Autocomplete
                multiple
                sx={{ flex: 1 }}
                options={(employees?.items ?? []).filter((e) => !taken.has(e.id))}
                getOptionLabel={(e) => e.fullName}
                value={[]}
                filterSelectedOptions
                onChange={(_event, picked) =>
                  addPeople(picked.map((e) => ({ id: e.id, name: e.fullName, projectId: defaultProject(e.currentProjectNames) })))
                }
                renderInput={(params) => <TextField {...params} label={t('accommodations.bulk.addPeople')} />}
              />
              <Autocomplete
                sx={{ flex: 1 }}
                options={projectList}
                getOptionLabel={(p) => p.name}
                value={null}
                loading={loadingTeam}
                onChange={(_event, project) => project && void addTeam(project.id)}
                renderInput={(params) => <TextField {...params} label={t('accommodations.bulk.addTeam')} helperText={t('accommodations.bulk.addTeamHint')} />}
              />
            </Stack>

            <Stack direction="row" spacing={1.5} useFlexGap sx={{ flexWrap: 'wrap', alignItems: 'center' }}>
              <TextField
                type="date"
                size="small"
                label={t('accommodations.since')}
                value={commonStart}
                onChange={(e) => setCommonStart(e.target.value)}
                slotProps={{ inputLabel: { shrink: true } }}
              />
              <TextField
                type="date"
                size="small"
                label={t('accommodations.until')}
                value={commonEnd}
                onChange={(e) => setCommonEnd(e.target.value)}
                slotProps={{ inputLabel: { shrink: true } }}
              />
              <Button
                variant="outlined"
                disabled={rows.length === 0}
                onClick={() => setRows((current) => current.map((r) => ({ ...r, startDate: commonStart, endDate: commonEnd })))}
              >
                {t('accommodations.bulk.applyAll')}
              </Button>
              <Typography variant="body2" color="text.secondary">
                {t('accommodations.bulk.people', { count: rows.length })}
              </Typography>
            </Stack>

            {rows.length === 0 ? (
              <Typography color="text.secondary">{t('accommodations.bulk.empty')}</Typography>
            ) : (
              <Box sx={{ overflowX: 'auto' }}>
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>{t('accommodations.person')}</TableCell>
                      <TableCell>{t('accommodations.since')}</TableCell>
                      <TableCell>{t('accommodations.until')}</TableCell>
                      <TableCell>{t('accommodations.chargedTo')}</TableCell>
                      <TableCell>{t('rates.note')}</TableCell>
                      <TableCell />
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {rows.map((row) => {
                      const bad = !!row.endDate && !!row.startDate && row.endDate < row.startDate;

                      return (
                        <TableRow key={row.employeeId}>
                          <TableCell sx={{ whiteSpace: 'nowrap' }}>{row.name}</TableCell>
                          <TableCell>
                            <TextField type="date" size="small" value={row.startDate} onChange={(e) => change(row.employeeId, { startDate: e.target.value })} />
                          </TableCell>
                          <TableCell>
                            <TextField
                              type="date"
                              size="small"
                              value={row.endDate}
                              error={bad}
                              onChange={(e) => change(row.employeeId, { endDate: e.target.value })}
                            />
                          </TableCell>
                          <TableCell sx={{ minWidth: 200 }}>
                            <TextField
                              select
                              size="small"
                              fullWidth
                              value={row.projectId}
                              slotProps={{ select: { displayEmpty: true } }}
                              onChange={(e) => change(row.employeeId, { projectId: e.target.value })}
                            >
                              <MenuItem value="">{t('accommodations.noProject')}</MenuItem>
                              {projectList.map((p) => (
                                <MenuItem key={p.id} value={p.id}>
                                  {p.name}
                                </MenuItem>
                              ))}
                            </TextField>
                          </TableCell>
                          <TableCell sx={{ minWidth: 160 }}>
                            <TextField size="small" fullWidth value={row.note} onChange={(e) => change(row.employeeId, { note: e.target.value })} />
                          </TableCell>
                          <TableCell>
                            <IconButton size="small" aria-label={t('common.delete')} onClick={() => setRows((c) => c.filter((r) => r.employeeId !== row.employeeId))}>
                              <DeleteOutlined fontSize="small" />
                            </IconButton>
                          </TableCell>
                        </TableRow>
                      );
                    })}
                  </TableBody>
                </Table>
              </Box>
            )}
          </Stack>
        )}

        {(step === 'check' || step === 'done') && result && (
          <Stack spacing={2} sx={{ mt: 0.5 }}>
            {step === 'check' && (
              <Box sx={{ border: 1, borderColor: 'divider', borderRadius: 1.5, p: 1.5 }}>
                <Stack direction="row" spacing={1} useFlexGap sx={{ justifyContent: 'space-between', flexWrap: 'wrap', mb: 1 }}>
                  <Typography sx={{ fontWeight: 700 }}>{t('accommodations.bulk.occupancy')}</Typography>
                  {beds === null ? (
                    <Chip size="small" label={t('accommodations.bulk.noBeds')} />
                  ) : (
                    <Chip
                      size="small"
                      color={result.peakOccupancy > beds ? 'warning' : 'success'}
                      label={t('accommodations.bulk.peak', { peak: result.peakOccupancy, beds })}
                    />
                  )}
                </Stack>
                {beds !== null && (
                  <LinearProgress
                    variant="determinate"
                    color={result.peakOccupancy > beds ? 'warning' : 'primary'}
                    value={Math.min(100, (result.peakOccupancy / Math.max(1, beds)) * 100)}
                    sx={{ height: 10, borderRadius: 5 }}
                  />
                )}
                {beds !== null && result.peakOccupancy > beds && (
                  <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                    {t('accommodations.bulk.overHint', { date: result.peakDate ? formatDate(result.peakDate) : '' })}
                  </Typography>
                )}
              </Box>
            )}

            {step === 'done' && (
              <Alert severity={result.saved === result.rows.length ? 'success' : 'warning'}>
                {t('accommodations.bulk.saved', { saved: result.saved, total: result.rows.length })}
                {' '}
                {result.saved > 0 && t('accommodations.bulk.notified')}
              </Alert>
            )}

            <Box sx={{ overflowX: 'auto' }}>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>{t('accommodations.person')}</TableCell>
                    <TableCell>{t('accommodations.bulk.period')}</TableCell>
                    <TableCell>{t('accommodations.chargedTo')}</TableCell>
                    <TableCell>{t('accommodations.bulk.outcome')}</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {result.rows.map((row) => (
                    <TableRow key={row.employeeId}>
                      <TableCell sx={{ whiteSpace: 'nowrap' }}>{row.employeeName ?? '—'}</TableCell>
                      <TableCell sx={{ whiteSpace: 'nowrap' }}>
                        {formatDate(row.startDate)} – {row.endDate ? formatDate(row.endDate) : '…'}
                      </TableCell>
                      <TableCell>{row.projectName ?? '—'}</TableCell>
                      <TableCell>
                        <Chip
                          size="small"
                          color={step === 'done' && row.stayId ? 'success' : TONE[row.status]}
                          label={
                            step === 'done' && row.stayId
                              ? t('accommodations.bulk.housed')
                              : t(`accommodations.bulk.status.${row.status}` as MessageKey)
                          }
                        />
                        {row.reason && (
                          <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
                            {reasonText(row)}
                          </Typography>
                        )}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </Box>
          </Stack>
        )}
      </DialogContent>

      <DialogActions>
        {step === 'choose' && (
          <>
            <Button onClick={close}>{t('common.cancel')}</Button>
            <Button variant="contained" disabled={!canContinue} loading={preview.isPending} onClick={check}>
              {t('accommodations.bulk.next')}
            </Button>
          </>
        )}
        {step === 'check' && (
          <>
            <Button onClick={() => setStep('choose')}>{t('accommodations.bulk.back')}</Button>
            <Button variant="contained" disabled={writable === 0} loading={save.isPending} onClick={write}>
              {t('accommodations.bulk.save', { count: writable })}
            </Button>
          </>
        )}
        {step === 'done' && (
          <>
            {result && result.saved < result.rows.length && (
              <Button onClick={retryFailed}>{t('accommodations.bulk.retry')}</Button>
            )}
            <Button variant="contained" onClick={close}>
              {t('accommodations.bulk.done')}
            </Button>
          </>
        )}
      </DialogActions>
    </Dialog>
  );
}
