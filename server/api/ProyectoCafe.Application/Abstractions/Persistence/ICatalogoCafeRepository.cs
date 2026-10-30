using ProyectoCafe.Domain.Entities;

namespace ProyectoCafe.Application.Abstractions.Persistence;

public interface ICatalogoCafeRepository
{
    Task<IReadOnlyList<PresentacionCafe>> ListPresentacionesAsync(CancellationToken ct);

    Task<PresentacionCafe?> FindPresentacionAsync(int presentacionCafeId, CancellationToken ct);

    Task<bool> PresentacionCodigoExistsAsync(
        string codigo,
        int? excludePresentacionCafeId,
        CancellationToken ct);

    Task<bool> PresentacionNombreExistsAsync(
        string nombre,
        int? excludePresentacionCafeId,
        CancellationToken ct);

    Task AddPresentacionAsync(PresentacionCafe presentacion, CancellationToken ct);

    Task<bool> PresentacionHasExistenciasAsync(int presentacionCafeId, CancellationToken ct);

    Task<bool> PresentacionHasStockAsync(int presentacionCafeId, CancellationToken ct);

    Task<IReadOnlyList<TipoCafe>> ListTiposCafeAsync(CancellationToken ct);

    Task<TipoCafe?> FindTipoCafeAsync(int tipoCafeId, CancellationToken ct);

    Task<bool> TipoCafeCodigoExistsAsync(string codigo, int? excludeTipoCafeId, CancellationToken ct);

    Task<bool> TipoCafeNombreExistsAsync(string nombre, int? excludeTipoCafeId, CancellationToken ct);

    Task AddTipoCafeAsync(TipoCafe tipoCafe, CancellationToken ct);

    Task<bool> TipoCafeHasStockAsync(int tipoCafeId, CancellationToken ct);
}
