using System.ComponentModel.DataAnnotations;

namespace ApostasApi.DTOs.Jogos;

public class CriarJogoDto
{
    [Required]
    public string CodigoJogo { get; set; } = string.Empty;

    [Required]
    public DateTime DataHora { get; set; }

    [Required]
    public string EquipaCasa { get; set; } = string.Empty;

    [Required]
    public string EquipaFora { get; set; } = string.Empty;

    [Required]
    public string Competicao { get; set; } = string.Empty;

    [Range(1, 5)]
    public int Estado { get; set; }
}