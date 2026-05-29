using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoCafe.API.Auth;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.API.Endpoints;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/admin")
            .WithTags("Admin");

        group.MapGet("/users", async (
            [FromServices] ProyectoCafeDbContext db,
            CancellationToken ct) =>
        {
            var users = await db.Usuarios
                .Where(u => u.Estado != UsuarioEstado.Eliminado)
                .OrderByDescending(u => u.CreadoEn)
                .Select(u => new UserDto
                {
                    UsuarioId = u.UsuarioId,
                    Email = u.Email,
                    Name = u.Nombre,
                    Estado = u.Estado,
                    CreadoEn = u.CreadoEn,
                    UltimoLoginEn = u.UltimoLoginEn,
                })
                .ToListAsync(ct);

            var userIds = users.Select(u => u.UsuarioId).ToArray();
            var roles = await db.UsuariosRoles
                .Where(ur => userIds.Contains(ur.UsuarioId))
                .Join(db.Roles, ur => ur.RolId, r => r.RolId, (ur, r) => new { ur.UsuarioId, r.Nombre, r.Estado })
                .Where(x => x.Estado == RolEstado.Activo)
                .ToListAsync(ct);

            var rolesByUser = roles
                .GroupBy(x => x.UsuarioId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.Nombre).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToArray());

            foreach (var u in users)
            {
                u.Roles = rolesByUser.TryGetValue(u.UsuarioId, out var r) ? r : Array.Empty<string>();
            }

            return Results.Ok(users);
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.AdministracionUsuarios, Permissions.Actions.Ver));

        group.MapPost("/users/{id:int}/activate", async (
            int id,
            [FromServices] ProyectoCafeDbContext db,
            CancellationToken ct) =>
        {
            var user = await db.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == id && u.Estado != UsuarioEstado.Eliminado, ct);
            if (user is null) return Results.NotFound();

            user.Estado = UsuarioEstado.Activo;
            user.ActualizadoEn = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.AdministracionUsuarios, Permissions.Actions.Editar));

        group.MapPost("/users/{id:int}/deactivate", async (
            int id,
            [FromServices] ProyectoCafeDbContext db,
            CancellationToken ct) =>
        {
            var user = await db.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == id && u.Estado != UsuarioEstado.Eliminado, ct);
            if (user is null) return Results.NotFound();

            user.Estado = UsuarioEstado.Inactivo;
            user.ActualizadoEn = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.AdministracionUsuarios, Permissions.Actions.Editar));

        group.MapDelete("/users/{id:int}", async (
            int id,
            [FromServices] ProyectoCafeDbContext db,
            CancellationToken ct) =>
        {
            var user = await db.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == id && u.Estado != UsuarioEstado.Eliminado, ct);
            if (user is null) return Results.NotFound();

            user.Estado = UsuarioEstado.Eliminado;
            user.ActualizadoEn = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.AdministracionUsuarios, Permissions.Actions.Eliminar));

        group.MapPut("/users/{id:int}/roles", async (
            int id,
            [FromBody] UpdateUserRolesRequest request,
            [FromServices] ProyectoCafeDbContext db,
            CancellationToken ct) =>
        {
            var user = await db.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == id && u.Estado != UsuarioEstado.Eliminado, ct);
            if (user is null) return Results.NotFound();

            var roleNames = (request.Roles ?? Array.Empty<string>())
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(r => r.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var roles = await db.Roles
                .Where(r => r.Estado == RolEstado.Activo)
                .ToListAsync(ct);

            var selected = roles
                .Where(r => roleNames.Any(x => string.Equals(x, r.Nombre, StringComparison.OrdinalIgnoreCase)))
                .Select(r => r.RolId)
                .ToArray();

            var existing = await db.UsuariosRoles
                .Where(ur => ur.UsuarioId == user.UsuarioId)
                .ToListAsync(ct);

            db.UsuariosRoles.RemoveRange(existing);
            foreach (var roleId in selected)
            {
                db.UsuariosRoles.Add(new UsuarioRol { UsuarioId = user.UsuarioId, RolId = roleId });
            }

            user.ActualizadoEn = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.AdministracionUsuarios, Permissions.Actions.Editar));

        group.MapGet("/roles", async (
            [FromServices] ProyectoCafeDbContext db,
            CancellationToken ct) =>
        {
            var activeModules = Permissions.Modulos.All;

            var roles = await db.Roles
                .Where(r => r.Estado != RolEstado.Eliminado)
                .OrderBy(r => r.Nombre)
                .Select(r => new RoleDto
                {
                    RolId = r.RolId,
                    Nombre = r.Nombre,
                    Descripcion = r.Descripcion,
                    Estado = r.Estado,
                })
                .ToListAsync(ct);

            var roleIds = roles.Select(r => r.RolId).ToArray();
            var permisoCounts = await db.RolesPermisos
                .Where(rp => roleIds.Contains(rp.RolId) && activeModules.Contains(rp.Modulo))
                .GroupBy(rp => rp.RolId)
                .Select(g => new { RolId = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            var countsByRole = permisoCounts.ToDictionary(x => x.RolId, x => x.Count);
            foreach (var r in roles)
            {
                r.PermisosCount = countsByRole.TryGetValue(r.RolId, out var c) ? c : 0;
            }

            return Results.Ok(roles);
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.AdministracionRoles, Permissions.Actions.Ver));

        group.MapPost("/roles", async (
            [FromBody] UpsertRoleRequest request,
            [FromServices] ProyectoCafeDbContext db,
            HttpContext http,
            CancellationToken ct) =>
        {
            var nombre = (request.Nombre ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(nombre))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { [nameof(request.Nombre)] = ["Nombre es requerido."] });
            }
            if (nombre.Length > 50)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { [nameof(request.Nombre)] = ["Nombre no puede exceder 50 caracteres."] });
            }

            var descripcion = (request.Descripcion ?? string.Empty).Trim();
            var descripcionValue = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion;
            if (descripcionValue is not null && descripcionValue.Length > 250)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { [nameof(request.Descripcion)] = ["Descripcion no puede exceder 250 caracteres."] });
            }

            var exists = await db.Roles
                .AnyAsync(r => r.Estado != RolEstado.Eliminado && r.Nombre == nombre, ct);
            if (exists)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { [nameof(request.Nombre)] = ["Ya existe un rol con ese nombre."] });
            }

            var role = new Rol { Nombre = nombre, Descripcion = descripcionValue, Estado = RolEstado.Activo };
            db.Roles.Add(role);
            await db.SaveChangesAsync(ct);

            var basePath = http.Request.Path.Value?.TrimEnd('/') ?? string.Empty;
            return Results.Created($"{basePath}/{role.RolId}", new { role.RolId });
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.AdministracionRoles, Permissions.Actions.Crear));

        group.MapPut("/roles/{id:int}", async (
            int id,
            [FromBody] UpsertRoleRequest request,
            [FromServices] ProyectoCafeDbContext db,
            CancellationToken ct) =>
        {
            var role = await db.Roles.FirstOrDefaultAsync(r => r.RolId == id && r.Estado != RolEstado.Eliminado, ct);
            if (role is null) return Results.NotFound();

            var nombre = (request.Nombre ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(nombre))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { [nameof(request.Nombre)] = ["Nombre es requerido."] });
            }
            if (nombre.Length > 50)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { [nameof(request.Nombre)] = ["Nombre no puede exceder 50 caracteres."] });
            }

            var descripcion = (request.Descripcion ?? string.Empty).Trim();
            var descripcionValue = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion;
            if (descripcionValue is not null && descripcionValue.Length > 250)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { [nameof(request.Descripcion)] = ["Descripcion no puede exceder 250 caracteres."] });
            }

            var nameExists = await db.Roles
                .AnyAsync(r => r.RolId != id && r.Estado != RolEstado.Eliminado && r.Nombre == nombre, ct);
            if (nameExists)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { [nameof(request.Nombre)] = ["Ya existe un rol con ese nombre."] });
            }

            role.Nombre = nombre;
            role.Descripcion = descripcionValue;
            role.Estado = request.Estado ?? role.Estado;

            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.AdministracionRoles, Permissions.Actions.Editar));

        group.MapDelete("/roles/{id:int}", async (
            int id,
            [FromServices] ProyectoCafeDbContext db,
            CancellationToken ct) =>
        {
            var role = await db.Roles.FirstOrDefaultAsync(r => r.RolId == id && r.Estado != RolEstado.Eliminado, ct);
            if (role is null) return Results.NotFound();

            role.Estado = RolEstado.Eliminado;
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.AdministracionRoles, Permissions.Actions.Eliminar));

        group.MapGet("/roles/{id:int}/permissions", async (
            int id,
            [FromServices] ProyectoCafeDbContext db,
            CancellationToken ct) =>
        {
            var activeModules = Permissions.Modulos.All;

            var roleExists = await db.Roles.AnyAsync(r => r.RolId == id && r.Estado != RolEstado.Eliminado, ct);
            if (!roleExists) return Results.NotFound();

            var perms = await db.RolesPermisos
                .Where(rp => rp.RolId == id && activeModules.Contains(rp.Modulo))
                .OrderBy(rp => rp.Modulo)
                .ThenBy(rp => rp.Accion)
                .Select(rp => new PermissionDto { Modulo = rp.Modulo, Accion = rp.Accion })
                .ToListAsync(ct);

            return Results.Ok(perms);
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.AdministracionPermisos, Permissions.Actions.Ver));

        group.MapPut("/roles/{id:int}/permissions", async (
            int id,
            [FromBody] UpdateRolePermissionsRequest request,
            [FromServices] ProyectoCafeDbContext db,
            CancellationToken ct) =>
        {
            var role = await db.Roles.FirstOrDefaultAsync(r => r.RolId == id && r.Estado != RolEstado.Eliminado, ct);
            if (role is null) return Results.NotFound();

            var list = request.Permissions ?? Array.Empty<PermissionDto>();
            var normalized = list
                .Where(p => p is not null)
                .Select(p => new PermissionDto
                {
                    Modulo = (p.Modulo ?? string.Empty).Trim(),
                    Accion = (p.Accion ?? string.Empty).Trim(),
                })
                .Where(p => !string.IsNullOrWhiteSpace(p.Modulo) && !string.IsNullOrWhiteSpace(p.Accion))
                .DistinctBy(p => $"{p.Modulo}:{p.Accion}", StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (var p in normalized)
            {
                var modulo = p.Modulo ?? string.Empty;
                var accion = p.Accion ?? string.Empty;

                if (modulo.Length > 120)
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]> { [nameof(PermissionDto.Modulo)] = ["Modulo no puede exceder 120 caracteres."] });
                }
                if (accion.Length > 12)
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]> { [nameof(PermissionDto.Accion)] = ["Accion no puede exceder 12 caracteres."] });
                }
            }

            var existing = await db.RolesPermisos
                .Where(rp => rp.RolId == id)
                .ToListAsync(ct);

            db.RolesPermisos.RemoveRange(existing);
            foreach (var p in normalized)
            {
                db.RolesPermisos.Add(new RolPermiso
                {
                    RolId = id,
                    Modulo = p.Modulo ?? string.Empty,
                    Accion = p.Accion ?? string.Empty,
                });
            }

            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.AdministracionPermisos, Permissions.Actions.Editar));

        group.MapGet("/permissions/catalog", () =>
        {
            var data = Permissions.Modulos.All
                .OrderBy(m => m)
                .Select(m => new
                {
                    modulo = m,
                    acciones = Permissions.Actions.Crud
                })
                .ToArray();

            return Results.Ok(data);
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.AdministracionPermisos, Permissions.Actions.Ver));

        return endpoints;
    }

    public sealed class UserDto
    {
        public required int UsuarioId { get; set; }
        public required string Email { get; set; }
        public required string Name { get; set; }
        public required UsuarioEstado Estado { get; set; }
        public required DateTime CreadoEn { get; set; }
        public DateTime? UltimoLoginEn { get; set; }
        public string[] Roles { get; set; } = Array.Empty<string>();
    }

    public sealed class UpdateUserRolesRequest
    {
        public string[]? Roles { get; set; }
    }

    public sealed class RoleDto
    {
        public required int RolId { get; set; }
        public required string Nombre { get; set; }
        public string? Descripcion { get; set; }
        public required RolEstado Estado { get; set; }
        public int PermisosCount { get; set; }
    }

    public sealed class UpsertRoleRequest
    {
        public string? Nombre { get; set; }
        public string? Descripcion { get; set; }
        public RolEstado? Estado { get; set; }
    }

    public sealed class PermissionDto
    {
        public string? Modulo { get; set; }
        public string? Accion { get; set; }
    }

    public sealed class UpdateRolePermissionsRequest
    {
        public PermissionDto[]? Permissions { get; set; }
    }
}
