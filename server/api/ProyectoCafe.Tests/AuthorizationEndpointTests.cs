using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ProyectoCafe.API.Auth;
using ProyectoCafe.Tests.Infrastructure;

namespace ProyectoCafe.Tests;

public sealed class AuthorizationEndpointTests
{
    [Fact]
    public async Task AdminUsers_WithoutAuthentication_ReturnsUnauthorized()
    {
        await using var factory = new ProyectoCafeApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/admin/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminUsers_WithAuthenticatedUserWithoutPermission_ReturnsForbidden()
    {
        await using var factory = new ProyectoCafeApiFactory();
        await factory.SeedUserAsync("no-permissions@test.local");
        using var client = await factory.CreateAuthenticatedClientAsync("no-permissions@test.local");

        var response = await client.GetAsync("/api/v1/admin/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RolePermission_DoesNotGrantAccessToUsersModule()
    {
        await using var factory = new ProyectoCafeApiFactory();
        await factory.SeedUserWithPermissionsAsync(
            "roles-viewer@test.local",
            (Permissions.Modulos.AdministracionRoles, Permissions.Actions.Ver));
        using var client = await factory.CreateAuthenticatedClientAsync("roles-viewer@test.local");

        var users = await client.GetAsync("/api/v1/admin/users");
        var roles = await client.GetAsync("/api/v1/admin/roles");

        Assert.Equal(HttpStatusCode.Forbidden, users.StatusCode);
        Assert.Equal(HttpStatusCode.OK, roles.StatusCode);
    }

    [Fact]
    public async Task InactiveRole_DoesNotGrantPermissions()
    {
        await using var factory = new ProyectoCafeApiFactory();
        await factory.SeedRoleAsync("Inactive", ProyectoCafe.Domain.Entities.RolEstado.Inactivo,
            (Permissions.Modulos.AdministracionUsuarios, Permissions.Actions.Ver));
        await factory.SeedUserAsync("inactive-role@test.local", roles: ["Inactive"]);
        using var client = await factory.CreateAuthenticatedClientAsync("inactive-role@test.local");

        var response = await client.GetAsync("/api/v1/admin/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PermissionCatalog_RequiresPermissionsViewPermission()
    {
        await using var factory = new ProyectoCafeApiFactory();
        await factory.SeedUserWithPermissionsAsync(
            "roles-viewer@test.local",
            (Permissions.Modulos.AdministracionRoles, Permissions.Actions.Ver));
        using var client = await factory.CreateAuthenticatedClientAsync("roles-viewer@test.local");

        var forbidden = await client.GetAsync("/api/v1/admin/permissions/catalog");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task PermissionCatalog_WithPermission_ReturnsExposedModules()
    {
        await using var factory = new ProyectoCafeApiFactory();
        await factory.SeedUserWithPermissionsAsync(
            "permissions-viewer@test.local",
            (Permissions.Modulos.AdministracionPermisos, Permissions.Actions.Ver));
        using var client = await factory.CreateAuthenticatedClientAsync("permissions-viewer@test.local");

        var response = await client.GetAsync("/api/v1/admin/permissions/catalog");

        response.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var modules = body.RootElement.EnumerateArray()
            .Select(item => item.GetProperty("modulo").GetString() ?? string.Empty)
            .ToArray();

        Assert.Equal(
            [
                Permissions.Modulos.AdministracionPermisos,
                Permissions.Modulos.AdministracionRoles,
                Permissions.Modulos.AdministracionUsuarios,
            ],
            modules);
    }

    [Fact]
    public async Task CreateRole_WithoutCreatePermission_ReturnsForbidden()
    {
        await using var factory = new ProyectoCafeApiFactory();
        await factory.SeedUserWithPermissionsAsync(
            "roles-viewer@test.local",
            (Permissions.Modulos.AdministracionRoles, Permissions.Actions.Ver));
        using var client = await factory.CreateAuthenticatedClientAsync("roles-viewer@test.local");

        var response = await client.PostAsJsonAsync("/api/v1/admin/roles", new { nombre = "Nuevo" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUserRoles_WithoutEditPermission_ReturnsForbidden()
    {
        await using var factory = new ProyectoCafeApiFactory();
        await factory.SeedUserWithPermissionsAsync(
            "users-viewer@test.local",
            (Permissions.Modulos.AdministracionUsuarios, Permissions.Actions.Ver));
        var targetId = await factory.SeedUserAsync("target@test.local");
        using var client = await factory.CreateAuthenticatedClientAsync("users-viewer@test.local");

        var response = await client.PutAsJsonAsync($"/api/v1/admin/users/{targetId}/roles", new
        {
            roles = new[] { "Admin" },
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_WithoutDeletePermission_ReturnsForbidden()
    {
        await using var factory = new ProyectoCafeApiFactory();
        await factory.SeedUserWithPermissionsAsync(
            "users-editor@test.local",
            (Permissions.Modulos.AdministracionUsuarios, Permissions.Actions.Editar));
        var targetId = await factory.SeedUserAsync("target@test.local");
        using var client = await factory.CreateAuthenticatedClientAsync("users-editor@test.local");

        var response = await client.DeleteAsync($"/api/v1/admin/users/{targetId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateRolePermissions_WithoutEditPermission_ReturnsForbidden()
    {
        await using var factory = new ProyectoCafeApiFactory();
        await factory.SeedUserWithPermissionsAsync(
            "permissions-viewer@test.local",
            (Permissions.Modulos.AdministracionPermisos, Permissions.Actions.Ver));
        var roleId = await factory.SeedRoleAsync("Operador");
        using var client = await factory.CreateAuthenticatedClientAsync("permissions-viewer@test.local");

        var response = await client.PutAsJsonAsync($"/api/v1/admin/roles/{roleId}/permissions", new
        {
            permissions = Array.Empty<object>(),
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
