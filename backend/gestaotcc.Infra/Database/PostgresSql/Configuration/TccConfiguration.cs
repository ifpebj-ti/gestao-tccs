using gestaotcc.Domain.Entities.Tcc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace gestaotcc.Infra.Database.PostgresSql.Configuration;

public class TccConfiguration : IEntityTypeConfiguration<TccEntity>
{
    public void Configure(EntityTypeBuilder<TccEntity> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .IsRequired(false)
            .HasMaxLength(255);

        builder.Property(x => x.Summary)
            .IsRequired(false)
            .HasMaxLength(255);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(x => x.Step)
            .IsRequired()
            .HasMaxLength(40);

        builder.Property(x => x.CreationDate)
            .IsRequired();

        builder.HasMany(x => x.TccInvites)
            .WithOne(x => x.Tcc)
            .HasForeignKey(x => x.TccId);
        
        builder.HasMany(x => x.Documents)
            .WithOne(x => x.Tcc)
            .HasForeignKey(x => x.TccId);
    }
}