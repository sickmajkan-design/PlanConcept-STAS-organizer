import { CloseRounded, SendRounded } from '@mui/icons-material';
import {
  Alert,
  Box,
  Chip,
  CircularProgress,
  Divider,
  Drawer,
  IconButton,
  Paper,
  Stack,
  TextField,
  Toolbar,
  Tooltip,
  Typography,
} from '@mui/material';
import { useEffect, useRef, useState } from 'react';

import { useAssistant, type AssistantTurn } from '../../features/assistant/useAssistant';
import { useT } from '../../i18n/useI18n';

const PANEL_WIDTH = 420;

/**
 * The assistant, as a drawer beside whatever screen is open.
 *
 * A drawer rather than a page on purpose: the questions people ask it are
 * about the screen they are already on — this crew, this week, this site — and
 * navigating away to ask would lose that. It reads, and only reads; there is
 * no action in here that changes a record.
 */
export function AssistantPanel({ open, onClose }: { open: boolean; onClose: () => void }) {
  const t = useT();
  const { turns, ask, clear, isAsking } = useAssistant();
  const [draft, setDraft] = useState('');
  const bottom = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    // Optional-called: scrollIntoView is not implemented everywhere the panel
    // is mounted — jsdom has no layout — and failing to scroll must not throw
    // away the answer that was just rendered.
    bottom.current?.scrollIntoView?.({ block: 'end' });
  }, [turns, isAsking]);

  const submit = () => {
    if (!draft.trim() || isAsking) {
      return;
    }

    const asked = draft;
    setDraft('');
    void ask(asked);
  };

  return (
    <Drawer
      anchor="right"
      open={open}
      onClose={onClose}
      slotProps={{ paper: { sx: { width: { xs: '100%', sm: PANEL_WIDTH } } } }}
    >
      {/* Matches the app bar's height so the panel's own header lines up with it. */}
      <Toolbar />

      <Stack direction="row" sx={{ alignItems: 'center', px: 2, py: 1.5, gap: 1 }}>
        <Typography variant="h6" sx={{ flex: 1 }}>
          {t('assistant.title')}
        </Typography>

        {turns.length > 0 && (
          <Tooltip title={t('assistant.clear')}>
            <IconButton onClick={clear} size="small" aria-label={t('assistant.clear')}>
              <Typography variant="body2">{t('assistant.clear')}</Typography>
            </IconButton>
          </Tooltip>
        )}

        <IconButton onClick={onClose} size="small" aria-label={t('common.close')}>
          <CloseRounded />
        </IconButton>
      </Stack>

      <Divider />

      <Box sx={{ flex: 1, overflowY: 'auto', px: 2, py: 2 }}>
        {turns.length === 0 && !isAsking && <EmptyState />}

        <Stack spacing={1.5}>
          {turns.map((turn) => (
            <Turn key={turn.id} turn={turn} />
          ))}

          {isAsking && (
            <Stack direction="row" spacing={1} sx={{ alignItems: 'center', px: 1 }}>
              <CircularProgress size={16} />
              <Typography variant="body2" color="text.secondary">
                {t('assistant.thinking')}
              </Typography>
            </Stack>
          )}
        </Stack>

        <div ref={bottom} />
      </Box>

      <Divider />

      <Box sx={{ p: 2 }}>
        <Stack direction="row" spacing={1} sx={{ alignItems: 'flex-end' }}>
          <TextField
            fullWidth
            multiline
            maxRows={5}
            size="small"
            value={draft}
            disabled={isAsking}
            label={t('assistant.ask')}
            placeholder={t('assistant.placeholder')}
            onChange={(event) => setDraft(event.target.value)}
            onKeyDown={(event) => {
              // Enter sends, Shift+Enter breaks the line. A question here is
              // one sentence far more often than it is a paragraph.
              if (event.key === 'Enter' && !event.shiftKey) {
                event.preventDefault();
                submit();
              }
            }}
          />

          <IconButton
            color="primary"
            onClick={submit}
            disabled={isAsking || !draft.trim()}
            aria-label={t('assistant.send')}
          >
            <SendRounded />
          </IconButton>
        </Stack>

        <Typography variant="caption" color="text.secondary" sx={{ mt: 1, display: 'block' }}>
          {t('assistant.disclaimer')}
        </Typography>
      </Box>
    </Drawer>
  );
}

function EmptyState() {
  const t = useT();

  return (
    <Stack spacing={1} sx={{ py: 4, px: 1 }}>
      <Typography variant="body2" color="text.secondary">
        {t('assistant.emptyTitle')}
      </Typography>
      <Typography variant="body2" color="text.secondary">
        {t('assistant.emptyExample1')}
      </Typography>
      <Typography variant="body2" color="text.secondary">
        {t('assistant.emptyExample2')}
      </Typography>
    </Stack>
  );
}

function Turn({ turn }: { turn: AssistantTurn }) {
  const t = useT();

  if (turn.role === 'error') {
    return <Alert severity="error">{turn.text}</Alert>;
  }

  const mine = turn.role === 'user';

  return (
    <Stack spacing={0.5} sx={{ alignItems: mine ? 'flex-end' : 'flex-start' }}>
      <Paper
        variant="outlined"
        sx={{
          px: 1.5,
          py: 1,
          maxWidth: '90%',
          bgcolor: mine ? 'action.hover' : 'background.paper',
        }}
      >
        {/* The model answers in prose with its own line breaks; preserving
            them is the difference between a readable list and one paragraph. */}
        <Typography variant="body2" sx={{ whiteSpace: 'pre-wrap' }}>
          {turn.text}
        </Typography>
      </Paper>

      {turn.truncated && (
        <Alert severity="warning" sx={{ width: '100%' }}>
          {t('assistant.truncated')}
        </Alert>
      )}

      {/* Named so the reader can see the answer was looked up rather than
          recalled, and which records it came from. */}
      {turn.toolsUsed && turn.toolsUsed.length > 0 && (
        <Stack direction="row" spacing={0.5} useFlexGap sx={{ flexWrap: 'wrap' }}>
          {turn.toolsUsed.map((tool) => (
            <Chip key={tool} size="small" variant="outlined" label={toolLabel(t, tool)} />
          ))}
        </Stack>
      )}
    </Stack>
  );
}

/**
 * What a tool is called, in the reader's language.
 *
 * The tool list lives on the server, so it can name one this build has no
 * translation for — a tool added before the panel is redeployed. `t` returns
 * the key itself when it knows nothing, and "assistant.tool.list_crews" is
 * worse to read than "list_crews", so that case falls back to the bare name.
 */
function toolLabel(t: ReturnType<typeof useT>, tool: string): string {
  const key = `assistant.tool.${tool}` as Parameters<typeof t>[0];
  const label = t(key);

  return label === key ? tool : label;
}
