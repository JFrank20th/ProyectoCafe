using Microsoft.AspNetCore.Mvc;
using ProyectoCafe.API.Auth;
using ProyectoCafe.Application.Common;
using ProyectoCafe.Application.DTOs.Administration;
using ProyectoCafe.Application.Services;

namespace ProyectoCafe.API.Endpoints;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/admin")
            .WithTags("Admin");

        group.MapGet("/users", async (
            [FromServices] AdministrationService administration,
            CancellationToken ct) =>
        {
            var users = await administration.GetUsersAsync(ct);
            return Results.Ok(users);
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.AdministracionUsuarios, Permissions.Actions.Ver));

        group.MapPost("/users/{id:int}/activate", async (
            int id,
            [FromServices] AdministrationService administration,
            CancellationToken ct) =>
        {
            var updated = await administration.ActivateUserAsync(id, ct);
            return updated ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.AdministracionUsuarios, Permissions.Actions.Editar));

        group.MapPost("/users/{id:int}/deactivate", async (
            int id,
            [FromServices] AdministrationService administration,
            CancellationToken ct) =>
        {
            var updated = await administration.DeactivateUserAsync(id, ct);
            return updated ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.AdministracionUsuarios, Permissions.Actions.Editar));

        group.MapDelete("/users/{id:int}", async (
            int id,
            [FromServices] AdministrationService administration,
            CancellationToken ct) =>
        {
            var deleted = await administration.DeleteUserAsync(id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.AdministracionUsuarios, Permissions.Actions.Eliminar));

        group.MapPut("/users/{id:int}/roles", async (
            int id,
            [FromBody] UpdateUserRolesRequest request,
            [FromServices] AdministrationService administration,
            CancellationToken ct) =>
        {
            var result = await administration.UpdateUserRolesAsync(id, request, ct);
            return result.IsSuccess ? Results.NoContent() : Failure(result);
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.AdministracionUsuarios, Permissions.Actions.Editar));

        group.MapGet("/roles", async (
            [FromServices] AdministrationService administration,
            CancellationToken ct) =>
        {
            var roles = await administration.GetRolesAsync(ct);
            return Results.Ok(roles);
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.AdministracionRoles, Permissions.Actions.Ver));

        group.MapPost("/roles", async (
            [FromBody] UpsertRoleRequest request,
            [FromServices] AdministrationService administration,
            HttpContext http,
            CancellationToken ct) =>
        {
            var result = await administration.CreateRoleAsync(request, ct);
            if (!result.IsSuccess)
            {
                return Failure(result);
            }

            var basePath = http.Request.Path.Value?.TrimEnd('/') ?? string.Empty;
            return Results.Created($"{basePath}/{result.Value}", new { rolId = result.Value });
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.AdministracionRoles, Permissions.Actions.Crear));

        group.MapPut("/roles/{id:int}", async (
            int id,
            [FromBody] UpsertRoleRequest request,
            [FromServices] AdministrationService administration,
            CancellationToken ct) =>
        {
            var result = await administration.UpdateRoleAsync(id, request, ct);
            return result.IsSuccess ? Results.NoContent() : Failure(result);
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.AdministracionRoles, Permissions.Actions.Editar));

        group.MapDelete("/roles/{id:int}", async (
            int id,
            [FromServices] AdministrationService administration,
            CancellationToken ct) =>
        {
            var deleted = await administration.DeleteRoleAsync(id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.AdministracionRoles, Permissions.Actions.Eliminar));

        group.MapGet("/roles/{id:int}/permissions", async (
            int id,
            [FromServices] AdministrationService administration,
            CancellationToken ct) =>
        {
            var result = await administration.GetRolePermissionsAsync(id, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Failure(result);
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.AdministracionPermisos, Permissions.Actions.Ver));

        group.MapPut("/roles/{id:int}/permissions", async (
            int id,
            [FromBody] UpdateRolePermissionsRequest request,
            [FromServices] AdministrationService administration,
            CancellationToken ct) =>
        {
            var result = await administration.UpdateRolePermissionsAsync(id, request, ct);
            return result.IsSuccess ? Results.NoContent() : Failure(result);
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.AdministracionPermisos, Permissions.Actions.Editar));

        group.MapGet("/permissions/catalog", (
            [FromServices] AdministrationService administration) =>
        {
            var data = administration.GetPermissionCatalog()
                .Select(m => new
                {
                    modulo = m.Modulo,
                    acciones = m.Acciones
                })
                .ToArray();

            return Results.Ok(data);
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.AdministracionPermisos, Permissions.Actions.Ver));

        return endpoints;
    }

    private static IResult Failure(OperationResult result) =>
        result.Status switch
        {
            OperationStatus.NotFound => Results.NotFound(),
            OperationStatus.ValidationFailed => Results.ValidationProblem(
                result.Errors.ToDictionary(pair => pair.Key, pair => pair.Value)),
            _ => Results.Problem(),
        };
}
