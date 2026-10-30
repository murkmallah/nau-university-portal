using System.Threading.Tasks;
using NAU_Web.Models;

namespace NAU_Web.Services
{
    public interface IAnnouncementService
    {
        Task<AnnouncementSetting> GetSettingAsync();
        Task UpdateSettingAsync(string tagText, string messageText, string? linkUrl, bool isVisible);
        Task<string> GetRenderedAnnouncementBarHtmlAsync();
    }
}
