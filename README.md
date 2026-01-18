# Notebook API Backend

.NET Core 9.0 Web API with Dapper ORM and SQL Server for the Notebook application.

## Features

- JWT Authentication & Authorization
- User Registration and Login
- CRUD Operations for Notes
- User Ownership Validation (users can only access their own notes)
- Dapper ORM for database access
- SQL Server database

## Prerequisites

- .NET 9.0 SDK
- Docker and Docker Compose (for local SQL Server)
- Visual Studio 2022, VS Code, or Rider

## Setup

### 1. Docker Setup for Local SQL Server (Recommended)

The easiest way to run SQL Server locally is using Docker:

1. **Start SQL Server container:**
   ```bash
   docker-compose up -d
   ```

2. **Wait for SQL Server to be ready** (usually takes 10-30 seconds). You can check the status with:
   ```bash
   docker-compose ps
   ```

3. **Initialize the database:**
   
   **Option A: Use the initialization script (Recommended):**
   ```bash
   ./init-db.sh
   ```
   
   **Option B: Manual initialization:**
   ```bash
   # Copy the script into the container
   docker cp Database/Scripts/CreateTables.sql notebook-sqlserver:/tmp/CreateTables.sql
   
   # Create database
   docker exec notebook-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "YourStrong@Passw0rd" -Q "IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'NotebookApp') CREATE DATABASE NotebookApp;"
   
   # Run the initialization script
   docker exec notebook-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "YourStrong@Passw0rd" -d NotebookApp -i /tmp/CreateTables.sql
   ```

4. **Stop SQL Server when done:**
   ```bash
   docker-compose down
   ```

   **Note:** To remove all data (volumes), use:
   ```bash
   docker-compose down -v
   ```

**Docker SQL Server Connection Details:**
- Server: `localhost,1433`
- Username: `sa`
- Password: `YourStrong@Passw0rd`
- Database: `NotebookApp`

The connection string is already configured in `appsettings.Development.json` for Docker setup.

### 2. Alternative: Manual SQL Server Setup

If you prefer to use a local SQL Server installation:

1. Open SQL Server Management Studio (SSMS) or use `sqlcmd`
2. Run the database script:
   ```sql
   -- Execute Database/Scripts/CreateTables.sql
   ```
   Or connect to your SQL Server and run:
   ```bash
   sqlcmd -S localhost -i Database/Scripts/CreateTables.sql
   ```

### 3. Configuration

**Connection String (Required):**

The application supports `.env` files for configuration. Create a `.env` file in the project root with the following format:

```env
CONNECTIONSTRINGS__DEFAULTCONNECTION=Server=localhost;Database=NotebookApp;Integrated Security=true;TrustServerCertificate=true;
```

**For Production:**
```env
CONNECTIONSTRINGS__DEFAULTCONNECTION=Server=your-server-name;Database=NotebookApp;User ID=your-username;Password=YOUR_PASSWORD;Encrypt=True;TrustServerCertificate=True;
```

**Note:** 
- The `.env` file is automatically loaded when the application starts
- The `.env` file is already in `.gitignore` and will not be committed to version control
- Use double underscores (`__`) to represent nested configuration keys (e.g., `ConnectionStrings:DefaultConnection` becomes `CONNECTIONSTRINGS__DEFAULTCONNECTION`)

**Alternative: Environment Variables**

You can also set the connection string as an environment variable directly:

**On macOS/Linux:**
```bash
export ConnectionStrings__DefaultConnection="Server=localhost;Database=NotebookApp;Integrated Security=true;TrustServerCertificate=true;"
```

**On Windows (PowerShell):**
```powershell
$env:ConnectionStrings__DefaultConnection="Server=localhost;Database=NotebookApp;Integrated Security=true;TrustServerCertificate=true;"
```

**On Windows (CMD):**
```cmd
set ConnectionStrings__DefaultConnection=Server=localhost;Database=NotebookApp;Integrated Security=true;TrustServerCertificate=true;
```

**JWT Configuration:**

The JWT settings are in `appsettings.json`. **Important:** Change the JWT Secret to a secure random string in production!

**Note:** Connection strings are stored in environment variables or `.env` files for security. The `appsettings.json` files have empty connection strings as placeholders.

#### Configuration Files

**`appsettings.json`** - Base configuration file used in all environments:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "Jwt": {
    "Secret": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "NotebookApi",
    "Audience": "NotebookApp",
    "ExpirationMinutes": "1440"
  }
}
```

**`appsettings.Development.json`** - Development-specific overrides (loaded when `ASPNETCORE_ENVIRONMENT=Development`):
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": ""
  }
}
```

**Configuration Priority (highest to lowest):**
1. Environment variables
2. `.env` file
3. `appsettings.{Environment}.json` (e.g., `appsettings.Development.json`)
4. `appsettings.json`

**Important Notes:**
- Connection strings in `appsettings.json` and `appsettings.Development.json` should be empty or use placeholder values
- Use environment variables or `.env` files for actual connection strings in all environments
- JWT Secret should be changed to a secure random string in production (minimum 32 characters)
- Development settings override base settings when running in Development mode

### 4. Install Dependencies

```bash
cd backend
dotnet restore
```

### 5. Run the API

```bash
dotnet run
```

The API will be available at:
- HTTP: `http://localhost:5266`
- HTTPS: `https://localhost:7032`
- Swagger UI: `http://localhost:5266/swagger` (in Development mode)

## API Endpoints

### Authentication

- `POST /api/auth/register` - Register a new user
  - Request: `{ "name": "John Doe", "email": "john@example.com", "password": "password123" }`
  - Response: `{ "user": { "id": "...", "email": "...", "name": "..." }, "token": "..." }`

- `POST /api/auth/login` - Login user
  - Request: `{ "email": "john@example.com", "password": "password123" }`
  - Response: `{ "user": { "id": "...", "email": "...", "name": "..." }, "token": "..." }`

### Notes (Requires Authentication)

All note endpoints require the `Authorization: Bearer {token}` header.

- `GET /api/notes` - Get all notes for authenticated user
- `GET /api/notes/{id}` - Get note by ID
- `POST /api/notes` - Create a new note
  - Request: `{ "title": "My Note", "content": "Note content" }`
- `PUT /api/notes/{id}` - Update a note
  - Request: `{ "title": "Updated Title", "content": "Updated content" }`
- `DELETE /api/notes/{id}` - Delete a note

## Testing with Swagger

1. Start the API: `dotnet run`
2. Open Swagger UI: `https://localhost:7032/swagger`
3. Register a new user via `/api/auth/register`
4. Copy the token from the response
5. Click "Authorize" button in Swagger
6. Enter: `Bearer {your-token}`
7. Test the notes endpoints

## Project Structure

```
backend/
├── Controllers/          # API Controllers
├── Data/                # Dapper repositories
├── Models/              # Domain models and DTOs
├── Services/            # Business logic services
├── Database/            # SQL scripts
└── Program.cs           # Application entry point
```

## Security Notes

- Passwords are hashed using BCrypt
- JWT tokens expire after 24 hours (configurable)
- All note operations validate user ownership
- CORS is configured for frontend origins only
- SQL injection prevention via Dapper parameterized queries

## Integration with Frontend

The API is designed to match the frontend's expected interface:
- Response formats match TypeScript interfaces
- String IDs (Guid.ToString()) for compatibility
- ISO 8601 date format
- Standard HTTP status codes

## License

MIT
