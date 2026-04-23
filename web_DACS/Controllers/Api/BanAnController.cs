using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using web_DACS.DTOs;
using web_DACS.Services.Interfaces;

namespace web_DACS.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,User")]
    public class BanAnController : ControllerBase
    {
        private readonly IBanAnService _banAnService;

        public BanAnController(IBanAnService banAnService)
        {
            _banAnService = banAnService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetBanAns()
        {
            var result = await _banAnService.GetAllAsync();
            return result; // Trả thẳng ApiResponse
        }

        [HttpGet("map")]
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> GetBanAnsMap()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            var result = await _banAnService.GetTableMapAsync(userId, isAdmin);
            return result; // Trả thẳng ApiResponse
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateBanAnRequest request)
        {
            if (!ModelState.IsValid) return ApiResponse.Fail("Dữ liệu không hợp lệ.");
            var result = await _banAnService.CreateAsync(request.SoBan, request.SoChoNgoi);
            if (!result.Status) return result;
            return result;
        }

        [HttpPost("ToggleStatus/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var result = await _banAnService.ToggleStatusAsync(id);
            if (!result.Status) return result;
            return result;
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _banAnService.DeleteAsync(id);
            if (!result.Status) return result;
            return result;
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateBanAnRequest request)
        {
            var result = await _banAnService.UpdateAsync(id, request.SoBan, request.SoChoNgoi, request.TrangThai);
            if (!result.Status) return result;
            return result;
        }
    }

    public class CreateBanAnRequest
    {
        public string SoBan { get; set; } = string.Empty;
        public int SoChoNgoi { get; set; }
    }

    public class UpdateBanAnRequest
    {
        public string SoBan { get; set; } = string.Empty;
        public int SoChoNgoi { get; set; }
        public int? TrangThai { get; set; }
    }
}
