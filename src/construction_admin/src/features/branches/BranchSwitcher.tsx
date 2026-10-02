import { CheckOutlined, ExpandMoreOutlined, SettingsOutlined } from '@mui/icons-material';
import { Box, Button, Divider, ListItemIcon, Menu, MenuItem, Typography } from '@mui/material';
import { useState } from 'react';
import { Link } from 'react-router-dom';

import { useAuth } from '../../auth/useAuth';
import { canAdministerAccounts } from '../../auth/authHelpers';
import { useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';
import { useBranchFilter } from './BranchContext';
import { BranchDot } from './BranchDot';
import { useBranchesQuery } from './useBranches';

/**
 * The header's business-unit switcher: a compact button that opens a menu, not permanent tabs.
 * Hidden until at least one unit exists — a company with no branches has nothing to switch.
 */
export function BranchSwitcher() {
  const t = useT();
  const { user } = useAuth();
  const { data: branches } = useBranchesQuery(!!user);
  const { branchId, setBranchId } = useBranchFilter();
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);

  const active = (branches ?? []).filter((b) => b.isActive || b.id === branchId);
  const canManage = canAdministerAccounts(user);

  if (!branches || (branches.length === 0 && !canManage)) return null;

  const current = branches.find((b) => b.id === branchId);

  const choose = (id: string | undefined) => {
    setBranchId(id);
    setAnchor(null);
  };

  return (
    <>
      <Button
        onClick={(event) => setAnchor(event.currentTarget)}
        color="inherit"
        aria-haspopup="menu"
        aria-expanded={!!anchor}
        endIcon={<ExpandMoreOutlined />}
        sx={{
          textTransform: 'none',
          fontWeight: 600,
          borderRadius: 2,
          px: 1.5,
          gap: 0.5,
          maxWidth: { xs: 160, md: 260 },
          bgcolor: current ? `${current.color}14` : undefined,
        }}
      >
        {current && <BranchDot color={current.color} />}
        <Box component="span" sx={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
          {current ? current.name : t('branches.all')}
        </Box>
      </Button>
      <Menu anchorEl={anchor} open={!!anchor} onClose={() => setAnchor(null)}>
        <Typography variant="overline" color="text.secondary" sx={{ px: 2 }}>
          {t('branches.title')}
        </Typography>
        <MenuItem selected={!branchId} onClick={() => choose(undefined)}>
          <Box sx={{ flex: 1 }}>{t('branches.all')}</Box>
          {!branchId && <CheckOutlined fontSize="small" sx={{ ml: 2 }} />}
        </MenuItem>
        {active.map((branch) => (
          <MenuItem key={branch.id} selected={branch.id === branchId} onClick={() => choose(branch.id)}>
            <ListItemIcon sx={{ minWidth: 24 }}>
              <BranchDot color={branch.color} />
            </ListItemIcon>
            <Box sx={{ flex: 1 }}>{branch.name}</Box>
            {branch.id === branchId && <CheckOutlined fontSize="small" sx={{ ml: 2 }} />}
          </MenuItem>
        ))}
        {canManage && <Divider />}
        {canManage && (
          <MenuItem component={Link} to={paths.branches} onClick={() => setAnchor(null)}>
            <ListItemIcon>
              <SettingsOutlined fontSize="small" />
            </ListItemIcon>
            {t('branches.manage')}
          </MenuItem>
        )}
      </Menu>
    </>
  );
}
