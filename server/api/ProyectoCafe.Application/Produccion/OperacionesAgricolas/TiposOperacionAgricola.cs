namespace ProyectoCafe.Application.Produccion.OperacionesAgricolas;

public static class TiposOperacionAgricola
{
    public const string AplicacionInsumo = "AplicacionInsumo";
    public const string Fertilizacion = "Fertilizacion";
    public const string Poda = "Poda";
    public const string Riego = "Riego";
    public const string Jornal = "Jornal";
    public const string Mantenimiento = "Mantenimiento";
    public const string Cosecha = "Cosecha";

    public static readonly string[] All = [AplicacionInsumo, Fertilizacion, Poda, Riego, Jornal, Mantenimiento, Cosecha];

    public static string? Normalize(string? value)
    {
        var normalized = (value ?? string.Empty).Trim();

        if (normalized.Equals(AplicacionInsumo, StringComparison.OrdinalIgnoreCase))
        {
            return AplicacionInsumo;
        }

        if (normalized.Equals(Fertilizacion, StringComparison.OrdinalIgnoreCase))
        {
            return Fertilizacion;
        }

        if (normalized.Equals(Poda, StringComparison.OrdinalIgnoreCase))
        {
            return Poda;
        }

        if (normalized.Equals(Riego, StringComparison.OrdinalIgnoreCase))
        {
            return Riego;
        }

        if (normalized.Equals(Jornal, StringComparison.OrdinalIgnoreCase))
        {
            return Jornal;
        }

        if (normalized.Equals(Mantenimiento, StringComparison.OrdinalIgnoreCase))
        {
            return Mantenimiento;
        }

        if (normalized.Equals(Cosecha, StringComparison.OrdinalIgnoreCase))
        {
            return Cosecha;
        }

        return null;
    }
}
