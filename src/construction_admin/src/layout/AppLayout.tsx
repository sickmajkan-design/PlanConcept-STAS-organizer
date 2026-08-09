import {
  ApartmentOutlined,
  HandymanOutlined,
  Inventory2Outlined,
  LocalShippingOutlined,
  LogoutOutlined,
  ManageAccountsOutlined,
  ChecklistOutlined,
  FolderOutlined,
  MapOutlined,
  ScheduleOutlined,
  CalendarMonthOutlined,
  EventBusyOutlined,
  PaidOutlined,
  LocalGasStationOutlined,
  SwapVertOutlined,
  RequestQuoteOutlined,
  MenuOutlined,
  NotificationsNoneOutlined,
  PasswordOutlined,
  PeopleOutlined,
} from '@mui/icons-material';
import {
  AppBar,
  Avatar,
  Badge,
  Box,
  Divider,
  Drawer,
  IconButton,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Menu,
  MenuItem,
  Toolbar,
  Typography,
  useMediaQuery,
  useTheme,
} from '@mui/material';
import { useState, type ReactNode } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';

import {
  canAdministerAccounts,
  canSeeLabourCost,
  canViewDirectory,
  displayName,
} from '../auth/authHelpers';
import { useAuth } from '../auth/useAuth';
import { LanguageSwitcher } from '../components/LanguageSwitcher';
import { OfflineBanner } from '../components/OfflineBanner';
import { useUnreadCountQuery } from '../features/notifications/useNotifications';
import { useEnumLabel } from '../i18n/enumLabels';
import { useT } from '../i18n/useI18n';
import { paths } from '../routes/paths';
import { initialsOf } from '../utils/formatting';

const DRAWER_WIDTH = 240;

interface NavItem {
  label: string;
  path: string;
  icon: ReactNode;
}

export function AppLayout({ children }: { children: ReactNode }) {
  const theme = useTheme();
  const isDesktop = useMediaQuery(theme.breakpoints.up('md'));
  const [mobileOpen, setMobileOpen] = useState(false);
  const [menuAnchor, setMenuAnchor] = useState<HTMLElement | null>(null);

  const { user, signOut } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const t = useT();
  const enumLabel = useEnumLabel();
  const { data: unreadCount } = useUnreadCountQuery();

  if (!user) {
    return null;
  }

  const navItems: NavItem[] = [
    { label: t('nav.liveMap'), path: paths.map, icon: <MapOutlined /> },
    ...(canViewDirectory(user)
      ? [
          { label: t('nav.employees'), path: paths.employees, icon: <PeopleOutlined /> },
          { label: t('nav.projects'), path: paths.projects, icon: <ApartmentOutlined /> },
          { label: t('nav.vehicles'), path: paths.vehicles, icon: <LocalShippingOutlined /> },
          { label: t('nav.tools'), path: paths.tools, icon: <HandymanOutlined /> },
          { label: t('nav.materials'), path: paths.materials, icon: <Inventory2Outlined /> },
          { label: t('nav.timeEntries'), path: paths.timeEntries, icon: <ScheduleOutlined /> },
          { label: t('nav.workItems'), path: paths.workItems, icon: <ChecklistOutlined /> },
          { label: t('nav.schedule'), path: paths.schedule, icon: <CalendarMonthOutlined /> },
          { label: t('nav.absences'), path: paths.absences, icon: <EventBusyOutlined /> },
          { label: t('nav.costs'), path: paths.costs, icon: <PaidOutlined /> },
          {
            label: t('nav.stockMovements'),
            path: paths.stockMovements,
            icon: <SwapVertOutlined />,
          },
          {
            label: t('nav.vehicleExpenses'),
            path: paths.vehicleExpenses,
            icon: <LocalGasStationOutlined />,
          },
        ]
      : []),
    ...(canSeeLabourCost(user)
      ? [
          { label: t('nav.rates'), path: paths.rates, icon: <RequestQuoteOutlined /> },
        ]
      : []),
    ...(canAdministerAccounts(user)
      ? [
          {
            label: t('nav.documents'),
            path: paths.expiringDocuments,
            icon: <FolderOutlined />,
          },
          { label: t('nav.users'), path: paths.users, icon: <ManageAccountsOutlined /> },
        ]
      : []),
  ];

  const handleSignOut = async () => {
    setMenuAnchor(null);
    await signOut();
    navigate(paths.login, { replace: true });
  };

  const drawerContent = (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
      <Toolbar sx={{ gap: 1 }}>
        <Typography variant="subtitle1" noWrap sx={{ fontWeight: 700 }}>
          {t('nav.appName')}
        </Typography>
      </Toolbar>
      <Divider />
      <List sx={{ flex: 1, px: 1, py: 1 }}>
        {navItems.map((item) => (
          <ListItemButton
            key={item.path}
            component={Link}
            to={item.path}
            selected={location.pathname.startsWith(item.path)}
            onClick={() => setMobileOpen(false)}
            sx={{ borderRadius: 1, mb: 0.5 }}
          >
            <ListItemIcon sx={{ minWidth: 40 }}>{item.icon}</ListItemIcon>
            <ListItemText primary={item.label} />
          </ListItemButton>
        ))}
      </List>
    </Box>
  );

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh' }}>
      <AppBar
        position="fixed"
        color="inherit"
        sx={{
          width: { md: `calc(100% - ${DRAWER_WIDTH}px)` },
          ml: { md: `${DRAWER_WIDTH}px` },
          bgcolor: 'background.paper',
        }}
      >
        <Toolbar sx={{ gap: 1 }}>
          {!isDesktop && (
            <IconButton edge="start" onClick={() => setMobileOpen(true)}>
              <MenuOutlined />
            </IconButton>
          )}
          <Box sx={{ flex: 1 }} />
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

      <Box component="nav" sx={{ width: { md: DRAWER_WIDTH }, flexShrink: { md: 0 } }}>
        <Drawer
          variant="temporary"
          open={mobileOpen}
          onClose={() => setMobileOpen(false)}
          ModalProps={{ keepMounted: true }}
          sx={{
            display: { xs: 'block', md: 'none' },
            '& .MuiDrawer-paper': { width: DRAWER_WIDTH },
          }}
        >
          {drawerContent}
        </Drawer>
        <Drawer
          variant="permanent"
          sx={{
            display: { xs: 'none', md: 'block' },
            '& .MuiDrawer-paper': { width: DRAWER_WIDTH, borderRight: '1px solid #e0e0e0' },
          }}
          open
        >
          {drawerContent}
        </Drawer>
      </Box>

      <Box
        component="main"
        sx={{
          flexGrow: 1,
          width: { md: `calc(100% - ${DRAWER_WIDTH}px)` },
          px: { xs: 2, sm: 3 },
          py: 3,
        }}
      >
        <Toolbar />
        {/* Above the screen rather than inside it: the reason the numbers on
            every page have stopped moving is the same reason, and it should be
            stated once, in the same place, wherever the operator is. */}
        <OfflineBanner />
        {children}
      </Box>
    </Box>
  );
}
