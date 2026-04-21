using System.ComponentModel.DataAnnotations;

namespace ApostasApi.DTOs.Utilizadores;

public class CriarUtilizadorDto
{
    [Required]
    [StringLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;
}