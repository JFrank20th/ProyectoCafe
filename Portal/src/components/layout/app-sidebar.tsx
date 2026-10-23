'use client';

import { useEffect, useMemo, useState, type ComponentType, type ElementType } from 'react';

import Link from 'next/link';
import { usePathname } from 'next/navigation';

import type { NavItem } from '@/lib/navigation';
import { NAV, filterVisibleNavItems } from '@/lib/navigation';
import { useAuth } from '@/lib/auth/AuthProvider';
import { getRuntimeConfig } from '@/lib/runtime-config';
import { useTranslations } from '@/hooks/useTranslations';
import { useUiConfig } from '@/hooks/useUiConfig';
import { berryDrawerWidth } from '@/theme/berry/constants';

import Box from '@mui/material/Box';
import Drawer from '@mui/material/Drawer';
import Chip from '@mui/material/Chip';
import ClickAwayListener from '@mui/material/ClickAwayListener';
import Collapse from '@mui/material/Collapse';
import Divider from '@mui/material/Divider';
import List from '@mui/material/List';
import ListItemButton from '@mui/material/ListItemButton';
import ListItemIcon from '@mui/material/ListItemIcon';
import ListItemText from '@mui/material/ListItemText';
import ListSubheader from '@mui/material/ListSubheader';
import Paper from '@mui/material/Paper';
import Popper from '@mui/material/Popper';
import Stack from '@mui/material/Stack';
import Toolbar from '@mui/material/Toolbar';
import Tooltip from '@mui/material/Tooltip';
import useMediaQuery from '@mui/material/useMediaQuery';
import Avatar from '@mui/material/Avatar';
import { styled, useTheme, type CSSObject } from '@mui/material/styles';

import FiberManualRecordIcon from '@mui/icons-material/FiberManualRecord';

import {
  IconChevronDown,
  IconChevronUp,
  IconDashboard,
  IconShield,
  IconUsers,
} from '@tabler/icons-react';

export type AppSidebarProps = {
  user?: {
    name: string;
    email: string;
  };
  open: boolean;
  onClose: () => void;
};

const MiniDrawerStyled = styled(Drawer, { shouldForwardProp: (prop) => prop !== 'open' })<{ open: boolean }>(
  ({ theme, open }) => {
    const openedMixin: CSSObject = {
      width: berryDrawerWidth,
      borderRight: 'none',
      zIndex: 1099,
      backgroundColor: theme.palette.background.paper,
      overflowX: 'hidden',
      boxShadow: 'none',
      transition: theme.transitions.create('width', {
        easing: theme.transitions.easing.sharp,
        duration: theme.transitions.duration.enteringScreen + 200,
      }),
    };

    const closedMixin: CSSObject = {
      borderRight: 'none',
      zIndex: 1099,
      backgroundColor: theme.palette.background.paper,
      overflowX: 'hidden',
      width: 72,
      transition: theme.transitions.create('width', {
        easing: theme.transitions.easing.sharp,
        duration: theme.transitions.duration.leavingScreen + 200,
      }),
    };

    return {
      width: berryDrawerWidth,
      borderRight: '0px',
      flexShrink: 0,
      whiteSpace: 'nowrap',
      boxSizing: 'border-box',
      overflowX: 'hidden',
      ...(open && {
        ...openedMixin,
        '& .MuiDrawer-paper': openedMixin,
      }),
      ...(!open && {
        ...closedMixin,
        '& .MuiDrawer-paper': closedMixin,
      }),
    };
  }
);

const navIconByKey: Record<string, ComponentType<{ size?: number; stroke?: number }>> = {
  dashboard: IconDashboard,
  usuarios: IconUsers,
  roles: IconShield,
  seguridad: IconShield,
};

export function AppSidebar({ user, open, onClose }: AppSidebarProps) {
  const pathname = usePathname();
  const theme = useTheme();
  const downMD = useMediaQuery(theme.breakpoints.down('md'));

  const {
    state: { borderRadius },
  } = useUiConfig();

  const { roles, permissions } = useAuth();
  const t = useTranslations('navigation');
  const { APP_NAME, APP_VERSION } = getRuntimeConfig();

  const initials = (APP_NAME || 'ProyectoCafe')
    .trim()
    .split(/\s+/)
    .slice(0, 2)
    .map((x) => x[0]?.toUpperCase() ?? '')
    .join('')
    .slice(0, 2);

  const logo = (
    <Box sx={{ display: 'flex', p: 2, alignItems: 'center', gap: 1.5 }}>
      <Avatar
        variant="rounded"
        sx={{
          ...theme.typography.commonAvatar,
          ...theme.typography.mediumAvatar,
          overflow: 'hidden',
          color: theme.palette.primary.dark,
          backgroundColor: theme.palette.primary.light,
        }}
      >
        {initials}
      </Avatar>
      {open ? (
        <Box sx={{ minWidth: 0 }}>
          <Box component="span" sx={{ display: 'block', fontSize: '0.875rem', fontWeight: 600, whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
            {APP_NAME}
          </Box>
          {user ? (
            <Box component="span" sx={{ display: 'block', fontSize: '0.75rem', color: 'text.secondary', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
              {user.email}
            </Box>
          ) : null}
        </Box>
      ) : null}
    </Box>
  );

  const visibleSections = useMemo(
    () =>
      NAV.map((section) => ({
        section,
        items: filterVisibleNavItems(roles, permissions, section.items),
      })).filter((x) => x.items.length > 0),
    [permissions, roles]
  );

  const [openByKey, setOpenByKey] = useState<Record<string, boolean>>({});
  const [miniMenu, setMiniMenu] = useState<null | { key: string; anchorEl: HTMLElement; items: NavItem[] }>(null);

  useEffect(() => {
    if (open || downMD) {
      setMiniMenu(null);
    }
  }, [open, downMD]);

  useEffect(() => {
    const keysToOpen = new Set<string>();

    const walk = (items: NavItem[], prefix: string): boolean => {
      let hasActive = false;
      for (const item of items) {
        const key = `${prefix}/${item.labelKey}`;
        if (item.items && item.items.length > 0) {
          const childActive = walk(item.items, key);
          if (childActive) keysToOpen.add(key);
          hasActive = hasActive || childActive;
        } else if (item.href && pathname === item.href) {
          hasActive = true;
        }
      }
      return hasActive;
    };

    for (const { section, items } of visibleSections) {
      walk(items, section.labelKey);
    }

    if (keysToOpen.size === 0) return;
    setOpenByKey((prev) => {
      const next = { ...prev };
      for (const k of keysToOpen) next[k] = true;
      return next;
    });
  }, [pathname, visibleSections]);

  const isItemActive = (item: NavItem): boolean => {
    if (item.href && pathname === item.href) return true;
    if (item.items && item.items.length > 0) return item.items.some(isItemActive);
    return false;
  };

  const renderOpenItem = (item: NavItem, level: number, prefix: string) => {
    const key = `${prefix}/${item.labelKey}`;
    const label = t(item.labelKey);

    const selected = isItemActive(item);

    if (item.items && item.items.length > 0) {
      const expanded = Boolean(openByKey[key]);
      const Icon = navIconByKey[item.labelKey] ?? IconDashboard;

      return (
        <Box key={key}>
          <ListItemButton
            selected={selected}
            onClick={() => setOpenByKey((prev) => ({ ...prev, [key]: !prev[key] }))}
            sx={{
              borderRadius: `${borderRadius}px`,
              mb: 0.5,
              ...(level !== 1 && { ml: `${level * 18}px` }),
            }}
          >
            <ListItemIcon sx={{ minWidth: level === 1 ? 36 : 18 }}>
              {level === 1 ? <Icon size={18} stroke={1.5} /> : <FiberManualRecordIcon sx={{ width: selected ? 8 : 6, height: selected ? 8 : 6 }} />}
            </ListItemIcon>
            <ListItemText primary={label} />
            {expanded ? <IconChevronUp stroke={1.5} size={16} /> : <IconChevronDown stroke={1.5} size={16} />}
          </ListItemButton>
          <Collapse in={expanded} timeout="auto" unmountOnExit>
            <List dense disablePadding>
              {item.items.map((child) => renderOpenItem(child, level + 1, key))}
            </List>
          </Collapse>
        </Box>
      );
    }

    if (!item.href) return null;

    const Icon = navIconByKey[item.labelKey] ?? IconDashboard;

    return (
      <ListItemButton
        key={key}
        component={Link as unknown as ElementType}
        href={item.href}
        selected={selected}
        onClick={() => {
          setMiniMenu(null);
          if (downMD) onClose();
        }}
        sx={{
          borderRadius: `${borderRadius}px`,
          mb: 0.5,
          ...(level !== 1 && { ml: `${level * 18}px` }),
        }}
      >
        <ListItemIcon sx={{ minWidth: level === 1 ? 36 : 18 }}>
          {level === 1 ? <Icon size={18} stroke={1.5} /> : <FiberManualRecordIcon sx={{ width: selected ? 8 : 6, height: selected ? 8 : 6 }} />}
        </ListItemIcon>
        <ListItemText primary={label} />
      </ListItemButton>
    );
  };

  const renderMiniItem = (item: NavItem, prefix: string) => {
    const key = `${prefix}/${item.labelKey}`;
    const label = t(item.labelKey);
    const selected = isItemActive(item);
    const Icon = navIconByKey[item.labelKey] ?? IconDashboard;

    if (item.items && item.items.length > 0) {
      const button = (
        <ListItemButton
          key={key}
          selected={selected}
          onClick={(e) => {
            if (miniMenu?.key === key) {
              setMiniMenu(null);
              return;
            }
            setMiniMenu({ key, anchorEl: e.currentTarget, items: item.items ?? [] });
          }}
          sx={{ borderRadius: `${borderRadius}px`, justifyContent: 'center', px: 1.5 }}
        >
          <ListItemIcon sx={{ minWidth: 'auto' }}>
            <Icon size={18} stroke={1.5} />
          </ListItemIcon>
        </ListItemButton>
      );

      return (
        <Tooltip key={key} title={label} placement="right">
          {button}
        </Tooltip>
      );
    }

    if (!item.href) return null;

    const button = (
      <ListItemButton
        key={key}
        component={Link as unknown as ElementType}
        href={item.href}
        selected={selected}
        onClick={() => {
          setMiniMenu(null);
          if (downMD) onClose();
        }}
        sx={{ borderRadius: `${borderRadius}px`, justifyContent: 'center', px: 1.5 }}
      >
        <ListItemIcon sx={{ minWidth: 'auto' }}>
          <Icon size={18} stroke={1.5} />
        </ListItemIcon>
      </ListItemButton>
    );

    return (
      <Tooltip key={key} title={label} placement="right">
        {button}
      </Tooltip>
    );
  };

  const navList = (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
      {!downMD ? <Toolbar /> : null}
      {logo}

      <Box sx={{ flex: 1, overflow: 'auto', px: open ? 2 : 0, pb: 2, mt: open ? 0 : 2.5 }}>
        {visibleSections.map(({ section, items }, sectionIndex) => {
          return (
            <Box key={section.labelKey}>
              {open ? (
                <List
                  dense
                  subheader={
                    <ListSubheader
                      disableSticky
                      sx={{
                        bgcolor: 'transparent',
                        px: 0,
                        lineHeight: '32px',
                        fontSize: '0.75rem',
                        fontWeight: 700,
                        color: 'text.secondary',
                      }}
                    >
                      {t(section.labelKey)}
                    </ListSubheader>
                  }
                >
                  {items.map((item) => renderOpenItem(item, 1, section.labelKey))}
                </List>
              ) : (
                <Box sx={{ pb: 1 }}>
                  {sectionIndex > 0 ? <Divider sx={{ my: 1 }} /> : null}
                  <List dense sx={{ px: 1 }}>
                    {items.map((item) => renderMiniItem(item, section.labelKey))}
                  </List>
                </Box>
              )}
            </Box>
          );
        })}
      </Box>

      {open ? (
        <Stack direction="row" sx={{ justifyContent: 'center', pb: 2 }}>
          <Chip label={APP_VERSION} size="small" color="default" />
        </Stack>
      ) : null}
    </Box>
  );

  const miniPopper = !open && miniMenu ? (
    <Popper
      open
      anchorEl={miniMenu.anchorEl}
      placement="right-start"
      modifiers={[{ name: 'offset', options: { offset: [-12, 0] } }]}
      sx={{ zIndex: 2001, minWidth: 180 }}
    >
      <Paper sx={{ overflow: 'hidden', boxShadow: theme.shadows[8], backgroundImage: 'none' }}>
        <ClickAwayListener onClickAway={() => setMiniMenu(null)}>
          <Box sx={{ p: 1.25 }}>
            <List dense disablePadding>
              {miniMenu.items
                .map((child) => {
                  const childKey = `${miniMenu.key}/${child.labelKey}`;
                  const childLabel = t(child.labelKey);
                  const childSelected = isItemActive(child);
                  const Icon = navIconByKey[child.labelKey] ?? IconDashboard;

                  if (child.items && child.items.length > 0) {
                    return (
                      <Box key={childKey} sx={{ px: 1, py: 0.75 }}>
                        <Box component="span" sx={{ display: 'block', fontSize: '0.75rem', fontWeight: 700, color: 'text.secondary' }}>
                          {childLabel}
                        </Box>
                        <List dense disablePadding sx={{ mt: 0.5 }}>
                          {child.items
                            .filter((x) => Boolean(x.href))
                            .map((grand) => {
                              const grandKey = `${childKey}/${grand.labelKey}`;
                              const grandLabel = t(grand.labelKey);
                              const grandSelected = isItemActive(grand);

                              return (
                                <ListItemButton
                                  key={grandKey}
                                  component={Link as unknown as ElementType}
                                  href={grand.href as string}
                                  selected={grandSelected}
                                  onClick={() => {
                                    setMiniMenu(null);
                                    if (downMD) onClose();
                                  }}
                                  sx={{ borderRadius: `${borderRadius}px` }}
                                >
                                  <ListItemIcon sx={{ minWidth: 28 }}>
                                    <FiberManualRecordIcon sx={{ width: grandSelected ? 8 : 6, height: grandSelected ? 8 : 6 }} />
                                  </ListItemIcon>
                                  <ListItemText primary={grandLabel} />
                                </ListItemButton>
                              );
                            })}
                        </List>
                      </Box>
                    );
                  }

                  if (!child.href) return null;

                  return (
                    <ListItemButton
                      key={childKey}
                      component={Link as unknown as ElementType}
                      href={child.href}
                      selected={childSelected}
                      onClick={() => {
                        setMiniMenu(null);
                        if (downMD) onClose();
                      }}
                      sx={{ borderRadius: `${borderRadius}px` }}
                    >
                      <ListItemIcon sx={{ minWidth: 36 }}>
                        <Icon size={18} stroke={1.5} />
                      </ListItemIcon>
                      <ListItemText primary={childLabel} />
                    </ListItemButton>
                  );
                })
                .filter(Boolean)}
            </List>
          </Box>
        </ClickAwayListener>
      </Paper>
    </Popper>
  ) : null;

  return (
    <>
      <Box
        component="nav"
        aria-label="sidebar"
        sx={{
          flexShrink: { md: 0 },
          width: { xs: 'auto', md: berryDrawerWidth },
        }}
      >
        {downMD ? (
          <Drawer
            variant="temporary"
            anchor="left"
            open={open}
            onClose={onClose}
            slotProps={{
              paper: {
                sx: {
                  mt: 0,
                  zIndex: 1099,
                  width: berryDrawerWidth,
                  bgcolor: 'background.paper',
                  color: 'text.primary',
                  borderRight: 'none',
                },
              },
            }}
            ModalProps={{ keepMounted: true }}
          >
            {navList}
          </Drawer>
        ) : (
          <MiniDrawerStyled variant="permanent" open={open}>
            {navList}
          </MiniDrawerStyled>
        )}
      </Box>
      {miniPopper}
    </>
  );
}
