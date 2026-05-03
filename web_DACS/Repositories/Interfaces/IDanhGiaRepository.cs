using web_DACS.Models;

namespace web_DACS.Repositories.Interfaces;

public interface IDanhGiaRepository : IGenericRepository<DanhGia>
{
    Task<DanhGia?> GetByDatBanIdAsync(int datBanId);
    Task<bool> ExistsForDatBanAsync(int datBanId);
}
