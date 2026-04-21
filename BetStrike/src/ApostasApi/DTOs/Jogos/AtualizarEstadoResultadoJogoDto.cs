using System.ComponentModel.DataAnnotations;

namespace ApostasApi.DTOs.Jogos;

public class AtualizarEstadoResultadoJogoDto
{
    [Range(1, 5)]
    public int EstadoJogo { get; set; }

    [Range(0, int.MaxValue)]
    public int GolosCasa { get; set; }

    [Range(0, int.MaxValue)]
    public int GolosFora { get; set; }
}