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
using Microsoft.AspNetCore.Authorization;

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
        [Authorize]
        [HttpPost("Upload")]
        [RequestSizeLimit(200_000_000)]
        public async Task<IActionResult> UploadVideo([FromForm] VideoUploadDto uploadDto)
        {
            //limit för storleken av en video som kan laddas upp i controllern
            if (uploadDto.File == null || uploadDto.File.Length > 50 * 1024 * 1024)
            {
                return BadRequest("File size should not exceed 50 MB.");
            }

            //2 rader av kod för att skapa random namn så att filer inte overwrite i azure
            var uniqueId = Guid.NewGuid().ToString();
            var fileName = $"{uniqueId}_{uploadDto.File.FileName}";

            var tempFilePath = Path.GetTempFileName();//skapar en temp fil

            using (var stream = System.IO.File.Create(tempFilePath))
            {
                await uploadDto.File.CopyToAsync(stream);//hämtar videon in i tomma filen
            }

            //service för att ladda upp temp filen till azure storage container input-videos, när filen laddas upp dit kommer blob trigger att se en ny video och konvertera skriva till
            //databasen som returnerar en länk till filen i container
            var uploadResult = await _azureService.UploadFileToBlobAsync("input-videos", tempFilePath, fileName);
            if (uploadResult == null)//om det inte kommer en response från uploaden till azure kommer tempfilen tas bort och skicka tillbaka en badrequest
            {
                System.IO.File.Delete(tempFilePath);
                return BadRequest("Could not upload the video file.");
            }

            System.IO.File.Delete(tempFilePath);//om det går bra tas filen bort ändå och controllern fortsätter

            //om en thumbnail skickades med till api kommer shouldgeneratethumbnail vara false, om det inte finns en thumbnail kommer shouldgeneratethumbnail vara true och då
            //vet blobtrigger att även skapa en thumbnail vid konverteringen
            bool shouldGenerateThumbnail = uploadDto.Thumbnail == null;
            string thumbnailBlobUrl = null;
            if (!shouldGenerateThumbnail)//om man inte behöver skapa thumbnail laddas thumbnail som finns upp och en länk till den returneras
            {
                thumbnailBlobUrl = await _azureService.UploadThumbnailToBlobAsync("thumbnails", uploadDto.Thumbnail);
            }

            var video = new Video //skriver till databasen med alla länkar till uppladdade filer och skriver all annan info 
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

        [HttpGet("ProcessingStatus/{videoId}")]//controller som visar om videon har konverteras än, anvädns för testing/redovisningen
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
        [Authorize]
        [HttpDelete("DeleteVideo/{videoId}")]
        public async Task<IActionResult> DeleteVideo(int videoId)//ta bort video med id
        {
            var video = await _context.Videos.FindAsync(videoId);//hämtar video
            if (video == null || video.ThumbnailUrl == null || video.BlobName == null)//ser till att den hittar videon
            {
                return NotFound();//om det inte finns en video, thumbnail eller blobname kommer det att avbrytas
            }

            //service som tar bort alla videos (processed och input) och thumbnail, alla parameter man skickar in ska return true för att videon ska tas bort annars avbryts det
            bool blobDeleted = await _azureService.DeleteBlobAsync(video.VideoBlobUrl, video.ProcessedVideoBlobUrl, video.ThumbnailUrl);
            if (!blobDeleted)
            {
                _logger.LogError($"Failed to delete blob '{video.BlobName}' from Azure Storage.");
                return StatusCode(500, "Failed to delete video blob from Azure Storage.");
            }

            _context.Videos.Remove(video);//tar bort alla hittade videos
            await _context.SaveChangesAsync();

            return Ok(RedirectToPage("/"));//efter man klickar deletevideo kommer en error eller så kommer man redirect
        }

        [Authorize]
        [HttpPut("EditVideo/{videoId}")]//basic edit controller
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

