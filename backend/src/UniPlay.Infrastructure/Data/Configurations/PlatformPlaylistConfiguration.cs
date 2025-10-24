using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniPlay.Core.Entities;

namespace UniPlay.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for PlatformPlaylist entity
/// </summary>
public class PlatformPlaylistConfiguration : IEntityTypeConfiguration<PlatformPlaylist>
{
    public void Configure(EntityTypeBuilder<PlatformPlaylist> builder)
    {
        builder.ToTable("platform_playlists");

        builder.HasKey(pp => pp.Id);

        builder.Property(pp => pp.UserId)
            .IsRequired()
            .HasColumnName("user_id");

        builder.Property(pp => pp.Name)
            .IsRequired()
            .HasMaxLength(255)
            .HasColumnName("name");

        builder.Property(pp => pp.Description)
            .HasMaxLength(1000)
            .HasColumnName("description");

        builder.Property(pp => pp.IsPublic)
            .IsRequired()
            .HasDefaultValue(false)
            .HasColumnName("is_public");

        builder.Property(pp => pp.TrackCount)
            .IsRequired()
            .HasDefaultValue(0)
            .HasColumnName("track_count");

        builder.Property(pp => pp.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()")
            .HasColumnName("created_at");

        builder.Property(pp => pp.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()")
            .HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(pp => pp.UserId)
            .HasDatabaseName("idx_platform_playlist_user");

        builder.HasIndex(pp => pp.CreatedAt)
            .HasDatabaseName("idx_platform_playlist_created");

        builder.HasIndex(pp => pp.Name)
            .HasDatabaseName("idx_platform_playlist_name");

        // Relationships
        builder.HasOne(pp => pp.User)
            .WithMany(u => u.PlatformPlaylists)
            .HasForeignKey(pp => pp.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(pp => pp.TrackAssociations)
            .WithOne(pta => pta.PlatformPlaylist)
            .HasForeignKey(pta => pta.PlaylistId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
