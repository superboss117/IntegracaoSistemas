using Microsoft.Data.SqlClient;
using System.Data;

namespace ApostasApi.Data;

public class SqlDbExecutor : IDbExecutor
{
    private readonly string _connectionString;

    public SqlDbExecutor(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("ApostasDb")
            ?? throw new InvalidOperationException("Connection string 'ApostasDb' não encontrada.");
    }

    public async Task<int> ExecuteAsync(string storedProcedure, IEnumerable<IDataParameter> parameters)
    {
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(storedProcedure, connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        foreach (var parameter in parameters)
            command.Parameters.Add(parameter);

        await connection.OpenAsync();
        return await command.ExecuteNonQueryAsync();
    }

    public async Task<DataTable> QueryAsync(string storedProcedure, IEnumerable<IDataParameter> parameters)
    {
        var table = new DataTable();

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(storedProcedure, connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        foreach (var parameter in parameters)
            command.Parameters.Add(parameter);

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();
        table.Load(reader);
        return table;
    }
}