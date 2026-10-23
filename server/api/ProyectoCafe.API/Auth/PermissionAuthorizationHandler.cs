using Microsoft.AspNetCore.Authorization;
using ProyectoCafe.Application.Abstractions.Persistence;

namespace ProyectoCafe.API.Auth;

public sealed class PermissionAuthorizationHandler(
    IUsuarioRepository usuarios,
    IHttpContextAccessor httpContextAccessor) : AuthorizationHandler<PermissionRequirement>
{
    private readonly IUsuarioRepository _usuarios = usuarios;
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

        var hasPermission = await _usuarios.HasPermissionAsync(
            userId,
            requirement.Modulo,
            requirement.Accion,
            ct);

        if (hasPermission)
        {
            context.Succeed(requirement);
        }
    }
}
