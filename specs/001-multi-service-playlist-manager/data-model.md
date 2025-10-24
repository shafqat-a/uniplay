# Data Model: Multi-Service Playlist Manager

**Feature**: Multi-Service Playlist Manager
**Date**: 2025-10-24
**Purpose**: Define all entities, relationships, and database schema for the UniPlay platform

## Overview

This document defines the complete data model for the Multi-Service Playlist Manager. The model supports:
- Multi-account service connections (up to 3 per service type)
- Platform-native playlists with cross-service tracks
- Service playlist references with bidirectional sync
- Background job tracking for copy/export/sync operations
- Token encryption and secure credential storage

**Database**: PostgreSQL 16 with Entity Framework Core 9.0

---

## Entity Relationship Diagram

```
┌─────────────────┐
│   UserAccount   │
└────────┬────────┘
         │ 1
         │
         │ n
┌────────▼────────────────┐
│  ServiceConnection      │
└─────────────┬───────────┘
              │ 1
              │
              │ n
       ┌──────┴──────┐
       │             │
       │ n           │ n
┌──────▼──────┐  ┌──▼────────────┐
│ServicePlaylist│ │PlatformPlaylist│
└──────┬──────┘  └──┬────────────┘
       │ n          │ 1
       │            │
       │            │ n
       │      ┌─────▼──────────────┐
       └──────►PlaylistTrackAssoc  │
              └─────┬──────────────┘
                    │ n
                    │
                    │ 1
              ┌─────▼──────┐
              │   Track    │
              └────────────┘

┌─────────────────┐
│   SyncQueue     │  (References ServiceConnection, ServicePlaylist)
└─────────────────┘

┌─────────────────┐
│  CopyOperation  │  (References ServiceConnection, tracks progress)
└─────────────────┘
```

---

## Core Entities

### 1. UserAccount

Represents a registered user of the UniPlay platform.

**Purpose**: Stores user authentication credentials and preferences.

**Fields**:
| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | Guid | PK | Unique user identifier |
| Email | string(320) | NOT NULL, UNIQUE | User email (RFC 5321 max length) |
| EmailConfirmed | bool | NOT NULL, DEFAULT false | Email verification status |
| PasswordHash | string(128) | NOT NULL | Hashed password (ASP.NET Core Identity) |
| SecurityStamp | string(64) | NOT NULL | For token invalidation |
| CreatedAt | DateTime | NOT NULL, DEFAULT NOW() | Account creation timestamp (UTC) |
| UpdatedAt | DateTime | NOT NULL, DEFAULT NOW() | Last modification timestamp (UTC) |
| LastLoginAt | DateTime? | NULL | Last successful login (UTC) |

**Validation Rules** (FR-001):
- Email must be valid email format
- Password minimum 8 characters, requires uppercase, lowercase, digit, special char

**Relationships**:
- One-to-many with `ServiceConnection`
- One-to-many with `PlatformPlaylist`

**Indexes**:
```sql
CREATE UNIQUE INDEX idx_user_email ON user_accounts(email);
CREATE INDEX idx_user_created_at ON user_accounts(created_at DESC);
```

**EF Core Configuration**:
```csharp
public class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(320);

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.CreatedAt)
            .HasDefaultValueSql("NOW()");
    }
}
```

---

### 2. ServiceConnection

Links a user to a specific music service account (Spotify, YouTube Music, Apple Music, Deezer).

**Purpose**: Stores OAuth credentials and connection status for each connected service.

**Fields**:
| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | Guid | PK | Unique connection identifier |
| UserId | Guid | FK, NOT NULL | References UserAccount.Id |
| ServiceType | string(50) | NOT NULL | Enum: "Spotify", "AppleMusic", "Deezer", "YouTubeMusic" |
| ServiceAccountId | string(255) | NOT NULL | User's ID on the external service |
| ServiceAccountEmail | string(320) | NULL | Account email (if available from service) |
| ServiceAccountDisplayName | string(255) | NULL | Display name from service |
| ServiceAccountProfileImageUrl | string(2048) | NULL | Profile picture URL |
| EncryptedAccessToken | string(1024) | NOT NULL | Encrypted OAuth access token |
| EncryptedRefreshToken | string(1024) | NULL | Encrypted OAuth refresh token (if available) |
| AccessTokenExpiresAt | DateTime | NOT NULL | Access token expiration (UTC) |
| Scopes | string(512) | NOT NULL | OAuth scopes granted (space-separated) |
| ConnectionStatus | string(50) | NOT NULL | Enum: "Active", "Expired", "Revoked", "Error" |
| LastSyncedAt | DateTime? | NULL | Last successful sync timestamp (UTC) |
| CreatedAt | DateTime | NOT NULL, DEFAULT NOW() | Connection creation timestamp (UTC) |
| UpdatedAt | DateTime | NOT NULL, DEFAULT NOW() | Last modification timestamp (UTC) |

**Validation Rules** (FR-002, FR-003):
- User can have maximum 3 connections per ServiceType
- ServiceType must be one of: Spotify, AppleMusic, Deezer, YouTubeMusic
- ConnectionStatus must be one of: Active, Expired, Revoked, Error
- Tokens encrypted using ASP.NET Core Data Protection API + Azure Key Vault

**Relationships**:
- Many-to-one with `UserAccount`
- One-to-many with `ServicePlaylist`
- One-to-many with `SyncQueue`
- One-to-many with `CopyOperation`

**Indexes**:
```sql
CREATE INDEX idx_service_connection_user_service ON service_connections(user_id, service_type);
CREATE INDEX idx_service_connection_status ON service_connections(connection_status);
CREATE INDEX idx_service_connection_expiration ON service_connections(access_token_expires_at)
    WHERE connection_status = 'Active';
```

**Unique Constraint** (FR-003):
```sql
-- Enforce max 3 accounts per service type per user (application-level validation)
-- Postgres doesn't support partial unique constraints on count, so use CHECK in app layer
```

**EF Core Configuration**:
```csharp
public class ServiceConnectionConfiguration : IEntityTypeConfiguration<ServiceConnection>
{
    public void Configure(EntityTypeBuilder<ServiceConnection> builder)
    {
        builder.HasKey(sc => sc.Id);

        builder.Property(sc => sc.ServiceType)
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion<string>();

        builder.Property(sc => sc.ConnectionStatus)
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion<string>();

        // Encrypt tokens using Data Protection API
        builder.Property(sc => sc.EncryptedAccessToken)
            .IsRequired()
            .HasMaxLength(1024)
            .HasConversion(
                v => _dataProtector.Protect(v),
                v => _dataProtector.Unprotect(v));

        builder.HasOne(sc => sc.User)
            .WithMany(u => u.ServiceConnections)
            .HasForeignKey(sc => sc.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(sc => new { sc.UserId, sc.ServiceType });
    }
}
```

**State Transitions** (FR-021):
```
Active → Expired (when AccessTokenExpiresAt < NOW and refresh fails)
Active → Revoked (when user revokes access on service)
Active → Error (when service API returns persistent error)
Expired → Active (when token successfully refreshed)
Revoked → Active (when user re-authorizes)
```

---

### 3. PlatformPlaylist

A playlist created within UniPlay that can contain tracks from any connected service.

**Purpose**: Enables cross-service playlists managed by the platform.

**Fields**:
| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | Guid | PK | Unique playlist identifier |
| UserId | Guid | FK, NOT NULL | References UserAccount.Id (owner) |
| Name | string(255) | NOT NULL | Playlist name |
| Description | string(1000) | NULL | User-provided description |
| IsPublic | bool | NOT NULL, DEFAULT false | Visibility (future: sharing feature) |
| TrackCount | int | NOT NULL, DEFAULT 0 | Denormalized count for performance |
| CreatedAt | DateTime | NOT NULL, DEFAULT NOW() | Creation timestamp (UTC) |
| UpdatedAt | DateTime | NOT NULL, DEFAULT NOW() | Last modification timestamp (UTC) |

**Validation Rules** (FR-006, FR-007, FR-008):
- Name required, 1-255 characters
- Description optional, max 1000 characters
- Supports tracks from multiple services (via Track.ServiceType)

**Relationships**:
- Many-to-one with `UserAccount`
- One-to-many with `PlaylistTrackAssociation`

**Indexes**:
```sql
CREATE INDEX idx_platform_playlist_user ON platform_playlists(user_id);
CREATE INDEX idx_platform_playlist_updated ON platform_playlists(updated_at DESC);
CREATE INDEX idx_platform_playlist_name ON platform_playlists USING GIN(to_tsvector('english', name));
```

**EF Core Configuration**:
```csharp
public class PlatformPlaylistConfiguration : IEntityTypeConfiguration<PlatformPlaylist>
{
    public void Configure(EntityTypeBuilder<PlatformPlaylist> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(p => p.Description)
            .HasMaxLength(1000);

        builder.HasOne(p => p.User)
            .WithMany(u => u.PlatformPlaylists)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Automatically update TrackCount
        builder.Property(p => p.TrackCount)
            .HasComputedColumnSql("(SELECT COUNT(*) FROM playlist_track_associations WHERE playlist_id = id AND playlist_type = 'Platform')", stored: false);
    }
}
```

---

### 4. ServicePlaylist

A reference to a playlist that exists on a connected music service.

**Purpose**: Mirrors service playlists for unified dashboard view and sync operations.

**Fields**:
| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | Guid | PK | Unique internal identifier |
| ServiceConnectionId | Guid | FK, NOT NULL | References ServiceConnection.Id |
| ServicePlaylistId | string(255) | NOT NULL | Playlist ID from the music service |
| Name | string(255) | NOT NULL | Playlist name from service |
| Description | string(1000) | NULL | Playlist description from service |
| ServiceOwnerId | string(255) | NOT NULL | User ID who owns playlist on service |
| IsOwnedByUser | bool | NOT NULL | True if user owns, false if following |
| TrackCount | int | NOT NULL | Number of tracks (from service) |
| ImageUrl | string(2048) | NULL | Playlist cover image URL |
| ServiceUrl | string(2048) | NULL | Direct link to playlist on service |
| LastSyncedAt | DateTime? | NULL | Last successful sync timestamp (UTC) |
| SyncStatus | string(50) | NOT NULL | Enum: "Synced", "Pending", "Failed", "Deleted" |
| CreatedAt | DateTime | NOT NULL, DEFAULT NOW() | First import timestamp (UTC) |
| UpdatedAt | DateTime | NOT NULL, DEFAULT NOW() | Last modification timestamp (UTC) |

**Validation Rules** (FR-004, FR-005, FR-009, FR-020):
- ServicePlaylistId + ServiceConnectionId must be unique
- SyncStatus must be one of: Synced, Pending, Failed, Deleted
- Only playlists owned by user can be edited (IsOwnedByUser = true)

**Relationships**:
- Many-to-one with `ServiceConnection`
- One-to-many with `PlaylistTrackAssociation`
- One-to-many with `SyncQueue`

**Indexes**:
```sql
CREATE UNIQUE INDEX idx_service_playlist_external ON service_playlists(service_connection_id, service_playlist_id);
CREATE INDEX idx_service_playlist_sync_status ON service_playlists(sync_status);
CREATE INDEX idx_service_playlist_last_synced ON service_playlists(last_synced_at) WHERE sync_status = 'Synced';
```

**EF Core Configuration**:
```csharp
public class ServicePlaylistConfiguration : IEntityTypeConfiguration<ServicePlaylist>
{
    public void Configure(EntityTypeBuilder<ServicePlaylist> builder)
    {
        builder.HasKey(sp => sp.Id);

        builder.Property(sp => sp.SyncStatus)
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion<string>();

        builder.HasOne(sp => sp.ServiceConnection)
            .WithMany(sc => sc.ServicePlaylists)
            .HasForeignKey(sp => sp.ServiceConnectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(sp => new { sp.ServiceConnectionId, sp.ServicePlaylistId })
            .IsUnique();
    }
}
```

**Sync Behavior** (FR-020):
- Background job runs every 6 hours for active users
- On-demand sync when user views playlist
- Detects deletions on service (SyncStatus = "Deleted")

---

### 5. Track

Represents a music track with metadata from a specific service.

**Purpose**: Stores track information for platform playlists and caching service track data.

**Fields**:
| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | Guid | PK | Unique internal track identifier |
| ServiceType | string(50) | NOT NULL | Enum: "Spotify", "AppleMusic", "Deezer", "YouTubeMusic" |
| ServiceTrackId | string(255) | NOT NULL | Track ID from the music service |
| Title | string(500) | NOT NULL | Track title |
| Artist | string(500) | NOT NULL | Primary artist name |
| Album | string(500) | NULL | Album name |
| DurationMs | int | NOT NULL | Track duration in milliseconds |
| ISRC | string(12) | NULL | International Standard Recording Code |
| ImageUrl | string(2048) | NULL | Album/track artwork URL |
| ServiceUrl | string(2048) | NULL | Direct link to track on service |
| Metadata | jsonb | NULL | Additional service-specific metadata (PostgreSQL JSONB) |
| CreatedAt | DateTime | NOT NULL, DEFAULT NOW() | First cache timestamp (UTC) |
| UpdatedAt | DateTime | NOT NULL, DEFAULT NOW() | Last update timestamp (UTC) |

**Validation Rules**:
- ServiceType + ServiceTrackId must be unique (per-service track identity)
- ISRC format: 2-letter country code + 3-char registrant + 2-digit year + 5-digit designation (e.g., "USRC17607839")
- Title, Artist required (minimum 1 character)

**Relationships**:
- One-to-many with `PlaylistTrackAssociation`

**Indexes**:
```sql
CREATE UNIQUE INDEX idx_track_service ON tracks(service_type, service_track_id);
CREATE INDEX idx_track_isrc ON tracks(isrc) WHERE isrc IS NOT NULL;
CREATE INDEX idx_track_search ON tracks USING GIN(
    to_tsvector('english', title || ' ' || artist || ' ' || COALESCE(album, ''))
);
CREATE INDEX idx_track_metadata ON tracks USING GIN(metadata jsonb_path_ops);
```

**Metadata JSONB Examples**:
```json
// Spotify
{
  "popularity": 87,
  "explicit": false,
  "preview_url": "https://...",
  "genres": ["pop", "dance"]
}

// Apple Music
{
  "composerName": "Taylor Swift",
  "contentRating": "clean",
  "playParams": {...}
}
```

**EF Core Configuration**:
```csharp
public class TrackConfiguration : IEntityTypeConfiguration<Track>
{
    public void Configure(EntityTypeBuilder<Track> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.ServiceType)
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion<string>();

        builder.Property(t => t.Metadata)
            .HasColumnType("jsonb");

        builder.HasIndex(t => new { t.ServiceType, t.ServiceTrackId })
            .IsUnique();

        builder.HasIndex(t => t.ISRC)
            .HasFilter("isrc IS NOT NULL");
    }
}
```

**Cache Strategy** (Research Decision #4):
- Cache hot tracks (20% = 80% of searches)
- LRU eviction when memory pressure
- 6-hour TTL for track metadata

---

### 6. PlaylistTrackAssociation

Links tracks to playlists (both platform and service playlists) with ordering information.

**Purpose**: Many-to-many relationship between playlists and tracks, maintains track order.

**Fields**:
| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | Guid | PK | Unique association identifier |
| PlaylistId | Guid | FK, NOT NULL | References PlatformPlaylist.Id or ServicePlaylist.Id |
| PlaylistType | string(50) | NOT NULL | Enum: "Platform", "Service" |
| TrackId | Guid | FK, NOT NULL | References Track.Id |
| Position | int | NOT NULL | Zero-based position in playlist |
| AddedAt | DateTime | NOT NULL, DEFAULT NOW() | When track was added (UTC) |
| AddedBy | Guid? | NULL | References UserAccount.Id (for collaborative future) |

**Validation Rules** (FR-007, FR-010, FR-011):
- PlaylistId + Position must be unique per playlist
- Position must be sequential (0, 1, 2, ...)
- For ServicePlaylist: Track.ServiceType must match ServiceConnection.ServiceType
- For PlatformPlaylist: Track can be from any service

**Relationships**:
- Many-to-one with `PlatformPlaylist` (when PlaylistType = "Platform")
- Many-to-one with `ServicePlaylist` (when PlaylistType = "Service")
- Many-to-one with `Track`

**Indexes**:
```sql
CREATE UNIQUE INDEX idx_playlist_track_position ON playlist_track_associations(playlist_id, playlist_type, position);
CREATE INDEX idx_playlist_track_track ON playlist_track_associations(track_id);
CREATE INDEX idx_playlist_track_added ON playlist_track_associations(added_at DESC);
```

**EF Core Configuration**:
```csharp
public class PlaylistTrackAssociationConfiguration : IEntityTypeConfiguration<PlaylistTrackAssociation>
{
    public void Configure(EntityTypeBuilder<PlaylistTrackAssociation> builder)
    {
        builder.HasKey(pta => pta.Id);

        builder.Property(pta => pta.PlaylistType)
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion<string>();

        builder.HasOne(pta => pta.Track)
            .WithMany()
            .HasForeignKey(pta => pta.TrackId)
            .OnDelete(DeleteBehavior.Restrict); // Don't delete tracks when removing from playlist

        builder.HasIndex(pta => new { pta.PlaylistId, pta.PlaylistType, pta.Position })
            .IsUnique();
    }
}
```

**Reordering Logic** (FR-007):
```csharp
// When dragging track from position 0 to position 2:
// 1. Update dragged track: position = 2
// 2. Decrement positions for tracks 1-2: positions become 0-1
// Use transaction to ensure atomicity
```

---

### 7. SyncQueue

Stores pending operations that need to be synchronized to music services.

**Purpose**: Queue for edit/add/delete operations on service playlists with retry logic.

**Fields**:
| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | Guid | PK | Unique queue item identifier |
| ServiceConnectionId | Guid | FK, NOT NULL | References ServiceConnection.Id |
| ServicePlaylistId | Guid | FK, NULL | References ServicePlaylist.Id (null for connection-level ops) |
| OperationType | string(50) | NOT NULL | Enum: "AddTrack", "RemoveTrack", "ReorderTrack", "RenamePlaylist", "DeletePlaylist" |
| OperationPayload | jsonb | NOT NULL | Operation-specific data (PostgreSQL JSONB) |
| Status | string(50) | NOT NULL | Enum: "Pending", "Processing", "Completed", "Failed", "Cancelled" |
| Priority | int | NOT NULL, DEFAULT 5 | 1-10 priority (1 = highest) |
| RetryCount | int | NOT NULL, DEFAULT 0 | Number of retry attempts |
| MaxRetries | int | NOT NULL, DEFAULT 5 | Maximum retry attempts |
| NextRetryAt | DateTime? | NULL | Next scheduled retry (UTC) |
| ErrorMessage | string(2000) | NULL | Last error message |
| CreatedAt | DateTime | NOT NULL, DEFAULT NOW() | Queue entry creation (UTC) |
| CompletedAt | DateTime? | NULL | Completion timestamp (UTC) |

**Validation Rules** (FR-022, FR-023):
- OperationType must be one of: AddTrack, RemoveTrack, ReorderTrack, RenamePlaylist, DeletePlaylist
- Status must be one of: Pending, Processing, Completed, Failed, Cancelled
- Priority range: 1-10
- Exponential backoff: RetryDelay = BaseDelay * 2^RetryCount (max 5 minutes)

**OperationPayload Examples**:
```json
// AddTrack
{
  "track_id": "spotify:track:abc123",
  "position": 5
}

// RemoveTrack
{
  "track_id": "spotify:track:abc123"
}

// ReorderTrack
{
  "track_id": "spotify:track:abc123",
  "from_position": 2,
  "to_position": 5
}

// RenamePlaylist
{
  "new_name": "My Updated Playlist"
}
```

**Relationships**:
- Many-to-one with `ServiceConnection`
- Many-to-one with `ServicePlaylist` (nullable)

**Indexes**:
```sql
CREATE INDEX idx_sync_queue_status_priority ON sync_queue(status, priority DESC) WHERE status IN ('Pending', 'Processing');
CREATE INDEX idx_sync_queue_next_retry ON sync_queue(next_retry_at) WHERE status = 'Pending' AND next_retry_at IS NOT NULL;
CREATE INDEX idx_sync_queue_service_connection ON sync_queue(service_connection_id);
```

**EF Core Configuration**:
```csharp
public class SyncQueueConfiguration : IEntityTypeConfiguration<SyncQueue>
{
    public void Configure(EntityTypeBuilder<SyncQueue> builder)
    {
        builder.HasKey(sq => sq.Id);

        builder.Property(sq => sq.OperationType)
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion<string>();

        builder.Property(sq => sq.Status)
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion<string>();

        builder.Property(sq => sq.OperationPayload)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.HasOne(sq => sq.ServiceConnection)
            .WithMany()
            .HasForeignKey(sq => sq.ServiceConnectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(sq => new { sq.Status, sq.Priority })
            .HasFilter("status IN ('Pending', 'Processing')");
    }
}
```

**Retry Logic** (Hangfire Job):
```csharp
BackgroundJob.Schedule<SyncQueueProcessor>(
    x => x.ProcessQueueItem(queueItemId),
    TimeSpan.FromSeconds(Math.Min(300, 2 * Math.Pow(2, retryCount))) // Max 5 minutes
);
```

---

### 8. CopyOperation

Represents a cross-service playlist copy job with progress tracking.

**Purpose**: Track playlist copy operations from one service to another, report matching results.

**Fields**:
| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | Guid | PK | Unique operation identifier |
| UserId | Guid | FK, NOT NULL | References UserAccount.Id |
| SourceServiceConnectionId | Guid | FK, NOT NULL | References ServiceConnection.Id (source) |
| DestinationServiceConnectionId | Guid | FK, NOT NULL | References ServiceConnection.Id (destination) |
| SourcePlaylistId | Guid | FK, NOT NULL | References ServicePlaylist.Id (source) |
| DestinationPlaylistId | Guid? | FK, NULL | References ServicePlaylist.Id (created destination) |
| Status | string(50) | NOT NULL | Enum: "Pending", "Matching", "Creating", "Completed", "Failed", "Cancelled" |
| TotalTracks | int | NOT NULL | Total tracks to copy |
| ProcessedTracks | int | NOT NULL, DEFAULT 0 | Tracks processed so far |
| MatchedTracks | int | NOT NULL, DEFAULT 0 | Successfully matched tracks |
| UnmatchedTracks | int | NOT NULL, DEFAULT 0 | Tracks not found on destination |
| UnmatchedTrackDetails | jsonb | NULL | List of unmatched tracks with details |
| ErrorMessage | string(2000) | NULL | Error message if failed |
| ProgressPercent | int | NOT NULL, DEFAULT 0 | 0-100 completion percentage |
| CreatedAt | DateTime | NOT NULL, DEFAULT NOW() | Operation start (UTC) |
| CompletedAt | DateTime? | NULL | Operation completion (UTC) |

**Validation Rules** (FR-012, FR-013):
- Status must be one of: Pending, Matching, Creating, Completed, Failed, Cancelled
- SourceServiceConnectionId ≠ DestinationServiceConnectionId (must copy to different service)
- ProgressPercent range: 0-100
- TotalTracks = MatchedTracks + UnmatchedTracks (when completed)

**UnmatchedTrackDetails JSONB Example**:
```json
[
  {
    "track_id": "spotify:track:abc123",
    "title": "Rare Track",
    "artist": "Indie Artist",
    "reason": "Not found on Apple Music",
    "search_query": "Rare Track Indie Artist",
    "confidence": 0.45
  }
]
```

**Relationships**:
- Many-to-one with `UserAccount`
- Many-to-one with `ServiceConnection` (source, destination)
- Many-to-one with `ServicePlaylist` (source)
- One-to-one with `ServicePlaylist` (destination, nullable until created)

**Indexes**:
```sql
CREATE INDEX idx_copy_operation_user ON copy_operations(user_id);
CREATE INDEX idx_copy_operation_status ON copy_operations(status);
CREATE INDEX idx_copy_operation_created ON copy_operations(created_at DESC);
```

**EF Core Configuration**:
```csharp
public class CopyOperationConfiguration : IEntityTypeConfiguration<CopyOperation>
{
    public void Configure(EntityTypeBuilder<CopyOperation> builder)
    {
        builder.HasKey(co => co.Id);

        builder.Property(co => co.Status)
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion<string>();

        builder.Property(co => co.UnmatchedTrackDetails)
            .HasColumnType("jsonb");

        builder.HasOne(co => co.User)
            .WithMany()
            .HasForeignKey(co => co.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ServiceConnection>()
            .WithMany()
            .HasForeignKey(co => co.SourceServiceConnectionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ServiceConnection>()
            .WithMany()
            .HasForeignKey(co => co.DestinationServiceConnectionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

**State Transitions**:
```
Pending → Matching (start track matching)
Matching → Creating (creating destination playlist)
Creating → Completed (all tracks added successfully)
Creating → Completed (partial success with unmatched tracks)
Any → Failed (unrecoverable error)
Any → Cancelled (user cancellation)
```

**Real-Time Progress** (SignalR):
```csharp
await _hubContext.Clients.User(userId).SendAsync("CopyProgress", new
{
    OperationId = operationId,
    Status = "Matching",
    ProgressPercent = 45,
    ProcessedTracks = 45,
    TotalTracks = 100
});
```

---

## Enumerations

### ServiceType
```csharp
public enum ServiceType
{
    Spotify,
    AppleMusic,
    Deezer,
    YouTubeMusic
}
```

### ConnectionStatus
```csharp
public enum ConnectionStatus
{
    Active,      // Token valid, connection working
    Expired,     // Token expired, needs refresh
    Revoked,     // User revoked access on service
    Error        // Persistent API error
}
```

### SyncStatus
```csharp
public enum SyncStatus
{
    Synced,      // Up-to-date with service
    Pending,     // Sync scheduled/in progress
    Failed,      // Sync failed (will retry)
    Deleted      // Deleted on service
}
```

### PlaylistType
```csharp
public enum PlaylistType
{
    Platform,    // Platform-native playlist
    Service      // Service playlist reference
}
```

### SyncOperationType
```csharp
public enum SyncOperationType
{
    AddTrack,
    RemoveTrack,
    ReorderTrack,
    RenamePlaylist,
    DeletePlaylist
}
```

### SyncQueueStatus
```csharp
public enum SyncQueueStatus
{
    Pending,     // Waiting to process
    Processing,  // Currently processing
    Completed,   // Successfully completed
    Failed,      // Failed after retries
    Cancelled    // User/system cancelled
}
```

### CopyOperationStatus
```csharp
public enum CopyOperationStatus
{
    Pending,     // Queued
    Matching,    // Finding matching tracks
    Creating,    // Creating destination playlist
    Completed,   // Finished (with or without unmatched)
    Failed,      // Unrecoverable error
    Cancelled    // User cancelled
}
```

---

## Database Schema Script (PostgreSQL)

```sql
-- Enable UUID extension
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- UserAccounts table
CREATE TABLE user_accounts (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    email VARCHAR(320) NOT NULL UNIQUE,
    email_confirmed BOOLEAN NOT NULL DEFAULT false,
    password_hash VARCHAR(128) NOT NULL,
    security_stamp VARCHAR(64) NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    last_login_at TIMESTAMP
);

CREATE UNIQUE INDEX idx_user_email ON user_accounts(email);
CREATE INDEX idx_user_created_at ON user_accounts(created_at DESC);

-- ServiceConnections table
CREATE TABLE service_connections (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES user_accounts(id) ON DELETE CASCADE,
    service_type VARCHAR(50) NOT NULL,
    service_account_id VARCHAR(255) NOT NULL,
    service_account_email VARCHAR(320),
    service_account_display_name VARCHAR(255),
    service_account_profile_image_url VARCHAR(2048),
    encrypted_access_token VARCHAR(1024) NOT NULL,
    encrypted_refresh_token VARCHAR(1024),
    access_token_expires_at TIMESTAMP NOT NULL,
    scopes VARCHAR(512) NOT NULL,
    connection_status VARCHAR(50) NOT NULL,
    last_synced_at TIMESTAMP,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),

    CONSTRAINT chk_service_type CHECK (service_type IN ('Spotify', 'AppleMusic', 'Deezer', 'YouTubeMusic')),
    CONSTRAINT chk_connection_status CHECK (connection_status IN ('Active', 'Expired', 'Revoked', 'Error'))
);

CREATE INDEX idx_service_connection_user_service ON service_connections(user_id, service_type);
CREATE INDEX idx_service_connection_status ON service_connections(connection_status);
CREATE INDEX idx_service_connection_expiration ON service_connections(access_token_expires_at)
    WHERE connection_status = 'Active';

-- PlatformPlaylists table
CREATE TABLE platform_playlists (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES user_accounts(id) ON DELETE CASCADE,
    name VARCHAR(255) NOT NULL,
    description VARCHAR(1000),
    is_public BOOLEAN NOT NULL DEFAULT false,
    track_count INT NOT NULL DEFAULT 0,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_platform_playlist_user ON platform_playlists(user_id);
CREATE INDEX idx_platform_playlist_updated ON platform_playlists(updated_at DESC);
CREATE INDEX idx_platform_playlist_name ON platform_playlists USING GIN(to_tsvector('english', name));

-- ServicePlaylists table
CREATE TABLE service_playlists (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    service_connection_id UUID NOT NULL REFERENCES service_connections(id) ON DELETE CASCADE,
    service_playlist_id VARCHAR(255) NOT NULL,
    name VARCHAR(255) NOT NULL,
    description VARCHAR(1000),
    service_owner_id VARCHAR(255) NOT NULL,
    is_owned_by_user BOOLEAN NOT NULL,
    track_count INT NOT NULL,
    image_url VARCHAR(2048),
    service_url VARCHAR(2048),
    last_synced_at TIMESTAMP,
    sync_status VARCHAR(50) NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),

    CONSTRAINT chk_sync_status CHECK (sync_status IN ('Synced', 'Pending', 'Failed', 'Deleted'))
);

CREATE UNIQUE INDEX idx_service_playlist_external ON service_playlists(service_connection_id, service_playlist_id);
CREATE INDEX idx_service_playlist_sync_status ON service_playlists(sync_status);
CREATE INDEX idx_service_playlist_last_synced ON service_playlists(last_synced_at) WHERE sync_status = 'Synced';

-- Tracks table
CREATE TABLE tracks (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    service_type VARCHAR(50) NOT NULL,
    service_track_id VARCHAR(255) NOT NULL,
    title VARCHAR(500) NOT NULL,
    artist VARCHAR(500) NOT NULL,
    album VARCHAR(500),
    duration_ms INT NOT NULL,
    isrc VARCHAR(12),
    image_url VARCHAR(2048),
    service_url VARCHAR(2048),
    metadata JSONB,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),

    CONSTRAINT chk_track_service_type CHECK (service_type IN ('Spotify', 'AppleMusic', 'Deezer', 'YouTubeMusic'))
);

CREATE UNIQUE INDEX idx_track_service ON tracks(service_type, service_track_id);
CREATE INDEX idx_track_isrc ON tracks(isrc) WHERE isrc IS NOT NULL;
CREATE INDEX idx_track_search ON tracks USING GIN(to_tsvector('english', title || ' ' || artist || ' ' || COALESCE(album, '')));
CREATE INDEX idx_track_metadata ON tracks USING GIN(metadata jsonb_path_ops);

-- PlaylistTrackAssociations table
CREATE TABLE playlist_track_associations (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    playlist_id UUID NOT NULL,
    playlist_type VARCHAR(50) NOT NULL,
    track_id UUID NOT NULL REFERENCES tracks(id) ON DELETE RESTRICT,
    position INT NOT NULL,
    added_at TIMESTAMP NOT NULL DEFAULT NOW(),
    added_by UUID REFERENCES user_accounts(id) ON DELETE SET NULL,

    CONSTRAINT chk_playlist_type CHECK (playlist_type IN ('Platform', 'Service'))
);

CREATE UNIQUE INDEX idx_playlist_track_position ON playlist_track_associations(playlist_id, playlist_type, position);
CREATE INDEX idx_playlist_track_track ON playlist_track_associations(track_id);
CREATE INDEX idx_playlist_track_added ON playlist_track_associations(added_at DESC);

-- SyncQueue table
CREATE TABLE sync_queue (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    service_connection_id UUID NOT NULL REFERENCES service_connections(id) ON DELETE CASCADE,
    service_playlist_id UUID REFERENCES service_playlists(id) ON DELETE CASCADE,
    operation_type VARCHAR(50) NOT NULL,
    operation_payload JSONB NOT NULL,
    status VARCHAR(50) NOT NULL,
    priority INT NOT NULL DEFAULT 5,
    retry_count INT NOT NULL DEFAULT 0,
    max_retries INT NOT NULL DEFAULT 5,
    next_retry_at TIMESTAMP,
    error_message VARCHAR(2000),
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    completed_at TIMESTAMP,

    CONSTRAINT chk_operation_type CHECK (operation_type IN ('AddTrack', 'RemoveTrack', 'ReorderTrack', 'RenamePlaylist', 'DeletePlaylist')),
    CONSTRAINT chk_sync_queue_status CHECK (status IN ('Pending', 'Processing', 'Completed', 'Failed', 'Cancelled')),
    CONSTRAINT chk_priority CHECK (priority BETWEEN 1 AND 10)
);

CREATE INDEX idx_sync_queue_status_priority ON sync_queue(status, priority DESC) WHERE status IN ('Pending', 'Processing');
CREATE INDEX idx_sync_queue_next_retry ON sync_queue(next_retry_at) WHERE status = 'Pending' AND next_retry_at IS NOT NULL;
CREATE INDEX idx_sync_queue_service_connection ON sync_queue(service_connection_id);

-- CopyOperations table
CREATE TABLE copy_operations (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES user_accounts(id) ON DELETE CASCADE,
    source_service_connection_id UUID NOT NULL REFERENCES service_connections(id) ON DELETE RESTRICT,
    destination_service_connection_id UUID NOT NULL REFERENCES service_connections(id) ON DELETE RESTRICT,
    source_playlist_id UUID NOT NULL REFERENCES service_playlists(id) ON DELETE RESTRICT,
    destination_playlist_id UUID REFERENCES service_playlists(id) ON DELETE SET NULL,
    status VARCHAR(50) NOT NULL,
    total_tracks INT NOT NULL,
    processed_tracks INT NOT NULL DEFAULT 0,
    matched_tracks INT NOT NULL DEFAULT 0,
    unmatched_tracks INT NOT NULL DEFAULT 0,
    unmatched_track_details JSONB,
    error_message VARCHAR(2000),
    progress_percent INT NOT NULL DEFAULT 0,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    completed_at TIMESTAMP,

    CONSTRAINT chk_copy_operation_status CHECK (status IN ('Pending', 'Matching', 'Creating', 'Completed', 'Failed', 'Cancelled')),
    CONSTRAINT chk_progress_percent CHECK (progress_percent BETWEEN 0 AND 100)
);

CREATE INDEX idx_copy_operation_user ON copy_operations(user_id);
CREATE INDEX idx_copy_operation_status ON copy_operations(status);
CREATE INDEX idx_copy_operation_created ON copy_operations(created_at DESC);

-- Update triggers for updated_at columns
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER update_user_accounts_updated_at BEFORE UPDATE ON user_accounts
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_service_connections_updated_at BEFORE UPDATE ON service_connections
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_platform_playlists_updated_at BEFORE UPDATE ON platform_playlists
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_service_playlists_updated_at BEFORE UPDATE ON service_playlists
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_tracks_updated_at BEFORE UPDATE ON tracks
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
```

---

## Data Validation Rules Summary

| Entity | Rule | Functional Requirement |
|--------|------|------------------------|
| UserAccount | Valid email format, unique | FR-001 |
| UserAccount | Password: min 8 chars, complexity | FR-001 |
| ServiceConnection | Max 3 connections per service type per user | FR-003 |
| ServiceConnection | Tokens encrypted with Data Protection API | FR-027 |
| ServiceConnection | Auto-refresh 5min before expiration | FR-021 |
| PlatformPlaylist | Name: 1-255 chars, required | FR-006 |
| PlatformPlaylist | Can contain tracks from any service | FR-006 |
| ServicePlaylist | ServicePlaylistId + ServiceConnectionId unique | FR-004 |
| ServicePlaylist | Only owned playlists editable | FR-009, FR-010 |
| Track | ServiceType + ServiceTrackId unique | - |
| Track | ISRC format validation (if present) | - |
| PlaylistTrackAssociation | Service playlists: Track.ServiceType matches connection | FR-011 |
| PlaylistTrackAssociation | Platform playlists: Any service allowed | FR-006 |
| PlaylistTrackAssociation | Sequential positions (no gaps) | FR-007 |
| SyncQueue | Exponential backoff (max 5min delay) | FR-022 |
| SyncQueue | Max 5 retries before marking Failed | FR-023 |
| CopyOperation | Source ≠ Destination service | FR-012 |
| CopyOperation | Report unmatched tracks | FR-013 |

---

## Performance Considerations

### Indexes Strategy
- **Composite indexes** for common query patterns (user_id + service_type)
- **Partial indexes** for filtered queries (WHERE status = 'Active')
- **GIN indexes** for full-text search (playlist names, track metadata)
- **JSONB indexes** for querying metadata fields

### Denormalization
- `TrackCount` on playlists (avoid COUNT(*) on large tables)
- Cache hot track metadata (20% of tracks = 80% of requests)

### Query Optimization
- **Dashboard query** (SC-005: <3 seconds for 100 playlists):
  ```sql
  SELECT p.*, COUNT(pta.id) as track_count
  FROM platform_playlists p
  LEFT JOIN playlist_track_associations pta ON p.id = pta.playlist_id
  WHERE p.user_id = $1
  GROUP BY p.id
  ORDER BY p.updated_at DESC
  LIMIT 100;
  ```

- **Search query** (SC-010: <2 seconds):
  ```sql
  SELECT * FROM platform_playlists
  WHERE to_tsvector('english', name) @@ plainto_tsquery('english', $1)
     OR name ILIKE '%' || $1 || '%'
  LIMIT 50;
  ```

### Connection Pooling
```csharp
// Connection string
"Host=postgres-host;Database=uniplay_prod;Username=uniplay;Password=***;Pooling=true;MinPoolSize=5;MaxPoolSize=100;ConnectionIdleLifetime=300"
```

---

## Migration Strategy

### Initial Migration (MVP)
1. Create all tables with schema above
2. Seed ServiceType enum values
3. Create indexes and constraints
4. Set up triggers for updated_at columns

### Future Migrations
- Add columns for collaborative playlists (AddedBy usage)
- Add columns for playlist sharing (IsPublic usage)
- Add tables for user preferences, notifications
- Optimize indexes based on query patterns

### Rollback Strategy
- All migrations versioned in EF Core migrations
- Database snapshots before production deployments
- Blue-green deployment for zero-downtime migrations

---

## Security Considerations

### Token Encryption (FR-027)
- **Development**: Data Protection API with file system key storage
- **Production**: Azure Key Vault + Azure Blob Storage for keys
- **Algorithm**: AES-256-CBC + HMACSHA256
- **Rotation**: Automatic 90-day key rotation

### SQL Injection Prevention
- **All queries**: Parameterized queries via EF Core
- **No raw SQL**: Use LINQ for all data access
- **Input validation**: Model validation attributes + FluentValidation

### Access Control
- **Row-level security**: UserId checks in queries
- **Playlist ownership**: IsOwnedByUser validation before edits
- **Service connection isolation**: User can only access their connections

---

## Monitoring and Observability

### Database Metrics
- Query execution time (P95, P99)
- Connection pool usage
- Slow query log (>100ms)
- Index usage statistics

### Application Metrics
- Sync queue depth (Pending + Processing items)
- Copy operation success rate
- Token refresh success rate
- Track match accuracy (per-service)

### Alerts
- Connection pool exhaustion (>90% usage)
- Sync queue backlog (>1000 items)
- Failed sync operations (>10% failure rate)
- Token refresh failures (>5% failure rate)

---

## Testing Strategy

### Unit Tests (EF Core)
```csharp
[Fact]
public void ServiceConnection_Should_Encrypt_AccessToken()
{
    // Arrange
    var connection = new ServiceConnection { EncryptedAccessToken = "plain_token" };

    // Act
    _dbContext.ServiceConnections.Add(connection);
    _dbContext.SaveChanges();

    // Assert
    var raw = _dbContext.Database.SqlQueryRaw<string>(
        "SELECT encrypted_access_token FROM service_connections WHERE id = {0}",
        connection.Id
    ).First();

    Assert.NotEqual("plain_token", raw); // Should be encrypted
}
```

### Integration Tests (In-Memory Database)
```csharp
[Fact]
public async Task User_Can_Create_CrossService_Playlist()
{
    // Arrange
    var user = await CreateTestUserAsync();
    var spotifyConnection = await CreateServiceConnectionAsync(user, ServiceType.Spotify);
    var appleMusicConnection = await CreateServiceConnectionAsync(user, ServiceType.AppleMusic);

    // Act
    var playlist = new PlatformPlaylist { UserId = user.Id, Name = "Cross-Service Mix" };
    await _playlistService.AddTrackAsync(playlist.Id, spotifyTrack);
    await _playlistService.AddTrackAsync(playlist.Id, appleMusicTrack);

    // Assert
    var tracks = await _dbContext.PlaylistTrackAssociations
        .Where(pta => pta.PlaylistId == playlist.Id)
        .Include(pta => pta.Track)
        .ToListAsync();

    Assert.Equal(2, tracks.Count);
    Assert.Contains(tracks, t => t.Track.ServiceType == ServiceType.Spotify);
    Assert.Contains(tracks, t => t.Track.ServiceType == ServiceType.AppleMusic);
}
```

---

## Appendix: Sample Queries

### Get User's Unified Dashboard
```csharp
var dashboard = await _dbContext.PlatformPlaylists
    .Where(p => p.UserId == userId)
    .Select(p => new DashboardPlaylistDto
    {
        Id = p.Id,
        Name = p.Name,
        Type = "Platform",
        TrackCount = p.TrackCount,
        UpdatedAt = p.UpdatedAt
    })
    .Union(
        _dbContext.ServicePlaylists
            .Where(sp => sp.ServiceConnection.UserId == userId && sp.SyncStatus == SyncStatus.Synced)
            .Select(sp => new DashboardPlaylistDto
            {
                Id = sp.Id,
                Name = sp.Name,
                Type = sp.ServiceConnection.ServiceType.ToString(),
                TrackCount = sp.TrackCount,
                UpdatedAt = sp.UpdatedAt
            })
    )
    .OrderByDescending(p => p.UpdatedAt)
    .Take(100)
    .ToListAsync();
```

### Check Max Connections Per Service
```csharp
var connectionCount = await _dbContext.ServiceConnections
    .CountAsync(sc => sc.UserId == userId && sc.ServiceType == serviceType);

if (connectionCount >= 3)
{
    throw new InvalidOperationException($"Maximum 3 {serviceType} connections per user");
}
```

### Get Expired Tokens for Refresh
```csharp
var expiringConnections = await _dbContext.ServiceConnections
    .Where(sc => sc.ConnectionStatus == ConnectionStatus.Active
                 && sc.AccessTokenExpiresAt < DateTime.UtcNow.AddMinutes(5))
    .ToListAsync();

foreach (var connection in expiringConnections)
{
    await _oauthService.RefreshTokenAsync(connection.Id);
}
```

---

## Next Phase

This data model document completes Phase 1's data design. Next steps:
1. **Generate API contracts** (contracts/api-endpoints.yaml)
2. **Create quickstart.md** for developers
3. **Update agent context** files
4. **Generate tasks.md** via `/speckit.tasks` command
