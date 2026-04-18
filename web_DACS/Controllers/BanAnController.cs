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
    public class BanAnController : ControllerBase
    {
        private readonly IBanAnRepository _repo; 
        private readonly IDatBanRepository _datBanRepo;

        public BanAnController(IBanAnRepository repo, IDatBanRepository datBanRepo)
        {
            _repo = repo;
            _datBanRepo = datBanRepo;
        }

        // 1. Lấy danh sách sơ đồ bàn thực tế
        [HttpGet]
        public async Task<IActionResult> GetBanAns()
        {
            var bayGio = DateTime.Now;
            var expiredBookings = await _datBanRepo.GetExpiredPendingBookingsAsync(bayGio);

            foreach (var booking in expiredBookings)
            {
                booking.TrangThai = 3;
                if (booking.BanAn != null) booking.BanAn.TrangThai = 0;
            }
            if (expiredBookings.Any()) await _datBanRepo.SaveChangesAsync();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var allActiveBookings = await _datBanRepo.GetActiveBookingsAsync();

            var listBanAn = await _repo.GetAllAsync(); 

            var result = listBanAn.Select(ban => {
                var active = allActiveBookings.FirstOrDefault(d =>
                    d.BanAnId == ban.Id ||
                    (d.GhiChuGopBan != null && d.GhiChuGopBan.Split(',').Contains(ban.Id.ToString()))
                );

                return new
                {
                    id = ban.Id,
                    soBan = ban.SoBan,
                    soChoNgoi = ban.SoChoNgoi,
                    trangThai = ban.TrangThai,
                    isOwner = active != null && active.UserId == userId,
                    bookingId = active?.Id
                };
            });

            return Ok(result);
        }

        // 2. Thêm bàn mới (Admin)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] BanAn banAn)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            await _repo.AddAsync(banAn);
            await _repo.SaveChangesAsync();
            return Ok(banAn);
        }

        // 3. Đổi trạng thái nhanh (Admin)
        [HttpPost("ToggleStatus/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var banAn = await _repo.GetByIdAsync(id);
            if (banAn == null) return NotFound();

            banAn.TrangThai = (banAn.TrangThai == 0) ? 1 : 0;
            _repo.Update(banAn);
            await _repo.SaveChangesAsync();
            return Ok(new { message = "Cập nhật thành công", currentStatus = banAn.TrangThai });
        }

        // 4. Xóa bàn (Admin)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var banAn = await _repo.GetByIdAsync(id);
            if (banAn == null) return NotFound();

            await _repo.DeleteAsync(id);
            await _repo.SaveChangesAsync();
            return Ok(new { message = "Đã xóa bàn thành công" });
        }

        // 5. Chỉnh sửa thông tin (Admin)
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] BanAn banAn)
        {
            if (id != banAn.Id) return BadRequest();
            _repo.Update(banAn);
            await _repo.SaveChangesAsync();
            return Ok(new { message = "Cập nhật thành công" });
        }
    }
}