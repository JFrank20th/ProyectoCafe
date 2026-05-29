using Microsoft.AspNetCore.Mvc;
using ProyectoCafe.API.Auth;
using ProyectoCafe.Application.Produccion.OperacionesAgricolas;

namespace ProyectoCafe.API.Endpoints;

public static class OperacionesAgricolasEndpoints
{
    public static IEndpointRouteBuilder MapOperacionesAgricolasEndpoints(this IEndpointRouteBuilder endpoints)
    {
        Map(endpoints, "/api/v1/produccion/operaciones-agricolas");
        Map(endpoints, "/api/Produccion/OperacionesAgricolas");
        return endpoints;
    }

    private static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints
            .MapGroup(prefix)
            .WithTags("OperacionesAgricolas");

        group.MapGet("/", async ([FromServices] IOperacionesAgricolasService service, CancellationToken ct) =>
        {
            var list = await service.GetAllAsync(ct);
            return Results.Ok(list);
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ProduccionOperacionesAgricolas, Permissions.Actions.Ver));

        group.MapGet("/{id:int}", async (
            int id,
            [FromServices] IOperacionesAgricolasService service,
            CancellationToken ct) =>
        {
            var item = await service.GetByIdAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        })
        .WithName($"{prefix}:GetById")
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ProduccionOperacionesAgricolas, Permissions.Actions.Ver));

        group.MapPost("/", async (
            [FromBody] OperacionAgricolaCreateRequest request,
            [FromServices] IOperacionesAgricolasService service,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var errors = Validate(request);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            OperacionAgricolaCreateResult result;
            try
            {
                result = await service.CreateAsync(request, ct);
            }
            catch (ArgumentException ex)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = [ex.Message] });
            }

            var location = $"{httpContext.Request.Path}/{result.OperacionAgricolaId}";
            return Results.Created(location, result);
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ProduccionOperacionesAgricolas, Permissions.Actions.Crear));

        group.MapPost("/{id:int}/anular", async (
            int id,
            [FromServices] IOperacionesAgricolasService service,
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
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ProduccionOperacionesAgricolas, Permissions.Actions.Eliminar));

        group.MapDelete("/{id:int}", async (
            int id,
            [FromServices] IOperacionesAgricolasService service,
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
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ProduccionOperacionesAgricolas, Permissions.Actions.Eliminar));
    }

    private static Dictionary<string, string[]> Validate(OperacionAgricolaCreateRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        var tipoOperacion = TiposOperacionAgricola.Normalize(request.TipoOperacion);
        var esCosecha = string.Equals(tipoOperacion, TiposOperacionAgricola.Cosecha, StringComparison.Ordinal);

        if (request.ParcelaId <= 0)
        {
            errors[nameof(request.ParcelaId)] = ["ParcelaId es requerido."];
        }

        if (tipoOperacion is null)
        {
            errors[nameof(request.TipoOperacion)] = ["TipoOperacion es inválido."];
        }

        if (request.InsumoId.HasValue && request.InsumoId.Value <= 0)
        {
            errors[nameof(request.InsumoId)] = ["InsumoId debe ser mayor a 0."];
        }

        if (request.CantidadInsumo.HasValue && request.CantidadInsumo.Value < 0)
        {
            errors[nameof(request.CantidadInsumo)] = ["CantidadInsumo no puede ser negativa."];
        }

        if (request.CostoManoObra < 0)
        {
            errors[nameof(request.CostoManoObra)] = ["CostoManoObra no puede ser negativo."];
        }

        if (request.CostoOtros < 0)
        {
            errors[nameof(request.CostoOtros)] = ["CostoOtros no puede ser negativo."];
        }

        if (esCosecha)
        {
            if (!request.Cantidad.HasValue || request.Cantidad.Value <= 0)
            {
                errors[nameof(request.Cantidad)] = ["Cantidad debe ser mayor a 0 para una cosecha."];
            }

            if (request.InsumoId.HasValue)
            {
                errors[nameof(request.InsumoId)] = ["Una cosecha no utiliza insumoId."];
            }

            if (request.CantidadInsumo.HasValue)
            {
                errors[nameof(request.CantidadInsumo)] = ["Una cosecha no utiliza cantidadInsumo."];
            }
        }

        return errors;
    }
}
