using Microsoft.AspNetCore.Mvc;
using ProyectoCafe.API.Auth;
using ProyectoCafe.Application.Produccion.EmpaquesCafe;

namespace ProyectoCafe.API.Endpoints;

public static class EmpaquesCafeEndpoints
{
    public static IEndpointRouteBuilder MapEmpaquesCafeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        Map(endpoints, "/api/v1/produccion/empaques-cafe");
        return endpoints;
    }

    private static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints
            .MapGroup(prefix)
            .WithTags("EmpaquesCafe");

        group.MapGet("/", async ([FromServices] IEmpaquesCafeService service, CancellationToken ct) =>
        {
            var list = await service.GetAllAsync(ct);
            return Results.Ok(list);
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ProduccionEmpaquesCafe, Permissions.Actions.Ver));

        group.MapGet("/{id:int}", async (
            int id,
            [FromServices] IEmpaquesCafeService service,
            CancellationToken ct) =>
        {
            var item = await service.GetByIdAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        })
        .WithName($"{prefix}:GetById")
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ProduccionEmpaquesCafe, Permissions.Actions.Ver));

        group.MapPost("/", async (
            [FromBody] EmpaqueCafeCreateRequest request,
            [FromServices] IEmpaquesCafeService service,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var errors = Validate(request);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            EmpaqueCafeCreateResult result;
            try
            {
                result = await service.CreateAsync(request, ct);
            }
            catch (ArgumentException ex)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = [ex.Message] });
            }

            var location = $"{httpContext.Request.Path}/{result.EmpaqueCafeId}";
            return Results.Created(location, result);
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ProduccionEmpaquesCafe, Permissions.Actions.Crear));

        group.MapPost("/{id:int}/anular", async (
            int id,
            [FromServices] IEmpaquesCafeService service,
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
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ProduccionEmpaquesCafe, Permissions.Actions.Eliminar));

        group.MapDelete("/{id:int}", async (
            int id,
            [FromServices] IEmpaquesCafeService service,
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
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ProduccionEmpaquesCafe, Permissions.Actions.Eliminar));
    }

    private static Dictionary<string, string[]> Validate(EmpaqueCafeCreateRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.LoteOrigenId <= 0)
        {
            errors[nameof(request.LoteOrigenId)] = ["LoteOrigenId es requerido."];
        }

        if (request.CostoEmpaque < 0)
        {
            errors[nameof(request.CostoEmpaque)] = ["CostoEmpaque no puede ser negativo."];
        }

        if (request.Detalles is null || request.Detalles.Count == 0)
        {
            errors[nameof(request.Detalles)] = ["Detalles son requeridos."];
        }
        else
        {
            for (var i = 0; i < request.Detalles.Count; i++)
            {
                var detalle = request.Detalles[i];

                if (detalle.ProductoSalidaId <= 0)
                {
                    errors[$"Detalles[{i}].{nameof(detalle.ProductoSalidaId)}"] = ["ProductoSalidaId es requerido."];
                }

                if (detalle.PresentacionId <= 0)
                {
                    errors[$"Detalles[{i}].{nameof(detalle.PresentacionId)}"] = ["PresentacionId es requerido."];
                }

                if (detalle.Cantidad <= 0)
                {
                    errors[$"Detalles[{i}].{nameof(detalle.Cantidad)}"] = ["Cantidad debe ser mayor a 0."];
                }

                var codigoLote = (detalle.CodigoLoteSalida ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(codigoLote) && codigoLote.Length > 50)
                {
                    errors[$"Detalles[{i}].{nameof(detalle.CodigoLoteSalida)}"] = ["CodigoLoteSalida no puede exceder 50 caracteres."];
                }
            }
        }

        var observaciones = (request.Observaciones ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(observaciones) && observaciones.Length > 250)
        {
            errors[nameof(request.Observaciones)] = ["Observaciones no puede exceder 250 caracteres."];
        }

        return errors;
    }
}
