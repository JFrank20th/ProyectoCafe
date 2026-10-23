# Guía Visual del Portal ProyectoCafe

## Alcance

El portal vigente cubre los módulos **Dashboard** y **Administración**. La guía describe cómo comunicar visualmente los indicadores de usuarios, roles, permisos y cobertura de acceso. No aplica a módulos operativos retirados.

## Semántica de administración

- **Usuarios**: usar primario verde cafetal para altas, crecimiento del periodo y estado de las cuentas.
- **Roles**: usar secundario café tostado para la estructura de roles y sus asignaciones.
- **Permisos**: usar acento cosecha para permisos efectivos, módulos cubiertos y asignaciones por rol.
- **Cobertura de acceso**: usar info teal para la relación entre usuarios activos y accesos asignados.
- **Riesgo y excepción**: usar warning y error solo para cuentas pendientes, bloqueos, roles inactivos o inconsistencias reales.

## Reglas por componente

- **Chips**: la variante light usa fondo suave, texto oscuro y borde tenue. `default` queda en neutro cálido para estados inactivos o informativos sin prioridad.
- **Alerts**: `standard` y `outlined` comunican contexto sin competir con la navegación; `filled` queda reservado para confirmaciones o alertas de mayor prioridad.
- **Tablas**: fondo paper, encabezado neutro cálido, hover y selección con tinte primario. Los chips dentro de celdas deben verse como etiquetas, no como bloques pesados.
- **KPI cards**: cada tarjeta usa una sola familia cromática, una línea superior de acento, un icono con tinte suave y texto secundario estable sobre superficie clara.
- **KPI cards**: solo la tarjeta de nuevos usuarios muestra delta contra el periodo anterior equivalente. Las tarjetas de roles, permisos y cobertura son una foto del estado actual y deben rotularse como **Estado actual**.
- **Rango personalizado**: la comparación toma la ventana inmediatamente anterior con la misma duración. Si el filtro es histórico, se muestra **Sin comparativo**.

## Mapa recomendado para el Dashboard

| Tarjeta | Dato principal | Apoyo | Tono |
| --- | --- | --- | --- |
| Nuevos usuarios | Altas del periodo | Usuarios activos sobre el total | Primario |
| Roles activos | Roles vigentes | Asignaciones sobre el total de roles | Secundario |
| Permisos vigentes | Permisos distintos | Módulos y asignaciones registradas | Acento cosecha |
| Cobertura de acceso | Porcentaje de usuarios activos con rol | Usuarios con rol sobre usuarios activos | Info teal |

## Filtros temporales

- Mantener presets cortos para lectura rápida: 7, 30, 90 y 365 días, más histórico.
- Permitir rango personalizado `desde/hasta` cuando se necesite revisar un corte puntual de altas.
- Aclarar en la interfaz que el periodo filtra únicamente las altas de usuarios; el resto de indicadores refleja el estado actual.
- Mostrar siempre el rango activo y, cuando exista comparación, dejar explícito el periodo anterior usado para el delta.

## Estados de acceso

- **Acceso completo** (`success`): usuario activo con al menos un rol vigente.
- **Sin rol asignado** (`info`): cuenta activa que no puede acceder a secciones protegidas.
- **Pendiente de activación** (`warning`): cuenta registrada que aún requiere acción administrativa.
- **Rol inactivo** (`default`): rol conservado, pero no vigente para asignación.

## Archivos de referencia

- `Portal/src/theme/berry/overrides/Chip.ts`
- `Portal/src/theme/berry/overrides/Alert.tsx`
- `Portal/src/theme/berry/overrides/DataGrid.ts`
- `Portal/src/components/dashboard/kpi-metric-card.tsx`
- `Portal/src/lib/dashboard/summary-metrics.ts`
- `Portal/src/app/dashboard/page.tsx`
- `Portal/src/app/administracion/usuarios/page.tsx`
- `Portal/src/app/administracion/roles/page.tsx`
