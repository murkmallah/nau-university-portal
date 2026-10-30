using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NAU_Web.Models;

namespace NAU_Web.Services
{
    public class FooterService : IFooterService
    {
        private readonly IServiceProvider _serviceProvider;
        private static FooterSetting? _cachedSetting;

        public FooterService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        private static string GetDefaultSocialLinksJson()
        {
            var list = new List<FooterSocialLinkItem>
            {
                new FooterSocialLinkItem { Title = "Facebook", Icon = "fab fa-facebook-f", Url = "https://www.facebook.com/CATI.PK/", Target = "_blank" },
                new FooterSocialLinkItem { Title = "Instagram", Icon = "fab fa-instagram", Url = "https://www.instagram.com/cati.gov.pk?igsh=NzE1anhjOGJkemNh", Target = "_blank" },
                new FooterSocialLinkItem { Title = "LinkedIn", Icon = "fab fa-linkedin-in", Url = "https://www.linkedin.com/company/civil-aviation-training-institute-cati/posts/", Target = "_blank" },
                new FooterSocialLinkItem { Title = "YouTube", Icon = "fab fa-youtube", Url = "https://www.youtube.com/@CATIHyderabad", Target = "_blank" },
                new FooterSocialLinkItem { Title = "WhatsApp", Icon = "fab fa-whatsapp", Url = "https://wa.me/92229260310", Target = "_blank" }
            };
            return JsonSerializer.Serialize(list);
        }

        private static string GetDefaultQuickLinksJson()
        {
            var list = new List<FooterColumnItem>
            {
                new FooterColumnItem
                {
                    ColumnTitle = "Quick Links",
                    Links = new List<FooterLinkItem>
                    {
                        new FooterLinkItem { Text = "About Us", Url = "/about.html", Target = "_self" },
                        new FooterLinkItem { Text = "Academics", Url = "/Faculties/Public", Target = "_self" },
                        new FooterLinkItem { Text = "Admissions", Url = "/Account/Register", Target = "_self" },
                        new FooterLinkItem { Text = "Training", Url = "/SimulationLabs/Public", Target = "_self" },
                        new FooterLinkItem { Text = "Campus Life", Url = "/facilities.html", Target = "_self" },
                        new FooterLinkItem { Text = "Contact Us", Url = "/Contact", Target = "_self" }
                    }
                }
            };
            return JsonSerializer.Serialize(list);
        }

        private static string GetDefaultBottomLinksJson()
        {
            var list = new List<FooterLinkItem>
            {
                new FooterLinkItem { Text = "Privacy Policy", Url = "#", Target = "_self" },
                new FooterLinkItem { Text = "Terms of Use", Url = "#", Target = "_self" },
                new FooterLinkItem { Text = "Sitemap", Url = "#", Target = "_self" }
            };
            return JsonSerializer.Serialize(list);
        }

        private async Task EnsureTableAndSeededAsync(NauDbContext db)
        {
            try
            {
                var count = await db.FooterSettings.CountAsync();
                if (count > 0) return;
            }
            catch
            {
                try
                {
                    await db.Database.ExecuteSqlRawAsync(@"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'FooterSettings')
                        BEGIN
                            CREATE TABLE [FooterSettings] (
                                [Id] INT IDENTITY(1,1) PRIMARY KEY,
                                [AboutText] NVARCHAR(MAX) NULL,
                                [ContactAddress] NVARCHAR(500) NULL,
                                [ContactPhone] NVARCHAR(200) NULL,
                                [ContactEmail] NVARCHAR(200) NULL,
                                [ContactWebsite] NVARCHAR(200) NULL,
                                [CopyrightText] NVARCHAR(500) NULL,
                                [SocialLinksJson] NVARCHAR(MAX) NULL,
                                [QuickLinksJson] NVARCHAR(MAX) NULL,
                                [BottomLinksJson] NVARCHAR(MAX) NULL,
                                [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
                            );
                        END
                    ");
                }
                catch { }
            }

            var setting = await db.FooterSettings.FirstOrDefaultAsync();
            if (setting == null)
            {
                setting = new FooterSetting
                {
                    AboutText = "A premier institution dedicated to excellence in aviation education, training, research and innovation.",
                    ContactAddress = "Airport Road, Hyderabad Sindh, Pakistan",
                    ContactPhone = "+92 22 9260310-19",
                    ContactEmail = "AdID.Academics@paa.gov.pk",
                    ContactWebsite = "https://catipakistan.com.pk",
                    CopyrightText = "© 2026 National Aviation University. All Rights Reserved.",
                    SocialLinksJson = GetDefaultSocialLinksJson(),
                    QuickLinksJson = GetDefaultQuickLinksJson(),
                    BottomLinksJson = GetDefaultBottomLinksJson(),
                    UpdatedAt = DateTime.UtcNow
                };
                db.FooterSettings.Add(setting);
                await db.SaveChangesAsync();
            }
            else
            {
                bool modified = false;
                if (string.IsNullOrWhiteSpace(setting.AboutText)) { setting.AboutText = "A premier institution dedicated to excellence in aviation education, training, research and innovation."; modified = true; }
                if (string.IsNullOrWhiteSpace(setting.ContactAddress)) { setting.ContactAddress = "Airport Road, Hyderabad Sindh, Pakistan"; modified = true; }
                if (string.IsNullOrWhiteSpace(setting.ContactPhone)) { setting.ContactPhone = "+92 22 9260310-19"; modified = true; }
                if (string.IsNullOrWhiteSpace(setting.ContactEmail)) { setting.ContactEmail = "AdID.Academics@paa.gov.pk"; modified = true; }
                if (string.IsNullOrWhiteSpace(setting.ContactWebsite) || setting.ContactWebsite == "www.cati.edu.pk") { setting.ContactWebsite = "https://catipakistan.com.pk"; modified = true; }
                if (string.IsNullOrWhiteSpace(setting.CopyrightText)) { setting.CopyrightText = "© 2026 National Aviation University. All Rights Reserved."; modified = true; }
                if (string.IsNullOrWhiteSpace(setting.SocialLinksJson) || setting.SocialLinksJson == "[]") { setting.SocialLinksJson = GetDefaultSocialLinksJson(); modified = true; }
                if (string.IsNullOrWhiteSpace(setting.QuickLinksJson) || setting.QuickLinksJson == "[]") { setting.QuickLinksJson = GetDefaultQuickLinksJson(); modified = true; }
                if (string.IsNullOrWhiteSpace(setting.BottomLinksJson) || setting.BottomLinksJson == "[]") { setting.BottomLinksJson = GetDefaultBottomLinksJson(); modified = true; }

                if (modified)
                {
                    setting.UpdatedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                }
            }
        }

        public async Task<FooterSetting> GetSettingAsync()
        {
            if (_cachedSetting != null) return _cachedSetting;

            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NauDbContext>();
            await EnsureTableAndSeededAsync(db);

            var setting = await db.FooterSettings.FirstOrDefaultAsync() ?? new FooterSetting
            {
                AboutText = "A premier institution dedicated to excellence in aviation education, training, research and innovation.",
                ContactAddress = "Airport Road, Hyderabad Sindh, Pakistan",
                ContactPhone = "+92 22 9260310-19",
                ContactEmail = "AdID.Academics@paa.gov.pk",
                ContactWebsite = "https://catipakistan.com.pk",
                CopyrightText = "© 2026 National Aviation University. All Rights Reserved.",
                SocialLinksJson = GetDefaultSocialLinksJson(),
                QuickLinksJson = GetDefaultQuickLinksJson(),
                BottomLinksJson = GetDefaultBottomLinksJson()
            };
            _cachedSetting = setting;
            return setting;
        }

        public FooterSetting GetSetting()
        {
            if (_cachedSetting != null) return _cachedSetting;
            return new FooterSetting
            {
                AboutText = "A premier institution dedicated to excellence in aviation education, training, research and innovation.",
                ContactAddress = "Airport Road, Hyderabad Sindh, Pakistan",
                ContactPhone = "+92 22 9260310-19",
                ContactEmail = "AdID.Academics@paa.gov.pk",
                ContactWebsite = "https://catipakistan.com.pk",
                CopyrightText = "© 2026 National Aviation University. All Rights Reserved.",
                SocialLinksJson = GetDefaultSocialLinksJson(),
                QuickLinksJson = GetDefaultQuickLinksJson(),
                BottomLinksJson = GetDefaultBottomLinksJson()
            };
        }

        public async Task UpdateSettingAsync(FooterSetting input)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NauDbContext>();
            await EnsureTableAndSeededAsync(db);

            var setting = await db.FooterSettings.FirstOrDefaultAsync();
            if (setting == null)
            {
                setting = new FooterSetting();
                db.FooterSettings.Add(setting);
            }

            setting.AboutText = input.AboutText;
            setting.ContactAddress = input.ContactAddress;
            setting.ContactPhone = input.ContactPhone;
            setting.ContactEmail = input.ContactEmail;
            setting.ContactWebsite = input.ContactWebsite;
            setting.CopyrightText = input.CopyrightText;
            setting.SocialLinksJson = input.SocialLinksJson;
            setting.QuickLinksJson = input.QuickLinksJson;
            setting.BottomLinksJson = input.BottomLinksJson;
            setting.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            _cachedSetting = setting;
        }

        public async Task ResetDefaultAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NauDbContext>();
            await EnsureTableAndSeededAsync(db);

            var setting = await db.FooterSettings.FirstOrDefaultAsync();
            if (setting == null)
            {
                setting = new FooterSetting();
                db.FooterSettings.Add(setting);
            }

            setting.AboutText = "A premier institution dedicated to excellence in aviation education, training, research and innovation.";
            setting.ContactAddress = "Airport Road, Hyderabad Sindh, Pakistan";
            setting.ContactPhone = "+92 22 9260310-19";
            setting.ContactEmail = "AdID.Academics@paa.gov.pk";
            setting.ContactWebsite = "https://catipakistan.com.pk";
            setting.CopyrightText = "© 2026 National Aviation University. All Rights Reserved.";
            setting.SocialLinksJson = GetDefaultSocialLinksJson();
            setting.QuickLinksJson = GetDefaultQuickLinksJson();
            setting.BottomLinksJson = GetDefaultBottomLinksJson();
            setting.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            _cachedSetting = setting;
        }
    }
}
