namespace ProyectoCafe.Domain.Entities;

public sealed class CultivoConfiguracionCosecha
{
    public int CultivoConfiguracionCosechaId { get; set; }

    public int CultivoId { get; set; }

    public required string NombreTipoCosecha { get; set; }

    public required string UnidadMedidaCodigo { get; set; }
}