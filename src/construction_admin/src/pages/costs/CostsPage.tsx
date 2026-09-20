import { Box, Button, ButtonGroup, Stack, Tab, Tabs, TextField } from '@mui/material';
import { useState } from 'react';

import { PageHeader } from '../../components/PageHeader';
import { useT } from '../../i18n/useI18n';
import { VehicleCostBoard, ToolCostBoard } from './AssetCostBoards';
import { monthOf, yearOf, type Period } from './monthWindow';
import { ProjectCostBoard } from './ProjectCostBoard';
import { RentalsOutBoard } from './RentalsOutBoard';

export function CostsPage() {
  const t = useT();
  const [tab, setTab] = useState<'projects' | 'vehicles' | 'tools' | 'rentalsOut'>('projects');
  const [period, setPeriod] = useState<Period>(() => monthOf(new Date()));

  return (
    <Box>
      <PageHeader title={t('costs.title')} description={t('costs.subtitle')} />

      <PeriodPicker period={period} onChange={setPeriod} />

      <Tabs variant="scrollable" scrollButtons="auto"
        value={tab}
        onChange={(_event, value) => setTab(value as typeof tab)}
        sx={{ mb: 2 }}
      >
        <Tab value="projects" label={t('costs.projects')} />
        <Tab value="vehicles" label={t('costs.vehicles')} />
        <Tab value="tools" label={t('costs.tools')} />
        <Tab value="rentalsOut" label={t('costs.rentalsOut')} />
      </Tabs>

      {tab === 'projects' && <ProjectCostBoard period={period} />}
      {tab === 'vehicles' && <VehicleCostBoard period={period} />}
      {tab === 'tools' && <ToolCostBoard period={period} />}
      {tab === 'rentalsOut' && <RentalsOutBoard period={period} />}
    </Box>
  );
}

function PeriodPicker({
  period,
  onChange,
}: {
  period: Period;
  onChange: (period: Period) => void;
}) {
  const t = useT();

  return (
    <Stack
      direction={{ xs: 'column', md: 'row' }}
      spacing={2} useFlexGap
      sx={{ flexWrap: 'wrap', mb: 2, alignItems: { md: 'center' } }}
    >
      {/* The three periods anyone actually asks for, before the date fields:
          "what did last month cost" is the question, and making somebody type
          two dates to ask it is how a report goes unread. */}
      <ButtonGroup size="small">
        <Button onClick={() => onChange(monthOf(new Date()))}>
          {t('costs.thisMonth')}
        </Button>
        <Button onClick={() => onChange(monthOf(new Date(), 1))}>
          {t('costs.lastMonth')}
        </Button>
        <Button onClick={() => onChange(yearOf(new Date()))}>
          {t('costs.thisYear')}
        </Button>
      </ButtonGroup>

      <TextField
        type="date"
        size="small"
        label={t('costs.from')}
        value={period.from}
        onChange={(event) => onChange({ ...period, from: event.target.value })}
        slotProps={{ inputLabel: { shrink: true } }}
      />
      <TextField
        type="date"
        size="small"
        label={t('costs.to')}
        value={period.to}
        onChange={(event) => onChange({ ...period, to: event.target.value })}
        slotProps={{ inputLabel: { shrink: true } }}
      />
    </Stack>
  );
}
