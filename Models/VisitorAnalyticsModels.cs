using System;
using System.Collections.Generic;

namespace NAU_Web.Models
{
    public class VisitorLog
    {
        public long Id { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public string IpAddress { get; set; } = "127.0.0.1";
        public string Country { get; set; } = "Pakistan";
        public string CountryCode { get; set; } = "PK";
        public string FlagEmoji { get; set; } = "🇵🇰";
        public string City { get; set; } = "Karachi";
        public string PageUrl { get; set; } = "/";
        public string PageTitle { get; set; } = "Home";
        public string Referrer { get; set; } = "Direct";
        public string UserAgent { get; set; } = string.Empty;
        public string Browser { get; set; } = "Chrome";
        public string OperatingSystem { get; set; } = "Windows";
        public string DeviceType { get; set; } = "Desktop"; // Desktop, Mobile, Tablet
        public bool IsRegisteredUser { get; set; } = false;
        public string? UserEmail { get; set; }
        public DateTime VisitedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    }

    public class CountryStatDto
    {
        public string CountryName { get; set; } = "Pakistan";
        public string CountryCode { get; set; } = "PK";
        public string FlagEmoji { get; set; } = "🇵🇰";
        public int VisitCount { get; set; }
        public double Percentage { get; set; }
    }

    public class DeviceStatDto
    {
        public string DeviceType { get; set; } = "Desktop";
        public int Count { get; set; }
        public double Percentage { get; set; }
        public string Icon { get; set; } = "fa-desktop";
        public string Color { get; set; } = "#0284c7";
    }

    public class BrowserStatDto
    {
        public string BrowserName { get; set; } = "Chrome";
        public int Count { get; set; }
        public double Percentage { get; set; }
        public string Icon { get; set; } = "fa-chrome";
    }

    public class OsStatDto
    {
        public string OsName { get; set; } = "Windows";
        public int Count { get; set; }
        public double Percentage { get; set; }
        public string Icon { get; set; } = "fa-windows";
    }

    public class PageVisitStatDto
    {
        public string PageUrl { get; set; } = "/";
        public string PageTitle { get; set; } = "Home";
        public int ViewsCount { get; set; }
        public int UniqueVisitorsCount { get; set; }
        public double Percentage { get; set; }
    }

    public class LiveVisitorSessionDto
    {
        public string SessionId { get; set; } = string.Empty;
        public string IpAddress { get; set; } = "127.0.0.1";
        public string Country { get; set; } = "Pakistan";
        public string CountryCode { get; set; } = "PK";
        public string FlagEmoji { get; set; } = "🇵🇰";
        public string City { get; set; } = "Karachi";
        public string CurrentPage { get; set; } = "/";
        public string PageTitle { get; set; } = "Home";
        public string DeviceType { get; set; } = "Desktop";
        public string Browser { get; set; } = "Chrome";
        public string OperatingSystem { get; set; } = "Windows";
        public bool IsOnline { get; set; } = true;
        public bool IsRegistered { get; set; } = false;
        public string? UserEmail { get; set; }
        public DateTime FirstSeenAt { get; set; } = DateTime.UtcNow;
        public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
        public int DurationSeconds { get; set; } = 0;
    }

    public class VisitorAnalyticsDashboardViewModel
    {
        public string TimeRange { get; set; } = "today"; // today, 7days, 30days, all
        public int LiveOnlineVisitorsCount { get; set; }
        public long TotalPageViews { get; set; }
        public long UniqueVisitorsCount { get; set; }
        public long TodayPageViews { get; set; }
        public long TodayUniqueVisitors { get; set; }
        public List<CountryStatDto> Countries { get; set; } = new();
        public List<DeviceStatDto> Devices { get; set; } = new();
        public List<BrowserStatDto> Browsers { get; set; } = new();
        public List<OsStatDto> OperatingSystems { get; set; } = new();
        public List<PageVisitStatDto> TopPages { get; set; } = new();
        public List<LiveVisitorSessionDto> RecentVisitors { get; set; } = new();
    }

    public class VisitorVisitInfo
    {
        public string IpAddress { get; set; } = "127.0.0.1";
        public string UserAgent { get; set; } = string.Empty;
        public string Referrer { get; set; } = "Direct Visit";
        public string SessionId { get; set; } = string.Empty;
        public bool IsRegistered { get; set; } = false;
        public string? UserEmail { get; set; }
        public string Path { get; set; } = "/";
        public string? PageTitle { get; set; }
    }
}

