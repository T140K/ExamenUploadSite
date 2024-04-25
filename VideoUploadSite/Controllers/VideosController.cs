using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VideoUploadSite.Data;
using VideoUploadSite.Interface;
using VideoUploadSite.Models;
using VideoUploadSite.Models.DTO;
using System.Net.Http.Json;
using Microsoft.VisualBasic.FileIO;

namespace VideoUploadSite.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VideosController : Controller
    {
        private readonly IAzureService _azureService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<VideosController> _logger;

        public VideosController(ApplicationDbContext context, IAzureService azureService, ILogger<VideosController> logger)
        {
            _context = context;
            _azureService = azureService ?? throw new ArgumentNullException(nameof(azureService));
            _logger = logger;
        }

        //-----------------------------------------------------------------
        [HttpPost("Upload")]
        [RequestSizeLimit(200_000_000)]
        public async Task<IActionResult> UploadVideo([FromForm] VideoUploadDto uploadDto)
        {
            if (uploadDto.File == null || uploadDto.File.Length > 50 * 1024 * 1024)
            {
                return BadRequest("File size should not exceed 50 MB.");
            }

            var uniqueId = Guid.NewGuid().ToString();
            var fileName = $"{uniqueId}_{uploadDto.File.FileName}";
            var tempFilePath = Path.GetTempFileName();

            using (var stream = System.IO.File.Create(tempFilePath))
            {
                await uploadDto.File.CopyToAsync(stream);
            }

            var uploadResult = await _azureService.UploadFileToBlobAsync("input-videos", tempFilePath, fileName);
            if (uploadResult == null)
            {
                System.IO.File.Delete(tempFilePath);
                return BadRequest("Could not upload the video file.");
            }

            System.IO.File.Delete(tempFilePath);

            bool shouldGenerateThumbnail = uploadDto.Thumbnail == null;
            string thumbnailBlobUrl = null;
            if (!shouldGenerateThumbnail)
            {
                thumbnailBlobUrl = await _azureService.UploadThumbnailToBlobAsync("thumbnails", uploadDto.Thumbnail);
            }

            var video = new Video
            {
                FileName = fileName,
                Title = uploadDto.VideoTitle,
                Description = uploadDto.VideoDescription,
                VideoBlobUrl = uploadResult,
                BlobName = fileName,
                ThumbnailUrl = thumbnailBlobUrl,
                ShouldGenerateThumbnail = shouldGenerateThumbnail,
                ProcessingStatus = "Processing"
            };

            _context.Videos.Add(video);
            await _context.SaveChangesAsync();

            return Ok("all good");
        }
        //------------------------------------------------------------------------------------------------------------------------------------
        [HttpGet("ProcessingStatus/{videoId}")]
        public async Task<IActionResult> GetProcessingStatus(int videoId)
        {
            var video = await _context.Videos.FindAsync(videoId);
            if (video == null)
            {
                return NotFound();
            }

            return Ok(video.ProcessingStatus);
        }
        //------------------------------------------------------------------------------------------------------------------------------------
        [HttpDelete("DeleteVideo/{videoId}")]
        public async Task<IActionResult> DeleteVideo(int videoId)
        {
            var video = await _context.Videos.FindAsync(videoId);
            if (video == null || video.ThumbnailUrl == null || video.BlobName == null)
            {
                return NotFound();
            }

            bool blobDeleted = await _azureService.DeleteBlobAsync(video.VideoBlobUrl, video.ProcessedVideoBlobUrl, video.ThumbnailUrl);
            if (!blobDeleted)
            {
                _logger.LogError($"Failed to delete blob '{video.BlobName}' from Azure Storage.");
                return StatusCode(500, "Failed to delete video blob from Azure Storage.");
            }

            _context.Videos.Remove(video);
            await _context.SaveChangesAsync();

            return Ok(RedirectToPage("/"));
        }
        [HttpPut("EditVideo/{videoId}")]
        public async Task<IActionResult> EditVideo(int videoId, [FromBody] VideoEditDto editDto)
        {
            var video = await _context.Videos.FindAsync(videoId);
            if (video == null)
            {
                return NotFound("Video not found.");
            }
            
            
            video.Title = editDto.Title;
            video.Description = editDto.Description;
            await _context.SaveChangesAsync();

            return Ok("Video details updated successfully.");
        }
    }
}
    
