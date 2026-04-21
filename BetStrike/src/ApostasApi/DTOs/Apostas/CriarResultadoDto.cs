using System.ComponentModel.DataAnnotations;

namespace ApostasApi.DTOs.Resultados;

public class CriarResultadoDto
{
    [Required]
    public string CodigoJogo { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int GolosCasa { get; set; }

    [Range(0, int.MaxValue)]
    public int GolosFora { get; set; }
}