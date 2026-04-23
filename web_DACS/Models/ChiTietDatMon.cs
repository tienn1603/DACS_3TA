using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        public virtual DatBan? DatBan { get; set; }

        [Required]
        public int MonAnId { get; set; }

        [ForeignKey("MonAnId")]
        public virtual MonAn? MonAn { get; set; }

        public int SoLuong { get; set; }

        public int BanAnId { get; set; }
    }
}
