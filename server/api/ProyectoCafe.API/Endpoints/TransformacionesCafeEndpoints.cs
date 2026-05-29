using Microsoft.AspNetCore.Mvc;
using ProyectoCafe.API.Auth;
using ProyectoCafe.Application.Produccion.TransformacionesCafe;

namespace ProyectoCafe.API.Endpoints;

public static class TransformacionesCafeEndpoints
{
    public static IEndpointRouteBuilder MapTransformacionesCafeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        Map(endpoints, "/api/v1/produccion/transformaciones-cafe");
        Map(endpoints, "/api/Produccion/TransformacionesCafe");
        return endpoints;
    }

    private static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints
            .MapGroup(prefix)
            .WithTags("TransformacionesCafe");

        group.MapGet("/", async ([FromServices] ITransformacionesCafeService service, CancellationToken ct) =>
        {
            var list = await service.GetAllAsync(ct);
            return Results.Ok(list);
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ProduccionTransformacionesCafe, Permissions.Actions.Ver));

        group.MapGet("/{id:int}", async (
            int id,
            [FromServices] ITransformacionesCafeService service,
            CancellationToken ct) =>
        {
            var item = await service.GetByIdAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        })
        .WithName($"{prefix}:GetById")
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ProduccionTransformacionesCafe, Permissions.Actions.Ver));

        group.MapPost("/", async (
            [FromBody] TransformacionCafeCreateRequest request,
            [FromServices] ITransformacionesCafeService service,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var errors = Validate(request);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            TransformacionCafeCreateResult result;
            try
            {
                result = await service.CreateAsync(request, ct);
            }
            catch (ArgumentException ex)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = [ex.Message] });
            }

            var location = $"{httpContext.Request.Path}/{result.TransformacionCafeId}";
            return Results.Created(location, result);
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ProduccionTransformacionesCafe, Permissions.Actions.Crear));

        group.MapPost("/{id:int}/anular", async (
            int id,
            [FromServices] ITransformacionesCafeService service,
            CancellationToken ct) =>
        {
            bool canceled;
            try
            {
                canceled = await service.CancelAsync(id, ct);
            }
            catch (ArgumentException ex)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = [ex.Message] });
            }

            return canceled ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ProduccionTransformacionesCafe, Permissions.Actions.Eliminar));

        group.MapDelete("/{id:int}", async (
            int id,
            [FromServices] ITransformacionesCafeService service,
            CancellationToken ct) =>
        {
            bool canceled;
            try
            {
                canceled = await service.CancelAsync(id, ct);
            }
            catch (ArgumentException ex)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = [ex.Message] });
            }

            return canceled ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ProduccionTransformacionesCafe, Permissions.Actions.Eliminar));
    }

    private static Dictionary<string, string[]> Validate(TransformacionCafeCreateRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.LoteOrigenId <= 0)
        {
            errors[nameof(request.LoteOrigenId)] = ["LoteOrigenId es requerido."];
        }

        if (request.ProductoSalidaId <= 0)
        {
            errors[nameof(request.ProductoSalidaId)] = ["ProductoSalidaId es requerido."];
        }

        if (string.IsNullOrWhiteSpace(request.TipoTransformacion))
        {
            errors[nameof(request.TipoTransformacion)] = ["TipoTransformacion es requerido."];
        }

        if (request.CantidadEntrada <= 0)
        {
            errors[nameof(request.CantidadEntrada)] = ["CantidadEntrada debe ser mayor a 0."];
        }

        if (request.CantidadSalida <= 0)
        {
            errors[nameof(request.CantidadSalida)] = ["CantidadSalida debe ser mayor a 0."];
        }

        if (request.CostoProceso < 0)
        {
            errors[nameof(request.CostoProceso)] = ["CostoProceso no puede ser negativo."];
        }

        return errors;
    }
}
