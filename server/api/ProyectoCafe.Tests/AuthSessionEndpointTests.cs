using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using ProyectoCafe.API.Auth;
using ProyectoCafe.Application.Security;
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
    public async Task Me_WithOnlyAuthorizationHeader_ReturnsUnauthorized()
    {
        await using var factory = new ProyectoCafeApiFactory();
        await factory.SeedUserAsync("cookie-only@test.local");
        using var client = factory.CreateClient();

        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "cookie-only@test.local",
            password = Password,
        });
        login.EnsureSuccessStatusCode();

        var token = ProyectoCafeApiFactory.ReadCookie(login, "pc_at");
        Assert.False(string.IsNullOrWhiteSpace(token));

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token}");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_RotatesSessionCookiesAndKeepsUserInfo()
    {
        await using var factory = new ProyectoCafeApiFactory();
        await factory.SeedUserWithPermissionsAsync(
            "refresh@test.local",
            (Permissions.Modulos.AdministracionUsuarios, Permissions.Actions.Ver));
        using var client = await factory.CreateAuthenticatedClientAsync("refresh@test.local");

        var refresh = await client.PostAsync("/api/v1/auth/refresh", content: null);

        refresh.EnsureSuccessStatusCode();
        var authCookie = ProyectoCafeApiFactory.ReadCookie(refresh, "pc_at");
        var csrfCookie = ProyectoCafeApiFactory.ReadCookie(refresh, "pc_csrf");
        Assert.False(string.IsNullOrWhiteSpace(authCookie));
        Assert.False(string.IsNullOrWhiteSpace(csrfCookie));

        using var body = JsonDocument.Parse(await refresh.Content.ReadAsStringAsync());
        Assert.Equal("refresh@test.local", body.RootElement.GetProperty("user").GetProperty("email").GetString());
        Assert.False(body.RootElement.TryGetProperty("accessToken", out _));
        Assert.False(body.RootElement.TryGetProperty("token", out _));

        var me = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);

        client.DefaultRequestHeaders.Remove("X-CSRF");
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-CSRF", csrfCookie);
        var adminUsers = await client.GetAsync("/api/v1/admin/users");
        Assert.Equal(HttpStatusCode.OK, adminUsers.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithoutCsrfHeader_IsBlocked()
    {
        await using var factory = new ProyectoCafeApiFactory();
        await factory.SeedUserAsync("refresh-csrf@test.local");
        using var client = await factory.CreateAuthenticatedClientAsync("refresh-csrf@test.local");
        client.DefaultRequestHeaders.Remove("X-CSRF");

        var response = await client.PostAsync("/api/v1/auth/refresh", content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("CSRF", body.RootElement.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task Logout_WithoutSession_ClearsCookies()
    {
        await using var factory = new ProyectoCafeApiFactory();
        using var client = factory.CreateClient();

        var logout = await client.PostAsync("/api/v1/auth/logout", content: null);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.True(logout.Headers.TryGetValues("Set-Cookie", out var cookies));
        var joinedCookies = string.Join(';', cookies!);
        Assert.Contains("pc_at=;", joinedCookies);
        Assert.Contains("pc_csrf=;", joinedCookies);
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

    [Fact]
    public async Task Login_IssuedJwt_IsSignedWithConfiguredKeyAndRejectsOtherKeys()
    {
        await using var factory = new ProyectoCafeApiFactory();
        await factory.SeedUserAsync("jwt@test.local", roles: ["User"]);
        using var client = factory.CreateClient();

        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "jwt@test.local",
            password = Password,
        });
        login.EnsureSuccessStatusCode();

        var token = ProyectoCafeApiFactory.ReadCookie(login, "pc_at");
        Assert.False(string.IsNullOrWhiteSpace(token));

        var handler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "tests",
            ValidateAudience = true,
            ValidAudience = "tests",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(ProyectoCafeApiFactory.DefaultSigningKey)),
            ClockSkew = TimeSpan.FromSeconds(30),
        };

        var principal = handler.ValidateToken(token, validationParameters, out var validatedToken);

        Assert.True(principal.Identity?.IsAuthenticated);
        Assert.Contains("User", principal.FindAll(ClaimTypes.Role).Select(claim => claim.Value));
        var email = principal.FindFirst(ClaimTypes.Email)?.Value
            ?? principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
        Assert.Equal("jwt@test.local", email);
        Assert.Equal("tests", Assert.IsType<JwtSecurityToken>(validatedToken).Issuer);

        var wrongKeyParameters = validationParameters.Clone();
        wrongKeyParameters.IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("a-different-signing-key-with-32-bytes-min!!"));

        Assert.ThrowsAny<SecurityTokenException>(() =>
            handler.ValidateToken(token, wrongKeyParameters, out _));
    }

    [Fact]
    public async Task Login_WithLegacyHash_RehashesPasswordToRecommendedIterations()
    {
        await using var factory = new ProyectoCafeApiFactory();
        var userId = await factory.SeedUserAsync("legacy@test.local", password: Password);

        var before = await factory.GetUserAsync(userId);
        Assert.True(PasswordHasher.TryParse(before.PasswordHash, out var beforeIterations, out _, out _));
        Assert.Equal(10_000, beforeIterations);

        using var client = await factory.CreateAuthenticatedClientAsync("legacy@test.local");

        var after = await factory.GetUserAsync(userId);
        Assert.True(PasswordHasher.TryParse(after.PasswordHash, out var afterIterations, out _, out _));
        Assert.True(afterIterations >= PasswordHasher.DefaultIterations);
        Assert.True(PasswordHasher.Verify(Password, after.PasswordHash));
    }
}
