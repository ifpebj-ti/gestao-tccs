using gestaotcc.Domain.Entities.AccessCode;
using gestaotcc.Domain.Entities.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace gestaotcc.Infra.Database.PostgresSql.Configuration;

public class AccessCodeConfiguration : IEntityTypeConfiguration<AccessCodeEntity>
{
    public void Configure(EntityTypeBuilder<AccessCodeEntity> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Code)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(a => a.IsActive)
            .IsRequired();

        builder.Property(a => a.IsUserUpdatePassword)
            .IsRequired();

        builder.Property(a => a.ExpirationDate)
            .IsRequired();

        builder.HasOne(x => x.User)
            .WithOne(u => u.AccessCode)
            .HasForeignKey<AccessCodeEntity>(u => u.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}