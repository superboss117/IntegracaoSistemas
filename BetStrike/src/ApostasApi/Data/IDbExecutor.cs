using System.Data;

namespace ApostasApi.Data;

public interface IDbExecutor
{
    Task<int> ExecuteAsync(string storedProcedure, IEnumerable<IDataParameter> parameters);
    Task<DataTable> QueryAsync(string storedProcedure, IEnumerable<IDataParameter> parameters);
}