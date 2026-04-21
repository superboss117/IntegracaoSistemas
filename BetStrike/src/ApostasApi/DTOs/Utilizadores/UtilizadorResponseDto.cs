namespace ApostasApi.DTOs.Utilizadores;

public class UtilizadorResponseDto
{
    public int UtilizadorId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public decimal SaldoInicial { get; set; }
}