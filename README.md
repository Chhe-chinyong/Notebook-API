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
- SQL Server (LocalDB, Express, or full instance)
- Visual Studio 2022, VS Code, or Rider

## Setup

### 1. Database Setup

1. Open SQL Server Management Studio (SSMS) or use `sqlcmd`
2. Run the database script:
   ```sql
   -- Execute Database/Scripts/CreateTables.sql
   ```
   Or connect to your SQL Server and run:
   ```bash
   sqlcmd -S localhost -i Database/Scripts/CreateTables.sql
   ```

### 2. Configuration

**Connection String (Required):**

The application supports `.env` files for configuration. Create a `.env` file in the project root with the following format:

```env
CONNECTIONSTRINGS__DEFAULTCONNECTION=Server=localhost;Database=NotebookApp;Integrated Security=true;TrustServerCertificate=true;
```

**For Production:**
```env
CONNECTIONSTRINGS__DEFAULTCONNECTION=Server=/cloudsql/project-8f33c2c1-6350-4a64-90f:asia-southeast1:sql-server-techbodia;Database=NotebookApp;User ID=sqlserver;Password=YOUR_PASSWORD;Encrypt=True;TrustServerCertificate=True;
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

### 2.1. Google Cloud Run to Cloud SQL Configuration

When deploying to Google Cloud Run and connecting to Cloud SQL, you need to complete the following steps:

#### Step 1: Connect Cloud Run Service to Cloud SQL Instance

**Via Google Cloud Console:**
1. Go to [Cloud Run](https://console.cloud.google.com/run)
2. Select your service
3. Click "Edit & Deploy New Revision"
4. Go to the "Connections" tab
5. Under "Cloud SQL connections", click "Add Connection"
6. Select your Cloud SQL instance
7. Click "Deploy"

**Via gcloud CLI:**
```bash
gcloud run services update YOUR_SERVICE_NAME \
  --add-cloudsql-instances=PROJECT_ID:REGION:INSTANCE_NAME \
  --region=REGION
```

Example:
```bash
gcloud run services update notebook-api \
  --add-cloudsql-instances=project-8f33c2c1-6350-4a64-90f:asia-southeast1:sql-server-techbodia \
  --region=asia-southeast1
```

**Important:** This step is **required** for Cloud Run to access Cloud SQL. Without this connection, the application will fail with connection errors.

#### Step 2: Set Environment Variables in Cloud Run

Set the following environment variables in your Cloud Run service:

**Option A: Using Individual Environment Variables (Recommended)**
```
GOOGLE_CLOUD_SQL_INSTANCE=/cloudsql/project-8f33c2c1-6350-4a64-90f:asia-southeast1:sql-server-techbodia
GOOGLE_CLOUD_SQL_DATABASE=NotebookApp
GOOGLE_CLOUD_SQL_USER_ID=sqlserver
GOOGLE_CLOUD_SQL_PASSWORD=your_password_here
```

**Option B: Using Connection String Environment Variable**
```
ConnectionStrings__DefaultConnection=Server=/cloudsql/project-8f33c2c1-6350-4a64-90f:asia-southeast1:sql-server-techbodia;Database=NotebookApp;User ID=sqlserver;Password=your_password_here;Encrypt=True;TrustServerCertificate=True;
```

**Via Google Cloud Console:**
1. Go to Cloud Run → Your Service → Edit & Deploy New Revision
2. Go to "Variables & Secrets" tab
3. Add each environment variable
4. Click "Deploy"

**Via gcloud CLI:**
```bash
gcloud run services update YOUR_SERVICE_NAME \
  --set-env-vars="GOOGLE_CLOUD_SQL_INSTANCE=/cloudsql/PROJECT:REGION:INSTANCE,GOOGLE_CLOUD_SQL_DATABASE=NotebookApp,GOOGLE_CLOUD_SQL_USER_ID=sqlserver,GOOGLE_CLOUD_SQL_PASSWORD=your_password" \
  --region=REGION
```

#### Connection String Format for Cloud SQL

The connection string must use the Unix socket path format:
```
Server=/cloudsql/PROJECT_ID:REGION:INSTANCE_NAME
```

Or when using `DataSource`:
```
DataSource=/cloudsql/PROJECT_ID:REGION:INSTANCE_NAME
```

**Important Notes:**
- The `/cloudsql/` prefix is required for Unix socket connections
- The format is: `/cloudsql/PROJECT_ID:REGION:INSTANCE_NAME`
- Do NOT use IP addresses or hostnames for Cloud SQL connections from Cloud Run
- Ensure `Encrypt=True` and `TrustServerCertificate=True` (or `False` with proper certificates)

#### Troubleshooting Cloud SQL Connection Issues

If you encounter "Connection string is not valid" or "server was not found" errors:

1. **Verify Cloud Run is connected to Cloud SQL:**
   ```bash
   gcloud run services describe YOUR_SERVICE_NAME --region=REGION --format="value(spec.template.spec.containers[0].env)"
   ```
   Look for `CLOUD_SQL_CONNECTION_NAME` or check the Connections tab in Console.

2. **Check environment variables are set:**
   ```bash
   gcloud run services describe YOUR_SERVICE_NAME --region=REGION --format="value(spec.template.spec.containers[0].env)"
   ```

3. **Verify connection string format:**
   - Must start with `/cloudsql/`
   - Format: `/cloudsql/PROJECT_ID:REGION:INSTANCE_NAME`
   - All required parameters (Database, User ID, Password) must be present

4. **Check Cloud SQL instance status:**
   ```bash
   gcloud sql instances describe INSTANCE_NAME
   ```

5. **Review application logs:**
   ```bash
   gcloud run services logs read YOUR_SERVICE_NAME --region=REGION
   ```
   The application logs connection information (without passwords) to help diagnose issues.

### 3. Install Dependencies

```bash
cd backend
dotnet restore
```

### 4. Run the API

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
