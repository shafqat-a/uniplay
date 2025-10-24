# UniPlay Deployment Guide

**Last Updated**: 2025-10-24
**Version**: 1.0.0 (User Story 1 Complete)
**Status**: Ready for Development/Testing Deployment

## Prerequisites

### Required Software
- **.NET 9.0 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Node.js 20+** - [Download](https://nodejs.org/)
- **Docker Desktop** - [Download](https://www.docker.com/products/docker-desktop)
- **PostgreSQL 16** (via Docker or standalone)
- **Git** - For cloning the repository

### Development Tools (Optional but Recommended)
- **Visual Studio Code** with C# Dev Kit
- **Rider** or **Visual Studio 2022**
- **Postman** or **Bruno** for API testing

## Quick Start (Development)

### 1. Clone and Setup

```bash
# Clone the repository
git clone <repository-url>
cd uniplay

# Start PostgreSQL and Redis
docker-compose up -d

# Wait for containers to be healthy
docker-compose ps
```

### 2. Configure Backend

```bash
cd backend/src/UniPlay.Api

# Copy environment template
cp appsettings.json appsettings.Development.json

# Set up user secrets (recommended for development)
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=uniplay_dev;Username=postgres;Password=yourpassword"
dotnet user-secrets set "JwtSettings:SecretKey" "your-secure-256-bit-secret-key-minimum-32-characters-required"
```

### 3. Configure OAuth Services

#### Spotify OAuth

1. Go to [Spotify Developer Dashboard](https://developer.spotify.com/dashboard)
2. Create a new app
3. Add redirect URI: `https://localhost:5001/api/v1/auth/spotify/callback`
4. Copy Client ID and Client Secret

```bash
# Add Spotify credentials to user secrets
dotnet user-secrets set "Spotify:ClientId" "your-spotify-client-id"
dotnet user-secrets set "Spotify:ClientSecret" "your-spotify-client-secret"
```

#### YouTube Music OAuth (Google OAuth 2.0)

1. Go to [Google Cloud Console](https://console.cloud.google.com)
2. Create a new project or select existing
3. **Enable YouTube Data API v3** in the API Library
4. Create OAuth 2.0 credentials:
   - Go to Credentials → Create Credentials → OAuth 2.0 Client ID
   - Application type: Web application
   - Add redirect URI: `https://localhost:5001/api/v1/services/youtubemusic/callback`
5. Create an API Key (optional, for quota tracking):
   - Go to Credentials → Create Credentials → API Key
   - Restrict to YouTube Data API v3 (recommended)
6. Copy Client ID, Client Secret, and API Key

```bash
# Add YouTube Music credentials to user secrets
dotnet user-secrets set "YouTubeMusic:ClientId" "your-google-client-id"
dotnet user-secrets set "YouTubeMusic:ClientSecret" "your-google-client-secret"
dotnet user-secrets set "YouTubeMusic:ApiKey" "your-youtube-data-api-key"
```

**Important Notes:**
- The API Key is optional when using OAuth but recommended for quota tracking
- YouTube Data API v3 has daily quota limits (10,000 units/day by default)
- Fetching playlists costs approximately 1-3 units per request
- OAuth scopes required: `youtube` and `youtube.readonly`

### 4. Apply Database Migrations

```bash
cd /path/to/uniplay/backend/src/UniPlay.Infrastructure

# Create/update database schema
dotnet ef database update --startup-project ../UniPlay.Api/UniPlay.Api.csproj --context ApplicationDbContext
```

### 5. Start Backend

```bash
cd ../UniPlay.Api

# Run the API
dotnet run

# API will be available at:
# - HTTPS: https://localhost:5001
# - HTTP: http://localhost:5000
# - Hangfire Dashboard: https://localhost:5001/hangfire
# - API Docs (Scalar): https://localhost:5001/scalar/v1
```

### 6. Setup and Start Frontend

```bash
cd ../../../frontend

# Install dependencies
npm install

# Create environment file
echo "VITE_API_URL=https://localhost:5001/api/v1" > .env.local

# Start development server
npm run dev

# Frontend will be available at: http://localhost:5173
```

## Database Migrations

### Create New Migration

```bash
cd backend/src/UniPlay.Infrastructure

dotnet ef migrations add <MigrationName> \
  --startup-project ../UniPlay.Api/UniPlay.Api.csproj \
  --context ApplicationDbContext
```

### Apply Migrations

```bash
dotnet ef database update \
  --startup-project ../UniPlay.Api/UniPlay.Api.csproj \
  --context ApplicationDbContext
```

### Rollback Migration

```bash
# Rollback to specific migration
dotnet ef database update <PreviousMigrationName> \
  --startup-project ../UniPlay.Api/UniPlay.Api.csproj \
  --context ApplicationDbContext

# Remove last migration
dotnet ef migrations remove \
  --startup-project ../UniPlay.Api/UniPlay.Api.csproj \
  --context ApplicationDbContext
```

## Environment Variables

### Backend (appsettings.json / User Secrets)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=uniplay_dev;Username=postgres;Password=yourpassword"
  },
  "JwtSettings": {
    "SecretKey": "your-256-bit-secret-key-minimum-32-characters",
    "Issuer": "UniPlay",
    "Audience": "UniPlayClient",
    "ExpirationMinutes": 60
  },
  "Spotify": {
    "ClientId": "your-spotify-client-id",
    "ClientSecret": "your-spotify-client-secret",
    "RedirectUri": "https://localhost:5001/api/v1/auth/spotify/callback"
  },
  "YouTubeMusic": {
    "ClientId": "your-google-client-id",
    "ClientSecret": "your-google-client-secret",
    "RedirectUri": "https://localhost:5001/api/v1/services/youtubemusic/callback",
    "Scopes": "https://www.googleapis.com/auth/youtube https://www.googleapis.com/auth/youtube.readonly",
    "ApiKey": "your-youtube-data-api-key"
  },
  "Hangfire": {
    "WorkerCount": 10,
    "PollingIntervalSeconds": 15
  }
}
```

### Frontend (.env.local)

```env
VITE_API_URL=https://localhost:5001/api/v1
```

## Docker Compose Services

The `docker-compose.yml` file provides:

### PostgreSQL 16
- **Port**: 5432
- **Database**: uniplay_dev
- **Username**: postgres
- **Password**: yourpassword (change in production!)
- **Volume**: postgres_data

### Redis 7
- **Port**: 6379
- **Volume**: redis_data

### Commands

```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop all services
docker-compose down

# Stop and remove volumes
docker-compose down -v
```

## Background Jobs (Hangfire)

### Configured Jobs

1. **Token Refresh Job** - Runs hourly
   - Refreshes OAuth tokens expiring within 1 hour
   - Ensures connections stay active

2. **Playlist Sync Job** - Runs daily
   - Syncs playlists from all active connections
   - Detects added, updated, and removed playlists

### Accessing Hangfire Dashboard

- **URL**: https://localhost:5001/hangfire
- **Auth**: Development mode (no auth required)
- **Production**: Implement proper authentication

## API Endpoints

### Authentication
- `POST /api/v1/auth/register` - User registration
- `POST /api/v1/auth/login` - User login

### Service Connections
- `GET /api/v1/services/connections` - Get all connections
- `GET /api/v1/services/connections/{id}` - Get specific connection
- `GET /api/v1/services/{serviceType}/connections` - Get connections by service
- `POST /api/v1/services/connect` - Initiate OAuth flow
- `POST /api/v1/services/callback` - Complete OAuth flow
- `DELETE /api/v1/services/connections/{id}` - Disconnect service
- `POST /api/v1/services/connections/{id}/refresh` - Refresh token

### Playlists
- `GET /api/v1/playlists` - Get all user playlists
- `GET /api/v1/playlists/connection/{id}` - Get playlists for connection
- `POST /api/v1/playlists/sync` - Sync specific connection
- `POST /api/v1/playlists/sync/all` - Sync all connections

### System
- `GET /health` - Health check endpoint

## Testing

### Backend Tests

```bash
cd backend

# Run all tests
dotnet test

# Run specific test project
dotnet test tests/UniPlay.Api.Tests
dotnet test tests/UniPlay.Core.Tests
dotnet test tests/UniPlay.Infrastructure.Tests

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Manual API Testing

1. Register a user: `POST /api/v1/auth/register`
2. Login: `POST /api/v1/auth/login` (save JWT token)
3. Connect Spotify: `POST /api/v1/services/connect`
4. Complete OAuth callback
5. Sync playlists: `POST /api/v1/playlists/sync/all`
6. View playlists: `GET /api/v1/playlists`

## Production Deployment

### Security Checklist

- [ ] Use strong JWT secret key (256-bit minimum)
- [ ] Enable HTTPS only (`RequireHttpsMetadata = true`)
- [ ] Configure CORS for specific frontend domain
- [ ] Use user secrets or environment variables (never commit secrets)
- [ ] Implement Hangfire dashboard authentication
- [ ] Use strong PostgreSQL password
- [ ] Enable database backups
- [ ] Configure rate limiting
- [ ] Enable request logging
- [ ] Set up monitoring and alerting

### Database

```bash
# Production migration
export ConnectionStrings__DefaultConnection="<production-connection-string>"
dotnet ef database update --startup-project src/UniPlay.Api --context ApplicationDbContext
```

### Backend Deployment

```bash
cd backend/src/UniPlay.Api

# Publish optimized build
dotnet publish -c Release -o ./publish

# The publish folder contains all files needed to deploy
```

### Frontend Deployment

```bash
cd frontend

# Build for production
npm run build

# The dist folder contains static files ready to deploy
# Deploy to: Vercel, Netlify, AWS S3 + CloudFront, Azure Static Web Apps, etc.
```

### Environment-Specific Configuration

**Production appsettings.Production.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "yourdomain.com"
}
```

## Troubleshooting

### Docker Issues

**PostgreSQL won't start:**
```bash
# Check if port 5432 is already in use
lsof -i :5432

# Kill the process or change port in docker-compose.yml
```

**Permission denied errors:**
```bash
# Fix volume permissions
sudo chown -R $USER:$USER postgres_data redis_data
```

### Migration Issues

**Assembly mismatch error:**
```bash
# Always run migrations from Infrastructure project
cd backend/src/UniPlay.Infrastructure
dotnet ef migrations add MigrationName --startup-project ../UniPlay.Api/UniPlay.Api.csproj
```

**Connection refused:**
- Ensure PostgreSQL is running: `docker-compose ps`
- Check connection string in appsettings.json or user secrets

### OAuth Issues

**Invalid redirect URI:**
- Ensure redirect URI in Spotify dashboard matches exactly: `https://localhost:5001/api/v1/auth/spotify/callback`
- Check HTTPS vs HTTP

**State validation failed:**
- Clear browser session storage
- Try OAuth flow in incognito/private mode

### Frontend API Calls Failing

**CORS errors:**
- Check CORS configuration in Program.cs
- Ensure frontend origin is allowed

**401 Unauthorized:**
- Check if JWT token is expired
- Verify token is being sent in Authorization header

## Monitoring

### Health Checks

```bash
# Check API health
curl https://localhost:5001/health

# Expected response:
# {"status":"healthy","timestamp":"2025-10-24T..."}
```

### Logs

**Backend:**
- Console logs in development
- Configure Serilog or NLog for production

**Hangfire:**
- View job execution history in dashboard
- Failed jobs are automatically retried

**Database:**
```bash
# Connect to PostgreSQL
docker exec -it uniplay-postgres psql -U postgres -d uniplay_dev

# Check tables
\dt

# View connections
SELECT * FROM service_connections;

# View playlists
SELECT * FROM service_playlists;
```

## Performance Optimization

### Database
- Indexes are already configured in EF Core configurations
- Consider connection pooling tuning for high load
- Regular VACUUM and ANALYZE operations

### API
- Polly resilience policies configured (retry, circuit breaker, timeout)
- Response caching can be added for read-heavy endpoints
- Consider Redis for distributed caching in multi-instance deployment

### Frontend
- Code splitting is automatic with Vite
- Consider lazy loading routes for large applications
- Optimize images (use CDN for playlist cover images)

## Support

For issues or questions:
1. Check IMPLEMENTATION_STATUS.md for current progress
2. Review specs/001-multi-service-playlist-manager/ for detailed documentation
3. Check GitHub issues
4. Contact development team

---

**Status**: ✅ User Story 1 Complete - Ready for Development/Testing
**Next**: User Story 2 - Additional music service integrations (Apple Music, Deezer)
