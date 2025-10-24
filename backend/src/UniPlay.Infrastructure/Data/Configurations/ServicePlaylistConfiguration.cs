using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniPlay.Core.Entities;

namespace UniPlay.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for ServicePlaylist entity
/// </summary>
public class ServicePlaylistConfiguration : IEntityTypeConfiguration<ServicePlaylist>
{
    public void Configure(EntityTypeBuilder<ServicePlaylist> builder)
    {
        builder.ToTable("service_playlists");

        builder.HasKey(sp => sp.Id);

        builder.Property(sp => sp.ServiceConnectionId)
            .IsRequired()
            .HasColumnName("service_connection_id");

        builder.Property(sp => sp.ServicePlaylistId)
            .IsRequired()
            .HasMaxLength(255)
            .HasColumnName("service_playlist_id");

        builder.Property(sp => sp.Name)
            .IsRequired()
            .HasMaxLength(255)
            .HasColumnName("name");

        builder.Property(sp => sp.Description)
            .HasMaxLength(1000)
            .HasColumnName("description");

        builder.Property(sp => sp.TrackCount)
            .IsRequired()
            .HasDefaultValue(0)
            .HasColumnName("track_count");

        builder.Property(sp => sp.ImageUrl)
            .HasMaxLength(2048)
            .HasColumnName("image_url");

        builder.Property(sp => sp.OwnerName)
            .HasMaxLength(255)
            .HasColumnName("owner_name");

        builder.Property(sp => sp.ServiceUrl)
            .HasMaxLength(2048)
            .HasColumnName("service_url");

        builder.Property(sp => sp.IsOwnedByUser)
            .IsRequired()
            .HasColumnName("is_owned_by_user");

        builder.Property(sp => sp.IsPublic)
            .IsRequired()
            .HasColumnName("is_public");

        builder.Property(sp => sp.IsCollaborative)
            .IsRequired()
            .HasDefaultValue(false)
            .HasColumnName("is_collaborative");

        builder.Property(sp => sp.SnapshotId)
            .HasMaxLength(255)
            .HasColumnName("snapshot_id");

        builder.Property(sp => sp.LastSyncedAt)
            .HasColumnName("last_synced_at");

        builder.Property(sp => sp.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()")
            .HasColumnName("created_at");

        builder.Property(sp => sp.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()")
            .HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(sp => new { sp.ServiceConnectionId, sp.ServicePlaylistId })
            .IsUnique()
            .HasDatabaseName("idx_service_playlist_connection_playlist");

        builder.HasIndex(sp => sp.Name)
            .HasDatabaseName("idx_service_playlist_name");

        builder.HasIndex(sp => sp.LastSyncedAt)
            .HasDatabaseName("idx_service_playlist_last_synced");

        // Relationships
        builder.HasOne(sp => sp.ServiceConnection)
            .WithMany(sc => sc.ServicePlaylists)
            .HasForeignKey(sp => sp.ServiceConnectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(sp => sp.TrackAssociations)
            .WithOne(pta => pta.ServicePlaylist)
            .HasForeignKey(pta => pta.PlaylistId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
