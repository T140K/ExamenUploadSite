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

        //h�mtar alla services
        private readonly IAzureService _azureService;
        public List<VideoPlayerModel> Videos { get; private set; }
        //dependency injection f�r azureservice
        public WatchVideoModel(IAzureService azureService)
        {
            _azureService = azureService;
        }

        public async Task OnGetAsync()
        {
            //videos fylls p� av en service i azureservice
            Videos = (await _azureService.ListVideoUrlsAsync()).ToList();
            //n�r man klickar p� en video i index kommer man hit med en id, den id anv�nds f�r att h�mta videon man klickade p�
            SelectedVideo = Videos.FirstOrDefault(video => video.Id == VideoId);
        }
    }
}