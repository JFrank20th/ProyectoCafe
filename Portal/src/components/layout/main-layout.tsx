'use client';

import type { ReactNode } from 'react';
import { useEffect, useState } from 'react';

import AppBar from '@mui/material/AppBar';
import Box from '@mui/material/Box';
import Toolbar from '@mui/material/Toolbar';
import useMediaQuery from '@mui/material/useMediaQuery';
import { useTheme } from '@mui/material/styles';

import { AppSidebar } from '@/components/layout/app-sidebar';
import { AppBreadcrumbs } from '@/components/layout/breadcrumbs';
import { Customization } from '@/components/layout/customization/Customization';
import { Header } from '@/components/layout/header';
import { Footer } from '@/components/layout/footer';
import MainContentStyled from '@/components/layout/main-content-styled';
import { useUiConfig } from '@/hooks/useUiConfig';

export type MainLayoutProps = {
  children: ReactNode;
  user?: {
    name: string;
    email: string;
  };
  onLogout?: () => void;
};

export function MainLayout({ children, user, onLogout }: MainLayoutProps) {
  const theme = useTheme();
  const downMD = useMediaQuery(theme.breakpoints.down('md'));

  const {
    state: { borderRadius },
  } = useUiConfig();

  const [drawerOpen, setDrawerOpen] = useState(true);

  useEffect(() => {
    if (downMD) {
      setDrawerOpen(false);
    } else {
      setDrawerOpen(true);
    }
  }, [downMD]);

  function toggleDrawer() {
    setDrawerOpen((prev) => !prev);
  }

  return (
    <Box sx={{ display: 'flex' }}>
      <AppBar
        enableColorOnDark
        position="fixed"
        color="inherit"
        elevation={0}
        sx={{ bgcolor: 'background.paper', zIndex: (t) => t.zIndex.drawer + 1 }}
      >
        <Toolbar sx={{ p: 2 }}>
          <Header user={user} onLogout={onLogout} drawerOpen={drawerOpen} onToggleDrawer={toggleDrawer} />
        </Toolbar>
      </AppBar>

      <AppSidebar user={user} open={drawerOpen} onClose={() => setDrawerOpen(false)} />

      <MainContentStyled open={drawerOpen} borderRadius={borderRadius}>
        <Box
          sx={{
            px: { xs: 0 },
            minHeight: 'calc(100vh - 128px)',
            display: 'flex',
            flexDirection: 'column',
          }}
        >
          <AppBreadcrumbs />
          <Box sx={{ flex: 1, width: '100%' }}>{children}</Box>
          <Footer />
        </Box>
      </MainContentStyled>

      <Customization />
    </Box>
  );
}
