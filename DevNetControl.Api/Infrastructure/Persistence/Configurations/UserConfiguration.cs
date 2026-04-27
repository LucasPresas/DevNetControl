using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DevNetControl.Api.Domain;

namespace DevNetControl.Api.Infrastructure.Persistence.Configurations
{
    public class UserConfiguration: IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {

            builder.HasKey(u => u.Id);
            builder.HasIndex(u => u.UserName).IsUnique();

            builder.HasOne(u => u.Parent)
                .WithMany(u => u.Subordinates)
                .HasForeignKey(u => u.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        }


    }
}