using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniPlay.Core.Entities;
using UniPlay.Core.Enums;

namespace UniPlay.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for ServiceConnection entity
/// </summary>
public class ServiceConnectionConfiguration : IEntityTypeConfiguration<ServiceConnection>
{
    public void Configure(EntityTypeBuilder<ServiceConnection> builder)
    {
        builder.ToTable("service_connections");

        builder.HasKey(sc => sc.Id);

        builder.Property(sc => sc.UserId)
            .IsRequired()
            .HasColumnName("user_id");

        builder.Property(sc => sc.ServiceType)
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion<string>()
            .HasColumnName("service_type");

        builder.Property(sc => sc.ServiceAccountId)
            .IsRequired()
            .HasMaxLength(255)
            .HasColumnName("service_account_id");

        builder.Property(sc => sc.ServiceAccountEmail)
            .HasMaxLength(320)
            .HasColumnName("service_account_email");

        builder.Property(sc => sc.ServiceAccountDisplayName)
            .HasMaxLength(255)
            .HasColumnName("service_account_display_name");

        builder.Property(sc => sc.ServiceAccountProfileImageUrl)
            .HasMaxLength(2048)
            .HasColumnName("service_account_profile_image_url");

        builder.Property(sc => sc.EncryptedAccessToken)
            .IsRequired()
            .HasMaxLength(1024)
            .HasColumnName("encrypted_access_token");

        builder.Property(sc => sc.EncryptedRefreshToken)
            .HasMaxLength(1024)
            .HasColumnName("encrypted_refresh_token");

        builder.Property(sc => sc.AccessTokenExpiresAt)
            .IsRequired()
            .HasColumnName("access_token_expires_at");

        builder.Property(sc => sc.Scopes)
            .IsRequired()
            .HasMaxLength(512)
            .HasColumnName("scopes");

        builder.Property(sc => sc.ConnectionStatus)
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion<string>()
            .HasColumnName("connection_status");

        builder.Property(sc => sc.LastSyncedAt)
            .HasColumnName("last_synced_at");

        builder.Property(sc => sc.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()")
            .HasColumnName("created_at");

        builder.Property(sc => sc.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()")
            .HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(sc => new { sc.UserId, sc.ServiceType })
            .HasDatabaseName("idx_service_connection_user_service");

        builder.HasIndex(sc => sc.ConnectionStatus)
            .HasDatabaseName("idx_service_connection_status");

        builder.HasIndex(sc => sc.AccessTokenExpiresAt)
            .HasFilter("connection_status = 'Active'")
            .HasDatabaseName("idx_service_connection_expiration");

        // Relationships
        builder.HasOne(sc => sc.User)
            .WithMany(u => u.ServiceConnections)
            .HasForeignKey(sc => sc.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(sc => sc.ServicePlaylists)
            .WithOne(sp => sp.ServiceConnection)
            .HasForeignKey(sp => sp.ServiceConnectionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
