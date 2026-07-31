import {
  ApartmentOutlined,
  HandymanOutlined,
  Inventory2Outlined,
  LocalShippingOutlined,
  LogoutOutlined,
  MapOutlined,
  MenuOutlined,
  PasswordOutlined,
  PeopleOutlined,
} from '@mui/icons-material';
import {
  AppBar,
  Avatar,
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

import { canViewDirectory, displayName } from '../auth/authHelpers';
import { useAuth } from '../auth/useAuth';
import { paths } from '../routes/paths';
import { humanizeEnum, initialsOf } from '../utils/formatting';

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

  if (!user) {
    return null;
  }

  const navItems: NavItem[] = [
    { label: 'Live map', path: paths.map, icon: <MapOutlined /> },
    ...(canViewDirectory(user)
      ? [
          { label: 'Employees', path: paths.employees, icon: <PeopleOutlined /> },
          { label: 'Projects', path: paths.projects, icon: <ApartmentOutlined /> },
          { label: 'Vehicles', path: paths.vehicles, icon: <LocalShippingOutlined /> },
          { label: 'Tools', path: paths.tools, icon: <HandymanOutlined /> },
          { label: 'Materials', path: paths.materials, icon: <Inventory2Outlined /> },
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
          Construction Organizer
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
                {humanizeEnum(user.role)}
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
              Change password
            </MenuItem>
            <MenuItem onClick={handleSignOut}>
              <ListItemIcon>
                <LogoutOutlined fontSize="small" />
              </ListItemIcon>
              Sign out
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
        {children}
      </Box>
    </Box>
  );
}
