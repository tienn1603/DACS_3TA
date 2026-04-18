using web_DACS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using web_DACS.Repositories.Interfaces;

namespace web_DACS.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,User")]
    public class DatBanController : ControllerBase
    {
        private readonly IDatBanRepository _datBanRepo;
        private readonly UserManager<ApplicationUser> _userManager;

        public DatBanController(
            IDatBanRepository datBanRepo,
            UserManager<ApplicationUser> userManager)
        {
            _datBanRepo = datBanRepo;
            _userManager = userManager;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetIndex()
        {
            return Ok(await _datBanRepo.GetAllAsync());
        }

        [HttpPost("Create")]
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> Create([FromBody] BookingRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            if (user.IsBlocked) return StatusCode(403, new { message = "Tài khoản bị chặn." });

            // Kiểm tra giới hạn đặt bàn
            if (await _datBanRepo.GetActiveBookingCountByUserAsync(user.Id) >= 2)
            {
                user.IsBlocked = true;
                await _userManager.UpdateAsync(user);
                return BadRequest(new { message = "Vượt quá giới hạn đặt bàn. Bạn đã bị chặn!" });
            }

            var datBan = new DatBan
            {
                TenKhachHang = request.TenKhachHang,
                SoDienThoai = request.SoDienThoai,
                GioDenDuyKien = request.GioDenDuyKien,
                BanAnId = request.BanAnId,
                UserId = user.Id,
                NgayDat = DateTime.Now,
                TrangThai = 0
            };

            var cartItems = request.CartItems?
                .Select(i => (i.Id, i.Qty < 1 ? 1 : i.Qty))
                .ToList() ?? [];

            await _datBanRepo.CreateWithDetailsAsync(datBan, cartItems);

            return Ok(new { message = "Thành công!", bookingId = datBan.Id });
        }

        [HttpPost("ConfirmPayment/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ConfirmPayment(int id)
        {
            var success = await _datBanRepo.ConfirmPaymentAsync(id);
            if (!success) return NotFound();

            return Ok(new { message = "Đã thanh toán và giải phóng bàn!" });
        }

        [HttpGet("my-history")]
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> GetMyHistory()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var history = await _datBanRepo.GetByUserIdAsync(userId);
            return Ok(history);
        }

        [HttpDelete("cancel/{id}")]
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> CancelBooking(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var success = await _datBanRepo.CancelPendingBookingAsync(id, userId);
            if (!success)
            {
                return BadRequest(new { message = "Không thể hủy đơn. Chỉ đơn Pending của chính bạn mới được hủy." });
            }

            return Ok(new { message = "Hủy đơn thành công, bàn đã được cập nhật Available." });
        }
    }
}

// Các class DTO để nhận dữ liệu từ JSON của VS Code
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
