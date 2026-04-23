using web_DACS.Models;
using web_DACS.Repositories.Interfaces;
using web_DACS.Services.Interfaces;

namespace web_DACS.Services.Implementations
{
    public class DatBanService : IDatBanService
    {
        private readonly IDatBanRepository _datBanRepo;
        private readonly IMonAnRepository _monAnRepo;

        public DatBanService(IDatBanRepository datBanRepo, IMonAnRepository monAnRepo)
        {
            _datBanRepo = datBanRepo;
            _monAnRepo = monAnRepo;
        }

        public async Task<ApiResponse> GetAllAsync()
        {
            var items = await _datBanRepo.GetAllAsync();
            return ApiResponse.Ok("Lấy danh sách đặt bàn thành công.", items);
        }

        public async Task<ApiResponse> GetMyHistoryAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return ApiResponse.Fail("UserId không hợp lệ.");
            var items = await _datBanRepo.GetByUserIdAsync(userId);
            return ApiResponse.Ok("Lấy lịch sử thành công.", items);
        }

        public async Task<ApiResponse> CreateAsync(
            string tenKhachHang,
            string soDienThoai,
            DateTime gioDenDuyKien,
            int banAnId,
            string userId,
            bool isBlocked,
            List<(int MonAnId, int SoLuong)> cartItems)
        {
            if (isBlocked)
                return ApiResponse.Fail("Tài khoản bị chặn.");

            if (await _datBanRepo.GetActiveBookingCountByUserAsync(userId) >= 2)
                return ApiResponse.Fail("Vượt quá giới hạn đặt bàn.");

            var datBan = new DatBan
            {
                TenKhachHang = tenKhachHang,
                SoDienThoai = soDienThoai,
                GioDenDuyKien = gioDenDuyKien,
                BanAnId = banAnId,
                UserId = userId,
                NgayDat = DateTime.Now,
                TrangThai = 0
            };

            await _datBanRepo.CreateWithDetailsAsync(datBan, cartItems);
            return ApiResponse.Ok("Thành công!", new { bookingId = datBan.Id });
        }

        public async Task<ApiResponse> ConfirmPaymentAsync(int datBanId)
        {
            var success = await _datBanRepo.ConfirmPaymentAsync(datBanId);
            if (!success) return ApiResponse.Fail("Không tìm thấy đơn đặt bàn.");
            return ApiResponse.Ok("Đã thanh toán và giải phóng bàn!");
        }

        public async Task<ApiResponse> CancelPendingBookingAsync(int datBanId, string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return ApiResponse.Fail("UserId không hợp lệ.");

            var success = await _datBanRepo.CancelPendingBookingAsync(datBanId, userId);
            if (!success)
                return ApiResponse.Fail("Không thể hủy đơn. Chỉ đơn Pending của chính bạn mới được hủy.");
            return ApiResponse.Ok("Hủy đơn thành công, bàn đã được cập nhật Available.");
        }

        public async Task<ApiResponse> GetActiveBookingsAsync()
        {
            var items = await _datBanRepo.GetActiveBookingsAsync();
            return ApiResponse.Ok("Lấy danh sách đơn active thành công.", items);
        }

        public async Task<ApiResponse> ProcessExpiredPendingBookingsAsync()
        {
            var now = DateTime.Now;
            var expiredBookings = await _datBanRepo.GetExpiredPendingBookingsAsync(now);

            foreach (var booking in expiredBookings)
            {
                booking.TrangThai = 3;
                if (booking.BanAn != null) booking.BanAn.TrangThai = 0;
            }

            if (expiredBookings.Count > 0)
                await _datBanRepo.SaveChangesAsync();

            return ApiResponse.Ok($"Đã xử lý {expiredBookings.Count} đơn quá hạn.");
        }

        public async Task<ApiResponse> ConfirmBookingAsync(int datBanId)
        {
            var success = await _datBanRepo.ConfirmBookingAsync(datBanId);
            if (!success) return ApiResponse.Fail("Không tìm thấy đơn chờ xác nhận.");
            return ApiResponse.Ok("Đã xác nhận đặt bàn, bàn được chuyển sang trạng thái sử dụng.");
        }
    }
}
