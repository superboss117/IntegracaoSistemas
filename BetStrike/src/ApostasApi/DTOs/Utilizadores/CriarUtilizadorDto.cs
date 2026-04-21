using System.ComponentModel.DataAnnotations;

namespace ApostasApi.DTOs.Utilizadores;

public class CriarUtilizadorDto
{
    [Required]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}