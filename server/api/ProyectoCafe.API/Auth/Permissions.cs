using ProyectoCafe.Application.Security;

namespace ProyectoCafe.API.Auth;

public static class Permissions
{
    public const string PolicyPrefix = PermissionKey.PolicyPrefix;

    public static class Actions
    {
        public const string Ver = "Ver";
        public const string Crear = "Crear";
        public const string Editar = "Editar";
        public const string Eliminar = "Eliminar";
        public const string Cancelar = "Cancelar";
        public const string Anular = "Anular";

        public static readonly string[] Crud = [Ver, Crear, Editar, Eliminar];
    }

    public static class Modulos
    {
        public const string AdministracionUsuarios = "Administracion.Usuarios";
        public const string AdministracionRoles = "Administracion.Roles";
        public const string AdministracionPermisos = "Administracion.Permisos";
        public const string InventarioLotes = "Inventario.Lotes";
        public const string InventarioExistencias = "Inventario.Existencias";
        public const string InventarioMovimientos = "Inventario.Movimientos";
        public const string InventarioPresentaciones = "Inventario.Presentaciones";
        public const string InventarioTiposCafe = "Inventario.TiposCafe";
        public const string InventarioVariedades = "Inventario.Variedades";
        public const string InventarioTiposTueste = "Inventario.TiposTueste";
        public const string InventarioTiposProceso = "Inventario.TiposProceso";
        public const string VentasVentas = "Ventas.Ventas";
        public const string VentasPrecios = "Ventas.Precios";
        public const string VentasClientes = "Ventas.Clientes";

        public static readonly string[] All = [
            AdministracionUsuarios,
            AdministracionRoles,
            AdministracionPermisos,
            InventarioLotes,
            InventarioExistencias,
            InventarioMovimientos,
            InventarioPresentaciones,
            InventarioTiposCafe,
            InventarioVariedades,
            InventarioTiposTueste,
            InventarioTiposProceso,
            VentasVentas,
            VentasPrecios,
            VentasClientes,
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
