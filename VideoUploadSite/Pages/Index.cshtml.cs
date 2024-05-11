using Microsoft.AspNetCore.Mvc.RazorPages;
using VideoUploadSite.Interface;
using System.Collections.Generic;
using System.Threading.Tasks;
using VideoUploadSite.Models;
using VideoUploadSite.Services;
using Microsoft.AspNetCore.Authorization;
using VideoUploadSite.Models.DTO;

namespace VideoUploadSite.Pages
{
    [AllowAnonymous]
    public class IndexModel : PageModel
    {
        private readonly IAzureService _azureService;

        public IndexModel(IAzureService azureService)
        {
            _azureService = azureService;
        }
        public List<VideoPlayerModel> Videos { get; private set; }

        public async Task OnGetAsync()
        {
            Videos = (await _azureService.ListVideoUrlsAsync()).ToList();
        }
    }
}
