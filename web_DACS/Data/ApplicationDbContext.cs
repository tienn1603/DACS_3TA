using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace web_DACS.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<BanAn> BanAns { get; set; }
        public DbSet<DatBan> DatBans { get; set; }
    }
}