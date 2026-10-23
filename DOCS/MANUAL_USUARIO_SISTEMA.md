<div align="center">

# Manual de Usuario del Sistema ProyectoCafe

## Edición lista para impresión y PDF

**Versión del documento:** 1.0  
**Fecha de emisión:** 20 de abril de 2026  
**Formato recomendado:** A4 vertical  
**Margen recomendado:** 18 mm a 20 mm  
**Uso sugerido:** impresión física o exportación a PDF

</div>

| Campo | Valor |
| --- | --- |
| Documento fuente | MANUAL_USUARIO_SISTEMA.md |
| Edición | Impresión / PDF |
| Público objetivo | Usuarios operativos, administrativos y de consulta |
| Control editorial | Esta edición conserva el contenido funcional y añade portada y saltos de página para exportación |

## Recomendaciones de exportación

1. Use tamaño A4 en orientación vertical.
2. Configure márgenes entre 18 mm y 20 mm.
3. Si la herramienta de exportación lo permite, active numeración de páginas en encabezado o pie.
4. Mantenga los saltos de página incluidos en el documento.
5. Para impresión física, use doble cara solo si la encuadernación no interfiere con tablas o listados extensos.

<div style="page-break-after: always;"></div>

## 1. Propósito del manual

Este manual explica cómo usar ProyectoCafe para registrar la operación del negocio y mantener la trazabilidad desde la producción o compra del café hasta la venta final.

Está pensado para usuarios no técnicos. Por esa razón, el documento se enfoca en lo que el usuario ve en pantalla, qué debe capturar en cada paso y cómo validar que la información quedó correctamente registrada.

Este manual describe el comportamiento actual del sistema a abril de 2026. Cuando existe una necesidad operativa del negocio que aún no está disponible como campo o pantalla, se indica claramente como límite actual.

## Índice

1. Propósito del manual.
2. Alcance del sistema.
3. Reglas generales de uso.
4. Conceptos clave.
5. Acceso al sistema.
6. Navegación general.
7. Módulo Dashboard.
8. Módulo Administración.
9. Módulo Compras.
10. Módulo Producción.
11. Módulo Inventario.
12. Módulo Ventas.
13. Módulo Contabilidad.
14. Proceso integral de trazabilidad del negocio.
15. Buenas prácticas operativas.
16. Límites actuales del sistema.
17. Glosario breve.
18. Cierre.

## 2. Alcance del sistema

ProyectoCafe está organizado en los siguientes módulos:

| Módulo | Propósito principal |
| --- | --- |
| Acceso y autenticación | Ingreso al sistema, creación de cuenta y control de sesión |
| Dashboard | Vista resumida del estado operativo, comercial, financiero y de trazabilidad |
| Administración | Gestión de usuarios, roles y permisos |
| Compras | Registro de proveedores y recepciones de café compradas |
| Producción | Registro de fincas, cultivos, operaciones agrícolas, cosechas, transformaciones y empaques |
| Inventario | Consulta de lotes, productos, insumos y catálogos maestros |
| Ventas | Registro de clientes, tipos de venta y ventas con salida por lote |
| Contabilidad | Consulta de balance y administración de costos, gastos y pasivos |

## 3. Reglas generales de uso

Antes de trabajar en cualquier módulo, tenga presentes estas reglas:

1. Las opciones visibles en el menú dependen de los permisos del usuario. Si no ve un módulo, normalmente significa que no tiene autorización para usarlo.
2. En los procesos operativos sensibles, como cosechas, recepciones, transformaciones, empaques y ventas, el sistema prioriza registrar y anular. En general, no se editan registros ya creados; si hubo un error, se anula el registro y se vuelve a crear correctamente.
3. Los lotes son el eje de la trazabilidad. Siempre que el sistema pida un lote o genere un lote, ese dato debe tratarse como la referencia principal para seguir el producto.
4. Si un código de lote se deja vacío en los formularios que lo permiten, el sistema puede generarlo automáticamente.
5. El botón Actualizar recarga la información visible. Es útil después de registrar, anular o modificar datos en otro módulo.

## 4. Conceptos clave

### 4.1 Finca

Es la unidad productiva principal. Sirve para identificar dónde se desarrolla la actividad agrícola.

### 4.2 Cultivo

Es el registro del cultivo dentro de una finca. Se usa para identificar qué se produce, dónde se produce y relacionar operaciones agrícolas y cosechas con mayor precisión.

### 4.3 Operación agrícola

Es cualquier actividad realizada sobre el cultivo, por ejemplo aplicación de insumos, fertilización, poda, riego, jornal o mantenimiento. Estas operaciones también afectan costos y trazabilidad del lote cuando el lote proviene de una cosecha propia.

### 4.4 Cosecha

Es el registro del café recolectado en un cultivo. Al registrarlo, el sistema genera un lote inicial.

### 4.5 Recepción de café

Es el ingreso de café comprado a un proveedor. También genera un lote inicial, pero su origen queda marcado como compra.

### 4.6 Lote

Es la unidad central de trazabilidad. Un lote tiene código, producto, cantidad inicial, cantidad disponible, estado, origen y movimientos posteriores.

### 4.7 Transformación o bache

Es el proceso por el cual un lote origen se convierte en otro lote de salida, por ejemplo en despulpado, secado, trillado o tostión. Para efectos operativos, un bache de tostión se controla como una transformación.

### 4.8 Empaque

Es el proceso de convertir un lote origen en lotes de salida listos para presentaciones comerciales, por ejemplo 250 g, 454 g o 500 g.

### 4.9 Presentación

Es el tamaño comercial del producto empacado. Se define por código, nombre y peso en gramos.

### 4.10 Tipo de venta

Es la clasificación comercial usada para aplicar precio. En el sistema actual existen, como mínimo, Normal, Mayorista e Internet, dependiendo de la configuración de la empresa.

### 4.11 Precio aplicado

Es el valor unitario que el sistema toma para una venta según el tipo de venta elegido y la configuración del producto.

### 4.12 Trazabilidad

Es la capacidad de seguir un producto desde su origen hasta su salida final. En ProyectoCafe esto se logra principalmente a través de la relación entre lote, movimientos, transformaciones, empaques, costos y ventas.

## 5. Acceso al sistema

## 5.1 Descripción general

El acceso al sistema se realiza desde las pantallas de inicio de sesión y registro. Un usuario puede entrar con sus credenciales o crear una cuenta, la cual queda pendiente de activación por un administrador.

## 5.2 Funcionalidades principales

1. Iniciar sesión con correo y contraseña.
2. Crear una cuenta nueva.
3. Volver a la pantalla de inicio desde el registro.
4. Trabajar con una sesión autenticada para que el sistema muestre solo los módulos permitidos.

## 5.3 Pantallas y opciones

### Pantalla de inicio de sesión

Opciones principales:

1. Email.
2. Contraseña.
3. Botón Ingresar.
4. Enlace Crear cuenta.

### Pantalla de registro

Opciones principales:

1. Registro de nueva cuenta.
2. Mensaje de que la cuenta quedará pendiente de activación.
3. Botón Registrarse.
4. Enlace Volver a iniciar sesión.

Si la organización tiene habilitado el flujo de confirmación de correo, el usuario debe completar ese paso según la instrucción recibida antes o después de la activación administrativa.

## 5.4 Flujo paso a paso

1. Abra el portal.
2. Ingrese su correo y contraseña.
3. Haga clic en Ingresar.
4. Si su usuario está activo, verá el Dashboard y el menú lateral.
5. Si aún no tiene acceso, use Crear cuenta y complete el registro.
6. Espere la activación y asignación de roles por parte de un administrador.

## 5.5 Ejemplo práctico

Ejemplo: un nuevo auxiliar de ventas se registra con su correo corporativo. Luego un administrador lo activa y le asigna el rol adecuado. Después de eso, al iniciar sesión verá Ventas e Inventario, pero no verá Producción ni Administración si no tiene esos permisos.

## 6. Navegación general

## 6.1 Descripción general

La navegación se realiza desde el menú lateral. El sistema agrupa los módulos por área: principal, ventas, producción, contabilidad, inventario, compras y administración.

## 6.2 Funcionalidades principales

1. Acceso directo a cada pantalla habilitada.
2. Ocultamiento automático de opciones no autorizadas.
3. Botones comunes para actualizar, crear, ver detalle, editar, eliminar o anular.

## 6.3 Elementos visibles más comunes

1. Dashboard como pantalla principal.
2. Botón Actualizar para recargar datos.
3. Botón Nuevo para crear registros.
4. Listado en tabla con estado y acciones.
5. Chips o etiquetas para mostrar estado del registro.
6. Cuadros de diálogo para crear, editar, anular o confirmar eliminación.

## 6.4 Flujo de uso paso a paso

1. Ingrese al sistema.
2. Revise el menú lateral.
3. Entre al módulo deseado.
4. Use Actualizar si necesita refrescar la información.
5. Use Nuevo para registrar un dato nuevo.
6. Use Ver detalle para revisar trazabilidad o información extendida.
7. Use Anular cuando un proceso operativo ya creado deba revertirse.

## 6.5 Ejemplo práctico

Ejemplo: un operador registra una cosecha en Producción. Luego entra a Inventario, abre Lotes, filtra por producto y revisa el lote generado en el detalle de trazabilidad.

<div style="page-break-after: always;"></div>

## 7. Módulo Dashboard

## 7.1 Descripción general del propósito

El Dashboard es la portada operativa del sistema. Resume el estado de la producción, las ventas, la trazabilidad y el comportamiento financiero general.

## 7.2 Funcionalidades principales

1. Consultar indicadores clave.
2. Cambiar el período de análisis.
3. Usar rango personalizado de fechas.
4. Comparar el período actual con el período anterior.
5. Interpretar la información con ayudas visuales de color.

## 7.3 Explicación de opciones y pantalla

La pantalla incluye:

1. Encabezado principal con descripción del estado operativo.
2. Selector de período con opciones de 7 días, 30 días, 90 días, 12 meses, histórico y rango personalizado.
3. Campos Desde y Hasta cuando se usa rango personalizado.
4. Botón Aplicar rango.
5. Tarjetas KPI, entre ellas producción disponible, ventas comprometidas, margen bruto esperado y lotes con trazabilidad.
6. Guía rápida de semántica visual para producción, comercial, finanzas, trazabilidad y riesgo.

## 7.4 Flujo de uso paso a paso

1. Ingrese al Dashboard.
2. Seleccione el período operativo deseado.
3. Si necesita un rango manual, elija Rango personalizado.
4. Ingrese fecha inicial y fecha final.
5. Haga clic en Aplicar rango.
6. Revise las tarjetas KPI y la comparación contra el período anterior.
7. Use esta vista para decidir en qué módulo profundizar después.

## 7.5 Ejemplo práctico

Ejemplo: el gerente revisa el Dashboard en período de 30 días para confirmar cuántos lotes siguen trazables, cuánto margen bruto se está generando y si la operación necesita más atención en ventas o producción.

<div style="page-break-after: always;"></div>

## 8. Módulo Administración

## 8.1 Descripción general del propósito

Este módulo controla quién puede entrar al sistema y qué puede hacer cada usuario. Está orientado a la seguridad operativa.

## 8.2 Funcionalidades principales

1. Consultar usuarios registrados.
2. Activar o desactivar usuarios.
3. Asignar roles a usuarios.
4. Crear y editar roles.
5. Asignar permisos por módulo y acción.

## 8.3 Pantalla Usuarios

### Propósito

Permite administrar el estado operativo de cada usuario y asignarle roles.

Nota operativa: esta pantalla administra cuentas ya registradas. La creación inicial de usuarios se realiza desde el flujo de registro del portal y luego el administrador activa la cuenta y asigna roles.

### Opciones y campos de la pantalla

1. Email.
2. Nombre.
3. Estado del usuario.
4. Fecha de creación.
5. Último login.
6. Roles asignados.
7. Acción Editar roles.
8. Acción Activar o Desactivar.
9. Acción Eliminar.

### Flujo de uso paso a paso

1. Entre a Administración.
2. Abra Usuarios.
3. Revise el estado del usuario.
4. Si el usuario está pendiente, use Activar.
5. Si necesita cambiar acceso, use Editar roles.
6. Seleccione uno o varios roles.
7. Guarde los cambios.
8. Si el usuario no debe seguir operando, desactívelo o elimínelo según la política interna.

### Ejemplo práctico

Ejemplo: un nuevo operador de producción ya se registró. El administrador lo activa, le asigna el rol de Producción y guarda. A partir de ese momento el usuario podrá ver Fincas, Cultivos, Operaciones, Cosechas, Transformaciones y Empaques.

## 8.4 Pantalla Roles

### Propósito

Permite crear perfiles de acceso y definir qué pantallas y acciones puede usar cada perfil.

### Opciones y campos de la pantalla

1. Nombre del rol.
2. Descripción.
3. Estado del rol.
4. Cantidad de permisos asignados.
5. Acción Editar.
6. Acción Permisos.
7. Acción Eliminar.

### Gestión de permisos

Desde la opción Permisos se seleccionan acciones por módulo. En términos operativos, los permisos suelen responder a acciones como Ver, Crear, Editar y Eliminar. En varios procesos, la acción Eliminar se usa en la interfaz como capacidad para anular.

### Flujo de uso paso a paso

1. Abra Administración.
2. Ingrese a Roles.
3. Haga clic en Nuevo rol si necesita crear uno.
4. Capture nombre, descripción y estado.
5. Guarde.
6. Abra la acción Permisos.
7. Marque los módulos y acciones necesarios.
8. Guarde nuevamente.

### Ejemplo práctico

Ejemplo: la empresa crea el rol Comercial Mayorista. Ese rol recibe permisos para ver y crear ventas, ver clientes, ver lotes y consultar productos, pero no recibe permisos para costos, fincas ni roles.

<div style="page-break-after: always;"></div>

## 9. Módulo Compras

## 9.1 Descripción general del propósito

El módulo de Compras se usa cuando el café no proviene de producción propia, sino de un proveedor. Su objetivo es registrar correctamente el origen comprado para que la trazabilidad no se rompa.

## 9.2 Funcionalidades principales

1. Crear y mantener el catálogo de proveedores.
2. Registrar recepciones de café compradas.
3. Generar lotes de origen por compra.
4. Anular recepciones cuando hubo un registro incorrecto.

## 9.3 Pantalla Proveedores

### Propósito

Mantener la base de terceros a quienes se compra café.

### Opciones y campos de la pantalla

1. Nombre.
2. Documento.
3. Teléfono.
4. Email.
5. Dirección.
6. Estado activo o inactivo.

### Flujo de uso paso a paso

1. Entre a Compras.
2. Abra Proveedores.
3. Haga clic en Nuevo.
4. Ingrese los datos básicos del proveedor.
5. Active el registro.
6. Guarde.

### Ejemplo práctico

Ejemplo: la empresa empezará a comprar café pergamino a un nuevo aliado. Antes de registrar la recepción, primero crea el proveedor para poder asociar correctamente la compra.

## 9.4 Pantalla Recepciones de café

### Propósito

Registrar cada compra de café y convertirla en un lote trazable dentro del inventario.

### Opciones y campos de la pantalla

1. Proveedor.
2. Producto.
3. Estado del café.
4. Fecha de recepción.
5. Número de documento.
6. Cantidad.
7. Precio unitario.
8. Humedad.
9. Código de lote opcional.
10. Observaciones.
11. Estado de la recepción.

### Resultado del registro

Al guardar una recepción, el sistema genera un lote inicial asociado a esa compra.

### Flujo de uso paso a paso

1. Entre a Compras.
2. Abra Recepciones de café.
3. Haga clic en Nuevo.
4. Seleccione el proveedor.
5. Seleccione el producto comprado.
6. Defina el estado del café, por ejemplo cereza, recién lavado o seco.
7. Ingrese fecha, cantidad y precio unitario.
8. Capture humedad y documento si corresponde.
9. Si ya tiene un código físico de lote, escríbalo. Si no, deje el campo vacío.
10. Guarde.
11. Verifique que en la tabla aparezcan la recepción y su código de lote.

### Ejemplo práctico

Ejemplo: se reciben 120 kg de café seco de un proveedor. Se registra la fecha, el precio unitario, el documento de compra y el estado del café. Al guardar, el sistema crea el lote que luego podrá venderse o transformarse.

<div style="page-break-after: always;"></div>

## 10. Módulo Producción

## 10.1 Descripción general del propósito

Este módulo administra la producción propia: dónde se produce, qué actividades se realizan en el cultivo, qué se cosecha, cómo se transforma el café y cómo se empaca.

## 10.2 Funcionalidades principales

1. Registrar fincas y cultivos.
2. Registrar operaciones agrícolas y costos asociados.
3. Registrar cosechas y generar lotes de origen.
4. Procesar transformaciones, incluyendo tostión por bache.
5. Empacar café en presentaciones comerciales.

## 10.3 Pantalla Fincas

### Propósito

Crear y mantener el catálogo de fincas donde opera el negocio.

### Opciones y campos de la pantalla

1. Nombre.
2. Descripción.
3. Ubicación.
4. Municipio.
5. Departamento.
6. Área en hectáreas.
7. Estado activo o inactivo.

### Flujo de uso paso a paso

1. Entre a Producción.
2. Abra Fincas.
3. Haga clic en Nuevo.
4. Complete los datos de identificación y ubicación.
5. Guarde.

### Ejemplo práctico

Ejemplo: se crea la finca El Mirador con su municipio, departamento y área, para luego asociarle cultivos y cosechas.

## 10.4 Pantalla Cultivos

### Propósito

Organizar la producción dentro de cada finca y registrar con precisión qué se cultiva y de dónde proviene cada cosecha.

### Opciones y campos de la pantalla

1. Finca asociada.
2. Producto asociado.
3. Nombre del cultivo.
4. Descripción.
5. Cultivo.
6. Área en hectáreas.
7. Estado.

### Flujo de uso paso a paso

1. Abra Producción.
2. Ingrese a Cultivos.
3. Haga clic en Nuevo.
4. Seleccione la finca y el producto.
5. Ingrese el nombre, la descripción, el cultivo y el área.
6. Active el registro si corresponde.
7. Guarde.

### Ejemplo práctico

Ejemplo: en la finca El Mirador se crea el cultivo Lote Norte, asociado al producto café en cereza, para distinguir cosechas y operaciones agrícolas.

## 10.5 Pantalla Operaciones agrícolas

### Propósito

Registrar las actividades realizadas en el cultivo y los costos que estas generan.

### Funcionalidades principales

1. Registrar aplicación de insumo.
2. Registrar fertilización, poda, riego, jornal o mantenimiento.
3. Asociar insumos cuando aplique.
4. Capturar costo de mano de obra y otros costos.
5. Anular operaciones cuando sea necesario revertir consumo y costo.

### Opciones y campos de la pantalla

1. Cultivo.
2. Tipo de operación.
3. Fecha.
4. Insumo, cuando aplique.
5. Cantidad de insumo.
6. Costo de mano de obra.
7. Costo otros.
8. Observaciones.
9. Estado de la operación.

### Flujo de uso paso a paso

1. Entre a Producción.
2. Abra Operaciones agrícolas.
3. Haga clic en Nuevo.
4. Seleccione el cultivo.
5. Elija el tipo de operación.
6. Si usó insumos, selecciónelos y capture la cantidad.
7. Ingrese costos de mano de obra y otros costos.
8. Agregue observaciones si hace falta.
9. Guarde.

### Ejemplo práctico

Ejemplo: se registra una fertilización en el cultivo Lote Norte, con el insumo utilizado, la cantidad aplicada y el costo de mano de obra. Más adelante, este costo podrá verse relacionado al lote cuando ese cultivo genere una cosecha.

## 10.6 Pantalla Cosechas de café

### Propósito

Registrar el café recolectado en un cultivo y convertirlo en un lote inicial de producción propia.

### Funcionalidades principales

1. Registrar la cosecha.
2. Indicar producto y estado del café.
3. Capturar cantidad, humedad y costo de cosecha.
4. Generar el lote inicial.
5. Anular la cosecha si fue registrada incorrectamente.

### Opciones y campos de la pantalla

1. Cultivo.
2. Producto.
3. Estado del café.
4. Fecha de cosecha.
5. Cantidad.
6. Humedad.
7. Costo de cosecha.
8. Código de lote opcional.
9. Observaciones.
10. Estado del registro.

### Flujo de uso paso a paso

1. Abra Producción.
2. Ingrese a Cosechas de café.
3. Haga clic en Nuevo.
4. Seleccione el cultivo y el producto.
5. Elija el estado del café.
6. Ingrese fecha, cantidad y costo de cosecha.
7. Capture humedad si la conoce.
8. Defina un código de lote solo si la empresa ya maneja uno físico.
9. Guarde.
10. Revise el código de lote creado.

### Ejemplo práctico

Ejemplo: en el cultivo Lote Norte se cosechan 80 kg de café en cereza. Se registra la fecha, cantidad, humedad y costo. El sistema genera un lote que más adelante puede secarse, trillarse o tostarse.

## 10.7 Pantalla Transformaciones de café

### Propósito

Registrar procesos que toman un lote origen y generan un nuevo lote de salida. Aquí se puede controlar el proceso de tostión por bache.

### Funcionalidades principales

1. Seleccionar lote origen con stock.
2. Elegir el producto de salida.
3. Definir el tipo de transformación.
4. Registrar cantidades de entrada y salida.
5. Calcular merma y rendimiento visibles en el listado.
6. Asociar costo del proceso.
7. Generar un nuevo lote de salida.
8. Anular la transformación si hubo error.

### Opciones y campos de la pantalla

1. Lote origen.
2. Producto salida.
3. Tipo de transformación.
4. Fecha de transformación.
5. Cantidad de entrada.
6. Cantidad de salida.
7. Costo del proceso.
8. Código de lote de salida opcional.
9. Observaciones.
10. Estado del registro.

### Recomendación operativa para tostión por bache

Use esta pantalla cada vez que un lote entre al proceso de tostión. Cada registro debe representar un bache real, con su lote origen, cantidad de entrada, cantidad de salida y costo del proceso.

### Nota importante

Puede aparecer una opción interna llamada Empaque 454 g dentro del tipo de transformación. Para el proceso normal de empaque comercial se recomienda usar la pantalla Empaques de café, que es la ruta operativa principal para presentaciones.

### Flujo de uso paso a paso

1. Abra Producción.
2. Entre a Transformaciones de café.
3. Haga clic en Nuevo.
4. Seleccione el lote origen.
5. Seleccione el producto de salida.
6. Elija el tipo de transformación, por ejemplo Tostión.
7. Ingrese fecha, cantidad de entrada, cantidad de salida y costo del proceso.
8. Si ya tiene un código de lote de salida, escríbalo.
9. Guarde.
10. Revise el nuevo lote resultante.

### Ejemplo práctico

Ejemplo: un lote de café seco entra a un bache de tostión. Se registran 50 kg de entrada, 45 kg de salida y el costo del proceso. El sistema deja trazado el lote origen y el lote tostado resultante.

## 10.8 Pantalla Empaques de café

### Propósito

Transformar un lote origen en uno o varios lotes de salida listos para su venta en presentaciones comerciales.

### Funcionalidades principales

1. Seleccionar el lote origen con stock.
2. Definir fecha y costo de empaque.
3. Agregar una o varias líneas de empaque.
4. Elegir productos de salida con presentación asociada.
5. Crear lotes de salida por cada línea.
6. Consultar el peso total de la operación.
7. Anular empaques cuando sea necesario revertir lotes de salida.

### Opciones y campos de la pantalla

1. Lote origen.
2. Fecha de empaque.
3. Costo de empaque.
4. Observaciones.
5. Líneas de empaque.
6. Producto de salida por línea.
7. Cantidad por línea.
8. Código de lote de salida opcional por línea.
9. Peso total calculado.

### Flujo de uso paso a paso

1. Abra Producción.
2. Entre a Empaques de café.
3. Haga clic en Nuevo.
4. Seleccione el lote origen.
5. Ingrese la fecha y el costo de empaque.
6. Agregue una línea por cada presentación que vaya a producir.
7. En cada línea, seleccione el producto de salida correcto.
8. Ingrese la cantidad de unidades a empacar.
9. Repita para todas las presentaciones necesarias.
10. Guarde.
11. Revise los lotes de salida generados.

### Ejemplo práctico

Ejemplo: de un lote de café tostado se empacan 100 bolsas de 250 g, 60 bolsas de 454 g y 40 bolsas de 500 g. Cada presentación queda trazada como salida del lote origen y genera sus propios lotes de empaque.

<div style="page-break-after: always;"></div>

## 11. Módulo Inventario

## 11.1 Descripción general del propósito

Este módulo se enfoca en tres tareas diarias: revisar existencias, mantener productos y entrar rápido a recepciones, ventas o transformaciones. La trazabilidad por lote se conserva, pero queda detrás de una experiencia más simple para usuarios operativos.

## 11.2 Funcionalidades principales

1. Consultar existencias por producto y bajar al detalle por lote cuando haga falta.
2. Ir directamente a entradas, salidas o transformaciones desde inventario.
3. Crear productos sin depender de varias pantallas previas.
4. Mantener unidades de medida y definir el formato del producto sin salir del flujo principal.

## 11.3 Pantalla Existencias

### Propósito

Dar una vista rápida de qué productos tienen stock, cuántos lotes activos existen y cuánto hay disponible antes de revisar el detalle de un lote específico.

### Funcionalidades principales

1. Ver un resumen por producto con cantidad disponible y número de lotes.
2. Filtrar por producto.
3. Abrir el detalle de lotes de un producto.
4. Entrar al historial completo de un lote.
5. Ir directamente a registrar una salida o una transformación desde el resumen del producto.

### Explicación de opciones y pantallas

En la vista principal se muestra:

1. Tarjetas con total de productos, lotes activos y lotes sin stock.
2. Tabla resumen por producto.
3. Botones para ver detalle, registrar salida y transformar o empacar.
4. Debajo, la lista de lotes del producto seleccionado o del conjunto filtrado.

En el historial de un lote se muestran estas secciones:

1. Resumen general del lote.
2. Origen.
3. Historial de movimientos.
4. Transformaciones relacionadas.
5. Costos relacionados.
6. Operaciones agrícolas relacionadas.

### Flujo de uso paso a paso

1. Abra Inventario.
2. Ingrese a Existencias.
3. Revise primero el resumen por producto.
4. Si necesita una acción, use Registrar salida o Transformar o empacar desde la fila del producto.
5. Si necesita trazabilidad, filtre por producto y abra el lote correspondiente.
6. Revise origen, movimientos y transformaciones para entender el recorrido completo.

### Ejemplo práctico

Ejemplo: el área comercial quiere saber si todavía hay stock de Café molido 454 g. Desde Existencias revisa el resumen del producto, confirma cuántos lotes activos tiene y, si necesita detalle, abre el lote para revisar su historial.

## 11.4 Pantalla Productos

### Propósito

Mantener el catálogo comercial de productos con la menor cantidad posible de pasos.

### Funcionalidades principales

1. Crear y editar productos.
2. Definir o reutilizar el formato del producto sin salir del formulario.
3. Definir unidad de medida.
4. Configurar precios por tipo de venta.
5. Activar o inactivar productos.

### Opciones y campos de la pantalla

1. Nombre.
2. Descripción.
3. Unidad.
4. Formato.
5. Tabla de precios por tipo de venta.
6. Estado.

### Flujo de uso paso a paso

1. Abra Inventario.
2. Entre a Productos.
3. Haga clic en Nuevo.
4. Ingrese el nombre del producto.
5. Defina la unidad.
6. Complete el formato si el producto lo necesita, o reutilice uno ya usado.
7. Ingrese los precios según cada tipo de venta activo.
8. Guarde.

### Ejemplo práctico

Ejemplo: se crea el producto Café tostado molido 454 g, se completa el formato 454 g dentro del mismo formulario, se define la unidad UN y se cargan sus precios Normal, Mayorista e Internet.

## 11.5 Entradas, salidas y transformaciones

### Propósito

Ejecutar las operaciones reales del inventario desde su módulo fuente, sin una pantalla intermedia adicional.

### Funcionalidades principales

1. Registrar entrada desde Compras.
2. Registrar salida desde Ventas.
3. Transformar o empacar usando un lote existente como origen.

### Flujo de uso paso a paso

1. Revise Existencias si necesita confirmar stock o trazabilidad.
2. Entre directamente a Recepciones de café, Ventas o Procesos según la tarea.
3. Complete el flujo correspondiente.

### Ejemplo práctico

Ejemplo: para empacar café, el usuario revisa Existencias, entra a Procesos, elige el lote a usar, indica el producto resultante y registra el nuevo lote de salida.

## 11.6 Pantalla Unidades de Medida

### Propósito

Mantener el único catálogo auxiliar que sigue siendo compartido por todo el inventario.

### Elementos incluidos

1. Unidades de medida activas e inactivas.

### Flujo de uso paso a paso

1. Abra Inventario.
2. Ingrese a Unidades de medida.
3. Cree, edite o inactive la unidad que necesite.

### Ejemplo práctico

Ejemplo: el administrador crea la unidad KG para nuevos productos y la deja disponible para captura en Productos. Los materiales o insumos de trabajo se administran desde Producción.

## 11.7 Recomendación de uso

Para la operación diaria, use primero Existencias, Productos, Recepciones de café, Ventas y Procesos. Entre a Unidades de medida solo cuando haga falta una nueva unidad base. Los materiales se administran desde Producción.

<div style="page-break-after: always;"></div>

## 12. Módulo Ventas

## 12.1 Descripción general del propósito

El módulo de Ventas registra al cliente, el tipo de precio aplicado y la salida de inventario por lote. Es la etapa final de la trazabilidad comercial.

## 12.2 Funcionalidades principales

1. Mantener el catálogo de clientes.
2. Configurar tipos de venta.
3. Registrar ventas con una o varias líneas.
4. Asociar el lote vendido al detalle de la venta.
5. Consultar rentabilidad y salidas por lote.
6. Anular ventas cuando se deba revertir la salida de inventario.

## 12.3 Pantalla Clientes

### Propósito

Administrar la base de clientes que participan en la venta.

### Opciones y campos de la pantalla

1. Nombre.
2. Documento.
3. Teléfono.
4. Email.
5. Dirección.
6. Estado.

### Flujo de uso paso a paso

1. Abra Ventas.
2. Entre a Clientes.
3. Haga clic en Nuevo.
4. Ingrese los datos básicos.
5. Guarde.

### Ejemplo práctico

Ejemplo: se crea el cliente Tienda La Estación para poder registrar una venta mayorista con sus datos completos.

## 12.4 Pantalla Tipos de venta

### Propósito

Definir los tipos comerciales que controlan el precio aplicado en la venta.

### Opciones y campos de la pantalla

1. Código.
2. Nombre.
3. Orden.
4. Estado.

### Flujo de uso paso a paso

1. Abra Ventas.
2. Entre a Tipos de venta.
3. Haga clic en Nuevo.
4. Ingrese código y nombre.
5. Defina el orden.
6. Active el registro.
7. Guarde.

### Ejemplo práctico

Ejemplo: se crean los tipos Normal y Mayorista. Después, en Productos, se define el precio para cada tipo, y en Ventas el usuario solo tendrá que seleccionar el tipo adecuado.

## 12.5 Pantalla Ventas

### Propósito

Registrar la salida comercial del producto y dejar vinculados cliente, lote, precio aplicado y resultado económico de la operación.

### Funcionalidades principales

1. Seleccionar cliente.
2. Definir método y estado de pago.
3. Seleccionar tipo de precio.
4. Agregar varias líneas de producto.
5. Seleccionar lote cuando el producto se maneja por lotes.
6. Ver precio unitario y subtotal calculados automáticamente.
7. Consultar detalle completo de la venta.
8. Revisar resumen de rentabilidad.
9. Revisar salidas por lote.
10. Anular la venta y revertir el inventario.

### Opciones y campos de la pantalla de registro

1. Cliente.
2. Método de pago.
3. Estado de pago.
4. Tipo de precio.
5. Líneas de venta.
6. Producto por línea.
7. Lote por línea, cuando aplica.
8. Cantidad.
9. Precio unitario calculado.
10. Subtotal por línea.
11. Total general.

### Detalle de la venta

Al abrir Ver detalle, el usuario puede consultar:

1. Cliente.
2. Fecha.
3. Método de pago.
4. Estado de pago.
5. Tipo de precio.
6. Total.
7. Costo total.
8. Margen bruto.
9. Rentabilidad.
10. Detalle por producto y lote.
11. Salidas por lote.
12. Pagos registrados, si existen.

### Flujo de uso paso a paso

1. Abra Ventas.
2. Ingrese a la pantalla Ventas.
3. Haga clic en Nuevo.
4. Seleccione el cliente.
5. Seleccione método de pago, si corresponde.
6. Seleccione el estado de pago.
7. Seleccione el tipo de precio.
8. Agregue una línea.
9. Seleccione el producto.
10. Si el producto trabaja por lote, seleccione el lote disponible.
11. Capture la cantidad.
12. Verifique el precio unitario que propone el sistema.
13. Revise el subtotal.
14. Agregue más líneas si hace falta.
15. Revise el total general.
16. Guarde.
17. Abra Ver detalle para revisar la rentabilidad y las salidas por lote.

### Ejemplo práctico

Ejemplo: un cliente compra 20 unidades de café tostado 454 g bajo precio Mayorista. El usuario selecciona el cliente, el tipo de precio Mayorista, el lote disponible del producto y la cantidad. El sistema calcula automáticamente el precio unitario y deja la salida vinculada al lote vendido.

<div style="page-break-after: always;"></div>

## 13. Módulo Contabilidad

## 13.1 Descripción general del propósito

El módulo de Contabilidad ayuda a revisar la carga económica de la operación y registrar conceptos manuales que complementan la trazabilidad de costos.

## 13.2 Funcionalidades principales

1. Consultar el balance resumido.
2. Registrar costos manuales.
3. Consultar costos generados desde producción.
4. Registrar gastos.
5. Registrar pasivos.

## 13.3 Pantalla Balance

### Propósito

Ofrecer una vista resumida de costos, gastos, pasivos y posición neta.

### Opciones y campos de la pantalla

1. Tarjeta de costos activos.
2. Tarjeta de gastos activos.
3. Tarjeta de pasivos activos.
4. Tarjeta de total egresos.
5. Resumen de posición neta.

### Flujo de uso paso a paso

1. Abra Contabilidad.
2. Entre a Balance.
3. Revise los totales activos por categoría.
4. Use el resultado para profundizar luego en Costos, Gastos o Pasivos.

### Ejemplo práctico

Ejemplo: el administrador financiero revisa si la carga de costos y gastos del período operativo amerita revisar con más detalle los costos generados por cosecha, transformación o empaque.

## 13.4 Pantalla Costos

### Propósito

Consultar todos los costos y registrar aquellos que son manuales.

### Funcionalidades principales

1. Ver costos manuales y automáticos.
2. Identificar el tipo de costo.
3. Identificar el módulo fuente.
4. Ver la relación con finca, cultivo o lote.
5. Crear, editar y eliminar costos manuales.

### Opciones y campos de la pantalla

1. Nombre.
2. Tipo de costo.
3. Fuente.
4. Relación.
5. Monto.
6. Fecha.
7. Estado.

### Flujo de uso paso a paso

1. Abra Contabilidad.
2. Ingrese a Costos.
3. Revise si el costo ya existe como automático.
4. Si necesita uno manual, haga clic en Nuevo.
5. Ingrese nombre, monto, fecha y estado.
6. Guarde.

### Ejemplo práctico

Ejemplo: se registra manualmente un costo extraordinario de mantenimiento de equipo que no nació desde Producción, pero que debe quedar contabilizado en el sistema.

## 13.5 Pantalla Gastos

### Propósito

Registrar egresos operativos o administrativos que no son costos directos de producción.

### Opciones y campos de la pantalla

1. Nombre.
2. Monto.
3. Fecha.
4. Estado.

### Flujo de uso paso a paso

1. Abra Contabilidad.
2. Entre a Gastos.
3. Haga clic en Nuevo.
4. Ingrese nombre, monto, fecha y estado.
5. Guarde.

### Ejemplo práctico

Ejemplo: se registra un gasto de transporte administrativo que no se quiere cargar como costo directo de un lote.

## 13.6 Pantalla Pasivos

### Propósito

Registrar obligaciones financieras o compromisos pendientes.

### Opciones y campos de la pantalla

1. Nombre.
2. Monto.
3. Fecha.
4. Estado.

### Flujo de uso paso a paso

1. Abra Contabilidad.
2. Entre a Pasivos.
3. Haga clic en Nuevo.
4. Ingrese nombre, monto, fecha y estado.
5. Guarde.

### Ejemplo práctico

Ejemplo: se registra un pasivo correspondiente a una obligación pendiente con un tercero para que el balance refleje la posición real del negocio.

<div style="page-break-after: always;"></div>

## 14. Proceso integral de trazabilidad del negocio

Esta es la sección más importante del manual. Resume cómo capturar la operación para conservar la trazabilidad completa del negocio.

## 14.1 Preparación de datos maestros

Antes de iniciar la operación diaria, valide que existan estos registros:

1. Fincas.
2. Cultivos.
3. Unidades de medida.
4. Productos.
5. Insumos.
6. Tipos de venta.
7. Clientes.
8. Proveedores, si habrá compras.

### Recomendación para formatos 250 g, 454 g y 500 g

1. Cree o valide los productos de salida de 250 g, 454 g y 500 g desde Inventario, Productos.
2. Complete el formato correspondiente dentro de cada producto.
3. Cargue los precios Normal y Mayorista en cada producto.

## 14.2 Producción del café desde cultivo hasta empaque

### Paso 1. Registrar la estructura productiva

1. Cree la finca.
2. Cree los cultivos asociados.
3. Mantenga activas únicamente las ubicaciones vigentes.

### Paso 2. Registrar operaciones agrícolas

1. Capture cada actividad relevante realizada en el cultivo.
2. Registre insumos, mano de obra y otros costos.
3. Anule el registro si hubo error y vuelva a crearlo correctamente.

### Paso 3. Registrar la cosecha

1. Capture cultivo, producto, estado del café, fecha, cantidad y costo de cosecha.
2. Guarde.
3. Verifique el lote generado.

### Paso 4. Consultar el lote en inventario

1. Vaya a Inventario, Lotes.
2. Localice el código de lote.
3. Abra el detalle.
4. Revise la sección Origen para comprobar que el lote quedó asociado a la cosecha correcta.

### Paso 5. Procesar el lote por baches

1. Si el café debe cambiar de estado, use Transformaciones de café.
2. Registre cada bache como una transformación individual.
3. Si el proceso es tostión, use tipo de transformación Tostión.
4. Registre cantidad de entrada, cantidad de salida y costo del proceso.
5. Guarde y valide el nuevo lote de salida.

### Paso 6. Empacar el café

1. Use Empaques de café con el lote origen ya transformado o tostado.
2. Agregue una línea por cada presentación comercial.
3. Seleccione el producto de salida correspondiente a 250 g, 454 g o 500 g.
4. Ingrese la cantidad de unidades a empacar.
5. Guarde.
6. Revise los lotes de salida generados para cada presentación.

## 14.3 Gestión de inventario

Para mantener un inventario confiable:

1. No registre ventas sin seleccionar el lote correcto cuando el producto se maneje por lote.
2. Use el detalle del lote para verificar origen, transformaciones, empaques y salidas.
3. Mantenga los productos y sus formatos activos según el portafolio vigente.
4. Valide stock y cantidad disponible antes de vender o transformar.

## 14.4 Proceso de tostión por baches

Use esta secuencia como procedimiento estándar:

1. Identifique el lote que entrará a tostión.
2. Abra Producción, Transformaciones de café.
3. Cree un nuevo registro con tipo Tostión.
4. Capture el lote origen.
5. Capture la cantidad de entrada al bache.
6. Capture la cantidad de salida del bache.
7. Registre el costo del proceso.
8. Guarde y valide el código del nuevo lote tostado.
9. Consulte el lote de salida en Inventario, Lotes.
10. Verifique que el origen del nuevo lote apunte al lote tostado de entrada.

## 14.5 Manejo de diferentes formatos

Para trabajar con 250 g, 454 g y 500 g:

1. Cree un producto específico para cada formato si el negocio los vende por separado.
2. En cada producto, complete el formato correcto dentro del formulario.
3. Valide unidad, cantidad y código antes de guardar.
4. En Empaques de café, use una línea por cada producto de salida.
5. Guarde el empaque para que el sistema genere los lotes correspondientes.

### Ejemplo práctico de empaque múltiple

1. Lote tostado origen: LT-20260420-01.
2. Formatos a producir: 250 g, 454 g y 500 g.
3. Cantidades: 100, 60 y 40 unidades respectivamente.
4. Resultado esperado: tres salidas asociadas al mismo empaque, cada una con su lote de salida y su relación con el lote origen.

## 14.6 Aplicación de precios mayorista y detal

El sistema actual aplica precio de esta forma:

1. Los precios se configuran en el producto por tipo de venta.
2. El usuario elige el tipo de precio al crear la venta.
3. El precio unitario de cada línea se calcula automáticamente según ese tipo.

### Procedimiento recomendado

1. Defina al menos los tipos de venta Normal y Mayorista.
2. Configure en cada producto el precio correspondiente a Normal y Mayorista.
3. Al registrar la venta, elija el tipo de precio correcto antes de agregar las líneas.
4. Verifique el precio unitario calculado por el sistema en cada línea.

### Interpretación práctica

1. Normal equivale al precio detal en la operación diaria.
2. Mayorista equivale al precio para clientes al por mayor.
3. Internet puede mantenerse como un tercer canal si la empresa lo usa.

## 14.7 Trazabilidad de la venta paso a paso

Para que la venta quede completamente trazable dentro del alcance actual del sistema, siga esta secuencia:

### Paso 1. Verifique que el producto a vender exista y tenga precio

1. Revise el producto en Inventario, Productos.
2. Confirme que el precio Normal y Mayorista estén cargados.

### Paso 2. Verifique que exista stock y lote disponible

1. Vaya a Inventario, Lotes.
2. Confirme que el lote esté activo y con cantidad disponible.

### Paso 3. Registre o valide el cliente

1. Si el cliente no existe, créelo en Ventas, Clientes.
2. Si la venta no requiere cliente identificado por política interna, defina previamente cómo manejará ese caso la organización.

### Paso 4. Cree la venta

1. Abra Ventas.
2. Haga clic en Nuevo.
3. Seleccione el cliente.
4. Seleccione método de pago, si aplica.
5. Seleccione el estado de pago.
6. Seleccione el tipo de precio, por ejemplo Normal o Mayorista.
7. Agregue las líneas de producto.
8. Seleccione el lote exacto en cada línea cuando corresponda.
9. Capture la cantidad.
10. Revise el precio unitario y el subtotal.
11. Guarde.

### Paso 5. Verifique el detalle de la venta

1. Abra Ver detalle en la venta recién creada.
2. Confirme cliente, fecha, método de pago, tipo de precio y total.
3. Confirme que cada línea tenga el lote correcto.
4. Revise la sección Salidas por lote.

### Paso 6. Verifique la trazabilidad desde el lote

1. Vaya a Inventario, Lotes.
2. Abra el lote vendido.
3. Revise la sección Movimientos del lote.
4. Confirme que aparezca la referencia a la venta y al cliente.

## 14.8 Datos que deben quedar registrados en la venta

Dentro del alcance actual del sistema, una venta bien registrada debe dejar trazados estos datos:

| Dato | Estado actual en el sistema | Dónde se valida |
| --- | --- | --- |
| Registro de la venta | Sí | Pantalla Ventas y detalle de venta |
| Producto vendido | Sí | Líneas de la venta |
| Lote o bache de origen | Sí, cuando el producto maneja lotes | Línea de venta y salidas por lote |
| Tipo de venta | Sí | Campo Tipo de precio |
| Precio aplicado | Sí | Precio unitario y subtotal |
| Cliente | Sí | Encabezado de la venta |
| Método de pago | Sí, si se selecciona | Encabezado y pagos |
| Estado de pago | Sí | Encabezado de la venta |
| Fecha de la venta | Sí | Encabezado y listado |
| Usuario que realiza la venta | No visible como campo operativo en la pantalla de venta actual | Requiere control complementario o futura mejora |
| Descuento | No existe como campo nativo en la venta actual | Requiere control complementario o futura mejora |

## 14.9 Qué hacer con descuento y usuario mientras no existan como campos visibles

Para no inducir a error, este manual no presenta descuento ni usuario capturador como datos operativos visibles de la venta, porque hoy no aparecen como campos nativos en el flujo de ventas revisado.

Mientras esa funcionalidad no exista de forma explícita en la interfaz, la organización debe definir un control complementario interno si necesita:

1. Registrar descuentos comerciales por operación.
2. Exigir que el usuario capturador quede visible dentro del comprobante o consulta de venta.

## 14.10 Escenario práctico 1: producción propia hasta venta detal

1. Crear finca y cultivo.
2. Registrar operaciones agrícolas.
3. Registrar cosecha y generar lote.
4. Registrar tostión por bache en Transformaciones.
5. Registrar empaque en 454 g.
6. Crear o validar el cliente.
7. Registrar la venta con tipo de precio Normal.
8. Abrir el lote final y confirmar origen, transformación, empaque y movimiento de venta.

## 14.11 Escenario práctico 2: compra de café y venta mayorista

1. Crear o validar proveedor.
2. Registrar recepción de café y generar lote de compra.
3. Si aplica, transformar o empacar el producto.
4. Crear o validar cliente mayorista.
5. Registrar la venta con tipo de precio Mayorista.
6. Confirmar en el detalle de la venta el lote usado y el precio aplicado.
7. Abrir el lote vendido para verificar que el origen quedó como Recepción de compra.

<div style="page-break-after: always;"></div>

## 15. Buenas prácticas operativas

1. No deje para después la selección del lote en ventas. Ese dato es la base de la trazabilidad comercial.
2. Configure primero los productos y sus formatos antes de intentar empacar.
3. Registre cada bache de tostión por separado, aunque provenga del mismo lote general.
4. Use la anulación en lugar de editar procesos operativos ya registrados.
5. Revise el detalle del lote al menos una vez después de cada operación crítica: cosecha, transformación, empaque y venta.
6. Mantenga activos solo los catálogos vigentes para reducir errores de captura.

<div style="page-break-after: always;"></div>

## 16. Límites actuales del sistema

Este punto es importante para que el usuario no espere funciones que aún no están visibles en la operación diaria:

1. Las ventas no muestran un campo nativo para descuento.
2. Las ventas no muestran un campo operativo visible para identificar directamente el usuario que registró la venta.
3. Los procesos de cosecha, recepción, transformación, empaque y operación agrícola se manejan principalmente mediante crear y anular, no mediante edición posterior.
4. El balance es una vista de resumen; no reemplaza el análisis detallado por lote o por período contable formal.
5. Los métodos de pago se seleccionan en ventas, pero su administración no aparece como módulo visible dentro del portal revisado.

<div style="page-break-after: always;"></div>

## 17. Glosario breve

| Término | Significado |
| --- | --- |
| Lote origen | Lote que entra a un proceso |
| Lote salida | Lote que resulta de una transformación o empaque |
| Bache | Registro individual de un proceso, por ejemplo una tostión |
| Formato | Tamaño comercial del producto, expresado en gramos u otra unidad |
| Tipo de precio | Regla comercial con la que el sistema calcula el precio de venta |
| Movimiento de lote | Entrada o salida registrada sobre un lote |
| Anulación | Reversión operativa de un registro ya creado |

<div style="page-break-after: always;"></div>

## 18. Cierre

Si el usuario sigue este manual en el orden propuesto, podrá:

1. Registrar la producción o compra del café.
2. Convertir esa materia prima en lotes trazables.
3. Transformar y empacar el producto en diferentes formatos.
4. Aplicar precio mayorista o detal según el caso.
5. Registrar la venta vinculada al lote correcto.
6. Consultar la trazabilidad completa desde el origen hasta la salida final.

La recomendación final es usar siempre el detalle del lote como punto de validación, porque allí converge la mayor parte de la trazabilidad del negocio.