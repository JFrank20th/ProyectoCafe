using Microsoft.AspNetCore.Mvc;
using ProyectoCafe.API.Auth;
using ProyectoCafe.Application.Compras.RecepcionesCafe;

namespace ProyectoCafe.API.Endpoints;

public static class RecepcionesCafeEndpoints
{
    public static IEndpointRouteBuilder MapRecepcionesCafeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        Map(endpoints, "/api/v1/compras/recepciones-cafe");
        Map(endpoints, "/api/Compras/RecepcionesCafe");
        return endpoints;
    }

    private static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints
            .MapGroup(prefix)
            .WithTags("RecepcionesCafe");

        group.MapGet("/", async ([FromServices] IRecepcionesCafeService service, CancellationToken ct) =>
        {
            var list = await service.GetAllAsync(ct);
            return Results.Ok(list);
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ComprasRecepcionesCafe, Permissions.Actions.Ver));

        group.MapGet("/{id:int}", async (
            int id,
            [FromServices] IRecepcionesCafeService service,
            CancellationToken ct) =>
        {
            var item = await service.GetByIdAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        })
        .WithName($"{prefix}:GetById")
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ComprasRecepcionesCafe, Permissions.Actions.Ver));

        group.MapPost("/", async (
            [FromBody] RecepcionCafeCreateRequest request,
            [FromServices] IRecepcionesCafeService service,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var errors = Validate(request);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            RecepcionCafeCreateResult result;
            try
            {
                result = await service.CreateAsync(request, ct);
            }
            catch (ArgumentException ex)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = [ex.Message] });
            }

            var location = $"{httpContext.Request.Path}/{result.RecepcionCafeId}";
            return Results.Created(location, result);
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ComprasRecepcionesCafe, Permissions.Actions.Crear));

        group.MapPost("/{id:int}/anular", async (
            int id,
            [FromServices] IRecepcionesCafeService service,
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
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ComprasRecepcionesCafe, Permissions.Actions.Eliminar));

        group.MapDelete("/{id:int}", async (
            int id,
            [FromServices] IRecepcionesCafeService service,
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
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ComprasRecepcionesCafe, Permissions.Actions.Eliminar));
    }

    private static Dictionary<string, string[]> Validate(RecepcionCafeCreateRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.ProveedorId <= 0)
        {
            errors[nameof(request.ProveedorId)] = ["ProveedorId es requerido."];
        }

        if (request.ProductoId <= 0)
        {
            errors[nameof(request.ProductoId)] = ["ProductoId es requerido."];
        }

        if (TiposCafeCompra.Normalize(request.TipoCafe) is null)
        {
            errors[nameof(request.TipoCafe)] = ["TipoCafe es inválido. Valores permitidos: Cereza, RecienLavado, Seco."];
        }

        if (request.Cantidad <= 0)
        {
            errors[nameof(request.Cantidad)] = ["Cantidad debe ser mayor a 0."];
        }

        if (request.PrecioUnitario < 0)
        {
            errors[nameof(request.PrecioUnitario)] = ["PrecioUnitario no puede ser negativo."];
        }

        if (request.Humedad.HasValue && (request.Humedad.Value < 0 || request.Humedad.Value > 100))
        {
            errors[nameof(request.Humedad)] = ["Humedad debe estar entre 0 y 100."];
        }

        return errors;
    }
}
