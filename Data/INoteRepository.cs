using NotebookApi.Models;

namespace NotebookApi.Data;

public interface INoteRepository
{
    Task<IEnumerable<Note>> GetByUserIdAsync(int userId);
    Task<Note?> GetByIdAsync(string id, int userId);
    Task<string> CreateAsync(Note note);
    Task<bool> UpdateAsync(Note note);
    Task<bool> DeleteAsync(string id, int userId);
    Task<bool> ExistsAsync(string id, int userId);
}
