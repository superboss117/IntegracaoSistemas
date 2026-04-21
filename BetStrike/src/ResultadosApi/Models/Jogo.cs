using System;

namespace ResultadosAPI.Models
{
    public class Jogo
    {
        public int Id { get; set; }
        public string Codigo_Jogo { get; set; } = string.Empty;
        public DateTime Data_Jogo { get; set; }
        public TimeSpan Hora_Inicio { get; set; }
        public string Equipa_Casa { get; set; } = string.Empty;
        public string Equipa_Fora { get; set; } = string.Empty;
        public int Golos_Casa { get; set; }
        public int Golos_Fora { get; set; }
        public int Estado { get; set; }
        public DateTime Data_Criacao { get; set; }
        public DateTime Data_Atualizacao { get; set; }
    }
}