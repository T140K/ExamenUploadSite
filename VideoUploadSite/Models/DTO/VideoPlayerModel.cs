using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace VideoUploadSite.Models.DTO
{
    public class VideoPlayerModel
    {
        [Key]
        public int Id { get; set; }
        public string? VideoUrl { get; set; }
        public string? ProcessingStatus { get; set; }
        public string? VideoTitle { get; set; }
        public string? VideoDescription { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? ThumbnailPath { get; set; }
        public string? VideoOwner { get; set; }
    }
}
