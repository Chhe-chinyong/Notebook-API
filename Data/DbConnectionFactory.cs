using System.Data;
using Microsoft.Data.SqlClient;

namespace NotebookApi.Data;

public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
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
