using Microsoft.EntityFrameworkCore;
using UniPlay.Core.Entities;

namespace UniPlay.Infrastructure.Data;

/// <summary>
/// Application database context for UniPlay
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // DbSets
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<ServiceConnection> ServiceConnections => Set<ServiceConnection>();
    public DbSet<PlatformPlaylist> PlatformPlaylists => Set<PlatformPlaylist>();
    public DbSet<ServicePlaylist> ServicePlaylists => Set<ServicePlaylist>();
    public DbSet<Track> Tracks => Set<Track>();
    public DbSet<PlaylistTrackAssociation> PlaylistTrackAssociations => Set<PlaylistTrackAssociation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Set default schema
        modelBuilder.HasDefaultSchema("public");
    }
}
