import {
  ArrowBackOutlined,
  ChevronRightOutlined,
  LogoutOutlined,
  MenuOutlined,
  PasswordOutlined,
  PlaceOutlined,
  PushPin,
  PushPinOutlined,
  ExpandLess,
  ExpandMore,
  HelpOutlined,
  KeyboardOutlined,
  MoreHorizOutlined,
  SearchOutlined,
  StarOutlined,
  StarBorderOutlined,
} from '@mui/icons-material';
import {
  AppBar,
  Avatar,
  Badge,
  BottomNavigation,
  BottomNavigationAction,
  Box,
  Breadcrumbs,
  ButtonBase,
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
  Paper,
  Popper,
  Toolbar,
  Tooltip,
  Typography,
  useMediaQuery,
  useTheme,
} from '@mui/material';
import { alpha, keyframes } from '@mui/material/styles';
import { useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';

import { canViewDirectory, displayName } from '../auth/authHelpers';
import { useAuth } from '../auth/useAuth';
import { readScoped, storageScope, writeScoped } from '../hooks/userScopedStorage';
import { LanguageSwitcher } from '../components/LanguageSwitcher';
import { OfflineBanner } from '../components/OfflineBanner';
import { UndoSnackbarHost } from '../components/UndoSnackbarHost';
import { useCompanyLogoUrl } from '../features/companySettings/useCompanyLogoUrl';
import {
  useCompanyBrandingQuery,
  useCompanySettingsQuery,
} from '../features/companySettings/useCompanySettings';
import type { MessageKey } from '../i18n/en';
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
import { BranchSwitcher } from '../features/branches/BranchSwitcher';
import { NotificationsMenu } from './NotificationsMenu';
import { usePresenceHeartbeat } from '../features/presence/usePresence';
import { ReleaseNotesDialog } from '../features/releaseNotes/ReleaseNotesDialog';
import { PlatformGuideDialog } from './PlatformGuideDialog';
import { isTypingTarget, ShortcutsHelpDialog } from './ShortcutsHelpDialog';
import { useFavorites } from './useFavorites';
import { useNavBadgeCounts } from './useNavBadgeCounts';
import { usePinnedPanel } from './usePinnedPanel';

/**
 * The pages worth one tap on a phone, most useful first. The bottom bar shows the
 * first four of these that the signed-in role may open, then "More" for the
 * rest — so a Foreman and a Super Admin each get a bar that never points at a
 * page that would answer 403.
 */
const PHONE_BAR_PATHS = [
  paths.home,
  paths.schedule,
  paths.timeEntries,
  paths.projects,
  paths.employees,
  paths.workItems,
  paths.absences,
  paths.notifications,
];
const PHONE_BAR_SLOTS = 4;
const PHONE_BAR_HEIGHT = 64;

/**
 * Every label the same size, on one line. MUI enlarges the label of the selected
 * item to 14 px, and on a 360-pixel phone (a Galaxy S9+) five items leave 72 px
 * each, so a selected "Radno vrijeme" wrapped to two lines and stretched the bar.
 * A long label is cut with an ellipsis rather than allowed to push its neighbours.
 */
const PHONE_BAR_ACTION_SX = {
  minWidth: 0,
  px: 0.25,
  '& .MuiBottomNavigationAction-label, &.Mui-selected .MuiBottomNavigationAction-label': {
    fontSize: '0.75rem',
    whiteSpace: 'nowrap',
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    maxWidth: '100%',
  },
} as const;

const RAIL_WIDTH = 72;
/** The panel that slides out of the rail with the pages of one group. */
const PANEL_WIDTH = 320;
/** Top bar height from `md` up, where the company's logo is shown in its middle. */
const TOP_BAR_HEIGHT = 80;
/**
 * How much room the page area (the screen minus the menu) must have before the company block
 * in the middle of the top bar shows the logo alone, with the name, and with the address too.
 * Measured against the room that is left, so a pinned menu panel hides it sooner.
 */
const BRAND_MIN_CONTENT = { logo: 760, name: 940, address: 1180 } as const;
const MOBILE_DRAWER_WIDTH = 260;
const EXPANDED_GROUPS_KEY = 'nav.expandedGroups';

const panelItemIn = keyframes`
  from { opacity: 0; transform: translateX(-10px); }
  to { opacity: 1; transform: none; }
`;

/** How long the pointer must hover the rail logo before the enlarged preview appears. */
const LOGO_PREVIEW_HOVER_DELAY_MS = 500;

export function AppLayout({ children }: { children: ReactNode }) {
  const theme = useTheme();
  const isDesktop = useMediaQuery(theme.breakpoints.up('md'));
  const isPhone = useMediaQuery(theme.breakpoints.down('sm'));
  const isWide = useMediaQuery(theme.breakpoints.up('lg'));
  const [mobileOpen, setMobileOpen] = useState(false);
  const [menuAnchor, setMenuAnchor] = useState<HTMLElement | null>(null);
  const [paletteOpen, setPaletteOpen] = useState(false);
  const [shortcutsOpen, setShortcutsOpen] = useState(false);
  const [guideOpen, setGuideOpen] = useState(false);
  // Which group's panel is slid out of the rail; `openTick` replays the items' entrance each time.
  const [flyoutKey, setFlyoutKey] = useState<string | null>(null);
  const [openTick, setOpenTick] = useState(0);
  const { pinned, togglePinned } = usePinnedPanel();
  const menuWidth = RAIL_WIDTH + (pinned && isWide ? PANEL_WIDTH : 0);
  const brandRoom = {
    logo: useMediaQuery(`(min-width:${menuWidth + BRAND_MIN_CONTENT.logo}px)`),
    name: useMediaQuery(`(min-width:${menuWidth + BRAND_MIN_CONTENT.name}px)`),
    address: useMediaQuery(`(min-width:${menuWidth + BRAND_MIN_CONTENT.address}px)`),
  };
  const hoverTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const [logoPreviewAnchor, setLogoPreviewAnchor] = useState<HTMLElement | null>(null);
  const logoHoverTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const { user, signOut } = useAuth();
  const scope = storageScope(user);
  const [expandedGroups, setExpandedGroups] = useState<Set<string>>(
    () => new Set(readScoped<string[]>(scope, EXPANDED_GROUPS_KEY, [])),
  );

  useEffect(() => {
    setExpandedGroups(new Set(readScoped<string[]>(scope, EXPANDED_GROUPS_KEY, [])));
  }, [scope]);

  const navigate = useNavigate();
  const location = useLocation();
  usePresenceHeartbeat(location.pathname, !!user);
  const t = useT();
  const enumLabel = useEnumLabel();
  const { data: branding } = useCompanyBrandingQuery();
  const logoUrl = useCompanyLogoUrl();
  const { data: companyDetails } = useCompanySettingsQuery(canViewDirectory(user));
  const favorites = useFavorites();
  const badgeCounts = useNavBadgeCounts(user);

  /**
   * Where clicking a nav item actually goes. Plain `item.path`, except for
   * Time Entries with a pending-review badge: its badge counts entries that
   * can be on any day, but the page itself only ever shows one day at a
   * time — so a badged click opens the detailed review queue dialog
   * directly instead, which finds them regardless of which day they're on.
   */
  const navItemHref = (path: string) =>
    path === paths.timeEntries && (badgeCounts[path] ?? 0) > 0
      ? `${path}?reviewQueue=true`
      : path;

  const navEntries = useMemo(() => (user ? buildNavEntries(user, t) : []), [user, t]);
  const flatItems = useMemo(() => flattenNavEntries(navEntries), [navEntries]);

  const phoneBarItems = useMemo(
    () =>
      PHONE_BAR_PATHS.map((path) => flatItems.find((item) => item.path === path))
        .filter((item): item is NavItem => !!item)
        .slice(0, PHONE_BAR_SLOTS),
    [flatItems],
  );
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
      : [item.path, ...(item.alsoActiveOn ?? [])].some((path) =>
          location.pathname.startsWith(path),
        );

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
      if (logoHoverTimer.current) clearTimeout(logoHoverTimer.current);
    },
    [],
  );

  const scheduleLogoPreview = (target: HTMLElement) => {
    if (logoHoverTimer.current) clearTimeout(logoHoverTimer.current);
    logoHoverTimer.current = setTimeout(() => {
      setLogoPreviewAnchor(target);
    }, LOGO_PREVIEW_HOVER_DELAY_MS);
  };

  const cancelLogoPreview = () => {
    if (logoHoverTimer.current) {
      clearTimeout(logoHoverTimer.current);
      logoHoverTimer.current = null;
    }
    setLogoPreviewAnchor(null);
  };

  const toggleGroup = (key: string) => {
    setExpandedGroups((prev) => {
      const next = new Set(prev);
      if (next.has(key)) {
        next.delete(key);
      } else {
        next.add(key);
      }
      writeScoped(scope, EXPANDED_GROUPS_KEY, [...next]);
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

  const openFlyout = (key: string) => {
    clearHoverTimer();
    setFlyoutKey((current) => {
      if (current !== key) setOpenTick((tick) => tick + 1);
      return key;
    });
  };

  const scheduleFlyoutOpen = (key: string) => {
    clearHoverTimer();
    hoverTimer.current = setTimeout(() => openFlyout(key), HOVER_OPEN_DELAY);
  };

  const scheduleFlyoutClose = () => {
    clearHoverTimer();
    if (menuAnchor) return;
    hoverTimer.current = setTimeout(() => setFlyoutKey(null), HOVER_CLOSE_DELAY);
  };

  // The pinned panel stays beside the rail, so it needs room: only from `lg` up.
  const panelDocked = pinned && isWide;
  const navGroups = navEntries.filter(isNavGroup);
  const activeGroup = navGroups.find(groupContainsActivePath) ?? navGroups[0];
  const shownGroup = navGroups.find((group) => group.key === flyoutKey) ?? (panelDocked ? activeGroup : undefined);
  const panelOpen = !!shownGroup;
  const navWidth = RAIL_WIDTH + (panelDocked ? PANEL_WIDTH : 0);
  const contentWidth = `calc(100% - ${navWidth}px)`;
  const showBrandLogo = isDesktop && brandRoom.logo;
  const showBrandName = showBrandLogo && brandRoom.name;
  const showBrandAddress = showBrandName && brandRoom.address;
  const reduceMotion = '@media (prefers-reduced-motion: reduce)';

  const railContent = (
    <Box
      data-nav-rail
      sx={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        height: '100%',
        py: 1.5,
        gap: 0.5,
        overflowY: 'auto',
        scrollbarWidth: 'none',
      }}
    >
      <Tooltip title={t('nav.home')} placement="right">
        <IconButton
          component={Link}
          to={paths.home}
          sx={{ mb: 1 }}
          onMouseEnter={(event) => scheduleLogoPreview(event.currentTarget)}
          onMouseLeave={cancelLogoPreview}
        >
          {branding?.hasLogo ? (
            <Box
              component="img"
              src={logoUrl}
              alt=""
              sx={{
                width: 48,
                height: 48,
                p: 0.5,
                objectFit: 'contain',
                borderRadius: 2,
                bgcolor: 'background.paper',
                border: '1px solid',
                borderColor: 'divider',
              }}
            />
          ) : (
            <Avatar variant="rounded" sx={{ width: 44, height: 44, bgcolor: 'primary.main', fontSize: 20 }}>
              {(branding?.name || t('nav.appName')).slice(0, 1)}
            </Avatar>
          )}
        </IconButton>
      </Tooltip>

      <Popper
        open={Boolean(logoPreviewAnchor)}
        anchorEl={logoPreviewAnchor}
        placement="right-start"
        transition
        sx={{ zIndex: (theme) => theme.zIndex.tooltip }}
        modifiers={[{ name: 'offset', options: { offset: [0, 12] } }]}
      >
        {({ TransitionProps }) => (
          <Fade {...TransitionProps} timeout={150}>
            <Paper
              elevation={8}
              sx={{ p: 2, width: 280 }}
              onMouseEnter={() => {
                if (logoHoverTimer.current) clearTimeout(logoHoverTimer.current);
              }}
              onMouseLeave={cancelLogoPreview}
            >
              <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 1.5 }}>
                {branding?.hasLogo ? (
                  <Box
                    component="img"
                    src={logoUrl}
                    alt=""
                    sx={{
                      width: 220,
                      height: 220,
                      objectFit: 'contain',
                      borderRadius: 1,
                      bgcolor: 'grey.50',
                    }}
                  />
                ) : (
                  <Avatar
                    variant="rounded"
                    sx={{ width: 220, height: 220, bgcolor: 'primary.main', fontSize: 64 }}
                  >
                    {(branding?.name || t('nav.appName')).slice(0, 1)}
                  </Avatar>
                )}

                <Box sx={{ display: 'flex', flexDirection: 'column', gap: 0.25, width: '100%' }}>
                  <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>
                    {branding?.name || t('nav.appName')}
                  </Typography>
                  {companyDetails?.address && (
                    <Typography variant="body2" color="text.secondary">
                      {companyDetails.address}
                    </Typography>
                  )}
                  {companyDetails?.taxId && (
                    <Typography variant="body2" color="text.secondary">
                      {t('companySettings.taxId')}: {companyDetails.taxId}
                    </Typography>
                  )}
                  {companyDetails?.phone && (
                    <Typography variant="body2" color="text.secondary">
                      {companyDetails.phone}
                    </Typography>
                  )}
                  {companyDetails?.email && (
                    <Typography variant="body2" color="text.secondary">
                      {companyDetails.email}
                    </Typography>
                  )}
                </Box>

                <Divider flexItem sx={{ width: '100%' }} />

                <Box sx={{ display: 'flex', flexDirection: 'column', gap: 0.25, width: '100%' }}>
                  <Typography variant="caption" color="text.secondary">
                    {t('companySettings.previewLoggedInAs')}
                  </Typography>
                  <Typography variant="body2" sx={{ fontWeight: 600 }}>
                    {displayName(user)}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    {user.email}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    {t(`role.${user.role}` as MessageKey)}
                  </Typography>
                </Box>
              </Box>
            </Paper>
          </Fade>
        )}
      </Popper>

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
                to={navItemHref(item.path)}
                color={isItemSelected(item) ? 'primary' : 'default'}
                sx={{
                  bgcolor: isItemSelected(item) ? 'action.selected' : 'transparent',
                }}
              >
                <Badge badgeContent={badgeCounts[item.path] ?? 0} color="error" max={99} overlap="circular">
                  {item.icon}
                </Badge>
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
            // No Tooltip here on purpose: the panel that slides out on the same hover
            // names the group as its own header, so a floating tooltip would land in
            // the same place at once. `aria-label` keeps the name for screen readers.
            <IconButton
              key={entry.key}
              aria-label={entry.label}
              aria-expanded={shownGroup?.key === entry.key}
              onClick={() => openFlyout(entry.key)}
              onMouseEnter={() => {
                if (!panelDocked) scheduleFlyoutOpen(entry.key);
              }}
              onMouseLeave={() => {
                if (!panelDocked) scheduleFlyoutClose();
              }}
              color={groupContainsActivePath(entry) ? 'primary' : 'default'}
              sx={{
                position: 'relative',
                width: 48,
                height: 48,
                borderRadius: 2,
                // Three looks, so the rail always says where you are: filled for the group the open
                // page belongs to, a soft tint for the group whose panel is open, and for the
                // planning group (the one used most) a light orange that is never mistaken for "here".
                bgcolor: groupContainsActivePath(entry)
                  ? 'primary.main'
                  : shownGroup?.key === entry.key
                    ? 'action.selected'
                    : entry.emphasized
                      ? (theme) => alpha(theme.palette.primary.main, 0.12)
                      : 'transparent',
                color: groupContainsActivePath(entry)
                  ? 'primary.contrastText'
                  : entry.emphasized
                    ? 'primary.main'
                    : 'text.secondary',
                boxShadow: groupContainsActivePath(entry) ? 3 : 'none',
                transition: 'background-color 0.18s, color 0.18s, box-shadow 0.18s',
                '&:hover': {
                  bgcolor: groupContainsActivePath(entry) ? 'primary.dark' : 'action.hover',
                },
                // The marker on the rail's edge for the group the open page belongs to.
                ...(groupContainsActivePath(entry) && {
                  '&::before': {
                    content: '""',
                    position: 'absolute',
                    left: -10,
                    top: 12,
                    bottom: 12,
                    width: 4,
                    borderRadius: '0 4px 4px 0',
                    bgcolor: 'primary.main',
                  },
                }),
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
          ) : (
            <Tooltip key={entry.path} title={entry.label} placement="right">
              <IconButton
                component={Link}
                to={navItemHref(entry.path)}
                color={isItemSelected(entry) ? 'primary' : 'default'}
                sx={{
                  bgcolor: isItemSelected(entry) ? 'action.selected' : 'transparent',
                }}
              >
                <Badge badgeContent={badgeCounts[entry.path] ?? 0} color="error" max={99} overlap="circular">
                  {entry.icon}
                </Badge>
              </IconButton>
            </Tooltip>
          ),
        )}

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
        {branding?.hasLogo ? (
          <Box
            component="img"
            src={logoUrl}
            alt=""
            sx={{ width: 36, height: 36, objectFit: 'contain', flexShrink: 0, borderRadius: 1 }}
          />
        ) : (
          <Avatar variant="rounded" sx={{ width: 36, height: 36, bgcolor: 'primary.main', fontSize: 18 }}>
            {(branding?.name || t('nav.appName')).slice(0, 1)}
          </Avatar>
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
          <ListItemText primary={t('commandPalette.trigger')} slotProps={{ primary: { noWrap: true } }} />
          <Typography variant="caption" sx={{ opacity: 0.7, display: { xs: 'none', md: 'block' }, whiteSpace: 'nowrap', ml: 1 }}>
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
                to={navItemHref(item.path)}
                selected={isItemSelected(item)}
                onClick={() => setMobileOpen(false)}
                sx={{ borderRadius: 1, mb: 0.5 }}
              >
                <ListItemIcon sx={{ minWidth: 40 }}>
                  <Badge badgeContent={badgeCounts[item.path] ?? 0} color="error" max={99} overlap="circular">
                    {item.icon}
                  </Badge>
                </ListItemIcon>
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
                  {entry.items.filter((item) => !item.inTabs).map((item) => (
                    // A button nested inside the link it sits on used to
                    // pollute the link's accessible name with the star's own
                    // label ("Employees Pin to favorites" instead of
                    // "Employees") — invalid HTML (interactive-in-interactive)
                    // as well as a real accessibility bug, not just a test
                    // inconvenience. Siblings inside a plain row fix both.
                    <Box
                      key={item.path}
                      sx={{
                        display: 'flex',
                        alignItems: 'center',
                        mb: 0.5,
                        '&:hover .nav-pin': { opacity: 1 },
                      }}
                    >
                      <ListItemButton
                        component={Link}
                        to={navItemHref(item.path)}
                        selected={isItemSelected(item)}
                        onClick={() => setMobileOpen(false)}
                        sx={{ borderRadius: 1, pl: 4, flex: 1, minWidth: 0 }}
                      >
                        <ListItemIcon sx={{ minWidth: 40 }}>
                          <Badge
                            badgeContent={badgeCounts[item.path] ?? 0}
                            color="error"
                            max={99}
                            overlap="circular"
                          >
                            {item.icon}
                          </Badge>
                        </ListItemIcon>
                        <ListItemText primary={item.label} />
                      </ListItemButton>
                      <IconButton
                        size="small"
                        className="nav-pin"
                        aria-label={t('nav.togglePin')}
                        // Full opacity when starred, otherwise always at least
                        // partly visible rather than 0 by default — this menu
                        // is also the one a phone gets (no desktop rail
                        // there), and touch has no hover to reveal a fully
                        // transparent icon with. The `&:hover` above still
                        // brightens it further for a mouse.
                        sx={{
                          opacity: favorites.isFavorite(item.path) ? 1 : 0.35,
                          transition: 'opacity 0.15s',
                          mr: 1,
                        }}
                        onClick={() => favorites.toggleFavorite(item.path)}
                      >
                        {favorites.isFavorite(item.path) ? (
                          <StarOutlined fontSize="inherit" color="warning" />
                        ) : (
                          <StarBorderOutlined fontSize="inherit" />
                        )}
                      </IconButton>
                    </Box>
                  ))}
                </List>
              </Collapse>
            </Box>
          ) : (
            <ListItemButton
              key={entry.path}
              component={Link}
              to={navItemHref(entry.path)}
              selected={isItemSelected(entry)}
              onClick={() => setMobileOpen(false)}
              sx={{ borderRadius: 1, mb: 0.5 }}
            >
              <ListItemIcon sx={{ minWidth: 40 }}>
                <Badge badgeContent={badgeCounts[entry.path] ?? 0} color="error" max={99} overlap="circular">
                  {entry.icon}
                </Badge>
              </ListItemIcon>
              <ListItemText primary={entry.label} />
            </ListItemButton>
          ),
        )}
      </List>
    </Box>
  );

  return (
    <Box sx={{ display: 'flex', minHeight: '100dvh' }}>
      <CommandPalette
        open={paletteOpen}
        onClose={() => setPaletteOpen(false)}
        items={flatItems}
        favorites={favorites}
        user={user}
        badgeCounts={badgeCounts}
      />
      <UndoSnackbarHost />
      <ShortcutsHelpDialog open={shortcutsOpen} onClose={() => setShortcutsOpen(false)} />
      <PlatformGuideDialog open={guideOpen} onClose={() => setGuideOpen(false)} />
      <ReleaseNotesDialog />
      <AppBar
        position="fixed"
        color="inherit"
        sx={{
          width: { md: contentWidth },
          ml: { md: `${navWidth}px` },
          bgcolor: 'background.paper',
          transition: 'width .2s, margin .2s',
          [reduceMotion]: { transition: 'none' },
        }}
      >
        <Toolbar sx={{ gap: 1, position: 'relative', minHeight: { md: TOP_BAR_HEIGHT } }}>
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

          {/* The company's own identity in the middle of the bar. It sits in the flex flow
              between two spacers, so it can never run under the buttons: it first drops the
              address, then the name, then the whole block as the page area narrows, and the
              picture itself scales with the window. */}
          {showBrandLogo && (
            <Box
              component={Link}
              to={paths.home}
              aria-label={branding?.name || t('nav.appName')}
              sx={{
                display: 'flex',
                alignItems: 'center',
                gap: 1.75,
                // A pill: logo, a hairline, the name and the address as one object.
                pl: 1.5,
                pr: showBrandName ? 3 : 1.5,
                py: 0.75,
                borderRadius: 999,
                border: '1px solid',
                borderColor: 'divider',
                bgcolor: 'action.hover',
                textDecoration: 'none',
                color: 'inherit',
                minWidth: 0,
                flexShrink: 1,
                transition: 'background-color 0.18s, border-color 0.18s',
                '&:hover': { bgcolor: 'action.selected', borderColor: 'text.disabled' },
              }}
            >
              {branding?.hasLogo ? (
                <Box
                  component="img"
                  src={logoUrl}
                  alt=""
                  sx={{
                    height: 'clamp(36px, 3.2vw, 46px)',
                    // A logo saved on a white background melts into the tinted pill instead of showing as a white box.
                    mixBlendMode: (theme) => (theme.palette.mode === 'light' ? 'multiply' : 'normal'),
                    width: 'auto',
                    maxWidth: showBrandName ? 220 : 320,
                    objectFit: 'contain',
                    flexShrink: 1,
                    minWidth: 0,
                  }}
                />
              ) : (
                <Avatar
                  variant="rounded"
                  sx={{ width: 48, height: 48, bgcolor: 'primary.main', fontSize: 22, flexShrink: 0 }}
                >
                  {(branding?.name || t('nav.appName')).slice(0, 1)}
                </Avatar>
              )}
              {showBrandName && (
                <>
                  <Divider orientation="vertical" flexItem sx={{ my: 0.75 }} />
                  <Box sx={{ minWidth: 0, textAlign: 'left' }}>
                    <Typography variant="subtitle1" noWrap sx={{ fontWeight: 700, lineHeight: 1.25 }}>
                      {branding?.name || t('nav.appName')}
                    </Typography>
                    {showBrandAddress && companyDetails?.address && (
                      <Typography
                        variant="body2"
                        color="text.secondary"
                        noWrap
                        sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}
                      >
                        <PlaceOutlined sx={{ fontSize: 15, flexShrink: 0 }} />
                        <Box component="span" sx={{ overflow: 'hidden', textOverflow: 'ellipsis' }}>
                          {companyDetails.address}
                        </Box>
                      </Typography>
                    )}
                  </Box>
                </>
              )}
            </Box>
          )}
          <Box sx={{ flex: 1 }} />

          <Tooltip title={t('guide.title')}>
            <IconButton onClick={() => setGuideOpen(true)} aria-label={t('guide.title')}>
              <HelpOutlined />
            </IconButton>
          </Tooltip>
          <BranchSwitcher />
          <LanguageSwitcher />
          {/* In the bar rather than the drawer: an inbox is personal, it is
              the same on every screen, and the count has to be visible from
              wherever the operator happens to be. */}
          <NotificationsMenu />
          <IconButton
            onClick={(event) => setMenuAnchor(event.currentTarget)}
            aria-label={`${t('common.account')}: ${displayName(user)}`}
          >
            <Avatar sx={{ width: 34, height: 34, bgcolor: 'primary.main', fontSize: 14 }}>
              {initialsOf(user.firstName, user.lastName, user.email)}
            </Avatar>
          </IconButton>
          <Menu
            anchorEl={menuAnchor}
            open={!!menuAnchor}
            onClose={() => setMenuAnchor(null)}
            anchorOrigin={
              menuAnchor?.hasAttribute('data-nav-footer')
                ? { vertical: 'top', horizontal: 'right' }
                : { vertical: 'bottom', horizontal: 'right' }
            }
            transformOrigin={
              menuAnchor?.hasAttribute('data-nav-footer')
                ? { vertical: 'bottom', horizontal: 'left' }
                : { vertical: 'top', horizontal: 'right' }
            }
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
              onClick={() => {
                setMenuAnchor(null);
                setShortcutsOpen(true);
              }}
              sx={{ display: { xs: 'none', md: 'flex' } }}
            >
              <ListItemIcon>
                <KeyboardOutlined fontSize="small" />
              </ListItemIcon>
              {t('shortcuts.title')}
            </MenuItem>
            <MenuItem
              onClick={() => {
                setMenuAnchor(null);
                togglePinned();
              }}
              sx={{ display: { xs: 'none', lg: 'flex' } }}
            >
              <ListItemIcon>
                {pinned ? <PushPin fontSize="small" /> : <PushPinOutlined fontSize="small" />}
              </ListItemIcon>
              {pinned ? t('nav.unpinPanel') : t('nav.pinPanel')}
            </MenuItem>
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

      <Box
        component="nav"
        sx={{
          width: { md: navWidth },
          flexShrink: { md: 0 },
          transition: 'width .2s',
          [reduceMotion]: { transition: 'none' },
        }}
      >
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
        <Paper
          component="div"
          square
          elevation={panelDocked ? 0 : 8}
          aria-hidden={!panelOpen}
          onMouseEnter={clearHoverTimer}
          onMouseLeave={() => {
            if (!panelDocked) scheduleFlyoutClose();
          }}
          onKeyDown={(event) => {
            if (event.key === 'Escape' && !panelDocked) setFlyoutKey(null);
          }}
          sx={{
            display: { xs: 'none', md: 'block' },
            position: 'fixed',
            top: 0,
            bottom: 0,
            left: RAIL_WIDTH,
            width: PANEL_WIDTH,
            zIndex: (t2) => t2.zIndex.drawer + 1,
            overflow: 'hidden',
            borderRight: '1px solid',
            borderColor: 'divider',
            transform: panelOpen ? 'none' : 'translateX(-24px)',
            opacity: panelOpen ? 1 : 0,
            visibility: panelOpen ? 'visible' : 'hidden',
            pointerEvents: panelOpen ? 'auto' : 'none',
            transition: panelOpen
              ? 'transform .22s cubic-bezier(.2,.8,.2,1), opacity .18s'
              : 'transform .22s cubic-bezier(.2,.8,.2,1), opacity .18s, visibility 0s .22s',
            [reduceMotion]: { transition: 'none' },
          }}
        >
          <ClickAwayListener
            mouseEvent="onMouseDown"
            onClickAway={(event) => {
              if (panelDocked || !flyoutKey) return;
              // A press on the rail is the rail's own business: it opens, keeps or switches the panel.
              if ((event.target as Element | null)?.closest?.('[data-nav-rail]')) return;
              setFlyoutKey(null);
            }}
          >
            <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
              {shownGroup && (
                <>
                  <Box sx={{ flex: 1, minHeight: 0, overflowY: 'auto', px: 1.25, py: 1.75 }}>
                  <Box sx={{ display: 'flex', alignItems: 'flex-start', px: 1.25, mb: 1 }}>
                    <Box sx={{ flex: 1, minWidth: 0 }}>
                      <Typography
                        variant="overline"
                        component="h2"
                        color="text.secondary"
                        sx={{ display: 'block', fontWeight: 600, lineHeight: 1.6 }}
                      >
                        {shownGroup.label}
                      </Typography>
                      {shownGroup.sub && (
                        <Typography variant="caption" color="text.secondary">
                          {shownGroup.sub}
                        </Typography>
                      )}
                    </Box>
                    {isWide && (
                      <Tooltip title={pinned ? t('nav.unpinPanel') : t('nav.pinPanel')}>
                        <IconButton
                          size="small"
                          onClick={togglePinned}
                          aria-label={pinned ? t('nav.unpinPanel') : t('nav.pinPanel')}
                          aria-pressed={pinned}
                          color={pinned ? 'primary' : 'default'}
                        >
                          {pinned ? <PushPin fontSize="small" /> : <PushPinOutlined fontSize="small" />}
                        </IconButton>
                      </Tooltip>
                    )}
                  </Box>
                  <Box key={`${shownGroup.key}-${openTick}`}>
                    {(() => {
                      let index = 0;
                      let lastSection: string | undefined;
                      return shownGroup.items
                        .filter((item) => !item.inTabs)
                        .map((item) => {
                          const heading = item.section && item.section !== lastSection ? item.section : null;
                          lastSection = item.section ?? lastSection;
                          const count = badgeCounts[item.path] ?? 0;
                          const delay = `${index++ * 28 + 40}ms`;
                          return (
                            <Box key={item.path}>
                              {heading && (
                                <Typography
                                  variant="caption"
                                  color="text.secondary"
                                  sx={{
                                    display: 'block',
                                    px: 1.25,
                                    pt: 1.5,
                                    pb: 0.5,
                                    fontWeight: 600,
                                    letterSpacing: '.06em',
                                    textTransform: 'uppercase',
                                  }}
                                >
                                  {heading}
                                </Typography>
                              )}
                              <Box
                                sx={{
                                  display: 'flex',
                                  alignItems: 'center',
                                  opacity: 0,
                                  animation: `${panelItemIn} .26s cubic-bezier(.2,.8,.2,1) ${delay} forwards`,
                                  [reduceMotion]: { animation: 'none', opacity: 1 },
                                  '&:hover .nav-pin': { opacity: 1 },
                                }}
                              >
                                <ListItemButton
                                  component={Link}
                                  to={navItemHref(item.path)}
                                  selected={isItemSelected(item)}
                                  onClick={() => setFlyoutKey(null)}
                                  sx={{ borderRadius: 1.5, flex: 1, minWidth: 0, gap: 0.5 }}
                                >
                                  <ListItemIcon sx={{ minWidth: 40, color: 'primary.main' }}>
                                    {item.icon}
                                  </ListItemIcon>
                                  <ListItemText
                                    primary={item.label}
                                    slotProps={{ primary: { noWrap: true, sx: { fontWeight: 600 } } }}
                                  />
                                  {count > 0 && (
                                    <Box
                                      component="span"
                                      sx={{
                                        ml: 1,
                                        px: 0.9,
                                        borderRadius: 999,
                                        bgcolor: 'error.main',
                                        color: 'error.contrastText',
                                        fontSize: '0.6875rem',
                                        fontWeight: 600,
                                        lineHeight: '18px',
                                      }}
                                    >
                                      {count > 99 ? '99+' : count}
                                    </Box>
                                  )}
                                </ListItemButton>
                                <IconButton
                                  size="small"
                                  className="nav-pin"
                                  aria-label={t('nav.togglePin')}
                                  sx={{ opacity: favorites.isFavorite(item.path) ? 1 : 0.35, transition: 'opacity 0.15s' }}
                                  onClick={() => favorites.toggleFavorite(item.path)}
                                >
                                  {favorites.isFavorite(item.path) ? (
                                    <StarOutlined fontSize="inherit" color="warning" />
                                  ) : (
                                    <StarBorderOutlined fontSize="inherit" />
                                  )}
                                </IconButton>
                              </Box>
                            </Box>
                          );
                        });
                    })()}
                  </Box>
                  </Box>
                  {/* Who is signed in, and for which company: at the foot of the panel, so the
                      top of it belongs to the group alone. Opens the account menu. */}
                  <ButtonBase
                    data-nav-footer
                    onClick={(event) => setMenuAnchor(event.currentTarget)}
                    aria-label={`${t('common.account')}: ${displayName(user)}`}
                    sx={{
                      display: 'flex',
                      alignItems: 'center',
                      gap: 1.5,
                      width: '100%',
                      px: 2,
                      py: 1.5,
                      textAlign: 'left',
                      borderTop: '1px solid',
                      borderColor: 'divider',
                      transition: 'background-color 0.18s',
                      '&:hover': { bgcolor: 'action.hover' },
                    }}
                  >
                    <Avatar sx={{ width: 36, height: 36, bgcolor: 'primary.main', fontSize: 14, flexShrink: 0 }}>
                      {initialsOf(user.firstName, user.lastName, user.email)}
                    </Avatar>
                    <Box sx={{ minWidth: 0, flex: 1 }}>
                      <Typography variant="subtitle2" noWrap sx={{ fontWeight: 700, lineHeight: 1.3 }}>
                        {displayName(user)}
                      </Typography>
                      <Typography variant="caption" color="text.secondary" noWrap sx={{ display: 'block' }}>
                        {[enumLabel('role', user.role), branding?.name].filter(Boolean).join(' · ')}
                      </Typography>
                    </Box>
                    <ChevronRightOutlined fontSize="small" sx={{ color: 'text.secondary', flexShrink: 0 }} />
                  </ButtonBase>
                </>
              )}
            </Box>
          </ClickAwayListener>
        </Paper>
      </Box>

      <Box
        component="main"
        sx={{
          flexGrow: 1,
          minWidth: 0,
          width: { md: contentWidth },
          transition: 'width .2s',
          [reduceMotion]: { transition: 'none' },
          px: { xs: 2, sm: 3 },
          py: 3,
          // Room for the phone's bottom bar, plus the notch area on an iPhone.
          pb: { xs: `calc(${PHONE_BAR_HEIGHT + 16}px + env(safe-area-inset-bottom))`, sm: 3 },
        }}
      >
        <Toolbar sx={{ minHeight: { md: TOP_BAR_HEIGHT } }} />
        {breadcrumbTrail.length > 0 && (
          // Hidden on a phone: three tiny links are hard to hit, and the back arrow
          // in the bar and the bottom navigation already cover where they lead.
          <Breadcrumbs sx={{ mb: 1.5, fontSize: '0.875rem', display: { xs: 'none', sm: 'block' } }}>
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
        {/*
          The office assistant is built but deliberately not mounted. It is
          finished and tested — src/components/assistant/, src/features/assistant/,
          and the API side under Construction.API/Assistant — and held back to
          ship with a later phase rather than on its own. Re-enable by rendering
          <AssistantLauncher /> here; it hides itself unless the server has an
          API key, so this line alone does not turn anything on.
        */}
      </Box>

      {isPhone && (
        <Paper
          component="nav"
          aria-label={t('nav.bottomBar')}
          elevation={8}
          sx={{
            position: 'fixed',
            left: 0,
            right: 0,
            bottom: 0,
            zIndex: (t2) => t2.zIndex.appBar,
            borderTop: '1px solid #e0e0e0',
            borderRadius: 0,
            pb: 'env(safe-area-inset-bottom)',
          }}
        >
          <BottomNavigation
            showLabels
            value={phoneBarItems.find((item) => isItemSelected(item))?.path ?? false}
            sx={{ height: PHONE_BAR_HEIGHT }}
          >
            {phoneBarItems.map((item) => (
              <BottomNavigationAction
                key={item.path}
                value={item.path}
                label={item.path === paths.timeEntries ? t('nav.barTimeEntries') : item.label}
                component={Link}
                to={navItemHref(item.path)}
                icon={
                  (badgeCounts[item.path] ?? 0) > 0 ? (
                    <Badge badgeContent={badgeCounts[item.path]} color="error" max={99} overlap="circular">
                      {item.icon}
                    </Badge>
                  ) : (
                    item.icon
                  )
                }
                sx={PHONE_BAR_ACTION_SX}
              />
            ))}
            <BottomNavigationAction
              label={t('nav.more')}
              icon={<MoreHorizOutlined />}
              onClick={() => setMobileOpen(true)}
              sx={PHONE_BAR_ACTION_SX}
            />
          </BottomNavigation>
        </Paper>
      )}
    </Box>
  );
}
