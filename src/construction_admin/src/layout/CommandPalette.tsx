import {
  HistoryOutlined,
  SearchOutlined,
  StarOutlined,
  StarBorderOutlined,
} from '@mui/icons-material';
import {
  CircularProgress,
  Dialog,
  IconButton,
  InputAdornment,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  TextField,
  Typography,
} from '@mui/material';
import { useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';

import type { User } from '../api/types';
import {
  useGlobalSearch,
  type GlobalSearchResult,
} from '../features/globalSearch/useGlobalSearch';
import { useT } from '../i18n/useI18n';
import type { NavItem } from './navConfig';
import type { useFavorites } from './useFavorites';
import { readRecentRecords } from './useRecentRecords';

interface CommandPaletteProps {
  open: boolean;
  onClose: () => void;
  items: NavItem[];
  favorites: ReturnType<typeof useFavorites>;
  user: User | null | undefined;
}

type Row =
  | { kind: 'page'; key: string; label: string; sublabel?: string; icon: React.ReactNode; path: string }
  | { kind: 'entity'; key: string; label: string; sublabel?: string; icon: React.ReactNode; path: string }
  | { kind: 'recent'; key: string; label: string; sublabel?: string; icon: React.ReactNode; path: string };

export function CommandPalette({ open, onClose, items, favorites, user }: CommandPaletteProps) {
  const t = useT();
  const navigate = useNavigate();
  const [query, setQuery] = useState('');
  const [highlightedIndex, setHighlightedIndex] = useState(0);
  const [recent, setRecent] = useState<Row[]>([]);
  const inputRef = useRef<HTMLInputElement>(null);
  const { groups, loading } = useGlobalSearch(query, user);

  const pageResults = useMemo(() => {
    const q = query.trim().toLowerCase();
    if (!q) return items;
    return items.filter((item) => item.label.toLowerCase().includes(q));
  }, [items, query]);

  const rows: Row[] = useMemo(() => {
    const isEmptyQuery = query.trim() === '';
    const pageRows: Row[] = pageResults.map((item) => ({
      kind: 'page',
      key: `page:${item.path}`,
      label: item.label,
      icon: item.icon,
      path: item.path,
    }));
    const entityRows: Row[] = groups.flatMap((group) =>
      group.results.map(
        (result: GlobalSearchResult): Row => ({
          kind: 'entity',
          key: `${group.key}:${result.id}`,
          label: result.label,
          sublabel: result.sublabel,
          icon: group.icon,
          path: result.path,
        }),
      ),
    );
    return isEmptyQuery ? [...recent, ...pageRows] : [...pageRows, ...entityRows];
  }, [pageResults, groups, recent, query]);

  useEffect(() => {
    if (open) {
      setQuery('');
      setHighlightedIndex(0);
      setRecent(
        readRecentRecords().map((r) => ({
          kind: 'recent',
          key: `recent:${r.path}`,
          label: r.label,
          icon: <HistoryOutlined fontSize="small" />,
          path: r.path,
        })),
      );
      // Dialog mounts before its content is painted; wait a tick so autoFocus doesn't race it.
      requestAnimationFrame(() => inputRef.current?.focus());
    }
  }, [open]);

  useEffect(() => {
    setHighlightedIndex(0);
  }, [query]);

  const go = (path: string) => {
    navigate(path);
    onClose();
  };

  const handleKeyDown = (event: React.KeyboardEvent) => {
    if (event.key === 'ArrowDown') {
      event.preventDefault();
      setHighlightedIndex((i) => Math.min(i + 1, rows.length - 1));
    } else if (event.key === 'ArrowUp') {
      event.preventDefault();
      setHighlightedIndex((i) => Math.max(i - 1, 0));
    } else if (event.key === 'Enter') {
      event.preventDefault();
      const target = rows[highlightedIndex];
      if (target) go(target.path);
    } else if (event.key === 'Escape') {
      onClose();
    }
  };

  return (
    <Dialog
      open={open}
      onClose={onClose}
      maxWidth="sm"
      fullWidth
      slotProps={{ paper: { sx: { position: 'fixed', top: 96, m: 0 } } }}
    >
      <TextField
        inputRef={inputRef}
        value={query}
        onChange={(event) => setQuery(event.target.value)}
        onKeyDown={handleKeyDown}
        placeholder={t('commandPalette.placeholder')}
        variant="standard"
        fullWidth
        slotProps={{
          input: {
            disableUnderline: true,
            startAdornment: (
              <InputAdornment position="start">
                <SearchOutlined color="action" />
              </InputAdornment>
            ),
            endAdornment: loading ? (
              <InputAdornment position="end">
                <CircularProgress size={16} />
              </InputAdornment>
            ) : undefined,
          },
        }}
        sx={{ px: 2, py: 1.5 }}
      />
      <List sx={{ maxHeight: 420, overflowY: 'auto', pt: 0 }}>
        {rows.length === 0 && (
          <Typography variant="body2" color="text.secondary" sx={{ px: 2, py: 3 }}>
            {t('commandPalette.noResults')}
          </Typography>
        )}
        {recent.length > 0 && query.trim() === '' && (
          <Typography
            variant="caption"
            color="text.secondary"
            sx={{ px: 2, pt: 1, pb: 0.5, display: 'block', fontWeight: 600 }}
          >
            {t('commandPalette.recentHeading')}
          </Typography>
        )}
        {rows.map((row, index) => (
          <ListItemButton
            key={row.key}
            selected={index === highlightedIndex}
            onMouseEnter={() => setHighlightedIndex(index)}
            onClick={() => go(row.path)}
          >
            <ListItemIcon sx={{ minWidth: 40 }}>{row.icon}</ListItemIcon>
            <ListItemText primary={row.label} secondary={row.sublabel} />
            {row.kind === 'page' && (
              <IconButton
                size="small"
                edge="end"
                aria-label={t('nav.togglePin')}
                onClick={(event) => {
                  event.stopPropagation();
                  favorites.toggleFavorite(row.path);
                }}
              >
                {favorites.isFavorite(row.path) ? (
                  <StarOutlined fontSize="small" color="warning" />
                ) : (
                  <StarBorderOutlined fontSize="small" />
                )}
              </IconButton>
            )}
          </ListItemButton>
        ))}
      </List>
    </Dialog>
  );
}
