using Microsoft.EntityFrameworkCore;
using web_DACS.Models;
using web_DACS.Repositories.Interfaces;

namespace web_DACS.Repositories.Implementations
{
    public class BanAnRepository : IBanAnRepository
    {
        private readonly ApplicationDbContext _context;
        public BanAnRepository(ApplicationDbContext context) => _context = context;

        public async Task<IEnumerable<BanAn>> GetAllAsync() => await _context.BanAns.ToListAsync();

        public async Task<BanAn> GetByIdAsync(int id) => await _context.BanAns.FindAsync(id);

        public async Task UpdateStatusAsync(int id, string status)
        {
            var ban = await _context.BanAns.FindAsync(id);
            if (ban != null)
            {
                ban.TrangThai = status;
                await _context.SaveChangesAsync();
            }
        }
    }
}