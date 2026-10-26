import { AddOutlined, DeleteOutlined, MoreVertOutlined } from '@mui/icons-material';
import {
  Alert,
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
  IconButton,
  Menu,
  MenuItem,
  Radio,
  RadioGroup,
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
import type { ToolKit, ToolKitAction, ToolKitItem, ToolKitItemState, ToolKitPayoutMethod } from '../../api/tools';
import { useI18n, useT } from '../../i18n/useI18n';
import type { MessageKey } from '../../i18n/en';
import { formatDate, formatMoney } from '../../utils/formatting';
import { useAllEmployeesQuery } from '../employees/useEmployees';
import {
  useActOnKitItem,
  useAddKitItem,
  useRemoveKitItem,
  useSettleKitEvent,
  useToolKitQuery,
  useUpdateKitItem,
} from './useTools';

/** What can be done to an item, by where it stands. */
const NEXT: Record<ToolKitItemState, ToolKitAction[]> = {
  InKit: ['ReportMissing', 'ReportBroken'],
  Missing: ['ChargeWorker', 'ReplacementBought', 'WriteOff', 'Restore'],
  Broken: ['ReceiveBroken'],
  BrokenHandedIn: ['ReplacementBought', 'WriteOff', 'Restore'],
  Charged: ['Restore'],
  WrittenOff: ['Restore'],
};

const STATE_COLOR: Record<ToolKitItemState, 'default' | 'success' | 'warning' | 'error'> = {
  InKit: 'success',
  Missing: 'error',
  Broken: 'warning',
  BrokenHandedIn: 'warning',
  Charged: 'error',
  WrittenOff: 'default',
};

/** Sum and worker are asked for on these two. The rest only take a note. */
const MONEY: ToolKitAction[] = ['ChargeWorker', 'ReplacementBought'];

/** What a case of tools is made of, what is wrong with it, and what is owed because of it. */
export function ToolKitCard({ toolId, holderId }: { toolId: string; holderId: string | null }) {
  const t = useT();
  const { locale } = useI18n();
  const { data: kit } = useToolKitQuery(toolId);
  const add = useAddKitItem(toolId);
  const update = useUpdateKitItem(toolId);
  const remove = useRemoveKitItem(toolId);
  const settle = useSettleKitEvent(toolId);

  const [draft, setDraft] = useState({ name: '', quantity: '1', unitPrice: '' });
  const [editing, setEditing] = useState<ToolKitItem | null>(null);
  const [acting, setActing] = useState<{ item: ToolKitItem; action: ToolKitAction } | null>(null);

  if (!kit) return null;

  const money = (n: number) => formatMoney(n, locale);
  const addInvalid = draft.name.trim() === '' || Number(draft.quantity) < 1 || draft.unitPrice === '' || Number(draft.unitPrice) < 0;

  const submitAdd = async () => {
    await add.mutateAsync({ name: draft.name.trim(), quantity: Number(draft.quantity), unitPrice: Number(draft.unitPrice) });
    setDraft({ name: '', quantity: '1', unitPrice: '' });
  };

  const openMoney = kit.events.filter(
    (e) => e.settledAt === null && e.amount != null && (e.type === 'ChargedToWorker' || e.type === 'ReplacementBought'),
  );

  return (
    <Card>
      <CardContent>
        <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1.5 }}>
          {t('toolKit.title')}
        </Typography>

        <Stack direction="row" useFlexGap spacing={1.5} sx={{ flexWrap: 'wrap', mb: 2 }}>
          <Tile label={t('toolKit.total')} value={money(kit.totalValue)} />
          <Tile label={t('toolKit.missing')} value={money(kit.missingValue)} tone={kit.missingValue > 0 ? 'error' : undefined} />
          <Tile label={t('toolKit.broken')} value={money(kit.brokenValue)} tone={kit.brokenValue > 0 ? 'warning' : undefined} />
          <Tile label={t('toolKit.openCharges')} value={money(kit.openCharges)} tone={kit.openCharges > 0 ? 'error' : undefined} />
          <Tile label={t('toolKit.openRefunds')} value={money(kit.openRefunds)} tone={kit.openRefunds > 0 ? 'warning' : undefined} />
        </Stack>

        {kit.items.length === 0 ? (
          <Typography color="text.secondary" sx={{ mb: 2 }}>
            {t('toolKit.noItems')}
          </Typography>
        ) : (
          <Box sx={{ overflowX: 'auto', mb: 2 }}>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>{t('toolKit.item')}</TableCell>
                  <TableCell align="right">{t('toolKit.qty')}</TableCell>
                  <TableCell align="right">{t('toolKit.price')}</TableCell>
                  <TableCell align="right">{t('toolKit.lineTotal')}</TableCell>
                  <TableCell>{t('toolKit.state')}</TableCell>
                  <TableCell />
                </TableRow>
              </TableHead>
              <TableBody>
                {kit.items.map((item) => (
                  <ItemRow
                    key={item.id}
                    item={item}
                    money={money}
                    onEdit={() => setEditing(item)}
                    onRemove={() => void remove.mutateAsync(item.id)}
                    onAct={(action) => setActing({ item, action })}
                  />
                ))}
              </TableBody>
            </Table>
          </Box>
        )}

        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1} useFlexGap sx={{ alignItems: { sm: 'flex-start' }, mb: 2 }}>
          <TextField
            size="small"
            label={t('toolKit.item')}
            value={draft.name}
            onChange={(e) => setDraft({ ...draft, name: e.target.value })}
            sx={{ flex: 1, minWidth: 180 }}
          />
          <TextField
            size="small"
            type="number"
            label={t('toolKit.qty')}
            value={draft.quantity}
            onChange={(e) => setDraft({ ...draft, quantity: e.target.value })}
            slotProps={{ htmlInput: { min: 1 } }}
            sx={{ width: 90 }}
          />
          <TextField
            size="small"
            type="number"
            label={t('toolKit.price')}
            value={draft.unitPrice}
            onChange={(e) => setDraft({ ...draft, unitPrice: e.target.value })}
            slotProps={{ htmlInput: { min: 0, step: '0.01' } }}
            sx={{ width: 120 }}
          />
          <Button variant="outlined" startIcon={<AddOutlined />} disabled={addInvalid || add.isPending} onClick={() => void submitAdd()}>
            {t('toolKit.addItem')}
          </Button>
        </Stack>
        {add.isError && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {toApiError(add.error).message}
          </Alert>
        )}

        {openMoney.length > 0 && (
          <Box sx={{ mb: 2 }}>
            <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 0.5 }}>
              {t('toolKit.open')}
            </Typography>
            {openMoney.map((e) => (
              <Stack key={e.id} direction="row" spacing={1} useFlexGap sx={{ alignItems: 'center', flexWrap: 'wrap', py: 0.5 }}>
                <Typography variant="body2" sx={{ flex: 1, minWidth: 220 }}>
                  {e.type === 'ChargedToWorker' ? t('toolKit.chargeLine') : t('toolKit.refundLine')}: <strong>{e.employeeName ?? '—'}</strong> · {e.itemName} ·{' '}
                  {money(e.amount ?? 0)}
                  {e.payoutMethod ? ` · ${t(`toolKit.payout.${e.payoutMethod}` as MessageKey)}` : ''}
                </Typography>
                <Button size="small" variant="outlined" onClick={() => void settle.mutateAsync(e.id)}>
                  {e.type === 'ChargedToWorker' ? t('toolKit.settleCharge') : t('toolKit.settleRefund')}
                </Button>
              </Stack>
            ))}
          </Box>
        )}

        <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 0.5 }}>
          {t('toolKit.history')}
        </Typography>
        {kit.events.length === 0 ? (
          <Typography variant="body2" color="text.secondary">
            —
          </Typography>
        ) : (
          <Stack spacing={0.25}>
            {kit.events.slice(0, 25).map((e) => (
              <Typography key={e.id} variant="body2" color="text.secondary">
                {formatDate(e.at)} · {t(`toolKit.event.${e.type}` as MessageKey)} · {e.itemName}
                {e.amount != null ? ` · ${money(e.amount)}` : ''}
                {e.payoutMethod ? ` · ${t(`toolKit.payout.${e.payoutMethod}` as MessageKey)}` : ''}
                {e.employeeName ? ` · ${e.employeeName}` : ''}
                {e.note ? ` · ${e.note}` : ''}
                {e.settledAt ? ` · ${t('toolKit.closed')}` : ''}
              </Typography>
            ))}
          </Stack>
        )}
      </CardContent>

      {editing && (
        <EditItemDialog
          item={editing}
          error={update.isError ? toApiError(update.error).message : null}
          onClose={() => setEditing(null)}
          onSave={async (input) => {
            await update.mutateAsync({ itemId: editing.id, input });
            setEditing(null);
          }}
        />
      )}

      {acting && <ActionDialog toolId={toolId} kit={kit} holderId={holderId} acting={acting} onClose={() => setActing(null)} />}
    </Card>
  );
}

function Tile({ label, value, tone }: { label: string; value: string; tone?: 'error' | 'warning' }) {
  return (
    <Box sx={{ border: 1, borderColor: 'divider', borderRadius: 1.5, px: 1.5, py: 1, minWidth: 130 }}>
      <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
        {label}
      </Typography>
      <Typography variant="subtitle1" sx={{ fontWeight: 700 }} color={tone ? `${tone}.main` : undefined}>
        {value}
      </Typography>
    </Box>
  );
}

function ItemRow({
  item,
  money,
  onEdit,
  onRemove,
  onAct,
}: {
  item: ToolKitItem;
  money: (n: number) => string;
  onEdit: () => void;
  onRemove: () => void;
  onAct: (action: ToolKitAction) => void;
}) {
  const t = useT();
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);

  return (
    <TableRow hover>
      <TableCell sx={{ cursor: 'pointer' }} onDoubleClick={onEdit}>
        {item.name}
      </TableCell>
      <TableCell align="right">{item.quantity}</TableCell>
      <TableCell align="right">{money(item.unitPrice)}</TableCell>
      <TableCell align="right">{money(item.lineTotal)}</TableCell>
      <TableCell>
        <Chip size="small" color={STATE_COLOR[item.state]} variant={item.state === 'InKit' ? 'outlined' : 'filled'} label={t(`toolKit.state.${item.state}` as MessageKey)} />
      </TableCell>
      <TableCell align="right" sx={{ whiteSpace: 'nowrap' }}>
        <IconButton size="small" aria-label={t('toolKit.whatNext')} onClick={(e) => setAnchor(e.currentTarget)}>
          <MoreVertOutlined fontSize="small" />
        </IconButton>
        <Menu anchorEl={anchor} open={!!anchor} onClose={() => setAnchor(null)}>
          {NEXT[item.state].map((action) => (
            <MenuItem
              key={action}
              onClick={() => {
                setAnchor(null);
                onAct(action);
              }}
            >
              {t(`toolKit.action.${action}` as MessageKey)}
            </MenuItem>
          ))}
          <MenuItem
            onClick={() => {
              setAnchor(null);
              onEdit();
            }}
          >
            {t('common.edit')}
          </MenuItem>
        </Menu>
        <IconButton size="small" aria-label={t('toolKit.remove')} onClick={onRemove}>
          <DeleteOutlined fontSize="small" />
        </IconButton>
      </TableCell>
    </TableRow>
  );
}

function EditItemDialog({
  item,
  error,
  onClose,
  onSave,
}: {
  item: ToolKitItem;
  error: string | null;
  onClose: () => void;
  onSave: (input: { name: string; quantity: number; unitPrice: number }) => Promise<unknown>;
}) {
  const t = useT();
  const [name, setName] = useState(item.name);
  const [quantity, setQuantity] = useState(String(item.quantity));
  const [unitPrice, setUnitPrice] = useState(String(item.unitPrice));
  const invalid = name.trim() === '' || Number(quantity) < 1 || unitPrice === '' || Number(unitPrice) < 0;

  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="xs">
      <DialogTitle>{t('common.edit')}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          <TextField label={t('toolKit.item')} value={name} onChange={(e) => setName(e.target.value)} autoFocus />
          <TextField type="number" label={t('toolKit.qty')} value={quantity} onChange={(e) => setQuantity(e.target.value)} slotProps={{ htmlInput: { min: 1 } }} />
          <TextField type="number" label={t('toolKit.price')} value={unitPrice} onChange={(e) => setUnitPrice(e.target.value)} slotProps={{ htmlInput: { min: 0, step: '0.01' } }} />
          {error && <Alert severity="error">{error}</Alert>}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button variant="contained" disabled={invalid} onClick={() => void onSave({ name: name.trim(), quantity: Number(quantity), unitPrice: Number(unitPrice) })}>
          {t('toolKit.save')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

function ActionDialog({
  toolId,
  kit,
  holderId,
  acting,
  onClose,
}: {
  toolId: string;
  kit: ToolKit;
  holderId: string | null;
  acting: { item: ToolKitItem; action: ToolKitAction };
  onClose: () => void;
}) {
  const t = useT();
  const act = useActOnKitItem(toolId);
  const { data: employees } = useAllEmployeesQuery();
  const { item, action } = acting;
  const needsMoney = MONEY.includes(action);

  const [employeeId, setEmployeeId] = useState(holderId ?? '');
  const [amount, setAmount] = useState(String(item.lineTotal));
  const [note, setNote] = useState('');
  const [refund, setRefund] = useState(true);
  const [method, setMethod] = useState<ToolKitPayoutMethod | ''>('');
  const bought = action === 'ReplacementBought';
  const owes = bought && refund;

  const submit = async () => {
    await act.mutateAsync({
      itemId: item.id,
      input: {
        action,
        note: note.trim() || null,
        ...(needsMoney ? { employeeId: employeeId || null, amount: owes || !bought ? Number(amount) : null } : {}),
        ...(bought ? { refund, payoutMethod: owes ? method || null : null } : {}),
      },
    });
    onClose();
  };

  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="xs">
      <DialogTitle>
        {t(`toolKit.action.${action}` as MessageKey)} · {item.name}
      </DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          <Typography variant="body2" color="text.secondary">
            {t(`toolKit.hint.${action}` as MessageKey)}
          </Typography>
          {needsMoney && (
            <>
              <TextField select label={t('toolKit.worker')} value={employeeId} onChange={(e) => setEmployeeId(e.target.value)} helperText={holderId ? t('toolKit.holderHint') : t('toolKit.noHolder')}>
                {(employees?.items ?? []).map((e) => (
                  <MenuItem key={e.id} value={e.id}>
                    {e.fullName}
                  </MenuItem>
                ))}
              </TextField>
              {bought && (
                <RadioGroup value={refund ? 'yes' : 'no'} onChange={(e) => setRefund(e.target.value === 'yes')}>
                  <FormControlLabel value="yes" control={<Radio />} label={t('toolKit.refund.yes')} />
                  <FormControlLabel value="no" control={<Radio />} label={t('toolKit.refund.no')} />
                </RadioGroup>
              )}
              {bought && !refund && <Alert severity="info">{t('toolKit.refund.noHint')}</Alert>}
              {(!bought || refund) && (
                <TextField type="number" label={t('toolKit.amount')} value={amount} onChange={(e) => setAmount(e.target.value)} helperText={t('toolKit.amountHint')} slotProps={{ htmlInput: { min: 0, step: '0.01' } }} />
              )}
              {owes && (
                <>
                  <RadioGroup value={method} onChange={(e) => setMethod(e.target.value as ToolKitPayoutMethod)}>
                    <FormControlLabel value="WithPayroll" control={<Radio />} label={t('toolKit.payout.WithPayroll')} />
                    <FormControlLabel value="Cash" control={<Radio />} label={t('toolKit.payout.Cash')} />
                  </RadioGroup>
                  <Alert severity={method ? 'info' : 'warning'}>
                    {method ? t(`toolKit.payout.${method}Hint` as MessageKey) : t('toolKit.payout.choose')}
                  </Alert>
                </>
              )}
            </>
          )}
          <TextField label={t('toolKit.note')} value={note} onChange={(e) => setNote(e.target.value)} multiline minRows={2} />
          {act.isError && <Alert severity="error">{toApiError(act.error).message}</Alert>}
          <input type="hidden" value={kit.toolId} readOnly />
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button variant="contained" disabled={act.isPending || (needsMoney && (!employeeId || (owes || !bought ? amount === '' : false))) || (owes && !method)} onClick={() => void submit()}>
          {t('toolKit.confirm')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
