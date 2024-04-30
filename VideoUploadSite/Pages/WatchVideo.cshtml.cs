using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using VideoUploadSite.Interface;
using VideoUploadSite.Models.DTO;
namespace VideoUploadSite.Pages
{
    public class WatchVideoModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public int VideoId { get; set; }

        public VideoPlayerModel SelectedVideo;

        //hämtar alla services
        private readonly IAzureService _azureService;
        public List<VideoPlayerModel> Videos { get; private set; }
        //dependency injection för azureservice
        public WatchVideoModel(IAzureService azureService)
        {
            _azureService = azureService;
        }

        public async Task OnGetAsync()
        {
            //videos fylls på av en service i azureservice
            Videos = (await _azureService.ListVideoUrlsAsync()).ToList();
            //när man klickar på en video i index kommer man hit med en id, den id används för att hämta videon man klickade på
            SelectedVideo = Videos.FirstOrDefault(video => video.Id == VideoId);
        }
    }
}