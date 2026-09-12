import { HelpOutlined } from '@mui/icons-material';
import { Dialog, DialogContent, DialogTitle, Tab, Tabs, Typography, Stack, Chip } from '@mui/material';
import { useState } from 'react';

import type { MessageKey } from '../i18n/en';
import { useT } from '../i18n/useI18n';

/** One numbered step or bullet in the guide, always one translated string. */
function GuideItem({ children }: { children: string }) {
  return (
    <Typography variant="body2" sx={{ mb: 1 }}>
      {children}
    </Typography>
  );
}

const ROLE_KEYS = ['superAdmin', 'admin', 'projectManager', 'foreman', 'worker'] as const;

/**
 * The short "how this works" reference the client asked for after using the
 * platform for a while without one — what the platform is for, and who does
 * what, since the five roles are not self-explanatory from the UI alone.
 * Opened from the AppBar's help icon, same pattern as {@link ShortcutsHelpDialog}.
 */
export function PlatformGuideDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const t = useT();
  const [tab, setTab] = useState(0);

  const usageSteps: MessageKey[] = [
    'guide.usage.step1',
    'guide.usage.step2',
    'guide.usage.step3',
    'guide.usage.step4',
    'guide.usage.step5',
    'guide.usage.step6',
  ];

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
        <HelpOutlined fontSize="small" />
        {t('guide.title')}
      </DialogTitle>
      <Tabs value={tab} onChange={(_, value) => setTab(value)} sx={{ px: 3 }}>
        <Tab label={t('guide.tabUsage')} />
        <Tab label={t('guide.tabRoles')} />
      </Tabs>
      <DialogContent>
        {tab === 0 && (
          <Stack spacing={0.5}>
            {usageSteps.map((key, index) => (
              <Stack key={key} direction="row" spacing={1.5}>
                <Typography variant="body2" color="text.secondary" sx={{ minWidth: 20 }}>
                  {index + 1}.
                </Typography>
                <GuideItem>{t(key)}</GuideItem>
              </Stack>
            ))}
          </Stack>
        )}

        {tab === 1 && (
          <Stack spacing={2.5}>
            {ROLE_KEYS.map((role) => (
              <Stack key={role} spacing={0.5}>
                <Chip
                  label={t(`guide.role.${role}.name` as MessageKey)}
                  size="small"
                  color="primary"
                  variant="outlined"
                  sx={{ alignSelf: 'flex-start', fontWeight: 600 }}
                />
                <Typography variant="body2" color="text.secondary">
                  {t(`guide.role.${role}.description` as MessageKey)}
                </Typography>
              </Stack>
            ))}
          </Stack>
        )}
      </DialogContent>
    </Dialog>
  );
}
