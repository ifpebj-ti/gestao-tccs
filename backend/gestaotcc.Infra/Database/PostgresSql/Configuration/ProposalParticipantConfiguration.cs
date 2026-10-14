using gestaotcc.Domain.Entities.Tcc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace gestaotcc.Infra.Database.PostgresSql.Configuration;

public class ProposalParticipantConfiguration : IEntityTypeConfiguration<ProposalParticipantEntity>
{
    public void Configure(EntityTypeBuilder<ProposalParticipantEntity> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(16);
        builder.Property(x => x.Email).IsRequired().HasMaxLength(254);
        builder.HasIndex(x => new { x.TccId, x.Email }).IsUnique();
        builder.HasIndex(x => new { x.TccId, x.UserId }).IsUnique();
        builder.HasOne(x => x.Tcc).WithMany(x => x.ParticipantInvitations).HasForeignKey(x => x.TccId);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
