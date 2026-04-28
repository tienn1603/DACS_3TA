using Microsoft.EntityFrameworkCore;
using web_DACS.Data;
using web_DACS.Models;
using web_DACS.Repositories.Interfaces;

namespace web_DACS.Repositories.Implementations
{
    public class BanAnRepository : GenericRepository<BanAn>, IBanAnRepository
    {
        public BanAnRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<BanAn>> GetAllWithActiveBookingAsync()
        {
            var banAns = await _context.BanAns.ToListAsync();
            foreach (var ban in banAns)
            {
                if (ban.TrangThai == 1) 
                {
                    ban.ActiveDatBan = await _context.DatBans
                        .Where(d => d.BanAnId == ban.Id && (d.TrangThai == 0 || d.TrangThai == 1))
                        .OrderByDescending(d => d.NgayDat)
                        .FirstOrDefaultAsync();
                }
            }
            return banAns;
        }
    }
}