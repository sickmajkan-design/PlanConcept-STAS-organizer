import { ExpandMoreOutlined } from '@mui/icons-material';
import { Chip, ListItemIcon, Menu, MenuItem } from '@mui/material';
import { useState } from 'react';

import type { LedgerDetail } from '../../api/types';
import { useT } from '../../i18n/useI18n';
import { useUpdateLedger } from '../ledgers/useLedgers';
import { BranchDot } from './BranchDot';
import { useBranchesQuery } from './useBranches';

/**
 * Whose payroll a month is: the business unit it belongs to, changeable in place. A unit's payroll
 * is checked against the people that unit employed in the month, so setting it is what turns the
 * check on.
 */
export function LedgerBranchChip({ ledger }: { ledger: LedgerDetail }) {
  const t = useT();
  const { data: branches } = useBranchesQuery();
  const update = useUpdateLedger(ledger.id);
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);

  const choose = (branchId: string | null) => {
    setAnchor(null);
    if (branchId === ledger.branchId) return;
    update.mutate({ name: ledger.name, year: ledger.year, month: ledger.month, note: ledger.note, branchId });
  };

  return (
    <>
      <Chip
        clickable
        size="small"
        variant={ledger.branchId ? 'filled' : 'outlined'}
        icon={ledger.branchColor ? <BranchDot color={ledger.branchColor} /> : undefined}
        label={ledger.branchName ?? t('ledgers.branch.none')}
        deleteIcon={<ExpandMoreOutlined />}
        onDelete={(event) => setAnchor(event.currentTarget.parentElement)}
        onClick={(event) => setAnchor(event.currentTarget)}
        disabled={update.isPending}
        sx={{ pl: ledger.branchColor ? 0.75 : undefined }}
      />
      <Menu anchorEl={anchor} open={!!anchor} onClose={() => setAnchor(null)}>
        <MenuItem selected={!ledger.branchId} onClick={() => choose(null)}>
          <em>{t('ledgers.branch.none')}</em>
        </MenuItem>
        {(branches ?? [])
          .filter((b) => b.isActive || b.id === ledger.branchId)
          .map((branch) => (
            <MenuItem key={branch.id} selected={branch.id === ledger.branchId} onClick={() => choose(branch.id)}>
              <ListItemIcon sx={{ minWidth: 24 }}>
                <BranchDot color={branch.color} />
              </ListItemIcon>
              {branch.name}
            </MenuItem>
          ))}
      </Menu>
    </>
  );
}
