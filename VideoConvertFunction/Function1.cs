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
        //h�mtar variabler fr�n appsettings
        private static readonly string FfmpegExecutablePath = Environment.GetEnvironmentVariable("FfmpegPath");
        private static readonly string BlobStorageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        private static readonly string DatabaseConnectionString = Environment.GetEnvironmentVariable("DefaultConnection");

        [FunctionName("Function1")]
        public static async Task Run(
            [BlobTrigger("input-videos/{name}", Connection = "AzureWebJobsStorage")] BlobClient inputBlob,
            string name,//n�r det kommer en ny blob i input-videos kommer dess namn och fil h�mtas 
            ILogger log)
        {
            string localInputPath = Path.GetTempFileName();
            string localOutputPath = null;
            string localThumbnailPath = null;

            try
            {
                await inputBlob.DownloadToAsync(localInputPath);//laddar ner blob till lokal fil

                string outputFileName = $"{Path.GetFileNameWithoutExtension(name)}.mp4";//d�per om konverterade filen till samma namn med mp4 i slutet
                localOutputPath = Path.Combine(Path.GetTempPath(), outputFileName); //skapar en tom fil och d�per den till tidigare raden
                localThumbnailPath = Path.Combine(Path.GetTempPath(), $"{Path.GetFileNameWithoutExtension(name)}.jpg"); //skapar namnet f�r thumbnail

                log.LogInformation($"Processing video: {name}"); //log.x �r logging 
                if (!await ConvertVideoAsync(localInputPath, localOutputPath, log))//funktionen som konverterar videon
                {
                    log.LogError($"Failed to process video: {name}");
                    return;
                }

                string processedVideoUrl = await UploadFileToBlobAsync("processed-videos", localOutputPath, outputFileName, log);//funktion som laddar upp till blob storage
                log.LogInformation($"Processed video URL: {processedVideoUrl}");

                // updatera databasen och skapa thumbnail om det beh�vs
                await UpdateVideoDatabaseEntry(name, processedVideoUrl, localInputPath, localThumbnailPath, log);
            }
            catch (Exception ex)
            {
                log.LogError($"Exception occurred: {ex.Message}");
            }
            finally
            {
                CleanUpLocalFiles(new string[] { localInputPath, localOutputPath, localThumbnailPath }, log);//tar bort alla temp files som skapades f�r att store och upload videon med
                                                                                                             //korrekt namn
            }
        }

        private static async Task<bool> ConvertVideoAsync(string inputPath, string outputPath, ILogger log)//funktionen som samlar inst�llningar f�r konverteringen och sedan kallar
                                                                                                           //�p� funktionen som konverterar
        {
            var arguments = $"-y -i \"{inputPath}\" -c:v libx264 -preset fast -crf 22 -c:a aac -b:a 192k \"{outputPath}\"";//settings f�r ffmpeg
            return await ExecuteFfmpegAsync(arguments, log);//konverteringen
        }

        private static async Task<bool> ExecuteFfmpegAsync(string arguments, ILogger log)
        {
            using (var process = new Process())//skapar ny process
            {
                process.StartInfo.FileName = FfmpegExecutablePath;//s�kv�gen till ffmpeg filen som finns i appsettings
                process.StartInfo.Arguments = arguments;//argumenten f�r ffmpeg

                //inst�llningar f�r processen
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                //stringbuilder f�r att lagra output och error meddelande fr�n ffmpeg
                var output = new StringBuilder();
                var error = new StringBuilder();

                //event handler f�r vanlig output
                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        output.AppendLine(e.Data);
                        log.LogInformation(e.Data);
                    }
                };

                //eventhandler vid errormeddelande
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        error.AppendLine(e.Data);
                        log.LogError(e.Data);
                    }
                };

                process.Start();//det som startar processen med all konfiguration ovan

                //b�rja l�sningen av output och error meddelande
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                //v�ntar p� avslutning
                var exited = await Task.Run(() => process.WaitForExit(6000000)); // dont touch the damn timeout
                //om det tar f�r l�ng tid som speciferas i timeout
                if (!exited)
                {
                    log.LogError("FFmpeg processing timed out.");
                    process.Kill();
                    return false;
                }
                //om den avslutar med error
                if (process.ExitCode != 0)
                {
                    log.LogError($"FFmpeg processing failed with exit code {process.ExitCode}.");
                    log.LogError($"FFmpeg error: {error.ToString()}");
                    return false;
                }
                //n�r var exited blir klar
                log.LogInformation("FFmpeg processing completed successfully.");
                return true;
            }
        }

        private static async Task<string> UploadFileToBlobAsync(string containerName, string filePath, string fileName, ILogger log)//uppladning av filer till blob
        {
            var blobServiceClient = new BlobServiceClient(BlobStorageConnectionString);//connect till blobstorage
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);//connect till r�tt container
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);//om container inte finns skapa den

            var blobClient = containerClient.GetBlobClient(fileName);//skpar blobclient f�r filen
            await using (var uploadFileStream = File.OpenRead(filePath))//�ppnar filen f�r uppladdning
            {
                await blobClient.UploadAsync(uploadFileStream, true);//laddar upp filen till blob storage
            }

            log.LogInformation($"File uploaded to {blobClient.Uri}");
            return blobClient.Uri.ToString();//returnerar l�nken till filen i blob storage
        }

        private static async Task UpdateVideoDatabaseEntry(string blobName, string processedVideoUrl, string inputPath, string thumbnailPath, ILogger log)//funktion som skriver till db
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();//h�mtar dbcontext
            optionsBuilder.UseSqlServer(DatabaseConnectionString);//connect med connectionstring

            using (var dbContext = new ApplicationDbContext(optionsBuilder.Options))
            {
                var video = await dbContext.Videos.FirstOrDefaultAsync(v => v.BlobName == blobName);//hittar raden i databasen d�r blobname matchar
                if (video != null)//om det finns
                {
                    video.ProcessedVideoBlobUrl = processedVideoUrl;//skriver in l�nken f�r processed video
                    video.ProcessingStatus = "Ready";

                    if (video.ShouldGenerateThumbnail)
                    {
                        if (await GenerateThumbnailAsync(inputPath, thumbnailPath, log))//om skapandet av thumbnail g�r bra
                        {
                            string thumbnailUrl = await UploadFileToBlobAsync("thumbnails", thumbnailPath, Path.GetFileName(thumbnailPath), log);//laddar upp thumbnail till blobstorage
                            video.ThumbnailUrl = thumbnailUrl;//skriver thumbnail l�nk till databasen
                        }
                    }

                    await dbContext.SaveChangesAsync();//sparar �ndringar
                    log.LogInformation("Database updated with processed video URL and status.");
                }
                else
                {
                    log.LogWarning($"Video with FileName '{blobName}' not found in the database.");
                }
            }
        }

        private static async Task<bool> GenerateThumbnailAsync(string videoPath, string thumbnailPath, ILogger log)//funktion f�r skapa thumbnail
        {
            var arguments = $"-y -i \"{videoPath}\" -ss 00:00:01 -vframes 1 \"{thumbnailPath}\"";//inst�llningar p� ffmpeg f�r att skapa en thumbnail fr�n en video
            return await ExecuteFfmpegAsync(arguments, log);//k�r ffmpeg med arguments 
        }

        private static void CleanUpLocalFiles(string[] filePaths, ILogger log)//funktion f�r att ta bort lokla filer som speciferas
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
