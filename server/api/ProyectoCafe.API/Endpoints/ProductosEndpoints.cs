using Microsoft.AspNetCore.Mvc;
using ProyectoCafe.API.Auth;
using ProyectoCafe.Application.Inventario.Productos;

namespace ProyectoCafe.API.Endpoints;

public static class ProductosEndpoints
{
    public static IEndpointRouteBuilder MapProductosEndpoints(this IEndpointRouteBuilder endpoints)
    {
        Map(endpoints, "/api/v1/productos");
        Map(endpoints, "/api/Productos"); // compatibilidad con Berry
        return endpoints;
    }

    private static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints
            .MapGroup(prefix)
            .WithTags("Productos");

        group.MapGet("/", async ([FromServices] IProductosService service, CancellationToken ct) =>
        {
            var list = await service.GetAllAsync(ct);
            return Results.Ok(list);
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.InventarioProductos, Permissions.Actions.Ver));

        group.MapGet("/{id:int}", async (
            int id,
            [FromServices] IProductosService service,
            CancellationToken ct) =>
        {
            var item = await service.GetByIdAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        })
        .WithName($"{prefix}:GetById")
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.InventarioProductos, Permissions.Actions.Ver));

        group.MapPost("/", async (
            [FromBody] ProductoRequest request,
            [FromServices] IProductosService service,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var errors = Validate(request, isCreate: true);
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

            var location = $"{httpContext.Request.Path}/{newId}";
            return Results.Created(location, new { id = newId });
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.InventarioProductos, Permissions.Actions.Crear));

        group.MapPut("/{id:int}", async (
            int id,
            [FromBody] ProductoRequest request,
            [FromServices] IProductosService service,
            CancellationToken ct) =>
        {
            var errors = Validate(request, isCreate: false);
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
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.InventarioProductos, Permissions.Actions.Editar));

        group.MapDelete("/{id:int}", async (
            int id,
            [FromServices] IProductosService service,
            CancellationToken ct) =>
        {
            var deleted = await service.DeleteAsync(id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.InventarioProductos, Permissions.Actions.Eliminar));
    }

    private static Dictionary<string, string[]> Validate(ProductoRequest request, bool isCreate)
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

        var descripcion = (request.Descripcion ?? string.Empty).Trim();
        if (!string.IsNullOrEmpty(descripcion) && descripcion.Length > 250)
        {
            errors[nameof(request.Descripcion)] = ["Descripcion no puede exceder 250 caracteres."];
        }

        if (request.Precio < 0)
        {
            errors[nameof(request.Precio)] = ["Precio no puede ser negativo."];
        }

        if (request.PrecioInternet < 0)
        {
            errors[nameof(request.PrecioInternet)] = ["PrecioInternet no puede ser negativo."];
        }

        if (request.PrecioMayorista < 0)
        {
            errors[nameof(request.PrecioMayorista)] = ["PrecioMayorista no puede ser negativo."];
        }

        if (request.Stock is not null && request.Stock < 0)
        {
            errors[nameof(request.Stock)] = ["Stock no puede ser negativo."];
        }

        if (request.CategoriaId <= 0)
        {
            errors[nameof(request.CategoriaId)] = ["CategoriaId es requerido."];
        }

        if (!request.PresentacionId.HasValue || request.PresentacionId.Value <= 0)
        {
            errors[nameof(request.PresentacionId)] = ["PresentacionId es requerido."];
        }

        var unidadMedida = (request.UnidadMedida ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(unidadMedida) && unidadMedida.Length > 20)
        {
            errors[nameof(request.UnidadMedida)] = ["UnidadMedida no puede exceder 20 caracteres."];
        }

        var tipoLote = (request.TipoLote ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(tipoLote) && tipoLote.Length > 30)
        {
            errors[nameof(request.TipoLote)] = ["TipoLote no puede exceder 30 caracteres."];
        }

        if (request.PreciosTipoVenta is { Count: > 0 })
        {
            var duplicateTipoVentaId = request.PreciosTipoVenta
                .GroupBy(x => x.TipoVentaId)
                .Where(x => x.Key > 0 && x.Count() > 1)
                .Select(x => x.Key)
                .FirstOrDefault();

            if (duplicateTipoVentaId > 0)
            {
                errors[nameof(request.PreciosTipoVenta)] = [$"TipoVentaId {duplicateTipoVentaId} está repetido."];
            }

            for (var i = 0; i < request.PreciosTipoVenta.Count; i++)
            {
                var precio = request.PreciosTipoVenta[i];
                if (precio.TipoVentaId <= 0)
                {
                    errors[$"PreciosTipoVenta[{i}].{nameof(precio.TipoVentaId)}"] = ["TipoVentaId es requerido."];
                }

                if (precio.Precio < 0)
                {
                    errors[$"PreciosTipoVenta[{i}].{nameof(precio.Precio)}"] = ["Precio no puede ser negativo."];
                }
            }
        }

        return errors;
    }

    private static void Normalize(ProductoRequest request)
    {
        request.Nombre = (request.Nombre ?? string.Empty).Trim();

        var descripcion = (request.Descripcion ?? string.Empty).Trim();
        request.Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion;

        var unidadMedida = (request.UnidadMedida ?? string.Empty).Trim().ToUpperInvariant();
        request.UnidadMedida = string.IsNullOrWhiteSpace(unidadMedida) ? null : unidadMedida;

        var tipoLote = (request.TipoLote ?? string.Empty).Trim();
        request.TipoLote = string.IsNullOrWhiteSpace(tipoLote) ? null : tipoLote;
    }
}
