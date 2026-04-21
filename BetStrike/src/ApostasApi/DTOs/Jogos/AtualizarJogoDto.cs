using System.ComponentModel.DataAnnotations;

namespace ApostasApi.DTOs.Jogos;

public class AtualizarJogoDto

{
    public int NovoEstado { get; set; }
    public int? GolosCasa { get; set; }
    public int? GolosFora { get; set; }
}