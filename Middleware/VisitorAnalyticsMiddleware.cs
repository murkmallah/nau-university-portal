using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NAU_Web.Services;

namespace NAU_Web.Middleware
{
    public class VisitorAnalyticsMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly string[] _ignoredExtensions = new[]
        {
            ".css", ".js", ".png", ".jpg", ".jpeg", ".gif", ".webp", ".svg", ".ico",
            ".woff", ".woff2", ".ttf", ".eot", ".map", ".mp4", ".webm", ".json", ".xml"
        };

        public VisitorAnalyticsMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IVisitorAnalyticsService analyticsService)
        {
            var path = context.Request.Path.Value ?? "/";

            // Only track standard page GET requests
            if (HttpMethods.IsGet(context.Request.Method) && ShouldTrackPath(path))
            {
                try
                {
                    // Extract visitor info & set cookie synchronously on main request thread
                    var visitInfo = analyticsService.ExtractVisitInfo(context, path);

                    // Fire background tracking using thread-safe data snapshot (no HttpContext in background thread)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await analyticsService.TrackVisitInfoAsync(visitInfo);
                        }
                        catch { }
                    });
                }
                catch { }
            }

            await _next(context);
        }

        private static bool ShouldTrackPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;

            // Skip health checks, background polling, and internal API calls
            if (path.StartsWith("/health", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/Admin/LiveAnalyticsFeed", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/Admin/TriggerHealthDiagnostics", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Skip static file extensions
            var ext = Path.GetExtension(path);
            if (!string.IsNullOrEmpty(ext))
            {
                foreach (var ignored in _ignoredExtensions)
                {
                    if (ext.Equals(ignored, StringComparison.OrdinalIgnoreCase))
                        return false;
                }
            }

            return true;
        }
    }
}
