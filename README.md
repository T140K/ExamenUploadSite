# VideoUploadSite

Min repo för examenarbetet baserad på videouploadsite jag tidigare har gjort

To run this project you need to configure appsettings.json in videouploadsite:

{
  "DetailedErrors": true,
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "sql db connection string"
  },
  "BlobConnectionString": "azure blob storage connection string"
}

You also need local.settings.json for VideoConvertFunction

{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true;",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "VideoStorageConnectionString": "blob connection string",
    "DefaultConnection": "db connection string",
    "FfmpegPath": "ffmpeg/bin/ffmpeg.exe" //path to ffmpeg in VideoConvertfunction
  }
}

You also need to add ffmpeg files to VideoConvertFunction yourself since they are too big for git to handle
You can download them from this upload i did https://file.io/EuBc3yrilNmZ 

You can also get them by downloading ffmpeg from https://ffmpeg.org/download.html 
Once you have ffmpeg make a new folder according to ffmpegpath and put in the files into videoconvertfunction
