using gestaotcc.Domain.Entities.CampiCourse;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace gestaotcc.Infra.Database.PostgresSql.Configuration;

public class CampiCourseConfiguration : IEntityTypeConfiguration<CampiCourseEntity>
{
    public void Configure(EntityTypeBuilder<CampiCourseEntity> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.HasOne(x => x.Course)
            .WithMany(x => x.CampiCourses)
            .HasForeignKey(x => x.CourseId);
        
        builder.HasOne(x => x.Campi)
            .WithMany(x => x.CampiCourses)
            .HasForeignKey(x => x.CampiId);
    }
}