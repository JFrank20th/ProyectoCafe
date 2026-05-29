# Guia Visual del Portal ProyectoCafe

## Semantica de negocio

- Produccion: usar primario verde cafetal para rendimiento, disponibilidad, avance de proceso y stock util.
- Comercial: usar secundario cafe tostado para ventas, clientes, pedidos, facturacion y relacion comercial.
- Finanzas y destacados: usar acento cosecha para margen, cumplimiento, comparativos y metas clave.
- Trazabilidad e informacion: usar info teal para origen, seguimiento, rutas operativas e integraciones.
- Riesgo y excepcion: usar warning y error solo para alertas, bloqueos, desbalances o incidentes.

## Reglas por componente

- Chips: la variante light usa fondo suave, texto oscuro y borde tenue. `default` queda en neutro calido para estados inactivos o informativos sin prioridad.
- Alerts: `standard` y `outlined` comunican contexto sin competir con la navegacion; `filled` queda reservado para confirmaciones o alertas de mayor prioridad.
- DataGrid: fondo paper, encabezado neutro calido, hover y seleccion con tinte primario. Los badges dentro de celdas deben verse como etiquetas, no como bloques pesados.
- KPI cards: cada tarjeta usa una sola familia cromatica, una linea superior de acento, un icono con tinte suave y texto secundario estable sobre superficie clara.
- KPI cards: el chip de tendencia debe mostrar delta real contra el periodo anterior equivalente. Si el usuario aplica un rango personalizado, la comparacion toma la ventana inmediatamente anterior con la misma duracion.

## Mapa recomendado para dashboards y KPI

- Produccion: volumen cosechado, conversion, rendimiento, disponibilidad de lotes, inventario util.
- Comercial: ventas comprometidas, pedidos, clientes activos, ticket promedio, frecuencia de compra.
- Finanzas: margen bruto, utilidad esperada, costo por kilo, cumplimiento de meta, comparativos de periodo.
- Trazabilidad: lotes conciliados, origen completo, empaques vinculados, eventos auditables, seguimiento de salida.
- Riesgo: costos fuera de rango, pedidos pendientes, mermas altas, inconsistencias documentales, bloqueos operativos.

## Filtros temporales

- Mantener presets cortos para lectura rapida: 7, 30, 90 y 365 dias, mas historico cuando tenga sentido operativo.
- Permitir rango personalizado `desde/hasta` cuando el usuario necesite cierres, cortes contables o analisis por campaña.
- Mostrar siempre el rango activo y, cuando exista comparacion, dejar explicito el periodo anterior usado para el delta.

## Archivos de referencia

- Portal/src/theme/berry/overrides/Chip.ts
- Portal/src/theme/berry/overrides/Alert.tsx
- Portal/src/theme/berry/overrides/DataGrid.ts
- Portal/src/components/dashboard/kpi-metric-card.tsx
- Portal/src/app/dashboard/page.tsx