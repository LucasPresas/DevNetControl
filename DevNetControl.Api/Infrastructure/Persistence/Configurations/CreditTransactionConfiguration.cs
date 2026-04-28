using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DevNetControl.Api.Domain;

namespace DevNetControl.Api.Infrastructure.Persistence.Configurations
{
    public class CreditTransactionConfiguration: IEntityTypeConfiguration<CreditTransaction>
    {
        public void Configure(EntityTypeBuilder<CreditTransaction> builder)
        {
            builder.HasKey(ct => ct.Id);
            builder.Property(ct => ct.Amount).HasColumnType("decimal(18,2)");
            builder.Property(ct => ct.Note).HasMaxLength(500);
        }
    }
}
