using Microsoft.AspNetCore.Mvc;
using ProyectoCafe.API.Auth;
using ProyectoCafe.Application.Contabilidad.Pasivos;

namespace ProyectoCafe.API.Endpoints;

public static class PasivosEndpoints
{
    public static IEndpointRouteBuilder MapPasivosEndpoints(this IEndpointRouteBuilder endpoints)
    {
        Map(endpoints, "/api/v1/pasivos");
        Map(endpoints, "/api/Pasivos"); // compatibilidad con Berry
        return endpoints;
    }

    private static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints
            .MapGroup(prefix)
            .WithTags("Pasivos");

        group.MapGet("/", async ([FromServices] IPasivosService service, CancellationToken ct) =>
        {
            var list = await service.GetAllAsync(ct);
            return Results.Ok(list);
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ContabilidadPasivos, Permissions.Actions.Ver));

        group.MapGet("/{id:int}", async (
            int id,
            [FromServices] IPasivosService service,
            CancellationToken ct) =>
        {
            var item = await service.GetByIdAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        })
        .WithName($"{prefix}:GetById")
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ContabilidadPasivos, Permissions.Actions.Ver));

        group.MapPost("/", async (
            [FromBody] PasivoRequest request,
            [FromServices] IPasivosService service,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var errors = Validate(request);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            Normalize(request);

            int newId;
            try
            {
                newId = await service.CreateAsync(request, ct);
            }
            catch (ArgumentException ex)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = [ex.Message] });
            }
            var basePath = httpContext.Request.Path.Value?.TrimEnd('/') ?? string.Empty;
            var location = $"{basePath}/{newId}";
            return Results.Created(location, new { id = newId });
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ContabilidadPasivos, Permissions.Actions.Crear));

        group.MapPut("/{id:int}", async (
            int id,
            [FromBody] PasivoRequest request,
            [FromServices] IPasivosService service,
            CancellationToken ct) =>
        {
            var errors = Validate(request);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            Normalize(request);

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
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ContabilidadPasivos, Permissions.Actions.Editar));

        group.MapDelete("/{id:int}", async (
            int id,
            [FromServices] IPasivosService service,
            CancellationToken ct) =>
        {
            var deleted = await service.DeleteAsync(id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ContabilidadPasivos, Permissions.Actions.Eliminar));
    }

    private static Dictionary<string, string[]> Validate(PasivoRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        var nombre = (request.Nombre ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(nombre))
        {
            errors[nameof(request.Nombre)] = ["Nombre es requerido."];
        }
        else if (nombre.Length > 150)
        {
            errors[nameof(request.Nombre)] = ["Nombre no puede exceder 150 caracteres."];
        }

        if (request.Monto < 0)
        {
            errors[nameof(request.Monto)] = ["Monto no puede ser negativo."];
        }

        return errors;
    }

    private static void Normalize(PasivoRequest request)
    {
        request.Nombre = (request.Nombre ?? string.Empty).Trim();
    }
}
