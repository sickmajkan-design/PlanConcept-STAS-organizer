import { KeyboardOutlined } from '@mui/icons-material';
import {
  Dialog,
  DialogContent,
  DialogTitle,
  List,
  ListItem,
  ListItemText,
  Chip,
  Stack,
} from '@mui/material';

import type { MessageKey } from '../i18n/en';
import { useT } from '../i18n/useI18n';

/**
 * Whether the given event happened while the user was typing somewhere —
 * `?` is a real character in a search box, so the global `?` listener (in
 * `AppLayout`) only opens this outside any text input.
 */
export function isTypingTarget(target: EventTarget | null): boolean {
  if (!(target instanceof HTMLElement)) return false;
  const tag = target.tagName;
  return tag === 'INPUT' || tag === 'TEXTAREA' || target.isContentEditable;
}

interface Shortcut {
  keys: string;
  descriptionKey: MessageKey;
}

const SHORTCUTS: Shortcut[] = [
  { keys: 'Ctrl K', descriptionKey: 'shortcuts.commandPalette' },
  { keys: '?', descriptionKey: 'shortcuts.help' },
  { keys: 'Esc', descriptionKey: 'shortcuts.closeDialog' },
];

/**
 * A list of shortcuts that otherwise only reveal themselves by accident
 * (Ctrl+K, hovering a rail group icon, the ‹ › on a detail page reached
 * from a list). Opened by `?` or the AppBar's keyboard icon — `AppLayout`
 * owns the open state so both triggers, present or future, share it.
 */
export function ShortcutsHelpDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const t = useT();

  return (
    <Dialog open={open} onClose={onClose} maxWidth="xs" fullWidth>
      <DialogTitle sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
        <KeyboardOutlined fontSize="small" />
        {t('shortcuts.title')}
      </DialogTitle>
      <DialogContent>
        <List dense disablePadding>
          {SHORTCUTS.map((shortcut) => (
            <ListItem key={shortcut.keys} disableGutters>
              <Stack direction="row" spacing={1} sx={{ minWidth: 90 }}>
                {shortcut.keys.split(' ').map((key) => (
                  <Chip key={key} label={key} size="small" variant="outlined" />
                ))}
              </Stack>
              <ListItemText primary={t(shortcut.descriptionKey)} />
            </ListItem>
          ))}
          <ListItem disableGutters>
            <ListItemText
              secondary={t('shortcuts.railHint')}
              slotProps={{ secondary: { sx: { fontStyle: 'italic' } } }}
            />
          </ListItem>
          <ListItem disableGutters>
            <ListItemText
              secondary={t('shortcuts.siblingNavHint')}
              slotProps={{ secondary: { sx: { fontStyle: 'italic' } } }}
            />
          </ListItem>
        </List>
      </DialogContent>
    </Dialog>
  );
}
