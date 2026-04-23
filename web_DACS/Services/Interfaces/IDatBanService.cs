namespace web_DACS.Services.Interfaces
{
    public interface IDatBanService
    {
        Task<ApiResponse> GetAllAsync();
        Task<ApiResponse> GetMyHistoryAsync(string userId);
        Task<ApiResponse> CreateAsync(
            string tenKhachHang,
            string soDienThoai,
            DateTime gioDenDuyKien,
            int banAnId,
            string userId,
            bool isBlocked,
            List<(int MonAnId, int SoLuong)> cartItems);

        Task<ApiResponse> ConfirmPaymentAsync(int datBanId);
        Task<ApiResponse> CancelPendingBookingAsync(int datBanId, string userId);
        Task<ApiResponse> GetActiveBookingsAsync();
        Task<ApiResponse> ProcessExpiredPendingBookingsAsync();
        Task<ApiResponse> ConfirmBookingAsync(int datBanId);
    }
}
