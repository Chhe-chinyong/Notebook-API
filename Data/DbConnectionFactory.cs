using System.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NotebookApi.Data;

/// <summary>
/// Helper class for Google Cloud SQL Server connections.
/// Provides the same interface as Google.Cloud.SqlServer.CloudSqlServerConnection
/// if that package is available.
/// </summary>
internal static class CloudSqlServerConnection
{
    /// <summary>
    /// Gets the connection string for a Google Cloud SQL instance.
    /// Accepts format: "project-id:region:instance-name" or "/cloudsql/project-id:region:instance-name"
    /// </summary>
    public static string GetConnectionString(string instanceConnectionName)
    {
        if (string.IsNullOrWhiteSpace(instanceConnectionName))
        {
            throw new ArgumentException("Instance connection name cannot be null or empty.", nameof(instanceConnectionName));
        }

        // If already in Unix socket format, return as-is
        if (instanceConnectionName.StartsWith("/cloudsql/"))
        {
            return instanceConnectionName;
        }

        // Convert "project-id:region:instance-name" to "/cloudsql/project-id:region:instance-name"
        return $"/cloudsql/{instanceConnectionName}";
    }
}

public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;
    private readonly ILogger<DbConnectionFactory>? _logger;

    public DbConnectionFactory(IConfiguration configuration, IWebHostEnvironment environment, ILogger<DbConnectionFactory>? logger = null)
    {
        _logger = logger;
        string? connectionString;
        
        // If not development, use Google Cloud SQL Server connection
        if (!environment.IsDevelopment())
        {
            _logger?.LogInformation("Initializing Cloud SQL connection for production environment");
            
            // Get Google Cloud SQL configuration from configuration (appsettings.json) first, then environment variables
            var cloudSqlInstance = configuration["GoogleCloudSql:Instance"]
                ?? Environment.GetEnvironmentVariable("GOOGLE_CLOUD_SQL_INSTANCE");
            
            var database = configuration["GoogleCloudSql:Database"]
                ?? Environment.GetEnvironmentVariable("GOOGLE_CLOUD_SQL_DATABASE");
            
            var userId = configuration["GoogleCloudSql:UserID"]
                ?? Environment.GetEnvironmentVariable("GOOGLE_CLOUD_SQL_USER_ID");
            
            var password = configuration["GoogleCloudSql:Password"]
                ?? Environment.GetEnvironmentVariable("GOOGLE_CLOUD_SQL_PASSWORD");

            // Try to parse from DefaultConnection in appsettings.json if not explicitly set
            var defaultConnectionString = configuration.GetConnectionString("DefaultConnection")
                ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                ?? Environment.GetEnvironmentVariable("CONNECTIONSTRINGS__DEFAULTCONNECTION");
                
            if (!string.IsNullOrWhiteSpace(defaultConnectionString))
            {
                try
                {
                    var existingBuilder = new SqlConnectionStringBuilder(defaultConnectionString);
                    
                    // Extract instance from DataSource or Server if it contains /cloudsql/
                    // Note: SqlConnectionStringBuilder maps "Server" to "DataSource" property
                    var dataSource = existingBuilder.DataSource;
                    
                    if (string.IsNullOrWhiteSpace(cloudSqlInstance) && 
                        !string.IsNullOrWhiteSpace(dataSource) &&
                        dataSource.StartsWith("/cloudsql/"))
                    {
                        cloudSqlInstance = dataSource;
                        _logger?.LogInformation("Extracted Cloud SQL instance from connection string: {Instance}", 
                            cloudSqlInstance.Replace("/cloudsql/", ""));
                    }
                    
                    // Extract other values if not explicitly set
                    database ??= existingBuilder.InitialCatalog;
                    userId ??= existingBuilder.UserID;
                    password ??= existingBuilder.Password;
                }
                catch (Exception ex)
                {
                    // If parsing fails, log the error but continue with what we have
                    _logger?.LogWarning(ex, "Failed to parse DefaultConnection string, continuing with explicit configuration");
                }
            }

            // If Google Cloud SQL instance is configured, build connection string using Google Cloud SQL Server library
            if (!string.IsNullOrWhiteSpace(cloudSqlInstance))
            {
                if (string.IsNullOrWhiteSpace(database))
                {
                    throw new InvalidOperationException(
                        "Google Cloud SQL database name is required. Set it via GoogleCloudSql:Database config or GOOGLE_CLOUD_SQL_DATABASE environment variable.");
                }
                if (string.IsNullOrWhiteSpace(userId))
                {
                    throw new InvalidOperationException(
                        "Google Cloud SQL user ID is required. Set it via GoogleCloudSql:UserID config or GOOGLE_CLOUD_SQL_USER_ID environment variable.");
                }
                if (string.IsNullOrWhiteSpace(password))
                {
                    throw new InvalidOperationException(
                        "Google Cloud SQL password is required. Set it via GoogleCloudSql:Password config or GOOGLE_CLOUD_SQL_PASSWORD environment variable.");
                }

                // Ensure the instance connection name is in the correct format
                var instancePath = CloudSqlServerConnection.GetConnectionString(cloudSqlInstance);
                
                var builder = new SqlConnectionStringBuilder
                {
                    DataSource = instancePath,
                    InitialCatalog = database,
                    UserID = userId,
                    Password = password,
                    Encrypt = true,
                    TrustServerCertificate = false
                };

                connectionString = builder.ConnectionString;
                
                // Log connection info without password for debugging
                _logger?.LogInformation(
                    "Using Cloud SQL connection: Instance={Instance}, Database={Database}, User={User}", 
                    instancePath, database, userId);
            }
            else
            {
                // Fallback to traditional connection string from configuration first, then environment variables
                connectionString = defaultConnectionString;
                
                if (!string.IsNullOrWhiteSpace(connectionString))
                {
                    // Log connection method
                    try
                    {
                        var builder = new SqlConnectionStringBuilder(connectionString);
                        var dataSource = builder.DataSource ?? "unknown";
                        _logger?.LogInformation(
                            "Using traditional connection string: DataSource={DataSource}, Database={Database}", 
                            dataSource, builder.InitialCatalog ?? "unknown");
                    }
                    catch
                    {
                        _logger?.LogWarning("Using connection string but could not parse for logging");
                    }
                }
            }
        }
        else
        {
            // In development, use configuration (appsettings.Development.json)
            connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger?.LogInformation("Using development connection string from appsettings.Development.json");
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            var errorMessage = "Connection string 'DefaultConnection' not found. " +
                "For production, ensure you have set either:\n" +
                "1. Environment variables: GOOGLE_CLOUD_SQL_INSTANCE, GOOGLE_CLOUD_SQL_DATABASE, GOOGLE_CLOUD_SQL_USER_ID, GOOGLE_CLOUD_SQL_PASSWORD\n" +
                "2. Or ConnectionStrings__DefaultConnection environment variable\n" +
                "3. Or GoogleCloudSql configuration section in appsettings.json";
            _logger?.LogError(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        _connectionString = connectionString;
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
