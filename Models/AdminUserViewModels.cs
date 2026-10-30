using System;
using System.Collections.Generic;

namespace NAU_Web.Models
{
    public class AdminUserItemViewModel
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public List<string> Roles { get; set; } = new();
        public List<string> Permissions { get; set; } = new();
        public int ApplicationsCount { get; set; }
        public bool IsCurrentAdmin { get; set; }
    }

    public class DeleteUserDto
    {
        public int UserId { get; set; }
    }

    public class ToggleUserStatusDto
    {
        public int UserId { get; set; }
    }

    public class ToggleUserRoleDto
    {
        public int UserId { get; set; }
        public string Role { get; set; } = "Admin";
    }

    public class SaveUserPermissionsDto
    {
        public int UserId { get; set; }
        public List<string> Permissions { get; set; } = new();
        public string? PresetRole { get; set; }
    }
}
