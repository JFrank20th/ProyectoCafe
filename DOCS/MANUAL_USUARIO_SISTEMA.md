<div align="center">

# Manual de Usuario del Sistema ProyectoCafe

## Edición lista para impresión y PDF

**Versión del documento:** 2.0  
**Fecha de emisión:** 22 de octubre de 2026  
**Formato recomendado:** A4 vertical  
**Margen recomendado:** 18 mm a 20 mm  
**Uso sugerido:** impresión física o exportación a PDF

</div>

| Campo | Valor |
| --- | --- |
| Documento fuente | MANUAL_USUARIO_SISTEMA.md |
| Edición | Impresión / PDF |
| Público objetivo | Usuarios administrativos y de consulta |
| Alcance | Dashboard y Administración |
| Control editorial | Edición enfocada exclusivamente en los módulos vigentes del sistema |

## Recomendaciones de exportación

1. Use tamaño A4 en orientación vertical.
2. Configure márgenes entre 18 mm y 20 mm.
3. Si la herramienta de exportación lo permite, active numeración de páginas en encabezado o pie.
4. Mantenga los saltos de página incluidos en el documento.

<div style="page-break-after: always;"></div>

## 1. Propósito del manual

Este manual explica cómo usar ProyectoCafe en su alcance actual: el **Dashboard** y el módulo de **Administración**.

El documento está pensado para usuarios no técnicos. Se enfoca en lo que el usuario ve en pantalla, qué debe hacer en cada paso y cómo validar que la información quedó correctamente registrada.

ProyectoCafe ya no incluye módulos operativos de compras, producción, inventario, ventas ni contabilidad. Por eso, este manual no describe esas pantallas: toda la gestión disponible gira alrededor de las cuentas, los roles y los permisos que dan acceso al portal.

## Índice

1. Propósito del manual.
2. Alcance del sistema.
3. Reglas generales de uso.
4. Conceptos clave.
5. Acceso al sistema.
6. Navegación general.
7. Módulo Dashboard.
8. Módulo Administración.
9. Proceso integral de seguridad y accesos.
10. Buenas prácticas administrativas.
11. Límites actuales del sistema.
12. Glosario breve.
13. Cierre.

## 2. Alcance del sistema

ProyectoCafe está organizado en los siguientes componentes:

| Componente | Propósito principal |
| --- | --- |
| Acceso y autenticación | Ingreso al sistema, creación de cuenta y control de sesión |
| Dashboard | Resumen de usuarios, roles, permisos y cobertura de acceso |
| Administración | Gestión de usuarios, roles y permisos |

### 2.1 Dashboard

Es la pantalla de entrada después de iniciar sesión. Presenta indicadores de administración y seguridad: altas de usuarios, estado de las cuentas, roles vigentes, permisos registrados y cobertura de acceso.

### 2.2 Administración

Agrupa las pantallas que controlan quién puede entrar al sistema y qué puede hacer cada usuario:

1. **Usuarios**: consulta de cuentas, activación, desactivación, asignación de roles y eliminación lógica.
2. **Roles**: creación y mantenimiento de perfiles de acceso y asignación de permisos por módulo y acción.

### 2.3 Acceso compartido

Las pantallas de inicio de sesión, registro y confirmación de correo son infraestructura compartida. No son un módulo de negocio, pero condicionan el uso del Dashboard y de Administración.

## 3. Reglas generales de uso

Antes de trabajar en el sistema, tenga presentes estas reglas:

1. Las opciones visibles en el menú dependen de los permisos del usuario. Si no ve una pantalla, normalmente significa que no tiene autorización para usarla.
2. Toda cuenta nueva queda pendiente de activación hasta que un administrador la habilite.
3. Un usuario activo sin rol asignado no puede acceder a las secciones protegidas del portal.
4. Los permisos se agrupan por módulo y acción. En el alcance actual solo existen los módulos de Administración.
5. El botón **Actualizar** recarga la información visible. Es útil después de activar usuarios, cambiar roles o modificar permisos.
6. La eliminación de usuarios y roles es lógica: el registro deja de mostrarse en las listas, pero conserva su historial.

## 4. Conceptos clave

### 4.1 Usuario

Es una cuenta con correo, nombre, contraseña y estado. Puede estar pendiente de activación, activa, inactiva o eliminada.

### 4.2 Rol

Es un perfil de acceso que agrupa permisos. Se asigna a los usuarios para definir qué pantallas y acciones pueden usar.

### 4.3 Permiso

Es la combinación de un módulo y una acción. Por ejemplo, el permiso **Ver** sobre el módulo **Administracion.Usuarios** habilita la consulta de usuarios.

### 4.4 Módulo

Es el área funcional sobre la que se aplica un permiso. En el alcance actual existen tres módulos de Administración:

1. `Administracion.Usuarios`.
2. `Administracion.Roles`.
3. `Administracion.Permisos`.

### 4.5 Acción

Es la operación permitida sobre un módulo. Las acciones disponibles son **Ver**, **Crear**, **Editar** y **Eliminar**, según lo que exponga cada pantalla.

### 4.6 Cobertura de acceso

Es el porcentaje de usuarios activos que tienen al menos un rol vigente asignado. Una cobertura del 100 % indica que todos los usuarios activos cuentan con un perfil de acceso.

### 4.7 Periodo

Es la ventana de tiempo usada en el Dashboard. En el alcance actual, el periodo filtra únicamente las **altas de usuarios**; los demás indicadores reflejan el estado actual del sistema.

### 4.8 Estado de usuario

1. **Pendiente de activación**: la cuenta se registró, pero aún no fue habilitada.
2. **Activo**: la cuenta puede iniciar sesión y usar los módulos permitidos por sus roles.
3. **Inactivo**: la cuenta existe, pero no puede operar.
4. **Eliminado**: la cuenta fue retirada de las listas activas.

### 4.9 Estado de rol

1. **Activo**: el rol puede asignarse y sus permisos se aplican.
2. **Inactivo**: el rol se conserva, pero no se considera vigente.
3. **Eliminado**: el rol fue retirado de las listas activas.

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

Si la organización tiene habilitado el flujo de confirmación de correo, el usuario debe completar ese paso según la instrucción recibida antes o después de la activación administrativa.

### 5.4 Flujo paso a paso

1. Abra el portal.
2. Ingrese su correo y contraseña.
3. Haga clic en **Ingresar**.
4. Si su usuario está activo, verá el Dashboard y el menú lateral.
5. Si aún no tiene acceso, use **Crear cuenta** y complete el registro.
6. Espere la activación y la asignación de roles por parte de un administrador.

### 5.5 Ejemplo práctico

Una nueva persona del equipo administrativo se registra con su correo corporativo. Luego un administrador la activa y le asigna el rol adecuado. Al iniciar sesión, verá el Dashboard y las pantallas de Administración que sus permisos permitan.

<div style="page-break-after: always;"></div>

## 6. Navegación general

### 6.1 Descripción general

La navegación se realiza desde el menú lateral. El sistema agrupa las opciones en dos secciones:

1. **Principal**: Dashboard.
2. **Administración**: Usuarios y Roles, dentro del grupo Seguridad.

### 6.2 Funcionalidades principales

1. Acceso directo a cada pantalla habilitada.
2. Ocultamiento automático de opciones no autorizadas.
3. Botones comunes para actualizar, crear, editar, asignar permisos o eliminar.

### 6.3 Elementos visibles más comunes

1. Dashboard como pantalla principal.
2. Botón **Actualizar** para recargar datos.
3. Botón **Nuevo rol** para crear perfiles de acceso.
4. Listados en tabla con estado y acciones.
5. Chips o etiquetas para mostrar estados.
6. Cuadros de diálogo para crear, editar, asignar permisos o confirmar eliminaciones.

### 6.4 Flujo de uso paso a paso

1. Ingrese al sistema.
2. Revise el menú lateral.
3. Entre al Dashboard o a Administración.
4. Use **Actualizar** si necesita refrescar la información.
5. En Administración, use las acciones disponibles según su nivel de permiso.

### 6.5 Ejemplo práctico

Un administrador entra al Dashboard, detecta que hay cuentas activas sin rol y luego abre **Administración > Usuarios** para asignarles el rol correspondiente.

<div style="page-break-after: always;"></div>

## 7. Módulo Dashboard

### 7.1 Descripción general del propósito

El Dashboard es la portada del sistema. Resume el estado de la seguridad de acceso al portal a partir de cuatro indicadores:

1. Nuevos usuarios.
2. Roles activos.
3. Permisos vigentes.
4. Cobertura de acceso.

### 7.2 Funcionalidades principales

1. Consultar indicadores clave de administración.
2. Cambiar el periodo usado para contar las altas de usuarios.
3. Usar un rango personalizado de fechas.
4. Comparar las altas del periodo con el periodo anterior equivalente.
5. Interpretar la información con ayudas visuales de color.

### 7.3 Explicación de opciones y pantalla

La pantalla incluye:

1. Encabezado principal con la descripción del panel de administración.
2. Selector de periodo con opciones de 7 días, 30 días, 90 días, 12 meses, histórico y rango personalizado.
3. Campos **Desde** y **Hasta** cuando se usa rango personalizado.
4. Botón **Aplicar rango**.
5. Cuatro tarjetas KPI.
6. Bloque **Qué vigilar**, con los frentes de usuarios, roles y permisos.
7. Guía rápida de semántica visual.
8. Bloque de estados de acceso con chips y alertas.

### 7.4 Interpretación de los indicadores

| Indicador | Qué muestra | Cómo se calcula |
| --- | --- | --- |
| Nuevos usuarios | Altas registradas en el periodo | Usuarios no eliminados creados dentro de la ventana seleccionada |
| Roles activos | Roles disponibles para asignar | Roles no eliminados con estado activo |
| Permisos vigentes | Permisos efectivos registrados | Combinaciones distintas de módulo y acción asignadas a roles |
| Cobertura de acceso | Porcentaje de usuarios activos con rol | Usuarios activos con al menos un rol vigente ÷ usuarios activos |

Cada tarjeta muestra además un dato de apoyo:

1. **Nuevos usuarios**: cuántos usuarios activos hay sobre el total.
2. **Roles activos**: cuántas asignaciones existen sobre el total de roles.
3. **Permisos vigentes**: cuántos módulos y asignaciones se registran.
4. **Cobertura de acceso**: cuántos usuarios activos tienen rol.

### 7.5 Comparación contra el periodo anterior

La tarjeta **Nuevos usuarios** muestra la diferencia contra el periodo inmediatamente anterior de la misma duración. Por ejemplo, si se consultan 30 días, la comparación usa los 30 días previos.

Las demás tarjetas representan el estado actual del sistema, por lo que muestran la etiqueta **Estado actual** en lugar de una variación temporal.

Si se elige **Histórico**, no existe un periodo anterior equivalente y la comparación se muestra como **Sin comparativo**.

### 7.6 Flujo de uso paso a paso

1. Ingrese al Dashboard.
2. Seleccione el periodo de altas deseado.
3. Si necesita un rango manual, elija **Rango personalizado**.
4. Ingrese fecha inicial y fecha final.
5. Haga clic en **Aplicar rango**.
6. Revise las cuatro tarjetas KPI y la comparación de nuevos usuarios.
7. Use la cobertura de acceso para detectar cuentas activas sin rol.
8. Diríjase a Administración para corregir lo que corresponda.

### 7.7 Ejemplo práctico

Un administrador revisa el Dashboard en un periodo de 30 días y observa:

1. Cinco usuarios nuevos frente a tres del periodo anterior.
2. Cuatro roles activos.
3. Siete permisos vigentes en tres módulos.
4. Una cobertura de acceso del 85,7 %, porque uno de los siete usuarios activos no tiene rol.

Con esa lectura, entra a **Administración > Usuarios** y asigna un rol al usuario pendiente de cobertura.

<div style="page-break-after: always;"></div>

## 8. Módulo Administración

### 8.1 Descripción general del propósito

Este módulo controla quién puede entrar al sistema y qué puede hacer cada usuario. Está orientado a la seguridad operativa del portal.

### 8.2 Funcionalidades principales

1. Consultar usuarios registrados.
2. Activar o desactivar usuarios.
3. Asignar roles a usuarios.
4. Crear, editar y eliminar roles.
5. Asignar permisos por módulo y acción.
6. Consultar el catálogo de permisos expuesto por la API.

### 8.3 Pantalla Usuarios

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

Una nueva persona se registra en el portal y queda pendiente. El administrador la activa, le asigna el rol **User** y guarda. A partir de ese momento la cuenta puede iniciar sesión y ver las pantallas que sus permisos habiliten.

### 8.4 Pantalla Roles

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

Desde la opción **Permisos** se seleccionan acciones por módulo. El catálogo se construye automáticamente a partir de los endpoints protegidos que expone la API. En el alcance actual aparecen únicamente:

| Módulo | Acciones disponibles |
| --- | --- |
| Administracion.Usuarios | Ver, Editar, Eliminar |
| Administracion.Roles | Ver, Crear, Editar, Eliminar |
| Administracion.Permisos | Ver, Editar |

La acción **Eliminar** se usa en la interfaz como capacidad para retirar registros de las listas activas.

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

La empresa crea el rol **Soporte administrativo**. Ese rol recibe permisos para ver usuarios, ver roles y ver permisos, pero no recibe permisos para editar ni eliminar. Con ese perfil, la persona puede consultar la administración sin modificar la configuración de seguridad.

### 8.5 Catálogo de permisos

El catálogo de permisos no se mantiene a mano. Se descubre a partir de las rutas protegidas de la API, de modo que cualquier nueva pantalla que use la política de permisos aparece automáticamente en el diálogo de **Permisos**.

Esto implica dos reglas prácticas:

1. Si una acción no existe en la API, no aparece en el catálogo.
2. Si se retira una funcionalidad, sus permisos dejan de ofrecerse en la administración.

### 8.6 Roles iniciales

El sistema crea y mantiene dos roles base:

1. **Admin**: recibe automáticamente todos los permisos que la API expone.
2. **User**: rol de apoyo para cuentas sin privilegios administrativos.

El rol **Admin** se sincroniza al iniciar la aplicación: si aparece un permiso nuevo, se agrega a este rol sin intervención manual.

<div style="page-break-after: always;"></div>

## 9. Proceso integral de seguridad y accesos

### 9.1 Secuencia recomendada

1. La persona se registra en el portal.
2. El administrador revisa la cuenta en **Administración > Usuarios**.
3. El administrador activa la cuenta.
4. El administrador asigna uno o varios roles.
5. Si hace falta un perfil nuevo, se crea en **Administración > Roles**.
6. Se asignan los permisos por módulo y acción.
7. Se guarda y se verifica que el usuario vea las pantallas esperadas.
8. Se revisa el Dashboard para confirmar la cobertura de acceso.

### 9.2 Escenario práctico: habilitar a una persona nueva

1. La persona se registra con su correo corporativo.
2. Queda en estado **Pendiente de activación**.
3. El administrador la activa.
4. Le asigna el rol **User** o un rol personalizado.
5. La persona inicia sesión y confirma que ve el Dashboard y las pantallas autorizadas.

### 9.3 Escenario práctico: crear un perfil de solo consulta

1. En **Roles**, crear el rol **Consulta administrativa**.
2. Asignar **Ver** en Usuarios, Roles y Permisos.
3. Guardar.
4. En **Usuarios**, asignar ese rol a la persona correspondiente.
5. Verificar que la persona pueda consultar sin ver botones de edición o eliminación.

### 9.4 Escenario práctico: retirar acceso a una persona

1. En **Usuarios**, ubicar la cuenta.
2. Usar **Desactivar** si el acceso debe poder reactivarse.
3. Usar **Eliminar** si la cuenta debe salir de las listas activas.
4. Revisar el Dashboard: la cobertura de acceso se recalcula automáticamente.

### 9.5 Verificación de consistencia

Después de un cambio de seguridad, conviene validar:

1. Que el usuario pueda iniciar sesión.
2. Que vea solo las opciones permitidas.
3. Que las acciones sensibles estén bloqueadas cuando corresponda.
4. Que la cobertura de acceso del Dashboard siga siendo la esperada.

<div style="page-break-after: always;"></div>

## 10. Buenas prácticas administrativas

1. Active las cuentas solo después de validar la identidad de la persona.
2. Asigne el rol de menor privilegio suficiente para la tarea.
3. Use roles por función y evite asignar permisos individuales sin un perfil claro.
4. Revise el Dashboard al menos una vez por semana para detectar cuentas activas sin rol.
5. Desactive las cuentas que dejan la organización antes de eliminarlas, para conservar el historial.
6. Documente internamente qué significa cada rol personalizado.
7. No comparta credenciales entre personas.
8. Mantenga el rol **Admin** reservado para tareas de configuración.

<div style="page-break-after: always;"></div>

## 11. Límites actuales del sistema

Para no inducir a error, tenga en cuenta estos límites del alcance actual:

1. El sistema no incluye módulos operativos de compras, producción, inventario, ventas ni contabilidad.
2. Los usuarios se crean desde el registro del portal; la administración no crea cuentas manualmente.
3. La recuperación de contraseña no forma parte de este manual.
4. La eliminación de usuarios y roles es lógica, no física.
5. El Dashboard no muestra datos de negocio; solo indicadores de administración y seguridad.
6. El periodo del Dashboard solo filtra las altas de usuarios.
7. El catálogo de permisos depende de los endpoints realmente expuestos por la API.

<div style="page-break-after: always;"></div>

## 12. Glosario breve

| Término | Significado |
| --- | --- |
| Usuario | Cuenta que puede iniciar sesión en el portal |
| Rol | Perfil que agrupa permisos y se asigna a usuarios |
| Permiso | Combinación de módulo y acción autorizada |
| Módulo | Área funcional sobre la que se aplica un permiso |
| Acción | Operación permitida: Ver, Crear, Editar o Eliminar |
| Cobertura de acceso | Porcentaje de usuarios activos con al menos un rol |
| Activación | Cambio de una cuenta pendiente a estado activo |
| Eliminación lógica | Retiro de un registro de las listas activas sin borrar su historial |
| Periodo | Ventana de tiempo usada para contar altas de usuarios |
| Catálogo de permisos | Lista de módulos y acciones descubierta desde la API |

<div style="page-break-after: always;"></div>

## 13. Cierre

Si el usuario sigue este manual en el orden propuesto, podrá:

1. Ingresar al portal y entender el estado de su cuenta.
2. Interpretar los indicadores del Dashboard.
3. Activar cuentas y asignar roles.
4. Crear roles y ajustar permisos por módulo y acción.
5. Verificar que la cobertura de acceso sea la esperada.

La recomendación final es revisar el Dashboard después de cada cambio de seguridad: allí se refleja de inmediato el efecto de las altas, los roles y los permisos sobre el acceso al sistema.
