using gestaotcc.Domain.Entities.UserTcc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace gestaotcc.Infra.Database.PostgresSql.Configuration;

public class UserTccConfiguration : IEntityTypeConfiguration<UserTccEntity>
{
    public void Configure(EntityTypeBuilder<UserTccEntity> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.BindingDate)
            .IsRequired();

        builder.HasOne(x => x.User)
            .WithMany(x => x.UserTccs)
            .HasForeignKey(x => x.UserId);
        
        builder.HasOne(x => x.Tcc)
            .WithMany(x => x.UserTccs)
            .HasForeignKey(x => x.TccId);

        builder.HasOne(x => x.Profile)
            .WithMany(x => x.UserTccs)
            .HasForeignKey(x => x.ProfileId);
    }
}