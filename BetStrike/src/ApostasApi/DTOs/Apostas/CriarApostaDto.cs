using System.ComponentModel.DataAnnotations;

namespace ApostasApi.DTOs.Apostas;

public class CriarApostaDto
{
     public int JogoId { get; set; }
    public int UtilizadorId { get; set; }
    public string TipoAposta { get; set; } = string.Empty;
    public decimal Montante { get; set; }
    public decimal Odd { get; set; }
}