using web_DACS.Data;
using web_DACS.Models;
using web_DACS.Repositories.Interfaces;

namespace web_DACS.Repositories.Implementations
{
    public class BanAnRepository : GenericRepository<BanAn>, IBanAnRepository
    {
        public BanAnRepository(ApplicationDbContext context)
            : base(context)
        {
        }
    }
}