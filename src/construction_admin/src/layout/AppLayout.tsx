import {
  ArrowBackOutlined,
  LogoutOutlined,
  MenuOutlined,
  NotificationsNoneOutlined,
  PasswordOutlined,
  ExpandLess,
  ExpandMore,
  KeyboardOutlined,
  SearchOutlined,
  StarOutlined,
  StarBorderOutlined,
} from '@mui/icons-material';
import {
  AppBar,
  Avatar,
  Badge,
  Box,
  Breadcrumbs,
  ClickAwayListener,
  Collapse,
  Divider,
  Drawer,
  Fade,
  IconButton,
  Link as MuiLink,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Menu,
  MenuItem,
  MenuList,
  Paper,
  Popper,
  Toolbar,
  Tooltip,
  Typography,
  useMediaQuery,
  useTheme,
} from '@mui/material';
import { useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';

import { displayName } from '../auth/authHelpers';
import { useAuth } from '../auth/useAuth';
import { LanguageSwitcher } from '../components/LanguageSwitcher';
import { OfflineBanner } from '../components/OfflineBanner';
import { UndoSnackbarHost } from '../components/UndoSnackbarHost';
import { config } from '../config';
import { useCompanyBrandingQuery } from '../features/companySettings/useCompanySettings';
import { useUnreadCountQuery } from '../features/notifications/useNotifications';
import { useEnumLabel } from '../i18n/enumLabels';
import { useT } from '../i18n/useI18n';
import { paths } from '../routes/paths';
import { initialsOf } from '../utils/formatting';
import { CommandPalette } from './CommandPalette';
import {
  buildNavEntries,
  flattenNavEntries,
  getBreadcrumbTrail,
  isNavGroup,
  type NavGroup,
  type NavItem,
} from './navConfig';
import { isTypingTarget, ShortcutsHelpDialog } from './ShortcutsHelpDialog';
import { useFavorites } from './useFavorites';
import { useNavBadgeCounts } from './useNavBadgeCounts';

const RAIL_WIDTH = 72;
const MOBILE_DRAWER_WIDTH = 260;
const EXPANDED_GROUPS_STORAGE_KEY = 'nav.expandedGroups';

export function AppLayout({ children }: { children: ReactNode }) {
  const theme = useTheme();
  const isDesktop = useMediaQuery(theme.breakpoints.up('md'));
  const [mobileOpen, setMobileOpen] = useState(false);
  const [menuAnchor, setMenuAnchor] = useState<HTMLElement | null>(null);
  const [paletteOpen, setPaletteOpen] = useState(false);
  const [shortcutsOpen, setShortcutsOpen] = useState(false);
  const [railFlyout, setRailFlyout] = useState<{ key: string; anchorEl: HTMLElement } | null>(
    null,
  );
  const hoverTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const [expandedGroups, setExpandedGroups] = useState<Set<string>>(() => {
    try {
      const raw = localStorage.getItem(EXPANDED_GROUPS_STORAGE_KEY);
      return raw ? new Set(JSON.parse(raw) as string[]) : new Set();
    } catch {
      return new Set();
    }
  });

  const { user, signOut } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const t = useT();
  const enumLabel = useEnumLabel();
  const { data: unreadCount } = useUnreadCountQuery();
  const { data: branding } = useCompanyBrandingQuery();
  const favorites = useFavorites();
  const badgeCounts = useNavBadgeCounts(user);

  const navEntries = useMemo(() => (user ? buildNavEntries(user, t) : []), [user, t]);
  const flatItems = useMemo(() => flattenNavEntries(navEntries), [navEntries]);
  const favoriteItems = useMemo(
    () =>
      favorites.favorites
        .map((path) => flatItems.find((item) => item.path === path))
        .filter((item): item is NavItem => !!item),
    [favorites.favorites, flatItems],
  );

  const isItemSelected = (item: NavItem) =>
    item.path === paths.home
      ? location.pathname === paths.home
      : location.pathname.startsWith(item.path);

  /**
   * Detail/edit/new sub-pages (e.g. `/employees/:id/edit`) have no nav entry
   * of their own — the longest nav path that's a strict prefix of the current
   * one is their "list" page. A pathname that exactly matches a nav entry
   * (e.g. `/projects/annual-realization`, which is itself a page) is a
   * primary destination, not a sub-page, so it gets no back target.
   */
  const backTarget = useMemo(() => {
    if (location.pathname === paths.home) return null;
    if (flatItems.some((item) => item.path === location.pathname)) return null;
    let best: NavItem | null = null;
    for (const item of flatItems) {
      if (location.pathname.startsWith(`${item.path}/`) && (!best || item.path.length > best.path.length)) {
        best = item;
      }
    }
    return best;
  }, [location.pathname, flatItems]);

  const breadcrumbTrail = useMemo(
    () => getBreadcrumbTrail(navEntries, location.pathname, t('nav.home')),
    [navEntries, location.pathname, t],
  );

  const groupContainsActivePath = (group: NavGroup) => group.items.some(isItemSelected);

  useEffect(() => {
    for (const entry of navEntries) {
      if (isNavGroup(entry) && groupContainsActivePath(entry) && !expandedGroups.has(entry.key)) {
        setExpandedGroups((prev) => new Set(prev).add(entry.key));
      }
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [location.pathname]);

  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if ((event.metaKey || event.ctrlKey) && event.key.toLowerCase() === 'k') {
        event.preventDefault();
        setPaletteOpen(true);
        return;
      }
      if (event.key === '?' && !event.metaKey && !event.ctrlKey && !isTypingTarget(event.target)) {
        event.preventDefault();
        setShortcutsOpen(true);
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, []);

  useEffect(
    () => () => {
      if (hoverTimer.current) clearTimeout(hoverTimer.current);
    },
    [],
  );

  const toggleGroup = (key: string) => {
    setExpandedGroups((prev) => {
      const next = new Set(prev);
      if (next.has(key)) {
        next.delete(key);
      } else {
        next.add(key);
      }
      try {
        localStorage.setItem(EXPANDED_GROUPS_STORAGE_KEY, JSON.stringify([...next]));
      } catch {
        // best-effort persistence only
      }
      return next;
    });
  };

  if (!user) {
    return null;
  }

  const handleSignOut = async () => {
    setMenuAnchor(null);
    await signOut();
    navigate(paths.login, { replace: true });
  };

  const HOVER_OPEN_DELAY = 120;
  const HOVER_CLOSE_DELAY = 200;

  const clearHoverTimer = () => {
    if (hoverTimer.current) {
      clearTimeout(hoverTimer.current);
      hoverTimer.current = null;
    }
  };

  const scheduleFlyoutOpen = (key: string, target: HTMLElement) => {
    clearHoverTimer();
    hoverTimer.current = setTimeout(() => {
      setRailFlyout({ key, anchorEl: target });
    }, HOVER_OPEN_DELAY);
  };

  const scheduleFlyoutClose = () => {
    clearHoverTimer();
    hoverTimer.current = setTimeout(() => setRailFlyout(null), HOVER_CLOSE_DELAY);
  };

  const activeFlyoutGroup =
    railFlyout && navEntries.find((entry) => isNavGroup(entry) && entry.key === railFlyout.key);

  const railContent = (
    <Box
      sx={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        height: '100%',
        py: 1.5,
        gap: 0.5,
      }}
    >
      <Tooltip title={branding?.name || t('nav.appName')} placement="right">
        <IconButton component={Link} to={paths.home} sx={{ mb: 1 }}>
          {branding?.hasLogo ? (
            <Box
              component="img"
              src={`${config.apiBaseUrl}/api/v1/company-settings/logo`}
              alt=""
              sx={{ width: 28, height: 28, objectFit: 'contain', borderRadius: 0.5 }}
            />
          ) : (
            <Avatar sx={{ width: 32, height: 32, bgcolor: 'primary.main', fontSize: 14 }}>
              {(branding?.name || t('nav.appName')).slice(0, 1)}
            </Avatar>
          )}
        </IconButton>
      </Tooltip>

      <Tooltip title={t('commandPalette.trigger')} placement="right">
        <IconButton onClick={() => setPaletteOpen(true)} sx={{ color: 'text.secondary' }}>
          <SearchOutlined />
        </IconButton>
      </Tooltip>

      {favoriteItems.length > 0 && (
        <>
          <Divider flexItem sx={{ my: 0.5, width: '60%' }} />
          {favoriteItems.map((item) => (
            <Tooltip key={item.path} title={item.label} placement="right">
              <IconButton
                component={Link}
                to={item.path}
                color={isItemSelected(item) ? 'primary' : 'default'}
                sx={{
                  bgcolor: isItemSelected(item) ? 'action.selected' : 'transparent',
                }}
              >
                {item.icon}
              </IconButton>
            </Tooltip>
          ))}
        </>
      )}

      <Divider flexItem sx={{ my: 0.5, width: '60%' }} />

      {navEntries
        .filter((entry) => isNavGroup(entry) || entry.path !== paths.home)
        .map((entry) =>
          isNavGroup(entry) ? (
            <Tooltip
              key={entry.key}
              title={`${entry.label} — ${t('nav.hoverHint')}`}
              placement="top"
              disableInteractive
            >
              <IconButton
                onClick={() => {
                  clearHoverTimer();
                  setRailFlyout(null);
                  navigate(entry.items[0].path);
                }}
                onMouseEnter={(event) => scheduleFlyoutOpen(entry.key, event.currentTarget)}
                onMouseLeave={scheduleFlyoutClose}
                color={groupContainsActivePath(entry) ? 'primary' : 'default'}
                sx={{
                  bgcolor:
                    groupContainsActivePath(entry) || railFlyout?.key === entry.key
                      ? 'action.selected'
                      : 'transparent',
                }}
              >
                <Badge
                  badgeContent={badgeCounts[entry.key] ?? 0}
                  color="error"
                  max={99}
                  overlap="circular"
                >
                  {entry.icon}
                </Badge>
              </IconButton>
            </Tooltip>
          ) : (
            <Tooltip key={entry.path} title={entry.label} placement="right">
              <IconButton
                component={Link}
                to={entry.path}
                color={isItemSelected(entry) ? 'primary' : 'default'}
                sx={{
                  bgcolor: isItemSelected(entry) ? 'action.selected' : 'transparent',
                }}
              >
                {entry.icon}
              </IconButton>
            </Tooltip>
          ),
        )}

      <Popper
        open={!!railFlyout}
        anchorEl={railFlyout?.anchorEl}
        placement="right-start"
        transition
        sx={{ zIndex: (t2) => t2.zIndex.drawer + 2 }}
      >
        {({ TransitionProps }) => (
          <Fade {...TransitionProps} timeout={120}>
            <Paper
              elevation={4}
              onMouseEnter={clearHoverTimer}
              onMouseLeave={scheduleFlyoutClose}
              sx={{ minWidth: 220, py: 0.5 }}
            >
              <ClickAwayListener onClickAway={() => setRailFlyout(null)}>
                <MenuList>
                  {activeFlyoutGroup && isNavGroup(activeFlyoutGroup) && (
                    <>
                      <Typography
                        variant="caption"
                        color="text.secondary"
                        sx={{ px: 2, pt: 0.5, pb: 0.5, display: 'block', fontWeight: 600 }}
                      >
                        {activeFlyoutGroup.label}
                      </Typography>
                      {activeFlyoutGroup.items.map((item) => (
                        <MenuItem
                          key={item.path}
                          component={Link}
                          to={item.path}
                          selected={isItemSelected(item)}
                          onClick={() => setRailFlyout(null)}
                          sx={{ gap: 1 }}
                        >
                          <ListItemIcon sx={{ minWidth: 32 }}>{item.icon}</ListItemIcon>
                          {item.label}
                          <IconButton
                            size="small"
                            aria-label={t('nav.togglePin')}
                            sx={{ ml: 'auto' }}
                            onClick={(event) => {
                              event.preventDefault();
                              event.stopPropagation();
                              favorites.toggleFavorite(item.path);
                            }}
                          >
                            {favorites.isFavorite(item.path) ? (
                              <StarOutlined fontSize="inherit" color="warning" />
                            ) : (
                              <StarBorderOutlined fontSize="inherit" />
                            )}
                          </IconButton>
                        </MenuItem>
                      ))}
                    </>
                  )}
                </MenuList>
              </ClickAwayListener>
            </Paper>
          </Fade>
        )}
      </Popper>
    </Box>
  );

  const mobileDrawerContent = (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
      <Toolbar
        component={Link}
        to={paths.home}
        onClick={() => setMobileOpen(false)}
        sx={{ gap: 1, color: 'inherit', textDecoration: 'none' }}
      >
        {branding?.hasLogo && (
          <Box
            component="img"
            src={`${config.apiBaseUrl}/api/v1/company-settings/logo`}
            alt=""
            sx={{ width: 28, height: 28, objectFit: 'contain', flexShrink: 0 }}
          />
        )}
        <Typography variant="subtitle1" noWrap sx={{ fontWeight: 700 }}>
          {branding?.name || t('nav.appName')}
        </Typography>
      </Toolbar>
      <Divider />
      <Box sx={{ px: 1.5, pt: 1.5 }}>
        <ListItemButton
          onClick={() => setPaletteOpen(true)}
          sx={{
            borderRadius: 1,
            border: '1px solid',
            borderColor: 'divider',
            py: 0.75,
            color: 'text.secondary',
          }}
        >
          <ListItemIcon sx={{ minWidth: 32 }}>
            <SearchOutlined fontSize="small" />
          </ListItemIcon>
          <ListItemText primary={t('commandPalette.trigger')} />
          <Typography variant="caption" sx={{ opacity: 0.7 }}>
            Ctrl K
          </Typography>
        </ListItemButton>
      </Box>
      {favoriteItems.length > 0 && (
        <>
          <Typography
            variant="caption"
            color="text.secondary"
            sx={{ px: 2.5, pt: 2, pb: 0.5, display: 'block', fontWeight: 600 }}
          >
            {t('nav.favorites')}
          </Typography>
          <List sx={{ px: 1, py: 0 }}>
            {favoriteItems.map((item) => (
              <ListItemButton
                key={item.path}
                component={Link}
                to={item.path}
                selected={isItemSelected(item)}
                onClick={() => setMobileOpen(false)}
                sx={{ borderRadius: 1, mb: 0.5 }}
              >
                <ListItemIcon sx={{ minWidth: 40 }}>{item.icon}</ListItemIcon>
                <ListItemText primary={item.label} />
              </ListItemButton>
            ))}
          </List>
          <Divider sx={{ mx: 1.5 }} />
        </>
      )}
      <List sx={{ flex: 1, px: 1, py: 1, overflowY: 'auto' }}>
        {navEntries.map((entry) =>
          isNavGroup(entry) ? (
            <Box key={entry.key} sx={{ mb: 0.5 }}>
              <ListItemButton
                onClick={() => toggleGroup(entry.key)}
                selected={groupContainsActivePath(entry) && !expandedGroups.has(entry.key)}
                sx={{ borderRadius: 1 }}
              >
                <ListItemIcon sx={{ minWidth: 40 }}>
                  <Badge
                    badgeContent={badgeCounts[entry.key] ?? 0}
                    color="error"
                    max={99}
                    overlap="circular"
                  >
                    {entry.icon}
                  </Badge>
                </ListItemIcon>
                <ListItemText
                  primary={entry.label}
                  slotProps={{ primary: { sx: { fontWeight: 600 } } }}
                />
                {expandedGroups.has(entry.key) ? (
                  <ExpandLess fontSize="small" />
                ) : (
                  <ExpandMore fontSize="small" />
                )}
              </ListItemButton>
              <Collapse in={expandedGroups.has(entry.key)} timeout="auto" unmountOnExit>
                <List component="div" disablePadding>
                  {entry.items.map((item) => (
                    <ListItemButton
                      key={item.path}
                      component={Link}
                      to={item.path}
                      selected={isItemSelected(item)}
                      onClick={() => setMobileOpen(false)}
                      sx={{ borderRadius: 1, mb: 0.5, pl: 4, '&:hover .nav-pin': { opacity: 1 } }}
                    >
                      <ListItemIcon sx={{ minWidth: 40 }}>{item.icon}</ListItemIcon>
                      <ListItemText primary={item.label} />
                      <IconButton
                        size="small"
                        className="nav-pin"
                        aria-label={t('nav.togglePin')}
                        sx={{
                          opacity: favorites.isFavorite(item.path) ? 1 : 0,
                          transition: 'opacity 0.15s',
                        }}
                        onClick={(event) => {
                          event.preventDefault();
                          event.stopPropagation();
                          favorites.toggleFavorite(item.path);
                        }}
                      >
                        {favorites.isFavorite(item.path) ? (
                          <StarOutlined fontSize="inherit" color="warning" />
                        ) : (
                          <StarBorderOutlined fontSize="inherit" />
                        )}
                      </IconButton>
                    </ListItemButton>
                  ))}
                </List>
              </Collapse>
            </Box>
          ) : (
            <ListItemButton
              key={entry.path}
              component={Link}
              to={entry.path}
              selected={isItemSelected(entry)}
              onClick={() => setMobileOpen(false)}
              sx={{ borderRadius: 1, mb: 0.5 }}
            >
              <ListItemIcon sx={{ minWidth: 40 }}>{entry.icon}</ListItemIcon>
              <ListItemText primary={entry.label} />
            </ListItemButton>
          ),
        )}
      </List>
    </Box>
  );

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh' }}>
      <CommandPalette
        open={paletteOpen}
        onClose={() => setPaletteOpen(false)}
        items={flatItems}
        favorites={favorites}
        user={user}
      />
      <UndoSnackbarHost />
      <ShortcutsHelpDialog open={shortcutsOpen} onClose={() => setShortcutsOpen(false)} />
      <AppBar
        position="fixed"
        color="inherit"
        sx={{
          width: { md: `calc(100% - ${RAIL_WIDTH}px)` },
          ml: { md: `${RAIL_WIDTH}px` },
          bgcolor: 'background.paper',
        }}
      >
        <Toolbar sx={{ gap: 1 }}>
          {!isDesktop && (
            <IconButton edge="start" onClick={() => setMobileOpen(true)}>
              <MenuOutlined />
            </IconButton>
          )}
          {backTarget && (
            <Tooltip title={`${t('common.back')} — ${backTarget.label}`}>
              <IconButton component={Link} to={backTarget.path} edge={isDesktop ? 'start' : undefined}>
                <ArrowBackOutlined />
              </IconButton>
            </Tooltip>
          )}
          <Box sx={{ flex: 1 }} />
          <Tooltip title={t('shortcuts.title')}>
            <IconButton onClick={() => setShortcutsOpen(true)} aria-label={t('shortcuts.title')}>
              <KeyboardOutlined />
            </IconButton>
          </Tooltip>
          <LanguageSwitcher />
          {/* In the bar rather than the drawer: an inbox is personal, it is
              the same on every screen, and the count has to be visible from
              wherever the operator happens to be. */}
          <IconButton
            component={Link}
            to={paths.notifications}
            aria-label={t('notifications.title')}
          >
            <Badge badgeContent={unreadCount ?? 0} color="error" max={99}>
              <NotificationsNoneOutlined />
            </Badge>
          </IconButton>
          <IconButton onClick={(event) => setMenuAnchor(event.currentTarget)}>
            <Avatar sx={{ width: 34, height: 34, bgcolor: 'primary.main', fontSize: 14 }}>
              {initialsOf(user.firstName, user.lastName, user.email)}
            </Avatar>
          </IconButton>
          <Menu
            anchorEl={menuAnchor}
            open={!!menuAnchor}
            onClose={() => setMenuAnchor(null)}
            anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
            transformOrigin={{ vertical: 'top', horizontal: 'right' }}
          >
            <Box sx={{ px: 2, py: 1, minWidth: 200 }}>
              <Typography variant="subtitle2" noWrap sx={{ fontWeight: 700 }}>
                {displayName(user)}
              </Typography>
              <Typography variant="body2" color="text.secondary" noWrap>
                {user.email}
              </Typography>
              <Typography variant="caption" color="text.secondary">
                {enumLabel('role', user.role)}
              </Typography>
            </Box>
            <Divider />
            <MenuItem
              component={Link}
              to={paths.changePassword}
              onClick={() => setMenuAnchor(null)}
            >
              <ListItemIcon>
                <PasswordOutlined fontSize="small" />
              </ListItemIcon>
              {t('common.changePassword')}
            </MenuItem>
            <MenuItem onClick={handleSignOut}>
              <ListItemIcon>
                <LogoutOutlined fontSize="small" />
              </ListItemIcon>
              {t('common.signOut')}
            </MenuItem>
          </Menu>
        </Toolbar>
      </AppBar>

      <Box component="nav" sx={{ width: { md: RAIL_WIDTH }, flexShrink: { md: 0 } }}>
        <Drawer
          variant="temporary"
          open={mobileOpen}
          onClose={() => setMobileOpen(false)}
          ModalProps={{ keepMounted: true }}
          sx={{
            display: { xs: 'block', md: 'none' },
            '& .MuiDrawer-paper': { width: MOBILE_DRAWER_WIDTH },
          }}
        >
          {mobileDrawerContent}
        </Drawer>
        <Drawer
          variant="permanent"
          sx={{
            display: { xs: 'none', md: 'block' },
            '& .MuiDrawer-paper': { width: RAIL_WIDTH, borderRight: '1px solid #e0e0e0' },
          }}
          open
        >
          {railContent}
        </Drawer>
      </Box>

      <Box
        component="main"
        sx={{
          flexGrow: 1,
          width: { md: `calc(100% - ${RAIL_WIDTH}px)` },
          px: { xs: 2, sm: 3 },
          py: 3,
        }}
      >
        <Toolbar />
        {breadcrumbTrail.length > 0 && (
          <Breadcrumbs sx={{ mb: 1.5, fontSize: '0.875rem' }}>
            {breadcrumbTrail.map((segment, index) =>
              segment.path ? (
                <MuiLink
                  key={index}
                  component={Link}
                  to={segment.path}
                  underline="hover"
                  color="text.secondary"
                >
                  {segment.label}
                </MuiLink>
              ) : (
                <Typography key={index} color="text.primary" variant="body2">
                  {segment.label}
                </Typography>
              ),
            )}
          </Breadcrumbs>
        )}
        {/* Above the screen rather than inside it: the reason the numbers on
            every page have stopped moving is the same reason, and it should be
            stated once, in the same place, wherever the operator is. */}
        <OfflineBanner />
        {children}
      </Box>
    </Box>
  );
}
