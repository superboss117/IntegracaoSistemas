using System.ComponentModel.DataAnnotations;

namespace ApostasApi.DTOs.Apostas;

public class CriarApostaDto
{
    [Range(1, int.MaxValue)]
    public int IdUtilizador { get; set; }

    [Required]
    public string CodigoJogo { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(1|X|2)$", ErrorMessage = "TipoAposta tem de ser 1, X ou 2")]
    public string TipoAposta { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue)]
    public decimal ValorApostado { get; set; }

    [Range(typeof(decimal), "1.01", "999999")]
    public decimal OddMomento { get; set; }
}