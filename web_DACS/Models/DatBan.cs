using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace web_DACS.Models
{
    public class DatBan
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên khách hàng")]
        public string TenKhachHang { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string SoDienThoai { get; set; } = string.Empty;

        public DateTime NgayDat { get; set; }
        public string? GhiChuGopBan { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn giờ đến dự kiến")]
        public DateTime GioDenDuyKien { get; set; }

        public DateTime GioHetHan => GioDenDuyKien.AddHours(2);

        public int BanAnId { get; set; }

        [JsonIgnore] // Không trả về object BanAn để tránh lặp
        public virtual BanAn? BanAn { get; set; }

        public int TrangThai { get; set; }

        public string? UserId { get; set; }


        public virtual ICollection<ChiTietDatMon> ChiTietDatMons { get; set; } = new List<ChiTietDatMon>();
    }
}