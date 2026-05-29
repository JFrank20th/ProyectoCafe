'use client';

import Box from '@mui/material/Box';
import Divider from '@mui/material/Divider';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';

import { useTranslations } from '@/hooks/useTranslations';
import { getRuntimeConfig } from '@/lib/runtime-config';

export function Footer() {
  const t = useTranslations('footer');
  const { APP_VERSION } = getRuntimeConfig();
  const year = new Date().getFullYear();

  return (
    <Box component="footer" sx={{ mt: 'auto', pt: 2 }}>
      <Divider sx={{ mb: 2 }} />
      <Stack
        direction={{ xs: 'column', md: 'row' }}
        spacing={1.5}
        alignItems="center"
        justifyContent="space-between"
      >
        <Typography variant="caption" sx={{ color: 'text.secondary' }}>
          {t('copyright', { year })}
        </Typography>
        <Typography variant="caption" sx={{ color: 'text.secondary' }}>
          {t('version', { version: APP_VERSION })}
        </Typography>
      </Stack>
    </Box>
  );
}
