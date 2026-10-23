namespace ProyectoCafe.Application.Abstractions.Security;

public sealed record PermissionCatalogItem(string Modulo, IReadOnlyList<string> Acciones);

/// <summary>
/// Catálogo de permisos expuestos por la API. La implementación concreta vive en
/// la capa API porque se construye a partir de los endpoints registrados.
/// </summary>
public interface IPermissionCatalog
{
    IReadOnlyList<PermissionCatalogItem> GetCatalog();

    IReadOnlySet<string> GetPermissionKeys();
}
