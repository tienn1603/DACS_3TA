using Microsoft.EntityFrameworkCore;
using web_DACS.Data;
using web_DACS.Models;
using web_DACS.Repositories.Interfaces;

namespace web_DACS.Repositories.Implementations
{
    public class BanAnRepository : IBanAnRepository
    {
        private readonly ApplicationDbContext _context;

        public BanAnRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<BanAn>> GetAllAsync()
        {
            return await _context.BanAns.ToListAsync();
        }

        public async Task<BanAn?> GetByIdAsync(int id)
        {
            return await _context.BanAns.FindAsync(id);
        }

        public async Task AddAsync(BanAn banAn)
        {
            await _context.BanAns.AddAsync(banAn);
        }

        public async Task UpdateAsync(BanAn banAn)
        {
            _context.Entry(banAn).State = EntityState.Modified;
        }

        public async Task DeleteAsync(int id)
        {
            var banAn = await _context.BanAns.FindAsync(id);
            if (banAn != null) _context.BanAns.Remove(banAn);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}