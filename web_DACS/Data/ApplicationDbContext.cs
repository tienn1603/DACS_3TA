using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using web_DACS.Models;

namespace web_DACS.Data 
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<BanAn> BanAns { get; set; }
        public DbSet<MonAn> MonAns { get; set; }
        public DbSet<DatBan> DatBans { get; set; }
        public DbSet<ChiTietDatMon> ChiTietDatMons { get; set; }
        public DbSet<DanhGia> DanhGias { get; set; }
    }
}