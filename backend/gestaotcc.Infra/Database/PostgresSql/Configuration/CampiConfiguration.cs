using gestaotcc.Domain.Entities.Campi;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace gestaotcc.Infra.Database.PostgresSql.Configuration;

public class CampiConfiguration : IEntityTypeConfiguration<CampiEntity>
{
    public void Configure(EntityTypeBuilder<CampiEntity> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired();
    }
}