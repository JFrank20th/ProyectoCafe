# Guía Visual del Portal ProyectoCafe

## Alcance

El portal vigente cubre el módulo **Administración**. La guía describe cómo comunicar visualmente los indicadores de usuarios, roles, permisos y estados de acceso. No aplica a módulos operativos retirados.

La pantalla **Dashboard** se mantiene como pantalla de entrada vacía, sin componentes ni indicadores.

## Semántica de administración

- **Usuarios**: usar primario verde cafetal para altas y estado de las cuentas.
- **Roles**: usar secundario café tostado para la estructura de roles y sus asignaciones.
- **Permisos**: usar acento cosecha para permisos efectivos, módulos cubiertos y asignaciones por rol.
- **Riesgo y excepción**: usar warning y error solo para cuentas pendientes, bloqueos, roles inactivos o inconsistencias reales.

## Reglas por componente

- **Chips**: la variante light usa fondo suave, texto oscuro y borde tenue. `default` queda en neutro cálido para estados inactivos o informativos sin prioridad.
- **Alerts**: `standard` y `outlined` comunican contexto sin competir con la navegación; `filled` queda reservado para confirmaciones o alertas de mayor prioridad.
- **Tablas**: fondo paper, encabezado neutro cálido, hover y selección con tinte primario. Los chips dentro de celdas deben verse como etiquetas, no como bloques pesados.

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
