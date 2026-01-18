#!/bin/bash

# Script to initialize the NotebookApp database in Docker SQL Server

echo "Waiting for SQL Server to be ready..."
sleep 5

# Wait for SQL Server to be ready
until docker exec notebook-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourStrong@Passw0rd" -C -Q "SELECT 1" &> /dev/null
do
  echo "Waiting for SQL Server..."
  sleep 2
done

echo "SQL Server is ready!"

# Create database if it doesn't exist
echo "Creating database if it doesn't exist..."
docker exec notebook-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourStrong@Passw0rd" -C -Q "IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'NotebookApp') CREATE DATABASE NotebookApp;" || exit 1

# Copy the SQL script to the container
echo "Copying database script to container..."
docker cp Database/Scripts/CreateTables.sql notebook-sqlserver:/tmp/CreateTables.sql || exit 1

# Run the SQL script
echo "Running database initialization script..."
docker exec notebook-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourStrong@Passw0rd" -C -d NotebookApp -i /tmp/CreateTables.sql || exit 1

echo "Database initialized successfully!"
