using gestaotcc.Domain.Entities.TccBankingMember;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace gestaotcc.Infra.Database.PostgresSql.Configuration;

public class TccBankingMemberConfiguration : IEntityTypeConfiguration<TccBankingMemberEntity>
{
    public void Configure(EntityTypeBuilder<TccBankingMemberEntity> builder)
    {
        builder.ToTable("TccBankingMembers");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .ValueGeneratedOnAdd()
            .UseIdentityColumn();

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(t => t.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(t => t.Role)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.AccessToken)
            .HasMaxLength(255);

        builder.Property(t => t.TokenExpiryDate)
            .IsRequired(false);

        builder.Property(t => t.Grade)
            .HasColumnType("numeric(5,2)")
            .IsRequired(false);

        builder.Property(t => t.EvaluationComments)
            .HasColumnType("text")
            .IsRequired(false);

        builder.HasOne(t => t.Tcc)
            .WithMany(t => t.BankingMembers)
            .HasForeignKey(t => t.TccId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
