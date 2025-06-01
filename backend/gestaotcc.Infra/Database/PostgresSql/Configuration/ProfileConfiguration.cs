using gestaotcc.Domain.Entities.Profile;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace gestaotcc.Infra.Database.PostgresSql.Configuration;

public class ProfileConfiguration : IEntityTypeConfiguration<ProfileEntity>
{
    public void Configure(EntityTypeBuilder<ProfileEntity> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Role)
            .HasMaxLength(15)
            .IsRequired();
        
        builder.HasMany(x => x.DocumentTypes)
            .WithMany(x => x.Profiles)
            .UsingEntity(x => x.ToTable("documentType_profile"));
    }
}