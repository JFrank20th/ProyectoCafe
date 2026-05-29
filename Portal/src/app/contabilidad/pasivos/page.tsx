'use client';

import { LedgerCrud } from '@/components/contabilidad/LedgerCrud';
import { useTranslations } from '@/hooks/useTranslations';

export default function PasivosPage() {
  const nav = useTranslations('navigation');
  return (
    <LedgerCrud
      title={nav('pasivos')}
      apiBasePath="/api/v1/pasivos"
      idKey="pasivoId"
      permModule="Contabilidad.Pasivos"
    />
  );
}
