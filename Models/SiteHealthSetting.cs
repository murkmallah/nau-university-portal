using System;
using System.Collections.Generic;

namespace NAU_Web.Models
{
    public class SiteHealthSetting
    {
        public List<string> ConfiguredAlertEmails { get; set; } = new() { "mallahmurk06@gmail.com" };
        public bool AlertsEnabled { get; set; } = true;
        public string? WebhookUrl { get; set; }
        public int CheckIntervalSeconds { get; set; } = 60;
        public int ConsecutiveFailuresThreshold { get; set; } = 2;
        public DateTime? LastAlertSentAt { get; set; }
        public DateTime? LastRecoveredAt { get; set; }
        public bool IsCurrentlyDown { get; set; } = false;
        public int ConsecutiveFailuresCount { get; set; } = 0;
        public string LastCheckStatus { get; set; } = "Healthy";
        public string LastCheckMessage { get; set; } = "All systems operational.";
    }

    public class IncidentLogDto
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Healthy";
        public string Title { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public bool IsAlertSent { get; set; } = false;
    }

    public class HealthDashboardViewModel
    {
        public SiteHealthSetting Setting { get; set; } = new();
        public string Status { get; set; } = "Operational"; // Operational, Degraded, Down
        public bool DatabaseHealthy { get; set; } = true;
        public long DatabaseLatencyMs { get; set; } = 0;
        public string DatabaseName { get; set; } = "NAU_CMS";
        public TimeSpan ServerUptime { get; set; }
        public DateTime ServerStartTime { get; set; }
        public double MemoryUsageMb { get; set; }
        public double DiskFreeGb { get; set; }
        public double DiskTotalGb { get; set; }
        public string EnvironmentName { get; set; } = "Production";
        public string HostName { get; set; } = "localhost";
        public string DotNetVersion { get; set; } = ".NET 8.0";
        public List<IncidentLogDto> RecentIncidents { get; set; } = new();
    }

    public class UpdateHealthSettingDto
    {
        public List<string>? ConfiguredAlertEmails { get; set; }
        public bool AlertsEnabled { get; set; } = true;
        public string? WebhookUrl { get; set; }
        public int CheckIntervalSeconds { get; set; } = 60;
    }

    public class SendTestAlertDto
    {
        public string? TargetEmail { get; set; }
    }
}
