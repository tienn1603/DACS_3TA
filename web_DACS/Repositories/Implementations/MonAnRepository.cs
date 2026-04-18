using Microsoft.EntityFrameworkCore;
using web_DACS.Data;
using web_DACS.Models;
using web_DACS.Repositories.Interfaces;

namespace web_DACS.Repositories.Implementations
{
    public class MonAnRepository : GenericRepository<MonAn>, IMonAnRepository
    {
        public MonAnRepository(ApplicationDbContext context)
            : base(context)
        {
        }

        public override async Task<IEnumerable<MonAn>> GetAllAsync()
        {
            return await _context.MonAns.ToListAsync();
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

        public async Task<bool> AnyAsync()
        {
            return await _context.MonAns.AnyAsync();
        }

        public async Task AddRangeAsync(IEnumerable<MonAn> monAns)
        {
            await _context.MonAns.AddRangeAsync(monAns);
        }
    }
}