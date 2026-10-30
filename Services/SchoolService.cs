using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NAU_Web.Models;

namespace NAU_Web.Services;

public class SchoolService : ISchoolService
{
    private readonly IServiceProvider _serviceProvider;

    public SchoolService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task EnsureSeededAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NauDbContext>();

        // Ensure missing columns exist in Schools table
        try
        {
            await db.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Schools') AND name = 'Description')
                BEGIN
                    ALTER TABLE Schools ADD Description NVARCHAR(1000) NULL;
                END;

                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Schools') AND name = 'ImageUrl')
                BEGIN
                    ALTER TABLE Schools ADD ImageUrl NVARCHAR(500) NULL;
                END;

                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Schools') AND name = 'Slug')
                BEGIN
                    ALTER TABLE Schools ADD Slug NVARCHAR(100) NULL;
                END;

                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Schools') AND name = 'IsActive')
                BEGIN
                    ALTER TABLE Schools ADD IsActive BIT NOT NULL CONSTRAINT DF_Schools_IsActive DEFAULT 1;
                END;
            ");
        }
        catch { }

        var existing = await db.Schools.ToListAsync();
        if (!existing.Any())
        {
            var defaultSchools = new List<School>
            {
                new School
                {
                    Name = "School of Air Navigation Services (ANS)",
                    Slug = "navigation",
                    Description = "Air Traffic Control (ATC), Radar Operations, and CNS/ATM Systems Training",
                    ImageUrl = "/images/schools/school_ans.jpg",
                    Icon = "fas fa-broadcast-tower",
                    ColorHex = "#003366",
                    DisplayOrder = 1,
                    IsActive = true
                },
                new School
                {
                    Name = "School of Regulatory Services",
                    Slug = "regulatory",
                    Description = "Flight Safety Inspection, Aerodrome Standards, and Civil Aviation Regulatory Compliance",
                    ImageUrl = "/images/schools/school_regulatory.jpg",
                    Icon = "fas fa-scale-balanced",
                    ColorHex = "#1e8e3e",
                    DisplayOrder = 2,
                    IsActive = true
                },
                new School
                {
                    Name = "School of Airport Services",
                    Slug = "airport",
                    Description = "Aerodrome Rescue & Firefighting (ARFF), Aviation Security (AVSEC), and Ground Operations",
                    ImageUrl = "/images/schools/school_airport.jpg",
                    Icon = "fas fa-fire-extinguisher",
                    ColorHex = "#d97706",
                    DisplayOrder = 3,
                    IsActive = true
                },
                new School
                {
                    Name = "School of Aviation Management",
                    Slug = "management",
                    Description = "Safety Management Systems (SMS), Airport Executive Leadership, and Aviation CPD Workshops",
                    ImageUrl = "/images/schools/school_management.jpg",
                    Icon = "fas fa-briefcase",
                    ColorHex = "#6366f1",
                    DisplayOrder = 4,
                    IsActive = true
                }
            };
            db.Schools.AddRange(defaultSchools);
            await db.SaveChangesAsync();
        }
        else
        {
            // Auto update existing records if ImageUrl or Description or Slug is missing
            bool modified = false;
            foreach (var s in existing)
            {
                if (string.IsNullOrWhiteSpace(s.ImageUrl))
                {
                    if (s.Name.Contains("Navigation") || s.Name.Contains("ANS")) s.ImageUrl = "/images/schools/school_ans.jpg";
                    else if (s.Name.Contains("Regulatory")) s.ImageUrl = "/images/schools/school_regulatory.jpg";
                    else if (s.Name.Contains("Airport")) s.ImageUrl = "/images/schools/school_airport.jpg";
                    else if (s.Name.Contains("Management")) s.ImageUrl = "/images/schools/school_management.jpg";
                    else s.ImageUrl = "/images/schools/school_ans.jpg";
                    modified = true;
                }

                if (string.IsNullOrWhiteSpace(s.Slug))
                {
                    if (s.Name.Contains("Navigation") || s.Name.Contains("ANS")) s.Slug = "navigation";
                    else if (s.Name.Contains("Regulatory")) s.Slug = "regulatory";
                    else if (s.Name.Contains("Airport")) s.Slug = "airport";
                    else if (s.Name.Contains("Management")) s.Slug = "management";
                    else s.Slug = s.Name.ToLower().Replace(" ", "-");
                    modified = true;
                }

                if (string.IsNullOrWhiteSpace(s.Description))
                {
                    if (s.Name.Contains("Navigation")) s.Description = "Air Traffic Control (ATC), Radar Operations, and CNS/ATM Systems Training";
                    else if (s.Name.Contains("Regulatory")) s.Description = "Flight Safety Inspection, Aerodrome Standards, and Civil Aviation Regulatory Compliance";
                    else if (s.Name.Contains("Airport")) s.Description = "Aerodrome Rescue & Firefighting (ARFF), Aviation Security (AVSEC), and Ground Operations";
                    else if (s.Name.Contains("Management")) s.Description = "Safety Management Systems (SMS), Airport Executive Leadership, and Aviation CPD Workshops";
                    modified = true;
                }
            }

            if (modified)
            {
                await db.SaveChangesAsync();
            }
        }
    }

    public async Task<List<School>> GetActiveSchoolsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NauDbContext>();
        await EnsureSeededAsync();
        return await db.Schools
            .Where(s => s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<List<School>> GetAllSchoolsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NauDbContext>();
        await EnsureSeededAsync();
        return await db.Schools
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<School?> GetByIdAsync(int id)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NauDbContext>();
        return await db.Schools.FindAsync(id);
    }

    public async Task<School?> GetBySlugAsync(string slug)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NauDbContext>();
        return await db.Schools.FirstOrDefaultAsync(s => s.Slug == slug);
    }

    public async Task SaveSchoolAsync(School school)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NauDbContext>();

        if (school.SchoolId == 0)
        {
            if (string.IsNullOrWhiteSpace(school.Slug))
            {
                school.Slug = school.Name.ToLower().Replace(" ", "-");
            }
            db.Schools.Add(school);
        }
        else
        {
            var entity = await db.Schools.FindAsync(school.SchoolId);
            if (entity != null)
            {
                entity.Name = school.Name;
                entity.Description = school.Description;
                entity.ImageUrl = school.ImageUrl;
                entity.Icon = school.Icon;
                entity.ColorHex = school.ColorHex;
                entity.Slug = string.IsNullOrWhiteSpace(school.Slug) ? school.Name.ToLower().Replace(" ", "-") : school.Slug;
                entity.DisplayOrder = school.DisplayOrder;
                entity.IsActive = school.IsActive;
            }
        }
        await db.SaveChangesAsync();
    }

    public async Task DeleteSchoolAsync(int id)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NauDbContext>();
        var entity = await db.Schools.FindAsync(id);
        if (entity != null)
        {
            db.Schools.Remove(entity);
            await db.SaveChangesAsync();
        }
    }
}
