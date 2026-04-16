using web_DACS.Data;
using web_DACS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using web_DACS.Repositories.Interfaces;

namespace web_DACS.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class MonAnController : ControllerBase
    {
        private readonly IMonAnRepository _monAnRepo;
        private readonly ApplicationDbContext _context; 

        public MonAnController(IMonAnRepository monAnRepo, ApplicationDbContext context)
        {
            _monAnRepo = monAnRepo;
            _context = context;
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

        [HttpPost("Order")]
        [Authorize]
        public async Task<IActionResult> Order([FromBody] OrderRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Tìm đơn đặt bàn đang hoạt động
            var activeBooking = await _context.DatBans
                .FirstOrDefaultAsync(d => d.BanAnId == request.BanId
                                     && d.TrangThai != 2 // 2 là trạng thái đã hoàn tất/hủy tùy logic của bạn
                                     && (User.IsInRole("Admin") || d.UserId == userId));

            if (activeBooking == null)
                return BadRequest(new { message = "Bàn này hiện chưa có khách hoặc bạn không có quyền." });

            var existingItem = await _context.ChiTietDatMons
                .FirstOrDefaultAsync(o => o.DatBanId == activeBooking.Id && o.MonAnId == request.MonId);

            if (existingItem != null)
            {
                existingItem.SoLuong += (request.SoLuong < 1 ? 1 : request.SoLuong);
            }
            else
            {
                _context.ChiTietDatMons.Add(new ChiTietDatMon
                {
                    BanAnId = request.BanId,
                    MonAnId = request.MonId,
                    SoLuong = request.SoLuong,
                    DatBanId = activeBooking.Id
                });
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Chọn món thành công!" });
        }

        [HttpGet("GetTableDetails/{id}")]
        public async Task<IActionResult> GetTableDetails(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var activeBooking = await _context.DatBans
                .FirstOrDefaultAsync(d => d.BanAnId == id
                                     && d.TrangThai != 2
                                     && (d.UserId == userId || User.IsInRole("Admin")));

            if (activeBooking == null) return Ok(new { monDaDat = new List<object>() });

            var monDaDat = await _context.ChiTietDatMons
                .Where(ct => ct.DatBanId == activeBooking.Id)
                .Select(ct => new {
                    tenMon = ct.MonAn!.TenMon,
                    soLuong = ct.SoLuong,
                    gia = ct.MonAn.Gia,
                    thanhTien = ct.SoLuong * ct.MonAn.Gia
                }).ToListAsync();

            return Ok(new { monDaDat });
        }
    }
}

// Class phụ để nhận dữ liệu từ JSON body của Frontend
public class OrderRequest
    {
        public int BanId { get; set; }
        public int MonId { get; set; }
        public int SoLuong { get; set; }
    }
