import { Badge, Box, Tab, Tabs } from '@mui/material';
import { Link, Outlet, useLocation } from 'react-router-dom';

import { useAuth } from '../auth/useAuth';
import { canSeeLabourCost } from '../auth/authHelpers';
import { useT } from '../i18n/useI18n';
import { paths } from '../routes/paths';
import { useNavBadgeCounts } from './useNavBadgeCounts';

interface SectionTab {
  path: string;
  label: string;
  /** Something waiting on the reader in this tab. */
  badge?: number;
}

function TabStrip({ tabs }: { tabs: SectionTab[] }) {
  const { pathname } = useLocation();
  // The longest matching path wins, so a sub-page (fuel import, an
  // accommodation's detail) keeps its parent tab highlighted.
  const current = tabs
    .filter((tab) => pathname === tab.path || pathname.startsWith(`${tab.path}/`))
    .sort((a, b) => b.path.length - a.path.length)[0];

  return (
    <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 2 }}>
      <Tabs value={current?.path ?? false} variant="scrollable" scrollButtons="auto">
        {tabs.map((tab) => (
          <Tab
            key={tab.path}
            value={tab.path}
            label={
              tab.badge ? (
                <Badge badgeContent={tab.badge} color="error" max={99} sx={{ pr: 1.5 }}>
                  {tab.label}
                </Badge>
              ) : (
                tab.label
              )
            }
            component={Link}
            to={tab.path}
          />
        ))}
      </Tabs>
    </Box>
  );
}

/**
 * Every cost ledger lives behind one "Cost records" entry in the menu; this is
 * the strip that moves between them. The routes themselves are unchanged, so
 * every existing link and notification still lands where it did.
 */
export function CostRecordsLayout() {
  const t = useT();
  const { user } = useAuth();
  const counts = useNavBadgeCounts(user);

  const tabs: SectionTab[] = [
    {
      path: paths.vehicleExpenses,
      label: t('nav.vehicleExpenses'),
      badge: counts[paths.vehicleExpenses],
    },
    { path: paths.toolExpenses, label: t('nav.toolExpenses') },
    { path: paths.generalExpenses, label: t('nav.generalExpenses') },
    { path: paths.stockMovements, label: t('nav.stockMovements') },
    { path: paths.accommodations, label: t('nav.accommodations') },
    ...(user && canSeeLabourCost(user)
      ? [{ path: paths.financeEntries, label: t('nav.financeEntries') }]
      : []),
  ];

  return (
    <>
      <TabStrip tabs={tabs} />
      <Outlet />
    </>
  );
}

/** Pay rates, public holidays and the annual plan: set-and-forget, so one menu entry. */
export function BillingSettingsLayout() {
  const t = useT();

  return (
    <>
      <TabStrip
        tabs={[
          { path: paths.rates, label: t('nav.rates') },
          { path: paths.publicHolidays, label: t('nav.publicHolidays') },
          { path: paths.annualRealization, label: t('nav.annualRealization') },
        ]}
      />
      <Outlet />
    </>
  );
}
