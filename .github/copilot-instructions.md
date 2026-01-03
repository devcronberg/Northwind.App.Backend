# Northwind.App.Backend - AI Instructions

## Project Overview

This is an ASP.NET Core REST API backend application serving as a demo/reference implementation for best practices. It provides a complete RESTful API for the Northwind database with both public and authenticated endpoints.

## Technology Stack

- **.NET 10**
- **ASP.NET Core Web API**
- **Entity Framework Core** with SQLite database
- **JWT Bearer Authentication** for secure endpoints
- **Serilog** for structured logging
- **Swashbuckle** for OpenAPI/Swagger documentation

## Architecture & Best Practices

### Implemented Patterns

1. **JWT Authentication** - Token-based authentication with access and refresh tokens
2. **Entity Framework Core** - Code-first approach with SQLite database
3. **Structured Logging** - Serilog with console sink (container-ready)
4. **Health Checks** - Liveness and Readiness endpoints for container orchestration
5. **Problem Details (RFC 7807)** - Consistent error response format
6. **Response Compression** - Gzip/Brotli enabled for HTTPS
7. **CORS** - Configured to allow all origins (demo purposes)
8. **OpenAPI/Swagger** - Full API documentation with annotations and JWT authentication support
9. **Docker Support** - Multi-stage Dockerfile optimized for container deployment
10. **Cloud Deployment** - render.yaml configuration for Render.com deployment

### Code Guidelines

- **Language**: All code, comments, logs, and documentation must be in **English**
- **Logging**: Use `ILogger<T>` (backed by Serilog), never `Console.WriteLine`
- **Controllers**: Use proper `[ProducesResponseType]` attributes for Swagger
- **Async**: Use async/await for I/O operations
- **Error Handling**: Let exceptions bubble up - global handler returns Problem Details
- **Authentication**: Use `[Authorize]` attribute for protected endpoints
- **Database**: Use `AsNoTracking()` for read-only queries

### Project Structure

```
├── Controllers/                     # API controllers
│   ├── SystemController.cs          # Health, version, config endpoints
│   ├── AuthController.cs            # JWT authentication (login, refresh, logout)
│   ├── CustomersController.cs       # Protected customer endpoints
│   └── PublicCustomersController.cs # Public customer endpoints
├── Models/
│   ├── EF/                          # Entity Framework models
│   │   ├── NorthwindContext.cs      # DbContext for Northwind database
│   │   ├── Customer.cs              # Customer entity
│   │   ├── Order.cs                 # Order entity
│   │   ├── Product.cs               # Product entity
│   │   └── [other entities...]
│   └── MVC/                         # Controllers in wrong folder (legacy)
├── Assets/
│   └── Northwind.db                 # SQLite database file
├── Program.cs                       # Application entry point & configuration
├── appsettings.json                 # Configuration (Serilog, JWT, ConnectionStrings)
├── Dockerfile                       # Multi-stage Docker build configuration
├── .dockerignore                    # Docker build exclusions
├── render.yaml                      # Render.com deployment configuration
└── .github/
    └── copilot-instructions.md      # This file
```

### API Endpoints

#### System Endpoints
| Endpoint | Purpose |
|----------|---------|
| `GET /health` | Basic health check (built-in) |
| `GET /health/live` | Liveness probe for Kubernetes/containers |
| `GET /health/ready` | Readiness probe with dependency checks |
| `GET /version` | Application version |
| `GET /config` | Runtime configuration info |
| `GET /test` | Echo endpoint for testing |
| `GET /test/error` | Throws exception to demo Problem Details |

#### Authentication Endpoints
| Endpoint | Purpose |
|----------|---------|
| `POST /api/auth/login` | Login with username/password, returns JWT tokens |
| `POST /api/auth/refresh` | Refresh access token using refresh token |
| `POST /api/auth/logout` | Logout and invalidate refresh token |
| `POST /api/auth/logout-all` | Logout from all devices |
| `GET /api/auth/me` | Get current user info (requires auth) |

**Demo Users:**
- Username: `admin`, Password: `admin`, Role: `Admin`
- Username: `user`, Password: `user`, Role: `User`

#### Public Customer Endpoints (No Authentication)
| Endpoint | Purpose |
|----------|---------|
| `GET /api/public/customers` | Get all customers |
| `GET /api/public/customers/{id}` | Get customer by ID |
| `GET /api/public/customers/{id}/orders` | Get customer with orders |
| `POST /api/public/customers` | Create new customer |
| `PUT /api/public/customers/{id}` | Update customer |
| `DELETE /api/public/customers/{id}` | Delete customer |

#### Protected Customer Endpoints (Requires Authentication)
| Endpoint | Purpose |
|----------|---------|
| `GET /api/customers` | Get all customers (requires JWT) |

### JWT Configuration

JWT settings are configured in `appsettings.json`:
```json
{
  "Jwt": {
    "Secret": "ThisIsASecretKeyForDemoOnlyChangeInProduction123!",
    "Issuer": "Northwind.App.Backend",
    "Audience": "Northwind.App.Frontend",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  }
}
```

**Important**: In production, use environment variables for the JWT secret, never hardcode it.

### Database Configuration

- **Database**: SQLite (file-based, included in Assets folder)
- **Connection String**: `Data Source=Assets/Northwind.db`
- **Schema**: Classic Northwind database (Customers, Orders, Products, etc.)
- **Access**: Read-only in container (file system is ephemeral)

### Container & Deployment

#### Docker
- **Dockerfile**: Multi-stage build (build, publish, final)
- **Base Images**: 
  - Build: `mcr.microsoft.com/dotnet/sdk:10.0`
  - Runtime: `mcr.microsoft.com/dotnet/aspnet:10.0`
- **Security**: Runs as non-root user (UID/GID 1001)
- **Port**: Configurable via `PORT` environment variable (default: 8080)
- **Size**: Optimized with layer caching and minimal runtime image

#### Render.com Deployment
- **Configuration**: `render.yaml` (Blueprint)
- **Health Check**: `/health/live`
- **Environment Variables**: JWT settings configured via Render dashboard
- **Region**: Frankfurt (configurable in render.yaml)
- **Plan**: Free tier supported (spins down after 15 min inactivity)

To deploy:
1. Push code to GitHub
2. Connect repository to Render.com
3. Use Blueprint deployment (auto-detects render.yaml)
4. Application will be available at `https://[app-name].onrender.com`

### Swagger/OpenAPI

Access interactive API documentation at: `/swagger`

Features:
- All endpoints documented with XML comments
- Request/response schemas
- Try-it-out functionality
- JWT authentication support (use "Authorize" button with Bearer token)

### Logging Configuration

Serilog is configured to log to console (stdout) for container compatibility:
- **Minimum Level**: Information
- **Override**: Microsoft/System namespaces set to Warning
- **Format**: Structured logging with context
- **Output**: Console sink only (collected by container runtime)

Example logging:
```csharp
_logger.LogInformation("User {Username} logged in successfully", username);
```

### Error Handling

- Global exception handler returns RFC 7807 Problem Details
- All exceptions automatically converted to proper HTTP responses
- Custom validation errors also return Problem Details format
- Unauthorized access returns 401 with Problem Details

### CORS Policy

Configured to allow all origins (for demo purposes):
```csharp
policy.AllowAnyOrigin()
      .AllowAnyMethod()
      .AllowAnyHeader();
```

**Note**: In production, restrict to specific origins.

## When Modifying This Project

1. **Keep all text in English** - code, comments, logs, documentation
2. **Add `[ProducesResponseType]` attributes** to all controller actions for Swagger
3. **Use structured logging**: `_logger.LogInformation("Message {Parameter}", value)`
4. **Add XML documentation comments** (`///`) to public API methods
5. **Follow existing patterns**:
   - SystemController for system endpoints
   - AuthController for authentication
   - PublicCustomersController for public CRUD operations
   - CustomersController for authenticated endpoints
6. **Use `AsNoTracking()`** for read-only EF queries
7. **Use `[Authorize]`** attribute for protected endpoints
8. **Test endpoints** in Swagger UI before committing
9. **Update this file** when adding new features or patterns

## Security Considerations

- ✅ JWT tokens with configurable expiration
- ✅ Refresh token rotation
- ✅ Non-root container user
- ✅ HTTPS response compression
- ⚠️ Demo uses in-memory token storage (use database in production)
- ⚠️ Demo users are hardcoded (use proper user management in production)
- ⚠️ JWT secret should be in environment variables (not appsettings.json)
- ⚠️ CORS is wide open (restrict in production)
