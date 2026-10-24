import { AttachFileOutlined, CloseOutlined } from '@mui/icons-material';
import {
  Autocomplete,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  IconButton,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import { useState } from 'react';

import { weeklyReportTypes, type WeeklyReportType, type WeeklySiteReport } from '../../api/types';
import { canManageAssignments } from '../../auth/authHelpers';
import { useAuth } from '../../auth/useAuth';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useT } from '../../i18n/useI18n';
import { getLastCompletedIsoWeek, isoWeekMonths } from '../../utils/isoWeek';
import { useAllEmployeesQuery } from '../employees/useEmployees';
import { useFileDrop } from '../files/useFileDrop';
import { useCreateWeeklySiteReport, useReportableProjectsQuery } from './useWeeklySiteReports';

interface LineRow {
  employeeId: string;
  name: string;
  first: string;
  second: string;
}

function toNumber(text: string): number {
  const value = Number(text.replace(',', '.'));
  return Number.isFinite(value) ? value : 0;
}

const monthLabel = (month: { year: number; month: number }) => `${String(month.month).padStart(2, '0')}/${month.year}`;

/**
 * Sending a weekly report: the site and week, the workers with their hours, and the pictures of the signed sheet
 * (as many as the sheet has pages). The workers can be anyone, not only those posted to the site. For a week that
 * runs over a month's end the hours of each worker are given for both months.
 */
export function SubmitReportDialog({
  open,
  onClose,
  onCreated,
}: {
  open: boolean;
  onClose: () => void;
  /** The report that was made (or the one it was added to), so the screen can open it for checking. */
  onCreated?: (report: WeeklySiteReport) => void;
}) {
  const t = useT();
  const enumLabel = useEnumLabel();
  const { user } = useAuth();
  const canPickEmployee = canManageAssignments(user);
  const { data: projects } = useReportableProjectsQuery();
  const { data: employeesPage } = useAllEmployeesQuery();
  const create = useCreateWeeklySiteReport();

  const defaultWeek = getLastCompletedIsoWeek();
  const [projectId, setProjectId] = useState('');
  const [isoYear, setIsoYear] = useState(defaultWeek.isoYear);
  const [isoWeek, setIsoWeek] = useState(defaultWeek.isoWeek);
  const [type, setType] = useState<WeeklyReportType>('SignedHours');
  const [submittedByEmployeeId, setSubmittedByEmployeeId] = useState('');
  const [quantity, setQuantity] = useState('');
  const [note, setNote] = useState('');
  const [sourceNote, setSourceNote] = useState('');
  const [files, setFiles] = useState<File[]>([]);
  const [lines, setLines] = useState<LineRow[]>([]);

  const addFiles = (added: File[]) => setFiles((current) => [...current, ...added]);
  const { isOver, bind } = useFileDrop({ onFiles: addFiles });

  const months = isoWeekMonths(isoYear, isoWeek);
  const split = months.second !== null;
  const signed = type === 'SignedHours';
  const linesTotal = lines.reduce((sum, line) => sum + toNumber(line.first) + toNumber(line.second), 0);
  const linesValid = lines.every((line) => toNumber(line.first) >= 0 && toNumber(line.second) >= 0 && toNumber(line.first) + toNumber(line.second) <= 168);
  const canSend = !!projectId && files.length > 0 && linesValid && (!signed || lines.length > 0 || !!quantity);

  const reset = () => {
    setProjectId('');
    setSubmittedByEmployeeId('');
    setQuantity('');
    setNote('');
    setSourceNote('');
    setFiles([]);
    setLines([]);
  };

  const submit = () => {
    if (!canSend) return;

    create.mutate(
      {
        projectId,
        isoYear,
        isoWeek,
        type,
        submittedByEmployeeId: canPickEmployee && submittedByEmployeeId ? submittedByEmployeeId : null,
        quantity: signed && lines.length === 0 && quantity ? Number(quantity) : null,
        note: note.trim() || null,
        sourceNote: canPickEmployee ? sourceNote.trim() || null : null,
        files,
        lines: signed
          ? lines.map((line) => ({
              employeeId: line.employeeId,
              hours: toNumber(line.first) + toNumber(line.second),
              hoursSecondMonth: split ? toNumber(line.second) : 0,
            }))
          : [],
      },
      {
        onSuccess: (report) => {
          reset();
          onClose();
          onCreated?.(report);
        },
      },
    );
  };

  const addable = (employeesPage?.items ?? []).filter((e) => !lines.some((line) => line.employeeId === e.id));

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>{canPickEmployee ? t('weeklyReports.enter') : t('weeklyReports.submit')}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          <FormControl fullWidth>
            <InputLabel id="wr-project-label">{t('weeklyReports.project')}</InputLabel>
            <Select
              labelId="wr-project-label"
              label={t('weeklyReports.project')}
              value={projectId}
              onChange={(event) => setProjectId(event.target.value)}
            >
              {(projects ?? []).map((p) => (
                <MenuItem key={p.id} value={p.id}>
                  {p.name}
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          <Stack direction="row" spacing={1}>
            <TextField
              type="number"
              label={t('weeklyReports.isoYear')}
              value={isoYear}
              onChange={(event) => setIsoYear(Number(event.target.value) || isoYear)}
              sx={{ flex: 1 }}
            />
            <TextField
              type="number"
              label={t('weeklyReports.isoWeek')}
              value={isoWeek}
              onChange={(event) => setIsoWeek(Math.min(53, Math.max(1, Number(event.target.value) || 1)))}
              slotProps={{ htmlInput: { min: 1, max: 53 } }}
              sx={{ flex: 1 }}
            />
          </Stack>

          <FormControl fullWidth>
            <InputLabel id="wr-type-label">{t('weeklyReports.type')}</InputLabel>
            <Select
              labelId="wr-type-label"
              label={t('weeklyReports.type')}
              value={type}
              onChange={(event) => setType(event.target.value as WeeklyReportType)}
            >
              {weeklyReportTypes.map((value) => (
                <MenuItem key={value} value={value}>
                  {enumLabel('weeklyReportType', value)}
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          {canPickEmployee && (
            <FormControl fullWidth>
              {/* `shrink` forced on: `displayEmpty` always shows real text in the field, but MUI's label only
                  floats up on its own when `value` is non-empty. */}
              <InputLabel id="wr-employee-label" shrink>
                {t('weeklyReports.submittedByEmployee')}
              </InputLabel>
              <Select
                labelId="wr-employee-label"
                label={t('weeklyReports.submittedByEmployee')}
                value={submittedByEmployeeId}
                onChange={(event) => setSubmittedByEmployeeId(event.target.value)}
                displayEmpty
              >
                <MenuItem value="">
                  <em>{t('weeklyReports.submittedByEmployeeAuto')}</em>
                </MenuItem>
                {(employeesPage?.items ?? []).map((employee) => (
                  <MenuItem key={employee.id} value={employee.id}>
                    {employee.firstName} {employee.lastName}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          )}

          {canPickEmployee && (
            <TextField
              label={t('weeklyReports.sourceNote')}
              placeholder={t('weeklyReports.sourceNotePlaceholder')}
              helperText={t('weeklyReports.sourceNoteHelp')}
              value={sourceNote}
              onChange={(event) => setSourceNote(event.target.value)}
              slotProps={{ htmlInput: { maxLength: 500 } }}
              fullWidth
            />
          )}

          {signed && (
            <Box sx={{ border: 1, borderColor: 'divider', borderRadius: 1, p: 1.5 }}>
              <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 0.5 }}>
                {t('weeklyReports.submit.workers')}
              </Typography>
              {split && months.second && (
                <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 1 }}>
                  {t('weeklyReports.submit.splitHint', { first: monthLabel(months.first), second: monthLabel(months.second) })}
                </Typography>
              )}

              {lines.map((line) => (
                <Stack key={line.employeeId} direction="row" spacing={1} sx={{ alignItems: 'center', py: 0.5 }}>
                  <Typography sx={{ flex: 1, minWidth: 0 }} noWrap>
                    {line.name}
                  </Typography>
                  <TextField
                    size="small"
                    value={line.first}
                    onChange={(event) =>
                      setLines((current) => current.map((l) => (l.employeeId === line.employeeId ? { ...l, first: event.target.value } : l)))
                    }
                    label={split ? monthLabel(months.first) : t('weeklyReports.hours')}
                    slotProps={{ htmlInput: { inputMode: 'decimal', style: { textAlign: 'right' } } }}
                    sx={{ width: 110 }}
                  />
                  {split && months.second && (
                    <TextField
                      size="small"
                      value={line.second}
                      onChange={(event) =>
                        setLines((current) => current.map((l) => (l.employeeId === line.employeeId ? { ...l, second: event.target.value } : l)))
                      }
                      label={monthLabel(months.second)}
                      slotProps={{ htmlInput: { inputMode: 'decimal', style: { textAlign: 'right' } } }}
                      sx={{ width: 110 }}
                    />
                  )}
                  <Tooltip title={t('weeklyReports.review.removeRow')}>
                    <IconButton
                      size="small"
                      aria-label={t('weeklyReports.review.removeRow')}
                      onClick={() => setLines((current) => current.filter((l) => l.employeeId !== line.employeeId))}
                    >
                      <CloseOutlined fontSize="small" />
                    </IconButton>
                  </Tooltip>
                </Stack>
              ))}

              <Autocomplete
                size="small"
                sx={{ mt: 1 }}
                options={addable}
                value={null}
                blurOnSelect
                clearOnBlur
                getOptionLabel={(employee) => `${employee.firstName} ${employee.lastName}`}
                onChange={(_event, employee) => {
                  if (!employee) return;
                  setLines((current) => [
                    ...current,
                    { employeeId: employee.id, name: `${employee.firstName} ${employee.lastName}`, first: '', second: '' },
                  ]);
                }}
                renderInput={(params) => <TextField {...params} label={t('weeklyReports.review.addWorker')} />}
              />

              {lines.length > 0 ? (
                <Typography sx={{ mt: 1, textAlign: 'right' }}>
                  {t('weeklyReports.review.total')}: <strong>{String(Math.round(linesTotal * 100) / 100).replace('.', ',')} h</strong>
                </Typography>
              ) : (
                <TextField
                  type="number"
                  label={t('weeklyReports.submit.totalOnly')}
                  value={quantity}
                  onChange={(event) => setQuantity(event.target.value)}
                  fullWidth
                  sx={{ mt: 1 }}
                />
              )}
            </Box>
          )}

          <TextField
            label={t('weeklyReports.note')}
            value={note}
            onChange={(event) => setNote(event.target.value)}
            multiline
            minRows={2}
            fullWidth
          />

          <Box
            {...bind}
            sx={{
              border: 2,
              borderStyle: 'dashed',
              borderColor: isOver ? 'primary.main' : 'divider',
              borderRadius: 1,
              p: 1.5,
            }}
          >
            <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>
              {t('weeklyReports.submit.pictures')}
            </Typography>
            <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 1 }}>
              {t('weeklyReports.submit.picturesHint')}
            </Typography>
            {files.map((file, index) => (
              <Stack key={`${file.name}-${index}`} direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                <Typography sx={{ flex: 1, minWidth: 0 }} noWrap>
                  {index + 1}. {file.name}
                </Typography>
                <IconButton
                  size="small"
                  aria-label={t('weeklyReports.review.removeRow')}
                  onClick={() => setFiles((current) => current.filter((_f, i) => i !== index))}
                >
                  <CloseOutlined fontSize="small" />
                </IconButton>
              </Stack>
            ))}
            <Button variant="outlined" component="label" startIcon={<AttachFileOutlined />} sx={{ mt: 1 }} color={files.length ? 'success' : 'primary'}>
              {files.length ? t('weeklyReports.submit.addMore') : t('weeklyReports.attachFile')}
              <input
                hidden
                multiple
                type="file"
                accept=".pdf,.jpg,.jpeg,.png,.webp,.heic"
                onChange={(event) => {
                  addFiles(Array.from(event.target.files ?? []));
                  event.target.value = '';
                }}
              />
            </Button>
          </Box>

          {create.error && (
            <Typography variant="body2" color="error">
              {create.error.message}
            </Typography>
          )}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button variant="contained" onClick={submit} disabled={!canSend || create.isPending}>
          {t('common.save')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
