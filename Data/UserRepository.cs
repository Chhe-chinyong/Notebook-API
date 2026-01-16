using Dapper;
using NotebookApi.Models;

namespace NotebookApi.Data;

public class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UserRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT Id, Email, Name, PasswordHash, CreatedAt
            FROM t_user
            WHERE Email = @Email";

        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Email = email });
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT Id, Email, Name, PasswordHash, CreatedAt
            FROM t_user
            WHERE Id = @Id";

        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Id = id });
    }

    public async Task<int> CreateAsync(User user)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO t_user (Email, Name, PasswordHash, CreatedAt)
            VALUES (@Email, @Name, @PasswordHash, @CreatedAt);
            SELECT CAST(SCOPE_IDENTITY() as int);";

        var userId = await connection.QuerySingleAsync<int>(sql, user);
        return userId;
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT COUNT(1)
            FROM t_user
            WHERE Email = @Email";

        var count = await connection.QuerySingleAsync<int>(sql, new { Email = email });
        return count > 0;
    }
}
