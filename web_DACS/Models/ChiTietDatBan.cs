// Ví dụ cho ChiTietDatBan
using web_DACS.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class ChiTietDatBan
{
    [Key]
    public int Id { get; set; }

    public int DatBanId { get; set; }
    [ForeignKey("DatBanId")]
    public virtual DatBan? DatBan { get; set; }

    public int MonAnId { get; set; }
    [ForeignKey("MonAnId")]
    public virtual MonAn? MonAn { get; set; } // Giữ lại để biết món đó là món gì

    public int SoLuong { get; set; }
}