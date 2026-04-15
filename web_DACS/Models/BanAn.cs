using System.ComponentModel.DataAnnotations;

namespace web_DACS.Models
{
    public class BanAn
    {
        [Key]
        public int Id { get; set; }
        public string TenBan { get; set; }
        public int SucChua { get; set; }
        public string TrangThai { get; set; } // Trống, Đã đặt, Đang dùng
    }
}