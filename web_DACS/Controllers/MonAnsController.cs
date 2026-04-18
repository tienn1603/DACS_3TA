using web_DACS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using web_DACS.Repositories.Interfaces;

namespace web_DACS.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,User")]
    public class MonAnController : ControllerBase
    {
        private readonly IMonAnRepository _monAnRepo;
        private readonly IDatBanRepository _datBanRepo;
        private readonly IChiTietDatMonRepository _chiTietDatMonRepo;
        private readonly IWebHostEnvironment _environment;

        public MonAnController(
            IMonAnRepository monAnRepo,
            IDatBanRepository datBanRepo,
            IChiTietDatMonRepository chiTietDatMonRepo,
            IWebHostEnvironment environment)
        {
            _monAnRepo = monAnRepo;
            _datBanRepo = datBanRepo;
            _chiTietDatMonRepo = chiTietDatMonRepo;
            _environment = environment;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetMonAns([FromQuery] string? searchString)
        {
            // Seed Data nếu trống
            if (!await _monAnRepo.AnyAsync())
            {
                var danhSachMon = new List<MonAn>
                {
                    new MonAn { TenMon = "Tôm Hùm Nướng Phô Mai", Gia = 450000, Loai = "Food", HinhAnh = "tomphomai.jpg", MoTa = "Tôm hùm tươi sống nướng cùng lớp phô mai Mozzarella tan chảy." },
                    new MonAn { TenMon = "Cua Rang Me", Gia = 350000, Loai = "Food", HinhAnh = "cua.jpg", MoTa = "Cua thịt chắc ngọt quyện cùng sốt me chua cay đậm đà." },
                    new MonAn { TenMon = "Coca Cola", Gia = 15000, Loai = "Drink", HinhAnh = "coca.jpg", MoTa = "Nước giải khát có ga." }
                };
                await _monAnRepo.AddRangeAsync(danhSachMon);
                await _monAnRepo.SaveChangesAsync();
            }

            var result = await _monAnRepo.GetAllAsync(searchString);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetMonAnById(int id)
        {
            var monAn = await _monAnRepo.GetByIdAsync(id);
            if (monAn == null) return NotFound(new { message = "Không tìm thấy món ăn." });
            return Ok(monAn);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateMonAn([FromForm] CreateMonAnRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.TenMon))
            {
                return BadRequest(new { message = "Tên món ăn không được để trống." });
            }

            if (request.Gia <= 0)
            {
                return BadRequest(new { message = "Giá món ăn phải lớn hơn 0." });
            }

            if (!request.IsImageFileValid(out var imageValidationMessage))
            {
                return BadRequest(new { message = imageValidationMessage });
            }

            string? imageFileName = null;
            if (request.HinhAnhFile != null && request.HinhAnhFile.Length > 0)
            {
                imageFileName = await SaveImageAsync(request.HinhAnhFile);
            }

            var monAn = new MonAn
            {
                TenMon = request.TenMon,
                MoTa = request.MoTa,
                Gia = request.Gia,
                Loai = request.Loai,
                HinhAnh = imageFileName
            };

            await _monAnRepo.AddAsync(monAn);
            await _monAnRepo.SaveChangesAsync();

            return Ok(new { message = "Thêm món ăn thành công.", data = monAn });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateMonAn(int id, [FromBody] MonAn monAn)
        {
            if (id != monAn.Id)
            {
                return BadRequest(new { message = "Id không hợp lệ." });
            }

            var existingMonAn = await _monAnRepo.GetByIdAsync(id);
            if (existingMonAn == null)
            {
                return NotFound(new { message = "Không tìm thấy món ăn cần cập nhật." });
            }

            if (string.IsNullOrWhiteSpace(monAn.TenMon))
            {
                return BadRequest(new { message = "Tên món ăn không được để trống." });
            }

            if (monAn.Gia <= 0)
            {
                return BadRequest(new { message = "Giá món ăn phải lớn hơn 0." });
            }

            existingMonAn.TenMon = monAn.TenMon;
            existingMonAn.MoTa = monAn.MoTa;
            existingMonAn.Gia = monAn.Gia;
            existingMonAn.HinhAnh = monAn.HinhAnh;
            existingMonAn.Loai = monAn.Loai;

            _monAnRepo.Update(existingMonAn);
            await _monAnRepo.SaveChangesAsync();

            return Ok(new { message = "Cập nhật món ăn thành công.", data = existingMonAn });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteMonAn(int id)
        {
            var monAn = await _monAnRepo.GetByIdAsync(id);
            if (monAn == null)
            {
                return NotFound(new { message = "Không tìm thấy món ăn cần xóa." });
            }

            await _monAnRepo.DeleteAsync(id);
            await _monAnRepo.SaveChangesAsync();

            return Ok(new { message = "Xóa món ăn thành công." });
        }

        [HttpPost("Order")]
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> Order([FromBody] OrderRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Tìm đơn đặt bàn đang hoạt động
            var activeBooking = await _datBanRepo.GetActiveBookingForTableAsync(
                request.BanId,
                userId,
                User.IsInRole("Admin"));

            if (activeBooking == null)
                return BadRequest(new { message = "Bàn này hiện chưa có khách hoặc bạn không có quyền." });

            var existingItem = await _chiTietDatMonRepo.GetByBookingAndMonAnAsync(activeBooking.Id, request.MonId);

            if (existingItem != null)
            {
                existingItem.SoLuong += (request.SoLuong < 1 ? 1 : request.SoLuong);
                _chiTietDatMonRepo.Update(existingItem);
            }
            else
            {
                await _chiTietDatMonRepo.AddAsync(new ChiTietDatMon
                {
                    BanAnId = request.BanId,
                    MonAnId = request.MonId,
                    SoLuong = request.SoLuong,
                    DatBanId = activeBooking.Id
                });
            }

            await _chiTietDatMonRepo.SaveChangesAsync();
            return Ok(new { message = "Chọn món thành công!" });
        }

        [HttpGet("GetTableDetails/{id}")]
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> GetTableDetails(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var activeBooking = await _datBanRepo.GetActiveBookingForTableAsync(
                id,
                userId,
                User.IsInRole("Admin"));

            if (activeBooking == null) return Ok(new { monDaDat = new List<object>() });

            var monDaDat = await _chiTietDatMonRepo.GetTableDetailsAsync(activeBooking.Id);

            return Ok(new { monDaDat });
        }

        private async Task<string> SaveImageAsync(IFormFile imageFile)
        {
            var imagesFolder = Path.Combine(_environment.ContentRootPath, "frontend", "Images");
            Directory.CreateDirectory(imagesFolder);

            var extension = Path.GetExtension(imageFile.FileName);
            var uniqueFileName = $"{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(imagesFolder, uniqueFileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await imageFile.CopyToAsync(stream);

            return uniqueFileName;
        }
    }
}

// Class phụ để nhận dữ liệu từ JSON body của Frontend
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

        if (HinhAnhFile == null || HinhAnhFile.Length == 0)
        {
            return true;
        }

        var extension = Path.GetExtension(HinhAnhFile.FileName).ToLowerInvariant();
        if (!AllowedImageExtensions.Contains(extension))
        {
            errorMessage = "Định dạng ảnh không hợp lệ. Chỉ hỗ trợ: .jpg, .jpeg, .png, .webp";
            return false;
        }

        if (HinhAnhFile.Length > MaxImageSizeInBytes)
        {
            errorMessage = "Kích thước ảnh vượt quá 5MB.";
            return false;
        }

        return true;
    }
}

public class OrderRequest
    {
        public int BanId { get; set; }
        public int MonId { get; set; }
        public int SoLuong { get; set; }
    }
