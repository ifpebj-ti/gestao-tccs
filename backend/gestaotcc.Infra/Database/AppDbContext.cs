using gestaotcc.Domain.Entities.AccessCode;
using gestaotcc.Domain.Entities.Campi;
using gestaotcc.Domain.Entities.CampiCourse;
using gestaotcc.Domain.Entities.Course;
using gestaotcc.Domain.Entities.Document;
using gestaotcc.Domain.Entities.DocumentType;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Signature;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.TccCancellation;
using gestaotcc.Domain.Entities.TccInvite;
using gestaotcc.Domain.Entities.TccSchedule;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Entities.UserTcc;
using Microsoft.EntityFrameworkCore;

namespace gestaotcc.Infra.Database;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<UserEntity> Users { get; set; }
    public DbSet<ProfileEntity> Profiles { get; set; }
    public DbSet<AccessCodeEntity> AccessCodes { get; set; }
    public DbSet<TccEntity> Tccs { get; set; }
    public DbSet<UserTccEntity> UserTccs { get; set; }
    public DbSet<TccInviteEntity> TccInvites { get; set; }
    public DbSet<TccCancellationEntity> TccCancellations { get; set; }
    public DbSet<TccScheduleEntity> TccSchedules { get; set; }
    public DbSet<DocumentEntity> Documents { get; set; }
    public DbSet<DocumentTypeEntity> DocumentTypes { get; set; }
    public DbSet<SignatureEntity> Signatures { get; set; }
    
    public DbSet<CampiEntity> Campi { get; set; }
    public DbSet<CourseEntity> Courses { get; set; }
    public DbSet<CampiCourseEntity> CampiCourses { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}