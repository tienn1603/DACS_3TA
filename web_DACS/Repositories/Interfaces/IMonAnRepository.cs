using web_DACS.Models;

namespace web_DACS.Repositories.Interfaces
{
    public interface IMonAnRepository
    {
        Task<IEnumerable<MonAn>> GetAllAsync(string? searchString);
        Task<MonAn?> GetByIdAsync(int id);
        Task AddAsync(MonAn monAn);
        Task AddRangeAsync(IEnumerable<MonAn> monAns);
        Task UpdateAsync(MonAn monAn);
        Task DeleteAsync(int id);
        Task<bool> AnyAsync();
        Task SaveChangesAsync();
    }
}