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
        MapVariedades(group);
        MapTiposTueste(group);
        MapTiposProceso(group);

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

    private static void MapVariedades(IEndpointRouteBuilder group)
    {
        group.MapGet("/variedades", async (
            [FromServices] CatalogoCafeService service,
            CancellationToken ct) =>
        {
            var variedades = await service.GetVariedadesAsync(ct);
            return Results.Ok(variedades);
        })
        .RequireAuthorization(Permissions.Policy(
            Permissions.Modulos.InventarioVariedades,
            Permissions.Actions.Ver));

        group.MapGet("/variedades/{id:int}", async (
            int id,
            [FromServices] CatalogoCafeService service,
            CancellationToken ct) =>
        {
            var result = await service.GetVariedadAsync(id, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Failure(result);
        })
        .RequireAuthorization(Permissions.Policy(
            Permissions.Modulos.InventarioVariedades,
            Permissions.Actions.Ver));

        group.MapPost("/variedades", async (
            [FromBody] UpsertVariedadCafeRequest request,
            [FromServices] CatalogoCafeService service,
            CancellationToken ct) =>
        {
            var result = await service.CreateVariedadAsync(request, ct);
            if (!result.IsSuccess)
            {
                return Failure(result);
            }

            return Results.Created(
                $"/api/v1/inventario/variedades/{result.Value}",
                new { variedadCafeId = result.Value });
        })
        .RequireAuthorization(Permissions.Policy(
            Permissions.Modulos.InventarioVariedades,
            Permissions.Actions.Crear));

        group.MapPut("/variedades/{id:int}", async (
            int id,
            [FromBody] UpsertVariedadCafeRequest request,
            [FromServices] CatalogoCafeService service,
            CancellationToken ct) =>
        {
            var result = await service.UpdateVariedadAsync(id, request, ct);
            return result.IsSuccess ? Results.NoContent() : Failure(result);
        })
        .RequireAuthorization(Permissions.Policy(
            Permissions.Modulos.InventarioVariedades,
            Permissions.Actions.Editar));

        group.MapDelete("/variedades/{id:int}", async (
            int id,
            [FromServices] CatalogoCafeService service,
            CancellationToken ct) =>
        {
            var result = await service.DeleteVariedadAsync(id, ct);
            return result.IsSuccess ? Results.NoContent() : Failure(result);
        })
        .RequireAuthorization(Permissions.Policy(
            Permissions.Modulos.InventarioVariedades,
            Permissions.Actions.Eliminar));
    }

    private static void MapTiposTueste(IEndpointRouteBuilder group)
    {
        group.MapGet("/tipos-tueste", async (
            [FromServices] CatalogoCafeService service,
            CancellationToken ct) =>
        {
            var tiposTueste = await service.GetTiposTuesteAsync(ct);
            return Results.Ok(tiposTueste);
        })
        .RequireAuthorization(Permissions.Policy(
            Permissions.Modulos.InventarioTiposTueste,
            Permissions.Actions.Ver));

        group.MapGet("/tipos-tueste/{id:int}", async (
            int id,
            [FromServices] CatalogoCafeService service,
            CancellationToken ct) =>
        {
            var result = await service.GetTipoTuesteAsync(id, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Failure(result);
        })
        .RequireAuthorization(Permissions.Policy(
            Permissions.Modulos.InventarioTiposTueste,
            Permissions.Actions.Ver));

        group.MapPost("/tipos-tueste", async (
            [FromBody] UpsertTipoTuesteCafeRequest request,
            [FromServices] CatalogoCafeService service,
            CancellationToken ct) =>
        {
            var result = await service.CreateTipoTuesteAsync(request, ct);
            if (!result.IsSuccess)
            {
                return Failure(result);
            }

            return Results.Created(
                $"/api/v1/inventario/tipos-tueste/{result.Value}",
                new { tipoTuesteCafeId = result.Value });
        })
        .RequireAuthorization(Permissions.Policy(
            Permissions.Modulos.InventarioTiposTueste,
            Permissions.Actions.Crear));

        group.MapPut("/tipos-tueste/{id:int}", async (
            int id,
            [FromBody] UpsertTipoTuesteCafeRequest request,
            [FromServices] CatalogoCafeService service,
            CancellationToken ct) =>
        {
            var result = await service.UpdateTipoTuesteAsync(id, request, ct);
            return result.IsSuccess ? Results.NoContent() : Failure(result);
        })
        .RequireAuthorization(Permissions.Policy(
            Permissions.Modulos.InventarioTiposTueste,
            Permissions.Actions.Editar));

        group.MapDelete("/tipos-tueste/{id:int}", async (
            int id,
            [FromServices] CatalogoCafeService service,
            CancellationToken ct) =>
        {
            var result = await service.DeleteTipoTuesteAsync(id, ct);
            return result.IsSuccess ? Results.NoContent() : Failure(result);
        })
        .RequireAuthorization(Permissions.Policy(
            Permissions.Modulos.InventarioTiposTueste,
            Permissions.Actions.Eliminar));
    }

    private static void MapTiposProceso(IEndpointRouteBuilder group)
    {
        group.MapGet("/tipos-proceso", async (
            [FromServices] CatalogoCafeService service,
            CancellationToken ct) =>
        {
            var tiposProceso = await service.GetTiposProcesoAsync(ct);
            return Results.Ok(tiposProceso);
        })
        .RequireAuthorization(Permissions.Policy(
            Permissions.Modulos.InventarioTiposProceso,
            Permissions.Actions.Ver));

        group.MapGet("/tipos-proceso/{id:int}", async (
            int id,
            [FromServices] CatalogoCafeService service,
            CancellationToken ct) =>
        {
            var result = await service.GetTipoProcesoAsync(id, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Failure(result);
        })
        .RequireAuthorization(Permissions.Policy(
            Permissions.Modulos.InventarioTiposProceso,
            Permissions.Actions.Ver));

        group.MapPost("/tipos-proceso", async (
            [FromBody] UpsertTipoProcesoCafeRequest request,
            [FromServices] CatalogoCafeService service,
            CancellationToken ct) =>
        {
            var result = await service.CreateTipoProcesoAsync(request, ct);
            if (!result.IsSuccess)
            {
                return Failure(result);
            }

            return Results.Created(
                $"/api/v1/inventario/tipos-proceso/{result.Value}",
                new { tipoProcesoCafeId = result.Value });
        })
        .RequireAuthorization(Permissions.Policy(
            Permissions.Modulos.InventarioTiposProceso,
            Permissions.Actions.Crear));

        group.MapPut("/tipos-proceso/{id:int}", async (
            int id,
            [FromBody] UpsertTipoProcesoCafeRequest request,
            [FromServices] CatalogoCafeService service,
            CancellationToken ct) =>
        {
            var result = await service.UpdateTipoProcesoAsync(id, request, ct);
            return result.IsSuccess ? Results.NoContent() : Failure(result);
        })
        .RequireAuthorization(Permissions.Policy(
            Permissions.Modulos.InventarioTiposProceso,
            Permissions.Actions.Editar));

        group.MapDelete("/tipos-proceso/{id:int}", async (
            int id,
            [FromServices] CatalogoCafeService service,
            CancellationToken ct) =>
        {
            var result = await service.DeleteTipoProcesoAsync(id, ct);
            return result.IsSuccess ? Results.NoContent() : Failure(result);
        })
        .RequireAuthorization(Permissions.Policy(
            Permissions.Modulos.InventarioTiposProceso,
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
