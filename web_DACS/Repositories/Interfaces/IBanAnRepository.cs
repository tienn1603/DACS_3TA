using web_DACS.Models;

namespace web_DACS.Repositories.Interfaces
{
    public interface IBanAnRepository
    {
        Task<IEnumerable<BanAn>> GetAllAsync();
        Task<BanAn?> GetByIdAsync(int id);
        Task UpdateAsync(BanAn banAn);
        Task AddAsync(BanAn banAn);
        Task DeleteAsync(int id);
        Task SaveChangesAsync();
    }
}