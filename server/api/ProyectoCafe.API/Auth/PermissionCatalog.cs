using Microsoft.AspNetCore.Authorization;
using ProyectoCafe.Application.Abstractions.Security;
using ProyectoCafe.Application.Security;

namespace ProyectoCafe.API.Auth;

/// <summary>
/// Construye el catálogo de permisos a partir de los endpoints protegidos registrados en la API.
/// De esta forma, cualquier pantalla o funcionalidad nueva que use
/// <c>RequireAuthorization(Permissions.Policy(...))</c> aparece automáticamente en el módulo de Roles,
/// sin necesidad de mantener una lista manual de módulos y acciones.
/// </summary>
public sealed class PermissionCatalogService : IPermissionCatalog
{
    private readonly object _sync = new();
    private IReadOnlyCollection<EndpointDataSource> _dataSources = Array.Empty<EndpointDataSource>();
    private IReadOnlyList<PermissionCatalogItem>? _catalog;

    /// <summary>
    /// Asocia las fuentes de endpoints de la aplicación. Debe llamarse después de registrar todos los
    /// endpoints y antes de que la aplicación empiece a atender solicitudes.
    /// </summary>
    public void Attach(IEnumerable<EndpointDataSource> dataSources)
    {
        ArgumentNullException.ThrowIfNull(dataSources);

        lock (_sync)
        {
            _dataSources = dataSources as IReadOnlyCollection<EndpointDataSource> ?? dataSources.ToArray();
            _catalog = null;
        }
    }

    /// <summary>
    /// Devuelve los módulos y acciones disponibles. Se recalcula mientras el catálogo esté vacío
    /// (por ejemplo, si aún no se han registrado los endpoints) y se conserva en caché después.
    /// </summary>
    public IReadOnlyList<PermissionCatalogItem> GetCatalog()
    {
        lock (_sync)
        {
            if (_catalog is { Count: > 0 })
            {
                return _catalog;
            }

            _catalog = Build(_dataSources);
            return _catalog;
        }
    }

    /// <summary>
    /// Claves <c>Modulo:Accion</c> válidas según las funcionalidades realmente expuestas por la API.
    /// </summary>
    public IReadOnlySet<string> GetPermissionKeys()
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in GetCatalog())
        {
            foreach (var accion in item.Acciones)
            {
                keys.Add(BuildKey(item.Modulo, accion));
            }
        }

        return keys;
    }

    public static string BuildKey(string? modulo, string? accion) =>
        PermissionKey.Build(modulo, accion);

    private static IReadOnlyList<PermissionCatalogItem> Build(
        IReadOnlyCollection<EndpointDataSource> dataSources)
    {
        var modules = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var dataSource in dataSources)
        {
            foreach (var endpoint in dataSource.Endpoints)
            {
                foreach (var metadata in endpoint.Metadata)
                {
                    if (metadata is not IAuthorizeData authorize)
                    {
                        continue;
                    }

                    if (!TryParsePolicy(authorize.Policy, out var modulo, out var accion))
                    {
                        continue;
                    }

                    if (!modules.TryGetValue(modulo, out var acciones))
                    {
                        acciones = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        modules[modulo] = acciones;
                    }

                    acciones.Add(accion);
                }
            }
        }

        return modules
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Select(x => new PermissionCatalogItem(x.Key, OrderActions(x.Value)))
            .ToArray();
    }

    private static IReadOnlyList<string> OrderActions(IEnumerable<string> acciones)
    {
        return acciones
            .OrderBy(ActionOrder)
            .ThenBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static int ActionOrder(string accion)
    {
        for (var i = 0; i < Permissions.Actions.Crud.Length; i++)
        {
            if (string.Equals(Permissions.Actions.Crud[i], accion, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return int.MaxValue;
    }

    private static bool TryParsePolicy(string? policy, out string modulo, out string accion)
    {
        modulo = string.Empty;
        accion = string.Empty;

        if (string.IsNullOrWhiteSpace(policy) ||
            !policy.StartsWith(Permissions.PolicyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var body = policy[Permissions.PolicyPrefix.Length..];
        var separator = body.IndexOf(':');
        if (separator <= 0 || separator >= body.Length - 1)
        {
            return false;
        }

        modulo = body[..separator].Trim();
        accion = body[(separator + 1)..].Trim();

        return modulo.Length > 0 && accion.Length > 0;
    }
}
