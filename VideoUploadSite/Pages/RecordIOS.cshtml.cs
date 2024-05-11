using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace VideoUploadSite.Pages
{
    [Authorize]
    public class iosrecordtestModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
