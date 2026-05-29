using System.Net;
using Microsoft.AspNetCore.Mvc;
using ProyectoCafe.API.Auth;
using ProyectoCafe.Application.Inventario.UnidadesMedida;

namespace ProyectoCafe.API.Endpoints;

public static class UnidadesMedidaEndpoints
{
    public static IEndpointRouteBuilder MapUnidadesMedidaEndpoints(this IEndpointRouteBuilder endpoints)
    {
        Map(endpoints, "/api/v1/unidades-medida");
        Map(endpoints, "/api/UnidadesMedida"); // compatibilidad con Berry
        return endpoints;
    }

    private static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints
            .MapGroup(prefix)
            .WithTags("UnidadesMedida");

        group.MapGet("/", async ([FromServices] IUnidadesMedidaService service, CancellationToken ct) =>
        {
            var list = await service.GetAllAsync(ct);
            return Results.Ok(list);
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.InventarioUnidadesMedida, Permissions.Actions.Ver));

        group.MapGet("/{codigo}", async (
            string codigo,
            [FromServices] IUnidadesMedidaService service,
            CancellationToken ct) =>
        {
            var item = await service.GetByCodigoAsync(codigo, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        })
        .WithName($"{prefix}:GetByCodigo")
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.InventarioUnidadesMedida, Permissions.Actions.Ver));

        group.MapPost("/", async (
            [FromBody] UnidadMedidaRequest request,
            [FromServices] IUnidadesMedidaService service,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var codigo = (request.Codigo ?? string.Empty).Trim().ToUpperInvariant();
            var nombre = (request.Nombre ?? string.Empty).Trim();

            var errors = new Dictionary<string, string[]>();
            if (string.IsNullOrWhiteSpace(codigo))
            {
                errors[nameof(request.Codigo)] = ["Codigo es requerido."];
            }
            else if (codigo.Length > 20)
            {
                errors[nameof(request.Codigo)] = ["Codigo no puede exceder 20 caracteres."];
            }

            if (string.IsNullOrWhiteSpace(nombre))
            {
                errors[nameof(request.Nombre)] = ["Nombre es requerido."];
            }
            else if (nombre.Length > 80)
            {
                errors[nameof(request.Nombre)] = ["Nombre no puede exceder 80 caracteres."];
            }

            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            request.Codigo = codigo;
            request.Nombre = nombre;

            var createdCodigo = await service.CreateAsync(request, ct);
            var encoded = WebUtility.UrlEncode(createdCodigo);
            var location = $"{httpContext.Request.Path}/{encoded}";
            return Results.Created(location, new { codigo = createdCodigo });
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.InventarioUnidadesMedida, Permissions.Actions.Crear));

        group.MapPut("/{codigo}", async (
            string codigo,
            [FromBody] UnidadMedidaRequest request,
            [FromServices] IUnidadesMedidaService service,
            CancellationToken ct) =>
        {
            var nombre = (request.Nombre ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(nombre))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { [nameof(request.Nombre)] = ["Nombre es requerido."] });
            }

            if (nombre.Length > 80)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { [nameof(request.Nombre)] = ["Nombre no puede exceder 80 caracteres."] });
            }

            request.Codigo = null;
            request.Nombre = nombre;

            var updated = await service.UpdateAsync(codigo, request, ct);
            return updated ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.InventarioUnidadesMedida, Permissions.Actions.Editar));

        group.MapDelete("/{codigo}", async (
            string codigo,
            [FromServices] IUnidadesMedidaService service,
            CancellationToken ct) =>
        {
            var deleted = await service.DeleteAsync(codigo, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.InventarioUnidadesMedida, Permissions.Actions.Eliminar));
    }
}
