using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniPlay.Core.Entities;
using UniPlay.Core.Enums;

namespace UniPlay.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for PlaylistTrackAssociation entity
/// </summary>
public class PlaylistTrackAssociationConfiguration : IEntityTypeConfiguration<PlaylistTrackAssociation>
{
    public void Configure(EntityTypeBuilder<PlaylistTrackAssociation> builder)
    {
        builder.ToTable("playlist_track_associations");

        builder.HasKey(pta => pta.Id);

        builder.Property(pta => pta.PlaylistId)
            .IsRequired()
            .HasColumnName("playlist_id");

        builder.Property(pta => pta.PlaylistType)
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion<string>()
            .HasColumnName("playlist_type");

        builder.Property(pta => pta.TrackId)
            .IsRequired()
            .HasColumnName("track_id");

        builder.Property(pta => pta.Position)
            .IsRequired()
            .HasColumnName("position");

        builder.Property(pta => pta.AddedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()")
            .HasColumnName("added_at");

        builder.Property(pta => pta.AddedBy)
            .HasColumnName("added_by");

        // Indexes
        builder.HasIndex(pta => new { pta.PlaylistId, pta.PlaylistType, pta.Position })
            .IsUnique()
            .HasDatabaseName("idx_playlist_track_position");

        builder.HasIndex(pta => pta.TrackId)
            .HasDatabaseName("idx_playlist_track_track");

        builder.HasIndex(pta => pta.AddedAt)
            .HasDatabaseName("idx_playlist_track_added");

        // Relationships
        builder.HasOne(pta => pta.Track)
            .WithMany(t => t.PlaylistAssociations)
            .HasForeignKey(pta => pta.TrackId)
            .OnDelete(DeleteBehavior.Restrict); // Don't delete tracks when removing from playlist

        // Note: PlatformPlaylist and ServicePlaylist relationships are configured
        // using the PlaylistId foreign key, but we can't use HasOne/WithMany here
        // because they share the same foreign key column
    }
}
