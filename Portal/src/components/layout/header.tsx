'use client';

import { type ReactNode, useEffect, useMemo, useRef, useState } from 'react';

import Box from '@mui/material/Box';
import Avatar from '@mui/material/Avatar';
import Button from '@mui/material/Button';
import Card from '@mui/material/Card';
import Chip from '@mui/material/Chip';
import ClickAwayListener from '@mui/material/ClickAwayListener';
import Divider from '@mui/material/Divider';
import Grow from '@mui/material/Grow';
import IconButton from '@mui/material/IconButton';
import InputAdornment from '@mui/material/InputAdornment';
import List from '@mui/material/List';
import ListItemButton from '@mui/material/ListItemButton';
import ListItemIcon from '@mui/material/ListItemIcon';
import ListItemText from '@mui/material/ListItemText';
import OutlinedInput from '@mui/material/OutlinedInput';
import Paper from '@mui/material/Paper';
import Popper from '@mui/material/Popper';
import Stack from '@mui/material/Stack';
import Tooltip from '@mui/material/Tooltip';
import Typography from '@mui/material/Typography';
import useMediaQuery from '@mui/material/useMediaQuery';
import { useColorScheme, useTheme, type SxProps, type Theme } from '@mui/material/styles';

import {
  IconAdjustmentsHorizontal,
  IconBell,
  IconLogout,
  IconMenu2,
  IconMoon,
  IconSettings,
  IconSun,
  IconUser,
  IconX,
  IconSearch,
} from '@tabler/icons-react';

import { LanguageSelector } from '@/components/layout/language-selector';
import { useUiConfig } from '@/hooks/useUiConfig';
import { useTranslations } from '@/hooks/useTranslations';
import { getRuntimeConfig } from '@/lib/runtime-config';

export type HeaderProps = {
  user?: {
    name: string;
    email: string;
  };
  onLogout?: () => void;
  drawerOpen: boolean;
  onToggleDrawer: () => void;
};

export function Header({ user, onLogout, onToggleDrawer }: HeaderProps) {
  const t = useTranslations('common');
  const theme = useTheme();
  const downMD = useMediaQuery(theme.breakpoints.down('md'));
  const { APP_NAME } = getRuntimeConfig();

  const {
    state: { borderRadius },
  } = useUiConfig();

  const { mode, systemMode, setMode } = useColorScheme();
  const resolvedMode = mode === 'system' ? systemMode : mode;

  const initials = useMemo(() => {
    const source = (user?.name || APP_NAME || 'ProyectoCafe').trim();
    return source
      .split(/\s+/)
      .slice(0, 2)
      .map((x) => x[0]?.toUpperCase() ?? '')
      .join('')
      .slice(0, 2);
  }, [user?.name, APP_NAME]);

  useEffect(() => {
    // Mantener compatibilidad con los componentes Tailwind existentes (darkMode: 'class')
    if (!resolvedMode) return;
    document.documentElement.classList.toggle('dark', resolvedMode === 'dark');
  }, [resolvedMode]);

  function toggleTheme() {
    const next = resolvedMode === 'dark' ? 'light' : 'dark';
    setMode(next);
    document.documentElement.classList.toggle('dark', next === 'dark');
  }

  const primary = theme.vars?.palette.primary ?? theme.palette.primary;

  return (
    <Box sx={{ display: 'flex', width: '100%', alignItems: 'center', gap: 2 }}>
      {/* logo & toggler */}
      <Box sx={{ width: downMD ? 'auto' : 228, display: 'flex' }}>
        <Box component="span" sx={{ display: { xs: 'none', md: 'block' }, flexGrow: 1 }}>
          <Typography variant="h5" sx={{ fontWeight: 700 }}>
            {APP_NAME}
          </Typography>
        </Box>
        <Avatar
          variant="rounded"
          sx={{
            ...theme.typography.commonAvatar,
            ...theme.typography.mediumAvatar,
            overflow: 'hidden',
            transition: 'all .2s ease-in-out',
            color: primary.dark,
            backgroundColor: primary.light,
            '&:hover': {
              color: primary.light,
              backgroundColor: primary.dark,
            },
          }}
          onClick={onToggleDrawer}
          aria-label={t('toggleSidebar')}
        >
          <IconMenu2 stroke={1.5} size={20} />
        </Avatar>
      </Box>

      <SearchSection />

      <Box sx={{ flexGrow: 1 }} />
      <Box sx={{ flexGrow: 1 }} />

      <NotificationSection />

      <Tooltip title={t('toggleTheme')}>
        <IconButton onClick={toggleTheme} size="small" aria-label={t('toggleTheme')}>
          {resolvedMode === 'dark' ? <IconSun stroke={1.5} size={20} /> : <IconMoon stroke={1.5} size={20} />}
        </IconButton>
      </Tooltip>

      <LanguageSelector />

      {user ? (
        <ProfileSection
          borderRadius={borderRadius}
          initials={initials}
          name={user.name}
          email={user.email}
          onLogout={onLogout}
        />
      ) : null}
    </Box>
  );
}

function HeaderAvatar({
  children,
  onClick,
  sx,
  ariaLabel,
}: {
  children: ReactNode;
  onClick?: () => void;
  sx?: SxProps<Theme>;
  ariaLabel?: string;
}) {
  const theme = useTheme();
  const primary = theme.vars?.palette.primary ?? theme.palette.primary;

  return (
    <Avatar
      variant="rounded"
      sx={{
        ...theme.typography.commonAvatar,
        ...theme.typography.mediumAvatar,
        color: primary.dark,
        backgroundColor: primary.light,
        '&:hover': {
          color: primary.light,
          backgroundColor: primary.dark,
        },
        ...sx,
      }}
      onClick={onClick}
      aria-label={ariaLabel}
    >
      {children}
    </Avatar>
  );
}

function SearchSection() {
  const theme = useTheme();
  const t = useTranslations('common');
  const [value, setValue] = useState('');

  const anchorRef = useRef<HTMLDivElement | null>(null);
  const [open, setOpen] = useState(false);

  const orange = theme.vars?.palette.orange ?? theme.palette.orange;

  const handleToggle = () => setOpen((prev) => !prev);
  const handleClose = (event: MouseEvent | TouchEvent) => {
    if (anchorRef.current && anchorRef.current.contains(event.target as Node)) return;
    setOpen(false);
  };

  return (
    <>
      <Box sx={{ display: { xs: 'block', md: 'none' } }}>
        <Box ref={anchorRef} sx={{ ml: 2 }}>
          <HeaderAvatar ariaLabel={t('search')} onClick={handleToggle}>
            <IconSearch stroke={1.5} size={19} />
          </HeaderAvatar>
        </Box>

        <Popper
          placement="bottom"
          open={open}
          anchorEl={anchorRef.current}
          transition
          sx={{ zIndex: 1100, width: '99%', px: { xs: 1.25, sm: 1.5 } }}
          modifiers={[{ name: 'offset', options: { offset: [0, 12] } }]}
        >
          {({ TransitionProps }) => (
            <ClickAwayListener onClickAway={handleClose}>
              <Grow {...TransitionProps}>
                <Card sx={{ bgcolor: 'background.paper', border: 0, boxShadow: 'none' }}>
                  <Box sx={{ p: 2 }}>
                    <OutlinedInput
                      id="input-search-header-mobile"
                      value={value}
                      onChange={(e) => setValue(e.target.value)}
                      placeholder={t('search')}
                      startAdornment={
                        <InputAdornment position="start">
                          <IconSearch stroke={1.5} size={16} />
                        </InputAdornment>
                      }
                      endAdornment={
                        <InputAdornment position="end">
                          <HeaderAvatar sx={{ pointerEvents: 'none' }}>
                            <IconAdjustmentsHorizontal stroke={1.5} size={20} />
                          </HeaderAvatar>
                          <Box sx={{ ml: 2 }}>
                            <Avatar
                              variant="rounded"
                              sx={{
                                ...theme.typography.commonAvatar,
                                ...theme.typography.mediumAvatar,
                                bgcolor: orange?.light ?? 'orange.light',
                                color: orange?.dark ?? 'orange.dark',
                                '&:hover': {
                                  bgcolor: orange?.dark ?? 'orange.dark',
                                  color: orange?.light ?? 'orange.light',
                                },
                              }}
                              onClick={() => setOpen(false)}
                              aria-label={t('close')}
                            >
                              <IconX stroke={1.5} size={20} />
                            </Avatar>
                          </Box>
                        </InputAdornment>
                      }
                      sx={{ width: '100%', ml: 0.5, px: 2, bgcolor: 'background.paper' }}
                      slotProps={{ input: { 'aria-label': t('search'), sx: { bgcolor: 'transparent', pl: 0.5 } } }}
                    />
                  </Box>
                </Card>
              </Grow>
            </ClickAwayListener>
          )}
        </Popper>
      </Box>

      <Box sx={{ display: { xs: 'none', md: 'block' } }}>
        <OutlinedInput
          id="input-search-header"
          value={value}
          onChange={(e) => setValue(e.target.value)}
          placeholder={t('search')}
          startAdornment={
            <InputAdornment position="start">
              <IconSearch stroke={1.5} size={16} />
            </InputAdornment>
          }
          endAdornment={
            <InputAdornment position="end">
              <HeaderAvatar sx={{ pointerEvents: 'none' }} ariaLabel={t('search')}>
                <IconAdjustmentsHorizontal stroke={1.5} size={20} />
              </HeaderAvatar>
            </InputAdornment>
          }
          aria-describedby="search-helper-text"
          slotProps={{ input: { 'aria-label': t('search'), sx: { bgcolor: 'transparent', pl: 0.5 } } }}
          sx={{ width: { md: 250, lg: 434 }, ml: 2, px: 2 }}
        />
      </Box>
    </>
  );
}

function NotificationSection() {
  const theme = useTheme();
  const t = useTranslations('common');
  const downMD = useMediaQuery(theme.breakpoints.down('md'));

  const anchorRef = useRef<HTMLDivElement | null>(null);
  const [open, setOpen] = useState(false);

  const warning = theme.vars?.palette.warning ?? theme.palette.warning;

  const handleToggle = () => setOpen((prev) => !prev);
  const handleClose = (event: MouseEvent | TouchEvent) => {
    if (anchorRef.current && anchorRef.current.contains(event.target as Node)) return;
    setOpen(false);
  };

  return (
    <>
      <Box ref={anchorRef} sx={{ ml: 2 }}>
        <Avatar
          variant="rounded"
          sx={{
            ...theme.typography.commonAvatar,
            ...theme.typography.mediumAvatar,
            transition: 'all .2s ease-in-out',
            color: warning.dark,
            backgroundColor: warning.light,
            '&:hover, &[aria-controls="notification-menu"]': {
              color: warning.light,
              backgroundColor: warning.dark,
            },
          }}
          aria-controls={open ? 'notification-menu' : undefined}
          aria-haspopup="true"
          onClick={handleToggle}
          aria-label={t('notifications')}
        >
          <IconBell stroke={1.5} size={20} />
        </Avatar>
      </Box>

      <Popper
        placement={downMD ? 'bottom' : 'bottom-end'}
        open={open}
        anchorEl={anchorRef.current}
        transition
        disablePortal
        modifiers={[{ name: 'offset', options: { offset: [downMD ? 5 : 0, 20] } }]}
      >
        {({ TransitionProps }) => (
          <ClickAwayListener onClickAway={handleClose}>
            <Grow {...TransitionProps}>
              <Paper>
                <Card
                  variant="outlined"
                  sx={{
                    maxWidth: 330,
                    border: 0,
                    boxShadow: theme.shadows[16],
                  }}
                >
                  <Stack sx={{ gap: 1.5, p: 2 }}>
                    <Stack direction="row" sx={{ alignItems: 'center', justifyContent: 'space-between' }}>
                      <Stack direction="row" sx={{ gap: 1.25, alignItems: 'center' }}>
                        <Typography variant="subtitle1">{t('notifications')}</Typography>
                        <Chip size="small" label="0" variant="filled" sx={{ color: 'background.default', bgcolor: 'warning.dark' }} />
                      </Stack>
                      <Button size="small" disableElevation disabled>
                        {t('viewAll')}
                      </Button>
                    </Stack>
                    <Divider />
                    <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                      {t('noNotifications')}
                    </Typography>
                  </Stack>
                </Card>
              </Paper>
            </Grow>
          </ClickAwayListener>
        )}
      </Popper>
    </>
  );
}

function ProfileSection({
  initials,
  name,
  email,
  borderRadius,
  onLogout,
}: {
  initials: string;
  name: string;
  email: string;
  borderRadius: number;
  onLogout?: () => void;
}) {
  const theme = useTheme();
  const t = useTranslations('common');

  const anchorRef = useRef<HTMLDivElement | null>(null);
  const [open, setOpen] = useState(false);

  const handleToggle = () => setOpen((prev) => !prev);
  const handleClose = (event: MouseEvent | TouchEvent) => {
    if (anchorRef.current && anchorRef.current.contains(event.target as Node)) return;
    setOpen(false);
  };

  return (
    <>
      <Box ref={anchorRef}>
        <Chip
          slotProps={{ label: { sx: { lineHeight: 0 } } }}
          sx={{ ml: 2, height: '48px', alignItems: 'center', borderRadius: '27px' }}
          icon={
            <Avatar
              variant="rounded"
              sx={{ typography: 'mediumAvatar', margin: '8px 0 8px 8px !important', cursor: 'pointer' }}
              aria-label={t('userMenu')}
            >
              {initials || <IconUser stroke={1.5} size={18} />}
            </Avatar>
          }
          label={<IconSettings stroke={1.5} size={24} />}
          aria-controls={open ? 'profile-menu' : undefined}
          aria-haspopup="true"
          onClick={handleToggle}
          color="primary"
        />
      </Box>

      <Popper
        placement="bottom-end"
        open={open}
        anchorEl={anchorRef.current}
        transition
        disablePortal
        modifiers={[{ name: 'offset', options: { offset: [0, 14] } }]}
      >
        {({ TransitionProps }) => (
          <ClickAwayListener onClickAway={handleClose}>
            <Grow {...TransitionProps}>
              <Paper>
                <Card variant="outlined" sx={{ border: 0, boxShadow: theme.shadows[16] }}>
                  <Box sx={{ px: 2, pt: 2, pb: 1.5 }}>
                    <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>
                      {name}
                    </Typography>
                    <Typography variant="caption" sx={{ color: 'text.secondary' }}>
                      {email}
                    </Typography>
                  </Box>
                  <Divider />
                  <Box sx={{ p: 2, pt: 1.5 }}>
                    <List
                      component="nav"
                      sx={{
                        width: '100%',
                        maxWidth: 350,
                        minWidth: 280,
                        borderRadius: `${borderRadius}px`,
                        '& .MuiListItemButton-root': { mt: 0.5 },
                      }}
                    >
                      <ListItemButton
                        sx={{ borderRadius: `${borderRadius}px` }}
                        onClick={() => {
                          setOpen(false);
                          onLogout?.();
                        }}
                      >
                        <ListItemIcon>
                          <IconLogout stroke={1.5} size={20} />
                        </ListItemIcon>
                        <ListItemText primary={<Typography variant="body2">{t('logout')}</Typography>} />
                      </ListItemButton>
                    </List>
                  </Box>
                </Card>
              </Paper>
            </Grow>
          </ClickAwayListener>
        )}
      </Popper>
    </>
  );
}
