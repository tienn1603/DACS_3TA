using web_DACS.Models;
using web_DACS.Repositories.Interfaces;
using web_DACS.Services.Interfaces;

namespace web_DACS.Services.Implementations
{
    public class DatBanService : IDatBanService
    {
        private readonly IDatBanRepository _datBanRepo;
        private readonly IMonAnRepository _monAnRepo;
        private readonly IBanAnRepository _banAnRepo;

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
                return ApiResponse.Fail("Tài khoản của bạn hiện đang bị chặn.");

            // Kiểm tra giới hạn đặt đơn
            if (await _datBanRepo.GetActiveBookingCountByUserAsync(userId) >= 2)
                return ApiResponse.Fail("Bạn đã có 2 đơn đặt bàn đang chờ, không thể đặt thêm.");

            var datBan = new DatBan
            {
                TenKhachHang = tenKhachHang,
                SoDienThoai = soDienThoai,
                GioDenDuyKien = gioDenDuyKien,
                BanAnId = banAnId,
                UserId = userId,
                NgayDat = DateTime.Now,
                TrangThai = 0 // Đang chờ (Pending)
            };

            try
            {
                // Gọi Repo để xử lý Transaction
                await _datBanRepo.CreateWithDetailsAsync(datBan, cartItems);
                return ApiResponse.Ok("Đặt bàn thành công! Bàn của bạn đã được giữ chỗ.", new { bookingId = datBan.Id });
            }
            catch (InvalidOperationException ex)
            {
                // Bắt lỗi logic "Bàn đã bị đặt" từ Repository
                return ApiResponse.Fail(ex.Message);
            }
            catch (Exception)
            {
                // Bắt các lỗi hệ thống khác
                return ApiResponse.Fail("Đã xảy ra lỗi trong quá trình xử lý đơn đặt bàn.");
            }
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

            int count = 0;
            foreach (var booking in expiredBookings)
            {
                var success = await _datBanRepo.CancelPendingBookingAsync(booking.Id, booking.UserId);
                if (success)
                {
                    count++;
                }
            }

            return ApiResponse.Ok($"Đã xử lý giải phóng {count} đơn quá hạn.");
        }

        public async Task<ApiResponse> ConfirmBookingAsync(int datBanId)
        {
            var success = await _datBanRepo.ConfirmBookingAsync(datBanId);
            if (!success) return ApiResponse.Fail("Không tìm thấy đơn chờ xác nhận.");
            return ApiResponse.Ok("Đã xác nhận đặt bàn, bàn được chuyển sang trạng thái sử dụng.");
        }

    }
}
