using System.ComponentModel.DataAnnotations;

namespace web_DACS.Models
{
    public class DatBan
    {
        [Key]
        public int Id { get; set; }
        public string TenKhachHang { get; set; }
        public string SoDienThoai { get; set; }
        public DateTime ThoiGianDat { get; set; }
        public int SoLuongNguoi { get; set; }

        // Khóa ngoại liên kết tới bàn
        public int BanAnId { get; set; }
        public BanAn? BanAn { get; set; }
    }
}