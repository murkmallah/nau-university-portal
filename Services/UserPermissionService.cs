using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NAU_Web.Models;

namespace NAU_Web.Services
{
    public class UserPermissionService : IUserPermissionService
    {
        private readonly NauDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private static bool _tableEnsured = false;
        private static readonly object _tableLock = new();

        public UserPermissionService(
            NauDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;

            EnsureTableCreated();
        }

        private void EnsureTableCreated()
        {
            if (_tableEnsured) return;
            lock (_tableLock)
            {
                if (_tableEnsured) return;
                try
                {
                    const string sql = @"
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='UserPermissions' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[UserPermissions] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] INT NOT NULL,
        [PermissionKey] NVARCHAR(100) NOT NULL,
        [GrantedAt] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        [GrantedByUserId] INT NULL,
        CONSTRAINT [FK_UserPermissions_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([UserId]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_UserPermissions_UserId_Key] ON [dbo].[UserPermissions] ([UserId], [PermissionKey]);
END";
                    _context.Database.ExecuteSqlRaw(sql);
                    _tableEnsured = true;
                }
                catch
                {
                    // Fallback to letting EF handle table creation
                    _tableEnsured = true;
                }
            }
        }

        public async Task<bool> IsSuperAdminAsync(int userId)
        {
            if (userId <= 0) return false;
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return false;

            return await _userManager.IsInRoleAsync(user, "Admin");
        }

        public async Task<bool> HasPermissionAsync(int userId, string permissionKey)
        {
            if (userId <= 0 || string.IsNullOrWhiteSpace(permissionKey)) return false;

            // 1. Super Admins always have all permissions
            if (await IsSuperAdminAsync(userId)) return true;

            // 2. Check direct user permissions
            var hasDirect = await _context.UserPermissions
                .AnyAsync(p => p.UserId == userId && (p.PermissionKey == AppPermissions.AdminFullAccess || p.PermissionKey == permissionKey));

            return hasDirect;
        }

        public async Task<bool> HasAnyPermissionAsync(int userId, params string[] permissionKeys)
        {
            if (userId <= 0 || permissionKeys == null || permissionKeys.Length == 0) return false;

            if (await IsSuperAdminAsync(userId)) return true;

            return await _context.UserPermissions
                .AnyAsync(p => p.UserId == userId && (p.PermissionKey == AppPermissions.AdminFullAccess || permissionKeys.Contains(p.PermissionKey)));
        }

        public async Task<bool> HasSchoolPermissionAsync(int userId, string? schoolSlugOrKey)
        {
            if (userId <= 0) return false;
            if (await IsSuperAdminAsync(userId)) return true;

            var userPerms = await GetUserPermissionsAsync(userId);
            if (userPerms.Contains(AppPermissions.AdminFullAccess) || userPerms.Contains(AppPermissions.SchoolAll))
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(schoolSlugOrKey))
            {
                return userPerms.Any(p => p.StartsWith("School."));
            }

            var s = schoolSlugOrKey.ToLowerInvariant().Trim();

            if (s.Contains("navigation") || s.Contains("ans") || s.Contains("air traffic") || s == "school.ans")
            {
                return userPerms.Contains(AppPermissions.SchoolANS);
            }
            if (s.Contains("airport") || s.Contains("firefighting") || s.Contains("ground") || s == "school.airport")
            {
                return userPerms.Contains(AppPermissions.SchoolAirport);
            }
            if (s.Contains("regulatory") || s.Contains("safety") || s.Contains("audit") || s == "school.regulatory")
            {
                return userPerms.Contains(AppPermissions.SchoolRegulatory);
            }
            if (s.Contains("management") || s.Contains("aviation management") || s.Contains("leadership") || s == "school.management")
            {
                return userPerms.Contains(AppPermissions.SchoolManagement);
            }

            return userPerms.Contains(schoolSlugOrKey);
        }

        public async Task<bool> HasFacultyPermissionAsync(int userId, string? facultySlugOrKey)
        {
            if (userId <= 0) return false;
            if (await IsSuperAdminAsync(userId)) return true;

            var userPerms = await GetUserPermissionsAsync(userId);
            if (userPerms.Contains(AppPermissions.AdminFullAccess) || userPerms.Contains(AppPermissions.FacultyAll))
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(facultySlugOrKey))
            {
                return userPerms.Any(p => p.StartsWith("Faculty."));
            }

            var f = facultySlugOrKey.ToLowerInvariant().Trim();

            if (f.Contains("engineering") || f.Contains("be-ans") || f.Contains("be-airport") || f == "faculty.engineering")
            {
                return userPerms.Contains(AppPermissions.FacultyEngineering);
            }
            if (f.Contains("cyber") || f.Contains("it") || f.Contains("information-technology") || f == "faculty.it")
            {
                return userPerms.Contains(AppPermissions.FacultyIT);
            }
            if (f.Contains("operation") || f.Contains("operations") || f == "faculty.operations")
            {
                return userPerms.Contains(AppPermissions.FacultyOperations);
            }
            if (f.Contains("management") || f.Contains("mba") || f == "faculty.management")
            {
                return userPerms.Contains(AppPermissions.FacultyManagement);
            }

            return userPerms.Contains(facultySlugOrKey);
        }

        public async Task<bool> HasFacilityPermissionAsync(int userId, string? facilitySlugOrKey)
        {
            if (userId <= 0) return false;
            if (await IsSuperAdminAsync(userId)) return true;

            var userPerms = await GetUserPermissionsAsync(userId);
            if (userPerms.Contains(AppPermissions.AdminFullAccess) || userPerms.Contains(AppPermissions.ContentFacilities))
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(facilitySlugOrKey))
            {
                return userPerms.Any(p => p.StartsWith("Facility.") || p == AppPermissions.ContentFacilities);
            }

            var f = facilitySlugOrKey.ToLowerInvariant().Trim();

            if (f == "facilities" || f == "facility-overview") return userPerms.Contains(AppPermissions.ContentFacilities);
            if (f.Contains("library")) return userPerms.Contains(AppPermissions.FacilityLibrary);
            if (f.Contains("cafeteria") || f.Contains("dining")) return userPerms.Contains(AppPermissions.FacilityCafeteria);
            if (f.Contains("hostel") || f.Contains("lodging")) return userPerms.Contains(AppPermissions.FacilityHostel);
            if (f.Contains("mess")) return userPerms.Contains(AppPermissions.FacilityMess);
            if (f.Contains("sport") || f.Contains("gym")) return userPerms.Contains(AppPermissions.FacilitySports);
            if (f.Contains("medical") || f.Contains("clinic") || f.Contains("health")) return userPerms.Contains(AppPermissions.FacilityMedical);
            if (f.Contains("masjid") || f.Contains("mosque")) return userPerms.Contains(AppPermissions.FacilityMasjid);
            if (f.Contains("smart-campus") || f.Contains("wifi") || f.Contains("network")) return userPerms.Contains(AppPermissions.FacilitySmartCampus);

            return userPerms.Contains(AppPermissions.ContentFacilities) || userPerms.Contains(facilitySlugOrKey);
        }

        public async Task<bool> HasSimLabPermissionAsync(int userId, string? simLabSlugOrKey)
        {
            if (userId <= 0) return false;
            if (await IsSuperAdminAsync(userId)) return true;

            var userPerms = await GetUserPermissionsAsync(userId);
            if (userPerms.Contains(AppPermissions.AdminFullAccess) || userPerms.Contains(AppPermissions.ContentSimulationLabs))
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(simLabSlugOrKey))
            {
                return userPerms.Any(p => p.StartsWith("Sim.") || p == AppPermissions.ContentSimulationLabs);
            }

            var s = simLabSlugOrKey.ToLowerInvariant().Trim();

            if (s == "sim-labs" || s == "simulation-labs") return userPerms.Contains(AppPermissions.ContentSimulationLabs);
            if (s.Contains("flight")) return userPerms.Contains(AppPermissions.SimFlight);
            if (s.Contains("atc") || s.Contains("radar-sim")) return userPerms.Contains(AppPermissions.SimATC);
            if (s.Contains("avionics") || s.Contains("electronics")) return userPerms.Contains(AppPermissions.SimAvionics);
            if (s.Contains("navigation") || s.Contains("navaids")) return userPerms.Contains(AppPermissions.SimNavigation);

            return userPerms.Contains(AppPermissions.ContentSimulationLabs) || userPerms.Contains(simLabSlugOrKey);
        }

        public async Task<bool> CanEditPageAsync(int userId, string pageSlug)
        {
            if (userId <= 0) return false;
            if (await IsSuperAdminAsync(userId)) return true;

            var userPerms = await GetUserPermissionsAsync(userId);
            if (userPerms.Contains(AppPermissions.AdminFullAccess)) return true;

            if (string.IsNullOrWhiteSpace(pageSlug)) return false;

            var slug = pageSlug.ToLowerInvariant().Trim();

            if (slug == "home" || slug == "homepage" || slug == "index")
            {
                return userPerms.Contains(AppPermissions.ContentHomepage);
            }

            if (slug == "facilities" || slug.StartsWith("facility-"))
            {
                return await HasFacilityPermissionAsync(userId, slug);
            }

            if (slug == "sim-labs" || slug == "simulation-labs" || slug.StartsWith("sim-"))
            {
                return await HasSimLabPermissionAsync(userId, slug);
            }

            if (slug.StartsWith("school-") || slug.Contains("school"))
            {
                return await HasSchoolPermissionAsync(userId, slug);
            }

            if (slug.StartsWith("faculty-") || slug.Contains("faculty"))
            {
                return await HasFacultyPermissionAsync(userId, slug);
            }

            if (slug == "about" || slug.StartsWith("about-") || slug.StartsWith("history-") || slug == "management")
            {
                return userPerms.Contains(AppPermissions.ContentAbout);
            }

            if (slug == "financial" || slug.Contains("fee") || slug.Contains("scholarship"))
            {
                return userPerms.Contains(AppPermissions.ContentFinancial);
            }

            if (slug == "contact" || slug.Contains("location") || slug.Contains("address"))
            {
                return userPerms.Contains(AppPermissions.ContentContact);
            }

            if (slug == "news" || slug.Contains("news-") || slug.Contains("press"))
            {
                return userPerms.Contains(AppPermissions.ContentNewsAndMedia);
            }

            if (slug == "links" || slug.Contains("portal") || slug.Contains("lms"))
            {
                return userPerms.Contains(AppPermissions.ContentLinks);
            }

            if (slug == "course-catalog" || slug.Contains("catalog"))
            {
                return userPerms.Contains(AppPermissions.ContentCourseCatalog);
            }

            // Fallback check for custom CMS pages
            return userPerms.Contains(AppPermissions.ContentCustomPages);
        }

        public async Task<List<string>> GetUserPermissionsAsync(int userId)
        {
            if (userId <= 0) return new List<string>();

            if (await IsSuperAdminAsync(userId))
            {
                // Super Admin has all permission keys
                return AppPermissions.AllGroups
                    .SelectMany(g => g.Permissions)
                    .Select(p => p.Key)
                    .Distinct()
                    .ToList();
            }

            return await _context.UserPermissions
                .Where(p => p.UserId == userId)
                .Select(p => p.PermissionKey)
                .Distinct()
                .ToListAsync();
        }

        public async Task<bool> SetUserPermissionsAsync(int userId, List<string> permissionKeys, int? grantedByUserId)
        {
            if (userId <= 0) return false;

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return false;

            // Remove existing permissions
            var existing = await _context.UserPermissions.Where(p => p.UserId == userId).ToListAsync();
            _context.UserPermissions.RemoveRange(existing);

            var validKeys = permissionKeys?
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .Distinct()
                .ToList() ?? new List<string>();

            if (validKeys.Any())
            {
                var newPerms = validKeys.Select(k => new UserPermission
                {
                    UserId = userId,
                    PermissionKey = k.Trim(),
                    GrantedAt = DateTime.UtcNow,
                    GrantedByUserId = grantedByUserId
                });

                await _context.UserPermissions.AddRangeAsync(newPerms);

                // Ensure user has staff role so they can log in to admin panel / CMS
                if (!await _roleManager.RoleExistsAsync("Staff"))
                {
                    await _roleManager.CreateAsync(new ApplicationRole { Name = "Staff" });
                }

                if (!await _userManager.IsInRoleAsync(user, "Admin") && !await _userManager.IsInRoleAsync(user, "Staff"))
                {
                    await _userManager.AddToRoleAsync(user, "Staff");
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public List<PermissionGroupDto> GetAllPermissionGroups()
        {
            return AppPermissions.AllGroups;
        }
    }
}
