using System.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;

namespace NotebookApi.Data;

public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(IConfiguration configuration, IWebHostEnvironment environment)
    {
        string? connectionString;
        
        // If not development, try to get from environment variables first
        if (!environment.IsDevelopment())
        {
            // Try environment variable with double underscore format (ConnectionStrings:DefaultConnection)
            connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                ?? Environment.GetEnvironmentVariable("CONNECTIONSTRINGS__DEFAULTCONNECTION")
                ?? configuration.GetConnectionString("DefaultConnection");
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
