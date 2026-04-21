namespace ApostasApi.DTOs.Jogos
{
    public class JogoResultadosDto
    {
        public string Codigo_Jogo { get; set; } = string.Empty;
        public DateTime Data_Jogo { get; set; }
        public TimeSpan Hora_Inicio { get; set; }
        public string Equipa_Casa { get; set; } = string.Empty;
        public string Equipa_Fora { get; set; } = string.Empty;
        public int Golos_Casa { get; set; }
        public int Golos_Fora { get; set; }
        public int Estado { get; set; }
    }
}