using ProyectoCafe.Application.Abstractions.Persistence;
using ProyectoCafe.Application.Abstractions.Security;
using ProyectoCafe.Application.Common;
using ProyectoCafe.Application.DTOs.Administration;
using ProyectoCafe.Application.Security;
using ProyectoCafe.Domain.Entities;

namespace ProyectoCafe.Application.Services;

/// <summary>
/// Casos de uso del módulo de Administración (usuarios, roles y permisos).
/// Toda la persistencia se realiza a través de las abstracciones de la capa
/// Application, implementadas en Infrastructure.
/// </summary>
public sealed class AdministrationService(
    IUsuarioRepository usuarios,
    IRolRepository roles,
    IUnitOfWork unitOfWork,
    IPermissionCatalog permissionCatalog,
    TimeProvider timeProvider)
{
    private readonly IUsuarioRepository _usuarios = usuarios;
    private readonly IRolRepository _roles = roles;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IPermissionCatalog _permissionCatalog = permissionCatalog;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken ct)
    {
        var rows = await _usuarios.ListNotDeletedAsync(ct);

        return rows.Select(row => new UserDto
        {
            UsuarioId = row.UsuarioId,
            Email = row.Email,
            Name = row.Nombre,
            Estado = row.Estado,
            CreadoEn = row.CreadoEn,
            UltimoLoginEn = row.UltimoLoginEn,
            Roles = row.Roles,
        }).ToArray();
    }

    public Task<bool> ActivateUserAsync(int usuarioId, CancellationToken ct) =>
        SetUserStateAsync(usuarioId, UsuarioEstado.Activo, ct);

    public Task<bool> DeactivateUserAsync(int usuarioId, CancellationToken ct) =>
        SetUserStateAsync(usuarioId, UsuarioEstado.Inactivo, ct);

    public Task<bool> DeleteUserAsync(int usuarioId, CancellationToken ct) =>
        SetUserStateAsync(usuarioId, UsuarioEstado.Eliminado, ct);

    public async Task<OperationResult> UpdateUserRolesAsync(
        int usuarioId,
        UpdateUserRolesRequest request,
        CancellationToken ct)
    {
        var user = await _usuarios.FindNotDeletedByIdAsync(usuarioId, ct);
        if (user is null)
        {
            return OperationResult.NotFound();
        }

        var roleNames = (request.Roles ?? Array.Empty<string>())
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var activeRoles = await _roles.ListActiveAsync(ct);

        var selected = activeRoles
            .Where(r => roleNames.Any(x => string.Equals(x, r.Nombre, StringComparison.OrdinalIgnoreCase)))
            .Select(r => r.RolId)
            .ToArray();

        await _usuarios.ReplaceRolesAsync(user.UsuarioId, selected, ct);

        user.ActualizadoEn = _timeProvider.GetUtcNow().UtcDateTime;
        await _unitOfWork.SaveChangesAsync(ct);

        return OperationResult.Success();
    }

    public async Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken ct)
    {
        var validKeys = _permissionCatalog.GetPermissionKeys();

        var roles = (await _roles.ListNotDeletedAsync(ct))
            .Select(r => new RoleDto
            {
                RolId = r.RolId,
                Nombre = r.Nombre,
                Descripcion = r.Descripcion,
                Estado = r.Estado,
            })
            .ToArray();

        var roleIds = roles.Select(r => r.RolId).ToArray();
        var permisos = await _roles.ListPermissionsByRoleIdsAsync(roleIds, ct);

        var countsByRole = permisos
            .Where(p => validKeys.Count == 0 || validKeys.Contains(PermissionKey.Build(p.Modulo, p.Accion)))
            .GroupBy(p => p.RolId)
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (var role in roles)
        {
            role.PermisosCount = countsByRole.TryGetValue(role.RolId, out var count) ? count : 0;
        }

        return roles;
    }

    public async Task<OperationResult<int>> CreateRoleAsync(
        UpsertRoleRequest request,
        CancellationToken ct)
    {
        if (!TryNormalizeRole(request, out var nombre, out var descripcion, out var errors))
        {
            return OperationResult<int>.Validation(errors);
        }

        if (await _roles.NameExistsAsync(nombre, excludeRolId: null, ct))
        {
            return OperationResult<int>.Validation(new Dictionary<string, string[]>
            {
                [nameof(UpsertRoleRequest.Nombre)] = ["Ya existe un rol con ese nombre."]
            });
        }

        var role = new Rol { Nombre = nombre, Descripcion = descripcion, Estado = RolEstado.Activo };
        await _roles.AddAsync(role, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return OperationResult<int>.Success(role.RolId);
    }

    public async Task<OperationResult> UpdateRoleAsync(
        int rolId,
        UpsertRoleRequest request,
        CancellationToken ct)
    {
        var role = await _roles.FindNotDeletedAsync(rolId, ct);
        if (role is null)
        {
            return OperationResult.NotFound();
        }

        if (!TryNormalizeRole(request, out var nombre, out var descripcion, out var errors))
        {
            return OperationResult.Validation(errors);
        }

        if (await _roles.NameExistsAsync(nombre, rolId, ct))
        {
            return OperationResult.Validation(new Dictionary<string, string[]>
            {
                [nameof(UpsertRoleRequest.Nombre)] = ["Ya existe un rol con ese nombre."]
            });
        }

        role.Nombre = nombre;
        role.Descripcion = descripcion;
        role.Estado = request.Estado ?? role.Estado;

        await _unitOfWork.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<bool> DeleteRoleAsync(int rolId, CancellationToken ct)
    {
        var role = await _roles.FindNotDeletedAsync(rolId, ct);
        if (role is null)
        {
            return false;
        }

        role.Estado = RolEstado.Eliminado;
        await _unitOfWork.SaveChangesAsync(ct);
        return true;
    }

    public async Task<OperationResult<IReadOnlyList<PermissionDto>>> GetRolePermissionsAsync(
        int rolId,
        CancellationToken ct)
    {
        var validKeys = _permissionCatalog.GetPermissionKeys();

        var roleExists = await _roles.FindNotDeletedAsync(rolId, ct) is not null;
        if (!roleExists)
        {
            return OperationResult<IReadOnlyList<PermissionDto>>.NotFound();
        }

        var permissions = await _roles.ListPermissionsAsync(rolId, ct);

        var available = permissions
            .Where(p => validKeys.Count == 0 || validKeys.Contains(PermissionKey.Build(p.Modulo, p.Accion)))
            .Select(p => new PermissionDto { Modulo = p.Modulo, Accion = p.Accion })
            .OrderBy(p => p.Modulo)
            .ThenBy(p => p.Accion)
            .ToArray();

        return OperationResult<IReadOnlyList<PermissionDto>>.Success(available);
    }

    public async Task<OperationResult> UpdateRolePermissionsAsync(
        int rolId,
        UpdateRolePermissionsRequest request,
        CancellationToken ct)
    {
        var role = await _roles.FindNotDeletedAsync(rolId, ct);
        if (role is null)
        {
            return OperationResult.NotFound();
        }

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
                return OperationResult.Validation(new Dictionary<string, string[]>
                {
                    [nameof(PermissionDto.Modulo)] = ["Modulo no puede exceder 120 caracteres."]
                });
            }

            if (accion.Length > 12)
            {
                return OperationResult.Validation(new Dictionary<string, string[]>
                {
                    [nameof(PermissionDto.Accion)] = ["Accion no puede exceder 12 caracteres."]
                });
            }
        }

        // Solo se persisten permisos que correspondan a pantallas/funcionalidades existentes.
        var validKeys = _permissionCatalog.GetPermissionKeys();
        if (validKeys.Count > 0)
        {
            normalized = normalized
                .Where(p => validKeys.Contains(PermissionKey.Build(p.Modulo, p.Accion)))
                .ToArray();
        }

        var selected = normalized
            .Select(p => (Modulo: p.Modulo ?? string.Empty, Accion: p.Accion ?? string.Empty))
            .ToArray();

        await _roles.ReplacePermissionsAsync(role.RolId, selected, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return OperationResult.Success();
    }

    public IReadOnlyList<PermissionCatalogItem> GetPermissionCatalog() =>
        _permissionCatalog.GetCatalog();

    private async Task<bool> SetUserStateAsync(
        int usuarioId,
        UsuarioEstado estado,
        CancellationToken ct)
    {
        var user = await _usuarios.FindNotDeletedByIdAsync(usuarioId, ct);
        if (user is null)
        {
            return false;
        }

        user.Estado = estado;
        user.ActualizadoEn = _timeProvider.GetUtcNow().UtcDateTime;
        await _unitOfWork.SaveChangesAsync(ct);
        return true;
    }

    private static bool TryNormalizeRole(
        UpsertRoleRequest request,
        out string nombre,
        out string? descripcion,
        out Dictionary<string, string[]> errors)
    {
        nombre = (request.Nombre ?? string.Empty).Trim();
        descripcion = null;
        errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(nombre))
        {
            errors[nameof(UpsertRoleRequest.Nombre)] = ["Nombre es requerido."];
            return false;
        }

        if (nombre.Length > 50)
        {
            errors[nameof(UpsertRoleRequest.Nombre)] = ["Nombre no puede exceder 50 caracteres."];
            return false;
        }

        var descripcionValue = (request.Descripcion ?? string.Empty).Trim();
        descripcion = string.IsNullOrWhiteSpace(descripcionValue) ? null : descripcionValue;

        if (descripcion is not null && descripcion.Length > 250)
        {
            errors[nameof(UpsertRoleRequest.Descripcion)] = ["Descripcion no puede exceder 250 caracteres."];
            return false;
        }

        return true;
    }
}
