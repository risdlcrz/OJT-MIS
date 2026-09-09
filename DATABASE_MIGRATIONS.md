# Database Migration Commands

Run these commands in the `OJTMISApi/OJTMISApi` folder to set up the database:

## Initial Setup

```bash
# Add Entity Framework CLI tools (if not already installed)
dotnet tool install --global dotnet-ef

# Create initial migration
dotnet ef migrations add InitialCreate

# Apply migrations to create/update database
dotnet ef database update
```

## Common Commands

```bash
# View pending migrations
dotnet ef migrations list

# Create a new migration after model changes
dotnet ef migrations add DescriptiveNameOfChange

# Revert last migration
dotnet ef migrations remove

# Drop and recreate database
dotnet ef database drop
dotnet ef database update

# View SQL that will be executed
dotnet ef migrations script

# Generate SQL script from migration
dotnet ef migrations script -o migration.sql
```

## Notes

- Migrations are stored in the `Data/Migrations/` folder
- Always create a migration after modifying models
- Model changes require running `dotnet ef database update` for local development
- For production, generate SQL scripts and run through proper change management
