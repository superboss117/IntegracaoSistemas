using Microsoft.Data.SqlClient;
using ResultadosAPI.DTOs;
using ResultadosAPI.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace ResultadosAPI.Services
{
    public class JogoService
    {
        private readonly string _connectionString;

        public JogoService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("Resultados")!;
        }

        public async Task<(int id, string codigo)> InserirJogoAsync(InserirJogoDTO dto)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_InserirJogo", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Codigo_Jogo", dto.Codigo_Jogo);
            cmd.Parameters.AddWithValue("@Data_Jogo", dto.Data_Jogo.Date);
            cmd.Parameters.AddWithValue("@Hora_Inicio", dto.Hora_Inicio);
            cmd.Parameters.AddWithValue("@Equipa_Casa", dto.Equipa_Casa);
            cmd.Parameters.AddWithValue("@Equipa_Fora", dto.Equipa_Fora);
            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                return (Convert.ToInt32(reader["Id_Inserido"]), reader["Codigo_Jogo"].ToString()!);
            throw new Exception("Erro ao inserir jogo.");
        }

        public async Task AtualizarJogoAsync(string codigoJogo, AtualizarJogoDTO dto)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_AtualizarJogo", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Codigo_Jogo", codigoJogo);
            cmd.Parameters.AddWithValue("@Novo_Estado", dto.Novo_Estado);
            cmd.Parameters.AddWithValue("@Golos_Casa", dto.Golos_Casa);
            cmd.Parameters.AddWithValue("@Golos_Fora", dto.Golos_Fora);
            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<JogoRespostaDTO>> ListarJogosAsync(DateTime? data, int? estado)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_ListarJogos", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Data", data.HasValue ? data.Value.Date : DBNull.Value);
            cmd.Parameters.AddWithValue("@Estado", estado.HasValue ? estado.Value : DBNull.Value);
            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            var jogos = new List<JogoRespostaDTO>();
            while (await reader.ReadAsync())
                jogos.Add(MapearJogo(reader));
            return jogos;
        }

        public async Task<JogoRespostaDTO?> ObterJogoAsync(string codigoJogo)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_ObterJogo", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Codigo_Jogo", codigoJogo);
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
            cmd.Parameters.AddWithValue("@Codigo_Jogo", codigoJogo);
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