using System.Threading.Tasks;
using NAU_Web.Models;

namespace NAU_Web.Services
{
    public interface IFooterService
    {
        Task<FooterSetting> GetSettingAsync();
        FooterSetting GetSetting();
        Task UpdateSettingAsync(FooterSetting setting);
        Task ResetDefaultAsync();
    }
}
