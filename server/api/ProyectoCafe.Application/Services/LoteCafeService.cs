using ProyectoCafe.Application.Abstractions.Persistence;
using ProyectoCafe.Application.Common;
using ProyectoCafe.Application.DTOs.Inventario;
using ProyectoCafe.Domain.Entities;

namespace ProyectoCafe.Application.Services;

/// <summary>
/// Casos de uso de lotes, existencias y movimientos de café terminado. Las
/// reglas de saldo se ejecutan dentro de una transacción y las escrituras se
/// delegan a Infrastructure.
/// </summary>
public sealed class LoteCafeService(
    ILoteCafeRepository lotes,
    ICatalogoCafeRepository catalogos,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    private readonly ILoteCafeRepository _lotes = lotes;
    private readonly ICatalogoCafeRepository _catalogos = catalogos;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<OperationResult<int>> CreateLoteAsync(
        CreateLoteCafeRequest request,
        CancellationToken ct)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (!TryNormalizeLote(request, now, out var data, out var errors))
        {
            return OperationResult<int>.Validation(errors);
        }

        var catalogErrors = new Dictionary<string, string[]>();
        await ValidateLoteCatalogosAsync(data, catalogErrors, ct);
        if (catalogErrors.Count > 0)
        {
            return OperationResult<int>.Validation(catalogErrors);
        }

        if (await _lotes.CodigoExistsAsync(data.CodigoLote, ct))
        {
            return OperationResult<int>.Validation(Error(
                nameof(CreateLoteCafeRequest.CodigoLote),
                "Ya existe un lote con ese código."));
        }

        var lote = new LoteCafeTerminado
        {
            CodigoLote = data.CodigoLote,
            FechaProduccion = data.FechaProduccion,
            FechaEnvasado = data.FechaEnvasado,
            FechaVencimiento = data.FechaVencimiento,
            Observacion = data.Observacion,
            VariedadCafeId = data.VariedadCafeId,
            TipoTuesteCafeId = data.TipoTuesteCafeId,
            TipoProcesoCafeId = data.TipoProcesoCafeId,
            Estado = EstadoLoteCafe.Activo,
            CreadoEn = now,
        };

        try
        {
            await _lotes.AddLoteAsync(lote, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return OperationResult<int>.Success(lote.LoteCafeId);
        }
        catch (ConcurrencyConflictException)
        {
            return OperationResult<int>.Conflict();
        }
    }

    public async Task<IReadOnlyList<LoteCafeResumenDto>> GetLotesAsync(CancellationToken ct)
    {
        var rows = await _lotes.ListLotesAsync(ct);
        var variedades = await _catalogos.ListVariedadesAsync(ct);
        var tiposTueste = await _catalogos.ListTiposTuesteAsync(ct);
        var tiposProceso = await _catalogos.ListTiposProcesoAsync(ct);

        var variedadNames = variedades.ToDictionary(x => x.VariedadCafeId, x => x.Nombre);
        var tipoTuesteNames = tiposTueste.ToDictionary(x => x.TipoTuesteCafeId, x => x.Nombre);
        var tipoProcesoNames = tiposProceso.ToDictionary(x => x.TipoProcesoCafeId, x => x.Nombre);

        return rows.Select(row => MapResumen(
            row,
            variedadNames,
            tipoTuesteNames,
            tipoProcesoNames)).ToArray();
    }

    public async Task<OperationResult<LoteCafeDetalleDto>> GetLoteAsync(
        int loteCafeId,
        CancellationToken ct)
    {
        var lote = await _lotes.FindLoteAsync(loteCafeId, ct);
        if (lote is null)
        {
            return OperationResult<LoteCafeDetalleDto>.NotFound();
        }

        var existencias = await _lotes.ListExistenciasAsync(loteCafeId, ct);

        var variedad = lote.VariedadCafeId.HasValue
            ? await _catalogos.FindVariedadAsync(lote.VariedadCafeId.Value, ct)
            : null;
        var tipoTueste = lote.TipoTuesteCafeId.HasValue
            ? await _catalogos.FindTipoTuesteAsync(lote.TipoTuesteCafeId.Value, ct)
            : null;
        var tipoProceso = lote.TipoProcesoCafeId.HasValue
            ? await _catalogos.FindTipoProcesoAsync(lote.TipoProcesoCafeId.Value, ct)
            : null;

        return OperationResult<LoteCafeDetalleDto>.Success(new LoteCafeDetalleDto
        {
            LoteCafeId = lote.LoteCafeId,
            CodigoLote = lote.CodigoLote,
            FechaProduccion = lote.FechaProduccion,
            FechaEnvasado = lote.FechaEnvasado,
            FechaVencimiento = lote.FechaVencimiento,
            Estado = lote.Estado,
            Observacion = lote.Observacion,
            CreadoEn = lote.CreadoEn,
            ActualizadoEn = lote.ActualizadoEn,
            VariedadCafeId = lote.VariedadCafeId,
            VariedadCafeNombre = variedad?.Nombre,
            TipoTuesteCafeId = lote.TipoTuesteCafeId,
            TipoTuesteCafeNombre = tipoTueste?.Nombre,
            TipoProcesoCafeId = lote.TipoProcesoCafeId,
            TipoProcesoCafeNombre = tipoProceso?.Nombre,
            TotalPaquetes = existencias.Sum(x => x.CantidadPaquetes),
            TotalPesoNetoKg = Redondear(existencias.Sum(
                x => CalcularPesoNetoKg(x.CantidadPaquetes, x.PesoUnitarioGramos))),
        });
    }

    public async Task<OperationResult<IReadOnlyList<ExistenciaCafeDto>>> GetExistenciasAsync(
        int loteCafeId,
        CancellationToken ct)
    {
        if (await _lotes.FindLoteAsync(loteCafeId, ct) is null)
        {
            return OperationResult<IReadOnlyList<ExistenciaCafeDto>>.NotFound();
        }

        var existencias = await _lotes.ListExistenciasAsync(loteCafeId, ct);
        return OperationResult<IReadOnlyList<ExistenciaCafeDto>>.Success(
            existencias.Select(MapExistencia).ToArray());
    }

    public async Task<OperationResult<IReadOnlyList<MovimientoCafeDto>>> GetMovimientosAsync(
        int loteCafeId,
        CancellationToken ct)
    {
        if (await _lotes.FindLoteAsync(loteCafeId, ct) is null)
        {
            return OperationResult<IReadOnlyList<MovimientoCafeDto>>.NotFound();
        }

        var movimientos = await _lotes.ListMovimientosAsync(loteCafeId, ct);
        return OperationResult<IReadOnlyList<MovimientoCafeDto>>.Success(
            movimientos.Select(MapMovimiento).ToArray());
    }

    public async Task<OperationResult<MovimientoCafeDto>> RegisterMovementAsync(
        int loteCafeId,
        RegistrarMovimientoRequest request,
        int? usuarioId,
        CancellationToken ct)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (!TryNormalizeMovement(request, now, out var data, out var errors))
        {
            return OperationResult<MovimientoCafeDto>.Validation(errors);
        }

        try
        {
            return await _unitOfWork.ExecuteInTransactionAsync(
                token => RegisterMovementInTransactionAsync(loteCafeId, data, usuarioId, token),
                ct);
        }
        catch (ConcurrencyConflictException)
        {
            return OperationResult<MovimientoCafeDto>.Conflict();
        }
    }

    private async Task<OperationResult<MovimientoCafeDto>> RegisterMovementInTransactionAsync(
        int loteCafeId,
        MovimientoNormalizado data,
        int? usuarioId,
        CancellationToken ct)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var errors = new Dictionary<string, string[]>();

        var lote = await _lotes.FindLoteAsync(loteCafeId, ct);
        if (lote is null)
        {
            return OperationResult<MovimientoCafeDto>.NotFound();
        }

        if (lote.Estado == EstadoLoteCafe.Bloqueado)
        {
            AddError(errors, "Estado", "El lote está bloqueado y no admite movimientos.");
        }

        if (data.FechaMovimiento < lote.FechaProduccion)
        {
            AddError(errors, nameof(RegistrarMovimientoRequest.FechaMovimiento),
                "La fecha del movimiento no puede ser anterior a la fecha de tueste del lote.");
        }

        var presentacion = await _catalogos.FindPresentacionAsync(data.PresentacionCafeId, ct);
        if (presentacion is null)
        {
            AddError(errors, nameof(RegistrarMovimientoRequest.PresentacionCafeId),
                "La presentación no existe.");
        }
        else if (presentacion.Estado != EstadoCatalogoCafe.Activo)
        {
            AddError(errors, nameof(RegistrarMovimientoRequest.PresentacionCafeId),
                "La presentación está inactiva y no admite movimientos.");
        }

        var tipoCafe = await _catalogos.FindTipoCafeAsync(data.TipoCafeId, ct);
        if (tipoCafe is null)
        {
            AddError(errors, nameof(RegistrarMovimientoRequest.TipoCafeId),
                "El tipo de café no existe.");
        }
        else if (tipoCafe.Estado != EstadoCatalogoCafe.Activo)
        {
            AddError(errors, nameof(RegistrarMovimientoRequest.TipoCafeId),
                "El tipo de café está inactivo y no admite movimientos.");
        }

        if (errors.Count > 0 || presentacion is null || tipoCafe is null)
        {
            return OperationResult<MovimientoCafeDto>.Validation(errors);
        }

        var existencia = await _lotes.FindExistenciaAsync(
            loteCafeId,
            data.PresentacionCafeId,
            data.TipoCafeId,
            ct);

        var esEntrada = data.TipoMovimiento is
            TipoMovimientoLoteCafe.Ingreso or TipoMovimientoLoteCafe.AjustePositivo;

        if (esEntrada)
        {
            if (existencia is null)
            {
                existencia = new LoteCafeExistencia
                {
                    LoteCafeId = loteCafeId,
                    PresentacionCafeId = data.PresentacionCafeId,
                    TipoCafeId = data.TipoCafeId,
                    CantidadPaquetes = data.CantidadPaquetes,
                    PesoUnitarioGramos = presentacion.PesoUnitarioGramos,
                    CreadoEn = now,
                };
                await _lotes.AddExistenciaAsync(existencia, ct);
            }
            else
            {
                if (existencia.PesoUnitarioGramos != presentacion.PesoUnitarioGramos)
                {
                    return OperationResult<MovimientoCafeDto>.Validation(Error(
                        nameof(RegistrarMovimientoRequest.PresentacionCafeId),
                        "El peso de la presentación cambió. No se puede mezclar con existencias históricas del mismo SKU."));
                }

                var nuevaCantidad = (long)existencia.CantidadPaquetes + data.CantidadPaquetes;
                if (nuevaCantidad > int.MaxValue)
                {
                    return OperationResult<MovimientoCafeDto>.Validation(Error(
                        nameof(RegistrarMovimientoRequest.CantidadPaquetes),
                        "La cantidad resultante excede el máximo permitido."));
                }

                existencia.CantidadPaquetes = (int)nuevaCantidad;
                existencia.ActualizadoEn = now;
            }
        }
        else
        {
            if (existencia is null || existencia.CantidadPaquetes < data.CantidadPaquetes)
            {
                return OperationResult<MovimientoCafeDto>.Validation(Error(
                    nameof(RegistrarMovimientoRequest.CantidadPaquetes),
                    "No hay paquetes suficientes para registrar la salida o el ajuste negativo."));
            }

            existencia.CantidadPaquetes -= data.CantidadPaquetes;
            existencia.ActualizadoEn = now;
        }

        var movimiento = new MovimientoLoteCafe
        {
            LoteCafeExistencia = existencia,
            TipoMovimiento = data.TipoMovimiento,
            CantidadPaquetes = data.CantidadPaquetes,
            PesoNetoKg = CalcularPesoNetoKg(data.CantidadPaquetes, presentacion.PesoUnitarioGramos),
            FechaMovimiento = data.FechaMovimiento,
            Referencia = data.Referencia,
            Observacion = data.Observacion,
            UsuarioId = usuarioId,
            CreadoEn = now,
        };

        await _lotes.AddMovimientoAsync(movimiento, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var totalPaquetes = await _lotes.GetTotalPaquetesAsync(loteCafeId, ct);
        var nuevoEstado = lote.Estado;

        if (totalPaquetes == 0 && lote.Estado == EstadoLoteCafe.Activo)
        {
            nuevoEstado = EstadoLoteCafe.Agotado;
        }
        else if (totalPaquetes > 0 && lote.Estado == EstadoLoteCafe.Agotado)
        {
            nuevoEstado = EstadoLoteCafe.Activo;
        }

        if (nuevoEstado != lote.Estado)
        {
            lote.Estado = nuevoEstado;
        }

        // Se actualiza el lote en cada movimiento para que su RowVersion participe
        // en el control de concurrencia y dos movimientos simultáneos del mismo
        // lote no puedan dejar el estado Agotado/Activo inconsistente.
        lote.ActualizadoEn = now;
        await _unitOfWork.SaveChangesAsync(ct);

        return OperationResult<MovimientoCafeDto>.Success(
            MapMovimiento(movimiento, loteCafeId, presentacion, tipoCafe));
    }

    private static bool TryNormalizeLote(
        CreateLoteCafeRequest request,
        DateTime now,
        out LoteNormalizado data,
        out Dictionary<string, string[]> errors)
    {
        data = new LoteNormalizado();
        errors = new Dictionary<string, string[]>();

        var codigo = NormalizeCode(request.CodigoLote);
        if (string.IsNullOrWhiteSpace(codigo))
        {
            AddError(errors, nameof(CreateLoteCafeRequest.CodigoLote), "El código de lote es requerido.");
        }
        else if (codigo.Length > 50)
        {
            AddError(errors, nameof(CreateLoteCafeRequest.CodigoLote),
                "El código de lote no puede exceder 50 caracteres.");
        }
        else
        {
            data.CodigoLote = codigo;
        }

        if (request.FechaProduccion == default)
        {
            AddError(errors, nameof(CreateLoteCafeRequest.FechaProduccion),
                "La fecha de tueste es requerida.");
        }
        else if (request.FechaProduccion > now)
        {
            AddError(errors, nameof(CreateLoteCafeRequest.FechaProduccion),
                "La fecha de tueste no puede ser futura.");
        }
        else
        {
            data.FechaProduccion = request.FechaProduccion;
            data.FechaVencimiento = request.FechaProduccion.AddMonths(3);
        }

        if (request.FechaEnvasado is { } fechaEnvasado)
        {
            if (data.FechaProduccion != default && fechaEnvasado < data.FechaProduccion)
            {
                AddError(errors, nameof(CreateLoteCafeRequest.FechaEnvasado),
                    "La fecha de envasado no puede ser anterior a la fecha de tueste.");
            }
            else
            {
                data.FechaEnvasado = fechaEnvasado;
            }
        }

        // La fecha de vencimiento se calcula en el backend (tueste + 3 meses).
        // No se confía en el valor enviado por el cliente.

        var observacion = NormalizeOptionalText(request.Observacion);
        if (string.IsNullOrWhiteSpace(observacion))
        {
            AddError(errors, nameof(CreateLoteCafeRequest.Observacion),
                "La observación es requerida.");
        }
        else if (observacion.Length > 250)
        {
            AddError(errors, nameof(CreateLoteCafeRequest.Observacion),
                "La observación no puede exceder 250 caracteres.");
        }
        else
        {
            data.Observacion = observacion;
        }

        if (!request.VariedadCafeId.HasValue || request.VariedadCafeId.Value <= 0)
        {
            AddError(errors, nameof(CreateLoteCafeRequest.VariedadCafeId),
                "La variedad es requerida.");
        }
        else
        {
            data.VariedadCafeId = request.VariedadCafeId.Value;
        }

        if (!request.TipoTuesteCafeId.HasValue || request.TipoTuesteCafeId.Value <= 0)
        {
            AddError(errors, nameof(CreateLoteCafeRequest.TipoTuesteCafeId),
                "El tipo de tueste es requerido.");
        }
        else
        {
            data.TipoTuesteCafeId = request.TipoTuesteCafeId.Value;
        }

        if (!request.TipoProcesoCafeId.HasValue || request.TipoProcesoCafeId.Value <= 0)
        {
            AddError(errors, nameof(CreateLoteCafeRequest.TipoProcesoCafeId),
                "El tipo de proceso es requerido.");
        }
        else
        {
            data.TipoProcesoCafeId = request.TipoProcesoCafeId.Value;
        }

        return errors.Count == 0;
    }

    private async Task ValidateLoteCatalogosAsync(
        LoteNormalizado data,
        Dictionary<string, string[]> errors,
        CancellationToken ct)
    {
        if (data.VariedadCafeId.HasValue)
        {
            var variedad = await _catalogos.FindVariedadAsync(data.VariedadCafeId.Value, ct);
            if (variedad is null || variedad.Estado != EstadoCatalogoCafe.Activo)
            {
                AddError(errors, nameof(CreateLoteCafeRequest.VariedadCafeId),
                    "La variedad no existe o no está activa.");
            }
        }

        if (data.TipoTuesteCafeId.HasValue)
        {
            var tipoTueste = await _catalogos.FindTipoTuesteAsync(data.TipoTuesteCafeId.Value, ct);
            if (tipoTueste is null || tipoTueste.Estado != EstadoCatalogoCafe.Activo)
            {
                AddError(errors, nameof(CreateLoteCafeRequest.TipoTuesteCafeId),
                    "El tipo de tueste no existe o no está activo.");
            }
        }

        if (data.TipoProcesoCafeId.HasValue)
        {
            var tipoProceso = await _catalogos.FindTipoProcesoAsync(data.TipoProcesoCafeId.Value, ct);
            if (tipoProceso is null || tipoProceso.Estado != EstadoCatalogoCafe.Activo)
            {
                AddError(errors, nameof(CreateLoteCafeRequest.TipoProcesoCafeId),
                    "El tipo de proceso no existe o no está activo.");
            }
        }
    }

    private static bool TryNormalizeMovement(
        RegistrarMovimientoRequest request,
        DateTime now,
        out MovimientoNormalizado data,
        out Dictionary<string, string[]> errors)
    {
        data = new MovimientoNormalizado();
        errors = new Dictionary<string, string[]>();

        if (!Enum.IsDefined(request.TipoMovimiento))
        {
            AddError(errors, nameof(RegistrarMovimientoRequest.TipoMovimiento),
                "El tipo de movimiento no es válido.");
        }
        else
        {
            data.TipoMovimiento = request.TipoMovimiento;
        }

        if (request.PresentacionCafeId <= 0)
        {
            AddError(errors, nameof(RegistrarMovimientoRequest.PresentacionCafeId),
                "La presentación es requerida.");
        }
        else
        {
            data.PresentacionCafeId = request.PresentacionCafeId;
        }

        if (request.TipoCafeId <= 0)
        {
            AddError(errors, nameof(RegistrarMovimientoRequest.TipoCafeId),
                "El tipo de café es requerido.");
        }
        else
        {
            data.TipoCafeId = request.TipoCafeId;
        }

        if (request.CantidadPaquetes <= 0)
        {
            AddError(errors, nameof(RegistrarMovimientoRequest.CantidadPaquetes),
                "La cantidad de paquetes debe ser mayor que cero.");
        }
        else
        {
            data.CantidadPaquetes = request.CantidadPaquetes;
        }

        var fechaMovimiento = request.FechaMovimiento ?? now;
        if (fechaMovimiento > now)
        {
            AddError(errors, nameof(RegistrarMovimientoRequest.FechaMovimiento),
                "La fecha del movimiento no puede ser futura.");
        }
        else
        {
            data.FechaMovimiento = fechaMovimiento;
        }

        var referencia = NormalizeOptionalText(request.Referencia);
        if (referencia is { Length: > 100 })
        {
            AddError(errors, nameof(RegistrarMovimientoRequest.Referencia),
                "La referencia no puede exceder 100 caracteres.");
        }
        else
        {
            data.Referencia = referencia;
        }

        var observacion = NormalizeOptionalText(request.Observacion);
        if (observacion is { Length: > 250 })
        {
            AddError(errors, nameof(RegistrarMovimientoRequest.Observacion),
                "La observación no puede exceder 250 caracteres.");
        }
        else
        {
            data.Observacion = observacion;
        }

        if (data.TipoMovimiento is TipoMovimientoLoteCafe.AjustePositivo or TipoMovimientoLoteCafe.AjusteNegativo &&
            string.IsNullOrWhiteSpace(data.Observacion))
        {
            AddError(errors, nameof(RegistrarMovimientoRequest.Observacion),
                "Los ajustes requieren una justificación en la observación.");
        }

        return errors.Count == 0;
    }

    private static LoteCafeResumenDto MapResumen(
        LoteCafeResumenRow row,
        IReadOnlyDictionary<int, string> variedadNames,
        IReadOnlyDictionary<int, string> tipoTuesteNames,
        IReadOnlyDictionary<int, string> tipoProcesoNames) =>
        new()
        {
            LoteCafeId = row.LoteCafeId,
            CodigoLote = row.CodigoLote,
            FechaProduccion = row.FechaProduccion,
            FechaEnvasado = row.FechaEnvasado,
            FechaVencimiento = row.FechaVencimiento,
            Estado = row.Estado,
            Observacion = row.Observacion,
            CreadoEn = row.CreadoEn,
            ActualizadoEn = row.ActualizadoEn,
            VariedadCafeId = row.VariedadCafeId,
            VariedadCafeNombre = row.VariedadCafeId.HasValue &&
                variedadNames.TryGetValue(row.VariedadCafeId.Value, out var variedadNombre)
                ? variedadNombre
                : null,
            TipoTuesteCafeId = row.TipoTuesteCafeId,
            TipoTuesteCafeNombre = row.TipoTuesteCafeId.HasValue &&
                tipoTuesteNames.TryGetValue(row.TipoTuesteCafeId.Value, out var tipoTuesteNombre)
                ? tipoTuesteNombre
                : null,
            TipoProcesoCafeId = row.TipoProcesoCafeId,
            TipoProcesoCafeNombre = row.TipoProcesoCafeId.HasValue &&
                tipoProcesoNames.TryGetValue(row.TipoProcesoCafeId.Value, out var tipoProcesoNombre)
                ? tipoProcesoNombre
                : null,
            TotalPaquetes = row.TotalPaquetes,
            TotalPesoNetoKg = Redondear(row.TotalPesoNetoKg),
        };

    private static ExistenciaCafeDto MapExistencia(ExistenciaCafeRow row) =>
        new()
        {
            LoteCafeExistenciaId = row.LoteCafeExistenciaId,
            LoteCafeId = row.LoteCafeId,
            PresentacionCafeId = row.PresentacionCafeId,
            PresentacionCodigo = row.PresentacionCodigo,
            PresentacionNombre = row.PresentacionNombre,
            PresentacionEstado = row.PresentacionEstado,
            TipoCafeId = row.TipoCafeId,
            TipoCafeCodigo = row.TipoCafeCodigo,
            TipoCafeNombre = row.TipoCafeNombre,
            TipoCafeEstado = row.TipoCafeEstado,
            CantidadPaquetes = row.CantidadPaquetes,
            PesoUnitarioGramos = row.PesoUnitarioGramos,
            PesoNetoKg = CalcularPesoNetoKg(row.CantidadPaquetes, row.PesoUnitarioGramos),
            ActualizadoEn = row.ActualizadoEn,
        };

    private static MovimientoCafeDto MapMovimiento(MovimientoCafeRow row) =>
        new()
        {
            MovimientoLoteCafeId = row.MovimientoLoteCafeId,
            LoteCafeExistenciaId = row.LoteCafeExistenciaId,
            LoteCafeId = row.LoteCafeId,
            TipoMovimiento = row.TipoMovimiento,
            CantidadPaquetes = row.CantidadPaquetes,
            PesoNetoKg = Redondear(row.PesoNetoKg),
            FechaMovimiento = row.FechaMovimiento,
            Referencia = row.Referencia,
            Observacion = row.Observacion,
            UsuarioId = row.UsuarioId,
            CreadoEn = row.CreadoEn,
            PresentacionCafeId = row.PresentacionCafeId,
            PresentacionCodigo = row.PresentacionCodigo,
            PresentacionNombre = row.PresentacionNombre,
            TipoCafeId = row.TipoCafeId,
            TipoCafeCodigo = row.TipoCafeCodigo,
            TipoCafeNombre = row.TipoCafeNombre,
        };

    private static MovimientoCafeDto MapMovimiento(
        MovimientoLoteCafe movimiento,
        int loteCafeId,
        PresentacionCafe presentacion,
        TipoCafe tipoCafe) =>
        new()
        {
            MovimientoLoteCafeId = movimiento.MovimientoLoteCafeId,
            LoteCafeExistenciaId = movimiento.LoteCafeExistenciaId,
            LoteCafeId = loteCafeId,
            TipoMovimiento = movimiento.TipoMovimiento,
            CantidadPaquetes = movimiento.CantidadPaquetes,
            PesoNetoKg = Redondear(movimiento.PesoNetoKg),
            FechaMovimiento = movimiento.FechaMovimiento,
            Referencia = movimiento.Referencia,
            Observacion = movimiento.Observacion,
            UsuarioId = movimiento.UsuarioId,
            CreadoEn = movimiento.CreadoEn,
            PresentacionCafeId = presentacion.PresentacionCafeId,
            PresentacionCodigo = presentacion.Codigo,
            PresentacionNombre = presentacion.Nombre,
            TipoCafeId = tipoCafe.TipoCafeId,
            TipoCafeCodigo = tipoCafe.Codigo,
            TipoCafeNombre = tipoCafe.Nombre,
        };

    private static decimal CalcularPesoNetoKg(int cantidadPaquetes, int pesoUnitarioGramos) =>
        Redondear((decimal)cantidadPaquetes * pesoUnitarioGramos / 1000m);

    private static decimal Redondear(decimal value) =>
        Math.Round(value, 3, MidpointRounding.AwayFromZero);

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

    private sealed class LoteNormalizado
    {
        public string CodigoLote { get; set; } = string.Empty;
        public DateTime FechaProduccion { get; set; }
        public DateTime? FechaEnvasado { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public string? Observacion { get; set; }
        public int? VariedadCafeId { get; set; }
        public int? TipoTuesteCafeId { get; set; }
        public int? TipoProcesoCafeId { get; set; }
    }

    private sealed class MovimientoNormalizado
    {
        public TipoMovimientoLoteCafe TipoMovimiento { get; set; }
        public int PresentacionCafeId { get; set; }
        public int TipoCafeId { get; set; }
        public int CantidadPaquetes { get; set; }
        public DateTime FechaMovimiento { get; set; }
        public string? Referencia { get; set; }
        public string? Observacion { get; set; }
    }
}
