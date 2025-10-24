# Tasks: Multi-Service Playlist Manager

**Input**: Design documents from `/specs/001-multi-service-playlist-manager/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Organization**: Tasks are grouped by user story (P1 → P2 → P3) to enable independent implementation and testing.

**Tech Stack**: ASP.NET Core 9.0 + PostgreSQL + React 18 + Hangfire

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4, US5, US6)
- File paths relative to repository root

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [ ] T001 Create backend solution structure (backend/src/UniPlay.Api, UniPlay.Core, UniPlay.Infrastructure, UniPlay.Shared)
- [ ] T002 Create frontend project structure (frontend/src with React + TypeScript + Vite)
- [ ] T003 [P] Initialize backend projects with .NET 9.0 and install core dependencies (EF Core, Hangfire, Polly, FuzzySharp)
- [ ] T004 [P] Initialize frontend project with npm dependencies (React 18, TypeScript, @microsoft/signalr, dnd-kit)
- [ ] T005 [P] Configure EditorConfig and linting tools (.editorconfig, ESLint, Prettier)
- [ ] T006 Create test projects structure (backend/tests/UniPlay.Api.Tests, UniPlay.Core.Tests, UniPlay.Infrastructure.Tests)
- [ ] T007 [P] Setup Docker Compose for local development (PostgreSQL 16 container)
- [ ] T008 [P] Configure environment-based configuration (appsettings.json, appsettings.Development.json, User Secrets)

**Checkpoint**: Project structure ready for implementation

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T009 Create PostgreSQL database schema in backend/src/UniPlay.Infrastructure/Data/ApplicationDbContext.cs
- [ ] T010 [P] Define all entity classes in backend/src/UniPlay.Infrastructure/Data/Entities/ (UserAccount, ServiceConnection, PlatformPlaylist, ServicePlaylist, Track, PlaylistTrackAssociation, SyncQueue, CopyOperation)
- [ ] T011 [P] Create EF Core entity configurations in backend/src/UniPlay.Infrastructure/Data/Configurations/
- [ ] T012 Create initial EF Core migration with all tables
- [ ] T013 [P] Implement ASP.NET Core Identity for authentication in backend/src/UniPlay.Api/Program.cs
- [ ] T014 [P] Implement JWT token generation and validation in backend/src/UniPlay.Core/Services/TokenService.cs
- [ ] T015 [P] Create authentication middleware in backend/src/UniPlay.Api/Middleware/AuthenticationMiddleware.cs
- [ ] T016 [P] Create error handling middleware in backend/src/UniPlay.Api/Middleware/ErrorHandlingMiddleware.cs
- [ ] T017 [P] Implement Data Protection API for token encryption in backend/src/UniPlay.Infrastructure/Security/TokenEncryptionService.cs
- [ ] T018 [P] Configure Hangfire with PostgreSQL storage in backend/src/UniPlay.Api/Program.cs
- [ ] T019 [P] Setup Polly resilience policies in backend/src/UniPlay.Infrastructure/Extensions/PollyExtensions.cs
- [ ] T020 [P] Create base IMusicServiceAdapter interface in backend/src/UniPlay.Core/Interfaces/IMusicServiceAdapter.cs
- [ ] T021 [P] Setup API versioning and Scalar documentation in backend/src/UniPlay.Api/Program.cs
- [ ] T022 [P] Configure CORS for frontend in backend/src/UniPlay.Api/Program.cs
- [ ] T023 [P] Create frontend API client with Axios in frontend/src/services/apiClient.ts
- [ ] T024 [P] Setup React Router in frontend/src/App.tsx
- [ ] T025 Apply initial database migration to create all tables

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Connect Music Service Account (Priority: P1) 🎯 MVP

**Goal**: Users can connect Spotify accounts via OAuth and view their existing Spotify playlists

**Independent Test**: Connect a single Spotify account and verify playlists from that service appear in the platform

### Implementation for User Story 1

#### Backend - Authentication & User Management

- [ ] T026 [P] [US1] Create AuthController in backend/src/UniPlay.Api/Controllers/AuthController.cs (POST /register, POST /login)
- [ ] T027 [P] [US1] Create UserDto in backend/src/UniPlay.Core/Models/DTOs/UserDto.cs
- [ ] T028 [P] [US1] Implement user registration validation in backend/src/UniPlay.Core/Services/UserService.cs

#### Backend - Spotify Integration

- [ ] T029 [P] [US1] Install SpotifyAPI.Web NuGet package v7.2.1 in UniPlay.Infrastructure
- [ ] T030 [P] [US1] Implement SpotifyAdapter in backend/src/UniPlay.Infrastructure/MusicServices/SpotifyAdapter.cs
- [ ] T031 [P] [US1] Create ServiceConnectionDto in backend/src/UniPlay.Core/Models/DTOs/ServiceConnectionDto.cs
- [ ] T032 [P] [US1] Create ServicePlaylistDto in backend/src/UniPlay.Core/Models/DTOs/ServicePlaylistDto.cs
- [ ] T033 [US1] Implement ServiceConnectionService in backend/src/UniPlay.Core/Services/ServiceConnectionService.cs
- [ ] T034 [US1] Create ServiceConnectionsController in backend/src/UniPlay.Api/Controllers/ServiceConnectionsController.cs (POST /, GET /, DELETE /{id})
- [ ] T035 [US1] Implement OAuth callback endpoint in ServiceConnectionsController (POST /callback)
- [ ] T036 [US1] Add validation to enforce max 3 connections per service type in ServiceConnectionService

#### Backend - Repository Layer

- [ ] T037 [P] [US1] Create IServiceConnectionRepository interface in backend/src/UniPlay.Infrastructure/Repositories/IServiceConnectionRepository.cs
- [ ] T038 [US1] Implement ServiceConnectionRepository in backend/src/UniPlay.Infrastructure/Repositories/ServiceConnectionRepository.cs

#### Frontend - Authentication UI

- [ ] T039 [P] [US1] Create Register page in frontend/src/pages/Auth/Register.tsx
- [ ] T040 [P] [US1] Create Login page in frontend/src/pages/Auth/Login.tsx
- [ ] T041 [P] [US1] Create authService in frontend/src/services/authService.ts (register, login, logout, getCurrentUser)
- [ ] T042 [US1] Implement protected route wrapper in frontend/src/components/common/ProtectedRoute.tsx

#### Frontend - Service Connection UI

- [ ] T043 [P] [US1] Create ServiceConnections page in frontend/src/pages/ServiceConnections.tsx
- [ ] T044 [P] [US1] Create ServiceCard component in frontend/src/components/services/ServiceCard.tsx (displays connection status)
- [ ] T045 [P] [US1] Create ConnectServiceButton component in frontend/src/components/services/ConnectServiceButton.tsx
- [ ] T046 [P] [US1] Create serviceConnectionService in frontend/src/services/serviceConnectionService.ts (initiate OAuth, handle callback)
- [ ] T047 [US1] Implement OAuth callback handler page in frontend/src/pages/Auth/OAuthCallback.tsx

#### Hangfire - Token Refresh Job

- [ ] T048 [US1] Implement TokenRefreshJob in backend/src/UniPlay.Infrastructure/BackgroundJobs/TokenRefreshJob.cs
- [ ] T049 [US1] Register TokenRefreshJob as recurring job in Program.cs (every 5 minutes)

**Checkpoint**: User Story 1 complete - users can register, login, connect Spotify, and see Spotify playlists

---

## Phase 4: User Story 2 - View Unified Playlist Dashboard (Priority: P1)

**Goal**: Users can view playlists from all connected services in one unified dashboard with search and filtering

**Independent Test**: Connect two different services (Spotify + Apple Music) and verify both appear in unified view

### Implementation for User Story 2

#### Backend - Unified Playlist API

- [ ] T050 [P] [US2] Create PlaylistsController in backend/src/UniPlay.Api/Controllers/PlaylistsController.cs (GET /playlists with search and filtering)
- [ ] T051 [P] [US2] Create PlaylistSummaryDto in backend/src/UniPlay.Core/Models/DTOs/PlaylistSummaryDto.cs
- [ ] T052 [P] [US2] Create PaginationDto in backend/src/UniPlay.Core/Models/DTOs/PaginationDto.cs
- [ ] T053 [US2] Implement IPlaylistService interface in backend/src/UniPlay.Core/Services/IPlaylistService.cs
- [ ] T054 [US2] Implement PlaylistService in backend/src/UniPlay.Core/Services/PlaylistService.cs (GetUnifiedDashboard method)
- [ ] T055 [US2] Add full-text search indexes in database migration for playlist names

#### Backend - Additional Service Adapters

- [ ] T056 [P] [US2] Implement AppleMusicAdapter in backend/src/UniPlay.Infrastructure/MusicServices/AppleMusicAdapter.cs
- [ ] T057 [P] [US2] Install AspNet.Security.OAuth.Deezer package and implement DeezerAdapter in backend/src/UniPlay.Infrastructure/MusicServices/DeezerAdapter.cs
- [ ] T058 [US2] Add Apple Music and Deezer support to ServiceConnectionsController

#### Backend - Repository Layer

- [ ] T059 [P] [US2] Create IPlaylistRepository interface in backend/src/UniPlay.Infrastructure/Repositories/IPlaylistRepository.cs
- [ ] T060 [US2] Implement PlaylistRepository in backend/src/UniPlay.Infrastructure/Repositories/PlaylistRepository.cs

#### Frontend - Dashboard UI

- [ ] T061 [P] [US2] Create Dashboard page in frontend/src/pages/Dashboard.tsx
- [ ] T062 [P] [US2] Create PlaylistGrid component in frontend/src/components/dashboard/PlaylistGrid.tsx
- [ ] T063 [P] [US2] Create PlaylistCard component in frontend/src/components/playlists/PlaylistCard.tsx (displays playlist with service icon)
- [ ] T064 [P] [US2] Create SearchBar component in frontend/src/components/common/SearchBar.tsx
- [ ] T065 [P] [US2] Create ServiceFilter component in frontend/src/components/dashboard/ServiceFilter.tsx
- [ ] T066 [P] [US2] Create playlistService in frontend/src/services/playlistService.ts (getUnifiedDashboard, searchPlaylists)
- [ ] T067 [US2] Implement pagination in PlaylistGrid component

#### Hangfire - Periodic Sync Job

- [ ] T068 [US2] Implement PeriodicSyncJob in backend/src/UniPlay.Infrastructure/BackgroundJobs/PeriodicSyncJob.cs
- [ ] T069 [US2] Register PeriodicSyncJob as recurring job in Program.cs (every 6 hours)

**Checkpoint**: User Story 2 complete - unified dashboard displays playlists from all services with search/filter

---

## Phase 5: User Story 3 - Create Platform-Native Playlist (Priority: P2)

**Goal**: Users can create cross-service playlists and add tracks from multiple services

**Independent Test**: Create platform playlist and add tracks from both Spotify and Apple Music

### Implementation for User Story 3

#### Backend - Platform Playlist CRUD

- [ ] T070 [P] [US3] Create PlatformPlaylistDto in backend/src/UniPlay.Core/Models/DTOs/PlatformPlaylistDto.cs
- [ ] T071 [P] [US3] Create PlatformPlaylistDetailDto in backend/src/UniPlay.Core/Models/DTOs/PlatformPlaylistDetailDto.cs
- [ ] T072 [P] [US3] Create TrackDto in backend/src/UniPlay.Core/Models/DTOs/TrackDto.cs
- [ ] T073 [P] [US3] Create PlaylistTrackDto in backend/src/UniPlay.Core/Models/DTOs/PlaylistTrackDto.cs
- [ ] T074 [US3] Add platform playlist endpoints to PlaylistsController (POST /, PATCH /{id}, DELETE /{id}, GET /{id})
- [ ] T075 [US3] Implement CreatePlatformPlaylist method in PlaylistService
- [ ] T076 [US3] Implement UpdatePlatformPlaylist method in PlaylistService
- [ ] T077 [US3] Implement DeletePlatformPlaylist method in PlaylistService

#### Backend - Track Management

- [ ] T078 [P] [US3] Create /playlists/{id}/tracks endpoints in PlaylistsController (POST, DELETE, POST /reorder)
- [ ] T079 [US3] Implement AddTrackToPlaylist method in PlaylistService (validates cross-service support)
- [ ] T080 [US3] Implement RemoveTrackFromPlaylist method in PlaylistService
- [ ] T081 [US3] Implement ReorderTracks method in PlaylistService

#### Backend - Track Search

- [ ] T082 [P] [US3] Create /tracks/search endpoint in new TracksController in backend/src/UniPlay.Api/Controllers/TracksController.cs
- [ ] T083 [US3] Implement SearchTracks method in PlaylistService (searches across all connected services in parallel)

#### Frontend - Playlist Creation UI

- [ ] T084 [P] [US3] Create CreatePlaylistModal component in frontend/src/components/playlists/CreatePlaylistModal.tsx
- [ ] T085 [P] [US3] Create PlaylistDetails page in frontend/src/pages/PlaylistDetails.tsx
- [ ] T086 [P] [US3] Create TrackList component in frontend/src/components/playlists/TrackList.tsx with drag-drop (dnd-kit)
- [ ] T087 [P] [US3] Create TrackItem component in frontend/src/components/playlists/TrackItem.tsx (displays track with service icon)
- [ ] T088 [P] [US3] Create AddTrackModal component in frontend/src/components/playlists/AddTrackModal.tsx (search UI)
- [ ] T089 [US3] Implement drag-drop reordering in TrackList component
- [ ] T090 [US3] Add createPlaylist, updatePlaylist, deletePlaylist methods to playlistService.ts
- [ ] T091 [US3] Add addTrack, removeTrack, reorderTracks methods to playlistService.ts

**Checkpoint**: User Story 3 complete - users can create and manage platform playlists with cross-service tracks

---

## Phase 6: User Story 4 - Edit Existing Service Playlists (Priority: P2)

**Goal**: Users can edit service playlists with bidirectional sync to original services

**Independent Test**: Edit a Spotify playlist through the platform and verify changes sync to Spotify

### Implementation for User Story 4

#### Backend - Service Playlist Sync

- [ ] T092 [P] [US4] Create ServicePlaylistDetailDto in backend/src/UniPlay.Core/Models/DTOs/ServicePlaylistDetailDto.cs
- [ ] T093 [P] [US4] Create /service-playlists endpoints in new ServicePlaylistsController in backend/src/UniPlay.Api/Controllers/ServicePlaylistsController.cs (GET /, GET /{id}, PATCH /{id})
- [ ] T094 [US4] Implement GetServicePlaylist method in PlaylistService
- [ ] T095 [US4] Implement UpdateServicePlaylist method in PlaylistService (queues sync)

#### Backend - Sync Queue System

- [ ] T096 [P] [US4] Create SyncQueueDto in backend/src/UniPlay.Core/Models/DTOs/SyncQueueDto.cs
- [ ] T097 [US4] Implement ISyncService interface in backend/src/UniPlay.Core/Services/ISyncService.cs
- [ ] T098 [US4] Implement SyncService in backend/src/UniPlay.Core/Services/SyncService.cs (QueueOperation, GetOperationStatus)
- [ ] T099 [US4] Add /service-playlists/{id}/tracks endpoints to ServicePlaylistsController (POST, DELETE)
- [ ] T100 [US4] Implement validation in PlaylistService to prevent cross-service track additions (FR-011)

#### Hangfire - Sync Queue Processor

- [ ] T101 [US4] Implement SyncQueueProcessor in backend/src/UniPlay.Infrastructure/BackgroundJobs/SyncQueueProcessor.cs
- [ ] T102 [US4] Register SyncQueueProcessor as recurring job in Program.cs (every 10 seconds)
- [ ] T103 [US4] Implement ProcessQueueItem method with retry logic (exponential backoff)

#### Frontend - Service Playlist Editing

- [ ] T104 [P] [US4] Create EditServicePlaylistModal component in frontend/src/components/playlists/EditServicePlaylistModal.tsx
- [ ] T105 [P] [US4] Add service playlist editing UI to PlaylistDetails page
- [ ] T106 [US4] Display validation error when attempting to add cross-service tracks to service playlists
- [ ] T107 [US4] Add updateServicePlaylist method to playlistService.ts

**Checkpoint**: User Story 4 complete - service playlist edits sync back to original services

---

## Phase 7: User Story 5 - Copy Playlist Between Services (Priority: P3)

**Goal**: Users can copy playlists from one service to another with automatic track matching

**Independent Test**: Copy a Spotify playlist to Apple Music and verify matched tracks in new playlist

### Implementation for User Story 5

#### Backend - Track Matching Algorithm

- [ ] T108 [P] [US5] Install FuzzySharp NuGet package v2.0.2 in UniPlay.Infrastructure
- [ ] T109 [P] [US5] Create ITrackMatchingService interface in backend/src/UniPlay.Core/Services/ITrackMatchingService.cs
- [ ] T110 [US5] Implement TrackMatchingService in backend/src/UniPlay.Core/Services/TrackMatchingService.cs (ISRC + fuzzy matching)
- [ ] T111 [US5] Implement FindMatchAsync method (4-stage matching: ISRC, normalized search, fuzzy scoring, confidence)

#### Backend - Copy Operation

- [ ] T112 [P] [US5] Create CopyOperationDto in backend/src/UniPlay.Core/Models/DTOs/CopyOperationDto.cs
- [ ] T113 [P] [US5] Create CopyOperationDetailDto in backend/src/UniPlay.Core/Models/DTOs/CopyOperationDetailDto.cs
- [ ] T114 [P] [US5] Create PlaylistOperationsController in backend/src/UniPlay.Api/Controllers/PlaylistOperationsController.cs
- [ ] T115 [US5] Add POST /playlist-operations/copy endpoint
- [ ] T116 [US5] Add GET /playlist-operations/copy/{id} endpoint (progress tracking)
- [ ] T117 [US5] Add DELETE /playlist-operations/copy/{id} endpoint (cancel operation)

#### Hangfire - Playlist Copy Job

- [ ] T118 [US5] Implement PlaylistCopyJob in backend/src/UniPlay.Infrastructure/BackgroundJobs/PlaylistCopyJob.cs
- [ ] T119 [US5] Implement ExecuteAsync method with progress tracking (matching, creating, adding tracks)
- [ ] T120 [US5] Configure automatic retry (3 attempts with backoff)

#### SignalR - Real-time Progress

- [ ] T121 [P] [US5] Setup SignalR hub in backend/src/UniPlay.Api/Hubs/PlaylistHub.cs
- [ ] T122 [US5] Add SignalR progress notifications to PlaylistCopyJob
- [ ] T123 [P] [US5] Install @microsoft/signalr in frontend
- [ ] T124 [US5] Create SignalR connection in frontend/src/services/signalRService.ts

#### Frontend - Copy Playlist UI

- [ ] T125 [P] [US5] Create CopyPlaylistModal component in frontend/src/components/playlists/CopyPlaylistModal.tsx
- [ ] T126 [P] [US5] Create CopyProgressModal component in frontend/src/components/playlists/CopyProgressModal.tsx (real-time progress)
- [ ] T127 [P] [US5] Create CopyResultModal component in frontend/src/components/playlists/CopyResultModal.tsx (shows matched/unmatched tracks)
- [ ] T128 [US5] Add copyPlaylist method to playlistService.ts
- [ ] T129 [US5] Implement real-time progress updates using SignalR

**Checkpoint**: User Story 5 complete - users can copy playlists between services with track matching

---

## Phase 8: User Story 6 - Sync Platform Playlist to Service (Priority: P3)

**Goal**: Users can export platform playlists to music services (one-time snapshots)

**Independent Test**: Export platform playlist to Spotify and verify new Spotify playlist created

### Implementation for User Story 6

#### Backend - Export Operation

- [ ] T130 [P] [US6] Add POST /playlist-operations/export endpoint to PlaylistOperationsController
- [ ] T131 [US6] Implement multi-service export (can export to multiple services simultaneously)

#### Hangfire - Playlist Export Job

- [ ] T132 [US6] Implement PlaylistExportJob in backend/src/UniPlay.Infrastructure/BackgroundJobs/PlaylistExportJob.cs
- [ ] T133 [US6] Implement ExecuteAsync method (filters tracks by destination service, performs matching)
- [ ] T134 [US6] Add SignalR progress notifications to PlaylistExportJob

#### Frontend - Export Playlist UI

- [ ] T135 [P] [US6] Create ExportPlaylistModal component in frontend/src/components/playlists/ExportPlaylistModal.tsx (multi-select services)
- [ ] T136 [P] [US6] Create ExportProgressModal component in frontend/src/components/playlists/ExportProgressModal.tsx
- [ ] T137 [P] [US6] Create ExportResultModal component in frontend/src/components/playlists/ExportResultModal.tsx
- [ ] T138 [US6] Add exportPlaylist method to playlistService.ts

**Checkpoint**: User Story 6 complete - platform playlists can be exported to services

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Production readiness and improvements affecting multiple user stories

### Error Handling & Validation

- [ ] T139 [P] Implement comprehensive error handling in all controllers
- [ ] T140 [P] Add FluentValidation for all request DTOs in backend/src/UniPlay.Api/Validators/
- [ ] T141 [P] Create user-friendly error messages for all edge cases

### Performance Optimization

- [ ] T142 [P] Add database indexes per data-model.md specifications
- [ ] T143 [P] Implement caching layer (IMemoryCache) in PlaylistService for hot playlists
- [ ] T144 [P] Configure connection pooling in PostgreSQL connection string (min 5, max 100)
- [ ] T145 Add denormalized TrackCount columns with computed values

### Security Hardening

- [ ] T146 [P] Configure Azure Key Vault integration for production token encryption
- [ ] T147 [P] Implement rate limiting on API endpoints (ASP.NET Core rate limiter)
- [ ] T148 [P] Add HTTPS enforcement and HSTS configuration
- [ ] T149 [P] Configure CORS policies for production domains

### Testing

- [ ] T150 [P] Add unit tests for all service classes in backend/tests/UniPlay.Core.Tests/
- [ ] T151 [P] Add integration tests for all controllers in backend/tests/UniPlay.Api.Tests/
- [ ] T152 [P] Add unit tests for React components in frontend/tests/unit/
- [ ] T153 Create E2E test suite with Playwright in backend/tests/UniPlay.E2E.Tests/

### Documentation

- [ ] T154 [P] Generate TypeScript client from OpenAPI spec using NSwag
- [ ] T155 [P] Update quickstart.md with final setup instructions
- [ ] T156 [P] Create deployment guide in docs/deployment/
- [ ] T157 [P] Add API documentation comments for Scalar UI

### Monitoring & Logging

- [ ] T158 [P] Configure structured logging with Serilog
- [ ] T159 [P] Add correlation IDs to all requests
- [ ] T160 [P] Setup application insights or monitoring dashboard
- [ ] T161 [P] Add health check endpoints for Hangfire, database, external services

### Production Readiness

- [ ] T162 [P] Create Dockerfile for backend API
- [ ] T163 [P] Create Dockerfile for frontend (Nginx)
- [ ] T164 [P] Setup CI/CD pipeline configuration
- [ ] T165 Validate against all success criteria (SC-001 through SC-013)
- [ ] T166 Run quickstart.md validation end-to-end

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - **BLOCKS all user stories**
- **User Story 1 (Phase 3)**: Depends on Foundational completion - **MVP foundation**
- **User Story 2 (Phase 4)**: Depends on Foundational completion - Can parallelize with US1 if staffed
- **User Story 3 (Phase 5)**: Depends on Foundational completion - Independent of US1/US2
- **User Story 4 (Phase 6)**: Depends on Foundational + US1 (needs service connections) - Can parallelize with US3
- **User Story 5 (Phase 7)**: Depends on Foundational + US1 (needs service connections) - Independent of US3/US4
- **User Story 6 (Phase 8)**: Depends on Foundational + US1 + US3 (needs platform playlists)
- **Polish (Phase 9)**: Depends on all desired user stories being complete

### Critical Path for MVP

1. Phase 1: Setup → 2. Phase 2: Foundational → 3. Phase 3: User Story 1

**Deploy User Story 1 as MVP** (users can connect Spotify and see playlists)

### User Story Dependencies

- **US1 (P1)**: No dependencies on other stories - **MVP ready**
- **US2 (P1)**: Can start after Foundational - integrates with US1 but independently testable
- **US3 (P2)**: Can start after Foundational - independent of US1/US2
- **US4 (P2)**: Requires US1 (service connections must exist first)
- **US5 (P3)**: Requires US1 (service connections must exist first)
- **US6 (P3)**: Requires US1 (service connections) + US3 (platform playlists)

### Parallel Opportunities

**Within Setup (Phase 1)**:
- T003, T004, T005, T007, T008 can all run in parallel

**Within Foundational (Phase 2)**:
- T010, T011, T013, T014, T015, T016, T017, T018, T019, T020, T021, T022, T023, T024 can all run in parallel after T009

**User Story Parallelization** (if team capacity allows):
- After Foundational: US1, US2, US3 can all start in parallel
- US4 can start once US1 completes
- US5 can start once US1 completes (parallel with US4)
- US6 can start once US1 and US3 complete

**Within Each User Story**:
- All tasks marked [P] can run in parallel within that story

---

## Parallel Example: User Story 1

```bash
# Backend models and DTOs (parallel):
Task T027: Create UserDto
Task T031: Create ServiceConnectionDto
Task T032: Create ServicePlaylistDto

# Backend adapters and controllers (parallel after DTOs):
Task T030: Implement SpotifyAdapter
Task T026: Create AuthController
Task T037: Create IServiceConnectionRepository

# Frontend pages (parallel):
Task T039: Create Register page
Task T040: Create Login page
Task T043: Create ServiceConnections page

# All [P] tasks within US1 can start together once dependencies met
```

---

## Implementation Strategy

### MVP First (User Story 1 Only) - Recommended

**Timeline**: ~2-3 weeks for solo developer

1. **Week 1**: Complete Phase 1 (Setup) + Phase 2 (Foundational)
   - Day 1-2: Project setup, database schema, entity models
   - Day 3-4: Authentication, token encryption, Hangfire
   - Day 5: API infrastructure, frontend setup

2. **Week 2-3**: Complete Phase 3 (User Story 1)
   - Day 6-7: Spotify adapter, service connection backend
   - Day 8-9: Auth UI, service connection UI
   - Day 10-11: Token refresh job, OAuth flow testing
   - Day 12-13: Integration testing, bug fixes

3. **Deploy MVP**: Users can register, connect Spotify, view Spotify playlists

### Incremental Delivery

After MVP, add one story at a time:

1. **MVP (US1)**: Connect Spotify + view playlists → Deploy
2. **+US2**: Unified dashboard with multiple services → Deploy
3. **+US3**: Create platform playlists → Deploy
4. **+US4**: Edit service playlists with sync → Deploy
5. **+US5**: Copy playlists between services → Deploy
6. **+US6**: Export platform playlists → Deploy

Each deployment adds value without breaking previous features.

### Parallel Team Strategy (3+ developers)

**Week 1-2**: All developers complete Setup + Foundational together

**Week 3+**: Split by user story
- **Developer A**: User Story 1 (Spotify integration)
- **Developer B**: User Story 2 (Dashboard + Apple Music/Deezer)
- **Developer C**: User Story 3 (Platform playlists)

Once US1 completes, Developer A moves to US4 (sync), then US5 (copy).

---

## Notes

- **[P] tasks**: Different files, no dependencies - safe to parallelize
- **[Story] labels**: Maps task to specific user story for traceability
- **Checkpoint validation**: Test each story independently before moving to next
- **Commit frequency**: Commit after each task or logical group of tasks
- **YouTube Music**: Skip initially (see contracts/service-integrations/youtube-music-adapter.md for limitations)
- **Success criteria**: Validate SC-001 through SC-013 during polish phase

---

## Total Task Count

- **Setup (Phase 1)**: 8 tasks
- **Foundational (Phase 2)**: 17 tasks
- **User Story 1 (Phase 3)**: 24 tasks
- **User Story 2 (Phase 4)**: 20 tasks
- **User Story 3 (Phase 5)**: 22 tasks
- **User Story 4 (Phase 6)**: 16 tasks
- **User Story 5 (Phase 7)**: 22 tasks
- **User Story 6 (Phase 8)**: 9 tasks
- **Polish (Phase 9)**: 28 tasks

**Total**: 166 implementation tasks

**Estimated MVP (Setup + Foundational + US1)**: ~49 tasks (~2-3 weeks solo developer)
