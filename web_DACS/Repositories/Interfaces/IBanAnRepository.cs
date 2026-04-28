using web_DACS.Models;

namespace web_DACS.Repositories.Interfaces
{
    public interface IBanAnRepository : IGenericRepository<BanAn>
    {
        Task<IEnumerable<BanAn>> GetAllWithActiveBookingAsync();
    }
}