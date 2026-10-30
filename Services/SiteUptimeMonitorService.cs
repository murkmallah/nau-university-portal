using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NAU_Web.Models;

namespace NAU_Web.Services
{
    public interface ISiteUptimeMonitorService
    {
        Task<HealthDashboardViewModel> GetDashboardDataAsync(int currentUserId = 0);
        Task<SiteHealthSetting> GetSettingAsync();
        Task UpdateSettingAsync(UpdateHealthSettingDto dto);
        Task<bool> SendTestAlertAsync(string? targetEmail = null, int currentUserId = 0);
        Task<HealthDashboardViewModel> RunDiagnosticsNowAsync(int currentUserId = 0);
        bool IsCurrentlyHealthy();
    }

    public class SiteUptimeMonitorService : BackgroundService, ISiteUptimeMonitorService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SiteUptimeMonitorService> _logger;
        private static readonly DateTime _serverStartTime = DateTime.UtcNow;
        private static readonly ConcurrentQueue<IncidentLogDto> _incidentLogs = new();
        private static SiteHealthSetting _setting = new();
        private static readonly object _fileLock = new();
        private readonly string _settingFilePath;

        public SiteUptimeMonitorService(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILogger<SiteUptimeMonitorService> logger)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;
            _settingFilePath = Path.Combine(AppContext.BaseDirectory, "health_settings.json");

            LoadSetting();
            RecordIncident("Operational", "Monitoring Service Started", "Website uptime monitoring and automated admin outage alerting initialized.", false);
        }

        private void LoadSetting()
        {
            try
            {
                if (File.Exists(_settingFilePath))
                {
                    lock (_fileLock)
                    {
                        var json = File.ReadAllText(_settingFilePath);
                        var loaded = JsonSerializer.Deserialize<SiteHealthSetting>(json);
                        if (loaded != null)
                        {
                            _setting = loaded;
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load health settings file, using defaults.");
            }

            // Default fallback
            var defaultTo = _configuration["Smtp:ToEmail"] ?? _configuration["Resend:ToEmail"] ?? "mallahmurk06@gmail.com";
            if (_setting.ConfiguredAlertEmails == null || !_setting.ConfiguredAlertEmails.Any())
            {
                _setting.ConfiguredAlertEmails = new List<string> { defaultTo };
            }
        }

        private void SaveSetting()
        {
            try
            {
                lock (_fileLock)
                {
                    var json = JsonSerializer.Serialize(_setting, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(_settingFilePath, json);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save health settings file.");
            }
        }

        public Task<SiteHealthSetting> GetSettingAsync()
        {
            return Task.FromResult(_setting);
        }

        public Task UpdateSettingAsync(UpdateHealthSettingDto dto)
        {
            _setting.ConfiguredAlertEmails = dto.ConfiguredAlertEmails?
                .Where(e => !string.IsNullOrWhiteSpace(e) && e.Contains('@'))
                .Select(e => e.Trim().ToLower())
                .Distinct()
                .ToList() ?? new List<string>();

            _setting.AlertsEnabled = dto.AlertsEnabled;
            _setting.WebhookUrl = string.IsNullOrWhiteSpace(dto.WebhookUrl) ? null : dto.WebhookUrl.Trim();
            _setting.CheckIntervalSeconds = dto.CheckIntervalSeconds > 10 ? dto.CheckIntervalSeconds : 60;
            SaveSetting();
            return Task.CompletedTask;
        }

        public bool IsCurrentlyHealthy()
        {
            return !_setting.IsCurrentlyDown;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Site Uptime & Downtime Monitor Service running.");

            // Allow application startup to stabilize
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PerformHealthCheckAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error inside SiteUptimeMonitorService health check cycle.");
                }

                var delaySec = _setting.CheckIntervalSeconds > 10 ? _setting.CheckIntervalSeconds : 60;
                await Task.Delay(TimeSpan.FromSeconds(delaySec), stoppingToken);
            }
        }

        private async Task PerformHealthCheckAsync()
        {
            bool isDbOk = false;
            long latencyMs = 0;
            string errorDetails = string.Empty;

            var sw = Stopwatch.StartNew();
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<NauDbContext>();

                // Fast database connectivity & query check
                var canConnect = await db.Database.CanConnectAsync();
                if (canConnect)
                {
                    var count = await db.Database.ExecuteSqlRawAsync("SELECT 1");
                    sw.Stop();
                    latencyMs = sw.ElapsedMilliseconds;
                    isDbOk = true;
                }
                else
                {
                    sw.Stop();
                    errorDetails = "Database.CanConnect returned false (Unable to reach SQL Server instance).";
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                latencyMs = sw.ElapsedMilliseconds;
                isDbOk = false;
                errorDetails = ex.Message;
            }

            // Evaluate state
            if (!isDbOk)
            {
                _setting.ConsecutiveFailuresCount++;
                _setting.LastCheckStatus = "Unhealthy";
                _setting.LastCheckMessage = $"Database unreachable: {errorDetails}";

                _logger.LogWarning("Health check failed ({Count}/{Threshold}): {Msg}", 
                    _setting.ConsecutiveFailuresCount, _setting.ConsecutiveFailuresThreshold, errorDetails);

                // If threshold reached and not yet marked down -> Trigger Outage Alert
                if (_setting.ConsecutiveFailuresCount >= _setting.ConsecutiveFailuresThreshold && !_setting.IsCurrentlyDown)
                {
                    _setting.IsCurrentlyDown = true;
                    _setting.LastAlertSentAt = DateTime.UtcNow;
                    SaveSetting();

                    RecordIncident("Down", "CRITICAL OUTAGE DETECTED", $"Database and services unreachable: {errorDetails}", true);

                    if (_setting.AlertsEnabled)
                    {
                        await SendOutageAlertEmailAsync(errorDetails);
                    }
                }
            }
            else
            {
                // System is healthy
                _setting.LastCheckStatus = latencyMs > 800 ? "Degraded" : "Healthy";
                _setting.LastCheckMessage = $"All systems operational. DB latency: {latencyMs}ms.";

                // If recovering from a previous outage -> Trigger Recovery Notification
                if (_setting.IsCurrentlyDown)
                {
                    var outageDuration = _setting.LastAlertSentAt.HasValue
                        ? DateTime.UtcNow - _setting.LastAlertSentAt.Value
                        : TimeSpan.Zero;

                    _setting.IsCurrentlyDown = false;
                    _setting.ConsecutiveFailuresCount = 0;
                    _setting.LastRecoveredAt = DateTime.UtcNow;
                    SaveSetting();

                    RecordIncident("Operational", "SYSTEM RECOVERED & RESTORED", $"Website is back online and operational. DB latency: {latencyMs}ms. Outage duration: {FormatTimeSpan(outageDuration)}", true);

                    if (_setting.AlertsEnabled)
                    {
                        await SendRecoveryAlertEmailAsync(outageDuration, latencyMs);
                    }
                }
                else
                {
                    _setting.ConsecutiveFailuresCount = 0;
                }
            }
        }

        public async Task<HealthDashboardViewModel> RunDiagnosticsNowAsync(int currentUserId = 0)
        {
            await PerformHealthCheckAsync();
            return await GetDashboardDataAsync(currentUserId);
        }

        public async Task<HealthDashboardViewModel> GetDashboardDataAsync(int currentUserId = 0)
        {
            bool isDbOk = false;
            long latencyMs = 0;

            var sw = Stopwatch.StartNew();
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<NauDbContext>();

                if (await db.Database.CanConnectAsync())
                {
                    await db.Database.ExecuteSqlRawAsync("SELECT 1");
                    sw.Stop();
                    latencyMs = sw.ElapsedMilliseconds;
                    isDbOk = true;
                }
            }
            catch
            {
                sw.Stop();
                latencyMs = sw.ElapsedMilliseconds;
                isDbOk = false;
            }

            var status = !isDbOk ? "Down" : (latencyMs > 800 ? "Degraded" : "Operational");

            // Process memory
            var proc = Process.GetCurrentProcess();
            double memoryMb = Math.Round((double)proc.WorkingSet64 / (1024 * 1024), 1);

            // Disk space
            double freeGb = 0, totalGb = 0;
            try
            {
                var appDrive = Path.GetPathRoot(AppContext.BaseDirectory);
                if (!string.IsNullOrEmpty(appDrive))
                {
                    var drive = new DriveInfo(appDrive);
                    if (drive.IsReady)
                    {
                        freeGb = Math.Round((double)drive.AvailableFreeSpace / (1024 * 1024 * 1024), 1);
                        totalGb = Math.Round((double)drive.TotalSize / (1024 * 1024 * 1024), 1);
                    }
                }
            }
            catch { }

            return new HealthDashboardViewModel
            {
                Setting = _setting,
                Status = status,
                DatabaseHealthy = isDbOk,
                DatabaseLatencyMs = latencyMs,
                DatabaseName = "NAU_CMS",
                ServerStartTime = _serverStartTime,
                ServerUptime = DateTime.UtcNow - _serverStartTime,
                MemoryUsageMb = memoryMb,
                DiskFreeGb = freeGb,
                DiskTotalGb = totalGb,
                EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                HostName = Environment.MachineName,
                DotNetVersion = RuntimeInformation.FrameworkDescription,
                RecentIncidents = _incidentLogs.OrderByDescending(i => i.Timestamp).Take(25).ToList()
            };
        }

        private Task<List<string>> ResolveAllRecipientEmailsAsync()
        {
            var list = _setting.ConfiguredAlertEmails?
                .Where(e => !string.IsNullOrWhiteSpace(e) && e.Contains('@'))
                .Select(e => e.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();

            if (!list.Any())
            {
                var fallback = _configuration["Smtp:ToEmail"] ?? _configuration["Resend:ToEmail"] ?? "mallahmurk06@gmail.com";
                list.Add(fallback);
            }

            return Task.FromResult(list);
        }

        private void RecordIncident(string status, string title, string details, bool alertSent)
        {
            var item = new IncidentLogDto
            {
                Timestamp = DateTime.UtcNow,
                Status = status,
                Title = title,
                Details = details,
                IsAlertSent = alertSent
            };
            _incidentLogs.Enqueue(item);

            while (_incidentLogs.Count > 50)
            {
                _incidentLogs.TryDequeue(out _);
            }
        }

        public async Task<bool> SendTestAlertAsync(string? targetEmail = null, int currentUserId = 0)
        {
            var recipients = new List<string>();
            if (!string.IsNullOrWhiteSpace(targetEmail))
            {
                recipients.Add(targetEmail.Trim());
            }
            else
            {
                recipients = await ResolveAllRecipientEmailsAsync();
            }

            if (!recipients.Any()) return false;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

                var subject = "🔔 [TEST NOTIFICATION] NAU Website Downtime & Health Alert System";
                var recipientListHtml = string.Join(", ", recipients.Select(r => $"<strong>{r}</strong>"));

                var body = $@"
<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8'>
<style>
  body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background:#f4f6f9; margin:0; padding:20px; color:#333; }}
  .box {{ max-width:600px; margin:0 auto; background:#ffffff; border-radius:12px; overflow:hidden; box-shadow:0 4px 15px rgba(0,0,0,0.08); }}
  .hdr {{ background:linear-gradient(135deg, #0b2c4d, #1a5fa6); color:#fff; padding:24px; text-align:center; }}
  .hdr h1 {{ margin:0; font-size:20px; }}
  .cnt {{ padding:28px; font-size:14px; line-height:1.6; color:#444; }}
  .badge {{ display:inline-block; padding:6px 14px; border-radius:20px; font-weight:700; font-size:12px; background:#e0f2fe; color:#0284c7; margin-bottom:15px; }}
  .tbl {{ width:100%; border-collapse:collapse; margin:20px 0; font-size:13px; }}
  .tbl td {{ padding:10px 14px; border-bottom:1px solid #edf2f7; }}
  .tbl td:first-child {{ font-weight:600; color:#0b2c4d; width:35%; }}
  .ftr {{ background:#f8fafc; padding:16px 24px; text-align:center; font-size:12px; color:#94a3b8; border-top:1px solid #edf2f7; }}
</style>
</head>
<body>
  <div class='box'>
    <div class='hdr'>
      <h1>National Aviation University</h1>
      <p style='margin:4px 0 0; opacity:0.8; font-size:12px;'>Automated Uptime &amp; Health Monitoring System</p>
    </div>
    <div class='cnt'>
      <span class='badge'>AUTOMATIC ALERT VERIFIED</span>
      <h2 style='margin-top:0; color:#0b2c4d; font-size:18px;'>Health Monitoring Alert System is Working!</h2>
      <p>This is an automated test notification confirming that the <strong>Downtime &amp; Outage Alert System</strong> for National Aviation University (NAU) is fully active and automatically configured to alert all Administrators.</p>
      
      <table class='tbl'>
        <tr><td>Recipient(s):</td><td>{recipientListHtml}</td></tr>
        <tr><td>Dispatch Mode:</td><td><strong style='color:#16a34a;'>Automatic (All Registered Admins)</strong></td></tr>
        <tr><td>Server Host:</td><td>{Environment.MachineName}</td></tr>
        <tr><td>Monitoring Frequency:</td><td>Checking every {_setting.CheckIntervalSeconds} seconds</td></tr>
        <tr><td>Timestamp (UTC):</td><td>{DateTime.UtcNow:dd MMM yyyy, hh:mm:ss tt}</td></tr>
      </table>

      <p style='margin-top:15px;'>If the website or database goes down in production, an emergency outage notification will be automatically dispatched to all administrators immediately.</p>
    </div>
    <div class='ftr'>
      &copy; {DateTime.UtcNow.Year} National Aviation University &bull; Automated Health Monitor
    </div>
  </div>
</body>
</html>";

                foreach (var recipient in recipients)
                {
                    await emailSender.SendEmailAsync(subject, body, toEmailOverride: recipient);
                }

                RecordIncident("Operational", "Test Alert Sent", $"Test notification email automatically dispatched to: {string.Join(", ", recipients)}.", true);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send test alert email.");
                return false;
            }
        }

        private async Task SendOutageAlertEmailAsync(string errorDetails)
        {
            var recipients = await ResolveAllRecipientEmailsAsync();
            if (!recipients.Any()) return;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

                var subject = "🚨 [CRITICAL ALERT] National Aviation University Website / Database Down!";
                var body = $@"
<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8'>
<style>
  body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background:#fee2e2; margin:0; padding:20px; color:#333; }}
  .box {{ max-width:600px; margin:0 auto; background:#ffffff; border-radius:12px; overflow:hidden; box-shadow:0 8px 25px rgba(220,38,38,0.2); border-top:5px solid #dc2626; }}
  .hdr {{ background:#dc2626; color:#fff; padding:24px; text-align:center; }}
  .hdr h1 {{ margin:0; font-size:22px; font-weight:800; }}
  .cnt {{ padding:28px; font-size:14px; line-height:1.6; color:#334155; }}
  .badge {{ display:inline-block; padding:6px 14px; border-radius:20px; font-weight:700; font-size:12px; background:#fee2e2; color:#dc2626; margin-bottom:15px; text-transform:uppercase; }}
  .error-box {{ background:#fef2f2; border:1px solid #fecaca; border-radius:8px; padding:14px; font-family:monospace; font-size:12.5px; color:#991b1b; margin:16px 0; word-break:break-all; }}
  .tbl {{ width:100%; border-collapse:collapse; margin:20px 0; font-size:13px; }}
  .tbl td {{ padding:10px 14px; border-bottom:1px solid #edf2f7; }}
  .tbl td:first-child {{ font-weight:600; color:#0b2c4d; width:35%; }}
  .ftr {{ background:#f8fafc; padding:16px 24px; text-align:center; font-size:12px; color:#94a3b8; border-top:1px solid #edf2f7; }}
</style>
</head>
<body>
  <div class='box'>
    <div class='hdr'>
      <h1>🚨 WEBSITE OUTAGE DETECTED</h1>
      <p style='margin:4px 0 0; opacity:0.9; font-size:13px;'>National Aviation University (NAU) CMS</p>
    </div>
    <div class='cnt'>
      <span class='badge'>CRITICAL SERVICE OUTAGE</span>
      <h2 style='margin-top:0; color:#dc2626; font-size:18px;'>The NAU Website / Database is Currently DOWN</h2>
      <p>The automated health monitor has detected consecutive connection failures to the core database or application pipeline. The website may be returning 500/503 errors to visitors.</p>
      
      <div class='error-box'>
        <strong>Error Details:</strong><br>
        {errorDetails}
      </div>

      <table class='tbl'>
        <tr><td>Target Server:</td><td><strong>{Environment.MachineName}</strong></td></tr>
        <tr><td>Outage Detected At (UTC):</td><td><strong>{DateTime.UtcNow:dd MMM yyyy, hh:mm:ss tt}</strong></td></tr>
        <tr><td>Impact:</td><td><strong style='color:#dc2626;'>Database &amp; Web Services Offline</strong></td></tr>
      </table>

      <h3 style='color:#0b2c4d; font-size:14px; margin-top:20px;'>Recommended Admin Actions:</h3>
      <ol style='padding-left:20px; font-size:13px; color:#475569;'>
        <li>Verify if the SQL Server database service (<code>MSSQLSERVER</code> or <code>SQLEXPRESS</code>) is running.</li>
        <li>Check web server (IIS / Kestrel) application pool status and memory usage.</li>
        <li>Check server network connectivity and firewall status.</li>
      </ol>
      <p style='font-size:12.5px; color:#64748b;'>A follow-up recovery email will be automatically dispatched as soon as the service is restored online.</p>
    </div>
    <div class='ftr'>
      &copy; {DateTime.UtcNow.Year} National Aviation University &bull; Automated Health Monitor
    </div>
  </div>
</body>
</html>";

                foreach (var recipient in recipients)
                {
                    await emailSender.SendEmailAsync(subject, body, toEmailOverride: recipient);
                }

                _logger.LogInformation("Outage alert email automatically dispatched to {Count} recipients: {List}", 
                    recipients.Count, string.Join(", ", recipients));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send outage alert email.");
            }
        }

        private async Task SendRecoveryAlertEmailAsync(TimeSpan outageDuration, long latencyMs)
        {
            var recipients = await ResolveAllRecipientEmailsAsync();
            if (!recipients.Any()) return;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

                var subject = "✅ [RESOLVED] National Aviation University Website Restored Online";
                var body = $@"
<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8'>
<style>
  body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background:#f0fdf4; margin:0; padding:20px; color:#333; }}
  .box {{ max-width:600px; margin:0 auto; background:#ffffff; border-radius:12px; overflow:hidden; box-shadow:0 8px 25px rgba(220,38,38,0.2); border-top:5px solid #16a34a; }}
  .hdr {{ background:#16a34a; color:#fff; padding:24px; text-align:center; }}
  .hdr h1 {{ margin:0; font-size:22px; font-weight:800; }}
  .cnt {{ padding:28px; font-size:14px; line-height:1.6; color:#334155; }}
  .badge {{ display:inline-block; padding:6px 14px; border-radius:20px; font-weight:700; font-size:12px; background:#dcfce7; color:#16a34a; margin-bottom:15px; text-transform:uppercase; }}
  .tbl {{ width:100%; border-collapse:collapse; margin:20px 0; font-size:13px; }}
  .tbl td {{ padding:10px 14px; border-bottom:1px solid #edf2f7; }}
  .tbl td:first-child {{ font-weight:600; color:#0b2c4d; width:35%; }}
  .ftr {{ background:#f8fafc; padding:16px 24px; text-align:center; font-size:12px; color:#94a3b8; border-top:1px solid #edf2f7; }}
</style>
</head>
<body>
  <div class='box'>
    <div class='hdr'>
      <h1>✅ SYSTEM RESTORED ONLINE</h1>
      <p style='margin:4px 0 0; opacity:0.9; font-size:13px;'>National Aviation University (NAU) CMS</p>
    </div>
    <div class='cnt'>
      <span class='badge'>OUTAGE RESOLVED</span>
      <h2 style='margin-top:0; color:#16a34a; font-size:18px;'>Website &amp; Database Are Fully Operational</h2>
      <p>The automated health monitor has verified that the website and database have successfully recovered and are responding normally.</p>
      
      <table class='tbl'>
        <tr><td>Server Host:</td><td><strong>{Environment.MachineName}</strong></td></tr>
        <tr><td>Recovered At (UTC):</td><td><strong>{DateTime.UtcNow:dd MMM yyyy, hh:mm:ss tt}</strong></td></tr>
        <tr><td>Total Outage Duration:</td><td><strong style='color:#16a34a;'>{FormatTimeSpan(outageDuration)}</strong></td></tr>
        <tr><td>Database Latency:</td><td>{latencyMs} ms</td></tr>
        <tr><td>Current Status:</td><td><span style='color:#16a34a; font-weight:bold;'>100% Operational</span></td></tr>
      </table>

      <p style='font-size:13px; color:#475569; margin-top:15px;'>No further administrative action is required. Continuous background health monitoring remains active.</p>
    </div>
    <div class='ftr'>
      &copy; {DateTime.UtcNow.Year} National Aviation University &bull; Automated Health Monitor
    </div>
  </div>
</body>
</html>";

                foreach (var recipient in recipients)
                {
                    await emailSender.SendEmailAsync(subject, body, toEmailOverride: recipient);
                }

                _logger.LogInformation("Recovery alert email automatically sent to {Count} recipients.", recipients.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send recovery alert email.");
            }
        }

        private static string FormatTimeSpan(TimeSpan ts)
        {
            if (ts.TotalDays >= 1)
                return $"{(int)ts.TotalDays}d {ts.Hours}h {ts.Minutes}m";
            if (ts.TotalHours >= 1)
                return $"{ts.Hours}h {ts.Minutes}m {ts.Seconds}s";
            if (ts.TotalMinutes >= 1)
                return $"{ts.Minutes}m {ts.Seconds}s";
            return $"{Math.Max(1, ts.Seconds)} seconds";
        }
    }
}
