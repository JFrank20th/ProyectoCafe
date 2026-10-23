namespace ProyectoCafe.Application.Produccion.Cultivos;

public sealed class CultivoConfiguracionCosechaResponse
{
    public int CultivoConfiguracionCosechaId { get; set; }

    public required string NombreTipoCosecha { get; set; }

    public required string UnidadMedidaCodigo { get; set; }

    public required string UnidadMedidaNombre { get; set; }
}