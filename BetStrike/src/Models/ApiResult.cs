namespace ApostasApi.Models;

public enum EstadoJogo
{
    Agendado = 1,
    EmCurso = 2,
    Finalizado = 3,
    Cancelado = 4,
    Adiado = 5
}

public enum EstadoAposta
{
    Pendente = 1,
    Ganha = 2,
    Perdida = 3,
    Anulada = 4
}