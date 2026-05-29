namespace ProyectoCafe.Application.Produccion.TransformacionesCafe;

public static class TiposTransformacionCafe
{
    public const string Despulpado = "Despulpado";
    public const string Secado = "Secado";
    public const string Trillado = "Trillado";
    public const string Tostion = "Tostion";
    public const string Empaque = "Empaque";
    public const string Empaque454g = "Empaque454g";

    public static readonly string[] All = [Despulpado, Secado, Trillado, Tostion, Empaque, Empaque454g];

    public static string? Normalize(string? value)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        if (normalized.Equals(Despulpado, StringComparison.OrdinalIgnoreCase))
        {
            return Despulpado;
        }

        if (normalized.Equals(Secado, StringComparison.OrdinalIgnoreCase))
        {
            return Secado;
        }

        if (normalized.Equals(Trillado, StringComparison.OrdinalIgnoreCase))
        {
            return Trillado;
        }

        if (normalized.Equals(Tostion, StringComparison.OrdinalIgnoreCase))
        {
            return Tostion;
        }

        if (normalized.Equals(Empaque, StringComparison.OrdinalIgnoreCase))
        {
            return Empaque;
        }

        if (normalized.Equals(Empaque454g, StringComparison.OrdinalIgnoreCase))
        {
            return Empaque454g;
        }

        return null;
    }
}
