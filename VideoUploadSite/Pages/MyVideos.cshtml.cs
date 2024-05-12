using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using VideoUploadSite.Interface;
using VideoUploadSite.Models.DTO;

namespace VideoUploadSite.Pages
{
    [Authorize]
    public class MyVideosModel : PageModel
    {
        private readonly IAzureService _azureService;

        public MyVideosModel(IAzureService azureService)
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
