using System.Collections.Generic;
using System.Threading.Tasks;
using NAU_Web.Models;

namespace NAU_Web.Services;

public interface ISchoolService
{
    Task<List<School>> GetActiveSchoolsAsync();
    Task<List<School>> GetAllSchoolsAsync();
    Task<School?> GetByIdAsync(int id);
    Task<School?> GetBySlugAsync(string slug);
    Task SaveSchoolAsync(School school);
    Task DeleteSchoolAsync(int id);
    Task EnsureSeededAsync();
}
