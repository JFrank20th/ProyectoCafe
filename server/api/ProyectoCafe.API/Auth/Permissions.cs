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

        public static readonly string[] Crud = [Ver, Crear, Editar, Eliminar];
    }

    public static class Modulos
    {
        public const string AdministracionUsuarios = "Administracion.Usuarios";
        public const string AdministracionRoles = "Administracion.Roles";
        public const string AdministracionPermisos = "Administracion.Permisos";

        public static readonly string[] All = [
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
