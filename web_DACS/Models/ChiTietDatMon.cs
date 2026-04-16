using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace web_DACS.Models
{
    public class ChiTietDatMon
    {
        public int Id { get; set; }

        public int BanAnId { get; set; }

        [JsonIgnore] 
        public virtual BanAn? BanAn { get; set; }

        public int MonAnId { get; set; }

        public virtual MonAn? MonAn { get; set; }

        public int? DatBanId { get; set; }

        [JsonIgnore] 
        public virtual DatBan? DatBan { get; set; }

        public int SoLuong { get; set; }
    }
}