using System.Collections.Generic;
using System.Threading.Tasks;
using NAU_Web.Models;

namespace NAU_Web.Services
{
    public interface INavMenuService
    {
        Task EnsureSeededAsync();
        Task<List<NavMenuSetting>> GetAllSettingsAsync();
        Task<bool> IsHiddenAsync(string menuKey);
        bool IsHidden(string menuKey);
        Task<bool> ToggleVisibilityAsync(string menuKey, bool? isHidden = null);
        Task ResetDefaultsAsync();
        Task<string> GetRenderedNavBarHtmlAsync();
    }
}
