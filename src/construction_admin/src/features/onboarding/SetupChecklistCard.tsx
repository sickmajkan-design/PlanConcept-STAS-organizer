import { ChevronRightOutlined } from '@mui/icons-material';
import { Box, Button, Card, CardContent, Stack, Typography } from '@mui/material';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';

import type { SetupChecklistKey } from '../../api/onboarding';
import { ImportEmployeesDialog } from '../../components/ImportEmployeesDialog';
import type { MessageKey } from '../../i18n/en';
import { useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';
import { useSetupChecklistQuery } from './useOnboarding';

/** Where each gap is fixed. `import` opens the spreadsheet import instead of a page. */
const DESTINATIONS: Record<SetupChecklistKey, string> = {
  companyProfile: paths.companySettings,
  noEmployees: 'import',
  noProjects: paths.projectNew,
  employeesWithoutAccount: paths.employees,
  employeesWithoutProject: paths.employees,
  projectsWithoutLocation: paths.projects,
  holidaysMissing: paths.publicHolidays,
};

/**
 * What is still missing before the system does its job, on the first screen an
 * administrator sees. Computed from live data by the server, so it shrinks as
 * things are fixed and reappears if something breaks again — and renders
 * nothing at all when there is nothing to do.
 */
export function SetupChecklistCard() {
  const t = useT();
  const navigate = useNavigate();
  const { data } = useSetupChecklistQuery();
  const [importing, setImporting] = useState(false);

  const items = data?.items ?? [];

  if (items.length === 0) return null;

  const go = (key: SetupChecklistKey) => {
    const destination = DESTINATIONS[key];

    if (destination === 'import') {
      setImporting(true);
    } else {
      navigate(destination);
    }
  };

  return (
    <Card variant="outlined" sx={{ mb: 2 }}>
      <CardContent>
        <Stack direction="row" spacing={1} sx={{ alignItems: 'baseline', flexWrap: 'wrap' }} useFlexGap>
          <Typography variant="h6" sx={{ fontWeight: 700 }}>
            {t('onboarding.checklist.title')}
          </Typography>
          <Typography variant="body2" color="text.secondary">
            {t('onboarding.checklist.subtitle')}
          </Typography>
        </Stack>

        <Stack divider={<Box sx={{ borderTop: 1, borderColor: 'divider' }} />} sx={{ mt: 1 }}>
          {items.map((item) => (
            <Stack
              key={item.key}
              direction="row"
              spacing={1.5}
              sx={{ alignItems: 'center', justifyContent: 'space-between', py: 1 }}
            >
              <Typography variant="body2">
                {t(`onboarding.checklist.${item.key}` as MessageKey, { count: item.count })}
              </Typography>
              <Button size="small" endIcon={<ChevronRightOutlined />} onClick={() => go(item.key)}>
                {t('onboarding.checklist.fix')}
              </Button>
            </Stack>
          ))}
        </Stack>
      </CardContent>

      <ImportEmployeesDialog open={importing} onClose={() => setImporting(false)} />
    </Card>
  );
}
