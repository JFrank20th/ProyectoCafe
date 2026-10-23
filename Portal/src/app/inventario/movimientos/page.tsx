'use client';

import { useRouter } from 'next/navigation';

import { useEffect } from 'react';

import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';

import { useTranslations } from '@/hooks/useTranslations';
import { useAuth } from '@/lib/auth/AuthProvider';

export default function LegacyInventoryMovementsPage() {
  const tc = useTranslations('common');
  const tp = useTranslations('pages');
  const { has } = useAuth();
  const router = useRouter();

  const targetHref = has('Compras.RecepcionesCafe', 'Ver')
    ? '/compras/recepciones-cafe'
    : has('Ventas.Ventas', 'Ver')
      ? '/ventas'
      : has('Inventario.Procesos', 'Ver')
        ? '/inventario/procesos'
        : null;

  useEffect(() => {
    if (targetHref) {
      router.replace(targetHref);
    }
  }, [router, targetHref]);

  if (!targetHref) {
    return (
      <Stack spacing={1}>
        <Typography variant="body2" color="text.secondary">{tp('noPermission')}</Typography>
      </Stack>
    );
  }

  return (
    <Stack spacing={1}>
      <Typography variant="body2" color="text.secondary">{tc('loading')}</Typography>
    </Stack>
  );
}
