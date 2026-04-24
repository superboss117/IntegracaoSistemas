using Microsoft.Data.SqlClient;
using ResultadosAPI.DTOs;
using System.Data;

namespace ResultadosAPI.Services
{
    public class JogoService
    {
        private readonly string _connectionString;

        public JogoService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new Exception("Connection string DefaultConnection não foi carregada.");
        }

        public async Task<(int id, string codigo)> InserirJogoAsync(InserirJogoDTO dto)
        {
            Console.WriteLine("=== DEBUG INSERIR JOGO ===");
            Console.WriteLine($"Codigo: {dto.Codigo_Jogo}");
            Console.WriteLine($"Data: {dto.Data_Jogo:yyyy-MM-dd}");
            Console.WriteLine($"Hora: {dto.Hora_Inicio}");
            Console.WriteLine($"Casa: {dto.Equipa_Casa}");
            Console.WriteLine($"Fora: {dto.Equipa_Fora}");

            if (dto.Data_Jogo.Year < 1753)
                throw new Exception($"Data inválida recebida na API: {dto.Data_Jogo:yyyy-MM-dd}");

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_InserirJogo", conn);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@Codigo_Jogo", SqlDbType.VarChar, 20).Value = dto.Codigo_Jogo;
            cmd.Parameters.Add("@Data_Jogo", SqlDbType.Date).Value = dto.Data_Jogo.Date;
            cmd.Parameters.Add("@Hora_Inicio", SqlDbType.Time).Value = dto.Hora_Inicio;
            cmd.Parameters.Add("@Equipa_Casa", SqlDbType.NVarChar, 100).Value = dto.Equipa_Casa;
            cmd.Parameters.Add("@Equipa_Fora", SqlDbType.NVarChar, 100).Value = dto.Equipa_Fora;

            await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return (
                    Convert.ToInt32(reader["Id_Inserido"]),
                    reader["Codigo_Jogo"].ToString()!
                );
            }

            throw new Exception("Erro ao inserir jogo.");
        }

        public async Task AtualizarJogoAsync(string codigoJogo, AtualizarJogoDTO dto)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_AtualizarJogo", conn);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@Codigo_Jogo", SqlDbType.VarChar, 20).Value = codigoJogo;
            cmd.Parameters.Add("@Novo_Estado", SqlDbType.Int).Value = dto.Novo_Estado;
            cmd.Parameters.Add("@Golos_Casa", SqlDbType.Int).Value = dto.Golos_Casa;
            cmd.Parameters.Add("@Golos_Fora", SqlDbType.Int).Value = dto.Golos_Fora;

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<JogoRespostaDTO>> ListarJogosAsync(DateTime? data, int? estado)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_ListarJogos", conn);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@Data", SqlDbType.Date).Value =
                data.HasValue ? data.Value.Date : DBNull.Value;

            cmd.Parameters.Add("@Estado", SqlDbType.Int).Value =
                estado.HasValue ? estado.Value : DBNull.Value;

            await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();

            var jogos = new List<JogoRespostaDTO>();

            while (await reader.ReadAsync())
            {
                jogos.Add(MapearJogo(reader));
            }

            return jogos;
        }

        public async Task<JogoRespostaDTO?> ObterJogoAsync(string codigoJogo)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_ObterJogo", conn);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@Codigo_Jogo", SqlDbType.VarChar, 20).Value = codigoJogo;

            await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
                return MapearJogo(reader);

            return null;
        }

        public async Task RemoverJogoAsync(string codigoJogo)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_RemoverJogo", conn);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@Codigo_Jogo", SqlDbType.VarChar, 20).Value = codigoJogo;

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        private JogoRespostaDTO MapearJogo(SqlDataReader reader)
        {
            return new JogoRespostaDTO
            {
                Id = Convert.ToInt32(reader["Id"]),
                Codigo_Jogo = reader["Codigo_Jogo"].ToString()!,
                Data_Jogo = Convert.ToDateTime(reader["Data_Jogo"]),
                Hora_Inicio = (TimeSpan)reader["Hora_Inicio"],
                Equipa_Casa = reader["Equipa_Casa"].ToString()!,
                Equipa_Fora = reader["Equipa_Fora"].ToString()!,
                Golos_Casa = Convert.ToInt32(reader["Golos_Casa"]),
                Golos_Fora = Convert.ToInt32(reader["Golos_Fora"]),
                Estado = Convert.ToInt32(reader["Estado"]),
                Data_Criacao = Convert.ToDateTime(reader["Data_Criacao"]),
                Data_Atualizacao = Convert.ToDateTime(reader["Data_Atualizacao"])
            };
        }
    }
}