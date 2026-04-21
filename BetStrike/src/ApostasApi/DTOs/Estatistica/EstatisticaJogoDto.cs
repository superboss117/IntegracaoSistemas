namespace ApostasApi.DTOs.Estatisticas;

public class EstatisticaJogoDto
{
    public string CodigoJogo { get; set; } = string.Empty;
    public decimal TotalApostado { get; set; }
    public int ApostasTipo1 { get; set; }
    public int ApostasTipoX { get; set; }
    public int ApostasTipo2 { get; set; }
    public int Pendentes { get; set; }
    public int Ganhas { get; set; }
    public int Perdidas { get; set; }
    public int Anuladas { get; set; }
    public decimal MargemPlataforma { get; set; }
}