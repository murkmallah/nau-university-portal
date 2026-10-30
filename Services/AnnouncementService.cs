using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NAU_Web.Models;

namespace NAU_Web.Services
{
    public class AnnouncementService : IAnnouncementService
    {
        private readonly IServiceProvider _serviceProvider;
        private static AnnouncementSetting? _cachedSetting;
        private static readonly object _lock = new();

        public AnnouncementService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        private async Task EnsureTableAndSeededAsync(NauDbContext db)
        {
            try
            {
                var count = await db.AnnouncementSettings.CountAsync();
                if (count > 0) return;
            }
            catch
            {
                try
                {
                    await db.Database.ExecuteSqlRawAsync(@"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AnnouncementSettings')
                        BEGIN
                            CREATE TABLE [AnnouncementSettings] (
                                [Id] INT IDENTITY(1,1) PRIMARY KEY,
                                [TagText] NVARCHAR(50) NOT NULL,
                                [MessageText] NVARCHAR(300) NOT NULL,
                                [LinkUrl] NVARCHAR(300) NULL,
                                [IsVisible] BIT NOT NULL DEFAULT 1,
                                [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
                            );
                        END
                    ");
                }
                catch { }
            }

            var setting = await db.AnnouncementSettings.FirstOrDefaultAsync();
            if (setting == null)
            {
                setting = new AnnouncementSetting
                {
                    TagText = "ANNOUNCEMENT:",
                    MessageText = "Admissions Open Now",
                    LinkUrl = "/CourseCatalog",
                    IsVisible = true,
                    UpdatedAt = DateTime.UtcNow
                };
                db.AnnouncementSettings.Add(setting);
                await db.SaveChangesAsync();
            }
        }

        public async Task<AnnouncementSetting> GetSettingAsync()
        {
            if (_cachedSetting != null) return _cachedSetting;

            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NauDbContext>();
            await EnsureTableAndSeededAsync(db);

            var setting = await db.AnnouncementSettings.FirstOrDefaultAsync() ?? new AnnouncementSetting();
            _cachedSetting = setting;
            return setting;
        }

        public async Task UpdateSettingAsync(string tagText, string messageText, string? linkUrl, bool isVisible)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NauDbContext>();
            await EnsureTableAndSeededAsync(db);

            var setting = await db.AnnouncementSettings.FirstOrDefaultAsync();
            if (setting == null)
            {
                setting = new AnnouncementSetting();
                db.AnnouncementSettings.Add(setting);
            }

            setting.TagText = string.IsNullOrWhiteSpace(tagText) ? "ANNOUNCEMENT:" : tagText.Trim();
            setting.MessageText = string.IsNullOrWhiteSpace(messageText) ? "Admissions Open Now" : messageText.Trim();
            setting.LinkUrl = string.IsNullOrWhiteSpace(linkUrl) ? null : linkUrl.Trim();
            setting.IsVisible = isVisible;
            setting.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            _cachedSetting = setting;
        }

        public async Task<string> GetRenderedAnnouncementBarHtmlAsync()
        {
            var s = await GetSettingAsync();
            return GenerateAnnouncementBarHtml(s);
        }

        public static string GenerateAnnouncementBarHtml(AnnouncementSetting s)
        {
            if (!s.IsVisible) return "";

            var tag = System.Net.WebUtility.HtmlEncode(s.TagText ?? "ANNOUNCEMENT:");
            var msg = System.Net.WebUtility.HtmlEncode(s.MessageText ?? "Admissions Open Now");
            var link = !string.IsNullOrWhiteSpace(s.LinkUrl) ? System.Net.WebUtility.HtmlEncode(s.LinkUrl) : "";

            var messageContent = !string.IsNullOrEmpty(link)
                ? $"<a href=\"{link}\" style=\"color:inherit;text-decoration:none;\">{msg}</a>"
                : msg;

            return $@"<div class=""announcement"">
  <div class=""announcement-scroll-wrap"">
    <div class=""scroll""><span>{tag}</span> {messageContent}</div>
  </div>
  <div class=""account-dropdown""><a class=""apply-btn"" role=""button"" href=""#"">Account</a></div>
</div>";
        }
    }
}
