using gestaotcc.Domain.Entities.Course;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace gestaotcc.Infra.Database.PostgresSql.Configuration;

public class CourseConfiguration : IEntityTypeConfiguration<CourseEntity>
{
    public void Configure(EntityTypeBuilder<CourseEntity> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Level) 
            .HasMaxLength(1000)
            .IsRequired();
    }
}