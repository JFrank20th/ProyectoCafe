using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ProyectoCafe.Domain.Entities;
using ProyectoCafe.Infrastructure.Data;

namespace ProyectoCafe.API.Auth;

public sealed class PermissionAuthorizationHandler(
    ProyectoCafeDbContext db,
    IHttpContextAccessor httpContextAccessor) : AuthorizationHandler<PermissionRequirement>
{
    private readonly ProyectoCafeDbContext _db = db;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var uid = context.User.FindFirst("uid")?.Value;
        if (!int.TryParse(uid, out var userId))
        {
            return;
        }

        var ct = _httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;

        var hasPermission = await _db.UsuariosRoles
            .Where(ur => ur.UsuarioId == userId)
            .Join(_db.Roles, ur => ur.RolId, r => r.RolId, (_, r) => r)
            .Where(r => r.Estado == RolEstado.Activo)
            .Join(_db.RolesPermisos, r => r.RolId, rp => rp.RolId, (_, rp) => rp)
            .AnyAsync(rp => rp.Modulo == requirement.Modulo && rp.Accion == requirement.Accion, ct);

        if (hasPermission)
        {
            context.Succeed(requirement);
        }
    }
}
