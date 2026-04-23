namespace web_DACS.Services.Interfaces
{
    public interface IMonAnService
    {
        Task<ApiResponse> GetAllAsync(string? searchString = null);
        Task<ApiResponse> GetByIdAsync(int id);
        Task<ApiResponse> CreateAsync(string tenMon, string? moTa, decimal gia, string? loai, string? hinhAnh);
        Task<ApiResponse> UpdateAsync(int id, string tenMon, string? moTa, decimal gia, string? loai, string? hinhAnh);
        Task<ApiResponse> DeleteAsync(int id);
        Task<ApiResponse> SeedDataIfEmptyAsync();
    }
}
