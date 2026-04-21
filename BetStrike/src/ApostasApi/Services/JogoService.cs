using ApostasApi.Data;
using ApostasApi.DTOs.Jogos;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ApostasApi.Services
{
    public class JogoService : IJogoService
    {
        private readonly IDbExecutor _db;

        public JogoService(IDbExecutor db)
        {
            _db = db;
        }

        public async Task<JogoDto> CriarJogoAsync(CriarJogoDto dto)
        {
            var parameters = new List<IDataParameter>
            {
                new SqlParameter("@Codigo_Jogo", dto.CodigoJogo),
                new SqlParameter("@Data_Hora", dto.DataHora),
                new SqlParameter("@Equipa_Casa", dto.EquipaCasa),
                new SqlParameter("@Equipa_Fora", dto.EquipaFora),
                new SqlParameter("@Competicao", dto.Competicao),
                new SqlParameter("@Estado", dto.Estado)
            };

            var result = await _db.QueryAsync("SP_Inserir_Jogo", parameters);

            if (result.Rows.Count == 0)
            {
                throw new Exception("A stored procedure não devolveu dados do jogo criado.");
            }

            return MapRowToJogoDto(result.Rows[0]);
        }

        public async Task AtualizarJogoAsync(string codigoJogo, AtualizarJogoDto dto)
        {
            var parameters = new List<IDataParameter>
            {
                new SqlParameter("@Codigo_Jogo", codigoJogo),
                new SqlParameter("@Novo_Estado", dto.NovoEstado),
                new SqlParameter("@Golos_Casa", (object?)dto.GolosCasa ?? DBNull.Value),
                new SqlParameter("@Golos_Fora", (object?)dto.GolosFora ?? DBNull.Value)
            };

            await _db.ExecuteAsync("SP_Atualizar_Jogo", parameters);
        }

        public async Task<List<JogoDto>> ListarJogosAsync(DateTime? data, int? estado, string? competicao)
        {
            var parameters = new List<IDataParameter>
            {
                new SqlParameter("@Data", (object?)data ?? DBNull.Value),
                new SqlParameter("@Estado", (object?)estado ?? DBNull.Value),
                new SqlParameter("@Competicao", (object?)competicao ?? DBNull.Value)
            };

            var result = await _db.QueryAsync("SP_Listar_Jogos", parameters);

            var jogos = new List<JogoDto>();

            foreach (DataRow row in result.Rows)
            {
                jogos.Add(MapRowToJogoDto(row));
            }

            return jogos;
        }

        public async Task<JogoDto?> ObterJogoAsync(string codigoJogo)
        {
            var parameters = new List<IDataParameter>
            {
                new SqlParameter("@Codigo_Jogo", codigoJogo)
            };

            var result = await _db.QueryAsync("SP_Obter_Jogo", parameters);

            if (result.Rows.Count == 0)
                return null;

            return MapRowToJogoDto(result.Rows[0]);
        }

        public async Task RemoverJogoAsync(string codigoJogo)
        {
            var parameters = new List<IDataParameter>
            {
                new SqlParameter("@Codigo_Jogo", codigoJogo)
            };

            await _db.ExecuteAsync("SP_Remover_Jogo", parameters);
        }

        private static JogoDto MapRowToJogoDto(DataRow row)
        {
            return new JogoDto
            {
                Id = row.Table.Columns.Contains("Id") && row["Id"] != DBNull.Value ? Convert.ToInt32(row["Id"]) : 0,
                CodigoJogo = row.Table.Columns.Contains("Codigo_Jogo") ? row["Codigo_Jogo"]?.ToString() ?? string.Empty : string.Empty,
                DataHora = row.Table.Columns.Contains("Data_Hora") && row["Data_Hora"] != DBNull.Value ? Convert.ToDateTime(row["Data_Hora"]) : default,
                EquipaCasa = row.Table.Columns.Contains("Equipa_Casa") ? row["Equipa_Casa"]?.ToString() ?? string.Empty : string.Empty,
                EquipaFora = row.Table.Columns.Contains("Equipa_Fora") ? row["Equipa_Fora"]?.ToString() ?? string.Empty : string.Empty,
                Competicao = row.Table.Columns.Contains("Competicao") ? row["Competicao"]?.ToString() ?? string.Empty : string.Empty,
                Estado = row.Table.Columns.Contains("Estado") && row["Estado"] != DBNull.Value ? Convert.ToInt32(row["Estado"]) : 0
            };
        }
    }
}