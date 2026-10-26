import { Box, ButtonBase, List, ListItem, Popover, Typography } from '@mui/material';
import { useState } from 'react';

import type { Vehicle } from '../../api/types';
import { useI18n, useT } from '../../i18n/useI18n';
import type { MessageKey } from '../../i18n/en';
import { formatDate, formatMoney } from '../../utils/formatting';
import { vehicleState, type VehicleStateReason, type VehicleStateTone } from './vehicleState';

const COLOR: Record<VehicleStateTone, string> = {
  ok: 'success.main',
  warning: 'warning.main',
  error: 'error.main',
  none: 'text.disabled',
};

/** The words for one reason, e.g. "Osiguranje ističe 10.11.2026. (još 15 dana)". */
export function useReasonText() {
  const t = useT();
  const { locale } = useI18n();

  return (r: VehicleStateReason): string => {
    const what = r.kind ? t(`vehicleState.kind.${r.kind}` as MessageKey) : t('vehicleState.kind.toll');
    const date = r.date ? formatDate(r.date) : '';

    switch (r.what) {
      case 'date':
      case 'toll':
        return (r.days ?? 0) < 0
          ? t('vehicleState.expired', { what, date })
          : t('vehicleState.expiring', { what, date, days: r.days ?? 0 });
      case 'tollUnpaid':
        return t('vehicleState.tollUnpaid', { count: r.count ?? 0 });
      case 'fineOverdue':
        return t('vehicleState.fineOverdue', { count: r.count ?? 0 });
      case 'fineOpen':
        return t('vehicleState.fineOpen', { count: r.count ?? 0, amount: formatMoney(r.amount ?? 0, locale) });
    }
  };
}

/** One dot for a vehicle: green, amber, red or grey. A click says why, without opening the vehicle. */
export function VehicleStateDot({ vehicle }: { vehicle: Parameters<typeof vehicleState>[0] & Pick<Vehicle, 'registrationNumber'> }) {
  const t = useT();
  const reasonText = useReasonText();
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);
  const state = vehicleState(vehicle);

  return (
    <>
      <ButtonBase
        aria-label={t(`vehicleState.tone.${state.tone}` as MessageKey)}
        title={t(`vehicleState.tone.${state.tone}` as MessageKey)}
        onClick={(event) => {
          // The row it sits in opens the vehicle; the dot only explains itself.
          event.stopPropagation();
          setAnchor(event.currentTarget);
        }}
        sx={{ p: 1, borderRadius: '50%' }}
      >
        <Box sx={{ width: 14, height: 14, borderRadius: '50%', bgcolor: COLOR[state.tone] }} />
      </ButtonBase>

      <Popover open={!!anchor} anchorEl={anchor} onClose={() => setAnchor(null)} onClick={(event) => event.stopPropagation()}>
        <Box sx={{ p: 1.5, maxWidth: 340 }}>
          <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>
            {vehicle.registrationNumber}: {t(`vehicleState.tone.${state.tone}` as MessageKey)}
          </Typography>
          {state.reasons.length > 0 ? (
            <List dense disablePadding>
              {state.reasons.map((r, i) => (
                <ListItem key={i} disableGutters sx={{ color: r.tone === 'error' ? 'error.main' : 'warning.main' }}>
                  {reasonText(r)}
                </ListItem>
              ))}
            </List>
          ) : (
            <Typography variant="body2" color="text.secondary">
              {state.tone === 'none' ? t('vehicleState.noneHint') : t('vehicleState.okHint')}
            </Typography>
          )}
        </Box>
      </Popover>
    </>
  );
}
