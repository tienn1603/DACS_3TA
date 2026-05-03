using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using web_DACS.Data;
using web_DACS.Models;
using web_DACS.Repositories.Interfaces;

namespace web_DACS.Repositories.Implementations
{
    public class DatBanRepository : GenericRepository<DatBan>, IDatBanRepository
    {
        public DatBanRepository(ApplicationDbContext context)
            : base(context)
        {
        }

        public override async Task<IEnumerable<DatBan>> GetAllAsync()
        {
            return await _context.DatBans
                .Include(d => d.BanAn)
                .Include(d => d.ChiTietDatMons).ThenInclude(ct => ct.MonAn)
                .Include(d => d.DanhGias)
                .OrderByDescending(d => d.NgayDat)
                .ToListAsync();
        }

        public override async Task<DatBan?> GetByIdAsync(int id)
        {
            return await _context.DatBans
                .Include(d => d.BanAn)
                .Include(d => d.ChiTietDatMons).ThenInclude(ct => ct.MonAn)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<int> GetActiveBookingCountByUserAsync(string userId)
        {
            // Chỉ đếm các đơn đang hoạt động (Pending/Confirmed)
            return await _context.DatBans.CountAsync(d =>
                d.UserId == userId && (d.TrangThai == 0 || d.TrangThai == 1));
        }

        public async Task<List<DatBan>> GetActiveBookingsAsync()
        {
            return await _context.DatBans
                .Where(d => d.TrangThai == 0 || d.TrangThai == 1)
                .ToListAsync();
        }

        public async Task<List<DatBan>> GetExpiredPendingBookingsAsync(DateTime now)
        {
            return await _context.DatBans
                .Where(b => b.TrangThai == 0 
                       && b.GioDenDuyKien.AddMinutes(1) < now)
                .ToListAsync();
        }
        public async Task<DatBan?> GetActiveBookingForTableAsync(int banAnId, string? userId, bool isAdmin)
        {
            return await _context.DatBans
                .FirstOrDefaultAsync(d => d.BanAnId == banAnId
                    && (d.TrangThai == 0 || d.TrangThai == 1)
                    && (isAdmin || d.UserId == userId));
        }

        public async Task<DatBan> CreateWithDetailsAsync(DatBan datBan, IEnumerable<(int MonAnId, int SoLuong)> cartItems)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Kiểm tra bàn và khóa bàn ngay lập tức
                var ban = await _context.BanAns.FindAsync(datBan.BanAnId);
                if (ban == null || ban.TrangThai != 0)
                {
                    throw new Exception("Bàn đã có người khác đặt hoặc không tồn tại.");
                }

                // 2. Đổi trạng thái bàn thành 2 (Đã đặt)
                ban.TrangThai = 2;
                _context.BanAns.Update(ban);

                // 3. Lưu đơn đặt bàn
                _context.DatBans.Add(datBan);
                await _context.SaveChangesAsync();

                // 4. Lưu chi tiết món (Giữ nguyên code cũ của bạn)
                foreach (var item in cartItems)
                {
                    _context.ChiTietDatMons.Add(new ChiTietDatMon
                    {
                        DatBanId = datBan.Id,
                        MonAnId = item.MonAnId,
                        SoLuong = item.SoLuong,
                        BanAnId = datBan.BanAnId
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return datBan;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> ConfirmPaymentAsync(int datBanId)
        {
            await using IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var donDat = await _context.DatBans
                    .Include(d => d.BanAn)
                    .Include(d => d.ChiTietDatMons).ThenInclude(ct => ct.MonAn)
                    .Include(d => d.DanhGias)
                    .FirstOrDefaultAsync(d => d.Id == datBanId);

                if (donDat == null)
                {
                    return false;
                }

                // Chỉ cho phép thanh toán khi đơn đang ở trạng thái Đang dùng (1)
                if (donDat.TrangThai != 1)
                {
                    return false;
                }

                // Giải phóng bàn (gộp bàn hoặc bàn đơn)
                if (!string.IsNullOrEmpty(donDat.GhiChuGopBan))
                {
                    var ids = donDat.GhiChuGopBan.Split(',').Select(int.Parse);
                    var banAns = await _context.BanAns.Where(b => ids.Contains(b.Id)).ToListAsync();
                    foreach (var ban in banAns)
                    {
                        ban.TrangThai = 0;
                    }
                }
                else if (donDat.BanAn != null)
                {
                    donDat.BanAn.TrangThai = 0;
                }

                // KHÔNG xóa chi tiết món — giữ lại để hiển thị trong trang chi tiết đặt bàn

                // Đánh dấu hoàn thành (4)
                donDat.TrangThai = 4;
                _context.DatBans.Update(donDat);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<DatBan>> GetByUserIdAsync(string userId)
        {
            return await _context.DatBans
                .Include(d => d.BanAn)
                .Include(d => d.ChiTietDatMons).ThenInclude(ct => ct.MonAn)
                .Include(d => d.DanhGias)
                .Where(d => d.UserId == userId)
                .OrderByDescending(d => d.NgayDat)
                .ToListAsync();
        }

        public async Task<bool> CancelPendingBookingAsync(int datBanId, string userId)
        {
            await using IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var donDat = await _context.DatBans
                    .Include(d => d.BanAn)
                    .FirstOrDefaultAsync(d => d.Id == datBanId && d.UserId == userId);

                if (donDat == null || donDat.TrangThai != 0)
                {
                    return false;
                }

                donDat.TrangThai = -1; // Hủy
                if (donDat.BanAn != null)
                {
                    donDat.BanAn.TrangThai = 0; // Available
                }

                var chiTietMons = await _context.ChiTietDatMons
                    .Where(c => c.DatBanId == datBanId)
                    .ToListAsync();
                if (chiTietMons.Count > 0)
                {
                    _context.ChiTietDatMons.RemoveRange(chiTietMons);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> ConfirmBookingAsync(int datBanId)
        {
            var donDat = await _context.DatBans
                .Include(d => d.BanAn)
                .FirstOrDefaultAsync(d => d.Id == datBanId && d.TrangThai == 0);

            if (donDat == null) return false;

            donDat.TrangThai = 1;
            if (donDat.BanAn != null) donDat.BanAn.TrangThai = 2;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CancelPendingBookingByAdminAsync(int datBanId)
        {
            await using IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var donDat = await _context.DatBans
                    .Include(d => d.BanAn)
                    .FirstOrDefaultAsync(d => d.Id == datBanId);

                if (donDat == null || donDat.TrangThai == 4 || donDat.TrangThai == -1)
                {
                    return false;
                }

                donDat.TrangThai = -1;
                if (donDat.BanAn != null)
                {
                    donDat.BanAn.TrangThai = 0;
                }

                var chiTietMons = await _context.ChiTietDatMons
                    .Where(c => c.DatBanId == datBanId)
                    .ToListAsync();
                if (chiTietMons.Count > 0)
                {
                    _context.ChiTietDatMons.RemoveRange(chiTietMons);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}