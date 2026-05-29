'use client';

import { LedgerCrud } from '@/components/contabilidad/LedgerCrud';
import { useTranslations } from '@/hooks/useTranslations';

export default function GastosPage() {
  const nav = useTranslations('navigation');
  return (
    <LedgerCrud
      title={nav('gastos')}
      apiBasePath="/api/v1/gastos"
      idKey="gastoId"
      permModule="Contabilidad.Gastos"
    />
  );
}
