using System.Collections.Generic;
using System.Threading.Tasks;
using NAU_Web.Models;

namespace NAU_Web.Services
{
    public interface IUserPermissionService
    {
        Task<bool> HasPermissionAsync(int userId, string permissionKey);
        Task<bool> HasAnyPermissionAsync(int userId, params string[] permissionKeys);
        Task<bool> HasSchoolPermissionAsync(int userId, string? schoolSlugOrKey);
        Task<bool> HasFacultyPermissionAsync(int userId, string? facultySlugOrKey);
        Task<bool> HasFacilityPermissionAsync(int userId, string? facilitySlugOrKey);
        Task<bool> HasSimLabPermissionAsync(int userId, string? simLabSlugOrKey);
        Task<bool> CanEditPageAsync(int userId, string pageSlug);
        Task<List<string>> GetUserPermissionsAsync(int userId);
        Task<bool> SetUserPermissionsAsync(int userId, List<string> permissionKeys, int? grantedByUserId);
        Task<bool> IsSuperAdminAsync(int userId);
        List<PermissionGroupDto> GetAllPermissionGroups();
    }
}
