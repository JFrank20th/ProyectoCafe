'use client';

import { useState } from 'react';

import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Divider from '@mui/material/Divider';
import Drawer from '@mui/material/Drawer';
import Fab from '@mui/material/Fab';
import IconButton from '@mui/material/IconButton';
import Stack from '@mui/material/Stack';
import Tooltip from '@mui/material/Tooltip';
import Typography from '@mui/material/Typography';
import { useColorScheme, useTheme } from '@mui/material/styles';

import { IconPlus, IconSettings } from '@tabler/icons-react';

import { BorderRadius } from '@/components/layout/customization/BorderRadius';
import { FontFamily } from '@/components/layout/customization/FontFamily';
import { useUiConfig } from '@/hooks/useUiConfig';
import { useTranslations } from '@/hooks/useTranslations';

export function Customization() {
  const theme = useTheme();
  const t = useTranslations('customization');
  const { resetState } = useUiConfig();
  const { setMode } = useColorScheme();

  const [open, setOpen] = useState(false);

  const handleToggle = () => setOpen((prev) => !prev);

  const handleReset = () => {
    setMode('system');
    resetState();
  };

  const secondaryShadow = theme.customShadows?.secondary ?? theme.shadows[8];

  return (
    <>
      <Tooltip title={t('liveCustomize')}>
        <Fab
          component="div"
          onClick={handleToggle}
          size="medium"
          variant="circular"
          color="secondary"
          sx={{
            borderRadius: 0,
            borderTopLeftRadius: '50%',
            borderBottomLeftRadius: '50%',
            borderTopRightRadius: '50%',
            borderBottomRightRadius: '4px',
            top: '25%',
            position: 'fixed',
            right: 10,
            zIndex: 1200,
            boxShadow: secondaryShadow,
          }}
          aria-label={t('liveCustomize')}
        >
          <IconButton color="inherit" size="large" disableRipple aria-label={t('liveCustomize')}>
            <IconSettings />
          </IconButton>
        </Fab>
      </Tooltip>

      <Drawer anchor="right" onClose={handleToggle} open={open} slotProps={{ paper: { sx: { width: 375 } } }}>
        <Box sx={{ height: '100%', overflow: 'auto' }}>
          <Stack direction="row" sx={{ alignItems: 'center', justifyContent: 'space-between', p: 2.5, gap: 1.5 }}>
            <Typography variant="h5">{t('title')}</Typography>
            <Stack direction="row" sx={{ alignItems: 'center', gap: 1.25 }}>
              <Button variant="outlined" color="error" size="small" onClick={handleReset}>
                {t('reset')}
              </Button>
              <IconButton sx={{ p: 0, color: 'grey.600' }} onClick={handleToggle} aria-label={t('close')}>
                <IconPlus size={24} style={{ transform: 'rotate(45deg)' }} />
              </IconButton>
            </Stack>
          </Stack>
          <Divider />

          <FontFamily />
          <Divider />
          <BorderRadius />
          <Divider />
        </Box>
      </Drawer>
    </>
  );
}
