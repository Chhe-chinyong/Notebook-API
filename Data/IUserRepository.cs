using NotebookApi.Models;

namespace NotebookApi.Data;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(int id);
    Task<int> CreateAsync(User user);
    Task<bool> ExistsByEmailAsync(string email);
}
