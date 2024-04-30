using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VideoUploadSite.Models.DTO;

namespace VideoUploadSite.Interface
{
    public interface IAzureService
    {
        //interface för Services/AzureService
        Task<string> UploadFileToBlobAsync(string containerName, string filePath, string fileName);
        Task<IEnumerable<VideoPlayerModel>> ListVideoUrlsAsync();
        Task<string> UploadThumbnailToBlobAsync(string containerName, IFormFile thumbnail);
        Task<bool> DeleteBlobAsync(string inputVideoUrl, string processedVideoUrl, string thumbnailUrl);
    }
}