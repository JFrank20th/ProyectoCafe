'use client';

import { useMemo, useState } from 'react';

import { VentaFormDialog } from '@/components/ventas/venta-form-dialog';

import { useTranslations } from '@/hooks/useTranslations';
import { useAuth } from '@/lib/auth/AuthProvider';
import { InventarioDisponiblePanel } from '@/components/ventas/inventario-disponible-panel';
import { PreciosVentaPanel } from '@/components/ventas/precios-venta-panel';
import { ClientesPanel } from '@/components/ventas/clientes-panel';
import { VentasHistorialPanel } from '@/components/ventas/ventas-historial-panel';

import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Stack from '@mui/material/Stack';
import Tab from '@mui/material/Tab';
import Tabs from '@mui/material/Tabs';
import Typography from '@mui/material/Typography';

type SalesTab = 'ventas' | 'precios' | 'clientes';

export default function VentasPage() {
  const nav = useTranslations('navigation');
  const tc = useTranslations('common');
  const tp = useTranslations('pages');
  const ts = useTranslations('sales');

  const { has } = useAuth();
  const canViewVentas = has('Ventas.Ventas', 'Ver');
  const canViewPrecios = has('Ventas.Precios', 'Ver');
  const canViewClientes = has('Ventas.Clientes', 'Ver');

  const canCreatePrecios = has('Ventas.Precios', 'Crear');
  const canEditPrecios = has('Ventas.Precios', 'Editar');

  const canCreateClientes = has('Ventas.Clientes', 'Crear');
  const canEditClientes = has('Ventas.Clientes', 'Editar');

  const canQueryInventory =
    has('Inventario.Lotes', 'Ver') && has('Inventario.Existencias', 'Ver');
  const canLoadInventoryCatalogs =
    has('Inventario.Presentaciones', 'Ver') && has('Inventario.TiposCafe', 'Ver');

  const canQueryMovements =
    has('Inventario.Lotes', 'Ver') && has('Inventario.Movimientos', 'Ver');

  const canRegisterPayments = has('Ventas.Ventas', 'Crear');
  const canUpdateDelivery = has('Ventas.Ventas', 'Editar');
  const canCancelSale = has('Ventas.Ventas', 'Cancelar');
  const canAnulateSale = has('Ventas.Ventas', 'Anular');

  const canAccessVentas = canViewVentas || canViewPrecios || canViewClientes;

  const [activeTab, setActiveTab] = useState<SalesTab | null>(null);
  const [ventaDialogOpen, setVentaDialogOpen] = useState(false);

  const availableTabs = useMemo<SalesTab[]>(() => {
    const tabs: SalesTab[] = [];
    if (canViewVentas) tabs.push('ventas');
    if (canViewPrecios) tabs.push('precios');
    if (canViewClientes) tabs.push('clientes');
    return tabs;
  }, [canViewVentas, canViewPrecios, canViewClientes]);

  const effectiveTab =
    activeTab && availableTabs.includes(activeTab)
      ? activeTab
      : (availableTabs[0] ?? 'ventas');

  if (!canAccessVentas) {
    return (
      <Stack spacing={1}>
        <Typography variant="h4">{nav('ventasCafe')}</Typography>
        <Typography variant="body2" color="text.secondary">
          {tp('noPermission')}
        </Typography>
      </Stack>
    );
  }

  return (
    <Stack spacing={2}>
      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        alignItems={{ xs: 'flex-start', sm: 'center' }}
        justifyContent="space-between"
        spacing={2}
      >
        <Box>
          <Typography variant="h4">{nav('ventasCafe')}</Typography>
          <Typography variant="body2" color="text.secondary">
            {ts('subtitle')}
          </Typography>
        </Box>

        <Stack direction="row" spacing={1} useFlexGap flexWrap="wrap">
          <Button variant="outlined" disabled>
            {tc('refresh')}
          </Button>
          <Button
            variant="contained"
            onClick={() => setVentaDialogOpen(true)}
            disabled={!canViewVentas || !canQueryInventory}
          >
            {ts('newVentaTitle')}
          </Button>
        </Stack>
      </Stack>

      {availableTabs.length > 1 ? (
        <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
          <Tabs
            value={effectiveTab}
            onChange={(_event, value: SalesTab) => setActiveTab(value)}
            variant="scrollable"
            allowScrollButtonsMobile
          >
            {canViewVentas ? <Tab value="ventas" label={ts('tabVentas')} /> : null}
            {canViewPrecios ? <Tab value="precios" label={ts('tabPrecios')} /> : null}
            {canViewClientes ? <Tab value="clientes" label={ts('tabClientes')} /> : null}
          </Tabs>
        </Box>
      ) : null}

      {effectiveTab === 'ventas' ? (
        <Stack spacing={2}>
          <InventarioDisponiblePanel
            canQuery={canQueryInventory}
            canLoadCatalogs={canLoadInventoryCatalogs}
          />
          <VentasHistorialPanel
            canView={canViewVentas}
            canQueryMovements={canQueryMovements}
            canRegisterPayments={canRegisterPayments}
            canUpdateDelivery={canUpdateDelivery}
            canCancelSale={canCancelSale}
            canAnulateSale={canAnulateSale}
          />
        </Stack>
      ) : null}

      {effectiveTab === 'precios' ? (
        <PreciosVentaPanel
          canView={canViewPrecios}
          canCreate={canCreatePrecios}
          canEdit={canEditPrecios}
        />
      ) : null}

      {effectiveTab === 'clientes' ? (
        <ClientesPanel
          canView={canViewClientes}
          canCreate={canCreateClientes}
          canEdit={canEditClientes}
        />
      ) : null}

      <VentaFormDialog
        open={ventaDialogOpen}
        onClose={() => setVentaDialogOpen(false)}
        canQueryInventory={canQueryInventory}
        canLoadCatalogs={canLoadInventoryCatalogs}
        canLoadClientes={canViewClientes}
        canLoadPrecios={canViewPrecios}
      />
    </Stack>
  );
}