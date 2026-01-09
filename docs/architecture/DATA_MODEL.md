# Data Model

This document describes the database schema and entity relationships.

---

## Entity Relationship Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                              Notes                                   │
├─────────────────────────────────────────────────────────────────────┤
│ PK   Id            uniqueidentifier    NOT NULL                     │
│      Title         nvarchar(200)       NOT NULL                     │
│      Content       nvarchar(max)       NULL                         │
│      UserId        nvarchar(256)       NOT NULL                     │
│      IsDeleted     bit                 NOT NULL  DEFAULT 0          │
│      CreatedAt     datetime2           NOT NULL  DEFAULT GETUTCDATE │
│      UpdatedAt     datetime2           NOT NULL  DEFAULT GETUTCDATE │
├─────────────────────────────────────────────────────────────────────┤
│ Indexes:                                                            │
│   IX_Notes_UserId_IsDeleted (UserId, IsDeleted)                     │
│   IX_Notes_UpdatedAt (UpdatedAt DESC)                               │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Base Entity Pattern

All entities inherit from a base class that provides common fields:

```csharp
public interface IHasTimestamps
{
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}

public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
}

public abstract class BaseEntity : IHasTimestamps
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

### Automatic Timestamps

The `AppDbContext` automatically updates `UpdatedAt` on save:

```csharp
public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    foreach (var entry in ChangeTracker.Entries<IHasTimestamps>())
    {
        if (entry.State == EntityState.Modified)
        {
            entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
    }
    return base.SaveChangesAsync(cancellationToken);
}
```

---

## Entity: Note

### Schema

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `Id` | `uniqueidentifier` | PK, NOT NULL | Primary key (GUID) |
| `Title` | `nvarchar(200)` | NOT NULL | Note title |
| `Content` | `nvarchar(max)` | NULL | Note body text |
| `UserId` | `nvarchar(256)` | NOT NULL | Owner's user ID |
| `IsDeleted` | `bit` | NOT NULL, DEFAULT 0 | Soft delete flag |
| `CreatedAt` | `datetime2` | NOT NULL | Creation timestamp |
| `UpdatedAt` | `datetime2` | NOT NULL | Last modification |

### Entity Class

```csharp
// src/Cadence.Core/Features/Notes/Models/Entities/Note.cs
public class Note : BaseEntity, ISoftDeletable
{
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string UserId { get; set; } = string.Empty;
    public bool IsDeleted { get; set; } = false;
}
```

### Configuration

```csharp
// In AppDbContext.OnModelCreating
modelBuilder.Entity<Note>(entity =>
{
    entity.HasKey(e => e.Id);

    entity.Property(e => e.Title)
        .IsRequired()
        .HasMaxLength(200);

    entity.Property(e => e.UserId)
        .IsRequired()
        .HasMaxLength(256);

    entity.Property(e => e.CreatedAt)
        .HasColumnType("datetime2")
        .HasDefaultValueSql("GETUTCDATE()");

    entity.Property(e => e.UpdatedAt)
        .HasColumnType("datetime2")
        .HasDefaultValueSql("GETUTCDATE()");

    // Composite index for user queries with soft-delete filter
    entity.HasIndex(e => new { e.UserId, e.IsDeleted })
        .HasDatabaseName("IX_Notes_UserId_IsDeleted");

    // Index for sorting by date
    entity.HasIndex(e => e.UpdatedAt)
        .HasDatabaseName("IX_Notes_UpdatedAt");
});
```

---

## DTOs (Data Transfer Objects)

### NoteDto

Returned by API endpoints:

```csharp
public class NoteDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### CreateNoteRequest

For creating new notes:

```csharp
public class CreateNoteRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
}
```

### UpdateNoteRequest

For updating existing notes:

```csharp
public class UpdateNoteRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
}
```

---

## Mapping

### Entity ↔ DTO Conversion

```csharp
// src/Cadence.Core/Features/Notes/Mappers/NoteMapper.cs
public static class NoteMapper
{
    public static NoteDto ToDto(Note entity)
    {
        return new NoteDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Content = entity.Content,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public static Note ToEntity(CreateNoteRequest request, string userId)
    {
        return new Note
        {
            Title = request.Title,
            Content = request.Content,
            UserId = userId
        };
    }
}
```

---

## Query Patterns

### Get All Notes for User

```csharp
var notes = await _context.Notes
    .AsNoTracking()
    .Where(n => n.UserId == userId && !n.IsDeleted)
    .OrderByDescending(n => n.UpdatedAt)
    .Select(n => NoteMapper.ToDto(n))
    .ToListAsync();
```

### Get Single Note

```csharp
var note = await _context.Notes
    .AsNoTracking()
    .Where(n => n.Id == id && n.UserId == userId && !n.IsDeleted)
    .Select(n => NoteMapper.ToDto(n))
    .FirstOrDefaultAsync();
```

### Soft Delete

```csharp
var note = await _context.Notes
    .Where(n => n.Id == id && n.UserId == userId && !n.IsDeleted)
    .FirstOrDefaultAsync();

if (note != null)
{
    note.IsDeleted = true;
    await _context.SaveChangesAsync();
}
```

### Restore Soft-Deleted

```csharp
var note = await _context.Notes
    .Where(n => n.Id == id && n.UserId == userId && n.IsDeleted)
    .FirstOrDefaultAsync();

if (note != null)
{
    note.IsDeleted = false;
    await _context.SaveChangesAsync();
}
```

---

## Migrations

### Creating a Migration

```bash
cd src/api
dotnet ef migrations add MigrationName
```

### Applying Migrations

```bash
# Apply to database
dotnet ef database update

# Generate SQL script
dotnet ef migrations script
```

### Auto-Migration (Development Only)

The app can auto-apply migrations on startup:

```json
// local.settings.json
{
  "Values": {
    "Database:AutoMigrate": "true"
  }
}
```

---

## Adding a New Entity

### Step 1: Create Entity Class

```csharp
// src/Cadence.Core/Features/YourFeature/Models/Entities/YourEntity.cs
public class YourEntity : BaseEntity, ISoftDeletable
{
    public string Name { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public bool IsDeleted { get; set; } = false;
}
```

### Step 2: Add to DbContext

```csharp
// src/Cadence.Core/Data/AppDbContext.cs
public DbSet<YourEntity> YourEntities { get; set; }

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // ... existing config

    modelBuilder.Entity<YourEntity>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
        entity.Property(e => e.UserId).IsRequired().HasMaxLength(256);

        entity.HasIndex(e => new { e.UserId, e.IsDeleted });
    });
}
```

### Step 3: Create Migration

```bash
dotnet ef migrations add AddYourEntity
dotnet ef database update
```

### Step 4: Create DTOs and Mapper

```csharp
// Models/DTOs/YourEntityDto.cs
public class YourEntityDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// Mappers/YourEntityMapper.cs
public static class YourEntityMapper
{
    public static YourEntityDto ToDto(YourEntity entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}
```

---

## Best Practices

1. **Use `datetime2`** - More precise than `datetime`, no timezone issues
2. **Always soft delete** - Implement `ISoftDeletable` for data recovery
3. **Index foreign keys** - Especially `UserId` for multi-tenant queries
4. **Use `AsNoTracking()`** - For read-only queries (better performance)
5. **Map to DTOs** - Never expose entities directly to API consumers
6. **Validate at boundaries** - Use FluentValidation on DTOs
7. **UTC timestamps** - Store all dates in UTC, convert on display
