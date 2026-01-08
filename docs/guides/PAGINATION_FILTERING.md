# Pagination & Filtering Guide

> **Status:** Reference Guide - Not Yet Implemented in Template
> **Priority:** Medium for Production

This guide explains how to implement pagination, filtering, and sorting for list endpoints.

---

## Table of Contents

1. [Overview](#overview)
2. [Backend Implementation](#backend-implementation)
3. [Frontend Implementation](#frontend-implementation)
4. [Query Parameter Patterns](#query-parameter-patterns)
5. [Performance Considerations](#performance-considerations)
6. [Testing](#testing)

---

## Overview

### Why Pagination?

- **Performance** - Don't load thousands of records at once
- **Memory** - Reduce server and client memory usage
- **User Experience** - Faster initial load times
- **Bandwidth** - Less data transferred

### Current State

The Notes endpoint returns all records:

```csharp
// Returns ALL notes for user
var notes = await _context.Notes
    .Where(n => n.UserId == userId && !n.IsDeleted)
    .ToListAsync();
```

### Target State

Paginated, filterable, sortable responses:

```
GET /api/notes?page=1&pageSize=10&search=meeting&sortBy=createdAt&sortDir=desc
```

Response:

```json
{
  "data": [...],
  "pagination": {
    "page": 1,
    "pageSize": 10,
    "totalItems": 47,
    "totalPages": 5,
    "hasNext": true,
    "hasPrevious": false
  }
}
```

---

## Backend Implementation

### Step 1: Create Query Parameters Model

Create `src/api/Core/Models/PaginationParameters.cs`:

```csharp
namespace DynamisReferenceApp.Api.Core.Models;

/// <summary>
/// Common query parameters for paginated list endpoints.
/// </summary>
public class PaginationParameters
{
    private int _page = 1;
    private int _pageSize = 20;

    /// <summary>
    /// Page number (1-based).
    /// </summary>
    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    /// <summary>
    /// Number of items per page (max 100).
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            < 1 => 20,
            > 100 => 100,
            _ => value
        };
    }

    /// <summary>
    /// Search term to filter results.
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Field to sort by.
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Sort direction: "asc" or "desc".
    /// </summary>
    public string SortDirection { get; set; } = "desc";

    /// <summary>
    /// Whether to include soft-deleted items.
    /// </summary>
    public bool IncludeDeleted { get; set; } = false;

    /// <summary>
    /// Calculate skip count for EF query.
    /// </summary>
    public int Skip => (Page - 1) * PageSize;

    /// <summary>
    /// Whether sort is descending.
    /// </summary>
    public bool IsDescending => SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);
}
```

### Step 2: Create Paginated Response Model

Create `src/api/Core/Models/PaginatedResponse.cs`:

```csharp
namespace DynamisReferenceApp.Api.Core.Models;

/// <summary>
/// Wrapper for paginated API responses.
/// </summary>
public class PaginatedResponse<T>
{
    public List<T> Data { get; set; } = new();
    public PaginationMeta Pagination { get; set; } = new();

    public static PaginatedResponse<T> Create(
        List<T> items,
        int page,
        int pageSize,
        int totalItems)
    {
        return new PaginatedResponse<T>
        {
            Data = items,
            Pagination = new PaginationMeta
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                HasNext = page * pageSize < totalItems,
                HasPrevious = page > 1
            }
        };
    }
}

public class PaginationMeta
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public bool HasNext { get; set; }
    public bool HasPrevious { get; set; }
}
```

### Step 3: Create Query Extensions

Create `src/api/Core/Extensions/QueryableExtensions.cs`:

```csharp
using System.Linq.Expressions;

namespace DynamisReferenceApp.Api.Core.Extensions;

public static class QueryableExtensions
{
    /// <summary>
    /// Apply pagination to a query.
    /// </summary>
    public static IQueryable<T> Paginate<T>(
        this IQueryable<T> query,
        PaginationParameters parameters)
    {
        return query
            .Skip(parameters.Skip)
            .Take(parameters.PageSize);
    }

    /// <summary>
    /// Apply dynamic sorting to a query.
    /// </summary>
    public static IQueryable<T> ApplySort<T>(
        this IQueryable<T> query,
        string? sortBy,
        bool descending)
    {
        if (string.IsNullOrEmpty(sortBy))
        {
            return query;
        }

        var parameter = Expression.Parameter(typeof(T), "x");
        var property = typeof(T).GetProperty(sortBy,
            System.Reflection.BindingFlags.IgnoreCase |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance);

        if (property == null)
        {
            return query; // Invalid sort field, return unsorted
        }

        var propertyAccess = Expression.MakeMemberAccess(parameter, property);
        var orderByExpression = Expression.Lambda(propertyAccess, parameter);

        var methodName = descending ? "OrderByDescending" : "OrderBy";
        var resultExpression = Expression.Call(
            typeof(Queryable),
            methodName,
            new[] { typeof(T), property.PropertyType },
            query.Expression,
            Expression.Quote(orderByExpression));

        return query.Provider.CreateQuery<T>(resultExpression);
    }

    /// <summary>
    /// Apply search filter to string properties.
    /// </summary>
    public static IQueryable<T> ApplySearch<T>(
        this IQueryable<T> query,
        string? searchTerm,
        params Expression<Func<T, string?>>[] searchProperties)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return query;
        }

        var term = searchTerm.Trim().ToLower();
        var parameter = Expression.Parameter(typeof(T), "x");

        Expression? combinedCondition = null;

        foreach (var propertySelector in searchProperties)
        {
            var propertyBody = propertySelector.Body;
            if (propertyBody is MemberExpression memberExpr)
            {
                var propertyAccess = Expression.MakeMemberAccess(parameter, memberExpr.Member);
                var toLower = Expression.Call(propertyAccess,
                    typeof(string).GetMethod("ToLower", Type.EmptyTypes)!);
                var contains = Expression.Call(toLower,
                    typeof(string).GetMethod("Contains", new[] { typeof(string) })!,
                    Expression.Constant(term));
                var nullCheck = Expression.NotEqual(propertyAccess, Expression.Constant(null));
                var condition = Expression.AndAlso(nullCheck, contains);

                combinedCondition = combinedCondition == null
                    ? condition
                    : Expression.OrElse(combinedCondition, condition);
            }
        }

        if (combinedCondition == null)
        {
            return query;
        }

        var lambda = Expression.Lambda<Func<T, bool>>(combinedCondition, parameter);
        return query.Where(lambda);
    }
}
```

### Step 4: Update Notes Service

Update `src/api/Tools/Notes/Services/NotesService.cs`:

```csharp
public interface INotesService
{
    Task<PaginatedResponse<NoteDto>> GetNotesAsync(string userId, PaginationParameters parameters);
    // ... existing methods
}

public class NotesService : INotesService
{
    public async Task<PaginatedResponse<NoteDto>> GetNotesAsync(
        string userId,
        PaginationParameters parameters)
    {
        _logger.LogInformation(
            "GetNotes: Page={Page}, PageSize={PageSize}, Search={Search}, SortBy={SortBy}",
            parameters.Page, parameters.PageSize, parameters.Search, parameters.SortBy);

        // Build base query
        var query = _context.Notes
            .AsNoTracking()
            .Where(n => n.UserId == userId);

        // Apply soft-delete filter
        if (!parameters.IncludeDeleted)
        {
            query = query.Where(n => !n.IsDeleted);
        }

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            query = query.ApplySearch(
                parameters.Search,
                n => n.Title,
                n => n.Content);
        }

        // Get total count before pagination
        var totalItems = await query.CountAsync();

        // Apply sorting
        var sortBy = parameters.SortBy ?? "UpdatedAt";
        query = query.ApplySort(sortBy, parameters.IsDescending);

        // Apply pagination
        var items = await query
            .Paginate(parameters)
            .Select(n => NoteMapper.ToDto(n))
            .ToListAsync();

        return PaginatedResponse<NoteDto>.Create(
            items,
            parameters.Page,
            parameters.PageSize,
            totalItems);
    }
}
```

### Step 5: Update Notes Function

Update `src/api/Tools/Notes/Functions/NotesFunction.cs`:

```csharp
[Function("GetNotes")]
public async Task<IActionResult> GetNotes(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "notes")]
    HttpRequest req)
{
    var userId = GetUserId(req);

    // Parse query parameters
    var parameters = new PaginationParameters
    {
        Page = ParseQueryInt(req, "page", 1),
        PageSize = ParseQueryInt(req, "pageSize", 20),
        Search = req.Query["search"].FirstOrDefault(),
        SortBy = req.Query["sortBy"].FirstOrDefault(),
        SortDirection = req.Query["sortDir"].FirstOrDefault() ?? "desc",
        IncludeDeleted = ParseQueryBool(req, "includeDeleted", false)
    };

    _logger.LogInformation(
        "GetNotes called by user {UserId} with params: {@Parameters}",
        userId, parameters);

    var result = await _notesService.GetNotesAsync(userId, parameters);
    return new OkObjectResult(result);
}

private static int ParseQueryInt(HttpRequest req, string key, int defaultValue)
{
    var value = req.Query[key].FirstOrDefault();
    return int.TryParse(value, out var result) ? result : defaultValue;
}

private static bool ParseQueryBool(HttpRequest req, string key, bool defaultValue)
{
    var value = req.Query[key].FirstOrDefault();
    return bool.TryParse(value, out var result) ? result : defaultValue;
}
```

---

## Frontend Implementation

### Step 1: Create Types

Create `src/frontend/src/shared/types/pagination.ts`:

```typescript
/**
 * Query parameters for paginated requests
 */
export interface PaginationParams {
  page?: number;
  pageSize?: number;
  search?: string;
  sortBy?: string;
  sortDir?: "asc" | "desc";
  includeDeleted?: boolean;
}

/**
 * Pagination metadata from API response
 */
export interface PaginationMeta {
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  hasNext: boolean;
  hasPrevious: boolean;
}

/**
 * Paginated API response wrapper
 */
export interface PaginatedResponse<T> {
  data: T[];
  pagination: PaginationMeta;
}
```

### Step 2: Update Notes Service

Update `src/frontend/src/tools/notes/services/notesService.ts`:

```typescript
import { apiClient } from "@/core/services/api";
import type { PaginationParams, PaginatedResponse } from "@/shared/types/pagination";
import type { NoteDto, CreateNoteRequest, UpdateNoteRequest } from "../types";

export const notesService = {
  getNotes: async (params: PaginationParams = {}): Promise<PaginatedResponse<NoteDto>> => {
    const queryParams = new URLSearchParams();

    if (params.page) queryParams.set("page", params.page.toString());
    if (params.pageSize) queryParams.set("pageSize", params.pageSize.toString());
    if (params.search) queryParams.set("search", params.search);
    if (params.sortBy) queryParams.set("sortBy", params.sortBy);
    if (params.sortDir) queryParams.set("sortDir", params.sortDir);
    if (params.includeDeleted) queryParams.set("includeDeleted", "true");

    const url = `/notes${queryParams.toString() ? `?${queryParams}` : ""}`;
    const response = await apiClient.get<PaginatedResponse<NoteDto>>(url);
    return response.data;
  },

  // ... existing methods
};
```

### Step 3: Create Pagination Hook

Create `src/frontend/src/shared/hooks/usePagination.ts`:

```typescript
import { useState, useCallback, useMemo } from "react";
import type { PaginationParams, PaginationMeta } from "../types/pagination";

interface UsePaginationOptions {
  initialPage?: number;
  initialPageSize?: number;
  initialSortBy?: string;
  initialSortDir?: "asc" | "desc";
}

interface UsePaginationReturn {
  params: PaginationParams;
  pagination: PaginationMeta | null;
  setPagination: (meta: PaginationMeta) => void;
  setPage: (page: number) => void;
  setPageSize: (pageSize: number) => void;
  setSearch: (search: string) => void;
  setSort: (sortBy: string, sortDir?: "asc" | "desc") => void;
  nextPage: () => void;
  prevPage: () => void;
  resetPagination: () => void;
}

export const usePagination = (options: UsePaginationOptions = {}): UsePaginationReturn => {
  const {
    initialPage = 1,
    initialPageSize = 20,
    initialSortBy,
    initialSortDir = "desc",
  } = options;

  const [page, setPageState] = useState(initialPage);
  const [pageSize, setPageSizeState] = useState(initialPageSize);
  const [search, setSearchState] = useState("");
  const [sortBy, setSortByState] = useState<string | undefined>(initialSortBy);
  const [sortDir, setSortDirState] = useState<"asc" | "desc">(initialSortDir);
  const [pagination, setPaginationState] = useState<PaginationMeta | null>(null);

  const params = useMemo<PaginationParams>(
    () => ({
      page,
      pageSize,
      search: search || undefined,
      sortBy,
      sortDir,
    }),
    [page, pageSize, search, sortBy, sortDir]
  );

  const setPage = useCallback((newPage: number) => {
    setPageState(Math.max(1, newPage));
  }, []);

  const setPageSize = useCallback((newPageSize: number) => {
    setPageSizeState(newPageSize);
    setPageState(1); // Reset to first page when page size changes
  }, []);

  const setSearch = useCallback((newSearch: string) => {
    setSearchState(newSearch);
    setPageState(1); // Reset to first page when search changes
  }, []);

  const setSort = useCallback((newSortBy: string, newSortDir?: "asc" | "desc") => {
    if (sortBy === newSortBy && !newSortDir) {
      // Toggle direction if same field clicked
      setSortDirState((prev) => (prev === "asc" ? "desc" : "asc"));
    } else {
      setSortByState(newSortBy);
      setSortDirState(newSortDir ?? "desc");
    }
    setPageState(1);
  }, [sortBy]);

  const nextPage = useCallback(() => {
    if (pagination?.hasNext) {
      setPageState((prev) => prev + 1);
    }
  }, [pagination?.hasNext]);

  const prevPage = useCallback(() => {
    if (pagination?.hasPrevious) {
      setPageState((prev) => prev - 1);
    }
  }, [pagination?.hasPrevious]);

  const resetPagination = useCallback(() => {
    setPageState(initialPage);
    setPageSizeState(initialPageSize);
    setSearchState("");
    setSortByState(initialSortBy);
    setSortDirState(initialSortDir);
  }, [initialPage, initialPageSize, initialSortBy, initialSortDir]);

  return {
    params,
    pagination,
    setPagination: setPaginationState,
    setPage,
    setPageSize,
    setSearch,
    setSort,
    nextPage,
    prevPage,
    resetPagination,
  };
};
```

### Step 4: Update Notes Hook

Update `src/frontend/src/tools/notes/hooks/useNotes.ts`:

```typescript
import { useState, useEffect, useCallback } from "react";
import { toast } from "react-toastify";
import { notesService } from "../services/notesService";
import { usePagination } from "@/shared/hooks/usePagination";
import type { NoteDto, CreateNoteRequest, UpdateNoteRequest } from "../types";
import type { PaginationParams } from "@/shared/types/pagination";

export const useNotes = () => {
  const [notes, setNotes] = useState<NoteDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const {
    params,
    pagination,
    setPagination,
    setPage,
    setPageSize,
    setSearch,
    setSort,
    nextPage,
    prevPage,
  } = usePagination({
    initialPageSize: 10,
    initialSortBy: "updatedAt",
    initialSortDir: "desc",
  });

  const fetchNotes = useCallback(async (queryParams?: PaginationParams) => {
    try {
      setLoading(true);
      setError(null);
      const result = await notesService.getNotes(queryParams ?? params);
      setNotes(result.data);
      setPagination(result.pagination);
    } catch (err) {
      const message = err instanceof Error ? err.message : "Failed to load notes";
      setError(message);
      toast.error(message);
    } finally {
      setLoading(false);
    }
  }, [params, setPagination]);

  // Refetch when params change
  useEffect(() => {
    fetchNotes();
  }, [fetchNotes]);

  // ... existing CRUD methods

  return {
    notes,
    loading,
    error,
    pagination,
    fetchNotes,
    setPage,
    setPageSize,
    setSearch,
    setSort,
    nextPage,
    prevPage,
    createNote,
    updateNote,
    deleteNote,
  };
};
```

### Step 5: Create Pagination Controls Component

Create `src/frontend/src/shared/components/PaginationControls.tsx`:

```typescript
import {
  Box,
  IconButton,
  Select,
  MenuItem,
  Typography,
  Stack,
} from "@mui/material";
import {
  FirstPage,
  LastPage,
  ChevronLeft,
  ChevronRight,
} from "@mui/icons-material";
import type { PaginationMeta } from "../types/pagination";

interface PaginationControlsProps {
  pagination: PaginationMeta;
  onPageChange: (page: number) => void;
  onPageSizeChange: (pageSize: number) => void;
  pageSizeOptions?: number[];
}

export const PaginationControls: React.FC<PaginationControlsProps> = ({
  pagination,
  onPageChange,
  onPageSizeChange,
  pageSizeOptions = [10, 20, 50, 100],
}) => {
  const { page, pageSize, totalItems, totalPages, hasNext, hasPrevious } = pagination;

  const startItem = (page - 1) * pageSize + 1;
  const endItem = Math.min(page * pageSize, totalItems);

  return (
    <Stack
      direction="row"
      alignItems="center"
      justifyContent="space-between"
      spacing={2}
      sx={{ py: 2 }}
    >
      <Stack direction="row" alignItems="center" spacing={1}>
        <Typography variant="body2">Rows per page:</Typography>
        <Select
          size="small"
          value={pageSize}
          onChange={(e) => onPageSizeChange(Number(e.target.value))}
        >
          {pageSizeOptions.map((size) => (
            <MenuItem key={size} value={size}>
              {size}
            </MenuItem>
          ))}
        </Select>
      </Stack>

      <Typography variant="body2">
        {startItem}-{endItem} of {totalItems}
      </Typography>

      <Stack direction="row" alignItems="center" spacing={0.5}>
        <IconButton
          size="small"
          onClick={() => onPageChange(1)}
          disabled={!hasPrevious}
          aria-label="First page"
        >
          <FirstPage />
        </IconButton>
        <IconButton
          size="small"
          onClick={() => onPageChange(page - 1)}
          disabled={!hasPrevious}
          aria-label="Previous page"
        >
          <ChevronLeft />
        </IconButton>
        <Typography variant="body2" sx={{ mx: 1 }}>
          Page {page} of {totalPages}
        </Typography>
        <IconButton
          size="small"
          onClick={() => onPageChange(page + 1)}
          disabled={!hasNext}
          aria-label="Next page"
        >
          <ChevronRight />
        </IconButton>
        <IconButton
          size="small"
          onClick={() => onPageChange(totalPages)}
          disabled={!hasNext}
          aria-label="Last page"
        >
          <LastPage />
        </IconButton>
      </Stack>
    </Stack>
  );
};
```

### Step 6: Create Search Component

Create `src/frontend/src/shared/components/SearchField.tsx`:

```typescript
import { useState, useEffect, useCallback } from "react";
import { InputAdornment, IconButton } from "@mui/material";
import { Search, Clear } from "@mui/icons-material";
import { CobraTextField } from "@/theme/styledComponents";
import { useDebounce } from "../hooks/useDebounce";

interface SearchFieldProps {
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  debounceMs?: number;
}

export const SearchField: React.FC<SearchFieldProps> = ({
  value,
  onChange,
  placeholder = "Search...",
  debounceMs = 300,
}) => {
  const [localValue, setLocalValue] = useState(value);
  const debouncedValue = useDebounce(localValue, debounceMs);

  useEffect(() => {
    setLocalValue(value);
  }, [value]);

  useEffect(() => {
    if (debouncedValue !== value) {
      onChange(debouncedValue);
    }
  }, [debouncedValue, onChange, value]);

  const handleClear = useCallback(() => {
    setLocalValue("");
    onChange("");
  }, [onChange]);

  return (
    <CobraTextField
      value={localValue}
      onChange={(e) => setLocalValue(e.target.value)}
      placeholder={placeholder}
      size="small"
      InputProps={{
        startAdornment: (
          <InputAdornment position="start">
            <Search />
          </InputAdornment>
        ),
        endAdornment: localValue && (
          <InputAdornment position="end">
            <IconButton size="small" onClick={handleClear} edge="end">
              <Clear />
            </IconButton>
          </InputAdornment>
        ),
      }}
    />
  );
};
```

### Step 7: Update Notes Page

```typescript
import { Box, Stack } from "@mui/material";
import { useNotes } from "../hooks/useNotes";
import { SearchField } from "@/shared/components/SearchField";
import { PaginationControls } from "@/shared/components/PaginationControls";
import CobraStyles from "@/theme/CobraStyles";

export const NotesPage: React.FC = () => {
  const {
    notes,
    loading,
    error,
    pagination,
    setPage,
    setPageSize,
    setSearch,
    setSort,
  } = useNotes();

  return (
    <Box padding={CobraStyles.Padding.MainWindow}>
      <Stack direction="row" justifyContent="space-between" alignItems="center" mb={2}>
        <Typography variant="h5">Notes</Typography>
        <SearchField
          value=""
          onChange={setSearch}
          placeholder="Search notes..."
        />
      </Stack>

      {/* Notes list */}
      {loading ? (
        <CircularProgress />
      ) : error ? (
        <Typography color="error">{error}</Typography>
      ) : (
        <>
          {notes.map((note) => (
            <NoteCard key={note.id} note={note} />
          ))}

          {pagination && (
            <PaginationControls
              pagination={pagination}
              onPageChange={setPage}
              onPageSizeChange={setPageSize}
            />
          )}
        </>
      )}
    </Box>
  );
};
```

---

## Query Parameter Patterns

### Standard Parameters

| Parameter | Type | Description | Example |
|-----------|------|-------------|---------|
| `page` | int | Page number (1-based) | `?page=2` |
| `pageSize` | int | Items per page (max 100) | `?pageSize=50` |
| `search` | string | Full-text search | `?search=meeting` |
| `sortBy` | string | Sort field name | `?sortBy=createdAt` |
| `sortDir` | string | Sort direction | `?sortDir=desc` |

### Filtering Parameters

```
# Exact match
?status=active

# Multiple values (OR)
?status=active,pending

# Range
?createdAfter=2024-01-01&createdBefore=2024-12-31

# Boolean
?isArchived=true
```

### Complex Filtering (Optional)

For advanced filtering, consider a query language:

```
# OData-style
?$filter=status eq 'active' and priority gt 3

# Simple array syntax
?filter[status]=active&filter[priority][gt]=3
```

---

## Performance Considerations

### Database Indexes

Ensure indexes exist for commonly filtered/sorted fields:

```csharp
// In AppDbContext OnModelCreating
modelBuilder.Entity<Note>(entity =>
{
    // Index for user queries with soft-delete filter
    entity.HasIndex(e => new { e.UserId, e.IsDeleted })
        .HasDatabaseName("IX_Notes_UserId_IsDeleted");

    // Index for sorting by date
    entity.HasIndex(e => e.UpdatedAt)
        .HasDatabaseName("IX_Notes_UpdatedAt");

    // Full-text search index (SQL Server specific)
    // CREATE FULLTEXT INDEX ON Notes(Title, Content) ...
});
```

### Count Optimization

For large datasets, counting can be slow:

```csharp
// Option 1: Estimate for very large tables
if (await query.CountAsync() > 10000)
{
    // Use approximate count or skip total
}

// Option 2: Limit count depth
var totalItems = await query.Take(10001).CountAsync();
var hasMore = totalItems > 10000;
if (hasMore) totalItems = -1; // Indicate "10000+"
```

### Cursor-Based Pagination

For better performance with large datasets:

```
GET /api/notes?cursor=eyJpZCI6MTIzfQ&limit=20
```

```csharp
// Cursor-based query
var lastId = DecodeCursor(cursor);
var items = await query
    .Where(n => n.Id > lastId)
    .OrderBy(n => n.Id)
    .Take(limit + 1) // Fetch one extra to check hasMore
    .ToListAsync();
```

---

## Testing

### Backend Tests

```csharp
public class NotesServicePaginationTests
{
    [Fact]
    public async Task GetNotes_Returns_Correct_Page()
    {
        // Arrange: Create 25 notes
        for (int i = 0; i < 25; i++)
        {
            _context.Notes.Add(new Note { Title = $"Note {i}", UserId = "test-user" });
        }
        await _context.SaveChangesAsync();

        var parameters = new PaginationParameters { Page = 2, PageSize = 10 };

        // Act
        var result = await _service.GetNotesAsync("test-user", parameters);

        // Assert
        result.Data.Should().HaveCount(10);
        result.Pagination.Page.Should().Be(2);
        result.Pagination.TotalItems.Should().Be(25);
        result.Pagination.TotalPages.Should().Be(3);
        result.Pagination.HasPrevious.Should().BeTrue();
        result.Pagination.HasNext.Should().BeTrue();
    }

    [Fact]
    public async Task GetNotes_Filters_By_Search()
    {
        // Arrange
        _context.Notes.AddRange(
            new Note { Title = "Meeting notes", UserId = "test-user" },
            new Note { Title = "Shopping list", UserId = "test-user" },
            new Note { Title = "Daily standup meeting", UserId = "test-user" }
        );
        await _context.SaveChangesAsync();

        var parameters = new PaginationParameters { Search = "meeting" };

        // Act
        var result = await _service.GetNotesAsync("test-user", parameters);

        // Assert
        result.Data.Should().HaveCount(2);
        result.Data.All(n => n.Title.Contains("meeting", StringComparison.OrdinalIgnoreCase))
            .Should().BeTrue();
    }

    [Fact]
    public async Task GetNotes_Sorts_Correctly()
    {
        // Arrange
        _context.Notes.AddRange(
            new Note { Title = "C Note", CreatedAt = DateTime.UtcNow.AddDays(-1), UserId = "test-user" },
            new Note { Title = "A Note", CreatedAt = DateTime.UtcNow.AddDays(-3), UserId = "test-user" },
            new Note { Title = "B Note", CreatedAt = DateTime.UtcNow.AddDays(-2), UserId = "test-user" }
        );
        await _context.SaveChangesAsync();

        var parameters = new PaginationParameters { SortBy = "Title", SortDirection = "asc" };

        // Act
        var result = await _service.GetNotesAsync("test-user", parameters);

        // Assert
        result.Data.Select(n => n.Title).Should().BeInAscendingOrder();
    }
}
```

### Frontend Tests

```typescript
describe("usePagination", () => {
  it("initializes with default values", () => {
    const { result } = renderHook(() => usePagination());

    expect(result.current.params.page).toBe(1);
    expect(result.current.params.pageSize).toBe(20);
  });

  it("resets page when search changes", () => {
    const { result } = renderHook(() => usePagination());

    act(() => {
      result.current.setPage(5);
    });
    expect(result.current.params.page).toBe(5);

    act(() => {
      result.current.setSearch("test");
    });
    expect(result.current.params.page).toBe(1);
  });

  it("toggles sort direction when clicking same field", () => {
    const { result } = renderHook(() =>
      usePagination({ initialSortBy: "title", initialSortDir: "asc" })
    );

    act(() => {
      result.current.setSort("title");
    });

    expect(result.current.params.sortDir).toBe("desc");
  });
});
```

---

## Related Documentation

- [EF Core Pagination](https://docs.microsoft.com/ef/core/querying/pagination)
- [REST API Design: Filtering, Sorting, Pagination](https://www.moesif.com/blog/technical/api-design/REST-API-Design-Filtering-Sorting-and-Pagination/)
- [Cursor vs Offset Pagination](https://uxdesign.cc/why-facebook-says-cursor-pagination-is-the-greatest-d6b98d86b6c0)
