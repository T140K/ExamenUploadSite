using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using VideoUploadSite.Data;
using VideoUploadSite.Interface;
using VideoUploadSite.Services;
using VideoUploadSite.Models;

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


            //finns för att lägga till egna rader till user table och har options som requireconfrim, alltså måste användaren confrim sin email som ajg stängde av
            builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false).AddEntityFrameworkStores<ApplicationDbContext>();

            /*builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();*/

            builder.Services.AddAuthentication().AddGoogle(googleOptions =>
            {
                googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"];
                googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
            });


            var blobStorageConnectionString = builder.Configuration.GetConnectionString("BlobConnectionString");
            builder.Services.AddSingleton(x => new BlobServiceClient(blobStorageConnectionString));

            //tester f�r att limit max upload size till azure blob storage
            builder.Services.Configure<KestrelServerOptions>(options =>
            {
                options.Limits.MaxRequestBodySize = 2147483648;
            });

            //tester f�r att limit max upload size till azure blob storage
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

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseCors("CorsVideoPolicy");
            app.MapControllers();
            app.MapRazorPages();

            app.Run();
        }
    }
}
