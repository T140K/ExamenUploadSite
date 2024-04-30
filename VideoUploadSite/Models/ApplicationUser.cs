using Microsoft.AspNetCore.Identity;

namespace VideoUploadSite.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string DisplayName { get; set; } = "";
    }
}
