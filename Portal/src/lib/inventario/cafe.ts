export type EstadoLoteCafe = 1 | 2 | 3 | 4;

export type EstadoLoteFiltro = 'all' | EstadoLoteCafe;

export type TipoMovimientoLoteCafe = 1 | 2 | 3 | 4;

export type EstadoCatalogoCafe = 1 | 2 | 3;

export type PresentacionCafe = {
  presentacionCafeId: number;
  codigo: string;
  nombre: string;
  pesoUnitarioGramos: number;
  estado: EstadoCatalogoCafe;
  creadoEn: string;
  actualizadoEn?: string | null;
};

export type TipoCafe = {
  tipoCafeId: number;
  codigo: string;
  nombre: string;
  estado: EstadoCatalogoCafe;
  creadoEn: string;
  actualizadoEn?: string | null;
};

export type LoteCafeResumen = {
  loteCafeId: number;
  codigoLote: string;
  fechaProduccion: string;
  fechaEnvasado?: string | null;
  fechaVencimiento?: string | null;
  estado: EstadoLoteCafe;
  observacion?: string | null;
  creadoEn: string;
  actualizadoEn?: string | null;
  totalPaquetes: number;
  totalPesoNetoKg: number;
};

export type ExistenciaCafe = {
  loteCafeExistenciaId: number;
  loteCafeId: number;
  presentacionCafeId: number;
  presentacionCodigo: string;
  presentacionNombre: string;
  presentacionEstado: EstadoCatalogoCafe;
  tipoCafeId: number;
  tipoCafeCodigo: string;
  tipoCafeNombre: string;
  tipoCafeEstado: EstadoCatalogoCafe;
  cantidadPaquetes: number;
  pesoUnitarioGramos: number;
  pesoNetoKg: number;
  actualizadoEn?: string | null;
};

export type MovimientoCafe = {
  movimientoLoteCafeId: number;
  loteCafeExistenciaId: number;
  loteCafeId: number;
  tipoMovimiento: TipoMovimientoLoteCafe;
  cantidadPaquetes: number;
  pesoNetoKg: number;
  fechaMovimiento: string;
  referencia?: string | null;
  observacion?: string | null;
  usuarioId?: number | null;
  creadoEn: string;
  presentacionCafeId: number;
  presentacionCodigo: string;
  presentacionNombre: string;
  tipoCafeId: number;
  tipoCafeCodigo: string;
  tipoCafeNombre: string;
};

export type ChipColor = 'default' | 'info' | 'success' | 'warning' | 'error';

export function filterLotes(
  lotes: readonly LoteCafeResumen[],
  query: string,
  estado: EstadoLoteFiltro,
): LoteCafeResumen[] {
  const normalizedQuery = query.trim().toLowerCase();

  return lotes.filter((lote) => {
    if (estado !== 'all' && lote.estado !== estado) {
      return false;
    }

    return !normalizedQuery || lote.codigoLote.toLowerCase().includes(normalizedQuery);
  });
}

export function estadoLoteKey(estado: EstadoLoteCafe): string {
  switch (estado) {
    case 1:
      return 'lotStateActive';
    case 2:
      return 'lotStateAgotado';
    case 3:
      return 'lotStateBloqueado';
    default:
      return 'lotStateEliminado';
  }
}

export function estadoLoteColor(estado: EstadoLoteCafe): ChipColor {
  switch (estado) {
    case 1:
      return 'success';
    case 2:
      return 'warning';
    case 3:
      return 'error';
    default:
      return 'default';
  }
}

export function tipoMovimientoKey(tipo: TipoMovimientoLoteCafe): string {
  switch (tipo) {
    case 1:
      return 'movementIngreso';
    case 2:
      return 'movementSalida';
    case 3:
      return 'movementAjustePositivo';
    default:
      return 'movementAjusteNegativo';
  }
}

export function tipoMovimientoColor(tipo: TipoMovimientoLoteCafe): ChipColor {
  switch (tipo) {
    case 1:
      return 'success';
    case 2:
      return 'warning';
    case 3:
      return 'info';
    default:
      return 'error';
  }
}

export function isCatalogoActivo(estado: EstadoCatalogoCafe): boolean {
  return estado === 1;
}

export function estadoCatalogoKey(estado: EstadoCatalogoCafe): string {
  switch (estado) {
    case 1:
      return 'catalogStateActive';
    case 2:
      return 'catalogStateInactive';
    default:
      return 'catalogStateDeleted';
  }
}

export function estadoCatalogoColor(estado: EstadoCatalogoCafe): ChipColor {
  switch (estado) {
    case 1:
      return 'success';
    case 2:
      return 'default';
    default:
      return 'error';
  }
}

export function formatDate(locale: string, value?: string | null): string {
  const date = parseDate(value);
  if (!date) return '—';

  return new Intl.DateTimeFormat(locale, { dateStyle: 'medium' }).format(date);
}

export function formatDateTime(locale: string, value?: string | null): string {
  const date = parseDate(value);
  if (!date) return '—';

  return new Intl.DateTimeFormat(locale, {
    dateStyle: 'short',
    timeStyle: 'short',
  }).format(date);
}

export function formatNumber(locale: string, value: number): string {
  return new Intl.NumberFormat(locale, { maximumFractionDigits: 0 }).format(value);
}

export function formatGrams(locale: string, value: number): string {
  return `${new Intl.NumberFormat(locale, { maximumFractionDigits: 0 }).format(value)} g`;
}

export function formatKg(locale: string, value: number): string {
  return `${new Intl.NumberFormat(locale, {
    minimumFractionDigits: 0,
    maximumFractionDigits: 3,
  }).format(value)} kg`;
}

function parseDate(value?: string | null): Date | null {
  if (!value) return null;

  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? null : date;
}
