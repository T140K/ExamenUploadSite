using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoUploadSite.DTO
{
    public class Video//modell för databasen
    {
        [Key]
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? FileName;
        public string? VideoBlobUrl { get; set; }
        public string? ProcessedVideoBlobUrl { get; set; } = null;
        public string? ThumbnailUrl { get; set; } = null;
        public string? BlobName { get; set; } = null;
        public bool ShouldGenerateThumbnail { get; set; }
        public string? ProcessingStatus { get; set; }
        public string? VideoOwner { get; set; }
        public string? VideoLink { get; set; }
    }
}
