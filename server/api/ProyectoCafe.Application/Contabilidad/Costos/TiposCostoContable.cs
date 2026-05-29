namespace ProyectoCafe.Application.Contabilidad.Costos;

public static class TiposCostoContable
{
    public const string Manual = "Manual";
    public const string OperacionAgricola = "OperacionAgricola";
    public const string CosechaCafe = "CosechaCafe";
    public const string TransformacionCafe = "TransformacionCafe";
    public const string EmpaqueCafe = "EmpaqueCafe";

    public static readonly string[] All = [Manual, OperacionAgricola, CosechaCafe, TransformacionCafe, EmpaqueCafe];

    public static string Normalize(string? value)
    {
        var normalized = (value ?? string.Empty).Trim();

        if (normalized.Equals(OperacionAgricola, StringComparison.OrdinalIgnoreCase))
        {
            return OperacionAgricola;
        }

        if (normalized.Equals(CosechaCafe, StringComparison.OrdinalIgnoreCase))
        {
            return CosechaCafe;
        }

        if (normalized.Equals(TransformacionCafe, StringComparison.OrdinalIgnoreCase))
        {
            return TransformacionCafe;
        }

        if (normalized.Equals(EmpaqueCafe, StringComparison.OrdinalIgnoreCase))
        {
            return EmpaqueCafe;
        }

        return Manual;
    }
}
