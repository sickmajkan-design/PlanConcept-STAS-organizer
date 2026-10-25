import { CheckOutlined, ExpandMoreOutlined, SettingsOutlined } from '@mui/icons-material';
import {
  Box,
  Button,
  Chip,
  Dialog,
  Divider,
  ListItemIcon,
  Menu,
  MenuItem,
  MenuList,
  ToggleButton,
  ToggleButtonGroup,
  Tooltip,
  Typography,
  useMediaQuery,
  useTheme,
} from '@mui/material';
import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';

import { useAuth } from '../../auth/useAuth';
import { canAdministerAccounts } from '../../auth/authHelpers';
import { useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';
import { useBranchFilter } from './BranchContext';
import { BranchMark } from './BranchMark';
import { buildBranchTree, flattenBranchTree } from './branchTree';
import { useBranchesQuery } from './useBranches';

/**
 * The header's business-unit switcher: a compact button that opens a menu, not permanent tabs.
 * Hidden until at least one unit exists — a company with no branches has nothing to switch.
 */
export function BranchSwitcher() {
  const t = useT();
  const { user } = useAuth();
  const { data: branches } = useBranchesQuery(!!user);
  const { branchId, setBranchId, basis, setBasis } = useBranchFilter();
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);
  const isPhone = useMediaQuery(useTheme().breakpoints.down('sm'));

  const active = (branches ?? []).filter((b) => b.isActive || b.id === branchId);
  const canManage = canAdministerAccounts(user);

  // Units in tree order, so a region sits above its branches and each one is indented by its depth.
  const nodes = useMemo(() => flattenBranchTree(buildBranchTree(active)), [active]);
  const currentNode = nodes.find((n) => n.branch.id === branchId);

  if (!branches || (branches.length === 0 && !canManage)) return null;

  const current = branches.find((b) => b.id === branchId);

  const choose = (id: string | undefined) => {
    setBranchId(id);
    setAnchor(null);
  };

  const entries = [
    <Typography key="title" variant="overline" color="text.secondary" sx={{ px: 2 }}>
      {t('branches.title')}
    </Typography>,
    <MenuItem key="all" selected={!branchId} onClick={() => choose(undefined)}>
      <Box sx={{ flex: 1 }}>{t('branches.all')}</Box>
      {!branchId && <CheckOutlined fontSize="small" sx={{ ml: 2 }} />}
    </MenuItem>,
    ...nodes.map(({ branch, depth }) => (
      <MenuItem
        key={branch.id}
        selected={branch.id === branchId}
        onClick={() => choose(branch.id)}
        sx={{ pl: 2 + (depth - 1) * 2.5 }}
      >
        <ListItemIcon sx={{ minWidth: 32 }}>
          <BranchMark branch={branch} />
        </ListItemIcon>
        <Box sx={{ flex: 1 }}>{branch.name}</Box>
        {branch.id === branchId && <CheckOutlined fontSize="small" sx={{ ml: 2 }} />}
      </MenuItem>
    )),
    currentNode && currentNode.descendantIds.length > 0 && (
      <Typography key="sub" variant="caption" color="text.secondary" sx={{ display: 'block', px: 2, py: 0.5 }}>
        {t('branches.includesSub', { count: currentNode.descendantIds.length })}
      </Typography>
    ),
    branchId && <Divider key="d1" />,
    branchId && (
      <Box key="basis" sx={{ px: 2, py: 1, maxWidth: 320 }}>
        <Typography variant="overline" color="text.secondary">
          {t('branches.basis.title')}
        </Typography>
        <ToggleButtonGroup
          exclusive
          fullWidth
          size="small"
          value={basis}
          onChange={(_event, value: 'Site' | 'Employer' | null) => value && setBasis(value)}
        >
          <ToggleButton value="Site">{t('branches.basis.site')}</ToggleButton>
          <ToggleButton value="Employer">{t('branches.basis.employer')}</ToggleButton>
        </ToggleButtonGroup>
        <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 0.75 }}>
          {basis === 'Employer' ? t('branches.basis.employerHint') : t('branches.basis.siteHint')}
        </Typography>
      </Box>
    ),
    canManage && <Divider key="d2" />,
    canManage && (
      <MenuItem key="manage" component={Link} to={paths.organizationBranches} onClick={() => setAnchor(null)}>
        <ListItemIcon>
          <SettingsOutlined fontSize="small" />
        </ListItemIcon>
        {t('branches.manage')}
      </MenuItem>
    ),
  ];

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
          // On a phone the bar holds only the menu, the bell and the avatar besides this, so the name may use the rest.
          maxWidth: { xs: 'calc(100vw - 200px)', md: 260 },
          minWidth: 0,
          bgcolor: current ? `${current.color}14` : undefined,
        }}
      >
        {current && <BranchMark branch={current} />}
        <Box component="span" sx={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
          {current ? current.name : t('branches.all')}
        </Box>
        {currentNode && currentNode.descendantIds.length > 0 && (
          <Chip size="small" label={`+${currentNode.descendantIds.length}`} sx={{ height: 20, fontSize: 11 }} />
        )}
        {current && basis === 'Employer' && (
          <Tooltip title={t('branches.basis.employerHint')}>
            <Chip size="small" label={t('branches.basis.chip')} sx={{ height: 20, fontSize: 11 }} />
          </Tooltip>
        )}
      </Button>
      {isPhone ? (
        // On a phone the choice rises from the bottom edge, where a thumb reaches, like the other dialogs.
        <Dialog open={!!anchor} onClose={() => setAnchor(null)} fullWidth>
          <MenuList sx={{ py: 1.5 }}>{entries}</MenuList>
        </Dialog>
      ) : (
        <Menu anchorEl={anchor} open={!!anchor} onClose={() => setAnchor(null)}>
          {entries}
        </Menu>
      )}
    </>
  );
}
