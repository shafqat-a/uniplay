using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniPlay.Core.Entities;

namespace UniPlay.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for Track entity
/// </summary>
public class TrackConfiguration : IEntityTypeConfiguration<Track>
{
    public void Configure(EntityTypeBuilder<Track> builder)
    {
        builder.ToTable("tracks");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.ServiceType)
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion<string>()
            .HasColumnName("service_type");

        builder.Property(t => t.ServiceTrackId)
            .IsRequired()
            .HasMaxLength(255)
            .HasColumnName("service_track_id");

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(500)
            .HasColumnName("title");

        builder.Property(t => t.Artist)
            .IsRequired()
            .HasMaxLength(500)
            .HasColumnName("artist");

        builder.Property(t => t.Album)
            .HasMaxLength(500)
            .HasColumnName("album");

        builder.Property(t => t.DurationMs)
            .IsRequired()
            .HasColumnName("duration_ms");

        builder.Property(t => t.ISRC)
            .HasMaxLength(12)
            .HasColumnName("isrc");

        builder.Property(t => t.ImageUrl)
            .HasMaxLength(2048)
            .HasColumnName("image_url");

        builder.Property(t => t.ServiceUrl)
            .HasMaxLength(2048)
            .HasColumnName("service_url");

        builder.Property(t => t.Metadata)
            .HasColumnType("jsonb")
            .HasColumnName("metadata");

        builder.Property(t => t.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()")
            .HasColumnName("created_at");

        builder.Property(t => t.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()")
            .HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(t => new { t.ServiceType, t.ServiceTrackId })
            .IsUnique()
            .HasDatabaseName("idx_track_service");

        builder.HasIndex(t => t.ISRC)
            .HasFilter("isrc IS NOT NULL")
            .HasDatabaseName("idx_track_isrc");

        // Full-text search index (PostgreSQL GIN)
        // Note: This requires manual SQL migration as EF Core doesn't support GIN indexes directly
        // CREATE INDEX idx_track_search ON tracks USING GIN(to_tsvector('english', title || ' ' || artist || ' ' || COALESCE(album, '')));

        // Relationships
        builder.HasMany(t => t.PlaylistAssociations)
            .WithOne(pta => pta.Track)
            .HasForeignKey(pta => pta.TrackId)
            .OnDelete(DeleteBehavior.Restrict); // Don't delete tracks when removing from playlist
    }
}
