using Microsoft.EntityFrameworkCore;
using web_DACS.Data;
using web_DACS.Models;
using web_DACS.Repositories.Interfaces;

namespace web_DACS.Repositories.Implementations;

public class DanhGiaRepository : GenericRepository<DanhGia>, IDanhGiaRepository
{
    public DanhGiaRepository(ApplicationDbContext context)
        : base(context)
    {
    }

    public async Task<DanhGia?> GetByDatBanIdAsync(int datBanId)
    {
        return await _context.DanhGias
            .FirstOrDefaultAsync(d => d.DatBanId == datBanId);
    }

    public async Task<bool> ExistsForDatBanAsync(int datBanId)
    {
        return await _context.DanhGias
            .AnyAsync(d => d.DatBanId == datBanId);
    }
}
