using web_DACS.Models;

namespace web_DACS.Repositories.Interfaces
{
    public interface IDatBanRepository : IGenericRepository<DatBan>
    {
        Task<int> GetActiveBookingCountByUserAsync(string userId);
        Task<List<DatBan>> GetActiveBookingsAsync();
        Task<List<DatBan>> GetExpiredPendingBookingsAsync(DateTime now);
        Task<DatBan?> GetActiveBookingForTableAsync(int banAnId, string? userId, bool isAdmin);
        Task<DatBan> CreateWithDetailsAsync(DatBan datBan, IEnumerable<(int MonAnId, int SoLuong)> cartItems);
        Task<bool> ConfirmPaymentAsync(int datBanId);
        Task<List<DatBan>> GetByUserIdAsync(string userId);
        Task<bool> CancelPendingBookingAsync(int datBanId, string userId);
        Task<bool> ConfirmBookingAsync(int datBanId);
    }
}