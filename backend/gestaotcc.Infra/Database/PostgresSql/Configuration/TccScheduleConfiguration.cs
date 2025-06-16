using gestaotcc.Domain.Entities.TccSchedule;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace gestaotcc.Infra.Database.PostgresSql.Configuration;
public class TccScheduleConfiguration : IEntityTypeConfiguration<TccScheduleEntity>
{
    public void Configure(EntityTypeBuilder<TccScheduleEntity> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ScheduledDate)
            .IsRequired();
        
        builder.Property(x => x.Location)
            .IsRequired()
            .HasMaxLength(255);
        
        builder.HasOne(x => x.Tcc)
            .WithOne(t => t.TccSchedule)
            .HasForeignKey<TccScheduleEntity>(x => x.TccId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
