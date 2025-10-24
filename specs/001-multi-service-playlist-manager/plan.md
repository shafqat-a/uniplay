# Implementation Plan: Multi-Service Playlist Manager

**Branch**: `001-multi-service-playlist-manager` | **Date**: 2025-10-24 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-multi-service-playlist-manager/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

The Multi-Service Playlist Manager is a web application that enables users to manage playlists across multiple music streaming services (Spotify, YouTube Music, Apple Music, Deezer) from a unified dashboard. Users can connect multiple accounts per service, create cross-service playlists, edit existing service playlists with bidirectional sync, and copy/export playlists between services with automatic track matching. The system will be built using ASP.NET Core 9.0+ with OAuth-based authentication, background job processing for sync operations, and a responsive web frontend.

## Technical Context

**Language/Version**: C# with ASP.NET Core 9.0+
**Primary Dependencies**:
- ASP.NET Core Identity (user authentication)
- Entity Framework Core (data persistence)
- [NEEDS CLARIFICATION: OAuth client libraries for Spotify, YouTube Music, Apple Music, Deezer]
- [NEEDS CLARIFICATION: Background job processing framework - Hangfire or Quartz.NET]
- [NEEDS CLARIFICATION: Frontend framework - Blazor Server/WASM, or SPA framework (React/Vue/Angular)]
- [NEEDS CLARIFICATION: Caching layer - Redis or in-memory cache]

**Storage**:
- Relational database (SQL Server, PostgreSQL, or SQLite for development)
- OAuth tokens encrypted at rest
- Cache for playlist/track metadata

**Testing**:
- xUnit for unit and integration tests
- [NEEDS CLARIFICATION: E2E testing framework - Playwright or Selenium]
- In-memory database for integration tests

**Target Platform**:
- Server: Linux/Windows hosting (Azure, AWS, or on-premises)
- Client: Modern web browsers (Chrome, Firefox, Safari, Edge - current and -1 version)
- Responsive web design for mobile browsers

**Project Type**: Web application (backend API + frontend)

**Performance Goals**:
- Dashboard load: <3 seconds for 100 playlists
- Sync operations: 90% complete within 30 seconds
- Search response: <2 seconds across all services
- API response time: <200ms p95 for non-sync endpoints
- Support 1000 concurrent users

**Constraints**:
- Must handle OAuth token refresh automatically
- Must respect music service API rate limits (exponential backoff)
- Must queue operations during service outages
- Must encrypt OAuth tokens and sensitive data
- Must support up to 3 accounts per service type per user
- Dashboard pagination required for >50 playlists

**Scale/Scope**:
- Initial target: 10,000 users
- 4 music services (Spotify, YouTube Music, Apple Music, Deezer)
- Up to 12 connected accounts per user (3 per service × 4 services)
- Support playlists with up to 10,000 tracks
- 6 user stories with 28 functional requirements

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Status**: The constitution template is not yet populated with project-specific principles. This feature will proceed with ASP.NET Core best practices:

- **Clean Architecture**: Separate concerns into layers (API, Business Logic, Data Access)
- **Dependency Injection**: Use built-in DI container for loose coupling
- **Test-First Approach**: Write tests before implementation for critical paths
- **Security First**: OAuth tokens encrypted, HTTPS required, CORS configured
- **API Versioning**: Support versioned endpoints for future changes
- **Logging & Monitoring**: Structured logging with correlation IDs
- **Configuration Management**: Environment-based configuration (appsettings.json, secrets)

**Gates**:
1. ✅ All OAuth implementations must use secure token storage (encrypted in database)
2. ✅ All external API calls must implement retry logic with exponential backoff
3. ✅ All long-running operations (copy/export/sync) must use background jobs
4. ✅ All endpoints must validate user authorization for service connections
5. ✅ All sensitive operations must be covered by integration tests

*Re-evaluation required after Phase 1 design to ensure compliance.*

## Project Structure

### Documentation (this feature)

```text
specs/001-multi-service-playlist-manager/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
│   ├── api-endpoints.yaml      # OpenAPI 3.0 specification
│   ├── service-integrations/
│   │   ├── spotify-adapter.md
│   │   ├── youtube-music-adapter.md
│   │   ├── apple-music-adapter.md
│   │   └── deezer-adapter.md
│   └── background-jobs.md
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
backend/
├── src/
│   ├── UniPlay.Api/                    # ASP.NET Core Web API project
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs
│   │   │   ├── ServiceConnectionsController.cs
│   │   │   ├── PlaylistsController.cs
│   │   │   └── PlaylistOperationsController.cs
│   │   ├── Middleware/
│   │   │   ├── ErrorHandlingMiddleware.cs
│   │   │   └── RequestLoggingMiddleware.cs
│   │   ├── Filters/
│   │   │   └── ServiceConnectionAuthorizationFilter.cs
│   │   ├── Models/
│   │   │   ├── Requests/
│   │   │   └── Responses/
│   │   ├── Program.cs
│   │   └── appsettings.json
│   │
│   ├── UniPlay.Core/                   # Business logic layer
│   │   ├── Services/
│   │   │   ├── IPlaylistService.cs
│   │   │   ├── PlaylistService.cs
│   │   │   ├── IServiceConnectionService.cs
│   │   │   ├── ServiceConnectionService.cs
│   │   │   ├── ISyncService.cs
│   │   │   ├── SyncService.cs
│   │   │   ├── ITrackMatchingService.cs
│   │   │   └── TrackMatchingService.cs
│   │   ├── Interfaces/
│   │   │   └── IMusicServiceAdapter.cs
│   │   ├── Models/
│   │   │   ├── Domain/
│   │   │   └── DTOs/
│   │   └── Exceptions/
│   │
│   ├── UniPlay.Infrastructure/         # Data access & external services
│   │   ├── Data/
│   │   │   ├── ApplicationDbContext.cs
│   │   │   ├── Entities/
│   │   │   └── Configurations/
│   │   ├── Repositories/
│   │   │   ├── IPlaylistRepository.cs
│   │   │   ├── PlaylistRepository.cs
│   │   │   ├── IServiceConnectionRepository.cs
│   │   │   └── ServiceConnectionRepository.cs
│   │   ├── MusicServices/
│   │   │   ├── SpotifyAdapter.cs
│   │   │   ├── YouTubeMusicAdapter.cs
│   │   │   ├── AppleMusicAdapter.cs
│   │   │   └── DeezerAdapter.cs
│   │   ├── BackgroundJobs/
│   │   │   ├── SyncPlaylistJob.cs
│   │   │   ├── CopyPlaylistJob.cs
│   │   │   └── ExportPlaylistJob.cs
│   │   └── Security/
│   │       └── TokenEncryptionService.cs
│   │
│   └── UniPlay.Shared/                 # Shared utilities & constants
│       ├── Constants/
│       │   └── MusicServiceTypes.cs
│       ├── Extensions/
│       └── Helpers/
│
├── tests/
│   ├── UniPlay.Api.Tests/
│   │   ├── Controllers/
│   │   └── Integration/
│   ├── UniPlay.Core.Tests/
│   │   └── Services/
│   └── UniPlay.Infrastructure.Tests/
│       ├── Repositories/
│       └── MusicServices/
│
└── docs/
    └── architecture/

frontend/
├── src/
│   ├── components/
│   │   ├── common/
│   │   ├── dashboard/
│   │   ├── playlists/
│   │   └── services/
│   ├── pages/
│   │   ├── Dashboard.jsx
│   │   ├── ServiceConnections.jsx
│   │   ├── PlaylistDetails.jsx
│   │   └── Auth/
│   ├── services/
│   │   ├── apiClient.js
│   │   ├── playlistService.js
│   │   └── authService.js
│   ├── hooks/
│   ├── utils/
│   └── App.jsx
│
└── tests/
    ├── unit/
    └── e2e/
```

**Structure Decision**:

This is a **web application** structure with clear separation between backend and frontend:

**Backend** follows Clean Architecture principles:
- **UniPlay.Api**: Presentation layer with controllers, middleware, and API models
- **UniPlay.Core**: Business logic layer with services and domain models (framework-agnostic)
- **UniPlay.Infrastructure**: Infrastructure layer with data access, external API adapters, and background jobs
- **UniPlay.Shared**: Cross-cutting concerns and utilities

**Frontend** uses component-based architecture:
- Components organized by feature/domain
- Centralized API client for backend communication
- Separation of concerns between UI, business logic, and data fetching

This structure supports:
- Independent deployment of frontend and backend
- Easy testing at each layer
- Clean separation of concerns
- Scalability for adding new music services

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

No violations to report at this stage. The architecture follows ASP.NET Core best practices and maintains simplicity while meeting all functional requirements.

## Research Tasks (Phase 0)

The following items are marked as "NEEDS CLARIFICATION" in Technical Context and require research:

### 1. OAuth Client Libraries for Music Services
**Question**: Which OAuth/API client libraries should we use for Spotify, YouTube Music, Apple Music, and Deezer?
**Research Goal**: Identify maintained, well-documented libraries with OAuth 2.0 support for each service
**Success Criteria**: Library recommendations with pros/cons, or decision to build custom adapters

### 2. Background Job Processing Framework
**Question**: Should we use Hangfire, Quartz.NET, or another background job framework?
**Research Goal**: Compare frameworks for ASP.NET Core 9.0 compatibility, persistence, monitoring, and scalability
**Success Criteria**: Framework recommendation with justification

### 3. Frontend Framework Selection
**Question**: Should we use Blazor (Server/WASM) for C# full-stack, or a SPA framework (React/Vue/Angular)?
**Research Goal**: Evaluate trade-offs for team skills, performance, and ecosystem maturity
**Success Criteria**: Framework decision with rationale

### 4. Caching Strategy
**Question**: Should we use Redis for distributed caching or in-memory cache for simplicity?
**Research Goal**: Determine caching needs for playlist metadata, considering scalability and deployment
**Success Criteria**: Caching approach recommendation

### 5. Track Matching Algorithm
**Question**: What algorithm and fuzzy matching library should we use for cross-service track matching?
**Research Goal**: Research track matching approaches (metadata comparison, acoustic fingerprinting), fuzzy string matching libraries
**Success Criteria**: Algorithm design and library recommendation (e.g., Levenshtein distance, FuzzySharp)

### 6. Database Selection
**Question**: Should we use SQL Server (Azure SQL), PostgreSQL, or start with SQLite for development?
**Research Goal**: Evaluate database options for Entity Framework Core support, deployment environments, and cost
**Success Criteria**: Database recommendation for development and production

### 7. OAuth Token Security Best Practices
**Question**: How should we encrypt OAuth tokens at rest? Key management approach?
**Research Goal**: Research ASP.NET Core Data Protection API, Azure Key Vault integration, or custom encryption
**Success Criteria**: Token encryption implementation approach

### 8. Rate Limiting and Retry Patterns
**Question**: What libraries/patterns should we use for handling music service API rate limits?
**Research Goal**: Research Polly for retry policies, rate limiting patterns, circuit breakers
**Success Criteria**: Resilience strategy with library recommendations

### 9. E2E Testing Framework
**Question**: Should we use Playwright, Selenium, or another E2E testing tool?
**Research Goal**: Evaluate E2E frameworks for .NET compatibility, cross-browser support, and maintainability
**Success Criteria**: Testing framework recommendation

### 10. API Documentation and Versioning
**Question**: Should we use Swagger/OpenAPI, NSwag, or Swashbuckle for API documentation? What versioning strategy?
**Research Goal**: Best practices for API documentation and versioning in ASP.NET Core 9.0
**Success Criteria**: Documentation tooling and versioning approach (URL-based, header-based, etc.)

---

## Planning Completion Summary

**Status**: ✅ Phase 0 and Phase 1 COMPLETE

**Completed Artifacts**:
1. ✅ **research.md** - All 10 research questions resolved with comprehensive technology decisions
2. ✅ **data-model.md** - Complete data model with 8 entities, PostgreSQL schema, EF Core configurations
3. ✅ **contracts/api-endpoints.yaml** - OpenAPI 3.0 specification with all endpoints documented
4. ✅ **contracts/service-integrations/** - Adapter contracts for all 4 music services:
   - spotify-adapter.md
   - apple-music-adapter.md
   - deezer-adapter.md
   - youtube-music-adapter.md
5. ✅ **contracts/background-jobs.md** - Hangfire job specifications for all async operations
6. ✅ **quickstart.md** - Developer onboarding guide with setup instructions

**Key Decisions Made** (see research.md for details):
- **Backend**: ASP.NET Core 9.0 + PostgreSQL + Hangfire
- **Frontend**: React 18 + TypeScript + Vite
- **OAuth**: SpotifyAPI.Web + custom adapters for Apple Music/Deezer
- **Caching**: IMemoryCache (MVP) → Redis (scale)
- **Track Matching**: FuzzySharp + ISRC codes (88-92% accuracy expected)
- **Testing**: Playwright for .NET for E2E tests
- **API Docs**: Scalar + NSwag + URL versioning

**Next Steps**:
- Execute `/speckit.tasks` command to generate actionable implementation tasks (tasks.md)
- Break down implementation into dependency-ordered development work
- Begin Phase 2: Implementation
