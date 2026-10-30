using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace NAU_Web.Models;

public partial class NauDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, int>
{
    public NauDbContext()
    {
    }

    public NauDbContext(DbContextOptions<NauDbContext> options)
        : base(options)
    {
    }
    public virtual DbSet<Application> Applications { get; set; }
    public virtual DbSet<AcademicCalendarResult> AcademicCalendarResults { get; set; }

    public virtual DbSet<AnnouncementSetting> AnnouncementSettings { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<ContactSettings> ContactSettings { get; set; }

    public virtual DbSet<Course> Courses { get; set; }

    public virtual DbSet<Event> Events { get; set; }

    public virtual DbSet<Facility> Facilities { get; set; }

    public virtual DbSet<FacilityHour> FacilityHours { get; set; }

    public virtual DbSet<FacilityService> FacilityServices { get; set; }

    public virtual DbSet<FacilityStat> FacilityStats { get; set; }

    public virtual DbSet<FacilityCardTag> FacilityCardTags { get; set; }

    public virtual DbSet<Faculty> Faculties { get; set; }

    public virtual DbSet<FooterSetting> FooterSettings { get; set; }

    public virtual DbSet<GalleryPhoto> GalleryPhotos { get; set; }

    public virtual DbSet<GalleryVideo> GalleryVideos { get; set; }

    public virtual DbSet<LinkItem> Links { get; set; }

    public virtual DbSet<MediaLibrary> MediaLibraries { get; set; }

    public virtual DbSet<NavMenuSetting> NavMenuSettings { get; set; }

    public virtual DbSet<NewsPost> NewsPosts { get; set; }

    public virtual DbSet<Page> Pages { get; set; }

    public virtual DbSet<Program> Programs { get; set; }

    public virtual DbSet<ProgramCareer> ProgramCareers { get; set; }

    public virtual DbSet<ProgramHighlight> ProgramHighlights { get; set; }

    public virtual DbSet<School> Schools { get; set; }

    public virtual DbSet<SiteBrandingSetting> SiteBrandingSettings { get; set; }

    public virtual DbSet<SimulationLabSection> SimulationLabSections { get; set; }

    public virtual DbSet<SimulationLabImage> SimulationLabImages { get; set; }

    public virtual DbSet<SimulationLabFeature> SimulationLabFeatures { get; set; }

    public virtual DbSet<SimulationLabInfoCard> SimulationLabInfoCards { get; set; }

    public virtual DbSet<SimulationLabInfoItem> SimulationLabInfoItems { get; set; }

    public virtual DbSet<UserPermission> UserPermissions { get; set; }

    public virtual DbSet<VwCourseStatus> VwCourseStatuses { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserPermission>(entity =>
        {
            entity.ToTable("UserPermissions");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.PermissionKey });
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AcademicCalendarResult>(entity =>
        {
            entity.HasKey(e => e.ResultId).HasName("PK__Academic__976902089112C1B8");

            entity.HasIndex(e => e.ResultDate, "IX_AcademicCalendarResults_Date");

            entity.Property(e => e.Label).HasMaxLength(300);
            entity.Property(e => e.ResultType).HasMaxLength(20);

            entity.HasOne(d => d.Course).WithMany(p => p.AcademicCalendarResults)
                .HasForeignKey(d => d.CourseId)
                .HasConstraintName("FK__AcademicC__Cours__08B54D69");
        });

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("Users");
            entity.Property(u => u.Id).HasColumnName("UserId");
        });

        modelBuilder.Entity<Application>(entity =>
        {
            entity.ToTable("Applications");
            entity.HasOne(a => a.User)
                  .WithMany()
                  .HasForeignKey(a => a.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ApplicationRole>(entity =>
        {
            entity.ToTable("Roles");
            entity.Property(r => r.Id).HasColumnName("RoleId");
            entity.Property(r => r.Name).HasColumnName("RoleName");
        });

        modelBuilder.Entity<IdentityUserRole<int>>(entity =>
        {
            entity.ToTable("UserRoles");
        });
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.AuditLogId).HasName("PK__AuditLog__EB5F6CBD292F0611");

            entity.ToTable("AuditLog");

            entity.Property(e => e.Action).HasMaxLength(50);
            entity.Property(e => e.EntityName).HasMaxLength(100);
            entity.Property(e => e.OccurredAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.User).WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AuditLog__UserId__10566F31");
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.CourseId).HasName("PK__Courses__C92D71A7C3F213E2");

            entity.HasIndex(e => new { e.StartDate, e.EndDate }, "IX_Courses_Dates");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Duration).HasMaxLength(50);
            entity.Property(e => e.IsTbd).HasColumnName("IsTBD");
            entity.Property(e => e.Title).HasMaxLength(300);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.School).WithMany(p => p.Courses)
                .HasForeignKey(d => d.SchoolId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Courses__SchoolI__6A30C649");
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.EventId).HasName("PK__Events__7944C810375AA700");

            entity.HasIndex(e => e.EventDate, "IX_Events_Date");

            entity.Property(e => e.Location).HasMaxLength(250);
            entity.Property(e => e.Title).HasMaxLength(300);

            entity.HasOne(d => d.CreatedByUser).WithMany()
                .HasForeignKey(d => d.CreatedByUserId)
                .HasConstraintName("FK__Events__CreatedB__05D8E0BE");
        });

        modelBuilder.Entity<Facility>(entity =>
        {
            entity.HasKey(e => e.FacilityId).HasName("PK__Faciliti__5FB08A74E8AC62CE");

            entity.HasIndex(e => e.Slug, "UQ__Faciliti__BC7B5FB68324BE93").IsUnique();

            entity.Property(e => e.ColorHex).HasMaxLength(10);
            entity.Property(e => e.HeroImageUrl).HasMaxLength(500);
            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.Slug).HasMaxLength(100);
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.CardIcon).HasMaxLength(50);
            entity.Property(e => e.GradientColorHex2).HasMaxLength(10);
            entity.Property(e => e.ShortHours).HasMaxLength(150);
            entity.Property(e => e.IsAvailable).HasDefaultValue(true);
        });

        modelBuilder.Entity<FacilityHour>(entity =>
        {
            entity.HasKey(e => e.FacilityHourId).HasName("PK__Facility__7E6FB5F6793B00EC");

            entity.Property(e => e.DayOrLabel).HasMaxLength(100);
            entity.Property(e => e.TimeValue).HasMaxLength(100);

            entity.HasOne(d => d.Facility).WithMany(p => p.FacilityHours)
                .HasForeignKey(d => d.FacilityId)
                .HasConstraintName("FK__FacilityH__Facil__7C4F7684");
        });

        modelBuilder.Entity<FacilityService>(entity =>
        {
            entity.HasKey(e => e.FacilityServiceId).HasName("PK__Facility__DBE9580B6CAE61A6");

            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Icon).HasMaxLength(50);
            entity.Property(e => e.Title).HasMaxLength(250);

            entity.HasOne(d => d.Facility).WithMany(p => p.FacilityServices)
                .HasForeignKey(d => d.FacilityId)
                .HasConstraintName("FK__FacilityS__Facil__787EE5A0");
        });

        modelBuilder.Entity<FacilityStat>(entity =>
        {
            entity.HasKey(e => e.FacilityStatId).HasName("PK__Facility__4F2AFF7C09EF7A4D");

            entity.Property(e => e.StatLabel).HasMaxLength(150);
            entity.Property(e => e.StatNumber).HasMaxLength(50);

            entity.HasOne(d => d.Facility).WithMany(p => p.FacilityStats)
                .HasForeignKey(d => d.FacilityId)
                .HasConstraintName("FK__FacilityS__Facil__74AE54BC");
        });

        modelBuilder.Entity<FacilityCardTag>(entity =>
        {
            entity.HasKey(e => e.FacilityCardTagId);

            entity.Property(e => e.Icon).HasMaxLength(50);
            entity.Property(e => e.Label).HasMaxLength(150);

            entity.HasOne(d => d.Facility).WithMany(p => p.FacilityCardTags)
                .HasForeignKey(d => d.FacilityId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Faculty>(entity =>
        {
            entity.HasKey(e => e.FacultyId).HasName("PK__Facultie__306F630E42B2DE31");

            entity.HasIndex(e => e.Slug, "UQ__Facultie__BC7B5FB6C29E5EC2").IsUnique();

            entity.Property(e => e.ColorHex).HasMaxLength(10);
            entity.Property(e => e.Icon).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.Slug).HasMaxLength(100);
        });

        modelBuilder.Entity<MediaLibrary>(entity =>
        {
            entity.HasKey(e => e.MediaId).HasName("PK__MediaLib__B2C2B5CFEAFA7EB5");

            entity.ToTable("MediaLibrary");

            entity.Property(e => e.FileName).HasMaxLength(300);
            entity.Property(e => e.FilePath).HasMaxLength(500);
            entity.Property(e => e.FileType).HasMaxLength(50);
            entity.Property(e => e.UploadedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.UploadedByUser).WithMany()
                .HasForeignKey(d => d.UploadedByUserId)
                .HasConstraintName("FK__MediaLibr__Uploa__0C85DE4D");
        });

        modelBuilder.Entity<GalleryPhoto>(entity =>
        {
            entity.HasKey(e => e.PhotoId).HasName("PK_GalleryPhotos");

            entity.HasIndex(e => e.DisplayOrder, "IX_GalleryPhotos_DisplayOrder");

            entity.Property(e => e.Caption).HasMaxLength(300);
            entity.Property(e => e.Album).HasMaxLength(100);
            entity.Property(e => e.Icon).HasMaxLength(50);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.CreatedByUser).WithMany()
                .HasForeignKey(d => d.CreatedByUserId)
                .HasConstraintName("FK__GalleryPhotos__CreatedByUserId");
        });

        modelBuilder.Entity<GalleryVideo>(entity =>
        {
            entity.HasKey(e => e.VideoId).HasName("PK_GalleryVideos");

            entity.HasIndex(e => e.DisplayOrder, "IX_GalleryVideos_DisplayOrder");

            entity.Property(e => e.Title).HasMaxLength(300);
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.VideoUrl).HasMaxLength(500);
            entity.Property(e => e.ThumbnailUrl).HasMaxLength(500);
            entity.Property(e => e.Icon).HasMaxLength(50);
            entity.Property(e => e.Duration).HasMaxLength(20);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.CreatedByUser).WithMany()
                .HasForeignKey(d => d.CreatedByUserId)
                .HasConstraintName("FK__GalleryVideos__CreatedByUserId");
        });

        modelBuilder.Entity<LinkItem>(entity =>
        {
            entity.HasKey(e => e.LinkId).HasName("PK_Links");

            entity.HasIndex(e => e.DisplayOrder, "IX_Links_DisplayOrder");

            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Url).HasMaxLength(500);
            entity.Property(e => e.Icon).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.CreatedByUser).WithMany()
                .HasForeignKey(d => d.CreatedByUserId)
                .HasConstraintName("FK__Links__CreatedByUserId");
        });

        modelBuilder.Entity<NewsPost>(entity =>
        {
            entity.HasKey(e => e.NewsPostId).HasName("PK__NewsPost__C9F1531E3481BC91");

            entity.HasIndex(e => e.PublishDate, "IX_NewsPosts_PublishDate").IsDescending();

            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Icon).HasMaxLength(50);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.Title).HasMaxLength(300);

            entity.HasOne(d => d.CreatedByUser).WithMany()
                .HasForeignKey(d => d.CreatedByUserId)
                .HasConstraintName("FK__NewsPosts__Creat__02084FDA");
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.CourseId).HasName("PK__Courses__C92D71A7C3F213E2");

            entity.HasIndex(e => new { e.StartDate, e.EndDate }, "IX_Courses_Dates");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Duration).HasMaxLength(50);
            entity.Property(e => e.IsTbd).HasColumnName("IsTBD");
            entity.Property(e => e.Title).HasMaxLength(300);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.School).WithMany(p => p.Courses)
                .HasForeignKey(d => d.SchoolId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Courses__SchoolI__6A30C649");
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.EventId).HasName("PK__Events__7944C810375AA700");

            entity.HasIndex(e => e.EventDate, "IX_Events_Date");

            entity.Property(e => e.Location).HasMaxLength(250);
            entity.Property(e => e.Title).HasMaxLength(300);

            entity.HasOne(d => d.CreatedByUser).WithMany()
                .HasForeignKey(d => d.CreatedByUserId)
                .HasConstraintName("FK__Events__CreatedB__05D8E0BE");
        });

        modelBuilder.Entity<Facility>(entity =>
        {
            entity.HasKey(e => e.FacilityId).HasName("PK__Faciliti__5FB08A74E8AC62CE");

            entity.HasIndex(e => e.Slug, "UQ__Faciliti__BC7B5FB68324BE93").IsUnique();

            entity.Property(e => e.ColorHex).HasMaxLength(10);
            entity.Property(e => e.HeroImageUrl).HasMaxLength(500);
            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.Slug).HasMaxLength(100);
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.CardIcon).HasMaxLength(50);
            entity.Property(e => e.GradientColorHex2).HasMaxLength(10);
            entity.Property(e => e.ShortHours).HasMaxLength(150);
            entity.Property(e => e.IsAvailable).HasDefaultValue(true);
        });

        modelBuilder.Entity<FacilityHour>(entity =>
        {
            entity.HasKey(e => e.FacilityHourId).HasName("PK__Facility__7E6FB5F6793B00EC");

            entity.Property(e => e.DayOrLabel).HasMaxLength(100);
            entity.Property(e => e.TimeValue).HasMaxLength(100);

            entity.HasOne(d => d.Facility).WithMany(p => p.FacilityHours)
                .HasForeignKey(d => d.FacilityId)
                .HasConstraintName("FK__FacilityH__Facil__7C4F7684");
        });

        modelBuilder.Entity<FacilityService>(entity =>
        {
            entity.HasKey(e => e.FacilityServiceId).HasName("PK__Facility__DBE9580B6CAE61A6");

            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Icon).HasMaxLength(50);
            entity.Property(e => e.Title).HasMaxLength(250);

            entity.HasOne(d => d.Facility).WithMany(p => p.FacilityServices)
                .HasForeignKey(d => d.FacilityId)
                .HasConstraintName("FK__FacilityS__Facil__787EE5A0");
        });

        modelBuilder.Entity<FacilityStat>(entity =>
        {
            entity.HasKey(e => e.FacilityStatId).HasName("PK__Facility__4F2AFF7C09EF7A4D");

            entity.Property(e => e.StatLabel).HasMaxLength(150);
            entity.Property(e => e.StatNumber).HasMaxLength(50);

            entity.HasOne(d => d.Facility).WithMany(p => p.FacilityStats)
                .HasForeignKey(d => d.FacilityId)
                .HasConstraintName("FK__FacilityS__Facil__74AE54BC");
        });

        modelBuilder.Entity<FacilityCardTag>(entity =>
        {
            entity.HasKey(e => e.FacilityCardTagId);

            entity.Property(e => e.Icon).HasMaxLength(50);
            entity.Property(e => e.Label).HasMaxLength(150);

            entity.HasOne(d => d.Facility).WithMany(p => p.FacilityCardTags)
                .HasForeignKey(d => d.FacilityId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Faculty>(entity =>
        {
            entity.HasKey(e => e.FacultyId).HasName("PK__Facultie__306F630E42B2DE31");

            entity.HasIndex(e => e.Slug, "UQ__Facultie__BC7B5FB6C29E5EC2").IsUnique();

            entity.Property(e => e.ColorHex).HasMaxLength(10);
            entity.Property(e => e.Icon).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.Slug).HasMaxLength(100);
        });

        modelBuilder.Entity<MediaLibrary>(entity =>
        {
            entity.HasKey(e => e.MediaId).HasName("PK__MediaLib__B2C2B5CFEAFA7EB5");

            entity.ToTable("MediaLibrary");

            entity.Property(e => e.FileName).HasMaxLength(300);
            entity.Property(e => e.FilePath).HasMaxLength(500);
            entity.Property(e => e.FileType).HasMaxLength(50);
            entity.Property(e => e.UploadedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.UploadedByUser).WithMany()
                .HasForeignKey(d => d.UploadedByUserId)
                .HasConstraintName("FK__MediaLibr__Uploa__0C85DE4D");
        });

        modelBuilder.Entity<GalleryPhoto>(entity =>
        {
            entity.HasKey(e => e.PhotoId).HasName("PK_GalleryPhotos");

            entity.HasIndex(e => e.DisplayOrder, "IX_GalleryPhotos_DisplayOrder");

            entity.Property(e => e.Caption).HasMaxLength(300);
            entity.Property(e => e.Album).HasMaxLength(100);
            entity.Property(e => e.Icon).HasMaxLength(50);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.CreatedByUser).WithMany()
                .HasForeignKey(d => d.CreatedByUserId)
                .HasConstraintName("FK__GalleryPhotos__CreatedByUserId");
        });

        modelBuilder.Entity<GalleryVideo>(entity =>
        {
            entity.HasKey(e => e.VideoId).HasName("PK_GalleryVideos");

            entity.HasIndex(e => e.DisplayOrder, "IX_GalleryVideos_DisplayOrder");

            entity.Property(e => e.Title).HasMaxLength(300);
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.VideoUrl).HasMaxLength(500);
            entity.Property(e => e.ThumbnailUrl).HasMaxLength(500);
            entity.Property(e => e.Icon).HasMaxLength(50);
            entity.Property(e => e.Duration).HasMaxLength(20);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.CreatedByUser).WithMany()
                .HasForeignKey(d => d.CreatedByUserId)
                .HasConstraintName("FK__GalleryVideos__CreatedByUserId");
        });

        modelBuilder.Entity<LinkItem>(entity =>
        {
            entity.HasKey(e => e.LinkId).HasName("PK_Links");

            entity.HasIndex(e => e.DisplayOrder, "IX_Links_DisplayOrder");

            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Url).HasMaxLength(500);
            entity.Property(e => e.Icon).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.CreatedByUser).WithMany()
                .HasForeignKey(d => d.CreatedByUserId)
                .HasConstraintName("FK__Links__CreatedByUserId");
        });

        modelBuilder.Entity<NewsPost>(entity =>
        {
            entity.HasKey(e => e.NewsPostId).HasName("PK__NewsPost__C9F1531E3481BC91");

            entity.HasIndex(e => e.PublishDate, "IX_NewsPosts_PublishDate").IsDescending();

            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Icon).HasMaxLength(50);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.Title).HasMaxLength(300);

            entity.HasOne(d => d.CreatedByUser).WithMany()
                .HasForeignKey(d => d.CreatedByUserId)
                .HasConstraintName("FK__NewsPosts__Creat__02084FDA");
        });

        modelBuilder.Entity<Page>(entity =>
        {
            entity.HasKey(e => e.PageId).HasName("PK__Pages__C565B104DE3C2805");

            entity.HasIndex(e => e.Slug, "UQ__Pages__BC7B5FB6FD7F25B9").IsUnique();

            entity.Property(e => e.HeroImageUrl).HasMaxLength(500);
            entity.Property(e => e.IsPublished).HasDefaultValue(true);
            entity.Property(e => e.Slug).HasMaxLength(150);
            entity.Property(e => e.Title).HasMaxLength(250);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.UpdatedByUser).WithMany()
                .HasForeignKey(d => d.UpdatedByUserId)
                .HasConstraintName("FK__Pages__UpdatedBy__5629CD9C");
        });

        modelBuilder.Entity<Program>(entity =>
        {
            entity.HasKey(e => e.ProgramId).HasName("PK__Programs__7525605824997CA2");

            entity.HasIndex(e => new { e.FacultyId, e.Code }, "UQ_Program_Code").IsUnique();

            entity.Property(e => e.Adpyears)
                .HasMaxLength(20)
                .HasColumnName("ADPYears");
            entity.Property(e => e.Code).HasMaxLength(20);
            entity.Property(e => e.ColorHex).HasMaxLength(10);
            entity.Property(e => e.Department).HasMaxLength(250);
            entity.Property(e => e.GradYears).HasMaxLength(20);
            entity.Property(e => e.Icon).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.UndergradYears).HasMaxLength(20);

            entity.HasOne(d => d.Faculty).WithMany(p => p.Programs)
                .HasForeignKey(d => d.FacultyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Programs__Facult__5CD6CB2B");
        });

        modelBuilder.Entity<ProgramCareer>(entity =>
        {
            entity.HasKey(e => e.ProgramCareerId).HasName("PK__ProgramC__2CFB06122C1C7B1F");

            entity.Property(e => e.CareerTitle).HasMaxLength(250);

            entity.HasOne(d => d.Program).WithMany(p => p.ProgramCareers)
                .HasForeignKey(d => d.ProgramId)
                .HasConstraintName("FK__ProgramCa__Progr__5FB337D6");
        });

        modelBuilder.Entity<ProgramHighlight>(entity =>
        {
            entity.HasKey(e => e.ProgramHighlightId).HasName("PK__ProgramH__61157A68707327E8");

            entity.Property(e => e.HighlightText).HasMaxLength(500);

            entity.HasOne(d => d.Program).WithMany(p => p.ProgramHighlights)
                .HasForeignKey(d => d.ProgramId)
                .HasConstraintName("FK__ProgramHi__Progr__6383C8BA");
        });

        modelBuilder.Entity<School>(entity =>
        {
            entity.HasKey(e => e.SchoolId);

            entity.Property(e => e.ColorHex).HasMaxLength(10);
            entity.Property(e => e.Icon).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.Slug).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });


        modelBuilder.Entity<SimulationLabSection>(entity =>
        {
            entity.HasKey(e => e.SimulationLabSectionId);

            entity.HasIndex(e => e.SectionKey).IsUnique();

            entity.Property(e => e.SectionKey).HasMaxLength(100);
            entity.Property(e => e.Tag).HasMaxLength(150);
            entity.Property(e => e.Title).HasMaxLength(250);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.IconClass).HasMaxLength(50);
            entity.Property(e => e.ColorHex).HasMaxLength(10);
            entity.Property(e => e.ColorHex2).HasMaxLength(10);
            entity.Property(e => e.IsAvailable).HasDefaultValue(true);
        });

        modelBuilder.Entity<SimulationLabImage>(entity =>
        {
            entity.HasKey(e => e.SimulationLabImageId);

            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.IconClass).HasMaxLength(50);
            entity.Property(e => e.Title).HasMaxLength(250);
            entity.Property(e => e.Badge).HasMaxLength(150);

            entity.HasOne(d => d.SimulationLabSection).WithMany(p => p.SimulationLabImages)
                .HasForeignKey(d => d.SimulationLabSectionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SimulationLabFeature>(entity =>
        {
            entity.HasKey(e => e.SimulationLabFeatureId);

            entity.Property(e => e.IconClass).HasMaxLength(50);
            entity.Property(e => e.Label).HasMaxLength(150);

            entity.HasOne(d => d.SimulationLabSection).WithMany(p => p.SimulationLabFeatures)
                .HasForeignKey(d => d.SimulationLabSectionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SimulationLabInfoCard>(entity =>
        {
            entity.HasKey(e => e.SimulationLabInfoCardId);

            entity.HasIndex(e => e.SimulationLabSectionId).IsUnique();

            entity.Property(e => e.Heading).HasMaxLength(150);
            entity.Property(e => e.HighlightText).HasMaxLength(250);
            entity.Property(e => e.HighlightIcon).HasMaxLength(50);
            entity.Property(e => e.HighlightColor).HasMaxLength(20);

            entity.HasOne(d => d.SimulationLabSection).WithOne(p => p.SimulationLabInfoCard)
                .HasForeignKey<SimulationLabInfoCard>(d => d.SimulationLabSectionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SimulationLabInfoItem>(entity =>
        {
            entity.HasKey(e => e.SimulationLabInfoItemId);

            entity.Property(e => e.Text).HasMaxLength(250);
            entity.Property(e => e.ColorHex).HasMaxLength(30);

            entity.HasOne(d => d.SimulationLabInfoCard).WithMany(p => p.SimulationLabInfoItems)
                .HasForeignKey(d => d.SimulationLabInfoCardId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ContactSettings>(entity =>
        {
            entity.HasKey(e => e.ContactSettingsId);

            entity.Property(e => e.Heading).HasMaxLength(150);
            entity.Property(e => e.Subtext).HasMaxLength(500);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.Address).HasMaxLength(250);
            entity.Property(e => e.OfficeHours).HasMaxLength(150);
            entity.Property(e => e.SmtpUsername).HasMaxLength(150);
            entity.Property(e => e.SmtpPassword).HasMaxLength(150);
        });

        modelBuilder.Entity<VwCourseStatus>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_CourseStatus");

            entity.Property(e => e.CourseId).ValueGeneratedOnAdd();
            entity.Property(e => e.Duration).HasMaxLength(50);
            entity.Property(e => e.IsTbd).HasColumnName("IsTBD");
            entity.Property(e => e.Status)
                .HasMaxLength(9)
                .IsUnicode(false);
            entity.Property(e => e.Title).HasMaxLength(300);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}