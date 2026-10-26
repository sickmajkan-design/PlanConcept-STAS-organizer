import {
  Alert,
  Button,
  Checkbox,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
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
import type { Tool } from '../../api/types';
import { useT } from '../../i18n/useI18n';
import { useCopyTool } from './useTools';

/** The name each copy gets: `{n}` becomes its number, or the number is added at the end. */
export function copyName(template: string, number: number, width: number): string {
  const text = template.includes('{n}') ? template : `${template.trimEnd()} {n}`;
  return text.replace('{n}', String(number).padStart(width, '0')).trim();
}

/**
 * Makes copies of a tool or a case of tools, each with its own name and QR code. Everything tied to the one physical
 * piece (serial number, holder, history, documents) stays behind.
 */
export function CopyToolDialog({ tool, onClose, onDone }: { tool: Tool; onClose: () => void; onDone: (copies: Tool[]) => void }) {
  const t = useT();
  const copy = useCopyTool(tool.id);

  const [count, setCount] = useState('3');
  const [template, setTemplate] = useState(`${tool.name} {n}`);
  const [start, setStart] = useState('1');
  const [width, setWidth] = useState('3');
  const [copyItems, setCopyItems] = useState(true);
  const [copyPrices, setCopyPrices] = useState(true);
  const [copyBranch, setCopyBranch] = useState(false);

  const n = Math.max(1, Math.min(50, Number(count) || 1));
  const names = Array.from({ length: n }, (_, i) => copyName(template, (Number(start) || 0) + i, Number(width) || 0));

  const submit = async () => {
    const created = await copy.mutateAsync({
      count: n,
      nameTemplate: template,
      startNumber: Number(start) || 0,
      numberWidth: Number(width) || 0,
      copyItems,
      copyPrices,
      copyBranch,
    });
    onDone(created);
  };

  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>{t('toolCopy.title', { name: tool.name })}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
            <TextField label={t('toolCopy.count')} type="number" value={count} onChange={(e) => setCount(e.target.value)} slotProps={{ htmlInput: { min: 1, max: 50 } }} sx={{ width: { sm: 140 } }} />
            <TextField label={t('toolCopy.template')} value={template} onChange={(e) => setTemplate(e.target.value)} sx={{ flex: 1 }} />
          </Stack>
          <Stack direction="row" spacing={2}>
            <TextField label={t('toolCopy.start')} type="number" value={start} onChange={(e) => setStart(e.target.value)} />
            <TextField label={t('toolCopy.width')} type="number" value={width} onChange={(e) => setWidth(e.target.value)} slotProps={{ htmlInput: { min: 0, max: 6 } }} />
          </Stack>

          <Stack>
            {tool.isKit && (
              <>
                <FormControlLabel control={<Checkbox checked={copyItems} onChange={(e) => setCopyItems(e.target.checked)} />} label={t('toolCopy.copyItems')} />
                <FormControlLabel
                  control={<Checkbox checked={copyPrices} disabled={!copyItems} onChange={(e) => setCopyPrices(e.target.checked)} />}
                  label={t('toolCopy.copyPrices')}
                />
              </>
            )}
            <FormControlLabel control={<Checkbox checked={copyBranch} onChange={(e) => setCopyBranch(e.target.checked)} />} label={t('toolCopy.copyBranch')} />
          </Stack>

          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>#</TableCell>
                <TableCell>{t('toolCopy.newName')}</TableCell>
                <TableCell>{t('tools.qrCode')}</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {names.slice(0, 8).map((name, i) => (
                <TableRow key={i}>
                  <TableCell>{i + 1}</TableCell>
                  <TableCell>{name}</TableCell>
                  <TableCell>
                    <Typography variant="body2" color="text.secondary">
                      {t('toolCopy.qrAuto')}
                    </Typography>
                  </TableCell>
                </TableRow>
              ))}
              {names.length > 8 && (
                <TableRow>
                  <TableCell colSpan={3}>… +{names.length - 8}</TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>

          <Typography variant="caption" color="text.secondary">
            {t('toolCopy.note')}
          </Typography>
          {copy.isError && <Alert severity="error">{toApiError(copy.error).message}</Alert>}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button variant="contained" disabled={copy.isPending || template.trim() === ''} onClick={() => void submit()}>
          {t('toolCopy.create', { count: n })}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
