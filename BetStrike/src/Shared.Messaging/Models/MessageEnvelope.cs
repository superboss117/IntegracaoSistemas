using System;

namespace Shared.Messaging.Models;

public class MessageEnvelope<T>
{
    public Guid MessageId { get; set; } = Guid.NewGuid();
    public string MessageType { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string Source { get; set; } = string.Empty;
    public T Payload { get; set; } = default!;
    public int RetryCount { get; set; } = 0;
}

public class AuditarAcaoCommand
{
    public string Acao { get; set; } = string.Empty;
    public string Detalhes { get; set; } = string.Empty;
    public string Utilizador { get; set; } = string.Empty;
}

public class EnviarNotificacaoCommand
{
    public int UtilizadorId { get; set; }
    public string Mensagem { get; set; } = string.Empty;
    public string TipoNotificacao { get; set; } = string.Empty;
}

public class ProcessarAlertaCommand
{
    public string Alerta { get; set; } = string.Empty;
    public string Nivel { get; set; } = string.Empty;
}
