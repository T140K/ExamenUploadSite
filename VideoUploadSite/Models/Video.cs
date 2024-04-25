using System.ComponentModel.DataAnnotations;

namespace VideoUploadSite.Models
{
    public class Video
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
    }
}
