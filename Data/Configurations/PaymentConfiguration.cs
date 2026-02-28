using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentService.Entities;

namespace PaymentService.Data.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(payment => payment.Id);
        
        builder.Property(payment => payment.Status)
            .HasConversion<string>();

        builder.Property(payment => payment.IdempotencyKey)
            .IsRequired();
        
        builder.HasIndex(payment => payment.IdempotencyKey)
            .IsUnique();

        builder.HasOne(payment => payment.Order)
            .WithMany(order => order.Payments)
            .HasForeignKey(payment => payment.OrderId);

        builder.HasOne(payment => payment.User)
            .WithMany()
            .HasForeignKey(payment => payment.UserId);
    }
}
