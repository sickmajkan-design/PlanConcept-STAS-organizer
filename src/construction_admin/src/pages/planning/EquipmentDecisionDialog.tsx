import {
  Alert,
  Autocomplete,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
  TextField,
  ToggleButton,
  ToggleButtonGroup,
  Typography,
} from '@mui/material';
import { useEffect, useMemo, useState } from 'react';

import { toApiError } from '../../api/apiError';
import { EquipmentIcon } from '../../components/EquipmentIcon';
import {
  choiceRequest,
  heldBy,
  suggestReceiver,
  type EquipmentChoiceKind,
  type EquipmentChoiceState,
  type EquipmentItem,
} from '../../features/planning/equipment';
import type { Plan, PlanPerson } from '../../features/planning/planningLogic';
import { usePlanningActions } from '../../features/planning/usePlanning';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useT } from '../../i18n/useI18n';

const KINDS: EquipmentChoiceKind[] = ['Stay', 'HandOver', 'ToStore'];

/**
 * What to do with the equipment somebody holds when they leave a site or are away: each item stays with them, goes to
 * another person, or goes back to the store. Nothing is released silently: until it is decided the board keeps warning.
 */
export function EquipmentDecisionDialog({
  plan,
  personId,
  day,
  onClose,
}: {
  plan: Plan;
  personId: string;
  /** The day the board is on: who is free, and where people are, is read for it. */
  day: number;
  onClose: () => void;
}) {
  const t = useT();
  const enumLabel = useEnumLabel();
  const { decideEquipment } = usePlanningActions();

  const person = plan.people.find((p) => p.id === personId);
  const items = useMemo(() => (person ? heldBy(plan, person.id) : []), [plan, person]);
  const away = person?.cells[day]?.away ?? null;
  const suggestion = person ? suggestReceiver(plan, person, day) : null;
  const receivers = useMemo(() => plan.people.filter((p) => p.id !== personId), [plan.people, personId]);

  const [choices, setChoices] = useState<Record<string, EquipmentChoiceState>>(() =>
    Object.fromEntries(
      items.map((item) => [item.id, away && suggestion ? { choice: 'HandOver' as const, to: suggestion.id } : { choice: 'Stay' as const, to: null }]),
    ),
  );
  const [failure, setFailure] = useState<string | null>(null);
  // From when the decision counts: today (now), or a later day it is planned for.
  const today = plan.today;
  const [from, setFrom] = useState(() => (plan.date(day) > today ? plan.date(day) : today));

  // Nothing held (or the person is gone): there is nothing to decide.
  useEffect(() => {
    if (!person || items.length === 0) onClose();
  }, [person, items.length, onClose]);

  if (!person || items.length === 0) return null;

  const where = (p: PlanPerson): string => {
    const cell = p.cells[day];
    if (cell?.away) return enumLabel('absenceType', cell.away);
    if (cell?.project) return plan.projectById.get(cell.project)?.name ?? '';

    return t('planning.l.free');
  };

  const stateOf = (item: EquipmentItem): EquipmentChoiceState => choices[item.id] ?? { choice: 'Stay', to: null };
  const setItem = (item: EquipmentItem, next: EquipmentChoiceState) => setChoices((c) => ({ ...c, [item.id]: next }));

  const allKind = items.every((i) => stateOf(i).choice === stateOf(items[0]).choice) ? stateOf(items[0]).choice : null;
  const setAll = (kind: EquipmentChoiceKind) =>
    setChoices(
      Object.fromEntries(items.map((i) => [i.id, { choice: kind, to: kind === 'HandOver' ? (stateOf(i).to ?? suggestion?.id ?? null) : null }])),
    );

  const submit = async () => {
    const request = choiceRequest(items, choices);

    if (request.missingReceiver) {
      setFailure(t('planning.eqd.needReceiver'));
      return;
    }

    if (request.items.length === 0) {
      onClose();
      return;
    }

    try {
      await decideEquipment.mutateAsync({ items: request.items, effectiveFrom: from > today ? from : undefined });
      onClose();
    } catch (error) {
      setFailure(toApiError(error).message);
    }
  };

  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle sx={{ pb: 0.5 }}>
        {away ? t('planning.eqd.titleAway', { name: person.name }) : t('planning.eqd.titleLeft', { name: person.name })}
      </DialogTitle>
      <DialogContent>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          {away
            ? t('planning.eqd.introAway', { type: enumLabel('absenceType', away), count: items.length })
            : t('planning.eqd.introLeft', { count: items.length })}
        </Typography>

        <Stack spacing={1.5}>
          {items.length > 1 && (
            <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center', flexWrap: 'wrap' }} useFlexGap>
              <Typography variant="body2" sx={{ fontWeight: 600 }}>
                {t('planning.eqd.same')}
              </Typography>
              <ToggleButtonGroup size="small" exclusive value={allKind} onChange={(_, kind: EquipmentChoiceKind | null) => kind && setAll(kind)} aria-label={t('planning.eqd.same')}>
                {KINDS.map((kind) => (
                  <ToggleButton key={kind} value={kind} sx={{ textTransform: 'none', px: 1.5 }}>
                    {t(`planning.eqd.${kind}`)}
                  </ToggleButton>
                ))}
              </ToggleButtonGroup>
            </Stack>
          )}

          {items.map((item) => {
            const state = stateOf(item);
            const picked = receivers.find((p) => p.id === state.to) ?? null;

            return (
              <Box key={item.id} sx={{ border: 1, borderColor: 'divider', borderRadius: 2, p: 1.5 }}>
                <Stack direction="row" spacing={1.25} sx={{ alignItems: 'center', mb: 1 }}>
                  <Box component="span" sx={{ display: 'inline-flex', color: 'text.secondary', flex: 'none' }}>
                    <EquipmentIcon group={item.group} size={22} />
                  </Box>
                  <Typography variant="body2" sx={{ fontWeight: 600, overflowWrap: 'anywhere' }}>
                    {item.label}
                  </Typography>
                </Stack>

                <ToggleButtonGroup
                  size="small"
                  exclusive
                  fullWidth
                  value={state.choice}
                  onChange={(_, kind: EquipmentChoiceKind | null) =>
                    kind && setItem(item, { choice: kind, to: kind === 'HandOver' ? (state.to ?? suggestion?.id ?? null) : null })
                  }
                  aria-label={item.label}
                >
                  {KINDS.map((kind) => (
                    <ToggleButton key={kind} value={kind} sx={{ textTransform: 'none' }}>
                      {t(`planning.eqd.${kind}`)}
                    </ToggleButton>
                  ))}
                </ToggleButtonGroup>

                {state.choice === 'HandOver' && (
                  <Autocomplete
                    size="small"
                    sx={{ mt: 1 }}
                    options={receivers}
                    value={picked}
                    onChange={(_, value) => setItem(item, { choice: 'HandOver', to: value?.id ?? null })}
                    getOptionLabel={(p) => p.name}
                    isOptionEqualToValue={(a, b) => a.id === b.id}
                    renderOption={(props, p) => (
                      <li {...props} key={p.id}>
                        <Box>
                          <Typography variant="body2">{p.name}</Typography>
                          <Typography variant="caption" color="text.secondary">
                            {p.position} · {where(p)}
                            {suggestion?.id === p.id ? ` · ${t('planning.eqd.suggested')}` : ''}
                          </Typography>
                        </Box>
                      </li>
                    )}
                    renderInput={(params) => <TextField {...params} label={t('planning.eqd.to')} />}
                  />
                )}

                <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 0.75 }}>
                  {t(`planning.eqd.hint.${state.choice}`)}
                </Typography>
              </Box>
            );
          })}

          <TextField
            size="small"
            type="date"
            label={t('planning.eqd.from')}
            value={from}
            onChange={(e) => setFrom(e.target.value || today)}
            slotProps={{ htmlInput: { min: today }, inputLabel: { shrink: true } }}
            helperText={from > today ? t('planning.eqd.fromLater') : t('planning.eqd.fromNow')}
          />

          {failure && <Alert severity="error">{failure}</Alert>}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('planning.eqd.later')}</Button>
        <Button variant="contained" loading={decideEquipment.isPending} onClick={() => void submit()}>
          {from > today ? t('planning.eqd.savePlanned') : t('planning.eqd.save')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
