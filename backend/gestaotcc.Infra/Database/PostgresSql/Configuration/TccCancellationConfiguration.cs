using gestaotcc.Domain.Entities.TccCancellation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace gestaotcc.Infra.Database.PostgresSql.Configuration;
public class TccCancellationConfiguration : IEntityTypeConfiguration<TccCancellationEntity>
{
    public void Configure(EntityTypeBuilder<TccCancellationEntity> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Reason)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("PENDING"); // Default status is PENDING


        builder.HasOne(x => x.Tcc)
            .WithOne(t => t.TccCancellation)
            .HasForeignKey<TccCancellationEntity>(x => x.TccId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
