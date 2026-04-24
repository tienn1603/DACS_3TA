using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace web_DACS.Models
{
    [Table("ChiTietDatMons")]
    public class ChiTietDatMon
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DatBanId { get; set; }

        [ForeignKey("DatBanId")]
        [JsonIgnore] // Ngăn chu kỳ: ChiTietDatMon → DatBan → ChiTietDatMons
        public virtual DatBan? DatBan { get; set; }

        [Required]
        public int MonAnId { get; set; }

        [ForeignKey("MonAnId")]
        public virtual MonAn? MonAn { get; set; }

        public int SoLuong { get; set; }

        public int BanAnId { get; set; }
    }
}
