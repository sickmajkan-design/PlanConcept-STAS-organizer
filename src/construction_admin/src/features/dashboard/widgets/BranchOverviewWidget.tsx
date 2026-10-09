import { Box, Stack, Typography } from '@mui/material';
import { useMemo } from 'react';
import { Link } from 'react-router-dom';

import { BranchDot } from '../../branches/BranchDot';
import { buildBranchTree, flattenBranchTree } from '../../branches/branchTree';
import { useBranchesQuery } from '../../branches/useBranches';
import { useT } from '../../../i18n/useI18n';
import { paths } from '../../../routes/paths';
import type { DashboardWidgetProps } from '../widgetTypes';
import { WidgetShell } from './WidgetShell';

/** A thin bar scaled to the largest figure in the column, so units can be compared at a glance. */
function Bar({ value, max, color }: { value: number; max: number; color: string }) {
  return (
    <Box sx={{ height: 6, borderRadius: 3, bgcolor: 'action.hover', overflow: 'hidden', minWidth: 40 }}>
      <Box sx={{ width: `${max > 0 ? Math.max(value > 0 ? 4 : 0, (value / max) * 100) : 0}%`, height: '100%', bgcolor: color }} />
    </Box>
  );
}

/**
 * The business units side by side: who runs each, how many people and sites it has (counting the
 * units under it). For an owner of several branches this is the first thing to look at.
 */
export function BranchOverviewWidget({ instanceId: _instanceId, onRemove, onExpandWidth }: DashboardWidgetProps) {
  const t = useT();
  const { data: branches, isLoading, error } = useBranchesQuery();

  const rows = useMemo(
    () => flattenBranchTree(buildBranchTree((branches ?? []).filter((b) => b.isActive))),
    [branches],
  );
  const maxEmployees = Math.max(0, ...rows.map((row) => row.totals.employees));
  const maxProjects = Math.max(0, ...rows.map((row) => row.totals.projects));

  return (
    <WidgetShell
      title={t('dashboard.widget.BranchOverview')}
      isLoading={isLoading}
      error={error}
      onRemove={onRemove}
      onExpandWidth={onExpandWidth}
    >
      {rows.length === 0 && !isLoading ? (
        <Typography color="text.secondary" variant="body2">
          {t('dashboard.branchOverview.empty')}
        </Typography>
      ) : (
        <Stack spacing={1.25} sx={{ flex: 1, minHeight: 0, overflow: 'auto' }}>
          <Typography variant="caption" color="text.secondary">
            {t('dashboard.branchOverview.hint')}
          </Typography>
          {rows.map(({ branch, depth, totals }) => (
            <Box
              key={branch.id}
              sx={{
                display: 'grid',
                gridTemplateColumns: 'minmax(0, 1.4fr) minmax(0, 1fr) minmax(0, 1fr)',
                gap: 1.5,
                alignItems: 'center',
                pl: (depth - 1) * 2,
              }}
            >
              <Stack direction="row" spacing={1} sx={{ alignItems: 'center', minWidth: 0 }}>
                <BranchDot color={branch.color} />
                <Box sx={{ minWidth: 0 }}>
                  <Typography variant="body2" noWrap sx={{ fontWeight: 600 }} title={branch.name}>
                    {branch.name}
                  </Typography>
                  <Typography variant="caption" color="text.secondary" noWrap component="div">
                    {branch.headEmployeeName ?? t('hierarchy.structure.noHead')}
                  </Typography>
                </Box>
              </Stack>
              <Box>
                <Typography variant="caption" color="text.secondary" sx={{ fontVariantNumeric: 'tabular-nums' }}>
                  {t('hierarchy.structure.employees', { count: totals.employees })}
                </Typography>
                <Bar value={totals.employees} max={maxEmployees} color={branch.color} />
              </Box>
              <Box>
                <Typography variant="caption" color="text.secondary" sx={{ fontVariantNumeric: 'tabular-nums' }}>
                  {t('hierarchy.structure.projects', { count: totals.projects })}
                </Typography>
                <Bar value={totals.projects} max={maxProjects} color={branch.color} />
              </Box>
            </Box>
          ))}
          <Typography
            component={Link}
            to={paths.hierarchy}
            variant="body2"
            sx={{ color: 'primary.main', textDecoration: 'none', fontWeight: 600 }}
          >
            {t('dashboard.branchOverview.open')}
          </Typography>
        </Stack>
      )}
    </WidgetShell>
  );
}
