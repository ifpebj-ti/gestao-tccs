using gestaotcc.Domain.Entities.Semester;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace gestaotcc.Infra.Database.PostgresSql.Configuration;

public class SemesterConfiguration : IEntityTypeConfiguration<SemesterEntity>
{
    public void Configure(EntityTypeBuilder<SemesterEntity> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(50);
        builder.Property(x => x.StartDate).IsRequired();
        builder.Property(x => x.EndDate).IsRequired();
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);

        builder.HasMany(x => x.Tccs)
            .WithOne(t => t.Semester)
            .HasForeignKey(t => t.SemesterId)
            .OnDelete(DeleteBehavior.SetNull); // Se deletar o semestre, não apaga o TCC, apenas deixa nulo
    }
}
