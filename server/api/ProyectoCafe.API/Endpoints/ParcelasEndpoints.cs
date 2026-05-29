using Microsoft.AspNetCore.Mvc;
using ProyectoCafe.API.Auth;
using ProyectoCafe.Application.Produccion.Parcelas;

namespace ProyectoCafe.API.Endpoints;

public static class ParcelasEndpoints
{
    public static IEndpointRouteBuilder MapParcelasEndpoints(this IEndpointRouteBuilder endpoints)
    {
        Map(endpoints, "/api/v1/produccion/parcelas");
        Map(endpoints, "/api/Produccion/Parcelas");
        return endpoints;
    }

    private static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints
            .MapGroup(prefix)
            .WithTags("Parcelas");

        group.MapGet("/", async (
            [FromQuery] int? fincaId,
            [FromServices] IParcelasService service,
            CancellationToken ct) =>
        {
            var list = await service.GetAllAsync(fincaId, ct);
            return Results.Ok(list);
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ProduccionParcelas, Permissions.Actions.Ver));

        group.MapGet("/{id:int}", async (
            int id,
            [FromServices] IParcelasService service,
            CancellationToken ct) =>
        {
            var item = await service.GetByIdAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        })
        .WithName($"{prefix}:GetById")
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ProduccionParcelas, Permissions.Actions.Ver));

        group.MapPost("/", async (
            [FromBody] ParcelaRequest request,
            [FromServices] IParcelasService service,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var errors = Validate(request);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

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
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ProduccionParcelas, Permissions.Actions.Crear));

        group.MapPut("/{id:int}", async (
            int id,
            [FromBody] ParcelaRequest request,
            [FromServices] IParcelasService service,
            CancellationToken ct) =>
        {
            var errors = Validate(request);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

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
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ProduccionParcelas, Permissions.Actions.Editar));

        group.MapDelete("/{id:int}", async (
            int id,
            [FromServices] IParcelasService service,
            CancellationToken ct) =>
        {
            var deleted = await service.DeleteAsync(id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ProduccionParcelas, Permissions.Actions.Eliminar));
    }

    private static Dictionary<string, string[]> Validate(ParcelaRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.FincaId < 0)
        {
            errors[nameof(request.FincaId)] = ["FincaId es requerido."];
        }

        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            errors[nameof(request.Nombre)] = ["Nombre es requerido."];
        }

        if (!request.ProductoCosechaId.HasValue)
        {
            errors[nameof(request.ProductoCosechaId)] = ["ProductoCosechaId es requerido."];
        }

        if (request.AreaHectareas.HasValue && request.AreaHectareas.Value < 0)
        {
            errors[nameof(request.AreaHectareas)] = ["AreaHectareas no puede ser negativa."];
        }

        return errors;
    }
}
