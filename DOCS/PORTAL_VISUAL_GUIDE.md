# Guía Visual del Portal ProyectoCafe

## Alcance

El portal vigente cubre los módulos **Administración**, **Inventario** y **Ventas**. La guía describe cómo comunicar visualmente los indicadores de usuarios, roles, permisos, lotes, existencias, movimientos, ventas, pagos y entregas.

La pantalla **Dashboard** se mantiene como pantalla de entrada vacía, sin componentes ni indicadores.

## Semántica de administración

- **Usuarios**: usar primario verde cafetal para altas y estado de las cuentas.
- **Roles**: usar secundario café tostado para la estructura de roles y sus asignaciones.
- **Permisos**: usar acento cosecha para permisos efectivos, módulos cubiertos y asignaciones por rol.
- **Riesgo y excepción**: usar warning y error solo para cuentas pendientes, bloqueos, roles inactivos o inconsistencias reales.

## Semántica de inventario

- **Lotes**: usar primario verde cafetal para lotes activos y el detalle seleccionado.
- **Existencias**: mostrar paquetes y peso neto con cifras alineadas a la derecha; el saldo bajo o insuficiente usa warning.
- **Movimientos**: diferenciar ingreso, salida, ajuste positivo y ajuste negativo con chips y colores consistentes.
- **Catálogos**: usar chips para activo, inactivo y eliminado; los catálogos inactivos o eliminados se muestran solo para conservar el historial.
- **Lote bloqueado**: usar error y una alerta visible; no ofrecer acciones de movimiento.

## Semántica de ventas

- **Venta registrada**: estado neutro o primario; **cancelada** y **anulada** usan warning/error según el impacto.
- **Pago pendiente, abono parcial y pagada**: usar warning, info y success respectivamente.
- **Entrega**: usar una progresión visual discreta (pendiente, en preparación, lista, entregada) sin competir con el estado de pago.
- **Inventario disponible**: reutilizar la semántica de existencias; no mostrar acciones de venta si faltan permisos de inventario.
- **Acciones destructivas**: **Cancelar venta** y **Anular venta** exigen confirmación y motivo; se presentan como operaciones administrativas separadas del flujo normal.

## Reglas por componente

- **Chips**: la variante light usa fondo suave, texto oscuro y borde tenue. `default` queda en neutro cálido para estados inactivos o informativos sin prioridad.
- **Alerts**: `standard` y `outlined` comunican contexto sin competir con la navegación; `filled` queda reservado para confirmaciones o alertas de mayor prioridad.
- **Tablas**: fondo paper, encabezado neutro cálido, hover y selección con tinte primario. Los chips dentro de celdas deben verse como etiquetas, no como bloques pesados.

## Autenticación (Login y Register)

- Ambas pantallas comparten `AuthWrapper1` y `AuthCardWrapper` para conservar el fondo, la tarjeta, el sombreado y el espaciado.
- La tarjeta ocupa el 100% del ancho disponible con un máximo de 400 px en móvil/tablet y 475 px en `lg`. Así, la cantidad de campos de cada formulario no altera el ancho de la tarjeta.
- El contenedor de la tarjeta se estira al ancho disponible y centra su contenido con flex, evitando desbordes horizontales en pantallas pequeñas.
- Los campos, botones y enlaces usan `fullWidth` y el mismo `gap` en ambas pantallas para mantener la alineación vertical.

## Estados de acceso

- **Acceso completo** (`success`): usuario activo con al menos un rol vigente.
- **Sin rol asignado** (`info`): cuenta activa que no puede acceder a secciones protegidas.
- **Pendiente de activación** (`warning`): cuenta registrada que aún requiere acción administrativa.
- **Rol inactivo** (`default`): rol conservado, pero no vigente para asignación.

## Archivos de referencia

- `Portal/src/theme/berry/overrides/Chip.ts`
- `Portal/src/theme/berry/overrides/Alert.tsx`
- `Portal/src/theme/berry/overrides/DataGrid.ts`
- `Portal/src/app/administracion/usuarios/page.tsx`
- `Portal/src/app/administracion/roles/page.tsx`
- `Portal/src/app/inventario/CafeTostado/page.tsx`
- `Portal/src/app/dashboard/ventas/page.tsx`
- `Portal/src/components/inventario/`
- `Portal/src/components/ventas/`
