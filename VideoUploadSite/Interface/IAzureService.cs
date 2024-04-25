using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VideoUploadSite.Models.DTO;

namespace VideoUploadSite.Interface
{
    public interface IAzureService
    {

        Task<string> UploadFileToBlobAsync(string containerName, string filePath, string fileName);
        Task<IEnumerable<VideoPlayerModel>> ListVideoUrlsAsync(string containerName);
        Task<string> UploadThumbnailToBlobAsync(string containerName, IFormFile thumbnail);
        Task<bool> DeleteBlobAsync(string inputVideoUrl, string processedVideoUrl, string thumbnailUrl);
    }   
}