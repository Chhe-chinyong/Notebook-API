using System.Data;

namespace NotebookApi.Data;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
