using web_DACS.Models;

namespace web_DACS.Repositories.Interfaces
{
    public interface IMonAnRepository : IGenericRepository<MonAn>
    {
        Task<IEnumerable<MonAn>> GetAllAsync(string? searchString);
        Task AddRangeAsync(IEnumerable<MonAn> monAns);
        Task<bool> AnyAsync();
    }
}