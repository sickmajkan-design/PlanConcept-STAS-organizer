import { CheckCircleOutlined, ChevronRightOutlined } from '@mui/icons-material';
import { Box, Button, Card, CardContent, Stack, Typography } from '@mui/material';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';

import type { SetupChecklistItem, SetupChecklistKey } from '../../api/onboarding';
import { ImportEmployeesDialog } from '../../components/ImportEmployeesDialog';
import type { MessageKey } from '../../i18n/en';
import { useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';
import { useSetupChecklistQuery } from './useOnboarding';

/** Where each gap is fixed. `import` opens the spreadsheet import instead of a page; `server` has no screen. */
const DESTINATIONS: Record<SetupChecklistKey, string> = {
  companyProfile: paths.companySettings,
  noEmployees: 'import',
  noProjects: paths.projectNew,
  employeesWithoutAccount: paths.employees,
  employeesWithoutProject: paths.employees,
  projectsWithoutLocation: paths.projects,
  holidaysMissing: paths.publicHolidays,
  emailNotConfigured: 'server',
  pushNotConfigured: 'server',
};

/** The keys that only the server's owner can fix, listed apart from the rest. */
const SERVER_KEYS: SetupChecklistKey[] = ['emailNotConfigured', 'pushNotConfigured'];

/** A step of the first-run guide. Done when its checklist key is gone. */
interface WizardStep {
  id: string;
  key: SetupChecklistKey;
  title: MessageKey;
  help: MessageKey;
  /** Keys that must also be gone: inviting people means nothing until there are people. */
  alsoNeeds?: SetupChecklistKey[];
}

const WIZARD_STEPS: WizardStep[] = [
  { id: 'company', key: 'companyProfile', title: 'onboarding.wizard.company', help: 'onboarding.wizard.companyHelp' },
  { id: 'project', key: 'noProjects', title: 'onboarding.wizard.project', help: 'onboarding.wizard.projectHelp' },
  { id: 'employees', key: 'noEmployees', title: 'onboarding.wizard.employees', help: 'onboarding.wizard.employeesHelp' },
  {
    id: 'invites',
    key: 'employeesWithoutAccount',
    alsoNeeds: ['noEmployees'],
    title: 'onboarding.wizard.invites',
    help: 'onboarding.wizard.invitesHelp',
  },
];

/** The guide is only for a system that is still empty. */
const EMPTY_KEYS: SetupChecklistKey[] = ['companyProfile', 'noProjects', 'noEmployees'];

const SKIPPED_STORAGE_KEY = 'onboarding.wizard.skipped';

function readSkipped(): string[] {
  try {
    const raw = window.localStorage.getItem(SKIPPED_STORAGE_KEY);
    const parsed: unknown = raw ? JSON.parse(raw) : [];

    return Array.isArray(parsed) ? parsed.filter((x): x is string => typeof x === 'string') : [];
  } catch {
    return [];
  }
}

function writeSkipped(ids: string[]): void {
  try {
    window.localStorage.setItem(SKIPPED_STORAGE_KEY, JSON.stringify(ids));
  } catch {
    // Storage blocked: the step comes back next visit, which is harmless.
  }
}

/**
 * What is still missing before the system does its job, on the first screen an
 * administrator sees. Computed from live data by the server, so it shrinks as
 * things are fixed and reappears if something breaks again — and renders
 * nothing at all when there is nothing to do.
 *
 * A system that is still empty gets a guide first: company, project, people,
 * invitations, each step skippable. Which step is done is read from the same
 * list, never remembered, so it cannot disagree with it. A skipped step's gap
 * moves down into the list, where it stays until it is fixed.
 */
export function SetupChecklistCard() {
  const t = useT();
  const navigate = useNavigate();
  const { data } = useSetupChecklistQuery();
  const [importing, setImporting] = useState(false);
  const [skipped, setSkipped] = useState<string[]>(readSkipped);

  const items = data?.items ?? [];

  if (items.length === 0) return null;

  const present = new Set(items.map((item) => item.key));

  const go = (key: SetupChecklistKey) => {
    const destination = DESTINATIONS[key];

    if (destination === 'import') {
      setImporting(true);
    } else if (destination !== 'server') {
      navigate(destination);
    }
  };

  const skip = (id: string) => {
    const next = [...skipped, id];
    setSkipped(next);
    writeSkipped(next);
  };

  const isDone = (step: WizardStep) =>
    !present.has(step.key) && !(step.alsoNeeds ?? []).some((key) => present.has(key));

  const showWizard = EMPTY_KEYS.some((key) => present.has(key));
  const currentStep = showWizard
    ? WIZARD_STEPS.find((step) => !isDone(step) && !skipped.includes(step.id))
    : undefined;

  // The guide covers its own gaps; the list keeps everything else, and whatever a skipped step left.
  const listed = items.filter((item) => {
    if (SERVER_KEYS.includes(item.key)) return false;
    if (!showWizard) return true;

    const step = WIZARD_STEPS.find((s) => s.key === item.key);

    return step === undefined || skipped.includes(step.id);
  });
  const serverProblems = items.filter((item) => SERVER_KEYS.includes(item.key));

  return (
    <Card variant="outlined" sx={{ mb: 2 }}>
      <CardContent>
        {showWizard && (
          <Box sx={{ mb: listed.length > 0 || serverProblems.length > 0 ? 2 : 0 }}>
            <Typography variant="h6" sx={{ fontWeight: 700 }}>
              {t('onboarding.wizard.title')}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {t('onboarding.wizard.subtitle')}
            </Typography>

            <Stack spacing={1} sx={{ mt: 1.5 }}>
              {WIZARD_STEPS.map((step, index) => {
                const done = isDone(step);
                const active = currentStep?.id === step.id;

                return (
                  <Stack
                    key={step.id}
                    direction="row"
                    spacing={1.5}
                    sx={{
                      alignItems: 'center',
                      // On a phone the buttons go under the text instead of squeezing it.
                      flexWrap: 'wrap',
                      p: 1,
                      borderRadius: 1,
                      border: 1,
                      borderColor: active ? 'primary.main' : 'divider',
                      opacity: done ? 0.6 : 1,
                    }}
                  >
                    {done ? (
                      <CheckCircleOutlined color="success" fontSize="small" />
                    ) : (
                      <Typography variant="body2" sx={{ fontWeight: 700, width: 20, textAlign: 'center' }}>
                        {index + 1}
                      </Typography>
                    )}
                    <Box sx={{ flexGrow: 1, flexBasis: { xs: 'calc(100% - 44px)', sm: 0 }, minWidth: 0 }}>
                      <Typography variant="body2" sx={{ fontWeight: 600 }}>
                        {t(step.title)}
                      </Typography>
                      {active && (
                        <Typography variant="caption" color="text.secondary">
                          {t(step.help)}
                        </Typography>
                      )}
                    </Box>
                    {active && (
                      <Stack direction="row" spacing={1} sx={{ ml: { xs: 'auto', sm: 0 } }}>
                        <Button size="small" onClick={() => skip(step.id)}>
                          {t('onboarding.wizard.skip')}
                        </Button>
                        <Button
                          size="small"
                          variant="contained"
                          endIcon={<ChevronRightOutlined />}
                          onClick={() => go(step.key)}
                        >
                          {t('onboarding.wizard.go')}
                        </Button>
                      </Stack>
                    )}
                  </Stack>
                );
              })}
            </Stack>
          </Box>
        )}

        {listed.length > 0 && (
          <>
            <Stack direction="row" spacing={1} sx={{ alignItems: 'baseline', flexWrap: 'wrap' }} useFlexGap>
              <Typography variant="h6" sx={{ fontWeight: 700 }}>
                {t('onboarding.checklist.title')}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {t('onboarding.checklist.subtitle')}
              </Typography>
            </Stack>

            <ChecklistRows items={listed} onFix={go} />
          </>
        )}

        {serverProblems.length > 0 && (
          <Box sx={{ mt: listed.length > 0 || showWizard ? 2 : 0 }}>
            {listed.length === 0 && !showWizard && (
              <Typography variant="h6" sx={{ fontWeight: 700 }}>
                {t('onboarding.checklist.title')}
              </Typography>
            )}
            {serverProblems.map((item) => (
              <Typography key={item.key} variant="body2" sx={{ py: 0.5 }}>
                {t(`onboarding.checklist.${item.key}` as MessageKey, { count: item.count })}
              </Typography>
            ))}
            <Typography variant="caption" color="text.secondary">
              {t('onboarding.checklist.serverHint')}
            </Typography>
          </Box>
        )}
      </CardContent>

      <ImportEmployeesDialog open={importing} onClose={() => setImporting(false)} />
    </Card>
  );
}

function ChecklistRows({
  items,
  onFix,
}: {
  items: SetupChecklistItem[];
  onFix: (key: SetupChecklistKey) => void;
}) {
  const t = useT();

  return (
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
          <Button size="small" endIcon={<ChevronRightOutlined />} onClick={() => onFix(item.key)}>
            {t('onboarding.checklist.fix')}
          </Button>
        </Stack>
      ))}
    </Stack>
  );
}
