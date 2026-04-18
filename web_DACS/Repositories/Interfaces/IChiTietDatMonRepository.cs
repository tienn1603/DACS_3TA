using web_DACS.Models;

namespace web_DACS.Repositories.Interfaces
{
    public interface IChiTietDatMonRepository : IGenericRepository<ChiTietDatMon>
    {
        Task<ChiTietDatMon?> GetByBookingAndMonAnAsync(int datBanId, int monAnId);
        Task<List<object>> GetTableDetailsAsync(int datBanId);
        Task DeleteByDatBanIdAsync(int datBanId);
    }
}
