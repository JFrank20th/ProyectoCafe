namespace ProyectoCafe.Application.Security;

public static class PermissionKey
{
    public const string PolicyPrefix = "perm:";

    public static string Build(string? modulo, string? accion) => $"{modulo}:{accion}";
}
