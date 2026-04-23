namespace web_DACS.Services.Interfaces
{
    public interface IBanAnService
    {
        Task<ApiResponse> GetAllAsync();
        Task<ApiResponse> GetTableMapAsync(string? userId, bool isAdmin);
        Task<ApiResponse> CreateAsync(string soBan, int soChoNgoi);
        Task<ApiResponse> UpdateAsync(int id, string soBan, int soChoNgoi, int? trangThai = null);
        Task<ApiResponse> DeleteAsync(int id);
        Task<ApiResponse> ToggleStatusAsync(int id);
    }
}
