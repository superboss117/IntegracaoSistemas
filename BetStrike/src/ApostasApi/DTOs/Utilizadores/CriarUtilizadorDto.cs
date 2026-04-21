using System.ComponentModel.DataAnnotations;

namespace ApostasApi.DTOs.Utilizadores;

public class CriarUtilizadorDto
{
    public int UtilizadorId { get; set; }
    public string Nome { get; set; } = string.Empty;
}