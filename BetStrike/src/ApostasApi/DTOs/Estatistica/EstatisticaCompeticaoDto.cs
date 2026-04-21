namespace ApostasApi.DTOs.Estatisticas;

public class EstatisticaCompeticaoDto
{
    public string Competicao { get; set; } = string.Empty;
    public decimal MediaGolosPorJogo { get; set; }
    public decimal VolumeTotalApostado { get; set; }
    public decimal TaxaVitoria1 { get; set; }
    public decimal TaxaVitoriaX { get; set; }
    public decimal TaxaVitoria2 { get; set; }
}