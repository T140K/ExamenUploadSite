using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using VideoUploadSite.Data;
using VideoUploadSite.DTO;
using Azure.Storage.Blobs.Models;

namespace VideoConvertFunction
{
    public static class Function1
    {
        private static readonly string FfmpegExecutablePath = Environment.GetEnvironmentVariable("FfmpegPath");
        private static readonly string BlobStorageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        private static readonly string DatabaseConnectionString = Environment.GetEnvironmentVariable("DefaultConnection");

        [FunctionName("Function1")]
        public static async Task Run(
            [BlobTrigger("input-videos/{name}", Connection = "AzureWebJobsStorage")] BlobClient inputBlob,
            string name,
            ILogger log)
        {
            string localInputPath = Path.GetTempFileName();
            string localOutputPath = null;
            string localThumbnailPath = null;

            try
            {
                await inputBlob.DownloadToAsync(localInputPath);

                string outputFileName = $"{Path.GetFileNameWithoutExtension(name)}.mp4";
                localOutputPath = Path.Combine(Path.GetTempPath(), outputFileName);
                localThumbnailPath = Path.Combine(Path.GetTempPath(), $"{Path.GetFileNameWithoutExtension(name)}.jpg");

                log.LogInformation($"Processing video: {name}");
                if (!await ConvertVideoAsync(localInputPath, localOutputPath, log))
                {
                    log.LogError($"Failed to process video: {name}");
                    return;
                }

                string processedVideoUrl = await UploadFileToBlobAsync("processed-videos", localOutputPath, outputFileName, log);
                log.LogInformation($"Processed video URL: {processedVideoUrl}");

                // Update the database and possibly generate a thumbnail
                await UpdateVideoDatabaseEntry(name, processedVideoUrl, localInputPath, localThumbnailPath, log);
            }
            catch (Exception ex)
            {
                log.LogError($"Exception occurred: {ex.Message}");
            }
            finally
            {
                CleanUpLocalFiles(new string[] { localInputPath, localOutputPath, localThumbnailPath }, log);
            }
        }

        private static async Task<bool> ConvertVideoAsync(string inputPath, string outputPath, ILogger log)
        {
            var arguments = $"-y -i \"{inputPath}\" -c:v libx264 -preset fast -crf 22 -c:a aac -b:a 192k \"{outputPath}\"";
            return await ExecuteFfmpegAsync(arguments, log);
        }

        private static async Task<bool> ExecuteFfmpegAsync(string arguments, ILogger log)
        {
            using (var process = new Process())
            {
                process.StartInfo.FileName = FfmpegExecutablePath;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                var output = new StringBuilder();
                var error = new StringBuilder();

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        output.AppendLine(e.Data);
                        log.LogInformation(e.Data);
                    }
                };
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        error.AppendLine(e.Data);
                        log.LogError(e.Data);
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                var exited = await Task.Run(() => process.WaitForExit(6000000)); // dont touch the damn timeout
                if (!exited)
                {
                    log.LogError("FFmpeg processing timed out.");
                    process.Kill();
                    return false;
                }

                if (process.ExitCode != 0)
                {
                    log.LogError($"FFmpeg processing failed with exit code {process.ExitCode}.");
                    log.LogError($"FFmpeg error: {error.ToString()}");
                    return false;
                }

                log.LogInformation("FFmpeg processing completed successfully.");
                return true;
            }
        }

        private static async Task<string> UploadFileToBlobAsync(string containerName, string filePath, string fileName, ILogger log)
        {
            var blobServiceClient = new BlobServiceClient(BlobStorageConnectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            var blobClient = containerClient.GetBlobClient(fileName);
            await using (var uploadFileStream = File.OpenRead(filePath))
            {
                await blobClient.UploadAsync(uploadFileStream, true);
            }

            log.LogInformation($"File uploaded to {blobClient.Uri}");
            return blobClient.Uri.ToString();
        }

        private static async Task UpdateVideoDatabaseEntry(string blobName, string processedVideoUrl, string inputPath, string thumbnailPath, ILogger log)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlServer(DatabaseConnectionString);

            using (var dbContext = new ApplicationDbContext(optionsBuilder.Options))
            {
                var video = await dbContext.Videos.FirstOrDefaultAsync(v => v.BlobName == blobName);
                if (video != null)
                {
                    video.ProcessedVideoBlobUrl = processedVideoUrl;
                    video.ProcessingStatus = "Ready";

                    if (video.ShouldGenerateThumbnail)
                    {
                        if (await GenerateThumbnailAsync(inputPath, thumbnailPath, log))
                        {
                            string thumbnailUrl = await UploadFileToBlobAsync("thumbnails", thumbnailPath, Path.GetFileName(thumbnailPath), log);
                            video.ThumbnailUrl = thumbnailUrl;
                        }
                    }

                    await dbContext.SaveChangesAsync();
                    log.LogInformation("Database updated with processed video URL and status.");
                }
                else
                {
                    log.LogWarning($"Video with FileName '{blobName}' not found in the database.");
                }
            }
        }

        private static async Task<bool> GenerateThumbnailAsync(string videoPath, string thumbnailPath, ILogger log)
        {
            var arguments = $"-y -i \"{videoPath}\" -ss 00:00:01 -vframes 1 \"{thumbnailPath}\"";
            return await ExecuteFfmpegAsync(arguments, log);
        }

        private static void CleanUpLocalFiles(string[] filePaths, ILogger log)
        {
            foreach (var filePath in filePaths)
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    log.LogInformation($"Deleted temporary file: {filePath}");
                }
            }
        }
    }
}
