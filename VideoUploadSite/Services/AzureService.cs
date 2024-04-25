using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage;
using Azure.Storage.Blobs;
using VideoUploadSite.Interface;
using Microsoft.Extensions.Configuration;
using VideoUploadSite.Models;
using Azure.Storage.Blobs.Models;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Net;
using Microsoft.EntityFrameworkCore;
using VideoUploadSite.Data;
using VideoUploadSite.Models.DTO;
using Microsoft.AspNetCore.Http.HttpResults;
using Azure;

namespace VideoUploadSite.Services
{
    public class AzureService : IAzureService
    {
        # region Dependency Injection / Constructor
        private readonly string _storageConnectionString;
        private readonly string _storageContainerName;
        private readonly ILogger<AzureService> _logger;
        private static ApplicationDbContext _context;

        public AzureService(IConfiguration configuration, ILogger<AzureService> Logger, ApplicationDbContext context)
        {
            _storageConnectionString = configuration.GetValue<string>("BlobConnectionString");
            _storageContainerName = configuration.GetValue<string>("BlobContainerName");
            _logger = Logger;
            _context = context;
        }
        #endregion

        public async Task<IEnumerable<VideoPlayerModel>> ListVideoUrlsAsync(string containerName)
        {
            try
            {
                var videos = await _context.Videos.ToListAsync();
                videos.Reverse();

                if (videos.Count == 0)
                {
                    _logger.LogWarning("No videos were found in the database.");
                }

                List<VideoPlayerModel> videoList = new List<VideoPlayerModel>();

                foreach (var video in videos)
                {
                    _logger.LogTrace($"{video.Id} - {video.Title} \n {video.ProcessedVideoBlobUrl ?? video.VideoBlobUrl} \n {video.ThumbnailUrl}");

                    VideoPlayerModel videoModel = new VideoPlayerModel
                    {
                        Id = video.Id,
                        VideoTitle = video.Title,
                        VideoDescription = video.Description,
                        VideoUrl = video.ProcessedVideoBlobUrl ?? video.VideoBlobUrl,
                        ThumbnailUrl = video.ThumbnailUrl
                    };

                    videoList.Add(videoModel);
                }

                return videoList; //DB till Video, Video ska g� till videoplayermodel, videoplayermodel ska till index, index t user

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "error occured on getting videos from db");
                throw;
            }
        }

        public async Task<string> UploadThumbnailToBlobAsync(string containerName, IFormFile thumbnail)
        {
            try
            {
                var blobServiceClient = new BlobServiceClient(_storageConnectionString);
                var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
                await blobContainerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(thumbnail.FileName);
                var blobClient = blobContainerClient.GetBlobClient(fileName);
                using (var stream = thumbnail.OpenReadStream())
                {
                    await blobClient.UploadAsync(stream, true);
                }

                _logger.LogInformation($"Uploaded thumbnail '{fileName}' to container '{containerName}'.");
                return blobClient.Uri.AbsoluteUri;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error uploading thumbnail: {ex.Message}");
                return null;
            }
        }

        public async Task<string> UploadFileToBlobAsync(string containerName, string filePath, string fileName)
        {
            try
            {
                var blobServiceClient = new BlobServiceClient(_storageConnectionString);
                var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
                await blobContainerClient.CreateIfNotExistsAsync();

                var blobClient = blobContainerClient.GetBlobClient(fileName);

                await using var fileStream = File.OpenRead(filePath);
                await blobClient.UploadAsync(fileStream, true);

                _logger.LogInformation($"Uploaded file '{fileName}' to container '{containerName}'.");
                return blobClient.Uri.AbsoluteUri;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error uploading file: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> DeleteBlobAsync(string inputVideoUrl, string processedVideoUrl, string thumbnailUrl)
        {
            var blobServiceClient = new BlobServiceClient(_storageConnectionString);

            var inputvideosContainer = blobServiceClient.GetBlobContainerClient("input-videos");
            var processedvideosContainer = blobServiceClient.GetBlobContainerClient("processed-videos");
            var thumbnailContainer = blobServiceClient.GetBlobContainerClient("thumbnails");

            string inputVideoBlobName = ExtractBlobNameFromUrl(inputVideoUrl);
            string processedVideoBlobName = ExtractBlobNameFromUrl(processedVideoUrl);
            string thumbnailBlobName = ExtractBlobNameFromUrl(thumbnailUrl);

            bool inputVideoFound = await BlobExistsInContainerAsync(inputvideosContainer, inputVideoBlobName);
            bool processedVideoFound = await BlobExistsInContainerAsync(processedvideosContainer, processedVideoBlobName);
            bool thumbnailFound = await BlobExistsInContainerAsync(thumbnailContainer, thumbnailBlobName);

            if (thumbnailFound && (inputVideoFound || processedVideoFound))
            {
                bool inputVideosDeleted = await DeleteBlobFromContainerAsync(inputvideosContainer, inputVideoBlobName);
                bool processedVideosDeleted = await DeleteBlobFromContainerAsync(processedvideosContainer, processedVideoBlobName);
                bool thumbnailDeleted = await DeleteBlobFromContainerAsync(thumbnailContainer, thumbnailBlobName);
                
                if (thumbnailDeleted && (inputVideosDeleted || processedVideosDeleted))
                {
                    return true;
                }
            }

            return false;
        }
        private async Task<bool> DeleteBlobFromContainerAsync(BlobContainerClient container, string blobName)
        {
            try
            {
                var blobClient = container.GetBlobClient(blobName);
                if (blobClient != null)
                {
                    await blobClient.DeleteAsync();
                    return true;
                }

                return false;
            }
            catch (RequestFailedException ex)
            {
                // Handle any errors that occur during the deletion
                Console.WriteLine($"An error occurred while deleting blob '{blobName}': {ex.Message}");
                return false;
            }
        }


        private async Task<bool> BlobExistsInContainerAsync(BlobContainerClient containerClient, string blobName)
        {
            try
            {
                await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
                {
                    if (blobItem.Name == blobName)
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                Console.WriteLine($"Container not found or empty: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while checking blob existence: {ex.Message}");
                return false;
            }
        }

        private string ExtractBlobNameFromUrl(string url)
        {
            Uri uri = new Uri(url);
            return Path.GetFileName(uri.LocalPath);
        }
    }
}