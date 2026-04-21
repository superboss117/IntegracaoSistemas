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

        public async Task<object> CriarJogoAsync(CriarJogoDto dto)
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

            return await _db.QueryAsync("SP_Inserir_Jogo", parameters);
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

        public async Task<object> ListarJogosAsync(DateTime? data, int? estado, string? competicao)
        {
            var parameters = new List<IDataParameter>
            {
                new SqlParameter("@Data", (object?)data ?? DBNull.Value),
                new SqlParameter("@Estado", (object?)estado ?? DBNull.Value),
                new SqlParameter("@Competicao", (object?)competicao ?? DBNull.Value)
            };

            return await _db.QueryAsync("SP_Listar_Jogos", parameters);
        }

        public async Task<object?> ObterJogoAsync(string codigoJogo)
        {
            var parameters = new List<IDataParameter>
            {
                new SqlParameter("@Codigo_Jogo", codigoJogo)
            };

            var result = await _db.QueryAsync("SP_Obter_Jogo", parameters);

            if (result.Rows.Count == 0)
                return null;

            return result;
        }

        public async Task RemoverJogoAsync(string codigoJogo)
        {
            var parameters = new List<IDataParameter>
            {
                new SqlParameter("@Codigo_Jogo", codigoJogo)
            };

            await _db.ExecuteAsync("SP_Remover_Jogo", parameters);
        }
    }
}