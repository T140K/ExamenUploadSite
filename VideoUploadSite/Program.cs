using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using VideoUploadSite.Data;
using VideoUploadSite.Interface;
using VideoUploadSite.Services;

namespace VideoUploadSite
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorPages();

            // Custom configurations

            builder.Services.AddControllers();

            builder.Services.AddScoped<IAzureService, AzureService>();

            // Add CORS policy
            builder.Services.AddCors(option =>
            {
                option.AddPolicy("CorsVideoPolicy",
                    policyBuilder => policyBuilder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader());
            });

            // Configure Entity Framework Core with SQL Server
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
                sqlServerOptionsAction: sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure();
                }));

          
            var blobStorageConnectionString = builder.Configuration.GetConnectionString("BlobConnectionString");
            builder.Services.AddSingleton(x => new BlobServiceClient(blobStorageConnectionString));

            
            builder.Services.Configure<KestrelServerOptions>(options =>
            {
                options.Limits.MaxRequestBodySize = 2147483648;
            });

            
            builder.Services.Configure<FormOptions>(x =>
            {
                x.MultipartBodyLengthLimit = 2147483648; //2GB
            });

            // Build the application
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapRazorPages();

            app.UseCors("CorsVideoPolicy");
            app.MapControllers();
            app.MapRazorPages();

            app.Run();
        }
    }
}
