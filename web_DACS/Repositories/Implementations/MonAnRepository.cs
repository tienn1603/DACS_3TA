using Microsoft.EntityFrameworkCore;
using web_DACS.Data;
using web_DACS.Models;
using web_DACS.Repositories.Interfaces;

namespace web_DACS.Repositories.Implementations
{
    public class MonAnRepository : IMonAnRepository
    {
        private readonly ApplicationDbContext _context;

        public MonAnRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<MonAn>> GetAllAsync(string? searchString)
        {
            var query = _context.MonAns.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(s => s.TenMon!.Contains(searchString));
            }
            return await query.ToListAsync();
        }

        public async Task<MonAn?> GetByIdAsync(int id)
        {
            return await _context.MonAns.FindAsync(id);
        }

        public async Task<bool> AnyAsync()
        {
            return await _context.MonAns.AnyAsync();
        }

        public async Task AddAsync(MonAn monAn)
        {
            await _context.MonAns.AddAsync(monAn);
        }

        public async Task AddRangeAsync(IEnumerable<MonAn> monAns)
        {
            await _context.MonAns.AddRangeAsync(monAns);
        }

        public async Task UpdateAsync(MonAn monAn)
        {
            _context.Entry(monAn).State = EntityState.Modified;
        }

        public async Task DeleteAsync(int id)
        {
            var monAn = await _context.MonAns.FindAsync(id);
            if (monAn != null) _context.MonAns.Remove(monAn);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}