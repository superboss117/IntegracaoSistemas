using System.ComponentModel.DataAnnotations;

namespace ApostasApi.DTOs.Resultados;

public class CriarResultadoDto
{
    public string CodigoJogo { get; set; } = string.Empty;
    public int GolosCasa { get; set; }
    public int GolosFora { get; set; }
}