using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniPlay.Core.Entities;

namespace UniPlay.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for UserAccount entity
/// </summary>
public class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        builder.ToTable("user_accounts");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(320)
            .HasColumnName("email");

        builder.Property(u => u.EmailConfirmed)
            .IsRequired()
            .HasDefaultValue(false)
            .HasColumnName("email_confirmed");

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(128)
            .HasColumnName("password_hash");

        builder.Property(u => u.SecurityStamp)
            .IsRequired()
            .HasMaxLength(64)
            .HasColumnName("security_stamp");

        builder.Property(u => u.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()")
            .HasColumnName("created_at");

        builder.Property(u => u.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()")
            .HasColumnName("updated_at");

        builder.Property(u => u.LastLoginAt)
            .HasColumnName("last_login_at");

        // Indexes
        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("idx_user_email");

        builder.HasIndex(u => u.CreatedAt)
            .HasDatabaseName("idx_user_created_at");

        // Relationships
        builder.HasMany(u => u.ServiceConnections)
            .WithOne(sc => sc.User)
            .HasForeignKey(sc => sc.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.PlatformPlaylists)
            .WithOne(pp => pp.User)
            .HasForeignKey(pp => pp.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
