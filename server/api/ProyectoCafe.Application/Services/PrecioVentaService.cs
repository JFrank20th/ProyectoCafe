using ProyectoCafe.Application.Abstractions.Persistence;
using ProyectoCafe.Application.Common;
using ProyectoCafe.Application.DTOs.Ventas;
using ProyectoCafe.Domain.Entities;

namespace ProyectoCafe.Application.Services;

public sealed class PrecioVentaService(
    IPrecioVentaRepository precios,
    ICatalogoCafeRepository catalogos,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    private readonly IPrecioVentaRepository _precios = precios;
    private readonly ICatalogoCafeRepository _catalogos = catalogos;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<IReadOnlyList<PrecioVentaDto>> GetPreciosAsync(CancellationToken ct)
    {
        var list = await _precios.ListAsync(ct);
        return list.Select(Map).ToArray();
    }

    public async Task<OperationResult<PrecioVentaDto>> GetPrecioAsync(int precioVentaId, CancellationToken ct)
    {
        var precio = await _precios.FindAsync(precioVentaId, ct);
        return precio is null
            ? OperationResult<PrecioVentaDto>.NotFound()
            : OperationResult<PrecioVentaDto>.Success(Map(precio));
    }

    public async Task<OperationResult<int>> CreatePrecioAsync(
        UpsertPrecioVentaRequest request,
        CancellationToken ct)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (!TryNormalize(request, out var data, out var errors))
        {
            return OperationResult<int>.Validation(errors);
        }

        var presentacion = await _catalogos.FindPresentacionAsync(data.PresentacionCafeId, ct);
        if (presentacion is null || presentacion.Estado != EstadoCatalogoCafe.Activo)
        {
            return OperationResult<int>.Validation(Error(
                nameof(UpsertPrecioVentaRequest.PresentacionCafeId),
                "La presentación no existe o no está activa."));
        }

        if (await _precios.NombreExistsAsync(data.Nombre, data.PresentacionCafeId, null, ct))
        {
            return OperationResult<int>.Validation(Error(
                nameof(UpsertPrecioVentaRequest.Nombre),
                "Ya existe una tarifa con ese nombre para la presentación."));
        }

        var precio = new PrecioVenta
        {
            Nombre = data.Nombre,
            PresentacionCafeId = data.PresentacionCafeId,
            Precio = data.Precio,
            Estado = data.Estado ?? EstadoCatalogoCafe.Activo,
            CreadoEn = now,
        };

        try
        {
            await _precios.AddAsync(precio, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return OperationResult<int>.Success(precio.PrecioVentaId);
        }
        catch (ConcurrencyConflictException)
        {
            return OperationResult<int>.Conflict();
        }
    }

    public async Task<OperationResult> UpdatePrecioAsync(
        int precioVentaId,
        UpsertPrecioVentaRequest request,
        CancellationToken ct)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (!TryNormalize(request, out var data, out var errors))
        {
            return OperationResult.Validation(errors);
        }

        var precio = await _precios.FindAsync(precioVentaId, ct);
        if (precio is null)
        {
            return OperationResult.NotFound();
        }

        var presentacion = await _catalogos.FindPresentacionAsync(data.PresentacionCafeId, ct);
        if (presentacion is null || presentacion.Estado != EstadoCatalogoCafe.Activo)
        {
            return OperationResult.Validation(Error(
                nameof(UpsertPrecioVentaRequest.PresentacionCafeId),
                "La presentación no existe o no está activa."));
        }

        if (await _precios.NombreExistsAsync(data.Nombre, data.PresentacionCafeId, precioVentaId, ct))
        {
            return OperationResult.Validation(Error(
                nameof(UpsertPrecioVentaRequest.Nombre),
                "Ya existe una tarifa con ese nombre para la presentación."));
        }

        precio.Nombre = data.Nombre;
        precio.PresentacionCafeId = data.PresentacionCafeId;
        precio.Precio = data.Precio;
        if (data.Estado is { } estado)
        {
            precio.Estado = estado;
        }
        precio.ActualizadoEn = now;

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
            return OperationResult.Success();
        }
        catch (ConcurrencyConflictException)
        {
            return OperationResult.Conflict();
        }
    }

    private static bool TryNormalize(
        UpsertPrecioVentaRequest request,
        out PrecioNormalizado data,
        out Dictionary<string, string[]> errors)
    {
        data = new PrecioNormalizado();
        errors = new Dictionary<string, string[]>();

        var nombre = (request.Nombre ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(nombre))
        {
            AddError(errors, nameof(UpsertPrecioVentaRequest.Nombre), "El nombre de la tarifa es requerido.");
        }
        else if (nombre.Length > 80)
        {
            AddError(errors, nameof(UpsertPrecioVentaRequest.Nombre), "El nombre no puede exceder 80 caracteres.");
        }
        else
        {
            data.Nombre = nombre;
        }

        if (request.PresentacionCafeId <= 0)
        {
            AddError(errors, nameof(UpsertPrecioVentaRequest.PresentacionCafeId), "La presentación es requerida.");
        }
        else
        {
            data.PresentacionCafeId = request.PresentacionCafeId;
        }

        if (request.Precio is null || request.Precio <= 0)
        {
            AddError(errors, nameof(UpsertPrecioVentaRequest.Precio), "El precio debe ser mayor que cero.");
        }
        else
        {
            data.Precio = request.Precio.Value;
        }

        if (request.Estado is { } estado)
        {
            if (!Enum.IsDefined(estado))
            {
                AddError(errors, nameof(UpsertPrecioVentaRequest.Estado), "El estado no es válido.");
            }
            else if (estado == EstadoCatalogoCafe.Eliminado)
            {
                AddError(errors, nameof(UpsertPrecioVentaRequest.Estado), "Use la operación de eliminación para retirar la tarifa.");
            }
            else
            {
                data.Estado = estado;
            }
        }

        return errors.Count == 0;
    }

    private static PrecioVentaDto Map(PrecioVenta precio) =>
        new()
        {
            PrecioVentaId = precio.PrecioVentaId,
            Nombre = precio.Nombre,
            PresentacionCafeId = precio.PresentacionCafeId,
            PresentacionCodigo = precio.PresentacionCafe?.Codigo ?? string.Empty,
            PresentacionNombre = precio.PresentacionCafe?.Nombre ?? string.Empty,
            Precio = precio.Precio,
            Estado = precio.Estado,
            CreadoEn = precio.CreadoEn,
            ActualizadoEn = precio.ActualizadoEn,
        };

    private static Dictionary<string, string[]> Error(string key, string message) =>
        new() { [key] = [message] };

    private static void AddError(Dictionary<string, string[]> errors, string key, string message)
    {
        if (errors.TryGetValue(key, out var current))
        {
            errors[key] = [.. current, message];
            return;
        }

        errors[key] = [message];
    }

    private sealed class PrecioNormalizado
    {
        public string Nombre { get; set; } = string.Empty;
        public int PresentacionCafeId { get; set; }
        public decimal Precio { get; set; }
        public EstadoCatalogoCafe? Estado { get; set; }
    }
}