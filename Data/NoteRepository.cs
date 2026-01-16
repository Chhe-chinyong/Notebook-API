using Dapper;
using NotebookApi.Models;

namespace NotebookApi.Data;

public class NoteRepository : INoteRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public NoteRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<Note>> GetByUserIdAsync(int userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT Id, Title, Content, UserId, CreatedAt, UpdatedAt
            FROM t_note
            WHERE UserId = @UserId
            ORDER BY UpdatedAt DESC";

        return await connection.QueryAsync<Note>(sql, new { UserId = userId });
    }

    public async Task<Note?> GetByIdAsync(string id, int userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT Id, Title, Content, UserId, CreatedAt, UpdatedAt
            FROM t_note
            WHERE Id = @Id AND UserId = @UserId";

        return await connection.QueryFirstOrDefaultAsync<Note>(sql, new { Id = id, UserId = userId });
    }

    public async Task<string> CreateAsync(Note note)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO t_note (Id, Title, Content, UserId, CreatedAt, UpdatedAt)
            VALUES (@Id, @Title, @Content, @UserId, @CreatedAt, @UpdatedAt)";

        await connection.ExecuteAsync(sql, note);
        return note.Id;
    }

    public async Task<bool> UpdateAsync(Note note)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE t_note
            SET Title = @Title, Content = @Content, UpdatedAt = @UpdatedAt
            WHERE Id = @Id AND UserId = @UserId";

        var rowsAffected = await connection.ExecuteAsync(sql, note);
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(string id, int userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            DELETE FROM t_note
            WHERE Id = @Id AND UserId = @UserId";

        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id, UserId = userId });
        return rowsAffected > 0;
    }

    public async Task<bool> ExistsAsync(string id, int userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT COUNT(1)
            FROM t_note
            WHERE Id = @Id AND UserId = @UserId";

        var count = await connection.QuerySingleAsync<int>(sql, new { Id = id, UserId = userId });
        return count > 0;
    }
}
