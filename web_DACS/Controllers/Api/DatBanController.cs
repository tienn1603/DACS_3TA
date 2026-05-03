using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using web_DACS.DTOs;
using web_DACS.Models;
using web_DACS.Services.Interfaces;

namespace web_DACS.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,User")]
    public class DatBanController : ControllerBase
    {
        private readonly IDatBanService _datBanService;
        private readonly UserManager<ApplicationUser> _userManager;

        public DatBanController(IDatBanService datBanService, UserManager<ApplicationUser> userManager)
        {
            _datBanService = datBanService;
            _userManager = userManager;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetIndex()
        {
            var result = await _datBanService.GetAllAsync();
            return result; // Trả thẳng ApiResponse, KHÔNG bọc thêm Ok()
        }

        [HttpPost("Create")]
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> Create([FromBody] BookingRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized(ApiResponse.Fail("Người dùng không hợp lệ."));

            var cartItems = request.CartItems?
                .Select(i => (i.Id, i.Qty < 1 ? 1 : i.Qty))
                .ToList() ?? new List<(int, int)>();

            var result = await _datBanService.CreateAsync(
                request.TenKhachHang,
                request.SoDienThoai,
                request.GioDenDuyKien,
                request.BanAnId,
                user.Id,
                user.IsBlocked,
                cartItems);

            if (!result.Status)
            {
                if (user.IsBlocked)
                    return StatusCode(403, result);
                return result;
            }

            return result;
        }

        [HttpPost("ConfirmPayment/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ConfirmPayment(int id)
        {
            var result = await _datBanService.ConfirmPaymentAsync(id);
            if (!result.Status) return result;
            return result;
        }

        [HttpPost("ConfirmBooking/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ConfirmBooking(int id)
        {
            var result = await _datBanService.ConfirmBookingAsync(id);
            if (!result.Status) return result;
            return result;
        }

        [HttpGet("my-history")]
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> GetMyHistory()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse.Fail("UserId không hợp lệ."));

            var result = await _datBanService.GetMyHistoryAsync(userId);
            return result; // Trả thẳng ApiResponse
        }

        [HttpDelete("cancel/{id}")]
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> CancelBooking(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse.Fail("UserId không hợp lệ."));

            var result = await _datBanService.CancelPendingBookingAsync(id, userId);
            if (!result.Status) return result;
            return result; // Trả thẳng ApiResponse
        }

        [HttpPost("CreateDanhGia")]
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> CreateDanhGia([FromBody] DanhGiaRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse.Fail("UserId không hợp lệ."));

            if (request.SoSao < 1 || request.SoSao > 5)
                return BadRequest(ApiResponse.Fail("Số sao phải từ 1 đến 5."));

            var result = await _datBanService.CreateDanhGiaAsync(
                request.DatBanId, userId, request.SoSao, request.NoiDung);
            return result;
        }

        [HttpDelete("cancel-by-admin/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CancelByAdmin(int id)
        {
            var result = await _datBanService.CancelByAdminAsync(id);
            if (!result.Status) return result;
            return result;
        }
    }
}
