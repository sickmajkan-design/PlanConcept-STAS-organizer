import { BusinessOutlined, ExpandMoreOutlined, MoreVertOutlined } from '@mui/icons-material';
import { Avatar, Box, Chip, Collapse, IconButton, ListItemText, Menu, MenuItem, Paper, Stack, Typography } from '@mui/material';
import { useMemo, useState } from 'react';

import type { OrganizationHierarchyNode } from '../../api/types';
import { BranchDot } from '../../features/branches/BranchDot';
import { BranchQuickEditDialog, type QuickEdit } from '../../features/branches/BranchQuickEditDialog';
import { buildBranchTree, type BranchNode } from '../../features/branches/branchTree';
import { useBranchesQuery } from '../../features/branches/useBranches';
import { countryLabel } from '../../data/countries';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useT } from '../../i18n/useI18n';
import { groupByRank } from './rankTiers';

function initialsOf(name: string): string {
  const parts = name.trim().split(/\s+/);
  return ((parts[0]?.[0] ?? '') + (parts[parts.length - 1]?.[0] ?? '')).toUpperCase();
}

function PersonChip({ node, onOpen }: { node: OrganizationHierarchyNode; onOpen: () => void }) {
  return (
    <Paper
      variant="outlined"
      onClick={onOpen}
      sx={{
        px: 1.25,
        py: 0.5,
        cursor: 'pointer',
        borderLeft: '4px solid',
        borderLeftColor: 'primary.main',
        minWidth: 0,
        '&:hover': { boxShadow: 2 },
      }}
    >
      <Typography variant="body2" sx={{ fontWeight: 600, lineHeight: 1.3 }} noWrap title={node.fullName}>
        {node.fullName}
      </Typography>
      <Typography variant="caption" color="text.secondary" noWrap component="div" title={node.position}>
        {node.position}
      </Typography>
    </Paper>
  );
}

/** The people one unit employs, by rank, highest first. */
function UnitPeople({
  people,
  onOpenPerson,
}: {
  people: OrganizationHierarchyNode[];
  onOpenPerson: (node: OrganizationHierarchyNode) => void;
}) {
  const t = useT();
  const enumLabel = useEnumLabel();
  const { tiers, unplaced } = useMemo(() => groupByRank(people), [people]);

  if (people.length === 0) {
    return (
      <Typography variant="body2" color="text.secondary">
        {t('hierarchy.structure.noPeople')}
      </Typography>
    );
  }

  const rows = [
    ...tiers.map((tier) => ({ key: tier.rank, title: enumLabel('organizationRank', tier.rank), people: tier.people })),
    ...(unplaced.length > 0 ? [{ key: 'unplaced', title: t('hierarchy.unplaced'), people: unplaced }] : []),
  ];

  return (
    <Stack spacing={1.25}>
      {rows.map((row) => (
        <Box
          key={row.key}
          sx={{ display: 'flex', flexWrap: 'wrap', alignItems: 'center', gap: 1, rowGap: 0.75 }}
        >
          <Typography
            variant="caption"
            color="text.secondary"
            sx={{ width: { xs: '100%', sm: 130 }, fontWeight: 600, textTransform: 'uppercase', letterSpacing: '.05em' }}
          >
            {row.title}
          </Typography>
          {row.people.map((node) => (
            <PersonChip key={node.employeeId} node={node} onOpen={() => onOpenPerson(node)} />
          ))}
        </Box>
      ))}
    </Stack>
  );
}

function UnitCard({
  node,
  peopleByUnit,
  open,
  toggle,
  onOpenPerson,
  onQuickEdit,
}: {
  node: BranchNode;
  peopleByUnit: Map<string, OrganizationHierarchyNode[]>;
  open: Set<string>;
  toggle: (id: string) => void;
  onOpenPerson: (node: OrganizationHierarchyNode) => void;
  /** Present for people who may change units: adds the unit's own menu. */
  onQuickEdit?: (edit: QuickEdit) => void;
}) {
  const t = useT();
  const { branch } = node;
  const [menuAnchor, setMenuAnchor] = useState<HTMLElement | null>(null);
  const isOpen = open.has(branch.id);
  const place = [branch.city, countryLabel(branch.countryCode)].filter(Boolean).join(', ');

  return (
    <Paper variant="outlined" sx={{ minWidth: 0, opacity: branch.isActive ? 1 : 0.65 }}>
      <Box
        role="button"
        tabIndex={0}
        aria-expanded={isOpen}
        onClick={() => toggle(branch.id)}
        onKeyDown={(event) => {
          if (event.key === 'Enter' || event.key === ' ') {
            event.preventDefault();
            toggle(branch.id);
          }
        }}
        sx={{
          display: 'flex',
          alignItems: 'center',
          flexWrap: 'wrap',
          gap: 1.5,
          px: 2,
          py: 1.5,
          cursor: 'pointer',
          '&:hover': { bgcolor: 'action.hover' },
        }}
      >
        <BranchDot color={branch.color} />
        <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
          {branch.name}
        </Typography>
        <Chip
          size="small"
          variant="outlined"
          label={t(branch.kind === 'LegalEntity' ? 'branches.kindEntity' : 'branches.kindOffice')}
        />
        {place && <Chip size="small" variant="outlined" label={place} />}
        {!branch.isActive && <Chip size="small" label={t('branches.inactive')} />}

        <Stack direction="row" spacing={1} sx={{ alignItems: 'center', color: 'text.secondary' }}>
          <Avatar sx={{ width: 26, height: 26, fontSize: 11, bgcolor: branch.headEmployeeName ? 'text.primary' : 'action.disabledBackground' }}>
            {branch.headEmployeeName ? initialsOf(branch.headEmployeeName) : '?'}
          </Avatar>
          <Typography variant="body2" color="text.secondary">
            {branch.headEmployeeName ?? t('hierarchy.structure.noHead')}
          </Typography>
        </Stack>

        <Stack
          direction="row"
          spacing={2}
          sx={{ ml: 'auto', color: 'text.secondary', fontVariantNumeric: 'tabular-nums' }}
        >
          <Typography variant="body2">
            {t('hierarchy.structure.employees', { count: node.totals.employees })}
          </Typography>
          <Typography variant="body2">
            {t('hierarchy.structure.projects', { count: node.totals.projects })}
          </Typography>
        </Stack>
        <IconButton
          size="small"
          tabIndex={-1}
          aria-hidden
          sx={{ transform: isOpen ? 'rotate(180deg)' : 'none', transition: 'transform .2s' }}
        >
          <ExpandMoreOutlined fontSize="small" />
        </IconButton>
        {onQuickEdit && (
          <IconButton
            size="small"
            aria-label={t('hierarchy.structure.unitMenu', { name: branch.name })}
            onClick={(event) => {
              event.stopPropagation();
              setMenuAnchor(event.currentTarget);
            }}
          >
            <MoreVertOutlined fontSize="small" />
          </IconButton>
        )}
      </Box>
      {onQuickEdit && (
        <Menu anchorEl={menuAnchor} open={!!menuAnchor} onClose={() => setMenuAnchor(null)}>
          <MenuItem
            onClick={() => {
              setMenuAnchor(null);
              onQuickEdit({ branch, mode: 'head' });
            }}
          >
            <ListItemText>{t('hierarchy.structure.setHead')}</ListItemText>
          </MenuItem>
          <MenuItem
            onClick={() => {
              setMenuAnchor(null);
              onQuickEdit({ branch, mode: 'parent' });
            }}
          >
            <ListItemText>{t('hierarchy.structure.move')}</ListItemText>
          </MenuItem>
        </Menu>
      )}

      <Collapse in={isOpen} unmountOnExit>
        <Box sx={{ px: 2, py: 1.5, bgcolor: 'action.hover', borderTop: 1, borderColor: 'divider' }}>
          <UnitPeople people={peopleByUnit.get(branch.id) ?? []} onOpenPerson={onOpenPerson} />
        </Box>
      </Collapse>

      {node.children.length > 0 && (
        <Stack spacing={1.25} sx={{ ml: { xs: 1.5, sm: 3 }, pl: { xs: 1.5, sm: 2 }, py: 1.25, pr: 1.25, borderLeft: 2, borderColor: 'divider' }}>
          {node.children.map((child) => (
            <UnitCard
              key={child.branch.id}
              node={child}
              peopleByUnit={peopleByUnit}
              open={open}
              toggle={toggle}
              onOpenPerson={onOpenPerson}
              onQuickEdit={onQuickEdit}
            />
          ))}
        </Stack>
      )}
    </Paper>
  );
}

/**
 * The company and its business units as a tree: who runs each unit, how many people and sites it
 * has (counting the units under it), and, opened, the people it employs by rank.
 */
export function HierarchyStructure({
  companyName,
  people,
  onOpenPerson,
  canEdit = false,
}: {
  companyName: string;
  people: OrganizationHierarchyNode[];
  onOpenPerson: (node: OrganizationHierarchyNode) => void;
  /** Whether the viewer may change units (set a head, move one). */
  canEdit?: boolean;
}) {
  const t = useT();
  const { data: branches } = useBranchesQuery();
  const [open, setOpen] = useState<Set<string>>(new Set());
  const [quick, setQuick] = useState<QuickEdit | null>(null);

  const tree = useMemo(() => buildBranchTree(branches ?? []), [branches]);

  const peopleByUnit = useMemo(() => {
    const map = new Map<string, OrganizationHierarchyNode[]>();

    for (const node of people) {
      if (node.branchId) map.set(node.branchId, [...(map.get(node.branchId) ?? []), node]);
    }

    return map;
  }, [people]);

  const loose = useMemo(() => people.filter((node) => !node.branchId), [people]);

  const totals = useMemo(
    () => ({
      units: (branches ?? []).length,
      employees: tree.reduce((sum, n) => sum + n.totals.employees, 0) + loose.length,
      projects: tree.reduce((sum, n) => sum + n.totals.projects, 0),
    }),
    [branches, tree, loose],
  );

  const toggle = (id: string) =>
    setOpen((previous) => {
      const next = new Set(previous);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });

  return (
    <Stack spacing={1.5}>
      <Paper variant="outlined" sx={{ px: 2, py: 1.5, display: 'flex', alignItems: 'center', flexWrap: 'wrap', gap: 1.5, bgcolor: 'action.hover' }}>
        <BusinessOutlined color="primary" />
        <Box>
          <Typography variant="h6" sx={{ fontWeight: 700, lineHeight: 1.2 }}>
            {companyName}
          </Typography>
          <Typography variant="caption" color="text.secondary">
            {t('hierarchy.structure.company')}
          </Typography>
        </Box>
        <Stack direction="row" spacing={2} sx={{ ml: 'auto', color: 'text.secondary', fontVariantNumeric: 'tabular-nums' }}>
          <Typography variant="body2">
            {t('hierarchy.structure.units', { count: totals.units })}
          </Typography>
          <Typography variant="body2">
            {t('hierarchy.structure.employees', { count: totals.employees })}
          </Typography>
          <Typography variant="body2">
            {t('hierarchy.structure.projects', { count: totals.projects })}
          </Typography>
        </Stack>
      </Paper>

      {tree.length === 0 ? (
        <Typography color="text.secondary" sx={{ py: 3, textAlign: 'center' }}>
          {t('hierarchy.structure.noUnits')}
        </Typography>
      ) : (
        <Stack spacing={1.25} sx={{ ml: { xs: 1, sm: 2.5 }, pl: { xs: 1.5, sm: 2 }, borderLeft: 2, borderColor: 'divider' }}>
          {tree.map((node) => (
            <UnitCard
              key={node.branch.id}
              node={node}
              peopleByUnit={peopleByUnit}
              open={open}
              toggle={toggle}
              onOpenPerson={onOpenPerson}
              onQuickEdit={canEdit ? setQuick : undefined}
            />
          ))}

          {loose.length > 0 && (
            <Paper variant="outlined" sx={{ borderStyle: 'dashed', px: 2, py: 1.5 }}>
              <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center', mb: 1 }}>
                <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
                  {t('hierarchy.structure.noUnit')}
                </Typography>
                <Chip size="small" label={t('hierarchy.people', { count: loose.length })} />
              </Stack>
              <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                {t('hierarchy.structure.noUnitHint')}
              </Typography>
              <UnitPeople people={loose} onOpenPerson={onOpenPerson} />
            </Paper>
          )}
        </Stack>
      )}

      <BranchQuickEditDialog target={quick} onClose={() => setQuick(null)} />
    </Stack>
  );
}
