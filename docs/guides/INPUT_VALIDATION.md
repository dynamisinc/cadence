# Input Validation Guide

> **Status:** Reference Guide - Not Yet Implemented in Template
> **Priority:** Critical for Production

This guide explains how to implement comprehensive input validation for both backend and frontend.

---

## Table of Contents

1. [Overview](#overview)
2. [Backend Validation with FluentValidation](#backend-validation-with-fluentvalidation)
3. [Frontend Validation with Zod + React Hook Form](#frontend-validation-with-zod--react-hook-form)
4. [Validation Error Responses](#validation-error-responses)
5. [Common Validation Patterns](#common-validation-patterns)
6. [Testing](#testing)

---

## Overview

### Current State

The template has minimal validation:
- EF Core data annotations (`[Required]`, `[MaxLength]`)
- Basic null checks in service layer
- No frontend form validation

### Target State

Comprehensive validation at all boundaries:

```
┌────────────────┐     ┌────────────────┐     ┌────────────────┐
│    Frontend    │────▶│    Backend     │────▶│    Database    │
│  Zod + RHF     │     │ FluentValidation│    │  Constraints   │
│  (UX feedback) │     │  (Security)    │     │  (Data safety) │
└────────────────┘     └────────────────┘     └────────────────┘
```

**Why validate at multiple levels?**
- Frontend: Immediate user feedback
- Backend: Security (never trust client input)
- Database: Data integrity last resort

---

## Backend Validation with FluentValidation

### Step 1: Install Package

```bash
cd src/api
dotnet add package FluentValidation
dotnet add package FluentValidation.DependencyInjectionExtensions
```

### Step 2: Create Validators

Create `src/api/Tools/Notes/Validators/CreateNoteRequestValidator.cs`:

```csharp
using FluentValidation;
using DynamisReferenceApp.Api.Tools.Notes.Models.DTOs;

namespace DynamisReferenceApp.Api.Tools.Notes.Validators;

public class CreateNoteRequestValidator : AbstractValidator<CreateNoteRequest>
{
    public CreateNoteRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
                .WithMessage("Title is required")
                .WithErrorCode("TITLE_REQUIRED")
            .MaximumLength(200)
                .WithMessage("Title cannot exceed 200 characters")
                .WithErrorCode("TITLE_TOO_LONG")
            .Must(NotContainDangerousCharacters)
                .WithMessage("Title contains invalid characters")
                .WithErrorCode("TITLE_INVALID_CHARS");

        RuleFor(x => x.Content)
            .MaximumLength(10000)
                .WithMessage("Content cannot exceed 10,000 characters")
                .WithErrorCode("CONTENT_TOO_LONG");
    }

    private bool NotContainDangerousCharacters(string? value)
    {
        if (string.IsNullOrEmpty(value)) return true;

        // Prevent script injection
        var dangerous = new[] { "<script", "javascript:", "onerror=", "onload=" };
        return !dangerous.Any(d => value.Contains(d, StringComparison.OrdinalIgnoreCase));
    }
}

public class UpdateNoteRequestValidator : AbstractValidator<UpdateNoteRequest>
{
    public UpdateNoteRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
                .WithMessage("Title is required")
                .WithErrorCode("TITLE_REQUIRED")
            .MaximumLength(200)
                .WithMessage("Title cannot exceed 200 characters")
                .WithErrorCode("TITLE_TOO_LONG");

        RuleFor(x => x.Content)
            .MaximumLength(10000)
                .WithMessage("Content cannot exceed 10,000 characters")
                .WithErrorCode("CONTENT_TOO_LONG");
    }
}
```

### Step 3: Create Validation Service

Create `src/api/Core/Validation/ValidationService.cs`:

```csharp
using FluentValidation;
using FluentValidation.Results;

namespace DynamisReferenceApp.Api.Core.Validation;

/// <summary>
/// Centralized validation service for request DTOs.
/// </summary>
public interface IValidationService
{
    Task<ValidationResult> ValidateAsync<T>(T instance);
    Task<(bool IsValid, List<ValidationError> Errors)> ValidateWithErrorsAsync<T>(T instance);
}

public class ValidationService : IValidationService
{
    private readonly IServiceProvider _serviceProvider;

    public ValidationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<ValidationResult> ValidateAsync<T>(T instance)
    {
        var validator = _serviceProvider.GetService<IValidator<T>>();
        if (validator == null)
        {
            // No validator registered, assume valid
            return new ValidationResult();
        }

        return await validator.ValidateAsync(instance);
    }

    public async Task<(bool IsValid, List<ValidationError> Errors)> ValidateWithErrorsAsync<T>(T instance)
    {
        var result = await ValidateAsync(instance);

        if (result.IsValid)
        {
            return (true, new List<ValidationError>());
        }

        var errors = result.Errors
            .Select(e => new ValidationError
            {
                Field = e.PropertyName,
                Message = e.ErrorMessage,
                Code = e.ErrorCode
            })
            .ToList();

        return (false, errors);
    }
}

public class ValidationError
{
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Code { get; set; }
}
```

### Step 4: Register Validators

Update `src/api/Core/Extensions/ServiceCollectionExtensions.cs`:

```csharp
using FluentValidation;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register all validators from assembly
        services.AddValidatorsFromAssemblyContaining<CreateNoteRequestValidator>();

        // Register validation service
        services.AddScoped<IValidationService, ValidationService>();

        // ... existing service registrations
        return services;
    }
}
```

### Step 5: Use in Functions

Update `src/api/Tools/Notes/Functions/NotesFunction.cs`:

```csharp
public class NotesFunction
{
    private readonly INotesService _notesService;
    private readonly IValidationService _validationService;
    private readonly ILogger<NotesFunction> _logger;

    public NotesFunction(
        INotesService notesService,
        IValidationService validationService,
        ILogger<NotesFunction> logger)
    {
        _notesService = notesService;
        _validationService = validationService;
        _logger = logger;
    }

    [Function("CreateNote")]
    public async Task<NoteWithSignalROutput> CreateNote(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "notes")] HttpRequest req,
        FunctionContext context)
    {
        var userId = GetUserId(req);
        _logger.LogInformation("CreateNote called by user {UserId}", userId);

        var request = await ParseRequestBody<CreateNoteRequest>(req);
        if (request == null)
        {
            return new NoteWithSignalROutput
            {
                HttpResponse = new BadRequestObjectResult(new ValidationErrorResponse
                {
                    Message = "Invalid request body",
                    Errors = new List<ValidationError>
                    {
                        new() { Field = "body", Message = "Request body is required", Code = "BODY_REQUIRED" }
                    }
                })
            };
        }

        // Validate request
        var (isValid, errors) = await _validationService.ValidateWithErrorsAsync(request);
        if (!isValid)
        {
            _logger.LogWarning("Validation failed for CreateNote: {Errors}", errors);
            return new NoteWithSignalROutput
            {
                HttpResponse = new BadRequestObjectResult(new ValidationErrorResponse
                {
                    Message = "Validation failed",
                    Errors = errors
                })
            };
        }

        try
        {
            var note = await _notesService.CreateNoteAsync(request, userId);
            return new NoteWithSignalROutput
            {
                HttpResponse = new CreatedResult($"/api/notes/{note.Id}", note),
                SignalRMessage = SignalRBroadcastExtensions.NoteCreated(note.Id, userId)
            };
        }
        catch (ArgumentException ex)
        {
            return new NoteWithSignalROutput
            {
                HttpResponse = new BadRequestObjectResult(new { message = ex.Message })
            };
        }
    }
}

/// <summary>
/// Standard validation error response format.
/// </summary>
public class ValidationErrorResponse
{
    public string Message { get; set; } = "Validation failed";
    public List<ValidationError> Errors { get; set; } = new();
}
```

---

## Frontend Validation with Zod + React Hook Form

### Step 1: Install Packages

```bash
cd src/frontend
npm install zod react-hook-form @hookform/resolvers
```

### Step 2: Create Validation Schemas

Create `src/frontend/src/tools/notes/schemas/noteSchemas.ts`:

```typescript
import { z } from "zod";

/**
 * Schema for creating a new note
 */
export const createNoteSchema = z.object({
  title: z
    .string()
    .min(1, "Title is required")
    .max(200, "Title cannot exceed 200 characters")
    .refine(
      (val) => !/<script|javascript:|onerror=|onload=/i.test(val),
      "Title contains invalid characters"
    ),
  content: z
    .string()
    .max(10000, "Content cannot exceed 10,000 characters")
    .optional()
    .nullable(),
});

/**
 * Schema for updating an existing note
 */
export const updateNoteSchema = z.object({
  title: z
    .string()
    .min(1, "Title is required")
    .max(200, "Title cannot exceed 200 characters"),
  content: z
    .string()
    .max(10000, "Content cannot exceed 10,000 characters")
    .optional()
    .nullable(),
});

// Infer TypeScript types from schemas
export type CreateNoteFormData = z.infer<typeof createNoteSchema>;
export type UpdateNoteFormData = z.infer<typeof updateNoteSchema>;
```

### Step 3: Create Form Components

Create `src/frontend/src/tools/notes/components/NoteForm.tsx`:

```typescript
import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Stack, FormHelperText } from "@mui/material";
import { CobraTextField, CobraPrimaryButton, CobraLinkButton } from "@/theme/styledComponents";
import CobraStyles from "@/theme/CobraStyles";
import { createNoteSchema, CreateNoteFormData } from "../schemas/noteSchemas";

interface NoteFormProps {
  onSubmit: (data: CreateNoteFormData) => Promise<void>;
  onCancel: () => void;
  isLoading?: boolean;
  initialData?: Partial<CreateNoteFormData>;
}

export const NoteForm: React.FC<NoteFormProps> = ({
  onSubmit,
  onCancel,
  isLoading = false,
  initialData,
}) => {
  const {
    control,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<CreateNoteFormData>({
    resolver: zodResolver(createNoteSchema),
    defaultValues: {
      title: initialData?.title ?? "",
      content: initialData?.content ?? "",
    },
  });

  const handleFormSubmit = async (data: CreateNoteFormData) => {
    try {
      await onSubmit(data);
    } catch (error) {
      // Error handling is done by the parent component
      console.error("Form submission error:", error);
    }
  };

  return (
    <form onSubmit={handleSubmit(handleFormSubmit)}>
      <Stack spacing={CobraStyles.Spacing.FormFields}>
        <Controller
          name="title"
          control={control}
          render={({ field }) => (
            <div>
              <CobraTextField
                {...field}
                label="Title"
                fullWidth
                required
                error={!!errors.title}
                disabled={isLoading || isSubmitting}
                inputProps={{ maxLength: 200 }}
              />
              {errors.title && (
                <FormHelperText error>{errors.title.message}</FormHelperText>
              )}
            </div>
          )}
        />

        <Controller
          name="content"
          control={control}
          render={({ field }) => (
            <div>
              <CobraTextField
                {...field}
                value={field.value ?? ""}
                label="Content"
                fullWidth
                multiline
                rows={4}
                error={!!errors.content}
                disabled={isLoading || isSubmitting}
                inputProps={{ maxLength: 10000 }}
              />
              {errors.content && (
                <FormHelperText error>{errors.content.message}</FormHelperText>
              )}
            </div>
          )}
        />

        <Stack direction="row" spacing={2} justifyContent="flex-end">
          <CobraLinkButton onClick={onCancel} disabled={isSubmitting}>
            Cancel
          </CobraLinkButton>
          <CobraPrimaryButton
            type="submit"
            disabled={isLoading || isSubmitting}
          >
            {isSubmitting ? "Saving..." : "Save"}
          </CobraPrimaryButton>
        </Stack>
      </Stack>
    </form>
  );
};
```

### Step 4: Handle Server Validation Errors

Create `src/frontend/src/shared/utils/validationUtils.ts`:

```typescript
import { UseFormSetError, FieldValues, Path } from "react-hook-form";

/**
 * Server validation error format
 */
export interface ServerValidationError {
  field: string;
  message: string;
  code?: string;
}

export interface ServerErrorResponse {
  message: string;
  errors?: ServerValidationError[];
}

/**
 * Maps server validation errors to react-hook-form errors
 */
export function mapServerErrorsToForm<T extends FieldValues>(
  serverErrors: ServerValidationError[],
  setError: UseFormSetError<T>
): void {
  serverErrors.forEach((error) => {
    // Convert PascalCase field names to camelCase
    const fieldName = error.field.charAt(0).toLowerCase() + error.field.slice(1);
    setError(fieldName as Path<T>, {
      type: "server",
      message: error.message,
    });
  });
}

/**
 * Extracts validation errors from an API error response
 */
export function extractServerErrors(error: unknown): ServerValidationError[] {
  if (typeof error === "object" && error !== null) {
    const errorObj = error as Record<string, unknown>;

    // Check for Axios error response
    if (errorObj.response) {
      const response = errorObj.response as { data?: ServerErrorResponse };
      if (response.data?.errors) {
        return response.data.errors;
      }
    }

    // Direct error object
    if (Array.isArray(errorObj.errors)) {
      return errorObj.errors as ServerValidationError[];
    }
  }

  return [];
}
```

### Step 5: Update Form to Handle Server Errors

```typescript
import { mapServerErrorsToForm, extractServerErrors } from "@/shared/utils/validationUtils";

export const NoteForm: React.FC<NoteFormProps> = ({ onSubmit, onCancel }) => {
  const {
    control,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<CreateNoteFormData>({
    resolver: zodResolver(createNoteSchema),
  });

  const handleFormSubmit = async (data: CreateNoteFormData) => {
    try {
      await onSubmit(data);
    } catch (error) {
      // Map server validation errors to form fields
      const serverErrors = extractServerErrors(error);
      if (serverErrors.length > 0) {
        mapServerErrorsToForm(serverErrors, setError);
      } else {
        // Generic error
        setError("root", {
          type: "server",
          message: "An unexpected error occurred. Please try again.",
        });
      }
    }
  };

  return (
    <form onSubmit={handleSubmit(handleFormSubmit)}>
      {errors.root && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {errors.root.message}
        </Alert>
      )}
      {/* ... form fields */}
    </form>
  );
};
```

---

## Validation Error Responses

### Standard Error Response Format

All validation errors should follow this format:

```json
{
  "message": "Validation failed",
  "errors": [
    {
      "field": "title",
      "message": "Title is required",
      "code": "TITLE_REQUIRED"
    },
    {
      "field": "content",
      "message": "Content cannot exceed 10,000 characters",
      "code": "CONTENT_TOO_LONG"
    }
  ]
}
```

### HTTP Status Codes

| Status | Use Case |
|--------|----------|
| `400 Bad Request` | Validation failed |
| `422 Unprocessable Entity` | Alternative for validation (optional) |

---

## Common Validation Patterns

### Email Validation

```csharp
// Backend
RuleFor(x => x.Email)
    .NotEmpty().WithMessage("Email is required")
    .EmailAddress().WithMessage("Invalid email format")
    .MaximumLength(254).WithMessage("Email cannot exceed 254 characters");
```

```typescript
// Frontend
email: z.string()
  .min(1, "Email is required")
  .email("Invalid email format")
  .max(254, "Email cannot exceed 254 characters"),
```

### Phone Number Validation

```csharp
// Backend
RuleFor(x => x.Phone)
    .Matches(@"^\+?[\d\s\-\(\)]{10,20}$")
    .WithMessage("Invalid phone number format")
    .When(x => !string.IsNullOrEmpty(x.Phone));
```

```typescript
// Frontend
phone: z.string()
  .regex(/^\+?[\d\s\-\(\)]{10,20}$/, "Invalid phone number format")
  .optional(),
```

### URL Validation

```csharp
// Backend
RuleFor(x => x.Website)
    .Must(BeAValidUrl).WithMessage("Invalid URL format")
    .When(x => !string.IsNullOrEmpty(x.Website));

private bool BeAValidUrl(string? url)
{
    return Uri.TryCreate(url, UriKind.Absolute, out var result)
           && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
}
```

```typescript
// Frontend
website: z.string()
  .url("Invalid URL format")
  .optional()
  .or(z.literal("")),
```

### Date Validation

```csharp
// Backend
RuleFor(x => x.StartDate)
    .NotEmpty().WithMessage("Start date is required")
    .GreaterThan(DateTime.UtcNow).WithMessage("Start date must be in the future");

RuleFor(x => x.EndDate)
    .GreaterThan(x => x.StartDate).WithMessage("End date must be after start date")
    .When(x => x.EndDate.HasValue);
```

```typescript
// Frontend
startDate: z.coerce.date()
  .min(new Date(), "Start date must be in the future"),
endDate: z.coerce.date().optional(),
}).refine(
  (data) => !data.endDate || data.endDate > data.startDate,
  { message: "End date must be after start date", path: ["endDate"] }
);
```

### Conditional Validation

```csharp
// Backend - Validate address only if shipping is selected
RuleFor(x => x.ShippingAddress)
    .NotEmpty().WithMessage("Shipping address is required")
    .When(x => x.RequiresShipping);
```

```typescript
// Frontend
const orderSchema = z.discriminatedUnion("deliveryType", [
  z.object({
    deliveryType: z.literal("pickup"),
  }),
  z.object({
    deliveryType: z.literal("shipping"),
    shippingAddress: z.string().min(1, "Shipping address is required"),
  }),
]);
```

### Array Validation

```csharp
// Backend
RuleFor(x => x.Tags)
    .Must(tags => tags == null || tags.Count <= 10)
    .WithMessage("Cannot have more than 10 tags");

RuleForEach(x => x.Tags)
    .MaximumLength(50).WithMessage("Each tag cannot exceed 50 characters");
```

```typescript
// Frontend
tags: z.array(z.string().max(50, "Each tag cannot exceed 50 characters"))
  .max(10, "Cannot have more than 10 tags")
  .optional(),
```

---

## Testing

### Backend Validator Tests

Create `src/api.Tests/Validators/CreateNoteRequestValidatorTests.cs`:

```csharp
using FluentAssertions;
using FluentValidation.TestHelper;
using DynamisReferenceApp.Api.Tools.Notes.Models.DTOs;
using DynamisReferenceApp.Api.Tools.Notes.Validators;

namespace DynamisReferenceApp.Api.Tests.Validators;

public class CreateNoteRequestValidatorTests
{
    private readonly CreateNoteRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_When_Valid_Request()
    {
        var request = new CreateNoteRequest
        {
            Title = "Valid Title",
            Content = "Some content"
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_Title_Empty()
    {
        var request = new CreateNoteRequest { Title = "" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorCode("TITLE_REQUIRED");
    }

    [Fact]
    public void Should_Fail_When_Title_Too_Long()
    {
        var request = new CreateNoteRequest
        {
            Title = new string('a', 201)
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorCode("TITLE_TOO_LONG");
    }

    [Theory]
    [InlineData("<script>alert('xss')</script>")]
    [InlineData("javascript:void(0)")]
    [InlineData("test onerror=alert(1)")]
    public void Should_Fail_When_Title_Contains_Dangerous_Chars(string title)
    {
        var request = new CreateNoteRequest { Title = title };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorCode("TITLE_INVALID_CHARS");
    }
}
```

### Frontend Schema Tests

Create `src/frontend/src/tools/notes/schemas/noteSchemas.test.ts`:

```typescript
import { describe, it, expect } from "vitest";
import { createNoteSchema } from "./noteSchemas";

describe("createNoteSchema", () => {
  it("passes with valid data", () => {
    const result = createNoteSchema.safeParse({
      title: "Valid Title",
      content: "Some content",
    });

    expect(result.success).toBe(true);
  });

  it("fails when title is empty", () => {
    const result = createNoteSchema.safeParse({
      title: "",
    });

    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0].message).toBe("Title is required");
    }
  });

  it("fails when title exceeds 200 characters", () => {
    const result = createNoteSchema.safeParse({
      title: "a".repeat(201),
    });

    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0].message).toBe("Title cannot exceed 200 characters");
    }
  });

  it("fails when title contains script tags", () => {
    const result = createNoteSchema.safeParse({
      title: "<script>alert('xss')</script>",
    });

    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0].message).toBe("Title contains invalid characters");
    }
  });

  it("allows null content", () => {
    const result = createNoteSchema.safeParse({
      title: "Valid Title",
      content: null,
    });

    expect(result.success).toBe(true);
  });
});
```

---

## Best Practices Summary

1. **Validate at all layers** - Frontend for UX, backend for security, database for integrity
2. **Use consistent error format** - Same structure for all validation errors
3. **Include error codes** - Enables client-side error handling logic
4. **Keep validation in sync** - Frontend and backend rules should match
5. **Test edge cases** - Empty strings, max lengths, special characters
6. **Sanitize, don't just validate** - Consider escaping dangerous content
7. **Validate early** - Fail fast before processing
8. **Log validation failures** - Helps identify attack patterns

---

## Related Documentation

- [FluentValidation Docs](https://docs.fluentvalidation.net/)
- [Zod Documentation](https://zod.dev/)
- [React Hook Form](https://react-hook-form.com/)
