using gestaotcc.Domain.Entities.Document;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace gestaotcc.Infra.Database.PostgresSql.Configuration;

public class DocumentConfiguration : IEntityTypeConfiguration<DocumentEntity>
{
    public void Configure(EntityTypeBuilder<DocumentEntity> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.IsSigned)
            .IsRequired();
        
        builder.Property(x => x.File)
            .IsRequired()
            .HasColumnType("bytea");

        builder.HasMany(x => x.Signatures)
            .WithOne(x => x.Document)
            .HasForeignKey(x => x.DocumentId);
    }
}