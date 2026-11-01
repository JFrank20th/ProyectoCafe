using ProyectoCafe.Application.Abstractions.Persistence;
using ProyectoCafe.Application.Common;
using ProyectoCafe.Application.DTOs.Inventario;
using ProyectoCafe.Domain.Entities;

namespace ProyectoCafe.Application.Services;

/// <summary>
/// Casos de uso de los catálogos configurables de café terminado
/// (presentaciones y tipos de café). Los catálogos se desactivan o eliminan
/// lógicamente para conservar el historial de existencias y movimientos.
/// </summary>
public sealed class CatalogoCafeService(
    ICatalogoCafeRepository catalogos,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    private readonly ICatalogoCafeRepository _catalogos = catalogos;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<IReadOnlyList<PresentacionCafeDto>> GetPresentacionesAsync(CancellationToken ct)
    {
        var presentaciones = await _catalogos.ListPresentacionesAsync(ct);
        return presentaciones.Select(MapPresentacion).ToArray();
    }

    public async Task<OperationResult<PresentacionCafeDto>> GetPresentacionAsync(
        int presentacionCafeId,
        CancellationToken ct)
    {
        var presentacion = await _catalogos.FindPresentacionAsync(presentacionCafeId, ct);
        return presentacion is null
            ? OperationResult<PresentacionCafeDto>.NotFound()
            : OperationResult<PresentacionCafeDto>.Success(MapPresentacion(presentacion));
    }

    public async Task<OperationResult<int>> CreatePresentacionAsync(
        UpsertPresentacionCafeRequest request,
        CancellationToken ct)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (!TryNormalizePresentacion(request, out var data, out var errors))
        {
            return OperationResult<int>.Validation(errors);
        }

        if (await _catalogos.PresentacionCodigoExistsAsync(data.Codigo, null, ct))
        {
            return OperationResult<int>.Validation(Error(
                nameof(UpsertPresentacionCafeRequest.Codigo),
                "Ya existe una presentación con ese código."));
        }

        if (await _catalogos.PresentacionNombreExistsAsync(data.Nombre, null, ct))
        {
            return OperationResult<int>.Validation(Error(
                nameof(UpsertPresentacionCafeRequest.Nombre),
                "Ya existe una presentación con ese nombre."));
        }

        var presentacion = new PresentacionCafe
        {
            Codigo = data.Codigo,
            Nombre = data.Nombre,
            PesoUnitarioGramos = data.PesoUnitarioGramos,
            Estado = data.Estado ?? EstadoCatalogoCafe.Activo,
            CreadoEn = now,
        };

        try
        {
            await _catalogos.AddPresentacionAsync(presentacion, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return OperationResult<int>.Success(presentacion.PresentacionCafeId);
        }
        catch (ConcurrencyConflictException)
        {
            return OperationResult<int>.Conflict();
        }
    }

    public async Task<OperationResult> UpdatePresentacionAsync(
        int presentacionCafeId,
        UpsertPresentacionCafeRequest request,
        CancellationToken ct)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (!TryNormalizePresentacion(request, out var data, out var errors))
        {
            return OperationResult.Validation(errors);
        }

        var presentacion = await _catalogos.FindPresentacionAsync(presentacionCafeId, ct);
        if (presentacion is null)
        {
            return OperationResult.NotFound();
        }

        if (await _catalogos.PresentacionCodigoExistsAsync(data.Codigo, presentacionCafeId, ct))
        {
            return OperationResult.Validation(Error(
                nameof(UpsertPresentacionCafeRequest.Codigo),
                "Ya existe una presentación con ese código."));
        }

        if (await _catalogos.PresentacionNombreExistsAsync(data.Nombre, presentacionCafeId, ct))
        {
            return OperationResult.Validation(Error(
                nameof(UpsertPresentacionCafeRequest.Nombre),
                "Ya existe una presentación con ese nombre."));
        }

        if (data.PesoUnitarioGramos != presentacion.PesoUnitarioGramos &&
            await _catalogos.PresentacionHasExistenciasAsync(presentacionCafeId, ct))
        {
            return OperationResult.Validation(Error(
                nameof(UpsertPresentacionCafeRequest.PesoUnitarioGramos),
                "No se puede cambiar el peso unitario porque la presentación tiene existencias históricas."));
        }

        if (data.Estado == EstadoCatalogoCafe.Inactivo &&
            presentacion.Estado != EstadoCatalogoCafe.Inactivo &&
            await _catalogos.PresentacionHasStockAsync(presentacionCafeId, ct))
        {
            return OperationResult.Validation(Error(
                nameof(UpsertPresentacionCafeRequest.Estado),
                "No se puede inactivar la presentación porque tiene existencias disponibles."));
        }

        presentacion.Codigo = data.Codigo;
        presentacion.Nombre = data.Nombre;
        presentacion.PesoUnitarioGramos = data.PesoUnitarioGramos;
        if (data.Estado is { } estado)
        {
            presentacion.Estado = estado;
        }

        presentacion.ActualizadoEn = now;

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

    public async Task<OperationResult> DeletePresentacionAsync(
        int presentacionCafeId,
        CancellationToken ct)
    {
        var presentacion = await _catalogos.FindPresentacionAsync(presentacionCafeId, ct);
        if (presentacion is null)
        {
            return OperationResult.NotFound();
        }

        if (await _catalogos.PresentacionHasStockAsync(presentacionCafeId, ct))
        {
            return OperationResult.Validation(Error(
                "Estado",
                "No se puede eliminar la presentación porque tiene existencias disponibles."));
        }

        presentacion.Estado = EstadoCatalogoCafe.Eliminado;
        presentacion.ActualizadoEn = _timeProvider.GetUtcNow().UtcDateTime;

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

    public async Task<IReadOnlyList<TipoCafeDto>> GetTiposCafeAsync(CancellationToken ct)
    {
        var tipos = await _catalogos.ListTiposCafeAsync(ct);
        return tipos.Select(MapTipoCafe).ToArray();
    }

    public async Task<OperationResult<TipoCafeDto>> GetTipoCafeAsync(
        int tipoCafeId,
        CancellationToken ct)
    {
        var tipo = await _catalogos.FindTipoCafeAsync(tipoCafeId, ct);
        return tipo is null
            ? OperationResult<TipoCafeDto>.NotFound()
            : OperationResult<TipoCafeDto>.Success(MapTipoCafe(tipo));
    }

    public async Task<OperationResult<int>> CreateTipoCafeAsync(
        UpsertTipoCafeRequest request,
        CancellationToken ct)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (!TryNormalizeTipoCafe(request, out var data, out var errors))
        {
            return OperationResult<int>.Validation(errors);
        }

        if (await _catalogos.TipoCafeCodigoExistsAsync(data.Codigo, null, ct))
        {
            return OperationResult<int>.Validation(Error(
                nameof(UpsertTipoCafeRequest.Codigo),
                "Ya existe un tipo de café con ese código."));
        }

        if (await _catalogos.TipoCafeNombreExistsAsync(data.Nombre, null, ct))
        {
            return OperationResult<int>.Validation(Error(
                nameof(UpsertTipoCafeRequest.Nombre),
                "Ya existe un tipo de café con ese nombre."));
        }

        var tipo = new TipoCafe
        {
            Codigo = data.Codigo,
            Nombre = data.Nombre,
            Estado = data.Estado ?? EstadoCatalogoCafe.Activo,
            CreadoEn = now,
        };

        try
        {
            await _catalogos.AddTipoCafeAsync(tipo, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return OperationResult<int>.Success(tipo.TipoCafeId);
        }
        catch (ConcurrencyConflictException)
        {
            return OperationResult<int>.Conflict();
        }
    }

    public async Task<OperationResult> UpdateTipoCafeAsync(
        int tipoCafeId,
        UpsertTipoCafeRequest request,
        CancellationToken ct)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (!TryNormalizeTipoCafe(request, out var data, out var errors))
        {
            return OperationResult.Validation(errors);
        }

        var tipo = await _catalogos.FindTipoCafeAsync(tipoCafeId, ct);
        if (tipo is null)
        {
            return OperationResult.NotFound();
        }

        if (await _catalogos.TipoCafeCodigoExistsAsync(data.Codigo, tipoCafeId, ct))
        {
            return OperationResult.Validation(Error(
                nameof(UpsertTipoCafeRequest.Codigo),
                "Ya existe un tipo de café con ese código."));
        }

        if (await _catalogos.TipoCafeNombreExistsAsync(data.Nombre, tipoCafeId, ct))
        {
            return OperationResult.Validation(Error(
                nameof(UpsertTipoCafeRequest.Nombre),
                "Ya existe un tipo de café con ese nombre."));
        }

        if (data.Estado == EstadoCatalogoCafe.Inactivo &&
            tipo.Estado != EstadoCatalogoCafe.Inactivo &&
            await _catalogos.TipoCafeHasStockAsync(tipoCafeId, ct))
        {
            return OperationResult.Validation(Error(
                nameof(UpsertTipoCafeRequest.Estado),
                "No se puede inactivar el tipo de café porque tiene existencias disponibles."));
        }

        tipo.Codigo = data.Codigo;
        tipo.Nombre = data.Nombre;
        if (data.Estado is { } estado)
        {
            tipo.Estado = estado;
        }

        tipo.ActualizadoEn = now;

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

    public async Task<OperationResult> DeleteTipoCafeAsync(
        int tipoCafeId,
        CancellationToken ct)
    {
        var tipo = await _catalogos.FindTipoCafeAsync(tipoCafeId, ct);
        if (tipo is null)
        {
            return OperationResult.NotFound();
        }

        if (await _catalogos.TipoCafeHasStockAsync(tipoCafeId, ct))
        {
            return OperationResult.Validation(Error(
                "Estado",
                "No se puede eliminar el tipo de café porque tiene existencias disponibles."));
        }

        tipo.Estado = EstadoCatalogoCafe.Eliminado;
        tipo.ActualizadoEn = _timeProvider.GetUtcNow().UtcDateTime;

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

    public async Task<IReadOnlyList<VariedadCafeDto>> GetVariedadesAsync(CancellationToken ct)
    {
        var variedades = await _catalogos.ListVariedadesAsync(ct);
        return variedades.Select(MapVariedad).ToArray();
    }

    public async Task<OperationResult<VariedadCafeDto>> GetVariedadAsync(
        int variedadCafeId,
        CancellationToken ct)
    {
        var variedad = await _catalogos.FindVariedadAsync(variedadCafeId, ct);
        return variedad is null
            ? OperationResult<VariedadCafeDto>.NotFound()
            : OperationResult<VariedadCafeDto>.Success(MapVariedad(variedad));
    }

    public async Task<OperationResult<int>> CreateVariedadAsync(
        UpsertVariedadCafeRequest request,
        CancellationToken ct)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (!TryNormalizeVariedad(request, out var data, out var errors))
        {
            return OperationResult<int>.Validation(errors);
        }

        if (await _catalogos.VariedadCodigoExistsAsync(data.Codigo, null, ct))
        {
            return OperationResult<int>.Validation(Error(
                nameof(UpsertVariedadCafeRequest.Codigo),
                "Ya existe una variedad con ese código."));
        }

        if (await _catalogos.VariedadNombreExistsAsync(data.Nombre, null, ct))
        {
            return OperationResult<int>.Validation(Error(
                nameof(UpsertVariedadCafeRequest.Nombre),
                "Ya existe una variedad con ese nombre."));
        }

        var variedad = new VariedadCafe
        {
            Codigo = data.Codigo,
            Nombre = data.Nombre,
            Estado = data.Estado ?? EstadoCatalogoCafe.Activo,
            CreadoEn = now,
        };

        try
        {
            await _catalogos.AddVariedadAsync(variedad, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return OperationResult<int>.Success(variedad.VariedadCafeId);
        }
        catch (ConcurrencyConflictException)
        {
            return OperationResult<int>.Conflict();
        }
    }

    public async Task<OperationResult> UpdateVariedadAsync(
        int variedadCafeId,
        UpsertVariedadCafeRequest request,
        CancellationToken ct)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (!TryNormalizeVariedad(request, out var data, out var errors))
        {
            return OperationResult.Validation(errors);
        }

        var variedad = await _catalogos.FindVariedadAsync(variedadCafeId, ct);
        if (variedad is null)
        {
            return OperationResult.NotFound();
        }

        if (await _catalogos.VariedadCodigoExistsAsync(data.Codigo, variedadCafeId, ct))
        {
            return OperationResult.Validation(Error(
                nameof(UpsertVariedadCafeRequest.Codigo),
                "Ya existe una variedad con ese código."));
        }

        if (await _catalogos.VariedadNombreExistsAsync(data.Nombre, variedadCafeId, ct))
        {
            return OperationResult.Validation(Error(
                nameof(UpsertVariedadCafeRequest.Nombre),
                "Ya existe una variedad con ese nombre."));
        }

        variedad.Codigo = data.Codigo;
        variedad.Nombre = data.Nombre;
        if (data.Estado is { } estado)
        {
            variedad.Estado = estado;
        }

        variedad.ActualizadoEn = now;

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

    public async Task<OperationResult> DeleteVariedadAsync(
        int variedadCafeId,
        CancellationToken ct)
    {
        var variedad = await _catalogos.FindVariedadAsync(variedadCafeId, ct);
        if (variedad is null)
        {
            return OperationResult.NotFound();
        }

        variedad.Estado = EstadoCatalogoCafe.Eliminado;
        variedad.ActualizadoEn = _timeProvider.GetUtcNow().UtcDateTime;

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

    private static bool TryNormalizeVariedad(
        UpsertVariedadCafeRequest request,
        out VariedadNormalizada data,
        out Dictionary<string, string[]> errors)
    {
        data = new VariedadNormalizada();
        errors = new Dictionary<string, string[]>();

        var codigo = NormalizeCode(request.Codigo);
        if (string.IsNullOrWhiteSpace(codigo))
        {
            AddError(errors, nameof(UpsertVariedadCafeRequest.Codigo),
                "El código de la variedad es requerido.");
        }
        else if (codigo.Length > 20)
        {
            AddError(errors, nameof(UpsertVariedadCafeRequest.Codigo),
                "El código no puede exceder 20 caracteres.");
        }
        else
        {
            data.Codigo = codigo;
        }

        var nombre = NormalizeOptionalText(request.Nombre);
        if (string.IsNullOrWhiteSpace(nombre))
        {
            AddError(errors, nameof(UpsertVariedadCafeRequest.Nombre),
                "El nombre de la variedad es requerido.");
        }
        else if (nombre.Length > 80)
        {
            AddError(errors, nameof(UpsertVariedadCafeRequest.Nombre),
                "El nombre no puede exceder 80 caracteres.");
        }
        else
        {
            data.Nombre = nombre;
        }

        ValidateEstado(request.Estado, errors, nameof(UpsertVariedadCafeRequest.Estado));
        data.Estado = request.Estado;

        return errors.Count == 0;
    }

    private static VariedadCafeDto MapVariedad(VariedadCafe variedad) =>
        new()
        {
            VariedadCafeId = variedad.VariedadCafeId,
            Codigo = variedad.Codigo,
            Nombre = variedad.Nombre,
            Estado = variedad.Estado,
            CreadoEn = variedad.CreadoEn,
            ActualizadoEn = variedad.ActualizadoEn,
        };

    private sealed class VariedadNormalizada
    {
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public EstadoCatalogoCafe? Estado { get; set; }
    }

    public async Task<IReadOnlyList<TipoTuesteCafeDto>> GetTiposTuesteAsync(CancellationToken ct)
    {
        var tiposTueste = await _catalogos.ListTiposTuesteAsync(ct);
        return tiposTueste.Select(MapTipoTueste).ToArray();
    }

    public async Task<OperationResult<TipoTuesteCafeDto>> GetTipoTuesteAsync(
        int tipoTuesteCafeId,
        CancellationToken ct)
    {
        var tipoTueste = await _catalogos.FindTipoTuesteAsync(tipoTuesteCafeId, ct);
        return tipoTueste is null
            ? OperationResult<TipoTuesteCafeDto>.NotFound()
            : OperationResult<TipoTuesteCafeDto>.Success(MapTipoTueste(tipoTueste));
    }

    public async Task<OperationResult<int>> CreateTipoTuesteAsync(
        UpsertTipoTuesteCafeRequest request,
        CancellationToken ct)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (!TryNormalizeTipoTueste(request, out var data, out var errors))
        {
            return OperationResult<int>.Validation(errors);
        }

        if (await _catalogos.TipoTuesteCodigoExistsAsync(data.Codigo, null, ct))
        {
            return OperationResult<int>.Validation(Error(
                nameof(UpsertTipoTuesteCafeRequest.Codigo),
                "Ya existe un tipo de tueste con ese código."));
        }

        if (await _catalogos.TipoTuesteNombreExistsAsync(data.Nombre, null, ct))
        {
            return OperationResult<int>.Validation(Error(
                nameof(UpsertTipoTuesteCafeRequest.Nombre),
                "Ya existe un tipo de tueste con ese nombre."));
        }

        var tipoTueste = new TipoTuesteCafe
        {
            Codigo = data.Codigo,
            Nombre = data.Nombre,
            Estado = data.Estado ?? EstadoCatalogoCafe.Activo,
            CreadoEn = now,
        };

        try
        {
            await _catalogos.AddTipoTuesteAsync(tipoTueste, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return OperationResult<int>.Success(tipoTueste.TipoTuesteCafeId);
        }
        catch (ConcurrencyConflictException)
        {
            return OperationResult<int>.Conflict();
        }
    }

    public async Task<OperationResult> UpdateTipoTuesteAsync(
        int tipoTuesteCafeId,
        UpsertTipoTuesteCafeRequest request,
        CancellationToken ct)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (!TryNormalizeTipoTueste(request, out var data, out var errors))
        {
            return OperationResult.Validation(errors);
        }

        var tipoTueste = await _catalogos.FindTipoTuesteAsync(tipoTuesteCafeId, ct);
        if (tipoTueste is null)
        {
            return OperationResult.NotFound();
        }

        if (await _catalogos.TipoTuesteCodigoExistsAsync(data.Codigo, tipoTuesteCafeId, ct))
        {
            return OperationResult.Validation(Error(
                nameof(UpsertTipoTuesteCafeRequest.Codigo),
                "Ya existe un tipo de tueste con ese código."));
        }

        if (await _catalogos.TipoTuesteNombreExistsAsync(data.Nombre, tipoTuesteCafeId, ct))
        {
            return OperationResult.Validation(Error(
                nameof(UpsertTipoTuesteCafeRequest.Nombre),
                "Ya existe un tipo de tueste con ese nombre."));
        }

        tipoTueste.Codigo = data.Codigo;
        tipoTueste.Nombre = data.Nombre;
        if (data.Estado is { } estado)
        {
            tipoTueste.Estado = estado;
        }

        tipoTueste.ActualizadoEn = now;

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

    public async Task<OperationResult> DeleteTipoTuesteAsync(
        int tipoTuesteCafeId,
        CancellationToken ct)
    {
        var tipoTueste = await _catalogos.FindTipoTuesteAsync(tipoTuesteCafeId, ct);
        if (tipoTueste is null)
        {
            return OperationResult.NotFound();
        }

        tipoTueste.Estado = EstadoCatalogoCafe.Eliminado;
        tipoTueste.ActualizadoEn = _timeProvider.GetUtcNow().UtcDateTime;

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

    private static bool TryNormalizeTipoTueste(
        UpsertTipoTuesteCafeRequest request,
        out TipoTuesteNormalizado data,
        out Dictionary<string, string[]> errors)
    {
        data = new TipoTuesteNormalizado();
        errors = new Dictionary<string, string[]>();

        var codigo = NormalizeCode(request.Codigo);
        if (string.IsNullOrWhiteSpace(codigo))
        {
            AddError(errors, nameof(UpsertTipoTuesteCafeRequest.Codigo),
                "El código del tipo de tueste es requerido.");
        }
        else if (codigo.Length > 20)
        {
            AddError(errors, nameof(UpsertTipoTuesteCafeRequest.Codigo),
                "El código no puede exceder 20 caracteres.");
        }
        else
        {
            data.Codigo = codigo;
        }

        var nombre = NormalizeOptionalText(request.Nombre);
        if (string.IsNullOrWhiteSpace(nombre))
        {
            AddError(errors, nameof(UpsertTipoTuesteCafeRequest.Nombre),
                "El nombre del tipo de tueste es requerido.");
        }
        else if (nombre.Length > 80)
        {
            AddError(errors, nameof(UpsertTipoTuesteCafeRequest.Nombre),
                "El nombre no puede exceder 80 caracteres.");
        }
        else
        {
            data.Nombre = nombre;
        }

        ValidateEstado(request.Estado, errors, nameof(UpsertTipoTuesteCafeRequest.Estado));
        data.Estado = request.Estado;

        return errors.Count == 0;
    }

    private static TipoTuesteCafeDto MapTipoTueste(TipoTuesteCafe tipoTueste) =>
        new()
        {
            TipoTuesteCafeId = tipoTueste.TipoTuesteCafeId,
            Codigo = tipoTueste.Codigo,
            Nombre = tipoTueste.Nombre,
            Estado = tipoTueste.Estado,
            CreadoEn = tipoTueste.CreadoEn,
            ActualizadoEn = tipoTueste.ActualizadoEn,
        };

    private sealed class TipoTuesteNormalizado
    {
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public EstadoCatalogoCafe? Estado { get; set; }
    }

    public async Task<IReadOnlyList<TipoProcesoCafeDto>> GetTiposProcesoAsync(CancellationToken ct)
    {
        var tiposProceso = await _catalogos.ListTiposProcesoAsync(ct);
        return tiposProceso.Select(MapTipoProceso).ToArray();
    }

    public async Task<OperationResult<TipoProcesoCafeDto>> GetTipoProcesoAsync(
        int tipoProcesoCafeId,
        CancellationToken ct)
    {
        var tipoProceso = await _catalogos.FindTipoProcesoAsync(tipoProcesoCafeId, ct);
        return tipoProceso is null
            ? OperationResult<TipoProcesoCafeDto>.NotFound()
            : OperationResult<TipoProcesoCafeDto>.Success(MapTipoProceso(tipoProceso));
    }

    public async Task<OperationResult<int>> CreateTipoProcesoAsync(
        UpsertTipoProcesoCafeRequest request,
        CancellationToken ct)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (!TryNormalizeTipoProceso(request, out var data, out var errors))
        {
            return OperationResult<int>.Validation(errors);
        }

        if (await _catalogos.TipoProcesoCodigoExistsAsync(data.Codigo, null, ct))
        {
            return OperationResult<int>.Validation(Error(
                nameof(UpsertTipoProcesoCafeRequest.Codigo),
                "Ya existe un tipo de proceso con ese código."));
        }

        if (await _catalogos.TipoProcesoNombreExistsAsync(data.Nombre, null, ct))
        {
            return OperationResult<int>.Validation(Error(
                nameof(UpsertTipoProcesoCafeRequest.Nombre),
                "Ya existe un tipo de proceso con ese nombre."));
        }

        var tipoProceso = new TipoProcesoCafe
        {
            Codigo = data.Codigo,
            Nombre = data.Nombre,
            Estado = data.Estado ?? EstadoCatalogoCafe.Activo,
            CreadoEn = now,
        };

        try
        {
            await _catalogos.AddTipoProcesoAsync(tipoProceso, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return OperationResult<int>.Success(tipoProceso.TipoProcesoCafeId);
        }
        catch (ConcurrencyConflictException)
        {
            return OperationResult<int>.Conflict();
        }
    }

    public async Task<OperationResult> UpdateTipoProcesoAsync(
        int tipoProcesoCafeId,
        UpsertTipoProcesoCafeRequest request,
        CancellationToken ct)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (!TryNormalizeTipoProceso(request, out var data, out var errors))
        {
            return OperationResult.Validation(errors);
        }

        var tipoProceso = await _catalogos.FindTipoProcesoAsync(tipoProcesoCafeId, ct);
        if (tipoProceso is null)
        {
            return OperationResult.NotFound();
        }

        if (await _catalogos.TipoProcesoCodigoExistsAsync(data.Codigo, tipoProcesoCafeId, ct))
        {
            return OperationResult.Validation(Error(
                nameof(UpsertTipoProcesoCafeRequest.Codigo),
                "Ya existe un tipo de proceso con ese código."));
        }

        if (await _catalogos.TipoProcesoNombreExistsAsync(data.Nombre, tipoProcesoCafeId, ct))
        {
            return OperationResult.Validation(Error(
                nameof(UpsertTipoProcesoCafeRequest.Nombre),
                "Ya existe un tipo de proceso con ese nombre."));
        }

        tipoProceso.Codigo = data.Codigo;
        tipoProceso.Nombre = data.Nombre;
        if (data.Estado is { } estado)
        {
            tipoProceso.Estado = estado;
        }

        tipoProceso.ActualizadoEn = now;

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

    public async Task<OperationResult> DeleteTipoProcesoAsync(
        int tipoProcesoCafeId,
        CancellationToken ct)
    {
        var tipoProceso = await _catalogos.FindTipoProcesoAsync(tipoProcesoCafeId, ct);
        if (tipoProceso is null)
        {
            return OperationResult.NotFound();
        }

        tipoProceso.Estado = EstadoCatalogoCafe.Eliminado;
        tipoProceso.ActualizadoEn = _timeProvider.GetUtcNow().UtcDateTime;

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

    private static bool TryNormalizeTipoProceso(
        UpsertTipoProcesoCafeRequest request,
        out TipoProcesoNormalizado data,
        out Dictionary<string, string[]> errors)
    {
        data = new TipoProcesoNormalizado();
        errors = new Dictionary<string, string[]>();

        var codigo = NormalizeCode(request.Codigo);
        if (string.IsNullOrWhiteSpace(codigo))
        {
            AddError(errors, nameof(UpsertTipoProcesoCafeRequest.Codigo),
                "El código del tipo de proceso es requerido.");
        }
        else if (codigo.Length > 20)
        {
            AddError(errors, nameof(UpsertTipoProcesoCafeRequest.Codigo),
                "El código no puede exceder 20 caracteres.");
        }
        else
        {
            data.Codigo = codigo;
        }

        var nombre = NormalizeOptionalText(request.Nombre);
        if (string.IsNullOrWhiteSpace(nombre))
        {
            AddError(errors, nameof(UpsertTipoProcesoCafeRequest.Nombre),
                "El nombre del tipo de proceso es requerido.");
        }
        else if (nombre.Length > 80)
        {
            AddError(errors, nameof(UpsertTipoProcesoCafeRequest.Nombre),
                "El nombre no puede exceder 80 caracteres.");
        }
        else
        {
            data.Nombre = nombre;
        }

        ValidateEstado(request.Estado, errors, nameof(UpsertTipoProcesoCafeRequest.Estado));
        data.Estado = request.Estado;

        return errors.Count == 0;
    }

    private static TipoProcesoCafeDto MapTipoProceso(TipoProcesoCafe tipoProceso) =>
        new()
        {
            TipoProcesoCafeId = tipoProceso.TipoProcesoCafeId,
            Codigo = tipoProceso.Codigo,
            Nombre = tipoProceso.Nombre,
            Estado = tipoProceso.Estado,
            CreadoEn = tipoProceso.CreadoEn,
            ActualizadoEn = tipoProceso.ActualizadoEn,
        };

    private sealed class TipoProcesoNormalizado
    {
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public EstadoCatalogoCafe? Estado { get; set; }
    }

    private static bool TryNormalizePresentacion(
        UpsertPresentacionCafeRequest request,
        out PresentacionNormalizada data,
        out Dictionary<string, string[]> errors)
    {
        data = new PresentacionNormalizada();
        errors = new Dictionary<string, string[]>();

        var codigo = NormalizeCode(request.Codigo);
        if (string.IsNullOrWhiteSpace(codigo))
        {
            AddError(errors, nameof(UpsertPresentacionCafeRequest.Codigo),
                "El código de la presentación es requerido.");
        }
        else if (codigo.Length > 20)
        {
            AddError(errors, nameof(UpsertPresentacionCafeRequest.Codigo),
                "El código no puede exceder 20 caracteres.");
        }
        else
        {
            data.Codigo = codigo;
        }

        var nombre = NormalizeOptionalText(request.Nombre);
        if (string.IsNullOrWhiteSpace(nombre))
        {
            AddError(errors, nameof(UpsertPresentacionCafeRequest.Nombre),
                "El nombre de la presentación es requerido.");
        }
        else if (nombre.Length > 80)
        {
            AddError(errors, nameof(UpsertPresentacionCafeRequest.Nombre),
                "El nombre no puede exceder 80 caracteres.");
        }
        else
        {
            data.Nombre = nombre;
        }

        if (request.PesoUnitarioGramos is null or <= 0)
        {
            AddError(errors, nameof(UpsertPresentacionCafeRequest.PesoUnitarioGramos),
                "El peso unitario debe ser mayor que cero.");
        }
        else
        {
            data.PesoUnitarioGramos = request.PesoUnitarioGramos.Value;
        }

        ValidateEstado(request.Estado, errors, nameof(UpsertPresentacionCafeRequest.Estado));
        data.Estado = request.Estado;

        return errors.Count == 0;
    }

    private static bool TryNormalizeTipoCafe(
        UpsertTipoCafeRequest request,
        out TipoCafeNormalizado data,
        out Dictionary<string, string[]> errors)
    {
        data = new TipoCafeNormalizado();
        errors = new Dictionary<string, string[]>();

        var codigo = NormalizeCode(request.Codigo);
        if (string.IsNullOrWhiteSpace(codigo))
        {
            AddError(errors, nameof(UpsertTipoCafeRequest.Codigo),
                "El código del tipo de café es requerido.");
        }
        else if (codigo.Length > 20)
        {
            AddError(errors, nameof(UpsertTipoCafeRequest.Codigo),
                "El código no puede exceder 20 caracteres.");
        }
        else
        {
            data.Codigo = codigo;
        }

        var nombre = NormalizeOptionalText(request.Nombre);
        if (string.IsNullOrWhiteSpace(nombre))
        {
            AddError(errors, nameof(UpsertTipoCafeRequest.Nombre),
                "El nombre del tipo de café es requerido.");
        }
        else if (nombre.Length > 80)
        {
            AddError(errors, nameof(UpsertTipoCafeRequest.Nombre),
                "El nombre no puede exceder 80 caracteres.");
        }
        else
        {
            data.Nombre = nombre;
        }

        ValidateEstado(request.Estado, errors, nameof(UpsertTipoCafeRequest.Estado));
        data.Estado = request.Estado;

        return errors.Count == 0;
    }

    private static void ValidateEstado(
        EstadoCatalogoCafe? estado,
        Dictionary<string, string[]> errors,
        string key)
    {
        if (estado is null)
        {
            return;
        }

        if (!Enum.IsDefined(estado.Value))
        {
            AddError(errors, key, "El estado no es válido.");
            return;
        }

        if (estado == EstadoCatalogoCafe.Eliminado)
        {
            AddError(errors, key, "Use la operación de eliminación para retirar el catálogo.");
        }
    }

    private static PresentacionCafeDto MapPresentacion(PresentacionCafe presentacion) =>
        new()
        {
            PresentacionCafeId = presentacion.PresentacionCafeId,
            Codigo = presentacion.Codigo,
            Nombre = presentacion.Nombre,
            PesoUnitarioGramos = presentacion.PesoUnitarioGramos,
            Estado = presentacion.Estado,
            CreadoEn = presentacion.CreadoEn,
            ActualizadoEn = presentacion.ActualizadoEn,
        };

    private static TipoCafeDto MapTipoCafe(TipoCafe tipo) =>
        new()
        {
            TipoCafeId = tipo.TipoCafeId,
            Codigo = tipo.Codigo,
            Nombre = tipo.Nombre,
            Estado = tipo.Estado,
            CreadoEn = tipo.CreadoEn,
            ActualizadoEn = tipo.ActualizadoEn,
        };

    private static string NormalizeCode(string? value) =>
        (value ?? string.Empty).Trim().ToUpperInvariant();

    private static string? NormalizeOptionalText(string? value)
    {
        var normalized = (value ?? string.Empty).Trim();
        return normalized.Length == 0 ? null : normalized;
    }

    private static Dictionary<string, string[]> Error(string key, string message) =>
        new() { [key] = [message] };

    private static void AddError(
        Dictionary<string, string[]> errors,
        string key,
        string message)
    {
        if (errors.TryGetValue(key, out var current))
        {
            errors[key] = [.. current, message];
            return;
        }

        errors[key] = [message];
    }

    private sealed class PresentacionNormalizada
    {
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public int PesoUnitarioGramos { get; set; }
        public EstadoCatalogoCafe? Estado { get; set; }
    }

    private sealed class TipoCafeNormalizado
    {
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public EstadoCatalogoCafe? Estado { get; set; }
    }
}
