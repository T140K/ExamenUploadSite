using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VideoUploadSite.Models;

namespace VideoUploadSite.Data
{
    public class ApplicationDbContext : DbContext//basic dbcontext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Video> Videos { get; set; }
    }
}