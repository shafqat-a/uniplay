# Developer Quickstart: Multi-Service Playlist Manager

**Feature**: Multi-Service Playlist Manager
**Date**: 2025-10-24
**Target Audience**: Developers joining the UniPlay project

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Project Setup](#project-setup)
3. [Database Setup](#database-setup)
4. [Music Service Configuration](#music-service-configuration)
5. [Running the Application](#running-the-application)
6. [Development Workflow](#development-workflow)
7. [Testing](#testing)
8. [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Required Software
- **.NET 9.0 SDK** or later
  ```bash
  dotnet --version  # Should be 9.0.0 or higher
  ```

- **Node.js 20+** and **npm** (for frontend)
  ```bash
  node --version  # Should be 20.0.0 or higher
  npm --version
  ```

- **PostgreSQL 16** (or Docker for local PostgreSQL)
  ```bash
  psql --version  # Should be 16.0 or higher
  ```

- **Git**
  ```bash
  git --version
  ```

### Recommended Tools
- **Visual Studio 2022** or **VS Code** with C# extension
- **Docker Desktop** (for local PostgreSQL and Redis)
- **Postman** or **Insomnia** (for API testing)
- **Azure Data Studio** or **pgAdmin** (for database management)

---

## Project Setup

### 1. Clone the Repository

```bash
git clone https://github.com/your-org/uniplay.git
cd uniplay
```

### 2. Install Backend Dependencies

```bash
cd backend/src/UniPlay.Api
dotnet restore
```

### 3. Install Frontend Dependencies

```bash
cd ../../../frontend
npm install
```

### 4. Configure User Secrets (Development)

The backend uses **User Secrets** for sensitive configuration in development:

```bash
cd backend/src/UniPlay.Api

# Initialize user secrets
dotnet user-secrets init

# Add database connection string
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=uniplay_dev;Username=postgres;Password=yourpassword"

# Add JWT settings
dotnet user-secrets set "JwtSettings:SecretKey" "your-256-bit-secret-key-here-must-be-at-least-32-characters-long"
dotnet user-secrets set "JwtSettings:Issuer" "UniPlay"
dotnet user-secrets set "JwtSettings:Audience" "UniPlayClient"
dotnet user-secrets set "JwtSettings:ExpirationMinutes" "60"

# Music service credentials (add after registration - see next section)
dotnet user-secrets set "Spotify:ClientId" "your-spotify-client-id"
dotnet user-secrets set "Spotify:ClientSecret" "your-spotify-client-secret"

dotnet user-secrets set "AppleMusic:TeamId" "your-apple-team-id"
dotnet user-secrets set "AppleMusic:KeyId" "your-apple-key-id"
dotnet user-secrets set "AppleMusic:PrivateKeyPath" "/path/to/AuthKey_KEYID.p8"

dotnet user-secrets set "Deezer:AppId" "your-deezer-app-id"
dotnet user-secrets set "Deezer:SecretKey" "your-deezer-secret-key"
```

---

## Database Setup

### Option 1: Local PostgreSQL with Docker (Recommended)

```bash
# Start PostgreSQL container
docker run --name uniplay-postgres \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=yourpassword \
  -e POSTGRES_DB=uniplay_dev \
  -p 5432:5432 \
  -d postgres:16

# Verify container is running
docker ps | grep uniplay-postgres
```

### Option 2: Existing PostgreSQL Installation

Create a database manually:

```sql
CREATE DATABASE uniplay_dev;
CREATE USER uniplay WITH PASSWORD 'yourpassword';
GRANT ALL PRIVILEGES ON DATABASE uniplay_dev TO uniplay;
```

### Apply EF Core Migrations

```bash
cd backend/src/UniPlay.Api

# Apply migrations to create tables
dotnet ef database update

# Verify tables were created
psql -U postgres -d uniplay_dev -c "\dt"
```

**Expected Output**:
```
 Schema |           Name               | Type  | Owner
--------+-----------------------------+-------+--------
 public | user_accounts                | table | uniplay
 public | service_connections          | table | uniplay
 public | platform_playlists           | table | uniplay
 public | service_playlists            | table | uniplay
 public | tracks                       | table | uniplay
 public | playlist_track_associations  | table | uniplay
 public | sync_queue                   | table | uniplay
 public | copy_operations              | table | uniplay
```

---

## Music Service Configuration

You need to register applications with each music service to get API credentials.

### Spotify Configuration

1. **Register Application**
   - Go to: https://developer.spotify.com/dashboard
   - Click "Create an App"
   - App Name: "UniPlay Development"
   - Redirect URI: `http://localhost:5000/api/v1/service-connections/callback`

2. **Get Credentials**
   - Copy **Client ID** and **Client Secret**
   - Add to user secrets (see step 4 in Project Setup)

3. **Test Connection**
   ```bash
   curl http://localhost:5000/api/v1/service-connections \
     -X POST \
     -H "Authorization: Bearer YOUR_JWT_TOKEN" \
     -H "Content-Type: application/json" \
     -d '{"serviceType":"Spotify","redirectUri":"http://localhost:5000/callback"}'
   ```

### Apple Music Configuration

1. **Enroll in Apple Developer Program** ($99/year)
   - https://developer.apple.com/programs/

2. **Register MusicKit Identifier**
   - Go to: https://developer.apple.com/account/resources/identifiers
   - Click "+" → "Services IDs" → "MusicKit"
   - Name: "UniPlay Development"

3. **Generate Private Key**
   - Go to: https://developer.apple.com/account/resources/authkeys
   - Click "+" → Select "MusicKit" → Continue
   - Download `.p8` file (save securely, cannot re-download)
   - Note: Key ID and Team ID

4. **Add to User Secrets**
   ```bash
   dotnet user-secrets set "AppleMusic:TeamId" "ABC123DEF4"
   dotnet user-secrets set "AppleMusic:KeyId" "XXXXXXXXXX"
   dotnet user-secrets set "AppleMusic:PrivateKeyPath" "/Users/you/Downloads/AuthKey_XXXXXXXXXX.p8"
   ```

### Deezer Configuration

1. **Register Application**
   - Go to: https://developers.deezer.com/myapps
   - Click "Create a new application"
   - Application Domain: `localhost:5000`
   - Redirect URL: `http://localhost:5000/callback`

2. **Get Credentials**
   - Copy **Application ID** and **Secret Key**
   - Add to user secrets

3. **Test Connection**
   ```bash
   curl http://localhost:5000/api/v1/service-connections \
     -X POST \
     -H "Authorization: Bearer YOUR_JWT_TOKEN" \
     -H "Content-Type: application/json" \
     -d '{"serviceType":"Deezer","redirectUri":"http://localhost:5000/callback"}'
   ```

### YouTube Music (Optional - Not Recommended for MVP)

See `contracts/service-integrations/youtube-music-adapter.md` for limitations. Skip for initial development.

---

## Running the Application

### Start Backend API

```bash
cd backend/src/UniPlay.Api

# Run the API
dotnet run

# Or with hot reload
dotnet watch run
```

**Expected Output**:
```
info: Microsoft.Hosting.Lifetime[0]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Now listening on: https://localhost:5001
```

**Verify API is running**:
```bash
curl http://localhost:5000/api/v1/health
```

**Expected Response**: `{"status":"Healthy"}`

### Start Frontend (React + Vite)

```bash
cd frontend

# Start development server
npm run dev
```

**Expected Output**:
```
VITE v5.0.0  ready in 500 ms

➜  Local:   http://localhost:3000/
➜  Network: http://192.168.1.100:3000/
```

**Access the application**: Open http://localhost:3000 in your browser

### Start Hangfire Dashboard

Hangfire dashboard is automatically available at:

```
http://localhost:5000/hangfire
```

Login required (default: Admin user created by seed data)

---

## Development Workflow

### 1. Create a New Feature Branch

```bash
git checkout -b feature/add-playlist-search
```

### 2. Make Changes

Follow the project structure:

```
backend/src/
├── UniPlay.Api/          # Controllers, middleware
├── UniPlay.Core/         # Business logic
├── UniPlay.Infrastructure/  # Data access, external APIs
└── UniPlay.Shared/       # Utilities

frontend/src/
├── components/           # React components
├── pages/                # Page components
├── services/             # API clients
└── hooks/                # Custom hooks
```

### 3. Run Tests

```bash
# Backend unit tests
cd backend/tests/UniPlay.Core.Tests
dotnet test

# Backend integration tests
cd ../UniPlay.Api.Tests
dotnet test

# Frontend tests
cd ../../../frontend
npm test
```

### 4. Add Database Migration (if schema changed)

```bash
cd backend/src/UniPlay.Api

dotnet ef migrations add AddPlaylistSearchFeature

# Apply migration
dotnet ef database update
```

### 5. Commit Changes

```bash
git add .
git commit -m "feat: add playlist search functionality"
git push origin feature/add-playlist-search
```

### 6. Create Pull Request

- Open PR on GitHub
- Automated checks run (tests, linting, build)
- Request code review

---

## Testing

### Backend Unit Tests (xUnit)

```bash
cd backend/tests/UniPlay.Core.Tests
dotnet test --logger "console;verbosity=detailed"
```

### Backend Integration Tests

```bash
cd backend/tests/UniPlay.Api.Tests
dotnet test
```

**Integration tests use in-memory database** (no PostgreSQL required)

### Frontend Unit Tests (Vitest)

```bash
cd frontend
npm test
```

### E2E Tests (Playwright)

```bash
cd backend/tests/UniPlay.E2E.Tests

# First-time setup: install browsers
pwsh bin/Debug/net9.0/playwright.ps1 install

# Run E2E tests
dotnet test
```

### Manual API Testing

Use the Scalar API documentation UI:

```
http://localhost:5000/scalar/v1
```

Or import `contracts/api-endpoints.yaml` into Postman.

---

## Troubleshooting

### Issue: Database connection fails

**Error**: `Npgsql.NpgsqlException: could not connect to server`

**Solution**:
1. Verify PostgreSQL is running: `docker ps` or `pg_isready`
2. Check connection string in user secrets
3. Test connection:
   ```bash
   psql -U postgres -h localhost -d uniplay_dev
   ```

### Issue: EF Core migrations fail

**Error**: `A connection was successfully established, but then an error occurred during the pre-login handshake`

**Solution**:
1. Delete existing database: `dropdb uniplay_dev`
2. Recreate: `createdb uniplay_dev`
3. Re-apply migrations: `dotnet ef database update`

### Issue: Hangfire dashboard shows "No jobs found"

**Cause**: Recurring jobs not registered

**Solution**:
1. Verify `RecurringJob.AddOrUpdate` calls in `Program.cs`
2. Restart API
3. Check Hangfire logs for errors

### Issue: Spotify OAuth returns 401 Unauthorized

**Cause**: Invalid Client ID or Secret

**Solution**:
1. Verify credentials in user secrets:
   ```bash
   dotnet user-secrets list | grep Spotify
   ```
2. Regenerate Client Secret in Spotify Dashboard
3. Update user secrets

### Issue: Frontend cannot connect to backend

**Error**: `Failed to fetch` or `CORS error`

**Solution**:
1. Verify backend is running: `curl http://localhost:5000/api/v1/health`
2. Check CORS configuration in `Program.cs`:
   ```csharp
   builder.Services.AddCors(options =>
   {
       options.AddDefaultPolicy(policy =>
       {
           policy.WithOrigins("http://localhost:3000")
                 .AllowAnyMethod()
                 .AllowAnyHeader();
       });
   });
   ```
3. Restart backend

### Issue: Rate limiting blocks requests during development

**Solution**: Disable rate limiting in `appsettings.Development.json`:
```json
{
  "RateLimiting": {
    "Enabled": false
  }
}
```

---

## Next Steps

1. **Read the Architecture Documentation**
   - `docs/architecture/clean-architecture.md`
   - `specs/001-multi-service-playlist-manager/data-model.md`

2. **Review API Contracts**
   - `specs/001-multi-service-playlist-manager/contracts/api-endpoints.yaml`
   - Test endpoints using Scalar UI

3. **Explore Service Adapters**
   - `backend/src/UniPlay.Infrastructure/MusicServices/SpotifyAdapter.cs`
   - See `contracts/service-integrations/` for implementation contracts

4. **Run Through User Scenarios**
   - Register user account
   - Connect Spotify account
   - View unified dashboard
   - Create platform playlist
   - Copy playlist to another service

5. **Set Up Your IDE**
   - Install recommended VS Code extensions (C#, ESLint, Prettier)
   - Configure EditorConfig
   - Set up debugger launch configurations

---

## Useful Commands

### Backend

```bash
# Build solution
dotnet build

# Run with hot reload
dotnet watch run

# Create new migration
dotnet ef migrations add MigrationName

# Rollback migration
dotnet ef database update PreviousMigrationName

# Generate SQL script from migrations
dotnet ef migrations script

# Run specific test
dotnet test --filter "FullyQualifiedName~SpotifyAdapterTests"
```

### Frontend

```bash
# Development server
npm run dev

# Build for production
npm run build

# Preview production build
npm run preview

# Lint code
npm run lint

# Format code
npm run format
```

### Docker

```bash
# Start all services (PostgreSQL + Redis)
docker-compose up -d

# View logs
docker-compose logs -f

# Stop all services
docker-compose down

# Remove volumes (wipes data)
docker-compose down -v
```

---

## Getting Help

- **Slack**: #uniplay-dev channel
- **GitHub Issues**: Report bugs or request features
- **Documentation**: `docs/` directory
- **Team Lead**: @your-team-lead

---

## Code Style Guidelines

### C# (.NET)
- Follow [Microsoft C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use meaningful variable names
- Add XML comments for public APIs
- Maximum line length: 120 characters

### TypeScript (React)
- Follow [Airbnb JavaScript Style Guide](https://github.com/airbnb/javascript)
- Use functional components with hooks
- Prefer named exports
- Maximum line length: 100 characters

### Git Commit Messages
- Follow [Conventional Commits](https://www.conventionalcommits.org/)
- Format: `<type>(<scope>): <description>`
- Examples:
  - `feat(api): add playlist search endpoint`
  - `fix(ui): resolve playlist drag-drop issue`
  - `docs(readme): update setup instructions`

---

**Happy Coding! 🎵**
