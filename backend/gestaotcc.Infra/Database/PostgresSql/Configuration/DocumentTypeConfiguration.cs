using gestaotcc.Domain.Entities.DocumentType;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace gestaotcc.Infra.Database.PostgresSql.Configuration;

public class DocumentTypeConfiguration : IEntityTypeConfiguration<DocumentTypeEntity>
{
    public void Configure(EntityTypeBuilder<DocumentTypeEntity> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.SignatureOrder)
            .IsRequired();

        builder.HasMany(x => x.Documents)
            .WithOne(x => x.DocumentType)
            .HasForeignKey(x => x.DocumentTypeId);
    }
}