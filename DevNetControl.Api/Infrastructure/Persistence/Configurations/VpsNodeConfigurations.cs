using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DevNetControl.Api.Domain;

namespace DevNetControl.Api.Infrastructure.Persistence.Configurations
{
    public class VpsNodeConfiguration: IEntityTypeConfiguration<VpsNode>
    {
        public void Configure(EntityTypeBuilder<VpsNode> builder)
        {
            builder.HasKey(v => v.Id);
            builder.Property(v => v.IP).IsRequired();
            builder.Property(v => v.SshPort).HasDefaultValue(22);
            builder.Property(v => v.label).IsRequired();
            builder.Property(v => v.EncryptedPassword).IsRequired();

            builder.HasOne(v => v.Owner)
                .WithMany(u => u.OwnedNodes)
                .HasForeignKey(v => v.OwnerId);                
        }
    }

}   





