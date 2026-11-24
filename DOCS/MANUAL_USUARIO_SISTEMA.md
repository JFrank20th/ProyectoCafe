<div align="center">

# Manual de Usuario del Sistema ProyectoCafe

## Edición lista para impresión y PDF

**Versión del documento:** 2.1<br/>
**Fecha de emisión:** 24 de noviembre de 2026<br/>
**Formato recomendado:** A4 vertical<br/>
**Margen recomendado:** 18 mm a 20 mm  
**Uso sugerido:** impresión física o exportación a PDF

</div>

| Campo | Valor |
| --- | --- |
| Documento fuente | MANUAL_USUARIO_SISTEMA.md |
| Edición | Impresión / PDF |
| Público objetivo | Usuarios administrativos y operativos |
| Alcance | Administración, Inventario y Ventas |
| Control editorial | Edición alineada con los módulos implementados en la API y el Portal |

## Recomendaciones de exportación

1. Use tamaño A4 en orientación vertical.
2. Configure márgenes entre 18 mm y 20 mm.
3. Si la herramienta de exportación lo permite, active numeración de páginas en encabezado o pie.
4. Mantenga los saltos de página incluidos en el documento.

<div style="page-break-after: always;"></div>

## 1. Propósito del manual

Este manual explica cómo usar ProyectoCafe en su alcance actual: **Administración**, **Inventario de café terminado** y **Ventas**.

El documento está pensado para usuarios no técnicos. Se enfoca en lo que el usuario ve en pantalla, qué debe hacer en cada paso y cómo validar que la información quedó correctamente registrada.

ProyectoCafe no incluye módulos de compras, producción ni contabilidad. El inventario disponible corresponde al café ya terminado, y las ventas se registran contra ese inventario y los precios configurados.

## Índice

1. Propósito del manual.
2. Alcance del sistema.
3. Reglas generales de uso.
4. Conceptos clave.
5. Acceso al sistema.
6. Navegación general.
7. Módulo Administración.
8. Módulo Inventario.
9. Módulo Ventas.
10. Proceso integral de seguridad y accesos.
11. Buenas prácticas administrativas y operativas.
12. Límites actuales del sistema.
13. Glosario breve.
14. Cierre.

## 2. Alcance del sistema

ProyectoCafe está organizado en los siguientes componentes:

| Componente | Propósito principal |
| --- | --- |
| Acceso y autenticación | Ingreso al sistema, creación de cuenta y control de sesión |
| Administración | Gestión de usuarios, roles y permisos |
| Inventario | Lotes de café tostado, existencias, movimientos y catálogos |
| Ventas | Ventas, historial, precios, clientes, pagos y entregas |

### 2.1 Administración

Agrupa las pantallas que controlan quién puede entrar al sistema y qué puede hacer cada usuario:

1. **Usuarios**: consulta de cuentas, activación, desactivación, asignación de roles y eliminación lógica.
2. **Roles**: creación y mantenimiento de perfiles de acceso y asignación de permisos por módulo y acción.

### 2.2 Inventario

Gestiona el café terminado disponible para la venta:

1. **Lotes**: registro y consulta de lotes de café tostado, con fecha de tueste, envasado, vencimiento y atributos del café.
2. **Existencias**: paquetes disponibles por lote, gramaje y presentación, con su peso neto calculado.
3. **Movimientos**: ingresos, salidas y ajustes positivos o negativos sobre las existencias de un lote.
4. **Catálogos**: gramajes, presentaciones (tipos de café), variedades, tipos de tueste y tipos de proceso.

### 2.3 Ventas

Registra la salida de inventario y su cobro:

1. **Ventas**: consulta del inventario disponible y registro de una venta.
2. **Historial de ventas**: consulta de ventas, detalle, movimientos asociados, pagos y estado de entrega.
3. **Precios**: tarifas por gramaje que se usan al registrar una venta.
4. **Clientes**: datos básicos de los clientes, con cliente opcional en la venta.

### 2.4 Acceso compartido

Las pantallas de inicio de sesión y registro son infraestructura compartida. No son un módulo de negocio, pero condicionan el uso de todos los módulos.

La pantalla **Dashboard** se conserva únicamente como pantalla de entrada vacía: no muestra indicadores ni funcionalidad.

## 3. Reglas generales de uso

Antes de trabajar en el sistema, tenga presentes estas reglas:

1. Las opciones visibles en el menú dependen de los permisos del usuario. Si no ve una pantalla, normalmente significa que no tiene autorización para usarla.
2. Toda cuenta nueva queda pendiente de activación hasta que un administrador la habilite.
3. Un usuario activo sin rol asignado no puede acceder a las secciones protegidas del portal.
4. Los permisos se agrupan por módulo y acción. Los módulos vigentes son los de Administración, Inventario y Ventas.
5. El botón **Actualizar** recarga la información visible. Es útil después de activar usuarios, cambiar roles, modificar permisos o registrar movimientos.
6. La eliminación de usuarios, roles y catálogos es lógica: el registro deja de mostrarse en las listas, pero conserva su historial.
7. Las ventas no se eliminan: se **cancelan** o se **anulan** con un motivo, y el inventario se restituye cuando la operación lo permite.
8. Solo los catálogos activos se pueden seleccionar en lotes, movimientos y ventas.
9. Un lote bloqueado no admite movimientos.
10. Los ajustes de inventario requieren una observación.

## 4. Conceptos clave

### 4.1 Usuario

Es una cuenta con correo, nombre, contraseña y estado. Puede estar pendiente de activación, activa, inactiva o eliminada.

### 4.2 Rol

Es un perfil de acceso que agrupa permisos. Se asigna a los usuarios para definir qué pantallas y acciones pueden usar.

### 4.3 Permiso

Es la combinación de un módulo y una acción. Por ejemplo, el permiso **Ver** sobre el módulo **Inventario.Lotes** habilita la consulta de lotes.

### 4.4 Módulo

Es el área funcional sobre la que se aplica un permiso. En el alcance actual existen módulos de Administración, Inventario y Ventas.

### 4.5 Acción

Es la operación permitida sobre un módulo. Las acciones disponibles son **Ver**, **Crear**, **Editar**, **Eliminar**, **Cancelar** y **Anular**, según lo que exponga cada pantalla.

### 4.6 Estado de usuario

1. **Pendiente de activación**: la cuenta se registró, pero aún no fue habilitada.
2. **Activo**: la cuenta puede iniciar sesión y usar los módulos permitidos por sus roles.
3. **Inactivo**: la cuenta existe, pero no puede operar.
4. **Eliminado**: la cuenta fue retirada de las listas activas.

### 4.7 Estado de rol

1. **Activo**: el rol puede asignarse y sus permisos se aplican.
2. **Inactivo**: el rol se conserva, pero no se considera vigente.
3. **Eliminado**: el rol fue retirado de las listas activas.

### 4.8 Lote de café tostado

Es la unidad de producción y trazabilidad del café terminado. Tiene un código único, fechas de tueste, envasado y vencimiento, atributos de café y existencias asociadas.

### 4.9 Existencia

Es la cantidad de paquetes disponibles de un lote para una combinación de gramaje y presentación. El sistema calcula el peso neto a partir del peso unitario y la cantidad de paquetes.

### 4.10 Movimiento

Es un registro que modifica las existencias de un lote: ingreso, salida, ajuste positivo o ajuste negativo. Las ventas generan salidas; una cancelación o anulación genera un ajuste positivo de restitución.

### 4.11 Gramaje, presentación y catálogos

1. **Gramaje**: empaque por peso (por ejemplo, 250 g o 454 g) con su peso unitario.
2. **Presentación**: tipo de café ofrecido (por ejemplo, grano o molido).
3. **Variedad**, **tipo de tueste** y **tipo de proceso**: atributos del café que se asocian al lote.

### 4.12 Venta, pago y entrega

1. **Venta**: operación comercial con número único, cliente opcional, productos y total.
2. **Pago**: abono registrado contra una venta. La venta puede quedar pendiente, con abono parcial o pagada.
3. **Entrega**: estado logístico de la venta: pendiente de preparación, en preparación, lista para entregar o entregada.

## 5. Acceso al sistema

### 5.1 Descripción general

El acceso se realiza desde las pantallas de inicio de sesión y registro. Un usuario puede entrar con sus credenciales o crear una cuenta, la cual queda pendiente de activación por un administrador.

### 5.2 Funcionalidades principales

1. Iniciar sesión con correo y contraseña.
2. Crear una cuenta nueva.
3. Volver a la pantalla de inicio desde el registro.
4. Trabajar con una sesión autenticada para que el sistema muestre solo las opciones permitidas.

### 5.3 Pantallas y opciones

#### Pantalla de inicio de sesión

1. Email.
2. Contraseña.
3. Botón **Ingresar**.
4. Enlace **Crear cuenta**.

#### Pantalla de registro

1. Nombre.
2. Email.
3. Contraseña.
4. Mensaje de que la cuenta quedará pendiente de activación.
5. Botón **Registrarse**.
6. Enlace **Volver a iniciar sesión**.

### 5.4 Flujo paso a paso

1. Abra el portal.
2. Ingrese su correo y contraseña.
3. Haga clic en **Ingresar**.
4. Si su usuario está activo, verá el menú lateral con las opciones habilitadas.
5. Si aún no tiene acceso, use **Crear cuenta** y complete el registro.
6. Espere la activación y la asignación de roles por parte de un administrador.

### 5.5 Ejemplo práctico

Una nueva persona del equipo operativo se registra con su correo corporativo. Luego un administrador la activa y le asigna un rol con permisos de Inventario y Ventas. Al iniciar sesión, verá el menú lateral y solo las pantallas que sus permisos permitan.

<div style="page-break-after: always;"></div>

## 6. Navegación general

### 6.1 Descripción general

La navegación se realiza desde el menú lateral. El sistema agrupa las opciones en estas secciones:

1. **Principal**: Dashboard (pantalla vacía).
2. **Inventario**: Café Tostado.
3. **Ventas**: Ventas.
4. **Administración**: Usuarios y Roles, dentro del grupo Seguridad.

### 6.2 Funcionalidades principales

1. Acceso directo a cada pantalla habilitada.
2. Ocultamiento automático de opciones no autorizadas.
3. Botones comunes para actualizar, crear, editar, asignar permisos o eliminar.

### 6.3 Elementos visibles más comunes

1. Dashboard como pantalla de entrada, sin contenido.
2. Botón **Actualizar** para recargar datos.
3. Botones **Nuevo lote**, **Nueva venta**, **Nuevo rol**, según la pantalla.
4. Listados en tabla con estado y acciones.
5. Chips o etiquetas para mostrar estados.
6. Cuadros de diálogo para crear, editar, asignar permisos, registrar movimientos o confirmar eliminaciones.

### 6.4 Flujo de uso paso a paso

1. Ingrese al sistema.
2. Revise el menú lateral.
3. Entre al módulo que necesite: Inventario, Ventas o Administración.
4. Use **Actualizar** si necesita refrescar la información.
5. Use las acciones disponibles según su nivel de permiso.

### 6.5 Ejemplo práctico

Un administrador abre **Administración > Usuarios**, detecta que hay cuentas activas sin rol y les asigna el rol correspondiente. Un operador abre **Inventario > Café Tostado** y consulta las existencias del lote que va a vender.

<div style="page-break-after: always;"></div>

## 7. Módulo Administración

### 7.1 Descripción general del propósito

Este módulo controla quién puede entrar al sistema y qué puede hacer cada usuario. Está orientado a la seguridad operativa del portal.

### 7.2 Funcionalidades principales

1. Consultar usuarios registrados.
2. Activar o desactivar usuarios.
3. Asignar roles a usuarios.
4. Crear, editar y eliminar roles.
5. Asignar permisos por módulo y acción.
6. Consultar el catálogo de permisos expuesto por la API.

### 7.3 Pantalla Usuarios

#### Propósito

Permite administrar el estado operativo de cada usuario y asignarle roles.

Nota operativa: esta pantalla administra cuentas ya registradas. La creación inicial se realiza desde el flujo de registro del portal; luego el administrador activa la cuenta y asigna roles.

#### Opciones y campos de la pantalla

1. Email.
2. Nombre.
3. Estado del usuario.
4. Fecha de creación.
5. Último login.
6. Roles asignados.
7. Acción **Editar roles**.
8. Acción **Activar** o **Desactivar**.
9. Acción **Eliminar**.

#### Flujo de uso paso a paso

1. Entre a **Administración**.
2. Abra **Usuarios**.
3. Revise el estado de la cuenta.
4. Si el usuario está pendiente, use **Activar**.
5. Si necesita cambiar el acceso, use **Editar roles**.
6. Seleccione uno o varios roles.
7. Guarde los cambios.
8. Si el usuario no debe seguir operando, desactívelo o elimínelo según la política interna.

#### Ejemplo práctico

Una nueva persona se registra en el portal y queda pendiente. El administrador la activa, le asigna el rol **User** o un rol operativo y guarda. A partir de ese momento la cuenta puede iniciar sesión y ver las pantallas que sus permisos habiliten.

### 7.4 Pantalla Roles

#### Propósito

Permite crear perfiles de acceso y definir qué módulos y acciones puede usar cada perfil.

#### Opciones y campos de la pantalla

1. Nombre del rol.
2. Descripción.
3. Estado del rol.
4. Cantidad de permisos asignados.
5. Acción **Editar**.
6. Acción **Permisos**.
7. Acción **Eliminar**.
8. Botón **Nuevo rol**.

#### Gestión de permisos

Desde la opción **Permisos** se seleccionan acciones por módulo. El catálogo se construye automáticamente a partir de los endpoints protegidos que expone la API. En el alcance actual aparecen estos módulos:

| Módulo | Acciones disponibles |
| --- | --- |
| Administracion.Usuarios | Ver, Editar, Eliminar |
| Administracion.Roles | Ver, Crear, Editar, Eliminar |
| Administracion.Permisos | Ver, Editar |
| Inventario.Lotes | Ver, Crear |
| Inventario.Existencias | Ver |
| Inventario.Movimientos | Ver, Crear |
| Inventario.Presentaciones | Ver, Crear, Editar, Eliminar |
| Inventario.TiposCafe | Ver, Crear, Editar, Eliminar |
| Inventario.Variedades | Ver, Crear, Editar, Eliminar |
| Inventario.TiposTueste | Ver, Crear, Editar, Eliminar |
| Inventario.TiposProceso | Ver, Crear, Editar, Eliminar |
| Ventas.Ventas | Ver, Crear, Editar, Cancelar, Anular |
| Ventas.Precios | Ver, Crear, Editar |
| Ventas.Clientes | Ver, Crear, Editar |

La acción **Eliminar** se usa en la interfaz como capacidad para retirar registros de las listas activas. Las acciones **Cancelar** y **Anular** aplican a ventas y exigen un motivo.

#### Flujo de uso paso a paso

1. Abra **Administración**.
2. Ingrese a **Roles**.
3. Haga clic en **Nuevo rol** si necesita crear uno.
4. Capture nombre, descripción y estado.
5. Guarde.
6. Abra la acción **Permisos**.
7. Marque los módulos y acciones necesarios.
8. Guarde nuevamente.

#### Ejemplo práctico

La empresa crea el rol **Operador de inventario**. Ese rol recibe permisos para ver y crear lotes, ver existencias y registrar movimientos, pero no recibe permisos de ventas ni de administración. Con ese perfil, la persona puede operar el inventario sin modificar la configuración de seguridad.

### 7.5 Catálogo de permisos

El catálogo de permisos no se mantiene a mano. Se descubre a partir de las rutas protegidas de la API, de modo que cualquier nueva pantalla que use la política de permisos aparece automáticamente en el diálogo de **Permisos**.

Esto implica dos reglas prácticas:

1. Si una acción no existe en la API, no aparece en el catálogo.
2. Si se retira una funcionalidad, sus permisos dejan de ofrecerse en la administración.

### 7.6 Roles iniciales

El sistema crea y mantiene dos roles base:

1. **Admin**: recibe automáticamente todos los permisos que la API expone.
2. **User**: rol de apoyo para cuentas sin privilegios administrativos.

El rol **Admin** se sincroniza al iniciar la aplicación: si aparece un permiso nuevo, se agrega a este rol sin intervención manual.

<div style="page-break-after: always;"></div>

## 8. Módulo Inventario

### 8.1 Descripción general del propósito

El módulo **Inventario** administra el café tostado terminado: los lotes, sus existencias por gramaje y presentación, los movimientos que las modifican y los catálogos que se usan para clasificarlos.

La pantalla principal está en **Inventario > Café Tostado**. Su contenido se organiza en pestañas; solo aparecen las que el usuario puede ver según sus permisos.

### 8.2 Funcionalidades principales

1. Consultar lotes de café tostado y filtrarlos.
2. Crear lotes con sus líneas de ingreso.
3. Consultar las existencias de un lote.
4. Registrar ingresos, salidas y ajustes.
5. Consultar el historial de movimientos.
6. Mantener los catálogos de gramajes, presentaciones, variedades, tipos de tueste y tipos de proceso.

### 8.3 Pantalla Lotes

#### Propósito

Permite localizar un lote, revisar sus atributos y abrir su detalle de existencias y movimientos.

#### Filtros disponibles

1. Código de lote.
2. Estado del lote: activo, agotado o bloqueado.
3. Fecha de tueste desde.
4. Fecha de tueste hasta.
5. Botón **Limpiar filtros**.

#### Columnas del listado

1. Código de lote.
2. Fecha de tueste.
3. Fecha de vencimiento.
4. Estado.
5. Atributos del lote (variedad, tipo de tueste y tipo de proceso).
6. Total de paquetes.
7. Peso neto total.
8. Acción **Ver detalle**.

#### Flujo de uso paso a paso

1. Abra **Inventario > Café Tostado**.
2. Ubique la pestaña **Lotes**.
3. Use los filtros si necesita encontrar un lote específico.
4. Haga clic en **Ver detalle** sobre el lote.
5. Revise las existencias y los movimientos que aparecen debajo.
6. Use **Actualizar** para recargar la información.

### 8.4 Existencias y movimientos de un lote

Al seleccionar un lote, la pantalla muestra:

1. **Existencias por presentación y tipo**: gramaje, presentación, paquetes, peso unitario, peso neto y fecha de actualización.
2. **Historial de movimientos**: fecha, tipo de movimiento, gramaje, presentación, cantidad, peso neto, usuario, observación y referencia.

Si un lote está **bloqueado**, aparece una advertencia y no se permite registrar movimientos. Si hay existencias asociadas a catálogos inactivos o eliminados, se muestran igualmente para conservar el historial.

#### Registrar un movimiento

1. Seleccione el lote.
2. Haga clic en **Registrar ajuste**.
3. Elija el tipo de movimiento: **Ingreso**, **Salida**, **Ajuste positivo** o **Ajuste negativo**.
4. Seleccione gramaje y presentación.
5. Indique la cantidad de paquetes.
6. Complete la observación. Es obligatoria para los ajustes.
7. Confirme la operación.

Para una salida o un ajuste negativo, el saldo disponible debe cubrir la cantidad indicada. Si no alcanza, el sistema muestra un error y no registra el movimiento.

### 8.5 Crear un lote

1. En la pestaña **Lotes**, haga clic en **Nuevo lote**.
2. Capture el código del lote.
3. Indique la fecha de tueste. No puede ser futura.
4. Indique la fecha de envasado. No puede ser anterior al tueste.
5. Indique la fecha de vencimiento. No puede ser anterior a las fechas anteriores.
6. Seleccione variedad, tipo de tueste y tipo de proceso.
7. Agregue una observación si es necesario.
8. Añada una o más líneas de ingreso: gramaje, presentación y cantidad de paquetes.
9. Revise el total de paquetes y el peso neto estimado.
10. Haga clic en **Crear lote**.

El resultado muestra cada línea creada y, si alguna falló, el motivo. Un lote puede crearse sin líneas de ingreso; en ese caso queda sin existencias hasta que se registre un movimiento.

### 8.6 Catálogos del inventario

La pantalla incluye pestañas para mantener los catálogos que se usan en lotes, movimientos y ventas:

| Pestaña | Qué administra | Acciones típicas |
| --- | --- | --- |
| **Gramajes** | Empaques por peso, con código, nombre, gramos por paquete y estado | Crear, editar, eliminar |
| **Presentaciones** | Tipos de café, con código, nombre y estado | Crear, editar, eliminar |
| **Variedades** | Variedades de café, con código, nombre y estado | Crear, editar, eliminar |
| **Tipos de tueste** | Niveles o estilos de tueste, con código, nombre y estado | Crear, editar, eliminar |
| **Tipos de proceso** | Procesos de café, con código, nombre y estado | Crear, editar, eliminar |

Reglas de los catálogos:

1. Solo los catálogos **activos** se pueden seleccionar en lotes, movimientos y ventas.
2. La eliminación es lógica: el registro deja de mostrarse, pero se conserva para el historial.
3. Un gramaje con existencias históricas no permite cambiar su peso.
4. No se puede inactivar un gramaje o una presentación que tenga existencias disponibles.
5. Las variedades y los tipos de tueste o proceso pueden inactivarse sin afectar las existencias.
6. El código se guarda en mayúsculas y no puede repetirse.
7. Un catálogo asociado a existencias históricas se muestra con una etiqueta de inactivo o eliminado.

### 8.7 Flujo completo de ejemplo

1. Un operador crea el gramaje **250 g** y la presentación **Grano** si no existen.
2. Registra el lote **LOTE-2026-001** con fecha de tueste, variedad, tueste y proceso.
3. Añade una línea de ingreso con 40 paquetes de 250 g de café en grano.
4. El sistema crea la existencia y el movimiento de ingreso.
5. Si se venden 5 paquetes, la venta descuenta la existencia y registra una salida.
6. Si la venta se cancela, el sistema restituye los paquetes con un ajuste positivo.
7. Si el lote se bloquea, ya no admite movimientos ni ventas.

<div style="page-break-after: always;"></div>

## 9. Módulo Ventas

### 9.1 Descripción general del propósito

El módulo **Ventas** registra la salida de café terminado, administra precios y clientes, y da seguimiento a pagos y entregas. La pantalla está en **Ventas** y se organiza en pestañas.

La venta consume existencias reales del inventario. Por eso, para registrar una venta se necesitan permisos de consulta sobre el inventario y los catálogos, además del permiso de creación de ventas.

### 9.2 Funcionalidades principales

1. Consultar el inventario disponible para vender.
2. Registrar una venta con cliente opcional y varias líneas de producto.
3. Consultar el historial de ventas y su detalle.
4. Registrar pagos y consultar el saldo pendiente.
5. Actualizar el estado de entrega.
6. Cancelar o anular una venta con motivo.
7. Mantener precios por gramaje.
8. Mantener clientes.

### 9.3 Pestaña Ventas e inventario disponible

Muestra las existencias disponibles para la venta y permite filtrarlas por lote, gramaje y presentación. Cada fila indica la cantidad de paquetes disponibles.

#### Registrar una venta

1. Abra **Ventas**.
2. En la pestaña **Ventas**, haga clic en **Nueva venta**.
3. Seleccione un cliente o déjelo vacío.
4. Por cada producto, seleccione gramaje, presentación, lote, cantidad y tarifa.
5. Verifique el precio unitario, el subtotal y el total.
6. Use **Agregar producto** si necesita más líneas.
7. Use **Validar venta** si quiere comprobar el formulario sin registrar la operación.
8. Haga clic en **Registrar venta** para confirmar.

El sistema valida que el lote y la tarifa correspondan al gramaje, que la tarifa esté activa y que la cantidad no supere las existencias disponibles. Al registrar la venta, descuenta los paquetes y crea los movimientos de salida.

### 9.4 Pestaña Historial de ventas

Permite consultar las ventas registradas y filtrarlas por:

1. Número de venta.
2. Cliente.
3. Estado de la venta (registrada, cancelada o anulada).
4. Estado de pago (pendiente de pago, abono parcial o pagada).
5. Estado de entrega (pendiente de preparación, en preparación, lista para entregar o entregada).
6. Rango de fechas.

La acción **Ver detalle** abre el detalle de la venta con tres pestañas:

1. **Resumen de venta**: número, fecha, cliente, estado, total, total pagado, saldo pendiente, estado de pago y estado de entrega.
2. **Pagos**: historial de pagos y formulario para registrar un pago.
3. **Gestión**: cambio de estado de entrega, cancelación y anulación.

#### Registrar un pago

1. Abra el detalle de la venta.
2. Entre a la pestaña **Pagos**.
3. Indique el monto, la fecha, el método y una referencia si aplica.
4. Guarde el pago.

El monto debe ser mayor que cero y no puede superar el saldo pendiente. Si la venta ya está pagada, el sistema no permite registrar otro pago.

#### Actualizar la entrega

1. Abra la pestaña **Gestión**.
2. Seleccione el nuevo estado de entrega.
3. Haga clic en **Guardar entrega**.

No se puede cambiar la entrega de una venta cancelada o anulada.

#### Cancelar o anular una venta

1. Abra la pestaña **Gestión**.
2. Elija **Cancelar venta** o **Anular venta**.
3. Escriba el motivo. Es obligatorio y no puede exceder 250 caracteres.
4. Confirme la operación.

Diferencias prácticas:

1. **Cancelar**: se usa cuando la operación comercial no se completa, por ejemplo, si el cliente desiste de la compra.
2. **Anular**: se usa para corregir errores administrativos, registros duplicados o ventas registradas incorrectamente.

En ambos casos el sistema conserva el historial y restituye los paquetes al inventario con un ajuste positivo. No se puede cancelar ni anular una venta que ya está entregada.

### 9.5 Pestaña Precios

Permite configurar las tarifas que se usan en las ventas:

1. Nombre de la tarifa.
2. Gramaje al que aplica.
3. Precio.
4. Estado (activo o inactivo).

Flujo de uso:

1. Abra **Ventas > Precios**.
2. Haga clic en **Nueva tarifa**.
3. Capture nombre, gramaje, precio y estado.
4. Guarde.
5. Use **Editar** para cambiar una tarifa existente.
6. Use **Activar** o **Desactivar** según corresponda.

Una tarifa inactiva no se puede seleccionar en una venta nueva.

### 9.6 Pestaña Clientes

Permite administrar los datos básicos de los clientes:

1. Tipo de identificación.
2. Número de identificación.
3. Nombre o razón social.
4. Teléfono.
5. Correo electrónico.
6. Dirección.
7. Observación.
8. Estado (activo o inactivo).

Flujo de uso:

1. Abra **Ventas > Clientes**.
2. Use el buscador o el filtro de estado para localizar un cliente.
3. Haga clic en **Nuevo cliente** para crear uno.
4. Complete los datos obligatorios: tipo y número de identificación, y nombre.
5. Guarde.
6. Use **Editar** para actualizar los datos.

El número de identificación no puede repetirse. El cliente es opcional en la venta; si no se selecciona, la venta queda registrada como **Sin cliente**.

### 9.7 Flujo completo de ejemplo

1. Se configura la tarifa **Público 250 g** para el gramaje de 250 g.
2. Se registra el cliente **Cafetería Central**.
3. Se consulta el inventario disponible y se elige un lote.
4. Se registra la venta con dos líneas de producto.
5. Se registra un abono parcial; la venta queda con saldo pendiente.
6. Se actualiza la entrega a **En preparación** y luego a **Entregada**.
7. Cuando la venta queda entregada, ya no se puede cancelar ni anular.
8. Si el cliente hubiera desistido antes de la entrega, se cancelaría la venta con un motivo y el inventario se restituiría.

<div style="page-break-after: always;"></div>

## 10. Proceso integral de seguridad y accesos

### 10.1 Secuencia recomendada

1. La persona se registra en el portal.
2. El administrador revisa la cuenta en **Administración > Usuarios**.
3. El administrador activa la cuenta.
4. El administrador asigna uno o varios roles.
5. Si hace falta un perfil nuevo, se crea en **Administración > Roles**.
6. Se asignan los permisos por módulo y acción.
7. Se guarda y se verifica que el usuario vea las pantallas esperadas.

### 10.2 Escenario práctico: habilitar a una persona nueva

1. La persona se registra con su correo corporativo.
2. Queda en estado **Pendiente de activación**.
3. El administrador la activa.
4. Le asigna el rol **User** o un rol personalizado.
5. La persona inicia sesión y confirma que ve las pantallas autorizadas.

### 10.3 Escenario práctico: crear un perfil de solo consulta

1. En **Roles**, crear el rol **Consulta operativa**.
2. Asignar **Ver** en Administración, Inventario y Ventas según lo que deba consultar.
3. Guardar.
4. En **Usuarios**, asignar ese rol a la persona correspondiente.
5. Verificar que la persona pueda consultar sin ver botones de edición o eliminación.

### 10.4 Escenario práctico: separar inventario y ventas

1. Crear el rol **Operador de inventario** con permisos de lotes, existencias y movimientos.
2. Crear el rol **Vendedor** con permisos de ventas, precios y clientes.
3. Asignar cada rol a las personas correspondientes.
4. Verificar que el operador no pueda registrar ventas y que el vendedor no pueda modificar movimientos de inventario.

### 10.5 Escenario práctico: retirar acceso a una persona

1. En **Usuarios**, ubicar la cuenta.
2. Usar **Desactivar** si el acceso debe poder reactivarse.
3. Usar **Eliminar** si la cuenta debe salir de las listas activas.

### 10.6 Verificación de consistencia

Después de un cambio de seguridad, conviene validar:

1. Que el usuario pueda iniciar sesión.
2. Que vea solo las opciones permitidas.
3. Que las acciones sensibles estén bloqueadas cuando corresponda.
4. Que los permisos de inventario y ventas correspondan a la función real de la persona.

<div style="page-break-after: always;"></div>

## 11. Buenas prácticas administrativas y operativas

1. Active las cuentas solo después de validar la identidad de la persona.
2. Asigne el rol de menor privilegio suficiente para la tarea.
3. Use roles por función y evite asignar permisos individuales sin un perfil claro.
4. Desactive las cuentas que dejan la organización antes de eliminarlas, para conservar el historial.
5. Documente internamente qué significa cada rol personalizado.
6. No comparta credenciales entre personas.
7. Mantenga el rol **Admin** reservado para tareas de configuración.
8. Mantenga los catálogos de inventario actualizados antes de registrar lotes o ventas.
9. Verifique el saldo disponible antes de una salida o un ajuste negativo.
10. Use **Cancelar** para operaciones comerciales que no se completan y **Anular** solo para corregir errores administrativos.
11. No cancele ni anule ventas ya entregadas; use el procedimiento administrativo que defina la organización.
12. Revise el historial de movimientos cuando una existencia no coincida con lo esperado.

<div style="page-break-after: always;"></div>

## 12. Límites actuales del sistema

Para no inducir a error, tenga en cuenta estos límites del alcance actual:

1. El sistema no incluye módulos de compras, producción ni contabilidad.
2. El inventario corresponde a café terminado; no gestiona materia prima ni producción.
3. El Dashboard es una pantalla vacía, sin indicadores.
4. Los usuarios se crean desde el registro del portal; la administración no crea cuentas manualmente.
5. La recuperación de contraseña no forma parte de este manual.
6. La eliminación de usuarios, roles y catálogos es lógica, no física.
7. Las ventas no se eliminan: se cancelan o se anulan con motivo.
8. El catálogo de permisos depende de los endpoints realmente expuestos por la API.
9. Las ventas y los movimientos dependen de que el inventario y los catálogos estén configurados.

<div style="page-break-after: always;"></div>

## 13. Glosario breve

| Término | Significado |
| --- | --- |
| Usuario | Cuenta que puede iniciar sesión en el portal |
| Rol | Perfil que agrupa permisos y se asigna a usuarios |
| Permiso | Combinación de módulo y acción autorizada |
| Módulo | Área funcional sobre la que se aplica un permiso |
| Acción | Operación permitida: Ver, Crear, Editar, Eliminar, Cancelar o Anular |
| Activación | Cambio de una cuenta pendiente a estado activo |
| Eliminación lógica | Retiro de un registro de las listas activas sin borrar su historial |
| Catálogo de permisos | Lista de módulos y acciones descubierta desde la API |
| Lote | Conjunto de café tostado con código y fechas propias |
| Existencia | Paquetes disponibles de un lote para un gramaje y presentación |
| Movimiento | Ingreso, salida o ajuste que modifica una existencia |
| Gramaje | Empaque por peso, con su peso unitario |
| Presentación | Tipo de café ofrecido, por ejemplo grano o molido |
| Venta | Operación comercial que descuenta inventario |
| Pago | Abono registrado contra una venta |
| Entrega | Estado logístico de una venta |
| Cancelación | Cierre de una venta por una operación comercial no completada |
| Anulación | Corrección administrativa de una venta registrada incorrectamente |

<div style="page-break-after: always;"></div>

## 14. Cierre

Si el usuario sigue este manual en el orden propuesto, podrá:

1. Ingresar al portal y entender el estado de su cuenta.
2. Activar cuentas y asignar roles.
3. Crear roles y ajustar permisos por módulo y acción.
4. Gestionar lotes, existencias, movimientos y catálogos de inventario.
5. Registrar ventas, precios, clientes, pagos y entregas.
6. Verificar que cada usuario vea las pantallas autorizadas.

La recomendación final es revisar la asignación de roles y permisos después de cada cambio de seguridad, y validar el inventario después de registrar ventas, cancelaciones o anulaciones.
