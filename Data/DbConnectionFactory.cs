using System.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;

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

    public DbConnectionFactory(IConfiguration configuration, IWebHostEnvironment environment)
    {
        string? connectionString;
        
        // If not development, use Google Cloud SQL Server connection
        if (!environment.IsDevelopment())
        {
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
            var defaultConnectionString = configuration.GetConnectionString("DefaultConnection");
            if (!string.IsNullOrWhiteSpace(defaultConnectionString))
            {
                try
                {
                    var existingBuilder = new SqlConnectionStringBuilder(defaultConnectionString);
                    
                    // Extract instance from Server if it contains /cloudsql/
                    if (string.IsNullOrWhiteSpace(cloudSqlInstance) && 
                        !string.IsNullOrWhiteSpace(existingBuilder.DataSource) &&
                        existingBuilder.DataSource.StartsWith("/cloudsql/"))
                    {
                        cloudSqlInstance = existingBuilder.DataSource;
                    }
                    
                    // Extract other values if not explicitly set
                    database ??= existingBuilder.InitialCatalog;
                    userId ??= existingBuilder.UserID;
                    password ??= existingBuilder.Password;
                }
                catch
                {
                    // If parsing fails, continue with what we have
                }
            }

            // If Google Cloud SQL instance is configured, build connection string using Google Cloud SQL Server library
            if (!string.IsNullOrWhiteSpace(cloudSqlInstance))
            {
                if (string.IsNullOrWhiteSpace(database))
                {
                    throw new InvalidOperationException("Google Cloud SQL database name is required.");
                }
                if (string.IsNullOrWhiteSpace(userId))
                {
                    throw new InvalidOperationException("Google Cloud SQL user ID is required.");
                }
                if (string.IsNullOrWhiteSpace(password))
                {
                    throw new InvalidOperationException("Google Cloud SQL password is required.");
                }

                var builder = new SqlConnectionStringBuilder
                {
                    DataSource = CloudSqlServerConnection.GetConnectionString(cloudSqlInstance),
                    InitialCatalog = database,
                    UserID = userId,
                    Password = password,
                    Encrypt = true,
                    TrustServerCertificate = false
                };

                connectionString = builder.ConnectionString;
            }
            else
            {
                // Fallback to traditional connection string from configuration first, then environment variables
                connectionString = configuration.GetConnectionString("DefaultConnection")
                    ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                    ?? Environment.GetEnvironmentVariable("CONNECTIONSTRINGS__DEFAULTCONNECTION");
            }
        }
        else
        {
            // In development, use configuration (appsettings.Development.json)
            connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
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

            return new SqlConnection(_connectionString);
        }
        catch (ArgumentNullException ex)
        {
            throw new InvalidOperationException(
                "Cannot create database connection: Connection string is null.", ex);
        }
        catch (ArgumentException ex)
        {
            throw new InvalidOperationException(
                "Cannot create database connection: Invalid connection string format.", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "An unexpected error occurred while creating the database connection.", ex);
        }
    }
}
