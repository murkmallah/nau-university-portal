using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NAU_Web.Models;

namespace NAU_Web.Services
{
    public class SiteBrandingService : ISiteBrandingService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IWebHostEnvironment _env;
        private static SiteBrandingSetting? _cachedSetting;
        private static readonly object _lock = new();

        public SiteBrandingService(IServiceProvider serviceProvider, IWebHostEnvironment env)
        {
            _serviceProvider = serviceProvider;
            _env = env;
        }

        private async Task EnsureTableAndSeededAsync(NauDbContext db)
        {
            try
            {
                var count = await db.SiteBrandingSettings.CountAsync();
                if (count > 0) return;
            }
            catch
            {
                try
                {
                    await db.Database.ExecuteSqlRawAsync(@"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SiteBrandingSettings')
                        BEGIN
                            CREATE TABLE [SiteBrandingSettings] (
                                [Id] INT IDENTITY(1,1) PRIMARY KEY,
                                [SiteTitle] NVARCHAR(200) NOT NULL,
                                [Tagline] NVARCHAR(200) NULL,
                                [LogoUrl] NVARCHAR(500) NOT NULL,
                                [DarkModeLogoUrl] NVARCHAR(500) NULL,
                                [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
                            );
                        END
                    ");
                }
                catch { }
            }

            var setting = await db.SiteBrandingSettings.FirstOrDefaultAsync();
            if (setting == null)
            {
                setting = new SiteBrandingSetting
                {
                    SiteTitle = "National Aviation University",
                    Tagline = "LEARN · LEAD · FLY HIGH",
                    LogoUrl = "/images/logo.png",
                    DarkModeLogoUrl = null,
                    UpdatedAt = DateTime.UtcNow
                };
                db.SiteBrandingSettings.Add(setting);
                await db.SaveChangesAsync();
            }
        }

        public async Task<SiteBrandingSetting> GetSettingAsync()
        {
            if (_cachedSetting != null) return _cachedSetting;

            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NauDbContext>();
            await EnsureTableAndSeededAsync(db);

            var setting = await db.SiteBrandingSettings.FirstOrDefaultAsync() ?? new SiteBrandingSetting();
            _cachedSetting = setting;
            return setting;
        }

        public SiteBrandingSetting GetSetting()
        {
            if (_cachedSetting != null) return _cachedSetting;
            return new SiteBrandingSetting
            {
                SiteTitle = "National Aviation University",
                Tagline = "LEARN · LEAD · FLY HIGH",
                LogoUrl = "/images/logo.png"
            };
        }

        public async Task UpdateSettingAsync(string siteTitle, string tagline, string logoUrl, string? darkModeLogoUrl)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NauDbContext>();
            await EnsureTableAndSeededAsync(db);

            var setting = await db.SiteBrandingSettings.FirstOrDefaultAsync();
            if (setting == null)
            {
                setting = new SiteBrandingSetting();
                db.SiteBrandingSettings.Add(setting);
            }

            setting.SiteTitle = string.IsNullOrWhiteSpace(siteTitle) ? "National Aviation University" : siteTitle.Trim();
            setting.Tagline = string.IsNullOrWhiteSpace(tagline) ? "LEARN · LEAD · FLY HIGH" : tagline.Trim();
            setting.LogoUrl = string.IsNullOrWhiteSpace(logoUrl) ? "/images/logo.png" : logoUrl.Trim();
            setting.DarkModeLogoUrl = string.IsNullOrWhiteSpace(darkModeLogoUrl) ? null : darkModeLogoUrl.Trim();
            setting.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            _cachedSetting = setting;
        }

        public async Task<string> UploadLogoAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("No file provided.");

            var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".svg", ".webp", ".gif" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (Array.IndexOf(allowedExtensions, ext) < 0)
                throw new InvalidOperationException("Only image files (.png, .jpg, .svg, .webp) are allowed.");

            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "branding");
            if (!Directory.Exists(uploadsDir))
                Directory.CreateDirectory(uploadsDir);

            var fileName = $"logo_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..6]}{ext}";
            var fullPath = Path.Combine(uploadsDir, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/branding/{fileName}";
        }

        public async Task<string> GetHeaderLogoHtmlAsync()
        {
            var s = await GetSettingAsync();
            var logo = System.Net.WebUtility.HtmlEncode(s.LogoUrl ?? "/images/logo.png");
            var title = System.Net.WebUtility.HtmlEncode(s.SiteTitle ?? "National Aviation University");
            var tagline = System.Net.WebUtility.HtmlEncode(s.Tagline ?? "LEARN · LEAD · FLY HIGH");

            return $@"<a href=""/"" class=""logo"">
    <img src=""{logo}"" alt=""{title} Logo"" onerror=""this.style.display='none'"" style=""height:52px;width:auto;display:block;object-fit:contain;"">
    <div style=""display:flex;flex-direction:column;justify-content:center;gap:1px;"">
      <span style=""font-size:24px;font-weight:900;color:#0b2c4d;font-family:'Montserrat',sans-serif;line-height:1.1;letter-spacing:0px;"">{title}</span>
      <span style=""font-size:12px;font-weight:500;color:#474b50;letter-spacing:2.5px;font-family:'Open Sans',sans-serif;line-height:1;"">{tagline}</span>
    </div>
  </a>";
        }

        public async Task<string> GetFooterLogoHtmlAsync()
        {
            var s = await GetSettingAsync();
            var title = System.Net.WebUtility.HtmlEncode(s.SiteTitle ?? "National Aviation University");
            var logo = System.Net.WebUtility.HtmlEncode(!string.IsNullOrWhiteSpace(s.DarkModeLogoUrl) ? s.DarkModeLogoUrl : (s.LogoUrl ?? "/images/logo.png"));
            var filterStyle = string.IsNullOrWhiteSpace(s.DarkModeLogoUrl) ? "filter:brightness(0) invert(1);" : "";

            return $@"<a href=""/"" style=""display:flex;align-items:center;gap:12px;margin-bottom:14px;text-decoration:none;"">
        <img src=""{logo}"" alt=""{title} Logo"" onerror=""this.style.display='none'"" style=""height:48px;width:auto;display:block;object-fit:contain;{filterStyle}"">
        <span style=""font-size:20px;font-weight:700;color:#fff;font-family:'Poppins',sans-serif;white-space:nowrap;"">{title}</span>
      </a>";
        }
    }
}
