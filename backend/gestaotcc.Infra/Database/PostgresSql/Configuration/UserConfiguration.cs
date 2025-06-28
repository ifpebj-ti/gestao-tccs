using gestaotcc.Domain.Entities.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace gestaotcc.Infra.Database.PostgresSql.Configuration;

public class UserConfiguration : IEntityTypeConfiguration<UserEntity>
{
    public void Configure(EntityTypeBuilder<UserEntity> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();
        
        builder.Property(x => x.Email)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Registration)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(x => x.CPF)
            .HasMaxLength(14)
            .IsRequired();

        builder.Property(x => x.SIAPE)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(x => x.Password)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasMaxLength(15)
            .IsRequired();
        
        builder.HasMany(x => x.Profile)
            .WithMany(x => x.Users)
            .UsingEntity(x => x.ToTable("user_profile"));

        builder.HasMany(x => x.Signatures)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId);

        builder.HasMany(x => x.Documents)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .IsRequired(false);
    }
}