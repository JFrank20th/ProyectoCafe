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

        group.MapPost("/", async (
            [FromBody] LoteRequest request,
            [FromServices] ILotesService service,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var errors = new Dictionary<string, string[]>();

            if (request.ProductoId <= 0)
            {
                errors[nameof(request.ProductoId)] = ["ProductoId es requerido."];
            }

            var codigo = (request.CodigoLote ?? string.Empty).Trim();
            if (!string.IsNullOrEmpty(codigo) && codigo.Length > 50)
            {
                errors[nameof(request.CodigoLote)] = ["CodigoLote no puede exceder 50 caracteres."];
            }

            if (request.CantidadInicial < 0)
            {
                errors[nameof(request.CantidadInicial)] = ["CantidadInicial no puede ser negativa."];
            }

            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            request.CodigoLote = string.IsNullOrWhiteSpace(codigo) ? null : codigo.Trim().ToUpperInvariant();
            request.Estado = string.IsNullOrWhiteSpace(request.Estado) ? null : request.Estado.Trim();
            request.TipoLoteOverride = string.IsNullOrWhiteSpace(request.TipoLoteOverride) ? null : request.TipoLoteOverride.Trim();

            int newId;
            try
            {
                newId = await service.CreateAsync(request, ct);
            }
            catch (ArgumentException ex)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = [ex.Message] });
            }

            var location = $"{httpContext.Request.Path}/{newId}";
            return Results.Created(location, new { id = newId });
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.InventarioLotes, Permissions.Actions.Crear));

        group.MapPut("/{id:int}", async (
            int id,
            [FromBody] LoteUpdateRequest request,
            [FromServices] ILotesService service,
            CancellationToken ct) =>
        {
            var errors = new Dictionary<string, string[]>();

            var codigo = (request.CodigoLote ?? string.Empty).Trim();
            if (!string.IsNullOrEmpty(codigo) && codigo.Length > 50)
            {
                errors[nameof(request.CodigoLote)] = ["CodigoLote no puede exceder 50 caracteres."];
            }

            var estado = (request.Estado ?? string.Empty).Trim();
            if (!string.IsNullOrEmpty(estado) && estado.Length > 20)
            {
                errors[nameof(request.Estado)] = ["Estado no puede exceder 20 caracteres."];
            }

            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            request.CodigoLote = string.IsNullOrWhiteSpace(codigo) ? null : codigo.Trim().ToUpperInvariant();
            request.Estado = string.IsNullOrWhiteSpace(estado) ? null : estado;

            bool updated;
            try
            {
                updated = await service.UpdateAsync(id, request, ct);
            }
            catch (ArgumentException ex)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = [ex.Message] });
            }

            return updated ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.InventarioLotes, Permissions.Actions.Editar));

        group.MapDelete("/{id:int}", async (
            int id,
            [FromServices] ILotesService service,
            CancellationToken ct) =>
        {
            var deleted = await service.DeleteAsync(id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.InventarioLotes, Permissions.Actions.Eliminar));
    }
}
