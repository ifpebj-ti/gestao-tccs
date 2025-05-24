using gestaotcc.Domain.Entities.AccessCode;
using gestaotcc.Domain.Entities.Course;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.TccInvite;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Entities.UserTcc;
using Microsoft.EntityFrameworkCore;

namespace gestaotcc.Infra.Database;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<UserEntity> Users { get; set; }
    public DbSet<ProfileEntity> Profiles { get; set; }
    public DbSet<AccessCodeEntity> AccessCodes { get; set; }
    public DbSet<CourseEntity> Courses { get; set; }
    public DbSet<TccEntity> Tccs { get; set; }
    public DbSet<UserTccEntity> UserTccs { get; set; }
    public DbSet<TccInviteEntity> TccInvites { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}