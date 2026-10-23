using Microsoft.AspNetCore.Mvc;
using ProyectoCafe.API.Auth;
using ProyectoCafe.Application.Produccion.Cultivos;

namespace ProyectoCafe.API.Endpoints;

public static class CultivosEndpoints
{
    public static IEndpointRouteBuilder MapCultivosEndpoints(this IEndpointRouteBuilder endpoints)
    {
        Map(endpoints, "/api/v1/produccion/cultivos");
        Map(endpoints, "/api/Produccion/Cultivos");
        return endpoints;
    }

    private static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints
            .MapGroup(prefix)
            .WithTags("Cultivos");

        group.MapGet("/", async (
            [FromQuery] int? fincaId,
            [FromServices] ICultivosService service,
            CancellationToken ct) =>
        {
            var list = await service.GetAllAsync(fincaId, ct);
            return Results.Ok(list);
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ProduccionCultivos, Permissions.Actions.Ver));

        group.MapGet("/{id:int}", async (
            int id,
            [FromServices] ICultivosService service,
            CancellationToken ct) =>
        {
            var item = await service.GetByIdAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        })
        .WithName($"{prefix}:GetById")
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ProduccionCultivos, Permissions.Actions.Ver));

        group.MapPost("/", async (
            [FromBody] CultivoRequest request,
            [FromServices] ICultivosService service,
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
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ProduccionCultivos, Permissions.Actions.Crear));

        group.MapPut("/{id:int}", async (
            int id,
            [FromBody] CultivoRequest request,
            [FromServices] ICultivosService service,
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
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ProduccionCultivos, Permissions.Actions.Editar));

        group.MapDelete("/{id:int}", async (
            int id,
            [FromServices] ICultivosService service,
            CancellationToken ct) =>
        {
            try
            {
                var deleted = await service.DeleteAsync(id, ct);
                return deleted ? Results.NoContent() : Results.NotFound();
            }
            catch (ArgumentException ex)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = [ex.Message] });
            }
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ProduccionCultivos, Permissions.Actions.Eliminar));
    }

    private static Dictionary<string, string[]> Validate(CultivoRequest request)
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

        if (string.IsNullOrWhiteSpace(request.Descripcion))
        {
            errors[nameof(request.Descripcion)] = ["Descripcion es requerida."];
        }

        if (request.ConfiguracionesCosecha is null || request.ConfiguracionesCosecha.Count == 0)
        {
            errors[nameof(request.ConfiguracionesCosecha)] = ["Debes configurar al menos un tipo de cosecha."];
        }

        if (!request.AreaHectareas.HasValue)
        {
            errors[nameof(request.AreaHectareas)] = ["AreaHectareas es requerida."];
        }

        if (request.AreaHectareas.HasValue && request.AreaHectareas.Value < 0)
        {
            errors[nameof(request.AreaHectareas)] = ["AreaHectareas no puede ser negativa."];
        }

        if (request.ConfiguracionesCosecha is not null)
        {
            for (var index = 0; index < request.ConfiguracionesCosecha.Count; index++)
            {
                var item = request.ConfiguracionesCosecha[index];
                if (string.IsNullOrWhiteSpace(item.NombreTipoCosecha))
                {
                    errors[$"{nameof(request.ConfiguracionesCosecha)}[{index}].{nameof(item.NombreTipoCosecha)}"] = ["NombreTipoCosecha es requerido."];
                }

                if (string.IsNullOrWhiteSpace(item.UnidadMedidaCodigo))
                {
                    errors[$"{nameof(request.ConfiguracionesCosecha)}[{index}].{nameof(item.UnidadMedidaCodigo)}"] = ["UnidadMedidaCodigo es requerido."];
                }
            }
        }

        return errors;
    }
}
