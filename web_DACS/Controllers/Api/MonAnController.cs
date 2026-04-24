using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using web_DACS.DTOs;
using web_DACS.Models;
using web_DACS.Repositories.Interfaces;
using web_DACS.Services.Interfaces;

namespace web_DACS.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,User")]
    public class MonAnController : ControllerBase
    {
        private readonly IMonAnService _monAnService;
        private readonly IDatBanRepository _datBanRepo;
        private readonly IChiTietDatMonRepository _chiTietDatMonRepo;
        private readonly IWebHostEnvironment _environment;

        public MonAnController(
            IMonAnService monAnService,
            IDatBanRepository datBanRepo,
            IChiTietDatMonRepository chiTietDatMonRepo,
            IWebHostEnvironment environment)
        {
            _monAnService = monAnService;
            _datBanRepo = datBanRepo;
            _chiTietDatMonRepo = chiTietDatMonRepo;
            _environment = environment;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetMonAns([FromQuery] string? searchString)
        {
            // Seed if empty
            await _monAnService.SeedDataIfEmptyAsync();
            var result = await _monAnService.GetAllAsync(searchString);
            return result; // Trả thẳng ApiResponse, KHÔNG bọc thêm Ok() để tránh double-wrap
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetMonAnById(int id)
        {
            var result = await _monAnService.GetByIdAsync(id);
            if (!result.Status) return NotFound(result);
            return result; // Trả thẳng ApiResponse, KHÔNG bọc thêm Ok()
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateMonAn([FromForm] CreateMonAnRequest request)
        {
            if (!request.IsImageFileValid(out var imageValidationMessage))
                return ApiResponse.Fail(imageValidationMessage!);

            string? imageFileName = null;
            if (request.HinhAnhFile != null && request.HinhAnhFile.Length > 0)
                imageFileName = await SaveImageAsync(request.HinhAnhFile);

            var result = await _monAnService.CreateAsync(
                request.TenMon, request.MoTa, request.Gia, request.Loai, imageFileName);

            if (!result.Status) return result;
            return result; // Trả thẳng ApiResponse, KHÔNG bọc thêm Ok()
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateMonAn(int id, [FromBody] UpdateMonAnRequest request)
        {
            var result = await _monAnService.UpdateAsync(
                id, request.TenMon, request.MoTa, request.Gia, request.Loai, request.HinhAnh);
            if (!result.Status) return result;
            return result; // Trả thẳng ApiResponse, KHÔNG bọc thêm Ok()
        }

        [HttpPut("{id}/with-image")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateMonAnWithImage(int id, [FromForm] CreateMonAnRequest request)
        {
            string? imageFileName = null;
            if (request.HinhAnhFile != null && request.HinhAnhFile.Length > 0)
                imageFileName = await SaveImageAsync(request.HinhAnhFile);

            var result = await _monAnService.UpdateAsync(
                id, request.TenMon, request.MoTa, request.Gia, request.Loai,
                imageFileName ?? request.HinhAnh);
            if (!result.Status) return result;
            return result;
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteMonAn(int id)
        {
            var result = await _monAnService.DeleteAsync(id);
            if (!result.Status) return result;
            return result; // Trả thẳng ApiResponse, KHÔNG bọc thêm Ok()
        }

        [HttpPost("Order")]
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> Order([FromBody] OrderRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var activeBooking = await _datBanRepo.GetActiveBookingForTableAsync(
                request.BanId, userId, User.IsInRole("Admin"));

            if (activeBooking == null)
                return BadRequest(ApiResponse.Fail("Bàn này hiện chưa có khách hoặc bạn không có quyền."));

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
            return Ok(ApiResponse.Ok("Chọn món thành công!"));
        }

        [HttpGet("GetTableDetails/{id}")]
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> GetTableDetails(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _chiTietDatMonRepo.GetTableDetailsAsync(id);
            return Ok(ApiResponse.Ok("Lấy chi tiết thành công.", new { monDaDat = result }));
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

    public class UpdateMonAnRequest
    {
        public string TenMon { get; set; } = string.Empty;
        public string? MoTa { get; set; }
        public decimal Gia { get; set; }
        public string? Loai { get; set; }
        public string? HinhAnh { get; set; }
    }
}
