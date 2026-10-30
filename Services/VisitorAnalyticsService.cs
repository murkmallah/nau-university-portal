using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NAU_Web.Models;

namespace NAU_Web.Services
{
    public interface IVisitorAnalyticsService
    {
        VisitorVisitInfo ExtractVisitInfo(HttpContext context, string path, string? pageTitle = null);
        Task TrackVisitInfoAsync(VisitorVisitInfo info);
        Task TrackVisitAsync(HttpContext context, string path, string? pageTitle = null);
        Task<VisitorAnalyticsDashboardViewModel> GetDashboardDataAsync(string timeRange = "today");
        Task<object> GetLiveTelemetryAsync();
    }

    public class VisitorAnalyticsService : IVisitorAnalyticsService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<VisitorAnalyticsService> _logger;
        private static readonly ConcurrentDictionary<string, LiveVisitorSessionDto> _activeSessions = new();
        private static readonly ConcurrentBag<VisitorLog> _allLogs = new();
        private static readonly ConcurrentDictionary<string, (string Country, string Code, string Flag, string City)> _geoCache = new();
        private static readonly object _fileLock = new();
        private readonly string _logFilePath;

        public VisitorAnalyticsService(IHttpClientFactory httpClientFactory, ILogger<VisitorAnalyticsService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _logFilePath = Path.Combine(AppContext.BaseDirectory, "visitor_analytics.json");

            LoadPersistedLogs();
        }

        private void LoadPersistedLogs()
        {
            try
            {
                if (File.Exists(_logFilePath))
                {
                    lock (_fileLock)
                    {
                        var json = File.ReadAllText(_logFilePath);
                        var loaded = JsonSerializer.Deserialize<List<VisitorLog>>(json);
                        if (loaded != null && loaded.Any())
                        {
                            // Filter out any past mock/sample sessions
                            var realLogs = loaded.Where(l => !l.SessionId.StartsWith("s_")).TakeLast(5000).ToList();
                            foreach (var log in realLogs)
                            {
                                _allLogs.Add(log);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load visitor analytics persistence file.");
            }
        }

        private void PersistLogs()
        {
            try
            {
                lock (_fileLock)
                {
                    var logsToSave = _allLogs.Where(l => !l.SessionId.StartsWith("s_")).TakeLast(5000).ToList();
                    var json = JsonSerializer.Serialize(logsToSave, new JsonSerializerOptions { WriteIndented = false });
                    File.WriteAllText(_logFilePath, json);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist visitor analytics file.");
            }
        }

        public VisitorVisitInfo ExtractVisitInfo(HttpContext context, string path, string? pageTitle = null)
        {
            var info = new VisitorVisitInfo
            {
                Path = path ?? "/",
                PageTitle = pageTitle
            };

            if (context == null) return info;

            try
            {
                var now = DateTime.UtcNow;
                info.IpAddress = ExtractClientIp(context);
                info.UserAgent = context.Request.Headers["User-Agent"].ToString();
                var referrer = context.Request.Headers["Referer"].ToString();
                info.Referrer = string.IsNullOrWhiteSpace(referrer) ? "Direct Visit" : referrer;

                // Resolve Session ID from cookie or IP + UserAgent hash
                var sessionId = context.Request.Cookies["NAU_VISITOR_SID"];
                if (string.IsNullOrWhiteSpace(sessionId))
                {
                    sessionId = "v_" + Math.Abs((info.IpAddress + info.UserAgent).GetHashCode()).ToString("X8") + "_" + now.Ticks.ToString("X").Substring(6);
                    try
                    {
                        if (!context.Response.HasStarted)
                        {
                            context.Response.Cookies.Append("NAU_VISITOR_SID", sessionId, new CookieOptions
                            {
                                Expires = DateTimeOffset.UtcNow.AddDays(30),
                                HttpOnly = true,
                                SameSite = SameSiteMode.Lax
                            });
                        }
                    }
                    catch { }
                }
                info.SessionId = sessionId;

                info.IsRegistered = context.User?.Identity?.IsAuthenticated ?? false;
                info.UserEmail = info.IsRegistered ? context.User?.Identity?.Name : null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while extracting visitor info.");
            }

            return info;
        }

        public async Task TrackVisitAsync(HttpContext context, string path, string? pageTitle = null)
        {
            if (context == null) return;
            var info = ExtractVisitInfo(context, path, pageTitle);
            await TrackVisitInfoAsync(info);
        }

        public async Task TrackVisitInfoAsync(VisitorVisitInfo info)
        {
            if (info == null) return;

            try
            {
                var now = DateTime.UtcNow;
                var ip = info.IpAddress;
                var userAgent = info.UserAgent;
                var referrer = info.Referrer;
                var sessionId = info.SessionId;

                // Resolve Geolocation (Country, City, Flag)
                var geo = await ResolveGeoLocationAsync(ip);

                // Parse Device, Browser, OS
                var (device, browser, os) = ParseUserAgent(userAgent);

                var cleanTitle = !string.IsNullOrWhiteSpace(info.PageTitle) ? info.PageTitle : GetDefaultPageTitle(info.Path);

                var log = new VisitorLog
                {
                    Id = now.Ticks,
                    SessionId = sessionId,
                    IpAddress = ip,
                    Country = geo.Country,
                    CountryCode = geo.Code,
                    FlagEmoji = geo.Flag,
                    City = geo.City,
                    PageUrl = info.Path,
                    PageTitle = cleanTitle,
                    Referrer = referrer,
                    UserAgent = userAgent,
                    Browser = browser,
                    OperatingSystem = os,
                    DeviceType = device,
                    IsRegisteredUser = info.IsRegistered,
                    UserEmail = info.UserEmail,
                    VisitedAt = now,
                    LastActivityAt = now
                };

                _allLogs.Add(log);

                // Update Active Sessions Store
                _activeSessions.AddOrUpdate(sessionId, 
                    new LiveVisitorSessionDto
                    {
                        SessionId = sessionId,
                        IpAddress = ip,
                        Country = geo.Country,
                        CountryCode = geo.Code,
                        FlagEmoji = geo.Flag,
                        City = geo.City,
                        CurrentPage = info.Path,
                        PageTitle = cleanTitle,
                        DeviceType = device,
                        Browser = browser,
                        OperatingSystem = os,
                        IsOnline = true,
                        IsRegistered = info.IsRegistered,
                        UserEmail = info.UserEmail,
                        FirstSeenAt = now,
                        LastActivityAt = now,
                        DurationSeconds = 0
                    },
                    (k, old) =>
                    {
                        old.CurrentPage = info.Path;
                        old.PageTitle = cleanTitle;
                        old.LastActivityAt = now;
                        old.IsOnline = true;
                        old.DurationSeconds = (int)(now - old.FirstSeenAt).TotalSeconds;
                        return old;
                    });

                // Periodically persist
                if (_allLogs.Count % 5 == 0)
                {
                    _ = Task.Run(PersistLogs);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while tracking visitor event.");
            }
        }

        private string ExtractClientIp(HttpContext context)
        {
            // Support reverse proxy headers (Cloudflare, Nginx, IIS)
            var cfIp = context.Request.Headers["CF-Connecting-IP"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(cfIp)) return cfIp.Trim();

            var xff = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(xff))
            {
                var firstIp = xff.Split(',')[0].Trim();
                if (!string.IsNullOrWhiteSpace(firstIp)) return firstIp;
            }

            var xRealIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(xRealIp)) return xRealIp.Trim();

            var remoteIp = context.Connection.RemoteIpAddress?.ToString();
            return !string.IsNullOrWhiteSpace(remoteIp) ? remoteIp : "127.0.0.1";
        }

        private async Task<(string Country, string Code, string Flag, string City)> ResolveGeoLocationAsync(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip) || ip == "::1" || ip.StartsWith("127.") || ip.StartsWith("10.") || ip.StartsWith("192.168."))
            {
                return ("Pakistan", "PK", "🇵🇰", "Karachi / Local Server");
            }

            if (_geoCache.TryGetValue(ip, out var cached))
            {
                return cached;
            }

            try
            {
                using var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromMilliseconds(1200);
                var res = await client.GetFromJsonAsync<IpApiResponse>($"http://ip-api.com/json/{ip}?fields=status,country,countryCode,city");

                if (res != null && res.status == "success" && !string.IsNullOrWhiteSpace(res.country))
                {
                    var flag = CountryCodeToEmoji(res.countryCode ?? "PK");
                    var result = (res.country, res.countryCode ?? "PK", flag, res.city ?? "Karachi");
                    _geoCache[ip] = result;
                    return result;
                }
            }
            catch
            {
                // Fallback on timeout or offline
            }

            var fallback = ("Pakistan", "PK", "🇵🇰", "Karachi");
            _geoCache[ip] = fallback;
            return fallback;
        }

        private string CountryCodeToEmoji(string countryCode)
        {
            if (string.IsNullOrWhiteSpace(countryCode) || countryCode.Length != 2) return "🌐";
            countryCode = countryCode.ToUpperInvariant();
            if (countryCode == "PK") return "🇵🇰";
            if (countryCode == "US") return "🇺🇸";
            if (countryCode == "GB") return "🇬🇧";
            if (countryCode == "AE") return "🇦🇪";
            if (countryCode == "SA") return "🇸🇦";
            if (countryCode == "CA") return "🇨🇦";
            if (countryCode == "DE") return "🇩🇪";
            if (countryCode == "FR") return "🇫🇷";
            if (countryCode == "CN") return "🇨🇳";
            if (countryCode == "IN") return "🇮🇳";
            if (countryCode == "TR") return "🇹🇷";
            if (countryCode == "OM") return "🇴🇲";
            if (countryCode == "QA") return "🇶🇦";

            try
            {
                int firstChar = countryCode[0] - 'A' + 0x1F1E6;
                int secondChar = countryCode[1] - 'A' + 0x1F1E6;
                return char.ConvertFromUtf32(firstChar) + char.ConvertFromUtf32(secondChar);
            }
            catch
            {
                return "🌐";
            }
        }

        private (string Device, string Browser, string OS) ParseUserAgent(string userAgent)
        {
            if (string.IsNullOrWhiteSpace(userAgent))
                return ("Desktop", "Chrome", "Windows");

            var ua = userAgent.ToLowerInvariant();

            // Device
            string device = "Desktop";
            if (ua.Contains("ipad") || ua.Contains("tablet")) device = "Tablet";
            else if (ua.Contains("mobile") || ua.Contains("android") || ua.Contains("iphone")) device = "Mobile";

            // Browser
            string browser = "Chrome";
            if (ua.Contains("edg/")) browser = "Edge";
            else if (ua.Contains("opr/") || ua.Contains("opera")) browser = "Opera";
            else if (ua.Contains("firefox")) browser = "Firefox";
            else if (ua.Contains("safari") && !ua.Contains("chrome")) browser = "Safari";
            else if (ua.Contains("chrome")) browser = "Chrome";

            // OS
            string os = "Windows";
            if (ua.Contains("windows nt 10.0")) os = "Windows 11/10";
            else if (ua.Contains("windows")) os = "Windows";
            else if (ua.Contains("android")) os = "Android";
            else if (ua.Contains("iphone") || ua.Contains("ipad") || ua.Contains("ios")) os = "iOS";
            else if (ua.Contains("mac os")) os = "macOS";
            else if (ua.Contains("linux")) os = "Linux";

            return (device, browser, os);
        }

        private string GetDefaultPageTitle(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || path == "/") return "Homepage";
            if (path.StartsWith("/CourseCatalog", StringComparison.OrdinalIgnoreCase)) return "Course Catalog";
            if (path.StartsWith("/Courses", StringComparison.OrdinalIgnoreCase)) return "Short Courses";
            if (path.StartsWith("/Financial", StringComparison.OrdinalIgnoreCase)) return "Fee Structure & Financial";
            if (path.StartsWith("/About", StringComparison.OrdinalIgnoreCase) || path.Contains("about.html")) return "About NAU";
            if (path.StartsWith("/Contact", StringComparison.OrdinalIgnoreCase)) return "Contact Us";
            if (path.StartsWith("/Faculties", StringComparison.OrdinalIgnoreCase)) return "Faculties & Academics";
            if (path.StartsWith("/Facilities", StringComparison.OrdinalIgnoreCase) || path.Contains("facilities.html")) return "Facilities";
            if (path.StartsWith("/SimulationLabs", StringComparison.OrdinalIgnoreCase)) return "Simulation Labs";
            if (path.StartsWith("/NewsPosts", StringComparison.OrdinalIgnoreCase)) return "News & Media";
            if (path.StartsWith("/Account", StringComparison.OrdinalIgnoreCase)) return "Student Portal / Login";
            if (path.StartsWith("/Admin/Analytics", StringComparison.OrdinalIgnoreCase)) return "Visitor Analytics Dashboard";
            if (path.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase)) return "Admin Dashboard";
            return path;
        }

        public Task<VisitorAnalyticsDashboardViewModel> GetDashboardDataAsync(string timeRange = "today")
        {
            var now = DateTime.UtcNow;
            DateTime filterStart = timeRange switch
            {
                "7days" => now.AddDays(-7),
                "30days" => now.AddDays(-30),
                "all" => DateTime.MinValue,
                _ => now.Date // "today"
            };

            // Filter real logs only
            var filteredLogs = _allLogs
                .Where(l => !l.SessionId.StartsWith("s_") && l.VisitedAt >= filterStart)
                .ToList();

            var totalPageViews = filteredLogs.Count;
            var uniqueVisitors = filteredLogs.Select(l => l.SessionId).Distinct().Count();

            // Today metrics
            var todayLogs = _allLogs.Where(l => !l.SessionId.StartsWith("s_") && l.VisitedAt >= now.Date).ToList();
            var todayViews = todayLogs.Count;
            var todayUniques = todayLogs.Select(l => l.SessionId).Distinct().Count();

            // Real active sessions only (within 5 minutes)
            var liveOnlineCount = _activeSessions.Values
                .Count(s => !s.SessionId.StartsWith("s_") && (now - s.LastActivityAt).TotalMinutes <= 5);

            // Country Stats
            var countryStats = filteredLogs
                .GroupBy(l => new { l.Country, l.CountryCode, l.FlagEmoji })
                .Select(g => new CountryStatDto
                {
                    CountryName = g.Key.Country,
                    CountryCode = g.Key.CountryCode,
                    FlagEmoji = g.Key.FlagEmoji,
                    VisitCount = g.Count(),
                    Percentage = totalPageViews > 0 ? Math.Round((double)g.Count() / totalPageViews * 100, 1) : 0
                })
                .OrderByDescending(c => c.VisitCount)
                .Take(10)
                .ToList();

            // Device Stats
            var deviceStats = filteredLogs
                .GroupBy(l => l.DeviceType)
                .Select(g => new DeviceStatDto
                {
                    DeviceType = g.Key,
                    Count = g.Count(),
                    Percentage = totalPageViews > 0 ? Math.Round((double)g.Count() / totalPageViews * 100, 1) : 0,
                    Icon = g.Key == "Mobile" ? "fa-mobile-screen" : (g.Key == "Tablet" ? "fa-tablet-screen-button" : "fa-desktop"),
                    Color = g.Key == "Mobile" ? "#16a34a" : (g.Key == "Tablet" ? "#8b5cf6" : "#0284c7")
                })
                .OrderByDescending(d => d.Count)
                .ToList();

            // Browser Stats
            var browserStats = filteredLogs
                .GroupBy(l => l.Browser)
                .Select(g => new BrowserStatDto
                {
                    BrowserName = g.Key,
                    Count = g.Count(),
                    Percentage = totalPageViews > 0 ? Math.Round((double)g.Count() / totalPageViews * 100, 1) : 0,
                    Icon = g.Key.ToLower() switch
                    {
                        "edge" => "fa-brands fa-edge",
                        "safari" => "fa-brands fa-safari",
                        "firefox" => "fa-brands fa-firefox-browser",
                        _ => "fa-brands fa-chrome"
                    }
                })
                .OrderByDescending(b => b.Count)
                .Take(6)
                .ToList();

            // OS Stats
            var osStats = filteredLogs
                .GroupBy(l => l.OperatingSystem)
                .Select(g => new OsStatDto
                {
                    OsName = g.Key,
                    Count = g.Count(),
                    Percentage = totalPageViews > 0 ? Math.Round((double)g.Count() / totalPageViews * 100, 1) : 0,
                    Icon = g.Key.Contains("Windows") ? "fa-brands fa-windows" : (g.Key.Contains("Android") ? "fa-brands fa-android" : (g.Key.Contains("iOS") || g.Key.Contains("macOS") ? "fa-brands fa-apple" : "fa-brands fa-linux"))
                })
                .OrderByDescending(o => o.Count)
                .Take(6)
                .ToList();

            // Top Pages
            var topPages = filteredLogs
                .GroupBy(l => new { l.PageUrl, l.PageTitle })
                .Select(g => new PageVisitStatDto
                {
                    PageUrl = g.Key.PageUrl,
                    PageTitle = g.Key.PageTitle,
                    ViewsCount = g.Count(),
                    UniqueVisitorsCount = g.Select(x => x.SessionId).Distinct().Count(),
                    Percentage = totalPageViews > 0 ? Math.Round((double)g.Count() / totalPageViews * 100, 1) : 0
                })
                .OrderByDescending(p => p.ViewsCount)
                .Take(10)
                .ToList();

            // Live Recent Visitors Feed (Real sessions only)
            var recentVisitors = _activeSessions.Values
                .Where(s => !s.SessionId.StartsWith("s_"))
                .OrderByDescending(s => s.LastActivityAt)
                .Take(40)
                .Select(s => {
                    s.IsOnline = (now - s.LastActivityAt).TotalMinutes <= 5;
                    s.DurationSeconds = Math.Max(1, (int)(s.LastActivityAt - s.FirstSeenAt).TotalSeconds);
                    return s;
                })
                .ToList();

            return Task.FromResult(new VisitorAnalyticsDashboardViewModel
            {
                TimeRange = timeRange,
                LiveOnlineVisitorsCount = liveOnlineCount,
                TotalPageViews = totalPageViews,
                UniqueVisitorsCount = uniqueVisitors,
                TodayPageViews = todayViews,
                TodayUniqueVisitors = todayUniques,
                Countries = countryStats,
                Devices = deviceStats,
                Browsers = browserStats,
                OperatingSystems = osStats,
                TopPages = topPages,
                RecentVisitors = recentVisitors
            });
        }

        public Task<object> GetLiveTelemetryAsync()
        {
            var now = DateTime.UtcNow;
            var liveOnlineCount = _activeSessions.Values
                .Count(s => !s.SessionId.StartsWith("s_") && (now - s.LastActivityAt).TotalMinutes <= 5);

            var todayLogs = _allLogs.Where(l => !l.SessionId.StartsWith("s_") && l.VisitedAt >= now.Date).ToList();

            return Task.FromResult<object>(new
            {
                liveOnline = liveOnlineCount,
                todayViews = todayLogs.Count,
                todayUniques = todayLogs.Select(l => l.SessionId).Distinct().Count(),
                recentVisitors = _activeSessions.Values
                    .Where(s => !s.SessionId.StartsWith("s_"))
                    .OrderByDescending(s => s.LastActivityAt)
                    .Take(15)
                    .Select(s => new
                    {
                        s.SessionId,
                        s.IpAddress,
                        s.Country,
                        s.FlagEmoji,
                        s.City,
                        s.CurrentPage,
                        s.DeviceType,
                        s.Browser,
                        s.OperatingSystem,
                        isOnline = (now - s.LastActivityAt).TotalMinutes <= 5,
                        lastSeen = s.LastActivityAt.ToString("HH:mm:ss")
                    })
            });
        }

        private class IpApiResponse
        {
            public string? status { get; set; }
            public string? country { get; set; }
            public string? countryCode { get; set; }
            public string? city { get; set; }
        }
    }
}
