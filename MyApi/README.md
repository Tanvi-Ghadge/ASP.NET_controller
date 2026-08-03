# MyApi

MyApi is a modern ASP.NET Core Web API for employee management with authentication, authorization, refresh tokens, HMAC-based API security, Dapper and EF Core support, logging, rate limiting, and background jobs.

## 🚀 Features

- Employee CRUD operations
- Role-based authorization (Admin vs regular users)
- JWT authentication with refresh tokens
- HMAC-based API key and signature validation middleware
- Two data access approaches:
  - Entity Framework Core
  - Dapper
- Swagger / OpenAPI documentation
- Serilog-based structured logging to console and file
- Redis-backed nonce support
- Rate limiting for login endpoint
- Trace ID middleware for request tracing
- Response compression and middleware-based input sanitization
- Hangfire dashboard for background jobs

## 🛠️ Tech Stack

- .NET 8
- ASP.NET Core Web API
- Entity Framework Core
- Dapper
- SQL Server
- JWT Bearer Authentication
- AutoMapper
- BCrypt.Net-Next
- Serilog
- Redis
- Hangfire
- Swagger / Swashbuckle
- xUnit-style project structure patterns (service/repository/controller separation)

## 📁 Project Structure

- Controllers/: API endpoints
- DTO/: request and response models
- Service/: business logic
- Repository/: data access abstractions and implementations
- Middleware/: custom request processing middleware
- models/entities/: EF Core entities
- data/: DbContext and database configuration
- Migrations/: EF Core migrations

## ⚙️ Prerequisites

Make sure the following are installed:

- .NET SDK 8.0+
- SQL Server
- Redis (optional for some features, but configured)

## 🔧 Configuration

Update the connection strings and secrets in appsettings.json:

```json
{
  "ConnectionStrings": {
    "defaultconnection": "Server=YOUR_SERVER;Database=employeeDb;Trusted_Connection=True;TrustServerCertificate=True;",
    "connection": "Server=YOUR_SERVER;Database=teamsDB;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "YOUR_JWT_SECRET_KEY",
    "Issuer": "MyApi",
    "Audience": "MyApiUsers"
  },
  "Hmac": {
    "EncryptionKey": "YOUR_HMAC_ENCRYPTION_KEY"
  },
  "Redis": {
    "Connection": "localhost:6379"
  }
}
```

## ▶️ Getting Started

### 1. Restore dependencies

```bash
dotnet restore
```

### 2. Apply database migrations

```bash
dotnet ef database update
```

### 3. Run the API

```bash
dotnet run
```

The API will be available at:

- https://localhost:5001 (or the configured HTTPS port)
- http://localhost:5000

Swagger UI is available in development mode at:

- http://localhost:5000/swagger

## 🔐 Authentication Endpoints

### Register

```http
POST /api/auth/register
```

Body example:

```json
{
  "name": "John Doe",
  "email": "john@example.com",
  "password": "Password123!",
  "role": "Admin",
  "departmentId": 1
}
```

### Login

```http
POST /api/auth/login
```

### Refresh Token

```http
POST /api/auth/refresh?Token=YOUR_REFRESH_TOKEN
```

## 👥 Employee Endpoints

### Get all employees

```http
GET /api/employees
```

### Get employee by ID

```http
GET /api/employees/{id}
```

### Create employee

```http
POST /api/employees
```

### Update employee

```http
PUT /api/employees/{id}
```

### Delete employee

```http
DELETE /api/employees/{id}
```

## 🧩 Dapper Endpoints

```http
GET /api/dapper-employees/all
GET /api/dapper-employees/{id}
GET /api/dapper-employees/with-projects
POST /api/dapper-employees
PUT /api/dapper-employees/{id}
DELETE /api/dapper-employees/{id}
```

## 🧪 Notes

- Some routes are protected by JWT authentication.
- Admin-only actions require the role claim of Admin.
- HMAC validation is enforced through middleware and uses API key + signature headers.
- Logging output is written to the logs folder.

## 📌 Future Improvements

- Add unit and integration tests
- Add Docker support
- Improve exception handling consistency
- Add request validation filters
- Add CI/CD pipeline

## 👨‍💻 Author

Built as a sample ASP.NET Core API demonstrating enterprise-style patterns such as layered architecture, authentication, middleware, and data access abstraction.
