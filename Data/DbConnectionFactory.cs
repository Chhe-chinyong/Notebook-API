using System.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace NotebookApi.Data;

public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;
    private readonly ILogger<DbConnectionFactory>? _logger;

    public DbConnectionFactory(IConfiguration configuration, IWebHostEnvironment environment, ILogger<DbConnectionFactory>? logger = null)
    {
        _logger = logger;
        
        // Get connection string from configuration or environment variable
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? Environment.GetEnvironmentVariable("CONNECTIONSTRINGS__DEFAULTCONNECTION");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            var errorMessage = "Connection string 'DefaultConnection' not found. " +
                "Set it via ConnectionStrings__DefaultConnection environment variable or in appsettings.json";
            _logger?.LogError(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        // Fix connection string for Cloud SQL: convert Server=/cloudsql/... to DataSource=/cloudsql/...
        // This is required because Unix socket connections must use DataSource, not Server
        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            var dataSource = builder.DataSource;
            
            // If DataSource contains /cloudsql/, rebuild connection string to ensure DataSource is used (not Server)
            if (!string.IsNullOrWhiteSpace(dataSource) && dataSource.StartsWith("/cloudsql/"))
            {
                var rebuiltBuilder = new SqlConnectionStringBuilder
                {
                    DataSource = dataSource,
                    InitialCatalog = builder.InitialCatalog,
                    UserID = builder.UserID,
                    Password = builder.Password,
                    Encrypt = builder.Encrypt,
                    TrustServerCertificate = builder.TrustServerCertificate
                };
                connectionString = rebuiltBuilder.ConnectionString;
                _logger?.LogInformation(
                    "Fixed Cloud SQL connection string: DataSource={DataSource}, Database={Database}", 
                    dataSource, builder.InitialCatalog ?? "unknown");
            }
            else
            {
                _logger?.LogInformation(
                    "Using connection string: DataSource={DataSource}, Database={Database}", 
                    dataSource ?? "unknown", builder.InitialCatalog ?? "unknown");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Could not parse connection string for validation, using as-is");
        }

        _connectionString = "/cloudsql/project-8f33c2c1-6350-4a64-90f:asia-southeast1:sql-server-techbodia;Database=NotebookApp;User ID=sqlserver;Password=Helloworld01.;Encrypt=True;TrustServerCertificate=True;";
    }

    public IDbConnection CreateConnection()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                throw new InvalidOperationException(
                    "Cannot create database connection: Connection string is null or empty.");
            }

            var connection = new SqlConnection(_connectionString);
            _logger?.LogDebug("Created new SQL connection");
            return connection;
        }
        catch (ArgumentNullException ex)
        {
            _logger?.LogError(ex, "Failed to create database connection: Connection string is null");
            throw new InvalidOperationException(
                "Cannot create database connection: Connection string is null.", ex);
        }
        catch (ArgumentException ex)
        {
            _logger?.LogError(ex, "Failed to create database connection: Invalid connection string format. " +
                "For Cloud SQL, ensure the connection string uses DataSource=/cloudsql/PROJECT:REGION:INSTANCE format");
            throw new InvalidOperationException(
                "Cannot create database connection: Invalid connection string format. " +
                "For Cloud SQL on Cloud Run, ensure:\n" +
                "1. The Cloud Run service is connected to the Cloud SQL instance\n" +
                "2. The connection string uses DataSource=/cloudsql/PROJECT:REGION:INSTANCE format\n" +
                "3. All required parameters (Database, User ID, Password) are provided", ex);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "An unexpected error occurred while creating the database connection");
            throw new InvalidOperationException(
                "An unexpected error occurred while creating the database connection.", ex);
        }
    }
}
