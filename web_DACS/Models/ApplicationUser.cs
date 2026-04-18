using Microsoft.AspNetCore.Identity;

namespace web_DACS.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public bool IsBlocked { get; set; } = false;
        public string? Age { get; set; }
    }
}
