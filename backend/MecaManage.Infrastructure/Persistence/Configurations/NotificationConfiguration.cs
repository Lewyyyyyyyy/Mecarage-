using MecaManage.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MecaManage.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Title).IsRequired().HasMaxLength(200);
        builder.Property(n => n.Message).IsRequired().HasMaxLength(1000);
        builder.Property(n => n.NotificationType).IsRequired().HasMaxLength(100);

        builder.HasOne(n => n.Recipient)
               .WithMany(u => u.Notifications)
               .HasForeignKey(n => n.RecipientId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(n => n.SymptomReport)
               .WithMany(sr => sr.Notifications)
               .HasForeignKey(n => n.SymptomReportId)
               .OnDelete(DeleteBehavior.SetNull)
               .IsRequired(false);
    }
}

