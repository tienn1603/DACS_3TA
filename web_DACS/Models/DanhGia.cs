using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace web_DACS.Models;

public class DanhGia
{
    public int Id { get; set; }

    public int DatBanId { get; set; }

    [Range(1, 5, ErrorMessage = "Số sao phải từ 1 đến 5")]
    public int SoSao { get; set; }

    [MaxLength(500, ErrorMessage = "Nội dung đánh giá không quá 500 ký tự")]
    public string? NoiDung { get; set; }

    public DateTime NgayDanhGia { get; set; } = DateTime.UtcNow;

    public string UserId { get; set; } = string.Empty;

    [JsonIgnore]
    public virtual DatBan? DatBan { get; set; }
}
