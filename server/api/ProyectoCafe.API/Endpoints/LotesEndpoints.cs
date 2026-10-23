using Microsoft.AspNetCore.Mvc;
using ProyectoCafe.API.Auth;
using ProyectoCafe.Application.Inventario.Lotes;

namespace ProyectoCafe.API.Endpoints;

public static class LotesEndpoints
{
    public static IEndpointRouteBuilder MapLotesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        Map(endpoints, "/api/v1/lotes");
        Map(endpoints, "/api/Lotes"); // compatibilidad con Berry
        return endpoints;
    }

    private static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints
            .MapGroup(prefix)
            .WithTags("Lotes");

        group.MapGet("/", async (
            [FromQuery] int? productoId,
            [FromQuery] bool? soloActivos,
            [FromQuery] bool? soloConStock,
            [FromServices] ILotesService service,
            CancellationToken ct) =>
        {
            var list = await service.GetAllAsync(productoId, soloActivos == true, soloConStock == true, ct);
            return Results.Ok(list);
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.InventarioLotes, Permissions.Actions.Ver));

        group.MapGet("/resumen", async (
            [FromQuery] int? productoId,
            [FromQuery] bool? soloActivos,
            [FromQuery] bool? soloConStock,
            [FromServices] ILotesService service,
            CancellationToken ct) =>
        {
            var list = await service.GetSummaryAsync(productoId, soloActivos == true, soloConStock == true, ct);
            return Results.Ok(list);
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.InventarioLotes, Permissions.Actions.Ver));

        group.MapGet("/{id:int}", async (
            int id,
            [FromServices] ILotesService service,
            CancellationToken ct) =>
        {
            var item = await service.GetByIdAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        })
        .WithName($"{prefix}:GetById")
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.InventarioLotes, Permissions.Actions.Ver));

        group.MapGet("/{id:int}/trazabilidad", async (
            int id,
            [FromServices] ILotesService service,
            CancellationToken ct) =>
        {
            var item = await service.GetTraceabilityAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        })
        .WithName($"{prefix}:GetTraceability")
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.InventarioLotes, Permissions.Actions.Ver));
    }
}
