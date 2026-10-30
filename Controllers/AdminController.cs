using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NAU_Web.Models;
using NAU_Web.Services;

namespace NAU_Web.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class AdminController : Controller
    {
        private readonly INavMenuService _navMenuService;
        private readonly IAnnouncementService _announcementService;
        private readonly ISiteBrandingService _brandingService;
        private readonly IFooterService _footerService;
        private readonly ISchoolService _schoolService;
        private readonly ISiteUptimeMonitorService _uptimeMonitor;
        private readonly IVisitorAnalyticsService _analyticsService;
        private readonly IUserPermissionService _permissionService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly NauDbContext _context;

        public AdminController(
            INavMenuService navMenuService,
            IAnnouncementService announcementService,
            ISiteBrandingService brandingService,
            IFooterService footerService,
            ISchoolService schoolService,
            ISiteUptimeMonitorService uptimeMonitor,
            IVisitorAnalyticsService analyticsService,
            IUserPermissionService permissionService,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            NauDbContext context)
        {
            _navMenuService = navMenuService;
            _announcementService = announcementService;
            _brandingService = brandingService;
            _footerService = footerService;
            _schoolService = schoolService;
            _uptimeMonitor = uptimeMonitor;
            _analyticsService = analyticsService;
            _permissionService = permissionService;
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var isSuperAdmin = await _permissionService.IsSuperAdminAsync(currentUser.Id);
            var permissions = await _permissionService.GetUserPermissionsAsync(currentUser.Id);

            ViewBag.IsSuperAdmin = isSuperAdmin;
            ViewBag.UserPermissions = permissions;
            ViewBag.CurrentUserName = !string.IsNullOrWhiteSpace(currentUser.FullName) ? currentUser.FullName : (currentUser.UserName ?? "Admin");

            return View();
        }

        #region User & Permission Management

        [HttpGet]
        public async Task<IActionResult> Users()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            // Only Super Admin or users with Admin.ManageUsers permission can access User Management
            if (!await _permissionService.HasPermissionAsync(currentUser.Id, AppPermissions.AdminManageUsers))
            {
                return RedirectToAction("Index");
            }

            var currentUserId = currentUser.Id;
            currentUser.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(currentUser);

            var users = await _userManager.Users
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            // Populate LastLoginAt from actual user activity if untracked
            bool anyUpdated = false;
            foreach (var u in users)
            {
                if (!u.LastLoginAt.HasValue)
                {
                    if (u.Id == currentUserId)
                    {
                        u.LastLoginAt = DateTime.UtcNow;
                        anyUpdated = true;
                    }
                    else
                    {
                        var app = await _context.Applications.FirstOrDefaultAsync(a => a.UserId == u.Id);
                        if (app != null && app.SubmittedAt > DateTime.MinValue)
                        {
                            u.LastLoginAt = app.SubmittedAt;
                            anyUpdated = true;
                        }
                        else if (u.EmailConfirmed)
                        {
                            u.LastLoginAt = u.CreatedAt;
                            anyUpdated = true;
                        }
                    }
                }
            }

            if (anyUpdated)
            {
                await _context.SaveChangesAsync();
            }

            var userList = new List<AdminUserItemViewModel>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var perms = await _permissionService.GetUserPermissionsAsync(user.Id);

                userList.Add(new AdminUserItemViewModel
                {
                    UserId = user.Id,
                    FullName = !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : (user.UserName ?? "User"),
                    UserName = user.UserName ?? "",
                    Email = user.Email ?? "",
                    PhoneNumber = user.PhoneNumber ?? "",
                    EmailConfirmed = user.EmailConfirmed,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt,
                    Roles = roles.ToList(),
                    Permissions = perms,
                    IsCurrentAdmin = user.Id == currentUserId
                });
            }

            ViewBag.PermissionGroups = _permissionService.GetAllPermissionGroups();
            return View(userList);
        }

        [HttpGet]
        public async Task<IActionResult> GetUserPermissions(int userId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || !await _permissionService.HasPermissionAsync(currentUser.Id, AppPermissions.AdminManageUsers))
            {
                return Json(new { success = false, message = "Unauthorized." });
            }

            var targetUser = await _userManager.FindByIdAsync(userId.ToString());
            if (targetUser == null) return Json(new { success = false, message = "User not found." });

            var isSuperAdmin = await _permissionService.IsSuperAdminAsync(userId);
            var permissions = await _permissionService.GetUserPermissionsAsync(userId);
            var allGroups = _permissionService.GetAllPermissionGroups();

            return Json(new
            {
                success = true,
                userId,
                fullName = targetUser.FullName,
                email = targetUser.Email,
                isSuperAdmin,
                permissions,
                allGroups
            });
        }

        [HttpPost]
        public async Task<IActionResult> SaveUserPermissions([FromBody] SaveUserPermissionsDto dto)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || !await _permissionService.HasPermissionAsync(currentUser.Id, AppPermissions.AdminManageUsers))
            {
                return Json(new { success = false, message = "Unauthorized access." });
            }

            if (dto == null || dto.UserId <= 0)
            {
                return Json(new { success = false, message = "Invalid data payload." });
            }

            var targetUser = await _userManager.FindByIdAsync(dto.UserId.ToString());
            if (targetUser == null)
            {
                return Json(new { success = false, message = "Target user was not found." });
            }

            // If granting Super Admin via preset or full access key
            if (dto.Permissions.Contains(AppPermissions.AdminFullAccess) ||
                string.Equals(dto.PresetRole, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
            {
                if (!await _roleManager.RoleExistsAsync("Admin"))
                {
                    await _roleManager.CreateAsync(new ApplicationRole { Name = "Admin" });
                }

                if (!await _userManager.IsInRoleAsync(targetUser, "Admin"))
                {
                    await _userManager.AddToRoleAsync(targetUser, "Admin");
                }
            }
            else
            {
                // If removing super admin from someone else (cannot remove from self if current logged in)
                if (currentUser.Id != targetUser.Id && await _userManager.IsInRoleAsync(targetUser, "Admin"))
                {
                    await _userManager.RemoveFromRoleAsync(targetUser, "Admin");
                }
            }

            var ok = await _permissionService.SetUserPermissionsAsync(dto.UserId, dto.Permissions, currentUser.Id);
            if (ok)
            {
                return Json(new
                {
                    success = true,
                    message = $"Permissions for '{targetUser.FullName} ({targetUser.Email})' have been updated successfully!",
                    permissionCount = dto.Permissions.Count
                });
            }

            return Json(new { success = false, message = "Failed to update user permissions." });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser([FromBody] DeleteUserDto dto)
        {
            if (dto == null || dto.UserId <= 0)
                return Json(new { success = false, message = "Invalid user ID." });

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null && currentUser.Id == dto.UserId)
            {
                return Json(new { success = false, message = "You cannot delete your own currently logged-in account." });
            }

            var targetUser = await _userManager.FindByIdAsync(dto.UserId.ToString());
            if (targetUser == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            // Remove any related applications first to ensure clean cascade
            var relatedApps = await _context.Applications.Where(a => a.UserId == dto.UserId).ToListAsync();
            if (relatedApps.Any())
            {
                _context.Applications.RemoveRange(relatedApps);
                await _context.SaveChangesAsync();
            }

            var result = await _userManager.DeleteAsync(targetUser);
            if (result.Succeeded)
            {
                return Json(new { success = true, message = $"User '{targetUser.Email}' has been successfully deleted." });
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Json(new { success = false, message = $"Failed to delete user: {errors}" });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus([FromBody] ToggleUserStatusDto dto)
        {
            if (dto == null || dto.UserId <= 0)
                return Json(new { success = false, message = "Invalid user ID." });

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null && currentUser.Id == dto.UserId)
            {
                return Json(new { success = false, message = "You cannot deactivate your own currently logged-in account." });
            }

            var targetUser = await _userManager.FindByIdAsync(dto.UserId.ToString());
            if (targetUser == null)
                return Json(new { success = false, message = "User not found." });

            targetUser.IsActive = !targetUser.IsActive;
            var result = await _userManager.UpdateAsync(targetUser);
            if (result.Succeeded)
            {
                return Json(new { 
                    success = true, 
                    isActive = targetUser.IsActive, 
                    message = $"User status updated to {(targetUser.IsActive ? "Active" : "Inactive")}." 
                });
            }
            return Json(new { success = false, message = "Failed to update user status." });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleUserRole([FromBody] ToggleUserRoleDto dto)
        {
            if (dto == null || dto.UserId <= 0 || string.IsNullOrWhiteSpace(dto.Role))
                return Json(new { success = false, message = "Invalid data." });

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null && currentUser.Id == dto.UserId && dto.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                return Json(new { success = false, message = "You cannot remove Admin role from yourself." });
            }

            var targetUser = await _userManager.FindByIdAsync(dto.UserId.ToString());
            if (targetUser == null)
                return Json(new { success = false, message = "User not found." });

            if (await _userManager.IsInRoleAsync(targetUser, dto.Role))
            {
                await _userManager.RemoveFromRoleAsync(targetUser, dto.Role);
                return Json(new { success = true, isAssigned = false, message = $"Role '{dto.Role}' removed from {targetUser.Email}." });
            }
            else
            {
                if (!await _roleManager.RoleExistsAsync(dto.Role))
                {
                    await _roleManager.CreateAsync(new ApplicationRole { Name = dto.Role });
                }
                await _userManager.AddToRoleAsync(targetUser, dto.Role);
                return Json(new { success = true, isAssigned = true, message = $"Role '{dto.Role}' assigned to {targetUser.Email}." });
            }
        }

        #endregion

        #region Navigation & Announcements

        [HttpGet]
        public async Task<IActionResult> Navigation()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || !await _permissionService.HasPermissionAsync(currentUser.Id, AppPermissions.AdminManageSettings))
            {
                return RedirectToAction("Index");
            }

            var settings = await _navMenuService.GetAllSettingsAsync();
            return View(settings);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleNavMenu([FromBody] ToggleNavMenuDto dto)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || !await _permissionService.HasPermissionAsync(currentUser.Id, AppPermissions.AdminManageSettings))
            {
                return Json(new { success = false, message = "Unauthorized." });
            }

            if (dto == null || string.IsNullOrWhiteSpace(dto.MenuKey))
            {
                return Json(new { success = false, message = "Invalid menu key." });
            }

            var isHidden = await _navMenuService.ToggleVisibilityAsync(dto.MenuKey, dto.IsHidden);
            return Json(new { success = true, menuKey = dto.MenuKey, isHidden });
        }

        [HttpPost]
        public async Task<IActionResult> ResetNavMenu()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || !await _permissionService.HasPermissionAsync(currentUser.Id, AppPermissions.AdminManageSettings))
            {
                return Json(new { success = false, message = "Unauthorized." });
            }

            await _navMenuService.ResetDefaultsAsync();
            return Json(new { success = true, message = "All menus and columns have been reset to visible." });
        }

        [HttpGet]
        public async Task<IActionResult> Announcement()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || !await _permissionService.HasPermissionAsync(currentUser.Id, AppPermissions.AdminManageSettings))
            {
                return RedirectToAction("Index");
            }

            var setting = await _announcementService.GetSettingAsync();
            return View(setting);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAnnouncement([FromBody] UpdateAnnouncementDto dto)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || !await _permissionService.HasPermissionAsync(currentUser.Id, AppPermissions.AdminManageSettings))
            {
                return Json(new { success = false, message = "Unauthorized." });
            }

            if (dto == null) return Json(new { success = false, message = "Invalid data." });

            await _announcementService.UpdateSettingAsync(dto.TagText, dto.MessageText, dto.LinkUrl, dto.IsVisible);
            return Json(new { success = true, message = "Announcement bar updated across all pages!" });
        }

        [HttpGet]
        public async Task<IActionResult> Branding()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || !await _permissionService.HasPermissionAsync(currentUser.Id, AppPermissions.AdminManageSettings))
            {
                return RedirectToAction("Index");
            }

            var setting = await _brandingService.GetSettingAsync();
            return View(setting);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateBranding([FromBody] UpdateBrandingDto dto)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || !await _permissionService.HasPermissionAsync(currentUser.Id, AppPermissions.AdminManageSettings))
            {
                return Json(new { success = false, message = "Unauthorized." });
            }

            if (dto == null) return Json(new { success = false, message = "Invalid data." });

            await _brandingService.UpdateSettingAsync(dto.SiteTitle, dto.Tagline, dto.LogoUrl, dto.DarkModeLogoUrl);
            return Json(new { success = true, message = "Site branding and logo updated across all pages!" });
        }

        [HttpPost]
        public async Task<IActionResult> UploadLogo(Microsoft.AspNetCore.Http.IFormFile file)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || !await _permissionService.HasPermissionAsync(currentUser.Id, AppPermissions.AdminManageSettings))
            {
                return Json(new { success = false, message = "Unauthorized." });
            }

            try
            {
                if (file == null || file.Length == 0)
                {
                    return Json(new { success = false, message = "Please select a file to upload." });
                }

                var url = await _brandingService.UploadLogoAsync(file);
                return Json(new { success = true, url = url });
            }
            catch (System.Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Footer()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || !await _permissionService.HasPermissionAsync(currentUser.Id, AppPermissions.AdminManageSettings))
            {
                return RedirectToAction("Index");
            }

            var setting = await _footerService.GetSettingAsync();
            return View(setting);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateFooter([FromBody] FooterSetting dto)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || !await _permissionService.HasPermissionAsync(currentUser.Id, AppPermissions.AdminManageSettings))
            {
                return Json(new { success = false, message = "Unauthorized." });
            }

            if (dto == null) return Json(new { success = false, message = "Invalid data." });

            await _footerService.UpdateSettingAsync(dto);
            return Json(new { success = true, message = "Footer configuration updated across all pages!" });
        }

        [HttpPost]
        public async Task<IActionResult> ResetFooter()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || !await _permissionService.HasPermissionAsync(currentUser.Id, AppPermissions.AdminManageSettings))
            {
                return Json(new { success = false, message = "Unauthorized." });
            }

            await _footerService.ResetDefaultAsync();
            return Json(new { success = true, message = "Footer reset to default settings!" });
        }

        #endregion

        #region Uptime & Health Monitor

        [HttpGet]
        public async Task<IActionResult> Health()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || !await _permissionService.HasPermissionAsync(currentUser.Id, AppPermissions.AdminAnalyticsAndHealth))
            {
                return RedirectToAction("Index");
            }

            var currentUserId = currentUser?.Id ?? 0;
            var model = await _uptimeMonitor.GetDashboardDataAsync(currentUserId);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateHealthSettings([FromBody] UpdateHealthSettingDto dto)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || !await _permissionService.HasPermissionAsync(currentUser.Id, AppPermissions.AdminAnalyticsAndHealth))
            {
                return Json(new { success = false, message = "Unauthorized." });
            }

            if (dto == null) return Json(new { success = false, message = "Invalid request payload." });
            await _uptimeMonitor.UpdateSettingAsync(dto);
            return Json(new { success = true, message = "Health monitoring and alert settings updated successfully!" });
        }

        [HttpPost]
        public async Task<IActionResult> SendTestHealthAlert([FromBody] SendTestAlertDto dto)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || !await _permissionService.HasPermissionAsync(currentUser.Id, AppPermissions.AdminAnalyticsAndHealth))
            {
                return Json(new { success = false, message = "Unauthorized." });
            }

            var currentUserId = currentUser?.Id ?? 0;
            var sent = await _uptimeMonitor.SendTestAlertAsync(dto?.TargetEmail, currentUserId);
            if (sent)
            {
                return Json(new { success = true, message = "Test alert email has been dispatched automatically to Admin(s)! Please check your inbox." });
            }
            return Json(new { success = false, message = "Failed to dispatch email. Please check your email configuration." });
        }

        [HttpPost]
        public async Task<IActionResult> TriggerHealthDiagnostics()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || !await _permissionService.HasPermissionAsync(currentUser.Id, AppPermissions.AdminAnalyticsAndHealth))
            {
                return Json(new { success = false, message = "Unauthorized." });
            }

            var currentUserId = currentUser?.Id ?? 0;
            var model = await _uptimeMonitor.RunDiagnosticsNowAsync(currentUserId);
            return Json(new { 
                success = true, 
                status = model.Status,
                databaseHealthy = model.DatabaseHealthy,
                databaseLatencyMs = model.DatabaseLatencyMs,
                memoryUsageMb = model.MemoryUsageMb,
                diskFreeGb = model.DiskFreeGb,
                message = "Live system diagnostics completed."
            });
        }

        #region Visitor Analytics & Traffic Intelligence

        [HttpGet]
        public async Task<IActionResult> Analytics(string timeRange = "today")
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || !await _permissionService.HasPermissionAsync(currentUser.Id, AppPermissions.AdminAnalyticsAndHealth))
            {
                return RedirectToAction("Index");
            }

            var model = await _analyticsService.GetDashboardDataAsync(timeRange);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> LiveAnalyticsFeed()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || !await _permissionService.HasPermissionAsync(currentUser.Id, AppPermissions.AdminAnalyticsAndHealth))
            {
                return Json(new { success = false, message = "Unauthorized." });
            }

            var data = await _analyticsService.GetLiveTelemetryAsync();
            return Json(data);
        }

        #endregion

        #region Schools Manager

        [HttpGet("/Admin/Schools")]
        public async Task<IActionResult> Schools()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var schools = await _schoolService.GetAllSchoolsAsync();
            return View(schools);
        }

        [HttpPost("/Admin/SaveSchool")]
        public async Task<IActionResult> SaveSchool([FromForm] School model, IFormFile? imageFile)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Json(new { success = false, message = "Unauthorized." });

            if (imageFile != null && imageFile.Length > 0)
            {
                var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "schools");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                var fileName = "school_" + Guid.NewGuid().ToString("N").Substring(0, 8) + Path.GetExtension(imageFile.FileName);
                var fullPath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }
                model.ImageUrl = "/images/schools/" + fileName;
            }

            await _schoolService.SaveSchoolAsync(model);
            return Json(new { success = true, message = "School saved successfully!", imageUrl = model.ImageUrl });
        }

        [HttpPost("/Admin/DeleteSchool")]
        public async Task<IActionResult> DeleteSchool(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Json(new { success = false, message = "Unauthorized." });

            await _schoolService.DeleteSchoolAsync(id);
            return Json(new { success = true, message = "School deleted successfully!" });
        }

        #endregion

        #endregion
    }

    public class UpdateBrandingDto
    {
        public string SiteTitle { get; set; } = "National Aviation University";
        public string Tagline { get; set; } = "LEARN · LEAD · FLY HIGH";
        public string LogoUrl { get; set; } = "/images/logo.png";
        public string? DarkModeLogoUrl { get; set; }
    }

    public class UpdateAnnouncementDto
    {
        public string TagText { get; set; } = "ANNOUNCEMENT:";
        public string MessageText { get; set; } = "Admissions Open Now";
        public string? LinkUrl { get; set; }
        public bool IsVisible { get; set; } = true;
    }

    public class ToggleNavMenuDto
    {
        public string MenuKey { get; set; } = null!;
        public bool? IsHidden { get; set; }
    }
}