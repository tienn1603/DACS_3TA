using web_DACS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_DACS.Repositories.Interfaces;
using System.Security.Claims;

namespace web_DACS.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class DatBanController : ControllerBase
    {
        private readonly IDatBanRepository _datBanRepo;
        private readonly IBanAnRepository _banAnRepo;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly Data.ApplicationDbContext _context; // Dùng cho Chi tiết món

        public DatBanController(
            IDatBanRepository datBanRepo,
            IBanAnRepository banAnRepo,
            UserManager<ApplicationUser> userManager,
            Data.ApplicationDbContext context)
        {
            _datBanRepo = datBanRepo;
            _banAnRepo = banAnRepo;
            _userManager = userManager;
            _context = context;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetIndex()
        {
            return Ok(await _datBanRepo.GetAllAsync());
        }

        [HttpPost("Create")]
        [Authorize]
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

            await _datBanRepo.AddAsync(datBan);
            await _datBanRepo.SaveChangesAsync();

            // Cập nhật trạng thái bàn thông qua BanAnRepository
            var ban = await _banAnRepo.GetByIdAsync(request.BanAnId);
            if (ban != null)
            {
                ban.TrangThai = 2; // Màu vàng
                await _banAnRepo.UpdateAsync(ban);
                await _banAnRepo.SaveChangesAsync();
            }

            // Lưu món ăn
            if (request.CartItems != null && request.CartItems.Any())
            {
                foreach (var item in request.CartItems)
                {
                    _context.ChiTietDatMons.Add(new ChiTietDatMon
                    {
                        DatBanId = datBan.Id,
                        MonAnId = item.Id,
                        SoLuong = item.Qty,
                        BanAnId = request.BanAnId
                    });
                }
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Thành công!", bookingId = datBan.Id });
        }

        [HttpPost("ConfirmPayment/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ConfirmPayment(int id)
        {
            var donDat = await _datBanRepo.GetByIdAsync(id);
            if (donDat == null) return NotFound();

            // Giải phóng bàn (xử lý cả bàn đơn và bàn gộp)
            if (!string.IsNullOrEmpty(donDat.GhiChuGopBan))
            {
                var ids = donDat.GhiChuGopBan.Split(',').Select(int.Parse);
                foreach (var bId in ids)
                {
                    var b = await _banAnRepo.GetByIdAsync(bId);
                    if (b != null) b.TrangThai = 0;
                }
            }
            else if (donDat.BanAn != null)
            {
                donDat.BanAn.TrangThai = 0;
            }

            // Xóa món và đơn
            var chiTiet = _context.ChiTietDatMons.Where(c => c.DatBanId == id);
            _context.ChiTietDatMons.RemoveRange(chiTiet);

            await _datBanRepo.DeleteAsync(id);
            await _datBanRepo.SaveChangesAsync();

            return Ok(new { message = "Đã thanh toán và giải phóng bàn!" });
        }
    }
}

// Các class DTO để nhận dữ liệu từ JSON của VS Code
public class BookingRequest
    {
        public int BanAnId { get; set; }
        public string TenKhachHang { get; set; }
        public string SoDienThoai { get; set; }
        public DateTime GioDenDuyKien { get; set; }
        public List<CartItemDTO>? CartItems { get; set; }
    }

    public class CartItemDTO
    {
        public int Id { get; set; }
        public int Qty { get; set; }
    }
