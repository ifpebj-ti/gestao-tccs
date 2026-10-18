using gestaotcc.Domain.Entities.TccAdvisorChange;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace gestaotcc.Infra.Database.PostgresSql.Configuration;

public class TccAdvisorChangeConfiguration : IEntityTypeConfiguration<TccAdvisorChangeEntity>
{
    public void Configure(EntityTypeBuilder<TccAdvisorChangeEntity> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RequesterRole)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.Reason)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(30)
            .HasDefaultValue("PENDING_SIGNATURES");

        builder.Property(x => x.ReviewNotes)
            .HasMaxLength(2000);

        builder.HasOne(x => x.Tcc)
            .WithMany(t => t.AdvisorChanges)
            .HasForeignKey(x => x.TccId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Document)
            .WithMany()
            .HasForeignKey(x => x.DocumentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.CurrentAdvisor)
            .WithMany()
            .HasForeignKey(x => x.CurrentAdvisorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SuggestedAdvisor)
            .WithMany()
            .HasForeignKey(x => x.SuggestedAdvisorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.RequesterUser)
            .WithMany()
            .HasForeignKey(x => x.RequesterUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ReviewedByUser)
            .WithMany()
            .HasForeignKey(x => x.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
