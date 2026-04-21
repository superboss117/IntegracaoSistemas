namespace ApostasApi.DTOs.Jogos
{
public class JogoDto
{
    public int Id { get; set; }
    public string CodigoJogo { get; set; } = string.Empty;
    public DateTime DataHora { get; set; }
    public string EquipaCasa { get; set; } = string.Empty;
    public string EquipaFora { get; set; } = string.Empty;
    public string Competicao { get; set; } = string.Empty;
    public int Estado { get; set; }
    public int? GolosCasa { get; set; }
    public int? GolosFora { get; set; }
}
}