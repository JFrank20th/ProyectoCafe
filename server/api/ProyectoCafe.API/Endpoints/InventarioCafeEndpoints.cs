using Microsoft.AspNetCore.Mvc;
using ProyectoCafe.API.Auth;
using ProyectoCafe.Application.Common;
using ProyectoCafe.Application.DTOs.Inventario;
using ProyectoCafe.Application.Services;

namespace ProyectoCafe.API.Endpoints;

public static class InventarioCafeEndpoints
{
    public static IEndpointRouteBuilder MapInventarioCafeEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/inventario")
            .WithTags("Inventario");

        MapLotes(group);
        MapExistencias(group);
        MapMovimientos(group);
        MapPresentaciones(group);
        MapTiposCafe(group);

        return endpoints;
    }

    private static void MapLotes(IEndpointRouteBuilder group)
    {
        group.MapGet("/lotes", async (
            [FromServices] LoteCafeService service,
            CancellationToken ct) =>
        {
            var lotes = await service.GetLotesAsync(ct);
            return Results.Ok(lotes);
        })
        .RequireAuthorization(Permissions.Policy(
            Permissions.Modulos.InventarioLotes,
            Permissions.Actions.Ver));

        group.MapGet("/lotes/{id:int}", async (
            int id,
            [FromServices] LoteCafeService service,
            CancellationToken ct) =>
        {
            var result = await service.GetLoteAsync(id, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Failure(result);
        })
        .RequireAuthorization(Permissions.Policy(
            Permissions.Modulos.InventarioLotes,
            Permissions.Actions.Ver));

        group.MapPost("/lotes", async (
            [FromBody] CreateLoteCafeRequest request,
            [FromServices] LoteCafeService service,
            CancellationToken ct) =>
        {
            var result = await service.CreateLoteAsync(request, ct);
            if (!result.IsSuccess)
            {
                return Failure(result);
            }

            return Results.Created(
                $"/api/v1/inventario/lotes/{result.Value}",
                new { loteCafeId = result.Value });
        })
        .RequireAuthorization(Permissions.Policy(
            Permissions.Modulos.InventarioLotes,
            Permissions.Actions.Crear));
    }

    private static void MapExistencias(IEndpointRouteBuilder group)
    {
        group.MapGet("/lotes/{id:int}/existencias", async (
            int id,
            [FromServices] LoteCafeService service,
            CancellationToken ct) =>
        {
            var result = await service.GetExistenciasAsync(id, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Failure(result);
        })
        .RequireAuthorization(Permissions.Policy(
            Permissions.Modulos.InventarioExistencias,
            Permissions.Actions.Ver));
    }

    private static void MapMovimientos(IEndpointRouteBuilder group)
    {
        group.MapGet("/lotes/{id:int}/movimientos", async (
            int id,
            [FromServices] LoteCafeService service,
            CancellationToken ct) =>
        {
            var result = await service.GetMovimientosAsync(id, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Failure(result);
        })
        .RequireAuthorization(Permissions.Policy(
            Permissions.Modulos.InventarioMovimientos,
            Permissions.Actions.Ver));

        group.MapPost("/lotes/{id:int}/movimientos", async (
            int id,
            [FromBody] RegistrarMovimientoRequest request,
            [FromServices] LoteCafeService service,
            HttpContext http,
            CancellationToken ct) =>
        {
            var uid = http.User.FindFirst("uid")?.Value;
            if (!int.TryParse(uid, out var usuarioId))
            {
                return Results.Unauthorized();
            }

            var result = await service.RegisterMovementAsync(id, request, usuarioId, ct);
            if (!result.IsSuccess)
            {
                return Failure(result);
            }

            return Results.Created(
                $"/api/v1/inventario/lotes/{id}/movimientos/{result.Value!.MovimientoLoteCafeId}",
                result.Value);
        })
        .RequireAuthorization(Permissions.Policy(
            Permissions.Modulos.InventarioMovimientos,
            Permissions.Actions.Crear));
    }

    private static void MapPresentaciones(IEndpointRouteBuilder group)
    {
        group.MapGet("/presentaciones", async (
            [FromServices] CatalogoCafeService service,
            CancellationToken ct) =>
        {
            var presentaciones = await service.GetPresentacionesAsync(ct);
            return Results.Ok(presentaciones);
        })
        .RequireAuthorization(Permissions.Policy(
            Permissions.Modulos.InventarioPresentaciones,
            Permissions.Actions.Ver));

        group.MapGet("/presentaciones/{id:int}", async (
            int id,
            [FromServices] CatalogoCafeService service,
            CancellationToken ct) =>
        {
            var result = await service.GetPresentacionAsync(id, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Failure(result);
        })
        .RequireAuthorization(Permissions.Policy(
            Permissions.Modulos.InventarioPresentaciones,
            Permissions.Actions.Ver));

        group.MapPost("/presentaciones", async (
            [FromBody] UpsertPresentacionCafeRequest request,
            [FromServices] CatalogoCafeService service,
            CancellationToken ct) =>
        {
            var result = await service.CreatePresentacionAsync(request, ct);
            if (!result.IsSuccess)
            {
                return Failure(result);
            }

            return Results.Created(
                $"/api/v1/inventario/presentaciones/{result.Value}",
                new { presentacionCafeId = result.Value });
        })
        .RequireAuthorization(Permissions.Policy(
            Permissions.Modulos.InventarioPresentaciones,
            Permissions.Actions.Crear));

        group.MapPut("/presentaciones/{id:int}", async (
            int id,
            [FromBody] UpsertPresentacionCafeRequest request,
            [FromServices] CatalogoCafeService service,
            CancellationToken ct) =>
        {
            var result = await service.UpdatePresentacionAsync(id, request, ct);
            return result.IsSuccess ? Results.NoContent() : Failure(result);
        })
        .RequireAuthorization(Permissions.Policy(
            Permissions.Modulos.InventarioPresentaciones,
            Permissions.Actions.Editar));

        group.MapDelete("/presentaciones/{id:int}", async (
            int id,
            [FromServices] CatalogoCafeService service,
            CancellationToken ct) =>
        {
            var result = await service.DeletePresentacionAsync(id, ct);
            return result.IsSuccess ? Results.NoContent() : Failure(result);
        })
        .RequireAuthorization(Permissions.Policy(
            Permissions.Modulos.InventarioPresentaciones,
            Permissions.Actions.Eliminar));
    }

    private static void MapTiposCafe(IEndpointRouteBuilder group)
    {
        group.MapGet("/tipos-cafe", async (
            [FromServices] CatalogoCafeService service,
            CancellationToken ct) =>
        {
            var tipos = await service.GetTiposCafeAsync(ct);
            return Results.Ok(tipos);
        })
        .RequireAuthorization(Permissions.Policy(
            Permissions.Modulos.InventarioTiposCafe,
            Permissions.Actions.Ver));

        group.MapGet("/tipos-cafe/{id:int}", async (
            int id,
            [FromServices] CatalogoCafeService service,
            CancellationToken ct) =>
        {
            var result = await service.GetTipoCafeAsync(id, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Failure(result);
        })
        .RequireAuthorization(Permissions.Policy(
            Permissions.Modulos.InventarioTiposCafe,
            Permissions.Actions.Ver));

        group.MapPost("/tipos-cafe", async (
            [FromBody] UpsertTipoCafeRequest request,
            [FromServices] CatalogoCafeService service,
            CancellationToken ct) =>
        {
            var result = await service.CreateTipoCafeAsync(request, ct);
            if (!result.IsSuccess)
            {
                return Failure(result);
            }

            return Results.Created(
                $"/api/v1/inventario/tipos-cafe/{result.Value}",
                new { tipoCafeId = result.Value });
        })
        .RequireAuthorization(Permissions.Policy(
            Permissions.Modulos.InventarioTiposCafe,
            Permissions.Actions.Crear));

        group.MapPut("/tipos-cafe/{id:int}", async (
            int id,
            [FromBody] UpsertTipoCafeRequest request,
            [FromServices] CatalogoCafeService service,
            CancellationToken ct) =>
        {
            var result = await service.UpdateTipoCafeAsync(id, request, ct);
            return result.IsSuccess ? Results.NoContent() : Failure(result);
        })
        .RequireAuthorization(Permissions.Policy(
            Permissions.Modulos.InventarioTiposCafe,
            Permissions.Actions.Editar));

        group.MapDelete("/tipos-cafe/{id:int}", async (
            int id,
            [FromServices] CatalogoCafeService service,
            CancellationToken ct) =>
        {
            var result = await service.DeleteTipoCafeAsync(id, ct);
            return result.IsSuccess ? Results.NoContent() : Failure(result);
        })
        .RequireAuthorization(Permissions.Policy(
            Permissions.Modulos.InventarioTiposCafe,
            Permissions.Actions.Eliminar));
    }

    private static IResult Failure(OperationResult result) =>
        result.Status switch
        {
            OperationStatus.NotFound => Results.NotFound(),
            OperationStatus.ValidationFailed => Results.ValidationProblem(
                result.Errors.ToDictionary(pair => pair.Key, pair => pair.Value)),
            OperationStatus.Conflict => Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflicto de concurrencia",
                detail: "El inventario fue modificado por otra operación. Vuelva a consultar e intentar de nuevo."),
            _ => Results.Problem(),
        };
}
