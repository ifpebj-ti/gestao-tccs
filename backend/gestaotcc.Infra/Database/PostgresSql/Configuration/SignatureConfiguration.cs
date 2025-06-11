using gestaotcc.Domain.Entities.Signature;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace gestaotcc.Infra.Database.PostgresSql.Configuration;

public class SignatureConfiguration : IEntityTypeConfiguration<SignatureEntity>
{
    public void Configure(EntityTypeBuilder<SignatureEntity> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SignatureDate)
            .IsRequired();
    }
}