using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NAU_Web.Models;

namespace NAU_Web.Services
{
    public interface ISiteBrandingService
    {
        Task<SiteBrandingSetting> GetSettingAsync();
        SiteBrandingSetting GetSetting();
        Task UpdateSettingAsync(string siteTitle, string tagline, string logoUrl, string? darkModeLogoUrl);
        Task<string> UploadLogoAsync(IFormFile file);
        Task<string> GetHeaderLogoHtmlAsync();
        Task<string> GetFooterLogoHtmlAsync();
    }
}
