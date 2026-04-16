using web_DACS.Models;

namespace web_DACS.Repositories.Interfaces
{
    public interface IDatBanRepository
    {
        Task<IEnumerable<DatBan>> GetAllAsync();
        Task<DatBan?> GetByIdAsync(int id);
        Task<int> GetActiveBookingCountByUserAsync(string userId);
        Task AddAsync(DatBan datBan);
        Task UpdateAsync(DatBan datBan);
        Task DeleteAsync(int id);
        Task SaveChangesAsync();
    }
}