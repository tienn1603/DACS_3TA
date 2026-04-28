using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace web_DACS.Models
{
    public class BanAn
    {
        public int Id { get; set; }
        public string SoBan { get; set; } = string.Empty;
        public int SoChoNgoi { get; set; }
        public int TrangThai { get; set; }

        [JsonIgnore] // Chặn vòng lặp khi xuất JSON
        public virtual ICollection<DatBan> DatBans { get; set; } = new List<DatBan>();

        [NotMapped]
        public bool IsOwner { get; set; }

        [NotMapped]
        public DatBan? ActiveDatBan { get; set; }

        [NotMapped]
        public string TimeRange => ActiveDatBan != null
            ? $"{ActiveDatBan.GioDenDuyKien:HH:mm} - {ActiveDatBan.GioDenDuyKien.AddHours(2):HH:mm}"
            : string.Empty;
    }
}