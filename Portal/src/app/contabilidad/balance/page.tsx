'use client';

import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';

import { useTranslations } from '@/hooks/useTranslations';
import { BalanceSummary } from '@/components/contabilidad/BalanceSummary';

export default function BalancePage() {
  const nav = useTranslations('navigation');

  return (
    <Stack spacing={3}>
      <Typography variant="h5" fontWeight="bold">
        {nav('balance')}
      </Typography>
      <BalanceSummary />
    </Stack>
  );
}
