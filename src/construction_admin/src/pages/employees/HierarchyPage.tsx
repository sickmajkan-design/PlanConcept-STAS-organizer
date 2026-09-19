import { Avatar, Box, CircularProgress, Paper, Stack, Tooltip, Typography } from '@mui/material';
import { useMemo } from 'react';

import type { OrganizationHierarchyNode, Role } from '../../api/types';
import { ErrorState } from '../../components/ErrorState';
import { PageHeader } from '../../components/PageHeader';
import { useOrganizationHierarchyQuery } from '../../features/employees/useEmployees';
import { useT } from '../../i18n/useI18n';

/**
 * Who reports at roughly what level, grouped by the account role each
 * employee signs in with rather than drawn as connecting lines — there is no
 * per-employee "reports to" relationship recorded anywhere in the system to
 * draw a real chart from, and the four groups below are the ones that
 * actually distinguish authority here (see `Policies`/`CostRules`).
 */
const TIERS: { key: string; roles: readonly (Role | null)[] }[] = [
  { key: 'management', roles: ['SuperAdmin'] },
  { key: 'admins', roles: ['Admin'] },
  { key: 'managers', roles: ['ProjectManager', 'Foreman'] },
  // A worker's account and a subcontractor with no account at all read the
  // same on an org chart: neither one supervises anybody.
  { key: 'workers', roles: ['Worker', null] },
];

function initials(fullName: string): string {
  const parts = fullName.trim().split(/\s+/);
  return ((parts[0]?.[0] ?? '') + (parts[parts.length - 1]?.[0] ?? '')).toUpperCase();
}

function PersonChip({ node }: { node: OrganizationHierarchyNode }) {
  return (
    <Tooltip title={node.position}>
      <Stack
        spacing={0.5}
        sx={{ width: 88, textAlign: 'center', alignItems: 'center' }}
      >
        <Avatar sx={{ width: 40, height: 40, fontSize: '0.9rem' }}>
          {initials(node.fullName)}
        </Avatar>
        <Typography variant="caption" noWrap sx={{ maxWidth: 88 }}>
          {node.fullName}
        </Typography>
      </Stack>
    </Tooltip>
  );
}

export function HierarchyPage() {
  const t = useT();
  const { data, isLoading, isError, error } = useOrganizationHierarchyQuery();

  const tierLabels: Record<string, string> = {
    management: t('hierarchy.tier.management'),
    admins: t('hierarchy.tier.admins'),
    managers: t('hierarchy.tier.managers'),
    workers: t('hierarchy.tier.workers'),
  };

  const tiers = useMemo(() => {
    if (!data) return [];

    return TIERS.map((tier) => ({
      ...tier,
      people: data.filter((node) => tier.roles.includes(node.role)),
    }));
  }, [data]);

  return (
    <Box>
      <PageHeader title={t('hierarchy.title')} description={t('hierarchy.description')} />

      {isLoading && (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 6 }}>
          <CircularProgress />
        </Box>
      )}

      {isError && <ErrorState error={error} />}

      {!isLoading && !isError && (
        <Stack spacing={2}>
          {tiers.map((tier) =>
            tier.people.length === 0 ? null : (
              <Paper key={tier.key} variant="outlined" sx={{ p: 2 }}>
                <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1.5 }}>
                  {tierLabels[tier.key]} ({tier.people.length})
                </Typography>
                <Stack direction="row" sx={{ flexWrap: 'wrap', gap: 2 }}>
                  {tier.people.map((node) => (
                    <PersonChip key={node.employeeId} node={node} />
                  ))}
                </Stack>
              </Paper>
            ),
          )}
        </Stack>
      )}
    </Box>
  );
}
