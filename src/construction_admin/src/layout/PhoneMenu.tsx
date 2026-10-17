import {
  ChevronLeftOutlined,
  ChevronRightOutlined,
  CloseOutlined,
  LanguageOutlined,
  LogoutOutlined,
  SearchOutlined,
  SettingsBrightnessOutlined,
  StarBorderOutlined,
  StarOutlined,
} from '@mui/icons-material';
import {
  alpha,
  Avatar,
  Badge,
  Box,
  ButtonBase,
  Dialog,
  IconButton,
  InputBase,
  Slide,
  Stack,
  Typography,
  type SlideProps,
} from '@mui/material';
import { forwardRef, useEffect, useMemo, useState, type ReactElement, type ReactNode, type Ref } from 'react';
import { Link } from 'react-router-dom';

import { useColorMode } from '../ColorMode';
import { BranchSwitcher } from '../features/branches/BranchSwitcher';
import { useT } from '../i18n/useI18n';
import { locales, localeNames, type Locale } from '../i18n/types';
import { isNavGroup, type NavEntry, type NavGroup, type NavItem } from './navConfig';

/** Rises from the bottom with a small give at the end, like a sheet; drops away a little faster. */
const Rise = forwardRef(function Rise(props: SlideProps & { children: ReactElement }, ref: Ref<unknown>) {
  return (
    <Slide
      direction="up"
      ref={ref}
      {...props}
      easing={{ enter: 'cubic-bezier(0.32, 1.12, 0.42, 1)', exit: 'cubic-bezier(0.4, 0, 1, 1)' }}
      timeout={{ enter: 460, exit: 240 }}
    />
  );
});

interface PhoneMenuProps {
  open: boolean;
  onClose: () => void;
  title: string;
  entries: NavEntry[];
  badgeCounts: Record<string, number>;
  hrefFor: (path: string) => string;
  isSelected: (item: NavItem) => boolean;
  isFavorite: (path: string) => boolean;
  toggleFavorite: (path: string) => void;
  locale: Locale;
  setLocale: (locale: Locale) => void;
  onSignOut: () => void;
  groupLabel?: (group: NavGroup) => string;
}

const SOFT = (mode: string) => (mode === 'dark' ? 'rgba(255,255,255,0.06)' : '#ffffff');

/**
 * The menu on a phone: one full screen, opened over the page. A search field on top, the four everyday pages in a
 * row, then every group as a large tile with the number waiting in it; a group opens as a page of its own with a
 * "Menu" button back. At the foot: the business unit, language, theme and signing out, where a thumb reaches them.
 */
export function PhoneMenu({
  open,
  onClose,
  title,
  entries,
  badgeCounts,
  hrefFor,
  isSelected,
  isFavorite,
  toggleFavorite,
  locale,
  setLocale,
  onSignOut,
}: PhoneMenuProps) {
  const t = useT();
  const colorMode = useColorMode();
  const [groupKey, setGroupKey] = useState<string | null>(null);
  const [query, setQuery] = useState('');

  // Closed, it forgets where it was: the next opening starts at the top.
  useEffect(() => {
    if (!open) {
      setGroupKey(null);
      setQuery('');
    }
  }, [open]);

  const quick = entries.filter((entry): entry is NavItem => !isNavGroup(entry));
  const groups = entries.filter(isNavGroup);
  const group = groups.find((g) => g.key === groupKey) ?? null;

  const flat = useMemo(
    () => [
      ...quick.map((item) => ({ item, group: '' })),
      ...groups.flatMap((g) => g.items.map((item) => ({ item, group: g.label }))),
    ],
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [entries],
  );
  const needle = query.trim().toLowerCase();
  const results = needle ? flat.filter(({ item, group: g }) => `${item.label} ${g}`.toLowerCase().includes(needle)) : [];

  const itemsOf = (g: NavGroup) => g.items.filter((item) => !item.inTabs);

  const row = (item: NavItem, secondary?: string) => (
    <Stack key={item.path} direction="row" sx={{ alignItems: 'center' }}>
      <ButtonBase
        component={Link}
        to={hrefFor(item.path)}
        onClick={onClose}
        sx={(theme) => ({
          flex: 1,
          minWidth: 0,
          minHeight: 56,
          gap: 1.75,
          px: 2,
          py: 1.25,
          justifyContent: 'flex-start',
          textAlign: 'left',
          fontSize: '1.02rem',
          color: isSelected(item) ? 'primary.main' : 'text.primary',
          fontWeight: isSelected(item) ? 700 : 500,
          transition: 'background-color 0.12s ease-out',
          '&:active': { bgcolor: theme.palette.action.selected },
        })}
      >
        <Badge badgeContent={badgeCounts[item.path] ?? 0} color="error" max={99} overlap="circular">
          {item.icon}
        </Badge>
        <Box component="span" sx={{ flex: 1, minWidth: 0, display: 'grid' }}>
          <span>{item.label}</span>
          {secondary && (
            <Typography component="span" variant="caption" color="text.secondary">
              {secondary}
            </Typography>
          )}
        </Box>
        <ChevronRightOutlined fontSize="small" sx={{ color: 'text.disabled' }} />
      </ButtonBase>
      <IconButton
        aria-label={t('nav.togglePin')}
        onClick={() => toggleFavorite(item.path)}
        sx={{ mr: 0.5, opacity: isFavorite(item.path) ? 1 : 0.45 }}
      >
        {isFavorite(item.path) ? <StarOutlined fontSize="small" color="warning" /> : <StarBorderOutlined fontSize="small" />}
      </IconButton>
    </Stack>
  );

  const list = (children: ReactNode) => (
    <Box
      sx={(theme) => ({
        bgcolor: SOFT(theme.palette.mode),
        borderRadius: '20px',
        boxShadow: theme.palette.mode === 'dark' ? 'none' : '0 1px 2px rgba(31,27,22,0.06), 0 8px 24px -12px rgba(31,27,22,0.14)',
        overflow: 'hidden',
        '& > * + *': { borderTop: '1px solid', borderColor: 'divider' },
      })}
    >
      {children}
    </Box>
  );

  const label = (text: string) => (
    <Typography variant="overline" color="text.secondary" sx={{ display: 'block', mt: 2.5, mb: 1, px: 0.5, letterSpacing: '0.08em', fontWeight: 600 }}>
      {text}
    </Typography>
  );

  const themeLabel = colorMode.choice === 'system' ? t('theme.system') : colorMode.choice === 'light' ? t('theme.light') : t('theme.dark');
  const nextLocale = locales[(locales.indexOf(locale) + 1) % locales.length] as Locale;

  const cell = (icon: ReactNode, name: string, value: ReactNode, onClick?: () => void, danger = false) => (
    <ButtonBase
      onClick={onClick}
      component={onClick ? 'button' : 'div'}
      sx={(theme) => ({
        width: '100%',
        gap: 1.5,
        px: 2,
        minHeight: 56,
        justifyContent: 'flex-start',
        bgcolor: SOFT(theme.palette.mode),
        borderRadius: '16px',
        color: danger ? 'error.main' : 'text.primary',
        fontSize: '1rem',
        textAlign: 'left',
        '&:active': onClick ? { bgcolor: theme.palette.action.selected } : undefined,
      })}
    >
      {icon}
      <Box component="span" sx={{ flex: 1, fontWeight: 600 }}>
        {name}
      </Box>
      <Box component="span" sx={{ color: 'text.secondary', fontSize: '0.92rem' }}>
        {value}
      </Box>
    </ButtonBase>
  );

  return (
    <Dialog
      fullScreen
      open={open}
      onClose={onClose}
      slots={{ transition: Rise }}
      aria-label={t('nav.menu')}
      slotProps={{ paper: { sx: { bgcolor: 'background.default', backgroundImage: 'none' } } }}
    >
      <Stack sx={{ height: '100%', minHeight: 0 }}>
        <Stack sx={{ px: 2, pt: 'max(12px, env(safe-area-inset-top, 0px))', pb: 1, gap: 1.25, flexShrink: 0 }}>
          <Stack direction="row" sx={{ alignItems: 'center', gap: 1 }}>
            {group ? (
              <ButtonBase
                onClick={() => setGroupKey(null)}
                sx={{ color: 'primary.main', fontWeight: 600, fontSize: '1rem', gap: 0.25, py: 1, pr: 1.5, borderRadius: 2 }}
              >
                <ChevronLeftOutlined />
                {t('nav.menu')}
              </ButtonBase>
            ) : (
              <Typography component="h2" sx={{ flex: 1, fontSize: '1.7rem', fontWeight: 800, letterSpacing: '-0.02em' }} noWrap>
                {title}
              </Typography>
            )}
            <Box sx={{ flex: group ? 1 : 0 }} />
            <IconButton onClick={onClose} aria-label={t('nav.closeMenu')} sx={{ bgcolor: 'action.hover' }}>
              <CloseOutlined />
            </IconButton>
          </Stack>
          <Stack
            direction="row"
            sx={(theme) => ({
              alignItems: 'center',
              gap: 1.25,
              px: 1.75,
              minHeight: 48,
              borderRadius: '14px',
              bgcolor: SOFT(theme.palette.mode),
              boxShadow: theme.palette.mode === 'dark' ? 'none' : '0 1px 2px rgba(31,27,22,0.06), 0 6px 18px -10px rgba(31,27,22,0.16)',
            })}
          >
            <SearchOutlined sx={{ color: 'text.secondary' }} />
            <InputBase
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              placeholder={t('nav.menuSearch')}
              type="search"
              fullWidth
              inputProps={{ 'aria-label': t('nav.menuSearch'), style: { fontSize: 16 } }}
            />
          </Stack>
        </Stack>

        <Box sx={{ flex: 1, minHeight: 0, overflowY: 'auto', px: 2, pb: 'max(24px, env(safe-area-inset-bottom, 0px))' }}>
          {needle ? (
            <>
              {label(results.length > 0 ? t('nav.menuResults') : t('nav.menuNoResults'))}
              {results.length > 0 && list(results.map(({ item, group: g }) => row(item, g || undefined)))}
            </>
          ) : group ? (
            <>
              <Typography component="h3" sx={{ fontSize: '1.9rem', fontWeight: 800, letterSpacing: '-0.02em', mt: 1, mb: 1.5, px: 0.5 }}>
                {group.label}
              </Typography>
              {list(
                itemsOf(group).map((item, index, all) => (
                  <Box key={item.path}>
                    {item.section && item.section !== all[index - 1]?.section && (
                      <Typography variant="overline" color="text.secondary" sx={{ display: 'block', px: 2, pt: 1.5, pb: 0.25, fontWeight: 600, letterSpacing: '0.08em' }}>
                        {item.section}
                      </Typography>
                    )}
                    {row(item)}
                  </Box>
                )),
              )}
            </>
          ) : (
            <>
              {label(t('nav.menuQuick'))}
              <Box sx={{ display: 'grid', gridTemplateColumns: `repeat(${Math.max(1, Math.min(4, quick.length))}, 1fr)`, gap: 1 }}>
                {quick.slice(0, 4).map((item) => (
                  <ButtonBase
                    key={item.path}
                    component={Link}
                    to={hrefFor(item.path)}
                    onClick={onClose}
                    sx={(theme) => ({
                      flexDirection: 'column',
                      gap: 0.75,
                      py: 1.5,
                      px: 0.5,
                      borderRadius: '16px',
                      bgcolor: SOFT(theme.palette.mode),
                      boxShadow: theme.palette.mode === 'dark' ? 'none' : '0 1px 2px rgba(31,27,22,0.06), 0 8px 20px -12px rgba(31,27,22,0.16)',
                      color: isSelected(item) ? 'primary.main' : 'text.primary',
                      outline: isSelected(item) ? `2px solid ${theme.palette.primary.main}` : 'none',
                      fontSize: '0.76rem',
                      fontWeight: 600,
                      textAlign: 'center',
                      transition: 'transform 0.12s ease-out',
                      '&:active': { transform: 'scale(0.97)' },
                    })}
                  >
                    <Avatar variant="rounded" sx={(theme) => ({ width: 34, height: 34, borderRadius: '11px', bgcolor: alpha(theme.palette.primary.main, 0.12), color: 'primary.main' })}>
                      <Badge badgeContent={badgeCounts[item.path] ?? 0} color="error" max={99}>
                        {item.icon}
                      </Badge>
                    </Avatar>
                    <span>{item.label}</span>
                  </ButtonBase>
                ))}
              </Box>

              {label(t('nav.menuAll'))}
              <Box sx={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 1.25 }}>
                {groups.map((g) => (
                  <ButtonBase
                    key={g.key}
                    aria-label={g.label}
                    onClick={() => setGroupKey(g.key)}
                    sx={(theme) => ({
                      position: 'relative',
                      minHeight: 108,
                      p: 1.75,
                      borderRadius: '20px',
                      bgcolor: SOFT(theme.palette.mode),
                      boxShadow: theme.palette.mode === 'dark' ? 'none' : '0 1px 2px rgba(31,27,22,0.06), 0 10px 24px -14px rgba(31,27,22,0.18)',
                      flexDirection: 'column',
                      alignItems: 'flex-start',
                      justifyContent: 'space-between',
                      textAlign: 'left',
                      transition: 'transform 0.12s ease-out',
                      '&:active': { transform: 'scale(0.97)' },
                    })}
                  >
                    <Avatar variant="rounded" sx={(theme) => ({ width: 36, height: 36, borderRadius: '12px', bgcolor: alpha(theme.palette.primary.main, 0.12), color: 'primary.main' })}>
                      {g.icon}
                    </Avatar>
                    {(badgeCounts[g.key] ?? 0) > 0 && (
                      <Box
                        component="span"
                        sx={{ position: 'absolute', top: 12, right: 12, minWidth: 22, height: 22, px: 0.75, borderRadius: 11, bgcolor: 'error.main', color: 'error.contrastText', fontSize: '0.75rem', fontWeight: 700, display: 'grid', placeItems: 'center' }}
                      >
                        {badgeCounts[g.key]}
                      </Box>
                    )}
                    <Box component="span" sx={{ display: 'grid', gap: 0.25, minWidth: 0, maxWidth: '100%' }}>
                      <Typography component="span" sx={{ fontWeight: 600, fontSize: '0.98rem', lineHeight: 1.2 }} noWrap>
                        {g.label}
                      </Typography>
                      <Typography component="span" variant="caption" color="text.secondary">
                        {t('nav.menuPages', { count: itemsOf(g).length })}
                      </Typography>
                    </Box>
                  </ButtonBase>
                ))}
              </Box>

              <Stack sx={{ gap: 1, mt: 3 }}>
                <Box
                  sx={(theme) => ({
                    bgcolor: SOFT(theme.palette.mode),
                    borderRadius: '16px',
                    px: 1,
                    minHeight: 56,
                    display: 'flex',
                    alignItems: 'center',
                    '& > *': { flex: 1 },
                  })}
                >
                  <BranchSwitcher />
                </Box>
                {cell(<LanguageOutlined />, t('common.language'), localeNames[locale], () => setLocale(nextLocale))}
                {cell(<SettingsBrightnessOutlined />, t('theme.title'), themeLabel, colorMode.cycle)}
                {cell(<LogoutOutlined />, t('common.signOut'), '', onSignOut, true)}
              </Stack>
            </>
          )}
        </Box>
      </Stack>
    </Dialog>
  );
}
