# Database

## Overview

This application uses Entity Framework Core with Azure SQL Database.

## Database Setup

### Local Development

**Option 1: SQL Server LocalDB (Windows)**

```bash
# Create LocalDB instance
sqllocaldb create MSSQLLocalDB
sqllocaldb start MSSQLLocalDB
```

Connection string:
```
Server=(localdb)\MSSQLLocalDB;Database=Cadence;Trusted_Connection=True;
```

**Option 2: SQL Server Docker**

```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong!Passw0rd" \
  -p 1433:1433 --name sql-server \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

Connection string:
```
Server=localhost,1433;Database=Cadence;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;
```

**Option 3: Azure SQL (Free Tier)**

See the [Azure SQL Free Tier Setup Guide](../docs/DEPLOYMENT.md#azure-sql-free-tier) for instructions on setting up a free/low-cost Azure SQL database.

### Migrations

```bash
cd src/api

# Create a new migration
dotnet ef migrations add MigrationName

# Apply migrations
dotnet ef database update

# Generate SQL script (for review)
dotnet ef migrations script

# Rollback to specific migration
dotnet ef database update PreviousMigrationName
```

## Seed Data

Optional seed data for development:

```sql
-- See seed-data.sql for sample data
```

## Schema

The database schema is managed through EF Core migrations. Key tables:

| Table | Description |
|-------|-------------|
| `Notes` | Sample feature - user notes |
| `__EFMigrationsHistory` | EF Core migration tracking |

## Best Practices

1. **Never** commit connection strings with passwords
2. **Always** review migrations before applying to production
3. Use **Azure AD authentication** in production (not SQL auth)
4. Enable **auditing** for compliance requirements
5. Configure **automatic backups** in Azure
