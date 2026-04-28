using web_DACS.Models;
using web_DACS.Repositories.Interfaces;
using web_DACS.Services.Interfaces;

namespace web_DACS.Services.Implementations
{
    public class BanAnService : IBanAnService
    {
        private readonly IBanAnRepository _banAnRepo;
        private readonly IDatBanRepository _datBanRepo;

        public BanAnService(IBanAnRepository banAnRepo, IDatBanRepository datBanRepo)
        {
            _banAnRepo = banAnRepo;
            _datBanRepo = datBanRepo;
        }

        public async Task<ApiResponse> GetAllAsync()
        {
            // Sử dụng hàm repository mới mà chúng ta đã thống nhất trước đó
            var items = await _banAnRepo.GetAllWithActiveBookingAsync();
            return ApiResponse.Ok("Lấy danh sách bàn ăn thành công.", items);
        }
        public async Task<ApiResponse> GetTableMapAsync(string? userId, bool isAdmin)
        {
            var now = DateTime.Now;
            var expiredBookings = await _datBanRepo.GetExpiredPendingBookingsAsync(now);

            foreach (var booking in expiredBookings)
            {
                booking.TrangThai = 3;
                if (booking.BanAn != null) booking.BanAn.TrangThai = 0;
            }
            if (expiredBookings.Count > 0) await _datBanRepo.SaveChangesAsync();

            var allActiveBookings = await _datBanRepo.GetActiveBookingsAsync();
            var listBanAn = await _banAnRepo.GetAllAsync();

            var result = listBanAn.Select(ban =>
            {
                // Tìm đơn đặt bàn tương ứng với bàn này
                var active = allActiveBookings.FirstOrDefault(d =>
                    d.BanAnId == ban.Id ||
                    (d.GhiChuGopBan != null && d.GhiChuGopBan.Split(',').Contains(ban.Id.ToString()))
                );

                string timeRange = "";
                if (active != null)
                {
                    timeRange = $"{active.GioDenDuyKien:HH:mm} - {active.GioDenDuyKien.AddMinutes(1):HH:mm}";
                }

                return new
                {
                    id = ban.Id,
                    soBan = ban.SoBan,
                    soChoNgoi = ban.SoChoNgoi,
                    trangThai = ban.TrangThai,
                    isOwner = active != null && active.UserId == userId,
                    bookingId = active?.Id,
                    timeRange = timeRange, 
                    tenKhach = active?.TenKhachHang 
                };
            });

            return ApiResponse.Ok("Lấy sơ đồ bàn thành công.", result);
        }

        public async Task<ApiResponse> CreateAsync(string soBan, int soChoNgoi)
        {
            if (string.IsNullOrWhiteSpace(soBan))
                return ApiResponse.Fail("Số bàn không được để trống.");

            var item = new BanAn { SoBan = soBan, SoChoNgoi = soChoNgoi, TrangThai = 0 };
            await _banAnRepo.AddAsync(item);
            await _banAnRepo.SaveChangesAsync();
            return ApiResponse.Ok("Thêm bàn thành công.", item);
        }

        public async Task<ApiResponse> UpdateAsync(int id, string soBan, int soChoNgoi, int? trangThai = null)
        {
            if (string.IsNullOrWhiteSpace(soBan))
                return ApiResponse.Fail("Số bàn không được để trống.");

            var item = await _banAnRepo.GetByIdAsync(id);
            if (item == null)
                return ApiResponse.Fail("Không tìm thấy bàn cần cập nhật.");

            item.SoBan = soBan;
            item.SoChoNgoi = soChoNgoi;
            if (trangThai.HasValue) item.TrangThai = trangThai.Value;
            _banAnRepo.Update(item);
            await _banAnRepo.SaveChangesAsync();
            return ApiResponse.Ok("Cập nhật bàn thành công.", item);
        }

        public async Task<ApiResponse> DeleteAsync(int id)
        {
            var item = await _banAnRepo.GetByIdAsync(id);
            if (item == null)
                return ApiResponse.Fail("Không tìm thấy bàn cần xóa.");

            await _banAnRepo.DeleteAsync(id);
            await _banAnRepo.SaveChangesAsync();
            return ApiResponse.Ok("Xóa bàn thành công.");
        }

        public async Task<ApiResponse> ToggleStatusAsync(int id)
        {
            var item = await _banAnRepo.GetByIdAsync(id);
            if (item == null)
                return ApiResponse.Fail("Không tìm thấy bàn.");

            item.TrangThai = item.TrangThai == 0 ? 1 : 0;
            _banAnRepo.Update(item);
            await _banAnRepo.SaveChangesAsync();
            return ApiResponse.Ok("Cập nhật trạng thái thành công.", new { currentStatus = item.TrangThai });
        }
    }
}
