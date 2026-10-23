using System.Net.Http.Json;
using ProyectoCafe.API.Auth;
using ProyectoCafe.Tests.Infrastructure;

namespace ProyectoCafe.Tests;

public sealed class PermissionCatalogEndpointTests
{
    [Fact]
    public async Task GetCatalog_ReflectsOnlyActionsExposedByEndpoints()
    {
        await using var factory = new ProyectoCafeApiFactory();
        await factory.SeedUserWithPermissionsAsync(
            "catalog-viewer@test.local",
            (Permissions.Modulos.AdministracionPermisos, Permissions.Actions.Ver));
        using var client = await factory.CreateAuthenticatedClientAsync("catalog-viewer@test.local");

        var response = await client.GetAsync("/api/v1/admin/permissions/catalog");
        response.EnsureSuccessStatusCode();

        var catalog = await response.Content.ReadFromJsonAsync<CatalogItem[]>();
        Assert.NotNull(catalog);

        var byModule = catalog!
            .ToDictionary(item => item.Modulo, item => item.Acciones, StringComparer.OrdinalIgnoreCase);

        // El alcance actual del proyecto solo expone los módulos de Administración.
        Assert.Equal(3, catalog!.Length);
        Assert.Equal(
            new[]
            {
                "Administracion.Permisos",
                "Administracion.Roles",
                "Administracion.Usuarios",
            },
            catalog.Select(item => item.Modulo).ToArray());

        // Usuarios se activa/desactiva y se le asignan roles, pero no se crea desde administración.
        Assert.Equal(new[] { "Ver", "Editar", "Eliminar" }, byModule["Administracion.Usuarios"]);

        // Roles permite el ciclo CRUD completo.
        Assert.Equal(new[] { "Ver", "Crear", "Editar", "Eliminar" }, byModule["Administracion.Roles"]);

        // El propio módulo de permisos se descubre por sus endpoints de consulta y edición.
        Assert.Equal(new[] { "Ver", "Editar" }, byModule["Administracion.Permisos"]);
    }

    private sealed record CatalogItem(string Modulo, string[] Acciones);
}
