namespace web_DACS.Services.Interfaces
{
    public interface IAccountService
    {
        Task<ApiResponse> RegisterAsync(string username, string email, string password, string fullName, string? phoneNumber, string? address);
        Task<ApiResponse> LoginAsync(string username, string password);
    }
}
