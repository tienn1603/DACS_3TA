using Microsoft.EntityFrameworkCore;
using web_DACS.Data;
using web_DACS.Models;
using web_DACS.Repositories.Interfaces;

namespace web_DACS.Repositories.Implementations
{
    public class DatBanRepository : IDatBanRepository
    {
        private readonly ApplicationDbContext _context;

        public DatBanRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<DatBan>> GetAllAsync()
        {
            return await _context.DatBans
                .Include(d => d.BanAn)
                .OrderByDescending(d => d.NgayDat)
                .ToListAsync();
        }

        public async Task<DatBan?> GetByIdAsync(int id)
        {
            return await _context.DatBans
                .Include(d => d.BanAn)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<int> GetActiveBookingCountByUserAsync(string userId)
        {
            // Đếm các đơn không phải là trạng thái Hủy (-1) hoặc Đã xong (Tùy logic của bạn)
            return await _context.DatBans.CountAsync(d => d.UserId == userId && d.TrangThai != -1);
        }

        public async Task AddAsync(DatBan datBan)
        {
            await _context.DatBans.AddAsync(datBan);
        }

        public async Task UpdateAsync(DatBan datBan)
        {
            _context.Entry(datBan).State = EntityState.Modified;
        }

        public async Task DeleteAsync(int id)
        {
            var datBan = await _context.DatBans.FindAsync(id);
            if (datBan != null) _context.DatBans.Remove(datBan);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}