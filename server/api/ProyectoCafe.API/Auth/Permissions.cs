namespace ProyectoCafe.API.Auth;

public static class Permissions
{
    public const string PolicyPrefix = "perm:";

    public static class Actions
    {
        public const string Ver = "Ver";
        public const string Crear = "Crear";
        public const string Editar = "Editar";
        public const string Eliminar = "Eliminar";

        public static readonly string[] Crud = [Ver, Crear, Editar, Eliminar];
    }

    public static class Modulos
    {
        public const string InventarioUnidadesMedida = "Inventario.UnidadesMedida";
        public const string InventarioProductos = "Inventario.Productos";
        public const string InventarioInsumos = "Inventario.Insumos";
        public const string InventarioLotes = "Inventario.Lotes";
        public const string InventarioProcesos = "Inventario.Procesos";

        public const string ProduccionFincas = "Produccion.Fincas";
        public const string ProduccionCultivos = "Produccion.Cultivos";
        public const string ProduccionParcelasLegacy = "Produccion.Parcelas";
        public const string ProduccionOperacionesAgricolas = "Produccion.OperacionesAgricolas";

        public const string ComprasProveedores = "Compras.Proveedores";
        public const string ComprasRecepcionesCafe = "Compras.RecepcionesCafe";

        public const string VentasClientes = "Ventas.Clientes";
        public const string VentasTiposVenta = "Ventas.TiposVenta";
        public const string VentasVentas = "Ventas.Ventas";
        public const string VentasMetodosPago = "Ventas.MetodosPago";

        public const string ContabilidadCostos = "Contabilidad.Costos";
        public const string ContabilidadGastos = "Contabilidad.Gastos";
        public const string ContabilidadPasivos = "Contabilidad.Pasivos";

        public const string AdministracionUsuarios = "Administracion.Usuarios";
        public const string AdministracionRoles = "Administracion.Roles";
        public const string AdministracionPermisos = "Administracion.Permisos";

        public static readonly string[] All = [
            InventarioUnidadesMedida,
            InventarioProductos,
            InventarioInsumos,
            InventarioLotes,
            InventarioProcesos,
            ProduccionFincas,
            ProduccionCultivos,
            ProduccionOperacionesAgricolas,
            ComprasProveedores,
            ComprasRecepcionesCafe,
            VentasClientes,
            VentasTiposVenta,
            VentasVentas,
            VentasMetodosPago,
            ContabilidadCostos,
            ContabilidadGastos,
            ContabilidadPasivos,
            AdministracionUsuarios,
            AdministracionRoles,
            AdministracionPermisos,
        ];
    }

    public static string Policy(string modulo, string accion)
    {
        return $"{PolicyPrefix}{modulo}:{accion}";
    }

    public static IEnumerable<(string Modulo, string Accion)> AllCrudPermissions()
    {
        foreach (var modulo in Modulos.All)
        {
            foreach (var accion in Actions.Crud)
            {
                yield return (modulo, accion);
            }
        }
    }
}
