# Feature Specification: Multi-Service Playlist Manager

**Feature Branch**: `001-multi-service-playlist-manager`
**Created**: 2025-10-24
**Status**: Draft
**Input**: User description: "create a website with asp.net code 9.0+ that does the following. Manages playlist of various cloud based music services ( start with spotify, ytmusic, apple music, deezer ). It needs to be able create edit playlist between these services. it shoudl have its own playlist too. User should be able add same service under multiple credentials - nice to have."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Connect Music Service Account (Priority: P1)

Users can connect their existing music streaming service accounts to the platform to enable playlist management across services.

**Why this priority**: This is the foundation of the entire platform. Without the ability to connect service accounts, no other functionality is possible. This delivers immediate value by centralizing access to multiple music services.

**Independent Test**: Can be fully tested by connecting a single music service account (e.g., Spotify) and verifying that the user can view their existing playlists from that service within the platform.

**Acceptance Scenarios**:

1. **Given** a user has created an account on the platform, **When** they navigate to the "Connect Services" page and select "Connect Spotify", **Then** they are redirected to Spotify's authorization page to grant permissions
2. **Given** a user has authorized Spotify access, **When** they return to the platform, **Then** their Spotify account appears as "Connected" with account details visible (username, profile picture)
3. **Given** a user has one Spotify account connected, **When** they attempt to connect a second Spotify account with different credentials, **Then** they can successfully add it and both accounts are listed separately with distinguishing labels
4. **Given** a user has connected a service account, **When** they view their connected services, **Then** they can see a list of their playlists from that service
5. **Given** a user wants to disconnect a service, **When** they click "Disconnect" on a connected account, **Then** the account is removed and its playlists are no longer visible in the platform

---

### User Story 2 - View Unified Playlist Dashboard (Priority: P1)

Users can view all their playlists from all connected services in one unified dashboard, making it easy to see their entire music collection at a glance.

**Why this priority**: This is the core value proposition that solves the user's fragmentation problem. Being able to see all playlists in one place is essential for the platform to be useful.

**Independent Test**: Can be fully tested by connecting at least two different music services and verifying that playlists from both services appear in a single unified view with clear service identification.

**Acceptance Scenarios**:

1. **Given** a user has connected Spotify and Apple Music accounts, **When** they navigate to the dashboard, **Then** they see all playlists from both services displayed together with service icons indicating the source
2. **Given** a user has multiple playlists across services, **When** they view the dashboard, **Then** they can filter playlists by service (show only Spotify, only Apple Music, etc.)
3. **Given** a user is viewing their dashboard, **When** they search for a playlist by name, **Then** results include matches from all connected services
4. **Given** a user has created platform-native playlists, **When** they view the dashboard, **Then** these playlists appear alongside service playlists with a distinct "UniPlay" identifier

---

### User Story 3 - Create Platform-Native Playlist (Priority: P2)

Users can create their own playlists within the platform that are independent of any specific music service, allowing them to curate tracks from multiple services in one place.

**Why this priority**: This enables users to create cross-service playlists, which is a unique value proposition. However, it depends on being able to connect services and view content first (P1 features).

**Independent Test**: Can be fully tested by creating a new platform playlist, adding tracks from at least two different connected services, and verifying the playlist is saved and displayed correctly.

**Acceptance Scenarios**:

1. **Given** a user is logged into the platform, **When** they click "Create New Playlist", **Then** they can enter a playlist name and description
2. **Given** a user has created a new playlist, **When** they search for tracks across their connected services, **Then** they can add tracks from any service to the playlist
3. **Given** a user has added tracks from Spotify and Apple Music to a platform playlist, **When** they view the playlist, **Then** each track displays with its source service icon
4. **Given** a user has created a platform playlist, **When** they navigate away and return later, **Then** the playlist persists with all tracks intact
5. **Given** a user is viewing a platform playlist, **When** they reorder tracks by dragging them, **Then** the new order is saved

---

### User Story 4 - Edit Existing Service Playlists (Priority: P2)

Users can modify playlists that exist on their connected music services directly through the platform, with changes synchronized back to the original service.

**Why this priority**: This allows users to manage their service playlists without leaving the platform, but requires the foundation of connected services (P1) to work.

**Independent Test**: Can be fully tested by editing a Spotify playlist (adding/removing tracks, renaming) through the platform and verifying the changes are reflected on Spotify's native app/website.

**Acceptance Scenarios**:

1. **Given** a user has a Spotify playlist, **When** they view it in the platform and click "Edit", **Then** they can rename the playlist and the change syncs to Spotify
2. **Given** a user is editing a service playlist, **When** they add a track from the same service, **Then** the track is added both in the platform view and on the service itself
3. **Given** a user removes a track from a service playlist, **When** they save changes, **Then** the track is removed from the original service playlist
4. **Given** a user tries to add a track from a different service to a service-specific playlist, **Then** they see a message explaining that cross-service tracks can only be added to platform playlists
5. **Given** a user has edited a service playlist, **When** synchronization fails due to network issues, **Then** they see an error message and changes are queued for retry

---

### User Story 5 - Copy Playlist Between Services (Priority: P3)

Users can duplicate playlists from one music service to another, automatically finding matching tracks on the destination service.

**Why this priority**: This is a convenience feature that solves a common pain point but is not essential for the core platform functionality. It provides high value but can be delivered after the foundational features.

**Independent Test**: Can be fully tested by copying a Spotify playlist to Apple Music and verifying that matching tracks are created in a new Apple Music playlist.

**Acceptance Scenarios**:

1. **Given** a user has a playlist on Spotify, **When** they select "Copy to Another Service" and choose Apple Music, **Then** the system attempts to find matching tracks on Apple Music
2. **Given** the system is copying a playlist, **When** a track is found on the destination service, **Then** it is added to the new playlist
3. **Given** the system is copying a playlist, **When** a track cannot be found on the destination service, **Then** it is listed in a "Not Found" report shown to the user
4. **Given** a playlist copy operation completes, **When** the user views their destination service, **Then** they see a new playlist with all successfully matched tracks
5. **Given** a user initiates a copy operation, **When** the operation completes, **Then** a new playlist with the same name as the source is created on the destination service

---

### User Story 6 - Sync Platform Playlist to Service (Priority: P3)

Users can export their platform-native playlists to one or more connected music services, creating service-specific versions with available tracks.

**Why this priority**: This allows users to take their curated cross-service playlists and use them on individual services, but it's not essential for the core playlist management functionality.

**Independent Test**: Can be fully tested by creating a platform playlist with tracks from multiple services, exporting it to Spotify, and verifying that a new Spotify playlist is created with only the Spotify-compatible tracks.

**Acceptance Scenarios**:

1. **Given** a user has a platform playlist with tracks from multiple services, **When** they select "Export to Spotify", **Then** the system creates a new Spotify playlist with only the tracks available on Spotify
2. **Given** a user is exporting a playlist, **When** the export completes, **Then** they see a summary showing how many tracks were successfully exported and how many were unavailable
3. **Given** a user has exported a playlist to a service, **When** they later modify the platform playlist, **Then** the exported service playlist remains unchanged (exports are one-time snapshots)
4. **Given** a user wants to export to multiple services, **When** they select "Export to Multiple Services", **Then** they can choose which services to export to and the operation processes each independently

---

### Edge Cases

- What happens when a user's service authorization expires or is revoked? The system should detect this, mark the service as "Disconnected", and prompt the user to re-authenticate without losing platform data.
- How does the system handle a user deleting a playlist on the original service? The platform should detect the deletion during the next sync and either mark it as "Deleted on Service" or remove it from the platform view based on user preferences.
- What happens when two users try to edit the same shared service playlist simultaneously? Changes should follow the "last write wins" model of the underlying service, with the platform reflecting the final state after sync.
- How does the system handle duplicate track additions? The system should prevent adding the same track twice to a playlist and show a message indicating the track already exists.
- What happens when a music service is temporarily unavailable? The platform should show cached playlist data with a warning that it may be outdated, and queue any edit operations for retry when the service becomes available.
- How does the system handle rate limiting from music services? The platform should implement exponential backoff and show users a message if operations are delayed due to service rate limits.
- What happens when a user reaches the maximum playlist limit on a service? The system should show an error message indicating the service limit has been reached and prevent playlist creation on that service.
- How does the system handle very large playlists (thousands of tracks)? The platform should implement pagination for viewing and batch processing for sync operations to maintain performance.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow users to create accounts with email and password authentication
- **FR-002**: System MUST support OAuth-based authorization for Spotify, YouTube Music, Apple Music, and Deezer
- **FR-003**: System MUST allow users to connect multiple accounts from the same music service with different credentials
- **FR-004**: System MUST display all playlists from connected services in a unified dashboard view
- **FR-005**: System MUST clearly indicate the source service for each playlist (via icons, labels, or colors)
- **FR-006**: System MUST allow users to create platform-native playlists that can contain tracks from multiple services
- **FR-007**: System MUST allow users to add, remove, and reorder tracks in platform-native playlists
- **FR-008**: System MUST allow users to rename and delete platform-native playlists
- **FR-009**: System MUST synchronize changes made to service playlists back to the original music service
- **FR-010**: System MUST support adding tracks from the same service to service-specific playlists
- **FR-011**: System MUST prevent adding cross-service tracks to service-specific playlists and display appropriate messaging
- **FR-012**: System MUST support copying playlists from one service to another with track matching
- **FR-013**: System MUST provide a report of tracks that could not be matched during cross-service copy operations
- **FR-014**: System MUST support exporting platform playlists to connected music services
- **FR-015**: System MUST filter exported tracks based on availability on the destination service
- **FR-016**: System MUST allow users to search for playlists by name across all connected services
- **FR-017**: System MUST allow users to filter playlists by source service
- **FR-018**: System MUST allow users to disconnect service accounts
- **FR-019**: System MUST persist all platform-native playlist data even when service accounts are disconnected
- **FR-020**: System MUST periodically sync playlist metadata and track lists from connected services to keep data current
- **FR-021**: System MUST detect when service authorization expires and prompt users to re-authenticate
- **FR-022**: System MUST handle service API rate limits gracefully with appropriate retry logic
- **FR-023**: System MUST queue operations when a service is temporarily unavailable and retry when connectivity is restored
- **FR-024**: System MUST display error messages when service operations fail with clear explanation and next steps
- **FR-025**: System MUST support pagination for playlists and tracks when dealing with large collections
- **FR-026**: System MUST provide visual feedback during long-running operations (copying, syncing, exporting)
- **FR-027**: System MUST store only essential service data and respect user privacy by not accessing more permissions than necessary
- **FR-028**: System MUST allow users to view their connected service accounts with account details (username, email)

### Key Entities

- **User Account**: Represents a registered user of the platform, stores authentication credentials, preferences, and relationships to connected service accounts
- **Service Connection**: Links a user to a specific music service account (Spotify, YouTube Music, Apple Music, Deezer), stores OAuth tokens, account identifier, and connection status
- **Platform Playlist**: A playlist created within the platform that can contain tracks from any connected service, includes name, description, creation date, and track order
- **Service Playlist**: A reference to a playlist that exists on a connected music service, includes playlist ID from the service, name, track count, and last sync timestamp
- **Track**: Represents a music track with metadata (title, artist, album), service identifier, and reference to which service it comes from
- **Playlist Track Association**: Links tracks to playlists with ordering information, handles both platform and service playlists
- **Sync Queue**: Stores pending operations that need to be synchronized to music services (edits, additions, deletions), includes retry logic metadata
- **Copy Operation**: Represents a cross-service playlist copy job, tracks progress, matched tracks, and unmatched tracks for reporting

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can connect their first music service account and view their existing playlists within 3 minutes of account creation
- **SC-002**: Users can create a cross-service platform playlist with tracks from at least two different services within 5 minutes
- **SC-003**: 90% of edits to service playlists successfully synchronize to the original service within 30 seconds
- **SC-004**: System successfully matches at least 85% of tracks when copying playlists between services (based on common track matching algorithms)
- **SC-005**: Platform dashboard loads and displays all playlists from connected services in under 3 seconds for users with up to 100 playlists
- **SC-006**: Users can connect up to 3 accounts per service type without degradation in performance or user experience
- **SC-007**: 95% of users successfully complete the service connection flow on their first attempt
- **SC-008**: Platform maintains 99.5% uptime for core playlist viewing and creation functionality
- **SC-009**: Sync operations retry automatically and succeed within 5 minutes when temporary service outages resolve
- **SC-010**: Search functionality returns relevant results across all connected services in under 2 seconds

### User Satisfaction Metrics

- **SC-011**: Users report that managing playlists across services takes 50% less time compared to using individual service apps
- **SC-012**: 80% of users who connect two or more services actively use platform playlists within their first week
- **SC-013**: Average user connects at least 2 different music services within their first month of use

## Assumptions

1. **OAuth Support**: All target music services (Spotify, YouTube Music, Apple Music, Deezer) provide OAuth-based APIs that support third-party playlist management
2. **Track Matching**: Track matching between services will use a combination of metadata (artist name, track title, album name, duration) with fuzzy matching algorithms, accepting that 100% accuracy is not possible
3. **Service API Availability**: Music service APIs have reasonable rate limits that allow for normal user operations without significant delays
4. **Data Retention**: Platform will cache playlist and track metadata for reasonable performance, refreshing periodically (default: every 6 hours for active users)
5. **Authorization Scope**: Users understand and accept that the platform needs read/write permissions for playlists on their connected services
6. **Browser Support**: The platform will target modern web browsers (Chrome, Firefox, Safari, Edge - current versions and one major version back)
7. **Mobile Responsiveness**: The platform will be responsive and functional on mobile devices, though a mobile app is out of scope
8. **Service Playlist Limits**: The platform will respect and enforce playlist limits imposed by individual music services (e.g., maximum tracks per playlist)
9. **Export Behavior**: Playlist exports and copies to services are always one-time snapshots - subsequent changes to source playlists do not automatically sync
10. **Account Security**: Users are responsible for securing their platform account credentials and connected service authorizations

## Out of Scope

The following items are explicitly excluded from this feature:

- **Music Playback**: The platform does not provide music streaming or playback capabilities - users must use the native service apps to listen to music
- **Offline Support**: All operations require internet connectivity; offline playlist management is not supported
- **Social Features**: Sharing playlists with other platform users, collaborative editing, or social discovery features
- **Mobile Native Apps**: iOS and Android native applications (only responsive web interface is in scope)
- **Automatic Playlist Generation**: AI-powered playlist creation, recommendation engines, or mood-based playlist generation
- **Lyrics and Metadata Editing**: Ability to view lyrics or edit track metadata beyond what the services provide
- **Local File Support**: Uploading or managing local music files that aren't on streaming services
- **Music Service Account Creation**: Creating new accounts on music services - users must already have accounts
- **Podcast Support**: Managing podcast subscriptions or episodes from services that offer both music and podcasts
- **Download Functionality**: Downloading tracks for offline listening (this would violate service terms of service)
- **Payment Processing**: Subscription management or payment features for music services
- **Advanced Analytics**: Listening statistics, most-played tracks, or detailed analytics beyond basic playlist counts
- **Batch Operations**: Bulk playlist deletion, mass track removal, or other administrative bulk actions (beyond single playlist copy/export)
- **Service-Specific Features**: Access to service-specific features like Spotify's "Discover Weekly" or Apple Music's "For You" sections

## Dependencies

- **Music Service API Access**: Requires approved developer access and API keys for Spotify, YouTube Music, Apple Music, and Deezer APIs
- **OAuth Provider Registration**: Each music service requires application registration and approval before OAuth can be implemented
- **Rate Limit Quotas**: Need to ensure API rate limit quotas from services are sufficient for expected user base
- **Service API Documentation**: Comprehensive API documentation and support from each service provider

## Resolved Questions

1. **Playlist Export Sync Behavior**: Exports are one-time snapshots. Changes to the platform playlist do not automatically sync to exported service playlists. Users must manually re-export if they want to update the service playlist.
   - **Resolution**: One-time snapshot approach reduces complexity and prevents unexpected changes to user's service playlists

2. **Playlist Copy Destination**: The system always creates a new playlist on the destination service with the same name as the source playlist. Users cannot append to existing playlists during the copy operation.
   - **Resolution**: Creating new playlists prevents accidental modifications to existing playlists and provides a clearer, simpler user experience
