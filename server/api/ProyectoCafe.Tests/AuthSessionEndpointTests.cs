using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using ProyectoCafe.API.Auth;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Tests.Infrastructure;

namespace ProyectoCafe.Tests;

public sealed class AuthSessionEndpointTests
{
    private const string Password = ProyectoCafeApiFactory.DefaultPassword;

    [Fact]
    public async Task Login_WithInactiveUser_ReturnsForbiddenAndDoesNotSetAuthCookies()
    {
        await using var factory = new ProyectoCafeApiFactory();
        await factory.SeedUserAsync("inactive@test.local", UsuarioEstado.Inactivo);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "inactive@test.local",
            password = Password,
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.False(response.Headers.Contains("Set-Cookie"));

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var error = body.RootElement.GetProperty("error");
        Assert.Equal("USER_NOT_ACTIVE", error.GetProperty("code").GetString());
        Assert.Equal("Tu cuenta está desactivada.", error.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Login_WithPendingUser_ReturnsForbiddenWithActivationMessage()
    {
        await using var factory = new ProyectoCafeApiFactory();
        await factory.SeedUserAsync("pending@test.local", UsuarioEstado.PendienteActivacion);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "pending@test.local",
            password = Password,
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var error = body.RootElement.GetProperty("error");
        Assert.Equal("USER_NOT_ACTIVE", error.GetProperty("code").GetString());
        Assert.Contains("pendiente de activación", error.GetProperty("message").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Login_WithUnknownUser_ReturnsGenericInvalidCredentials()
    {
        await using var factory = new ProyectoCafeApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "does-not-exist@test.local",
            password = Password,
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var error = body.RootElement.GetProperty("error");
        Assert.Equal("INVALID_CREDENTIALS", error.GetProperty("code").GetString());
        Assert.Equal("Email o contraseña incorrectos.", error.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Login_WithValidCredentials_ExposesOnlyActiveRolesAndPermissions()
    {
        await using var factory = new ProyectoCafeApiFactory();
        await factory.SeedRoleAsync(
            "Admin",
            RolEstado.Activo,
            (Permissions.Modulos.AdministracionUsuarios, Permissions.Actions.Ver));
        await factory.SeedRoleAsync(
            "Legacy",
            RolEstado.Inactivo,
            (Permissions.Modulos.AdministracionRoles, Permissions.Actions.Eliminar));
        await factory.SeedUserAsync("admin@test.local", roles: ["Admin", "Legacy"]);

        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "admin@test.local",
            password = Password,
        });

        response.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var user = body.RootElement.GetProperty("user");

        var roles = user.GetProperty("roles").EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToArray();
        var permissions = user.GetProperty("permissions").EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToArray();

        Assert.Equal(["Admin"], roles);
        Assert.Equal(["perm:Administracion.Usuarios:Ver"], permissions);
    }

    [Fact]
    public async Task Me_WithoutAuthentication_ReturnsUnauthorized()
    {
        await using var factory = new ProyectoCafeApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithSession_ReturnsCurrentUserRolesAndPermissions()
    {
        await using var factory = new ProyectoCafeApiFactory();
        await factory.SeedUserWithPermissionsAsync(
            "viewer@test.local",
            (Permissions.Modulos.AdministracionRoles, Permissions.Actions.Ver));
        using var client = await factory.CreateAuthenticatedClientAsync("viewer@test.local");

        var response = await client.GetAsync("/api/v1/auth/me");

        response.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("viewer@test.local", body.RootElement.GetProperty("email").GetString());
        Assert.Equal(
            ["perm:Administracion.Roles:Ver"],
            body.RootElement.GetProperty("permissions").EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToArray());
        Assert.Single(body.RootElement.GetProperty("roles").EnumerateArray());
    }

    [Fact]
    public async Task DeactivatedUser_ExistingSessionIsRejected()
    {
        await using var factory = new ProyectoCafeApiFactory();
        var userId = await factory.SeedUserAsync("session@test.local");
        using var client = await factory.CreateAuthenticatedClientAsync("session@test.local");

        await factory.SetUserStateAsync(userId, UsuarioEstado.Inactivo);

        var response = await client.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_InvalidatesSessionCookies()
    {
        await using var factory = new ProyectoCafeApiFactory();
        await factory.SeedUserAsync("logout@test.local");
        using var client = await factory.CreateAuthenticatedClientAsync("logout@test.local");

        var logout = await client.PostAsync("/api/v1/auth/logout", content: null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var me = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }

    [Fact]
    public async Task Register_WithValidPayload_CreatesPendingUserWithHashedPassword()
    {
        await using var factory = new ProyectoCafeApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = "  New.User@Test.Local ",
            password = "AnotherSecret123!",
            name = "  Nueva Persona  ",
        });

        response.EnsureSuccessStatusCode();

        var user = await factory.FindUserByEmailAsync("new.user@test.local");
        Assert.NotNull(user);
        Assert.Equal(UsuarioEstado.PendienteActivacion, user!.Estado);
        Assert.Equal("Nueva Persona", user.Nombre);
        Assert.NotEqual("AnotherSecret123!", user.PasswordHash);
        Assert.True(PasswordHasher.Verify("AnotherSecret123!", user.PasswordHash));
    }

    [Fact]
    public async Task Register_WithInvalidPayload_ReturnsValidationProblem()
    {
        await using var factory = new ProyectoCafeApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = "not-an-email",
            password = "short",
            name = "Test",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(problem!.Errors.ContainsKey("Email"));
        Assert.True(problem.Errors.ContainsKey("Password"));
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsValidationProblem()
    {
        await using var factory = new ProyectoCafeApiFactory();
        await factory.SeedUserAsync("duplicate@test.local");
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = "DUPLICATE@test.local",
            password = "AnotherSecret123!",
            name = "Duplicate",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("ya está registrado", problem!.Errors["Email"].Single(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task AdminMutation_WithoutCsrfHeader_IsBlocked()
    {
        await using var factory = new ProyectoCafeApiFactory();
        await factory.SeedUserWithPermissionsAsync(
            "editor@test.local",
            (Permissions.Modulos.AdministracionUsuarios, Permissions.Actions.Editar));
        var targetId = await factory.SeedUserAsync("target@test.local", UsuarioEstado.PendienteActivacion);
        using var client = await factory.CreateAuthenticatedClientAsync("editor@test.local");
        client.DefaultRequestHeaders.Remove("X-CSRF");

        var response = await client.PostAsync($"/api/v1/admin/users/{targetId}/activate", content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("CSRF", body.RootElement.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task AdminMutation_WithWrongCsrfHeader_IsBlocked()
    {
        await using var factory = new ProyectoCafeApiFactory();
        await factory.SeedUserWithPermissionsAsync(
            "editor@test.local",
            (Permissions.Modulos.AdministracionUsuarios, Permissions.Actions.Editar));
        var targetId = await factory.SeedUserAsync("target@test.local", UsuarioEstado.PendienteActivacion);
        using var client = await factory.CreateAuthenticatedClientAsync(
            "editor@test.local",
            csrfTokenOverride: "not-the-real-token");

        var response = await client.PostAsync($"/api/v1/admin/users/{targetId}/activate", content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminRead_DoesNotRequireCsrfHeader()
    {
        await using var factory = new ProyectoCafeApiFactory();
        await factory.SeedUserWithPermissionsAsync(
            "viewer@test.local",
            (Permissions.Modulos.AdministracionUsuarios, Permissions.Actions.Ver));
        using var client = await factory.CreateAuthenticatedClientAsync("viewer@test.local");
        client.DefaultRequestHeaders.Remove("X-CSRF");

        var response = await client.GetAsync("/api/v1/admin/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
