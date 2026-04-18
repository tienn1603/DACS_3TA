using Microsoft.EntityFrameworkCore;
using web_DACS.Data;
using web_DACS.Models;
using web_DACS.Repositories.Interfaces;

namespace web_DACS.Repositories.Implementations
{
    public class ChiTietDatMonRepository : GenericRepository<ChiTietDatMon>, IChiTietDatMonRepository
    {
        public ChiTietDatMonRepository(ApplicationDbContext context)
            : base(context)
        {
        }

        public async Task<ChiTietDatMon?> GetByBookingAndMonAnAsync(int datBanId, int monAnId)
        {
            return await _context.ChiTietDatMons
                .FirstOrDefaultAsync(o => o.DatBanId == datBanId && o.MonAnId == monAnId);
        }

        public async Task<List<object>> GetTableDetailsAsync(int datBanId)
        {
            return await _context.ChiTietDatMons
                .Where(ct => ct.DatBanId == datBanId)
                .Select(ct => new
                {
                    tenMon = ct.MonAn!.TenMon,
                    soLuong = ct.SoLuong,
                    gia = ct.MonAn.Gia,
                    thanhTien = ct.SoLuong * ct.MonAn.Gia
                })
                .Cast<object>()
                .ToListAsync();
        }

        public async Task DeleteByDatBanIdAsync(int datBanId)
        {
            var records = await _context.ChiTietDatMons
                .Where(c => c.DatBanId == datBanId)
                .ToListAsync();

            if (records.Count > 0)
            {
                _context.ChiTietDatMons.RemoveRange(records);
            }
        }
    }
}
