using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http; 
using Microsoft.AspNetCore.Mvc;

namespace VideoUploadSite.Models.DTO
{
    public class VideoUploadDto
    {
        [Required(ErrorMessage = "Title is required")]
        public string VideoTitle { get; set; }
        public string? VideoDescription { get; set; }
        [Required(ErrorMessage = "File is required")]
        public IFormFile File { get; set; }
        public IFormFile? Thumbnail { get; set; }

    }
}