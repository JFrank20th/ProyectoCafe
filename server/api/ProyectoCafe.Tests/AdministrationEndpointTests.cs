using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using ProyectoCafe.API.Auth;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Tests.Infrastructure;

namespace ProyectoCafe.Tests;

public sealed class AdministrationEndpointTests
{
    [Fact]
    public async Task ListUsers_ReturnsNonDeletedUsersWithOnlyActiveRoles()
    {
        await using var factory = new ProyectoCafeApiFactory();
        await factory.SeedUserWithPermissionsAsync(
            "viewer@test.local",
            (Permissions.Modulos.AdministracionUsuarios, Permissions.Actions.Ver));
        await factory.SeedRoleAsync("ActiveRole", RolEstado.Activo);
        await factory.SeedRoleAsync("InactiveRole", RolEstado.Inactivo);
        await factory.SeedUserAsync("target@test.local", roles: ["ActiveRole", "InactiveRole"]);
        await factory.SeedUserAsync("deleted@test.local", UsuarioEstado.Eliminado);

        using var client = await factory.CreateAuthenticatedClientAsync("viewer@test.local");
        var response = await client.GetAsync("/api/v1/admin/users");

        response.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var users = body.RootElement.EnumerateArray().ToArray();

        Assert.DoesNotContain(users, user => user.GetProperty("email").GetString() == "deleted@test.local");

        var target = Assert.Single(users, user => user.GetProperty("email").GetString() == "target@test.local");
        Assert.Equal(
            ["ActiveRole"],
            target.GetProperty("roles").EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToArray());
    }

    [Fact]
    public async Task ActivateAndDeactivateUser_UpdatesPersistedState()
    {
        await using var factory = new ProyectoCafeApiFactory();
        await factory.SeedUserWithPermissionsAsync(
            "editor@test.local",
            (Permissions.Modulos.AdministracionUsuarios, Permissions.Actions.Editar));
        var targetId = await factory.SeedUserAsync("target@test.local", UsuarioEstado.PendienteActivacion);
        using var client = await factory.CreateAuthenticatedClientAsync("editor@test.local");

        var activate = await client.PostAsync($"/api/v1/admin/users/{targetId}/activate", content: null);
        Assert.Equal(HttpStatusCode.NoContent, activate.StatusCode);
        Assert.Equal(UsuarioEstado.Activo, (await factory.GetUserAsync(targetId)).Estado);

        var deactivate = await client.PostAsync($"/api/v1/admin/users/{targetId}/deactivate", content: null);
        Assert.Equal(HttpStatusCode.NoContent, deactivate.StatusCode);
        Assert.Equal(UsuarioEstado.Inactivo, (await factory.GetUserAsync(targetId)).Estado);
    }

    [Fact]
    public async Task ActivateUser_WhenMissing_ReturnsNotFound()
    {
        await using var factory = new ProyectoCafeApiFactory();
        await factory.SeedUserWithPermissionsAsync(
            "editor@test.local",
            (Permissions.Modulos.AdministracionUsuarios, Permissions.Actions.Editar));
        using var client = await factory.CreateAuthenticatedClientAsync("editor@test.local");

        var response = await client.PostAsync("/api/v1/admin/users/999999/activate", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_PerformsSoftDeleteAndHidesUserFromList()
    {
        await using var factory = new ProyectoCafeApiFactory();
        await factory.SeedUserWithPermissionsAsync(
            "deleter@test.local",
            (Permissions.Modulos.AdministracionUsuarios, Permissions.Actions.Ver),
            (Permissions.Modulos.AdministracionUsuarios, Permissions.Actions.Eliminar));
        var targetId = await factory.SeedUserAsync("target@test.local");
        using var client = await factory.CreateAuthenticatedClientAsync("deleter@test.local");

        var delete = await client.DeleteAsync($"/api/v1/admin/users/{targetId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(UsuarioEstado.Eliminado, (await factory.GetUserAsync(targetId)).Estado);

        var list = await client.GetAsync("/api/v1/admin/users");
        list.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        Assert.DoesNotContain(
            body.RootElement.EnumerateArray(),
            user => user.GetProperty("usuarioId").GetInt32() == targetId);
    }

    [Fact]
    public async Task UpdateUserRoles_ReplacesRolesAndIgnoresUnknownOrInactiveRoles()
    {
        await using var factory = new ProyectoCafeApiFactory();
        await factory.SeedUserWithPermissionsAsync(
            "editor@test.local",
            (Permissions.Modulos.AdministracionUsuarios, Permissions.Actions.Editar));
        await factory.SeedRoleAsync("Alpha", RolEstado.Activo);
        await factory.SeedRoleAsync("Beta", RolEstado.Inactivo);
        await factory.SeedRoleAsync("Gamma", RolEstado.Activo);
        var targetId = await factory.SeedUserAsync("target@test.local", roles: ["Alpha"]);
        using var client = await factory.CreateAuthenticatedClientAsync("editor@test.local");

        var response = await client.PutAsJsonAsync($"/api/v1/admin/users/{targetId}/roles", new
        {
            roles = new[] { "Gamma", "Beta", "Unknown" },
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(["Gamma"], await factory.GetUserRolesAsync(targetId));

        var clear = await client.PutAsJsonAsync($"/api/v1/admin/users/{targetId}/roles", new
        {
            roles = Array.Empty<string>(),
        });

        Assert.Equal(HttpStatusCode.NoContent, clear.StatusCode);
        Assert.Empty(await factory.GetUserRolesAsync(targetId));
    }

    [Fact]
    public async Task RoleCrud_LifecyclePersistsChangesAndSoftDeletes()
    {
        await using var factory = new ProyectoCafeApiFactory();
        await factory.SeedUserWithPermissionsAsync(
            "roles-admin@test.local",
            (Permissions.Modulos.AdministracionRoles, Permissions.Actions.Ver),
            (Permissions.Modulos.AdministracionRoles, Permissions.Actions.Crear),
            (Permissions.Modulos.AdministracionRoles, Permissions.Actions.Editar),
            (Permissions.Modulos.AdministracionRoles, Permissions.Actions.Eliminar));
        using var client = await factory.CreateAuthenticatedClientAsync("roles-admin@test.local");

        var create = await client.PostAsJsonAsync("/api/v1/admin/roles", new
        {
            nombre = "Operador",
            descripcion = "Rol de prueba",
        });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        using var createdBody = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var roleId = createdBody.RootElement.GetProperty("rolId").GetInt32();

        var update = await client.PutAsJsonAsync($"/api/v1/admin/roles/{roleId}", new
        {
            nombre = "Operador Senior",
            descripcion = "Actualizado",
            estado = (int)RolEstado.Inactivo,
        });

        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);
        var role = await factory.GetRoleAsync(roleId);
        Assert.Equal("Operador Senior", role.Nombre);
        Assert.Equal("Actualizado", role.Descripcion);
        Assert.Equal(RolEstado.Inactivo, role.Estado);

        var list = await client.GetAsync("/api/v1/admin/roles");
        list.EnsureSuccessStatusCode();
        using var listBody = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        Assert.Contains(
            listBody.RootElement.EnumerateArray(),
            item => item.GetProperty("rolId").GetInt32() == roleId);

        var delete = await client.DeleteAsync($"/api/v1/admin/roles/{roleId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(RolEstado.Eliminado, (await factory.GetRoleAsync(roleId)).Estado);

        var listAfterDelete = await client.GetAsync("/api/v1/admin/roles");
        listAfterDelete.EnsureSuccessStatusCode();
        using var listAfterDeleteBody = JsonDocument.Parse(await listAfterDelete.Content.ReadAsStringAsync());
        Assert.DoesNotContain(
            listAfterDeleteBody.RootElement.EnumerateArray(),
            item => item.GetProperty("rolId").GetInt32() == roleId);
    }

    [Theory]
    [InlineData("", "Nombre es requerido.")]
    [InlineData("   ", "Nombre es requerido.")]
    public async Task CreateRole_WithEmptyName_ReturnsValidationProblem(string name, string expectedMessage)
    {
        await using var factory = new ProyectoCafeApiFactory();
        await factory.SeedUserWithPermissionsAsync(
            "roles-admin@test.local",
            (Permissions.Modulos.AdministracionRoles, Permissions.Actions.Crear));
        using var client = await factory.CreateAuthenticatedClientAsync("roles-admin@test.local");

        var response = await client.PostAsJsonAsync("/api/v1/admin/roles", new { nombre = name });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains(expectedMessage, problem!.Errors["Nombre"].Single(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateRole_WithDuplicateName_ReturnsValidationProblem()
    {
        await using var factory = new ProyectoCafeApiFactory();
        await factory.SeedUserWithPermissionsAsync(
            "roles-admin@test.local",
            (Permissions.Modulos.AdministracionRoles, Permissions.Actions.Crear));
        await factory.SeedRoleAsync("Duplicado", RolEstado.Activo);
        using var client = await factory.CreateAuthenticatedClientAsync("roles-admin@test.local");

        var response = await client.PostAsJsonAsync("/api/v1/admin/roles", new { nombre = "Duplicado" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("Ya existe un rol con ese nombre.", problem!.Errors["Nombre"].Single(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateRole_WithOversizedName_ReturnsValidationProblem()
    {
        await using var factory = new ProyectoCafeApiFactory();
        await factory.SeedUserWithPermissionsAsync(
            "roles-admin@test.local",
            (Permissions.Modulos.AdministracionRoles, Permissions.Actions.Crear));
        using var client = await factory.CreateAuthenticatedClientAsync("roles-admin@test.local");

        var response = await client.PostAsJsonAsync("/api/v1/admin/roles", new
        {
            nombre = new string('R', 51),
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateRole_WhenRenamingToExistingName_ReturnsValidationProblem()
    {
        await using var factory = new ProyectoCafeApiFactory();
        await factory.SeedUserWithPermissionsAsync(
            "roles-admin@test.local",
            (Permissions.Modulos.AdministracionRoles, Permissions.Actions.Editar));
        await factory.SeedRoleAsync("Uno", RolEstado.Activo);
        var roleId = await factory.SeedRoleAsync("Dos", RolEstado.Activo);
        using var client = await factory.CreateAuthenticatedClientAsync("roles-admin@test.local");

        var response = await client.PutAsJsonAsync($"/api/v1/admin/roles/{roleId}", new
        {
            nombre = "Uno",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateRolePermissions_PersistsOnlyKeysFromCatalog()
    {
        await using var factory = new ProyectoCafeApiFactory();
        await factory.SeedUserWithPermissionsAsync(
            "permissions-admin@test.local",
            (Permissions.Modulos.AdministracionPermisos, Permissions.Actions.Ver),
            (Permissions.Modulos.AdministracionPermisos, Permissions.Actions.Editar));
        var roleId = await factory.SeedRoleAsync("Operador", RolEstado.Activo);
        using var client = await factory.CreateAuthenticatedClientAsync("permissions-admin@test.local");

        var update = await client.PutAsJsonAsync($"/api/v1/admin/roles/{roleId}/permissions", new
        {
            permissions = new[]
            {
                new { modulo = Permissions.Modulos.AdministracionRoles, accion = Permissions.Actions.Ver },
                new { modulo = Permissions.Modulos.AdministracionUsuarios, accion = Permissions.Actions.Editar },
                new { modulo = "Modulo.Inexistente", accion = Permissions.Actions.Ver },
                new { modulo = Permissions.Modulos.AdministracionRoles, accion = "NoExiste" },
            },
        });

        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);

        var persisted = await factory.GetRolePermissionsAsync(roleId);
        Assert.Equal(
            [
                (Permissions.Modulos.AdministracionRoles, Permissions.Actions.Ver),
                (Permissions.Modulos.AdministracionUsuarios, Permissions.Actions.Editar),
            ],
            persisted);

        var get = await client.GetAsync($"/api/v1/admin/roles/{roleId}/permissions");
        get.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        var returned = body.RootElement.EnumerateArray()
            .Select(item => (
                item.GetProperty("modulo").GetString() ?? string.Empty,
                item.GetProperty("accion").GetString() ?? string.Empty))
            .ToArray();

        Assert.Equal(persisted, returned);
    }

    [Fact]
    public async Task UpdateRolePermissions_WithOversizedAction_ReturnsValidationProblem()
    {
        await using var factory = new ProyectoCafeApiFactory();
        await factory.SeedUserWithPermissionsAsync(
            "permissions-admin@test.local",
            (Permissions.Modulos.AdministracionPermisos, Permissions.Actions.Editar));
        var roleId = await factory.SeedRoleAsync("Operador", RolEstado.Activo);
        using var client = await factory.CreateAuthenticatedClientAsync("permissions-admin@test.local");

        var response = await client.PutAsJsonAsync($"/api/v1/admin/roles/{roleId}/permissions", new
        {
            permissions = new[]
            {
                new { modulo = Permissions.Modulos.AdministracionRoles, accion = new string('A', 13) },
            },
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await factory.GetRolePermissionsAsync(roleId));
    }

    [Fact]
    public async Task UpdateRolePermissions_WhenRoleMissing_ReturnsNotFound()
    {
        await using var factory = new ProyectoCafeApiFactory();
        await factory.SeedUserWithPermissionsAsync(
            "permissions-admin@test.local",
            (Permissions.Modulos.AdministracionPermisos, Permissions.Actions.Editar));
        using var client = await factory.CreateAuthenticatedClientAsync("permissions-admin@test.local");

        var response = await client.PutAsJsonAsync("/api/v1/admin/roles/999999/permissions", new
        {
            permissions = Array.Empty<object>(),
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
