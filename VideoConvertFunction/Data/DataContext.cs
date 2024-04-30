using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VideoUploadSite.DTO;

namespace VideoUploadSite.Data
{
    public class ApplicationDbContext : DbContext //dbcontext för koppling med databasen
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Video> Videos { get; set; }//tabellen för databasen
    }
}
