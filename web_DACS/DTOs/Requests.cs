namespace web_DACS.DTOs
{
    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class BookingRequest
    {
        public int BanAnId { get; set; }
        public string TenKhachHang { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = string.Empty;
        public DateTime GioDenDuyKien { get; set; }
        public List<CartItemDTO>? CartItems { get; set; }
    }

    public class CartItemDTO
    {
        public int Id { get; set; }
        public int Qty { get; set; }
    }

    public class OrderRequest
    {
        public int BanId { get; set; }
        public int MonId { get; set; }
        public int SoLuong { get; set; }
    }

    public class CreateMonAnRequest
    {
        private const long MaxImageSizeInBytes = 5 * 1024 * 1024; // 5MB
        private static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".webp"];

        public string TenMon { get; set; } = string.Empty;
        public string? MoTa { get; set; }
        public decimal Gia { get; set; }
        public string? Loai { get; set; }
        public IFormFile? HinhAnhFile { get; set; }

        public bool IsImageFileValid(out string? errorMessage)
        {
            errorMessage = null;
            if (HinhAnhFile == null || HinhAnhFile.Length == 0) return true;
            var extension = Path.GetExtension(HinhAnhFile.FileName).ToLowerInvariant();
            if (!AllowedImageExtensions.Contains(extension))
            { errorMessage = "Định dạng ảnh không hợp lệ. Chỉ hỗ trợ: .jpg, .jpeg, .png, .webp"; return false; }
            if (HinhAnhFile.Length > MaxImageSizeInBytes)
            { errorMessage = "Kích thước ảnh vượt quá 5MB."; return false; }
            return true;
        }
    }
}
