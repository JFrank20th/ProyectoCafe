using Microsoft.EntityFrameworkCore;
using ProyectoCafe.Application.Abstractions.Persistence;
using ProyectoCafe.Application.Security;
using ProyectoCafe.Infrastructure.Data;
using ProyectoCafe.Infrastructure.Repositories;

namespace ProyectoCafe.Tests;

/// <summary>
/// Verifica que el bootstrap de seguridad pueda ejecutarse de forma repetida
/// sin duplicar roles, permisos ni usuarios (idempotencia).
/// </summary>
public sealed class SecurityBootstrapStoreTests
{
    private static ProyectoCafeDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<ProyectoCafeDbContext>()
            .UseInMemoryDatabase($"security-bootstrap-{Guid.NewGuid():N}")
            .Options);

    [Fact]
    public async Task EnsureRolesAsync_CanRunTwice_WithoutDuplicates()
    {
        await using var db = CreateDbContext();
        var store = new SecurityBootstrapStore(db);

        await store.EnsureRolesAsync(["Admin", "User"], CancellationToken.None);
        await store.EnsureRolesAsync(["Admin", "User"], CancellationToken.None);

        Assert.Equal(2, await db.Roles.CountAsync());
    }

    [Fact]
    public async Task EnsureRolePermissionsAsync_CanRunTwice_WithoutDuplicates()
    {
        await using var db = CreateDbContext();
        var store = new SecurityBootstrapStore(db);

        await store.EnsureRolesAsync(["Admin"], CancellationToken.None);

        SecurityPermission[] permissions =
        [
            new("Inventario.Presentaciones", "Ver"),
            new("Ventas.Ventas", "Crear"),
        ];

        await store.EnsureRolePermissionsAsync("Admin", permissions, CancellationToken.None);
        await store.EnsureRolePermissionsAsync("Admin", permissions, CancellationToken.None);

        Assert.Equal(2, await db.RolesPermisos.CountAsync());
    }

    [Fact]
    public async Task EnsureUsersAsync_CanRunTwice_WithoutDuplicates()
    {
        await using var db = CreateDbContext();
        var store = new SecurityBootstrapStore(db);

        await store.EnsureRolesAsync(["User"], CancellationToken.None);

        var passwordHash = PasswordHasher.HashPassword("A-strong-password!", iterations: 10_000);
        SecurityBootstrapUser[] users =
        [
            new("user@example.com", "Usuario", passwordHash, ["User"]),
        ];

        await store.EnsureUsersAsync(users, CancellationToken.None);
        await store.EnsureUsersAsync(users, CancellationToken.None);

        var user = await db.Usuarios.SingleAsync();
        Assert.Equal("user@example.com", user.EmailNormalizado);
        Assert.Single(await db.UsuariosRoles.ToListAsync());
    }
}
