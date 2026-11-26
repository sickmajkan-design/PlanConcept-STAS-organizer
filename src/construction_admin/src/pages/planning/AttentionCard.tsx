import { Box, ButtonBase, Button, Paper, Stack, Typography } from '@mui/material';
import { alpha } from '@mui/material/styles';

import type { BoardAlert } from '../../features/planning/equipment';
import { useT } from '../../i18n/useI18n';

export interface AttentionItem {
  key: BoardAlert;
  label: string;
  count: number;
  /** Red for what leaves a place short or somebody on the wrong site; amber for the rest. */
  tone: 'warning' | 'error';
}

/**
 * "What needs attention" above the board: one button per kind of trouble, with how many. Pressing one highlights what it is
 * about on the board (nothing is dimmed); pressing it again, or "Show all", takes the highlight away.
 */
export function AttentionCard({ items, active, onPick }: { items: AttentionItem[]; active: BoardAlert | null; onPick: (key: BoardAlert | null) => void }) {
  const t = useT();

  if (items.length === 0) return null;

  return (
    <Paper variant="outlined" component="section" aria-label={t('planning.attn.title')} sx={{ borderRadius: 2.5, p: 1.75 }}>
      <Stack direction="row" spacing={1.5} sx={{ alignItems: 'baseline', flexWrap: 'wrap', mb: 1.25 }} useFlexGap>
        <Typography variant="subtitle1" component="h2" sx={{ fontWeight: 700 }}>
          {t('planning.attn.title')}
        </Typography>
        <Typography variant="caption" color="text.secondary" sx={{ flex: 1 }}>
          {t('planning.attn.hint')}
        </Typography>
        {active && (
          <Button size="small" color="inherit" onClick={() => onPick(null)} sx={{ textTransform: 'none', borderRadius: 999, bgcolor: 'action.hover' }}>
            {t('planning.attn.showAll')}
          </Button>
        )}
      </Stack>
      <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap' }}>
        {items.map((item) => {
          const on = active === item.key;

          return (
            <ButtonBase
              key={item.key}
              aria-pressed={on}
              onClick={() => onPick(on ? null : item.key)}
              sx={(theme) => ({
                display: 'inline-flex',
                alignItems: 'center',
                gap: 1,
                minHeight: 40,
                pl: 0.75,
                pr: 1.75,
                borderRadius: 2,
                border: 2,
                borderColor: `${item.tone}.main`,
                bgcolor: on ? 'text.primary' : alpha(theme.palette[item.tone].main, 0.1),
                color: on ? 'background.paper' : 'text.primary',
                typography: 'body2',
                fontWeight: 600,
                textAlign: 'left',
              })}
            >
              <Box
                component="span"
                sx={{
                  minWidth: 26,
                  height: 26,
                  px: 0.75,
                  borderRadius: 999,
                  display: 'inline-flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  bgcolor: `${item.tone}.main`,
                  color: `${item.tone}.contrastText`,
                  fontSize: 13,
                  fontWeight: 700,
                }}
              >
                {item.count}
              </Box>
              {item.label}
            </ButtonBase>
          );
        })}
      </Stack>
    </Paper>
  );
}
