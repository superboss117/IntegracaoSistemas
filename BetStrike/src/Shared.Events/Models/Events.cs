using System;

namespace Shared.Events.Models;

public class JogoCriadoEvent
{
    public string CodigoJogo { get; set; } = string.Empty;
    public string EquipaCasa { get; set; } = string.Empty;
    public string EquipaFora { get; set; } = string.Empty;
    public DateTime DataHora { get; set; }
}

public class JogoAtualizadoEvent
{
    public string CodigoJogo { get; set; } = string.Empty;
    public int Estado { get; set; }
}

public class JogoFinalizadoEvent
{
    public string CodigoJogo { get; set; } = string.Empty;
    public int GolosCasa { get; set; }
    public int GolosFora { get; set; }
}

public class ResultadoAtualizadoEvent
{
    public string CodigoJogo { get; set; } = string.Empty;
    public int GolosCasa { get; set; }
    public int GolosFora { get; set; }
}

public class ApostaCriadaEvent
{
    public int ApostaId { get; set; }
    public int UtilizadorId { get; set; }
    public string CodigoJogo { get; set; } = string.Empty;
    public string TipoAposta { get; set; } = string.Empty;
    public decimal Montante { get; set; }
    public decimal Odd { get; set; }
}

public class ApostaResolvidaEvent
{
    public int ApostaId { get; set; }
    public string Resultado { get; set; } = string.Empty;
    public decimal Ganho { get; set; }
}

public class ApostaCanceladaEvent
{
    public int ApostaId { get; set; }
}

public class AlertaGeradoEvent
{
    public string Tipo { get; set; } = string.Empty;
    public string Nivel { get; set; } = string.Empty;
    public string Detalhes { get; set; } = string.Empty;
    public DateTime DataHora { get; set; }
}
