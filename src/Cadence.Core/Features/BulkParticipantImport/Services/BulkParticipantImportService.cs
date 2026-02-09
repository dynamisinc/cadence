using System.Collections.Concurrent;
using System.Text;
using Cadence.Core.Data;
using Cadence.Core.Features.BulkParticipantImport.Models.DTOs;
using Cadence.Core.Features.BulkParticipantImport.Models.Entities;
using Cadence.Core.Features.Organizations.Services;
using Cadence.Core.Hubs;
using Cadence.Core.Models.Entities;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cadence.Core.Features.BulkParticipantImport.Services;

/// <summary>
/// Orchestrates the bulk participant import flow: upload, preview, confirm.
/// Manages import sessions and coordinates parsing, classification, and processing.
/// </summary>
public class BulkParticipantImportService : IBulkParticipantImportService
{
    private readonly AppDbContext _context;
    private readonly IParticipantFileParser _parser;
    private readonly IParticipantClassificationService _classificationService;
    private readonly IMembershipService _membershipService;
    private readonly ICurrentOrganizationContext _orgContext;
    private readonly ILogger<BulkParticipantImportService> _logger;

    private static readonly ConcurrentDictionary<Guid, ImportSession> _sessions = new();
    private const int SessionTimeoutMinutes = 30;

    public BulkParticipantImportService(
        AppDbContext context,
        IParticipantFileParser parser,
        IParticipantClassificationService classificationService,
        IMembershipService membershipService,
        ICurrentOrganizationContext orgContext,
        ILogger<BulkParticipantImportService> logger)
    {
        _context = context;
        _parser = parser;
        _classificationService = classificationService;
        _membershipService = membershipService;
        _orgContext = orgContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<FileParseResult> UploadAndParseAsync(Guid exerciseId, Stream fileStream, string fileName)
    {
        if (!_orgContext.HasContext)
            throw new UnauthorizedAccessException("Organization context required");

        // Validate exercise exists and is in Draft or Active status
        var exercise = await _context.Exercises
            .AsNoTracking()
            .Where(e => e.Id == exerciseId && e.OrganizationId == _orgContext.CurrentOrganizationId)
            .FirstOrDefaultAsync();

        if (exercise == null)
            throw new KeyNotFoundException($"Exercise {exerciseId} not found");

        if (exercise.Status != ExerciseStatus.Draft && exercise.Status != ExerciseStatus.Active)
            throw new InvalidOperationException($"Cannot import participants to exercise in {exercise.Status} status. Exercise must be Draft or Active.");

        // Parse the file
        var parseResult = await _parser.ParseAsync(fileStream, fileName);

        // Store session
        var session = new ImportSession
        {
            SessionId = parseResult.SessionId,
            ExerciseId = exerciseId,
            FileName = fileName,
            ParseResult = parseResult,
            CreatedAt = DateTime.UtcNow
        };

        _sessions.TryAdd(session.SessionId, session);

        _logger.LogInformation(
            "Created import session {SessionId} for exercise {ExerciseId} with {RowCount} rows",
            session.SessionId, exerciseId, parseResult.TotalRows);

        return parseResult;
    }

    /// <inheritdoc />
    public async Task<ImportPreviewResult> GetPreviewAsync(Guid exerciseId, Guid sessionId)
    {
        if (!_orgContext.HasContext)
            throw new UnauthorizedAccessException("Organization context required");

        // Get session
        if (!_sessions.TryGetValue(sessionId, out var session))
            throw new KeyNotFoundException($"Import session {sessionId} not found");

        if (session.IsExpired)
        {
            _sessions.TryRemove(sessionId, out _);
            throw new InvalidOperationException("Import session has expired. Please upload the file again.");
        }

        // Validate exercise matches
        if (session.ExerciseId != exerciseId)
            throw new InvalidOperationException("Session does not belong to this exercise");

        // Classify valid rows if not already done
        if (session.ClassifiedRows == null)
        {
            var validRows = session.ParseResult.Rows.Where(r => r.IsValid).ToList();
            var classifiedRows = await _classificationService.ClassifyAsync(exerciseId, validRows);
            session.ClassifiedRows = classifiedRows;
        }

        // Build preview result
        var assignCount = session.ClassifiedRows.Count(r => r.Classification == ParticipantClassification.Assign);
        var updateCount = session.ClassifiedRows.Count(r => r.Classification == ParticipantClassification.Update);
        var inviteCount = session.ClassifiedRows.Count(r => r.Classification == ParticipantClassification.Invite);
        var errorCount = session.ClassifiedRows.Count(r => r.Classification == ParticipantClassification.Error);

        return new ImportPreviewResult
        {
            SessionId = sessionId,
            TotalRows = session.ParseResult.TotalRows,
            AssignCount = assignCount,
            UpdateCount = updateCount,
            InviteCount = inviteCount,
            ErrorCount = errorCount,
            Rows = session.ClassifiedRows
        };
    }

    /// <inheritdoc />
    public async Task<BulkImportResult> ConfirmImportAsync(Guid exerciseId, Guid sessionId, string importingUserId)
    {
        if (!_orgContext.HasContext)
            throw new UnauthorizedAccessException("Organization context required");

        // Get session with classified rows
        if (!_sessions.TryGetValue(sessionId, out var session))
            throw new KeyNotFoundException($"Import session {sessionId} not found");

        if (session.IsExpired)
        {
            _sessions.TryRemove(sessionId, out _);
            throw new InvalidOperationException("Import session has expired. Please upload the file again.");
        }

        if (session.ExerciseId != exerciseId)
            throw new InvalidOperationException("Session does not belong to this exercise");

        if (session.ClassifiedRows == null)
            throw new InvalidOperationException("Import preview has not been generated. Call GetPreviewAsync first.");

        var organizationId = _orgContext.CurrentOrganizationId!.Value;

        // Create BulkImportRecord entity
        var importRecord = new BulkImportRecord
        {
            Id = Guid.NewGuid(),
            ExerciseId = exerciseId,
            ImportedById = importingUserId,
            ImportedAt = DateTime.UtcNow,
            FileName = session.FileName,
            TotalRows = session.ParseResult.TotalRows,
            AssignedCount = 0,
            UpdatedCount = 0,
            InvitedCount = 0,
            ErrorCount = 0,
            SkippedCount = 0
        };

        _context.BulkImportRecords.Add(importRecord);

        var rowOutcomes = new List<ImportRowOutcome>();

        // Process each classified row
        foreach (var classifiedRow in session.ClassifiedRows)
        {
            var row = classifiedRow.ParsedRow;
            var rowStatus = BulkImportRowStatus.Success;
            string? rowMessage = null;

            try
            {
                switch (classifiedRow.Classification)
                {
                    case ParticipantClassification.Assign:
                        await ProcessAssignRowAsync(exerciseId, importingUserId, classifiedRow, importRecord.Id);
                        importRecord.AssignedCount++;
                        rowStatus = BulkImportRowStatus.Success;
                        rowMessage = "Participant assigned to exercise";
                        break;

                    case ParticipantClassification.Update:
                        var updateResult = await ProcessUpdateRowAsync(exerciseId, classifiedRow, importRecord.Id);
                        if (updateResult.WasUpdated)
                        {
                            importRecord.UpdatedCount++;
                            rowStatus = BulkImportRowStatus.Success;
                            rowMessage = $"Role updated from {updateResult.PreviousRole} to {row.ExerciseRole}";
                        }
                        else
                        {
                            importRecord.SkippedCount++;
                            rowStatus = BulkImportRowStatus.Skipped;
                            rowMessage = "No change needed - already assigned with this role";
                        }
                        break;

                    case ParticipantClassification.Invite:
                        await ProcessInviteRowAsync(exerciseId, organizationId, importingUserId, classifiedRow, importRecord.Id);
                        importRecord.InvitedCount++;
                        rowStatus = BulkImportRowStatus.Success;
                        rowMessage = "Organization invitation created with pending exercise assignment";
                        break;

                    case ParticipantClassification.Error:
                        importRecord.ErrorCount++;
                        rowStatus = BulkImportRowStatus.Failed;
                        rowMessage = classifiedRow.ErrorMessage ?? "Validation error";
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing row {RowNumber} (email: {Email})", row.RowNumber, row.Email);
                importRecord.ErrorCount++;
                rowStatus = BulkImportRowStatus.Failed;
                rowMessage = $"Processing failed: {ex.Message}";
            }

            var outcome = new ImportRowOutcome
            {
                RowNumber = row.RowNumber,
                Email = row.Email,
                ExerciseRole = row.ExerciseRole,
                Classification = classifiedRow.Classification,
                Status = rowStatus,
                Message = rowMessage
            };

            rowOutcomes.Add(outcome);

            // Create row result entity
            var rowResult = new BulkImportRowResult
            {
                Id = Guid.NewGuid(),
                BulkImportRecordId = importRecord.Id,
                RowNumber = row.RowNumber,
                Email = row.Email,
                ExerciseRole = row.ExerciseRole,
                DisplayName = row.DisplayName,
                Classification = classifiedRow.Classification,
                Status = outcome.Status,
                ErrorMessage = outcome.Message,
                PreviousExerciseRole = classifiedRow.CurrentExerciseRole?.ToString()
            };

            _context.BulkImportRowResults.Add(rowResult);
        }

        // Save all changes
        await _context.SaveChangesAsync();

        // Clean up session
        _sessions.TryRemove(sessionId, out _);

        _logger.LogInformation(
            "Completed bulk import {ImportRecordId} for exercise {ExerciseId}: {AssignedCount} assigned, {UpdatedCount} updated, {InvitedCount} invited, {ErrorCount} errors, {SkippedCount} skipped",
            importRecord.Id, exerciseId, importRecord.AssignedCount, importRecord.UpdatedCount, importRecord.InvitedCount, importRecord.ErrorCount, importRecord.SkippedCount);

        return new BulkImportResult
        {
            ImportRecordId = importRecord.Id,
            AssignedCount = importRecord.AssignedCount,
            UpdatedCount = importRecord.UpdatedCount,
            InvitedCount = importRecord.InvitedCount,
            ErrorCount = importRecord.ErrorCount,
            SkippedCount = importRecord.SkippedCount,
            RowOutcomes = rowOutcomes
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BulkImportRecordDto>> GetImportHistoryAsync(Guid exerciseId)
    {
        if (!_orgContext.HasContext)
            throw new UnauthorizedAccessException("Organization context required");

        var records = await _context.BulkImportRecords
            .AsNoTracking()
            .Where(r => r.ExerciseId == exerciseId)
            .Include(r => r.ImportedBy)
            .OrderByDescending(r => r.ImportedAt)
            .Select(r => new BulkImportRecordDto
            {
                Id = r.Id,
                ExerciseId = r.ExerciseId,
                ImportedById = r.ImportedById,
                ImportedByName = r.ImportedBy.DisplayName ?? r.ImportedBy.Email ?? "Unknown",
                ImportedAt = r.ImportedAt,
                FileName = r.FileName,
                TotalRows = r.TotalRows,
                AssignedCount = r.AssignedCount,
                UpdatedCount = r.UpdatedCount,
                InvitedCount = r.InvitedCount,
                ErrorCount = r.ErrorCount,
                SkippedCount = r.SkippedCount
            })
            .ToListAsync();

        return records;
    }

    /// <inheritdoc />
    public async Task<BulkImportRecordDto?> GetImportRecordAsync(Guid exerciseId, Guid importRecordId)
    {
        if (!_orgContext.HasContext)
            throw new UnauthorizedAccessException("Organization context required");

        var record = await _context.BulkImportRecords
            .AsNoTracking()
            .Where(r => r.Id == importRecordId && r.ExerciseId == exerciseId)
            .Include(r => r.ImportedBy)
            .Select(r => new BulkImportRecordDto
            {
                Id = r.Id,
                ExerciseId = r.ExerciseId,
                ImportedById = r.ImportedById,
                ImportedByName = r.ImportedBy.DisplayName ?? r.ImportedBy.Email ?? "Unknown",
                ImportedAt = r.ImportedAt,
                FileName = r.FileName,
                TotalRows = r.TotalRows,
                AssignedCount = r.AssignedCount,
                UpdatedCount = r.UpdatedCount,
                InvitedCount = r.InvitedCount,
                ErrorCount = r.ErrorCount,
                SkippedCount = r.SkippedCount
            })
            .FirstOrDefaultAsync();

        return record;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BulkImportRowResultDto>> GetImportRowResultsAsync(Guid importRecordId)
    {
        var results = await _context.BulkImportRowResults
            .AsNoTracking()
            .Where(r => r.BulkImportRecordId == importRecordId)
            .OrderBy(r => r.RowNumber)
            .Select(r => new BulkImportRowResultDto
            {
                Id = r.Id,
                RowNumber = r.RowNumber,
                Email = r.Email,
                ExerciseRole = r.ExerciseRole,
                DisplayName = r.DisplayName,
                Classification = r.Classification,
                Status = r.Status,
                ErrorMessage = r.ErrorMessage,
                PreviousExerciseRole = r.PreviousExerciseRole
            })
            .ToListAsync();

        return results;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PendingExerciseAssignmentDto>> GetPendingAssignmentsAsync(Guid exerciseId)
    {
        if (!_orgContext.HasContext)
            throw new UnauthorizedAccessException("Organization context required");

        var assignments = await _context.PendingExerciseAssignments
            .AsNoTracking()
            .Where(a => a.ExerciseId == exerciseId)
            .Include(a => a.OrganizationInvite)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new PendingExerciseAssignmentDto
            {
                Id = a.Id,
                OrganizationInviteId = a.OrganizationInviteId,
                Email = a.OrganizationInvite.Email ?? "",
                ExerciseRole = a.ExerciseRole.ToString(),
                DisplayName = null,
                Status = a.Status,
                InvitationStatus = a.OrganizationInvite.UsedAt != null ? "Accepted" :
                                   a.OrganizationInvite.ExpiresAt < DateTime.UtcNow ? "Expired" : "Pending",
                InvitationExpiresAt = a.OrganizationInvite.ExpiresAt,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        return assignments;
    }

    /// <inheritdoc />
    public async Task<(byte[] Content, string ContentType, string FileName)> GenerateTemplateAsync(string format)
    {
        format = format.ToLowerInvariant();

        if (format == "csv")
        {
            return GenerateCsvTemplate();
        }
        else if (format == "xlsx")
        {
            return GenerateXlsxTemplate();
        }
        else
        {
            throw new ArgumentException($"Unsupported format: {format}. Must be 'csv' or 'xlsx'.");
        }
    }

    #region Private Helper Methods

    private async Task ProcessAssignRowAsync(Guid exerciseId, string importingUserId, ClassifiedParticipantRow classifiedRow, Guid importRecordId)
    {
        var participant = new ExerciseParticipant
        {
            Id = Guid.NewGuid(),
            ExerciseId = exerciseId,
            UserId = classifiedRow.ExistingUserId!,
            Role = classifiedRow.ParsedRow.NormalizedExerciseRole!.Value,
            AssignedAt = DateTime.UtcNow,
            AssignedById = importingUserId
        };

        _context.ExerciseParticipants.Add(participant);
        await _context.SaveChangesAsync();
    }

    private async Task<(bool WasUpdated, string? PreviousRole)> ProcessUpdateRowAsync(Guid exerciseId, ClassifiedParticipantRow classifiedRow, Guid importRecordId)
    {
        var participant = await _context.ExerciseParticipants
            .Where(p => p.ExerciseId == exerciseId && p.UserId == classifiedRow.ExistingUserId)
            .FirstOrDefaultAsync();

        if (participant == null)
            throw new InvalidOperationException($"Participant {classifiedRow.ParsedRow.Email} not found for update");

        var newRole = classifiedRow.ParsedRow.NormalizedExerciseRole!.Value;

        // Check if role is actually changing
        if (participant.Role == newRole)
        {
            return (false, participant.Role.ToString());
        }

        var previousRole = participant.Role.ToString();
        participant.Role = newRole;
        await _context.SaveChangesAsync();

        return (true, previousRole);
    }

    private async Task ProcessInviteRowAsync(Guid exerciseId, Guid organizationId, string importingUserId, ClassifiedParticipantRow classifiedRow, Guid importRecordId)
    {
        var email = classifiedRow.ParsedRow.Email;
        var exerciseRole = classifiedRow.ParsedRow.NormalizedExerciseRole!.Value;
        var orgRole = classifiedRow.ParsedRow.NormalizedOrgRole ?? OrgRole.OrgUser;

        // Check for existing pending invite
        var existingInvite = await _context.OrganizationInvites
            .Where(i => i.OrganizationId == organizationId
                        && i.Email == email
                        && i.UsedAt == null
                        && i.ExpiresAt > DateTime.UtcNow)
            .FirstOrDefaultAsync();

        if (existingInvite == null)
        {
            // Create new invitation
            existingInvite = new OrganizationInvite
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                Email = email,
                Code = GenerateInviteCode(),
                Role = orgRole,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedByUserId = importingUserId,
                MaxUses = 1,
                UseCount = 0
            };

            _context.OrganizationInvites.Add(existingInvite);
            await _context.SaveChangesAsync();
        }

        // Create PendingExerciseAssignment
        var pendingAssignment = new PendingExerciseAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationInviteId = existingInvite.Id,
            ExerciseId = exerciseId,
            ExerciseRole = exerciseRole,
            Status = PendingAssignmentStatus.Pending,
            BulkImportRecordId = importRecordId
        };

        _context.PendingExerciseAssignments.Add(pendingAssignment);
        await _context.SaveChangesAsync();
    }

    private static string GenerateInviteCode()
    {
        const string chars = "ABCDEFGHJKMNPQRSTUVWXYZ23456789abcdefghjkmnpqrstuvwxyz";
        var bytes = new byte[8];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        var code = new char[8];
        for (int i = 0; i < 8; i++)
            code[i] = chars[bytes[i] % chars.Length];
        return new string(code);
    }

    private (byte[] Content, string ContentType, string FileName) GenerateCsvTemplate()
    {
        var sb = new StringBuilder();

        // Headers
        sb.AppendLine("Email,Exercise Role,Display Name,Organization Role");

        // Example rows
        sb.AppendLine("john.doe@example.com,Controller,John Doe,");
        sb.AppendLine("jane.smith@example.com,Evaluator,Jane Smith,OrgUser");
        sb.AppendLine("bob.wilson@example.com,Observer,Bob Wilson,");

        var content = Encoding.UTF8.GetBytes(sb.ToString());
        return (content, "text/csv", "participant-import-template.csv");
    }

    private (byte[] Content, string ContentType, string FileName) GenerateXlsxTemplate()
    {
        using var workbook = new XLWorkbook();

        // Participants sheet
        var participantsSheet = workbook.Worksheets.Add("Participants");

        // Headers
        participantsSheet.Cell(1, 1).Value = "Email";
        participantsSheet.Cell(1, 2).Value = "Exercise Role";
        participantsSheet.Cell(1, 3).Value = "Display Name";
        participantsSheet.Cell(1, 4).Value = "Organization Role";

        // Format headers
        var headerRange = participantsSheet.Range(1, 1, 1, 4);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        // Example data
        participantsSheet.Cell(2, 1).Value = "john.doe@example.com";
        participantsSheet.Cell(2, 2).Value = "Controller";
        participantsSheet.Cell(2, 3).Value = "John Doe";
        participantsSheet.Cell(2, 4).Value = "";

        participantsSheet.Cell(3, 1).Value = "jane.smith@example.com";
        participantsSheet.Cell(3, 2).Value = "Evaluator";
        participantsSheet.Cell(3, 3).Value = "Jane Smith";
        participantsSheet.Cell(3, 4).Value = "OrgUser";

        participantsSheet.Cell(4, 1).Value = "bob.wilson@example.com";
        participantsSheet.Cell(4, 2).Value = "Observer";
        participantsSheet.Cell(4, 3).Value = "Bob Wilson";
        participantsSheet.Cell(4, 4).Value = "";

        // Auto-fit columns
        participantsSheet.Columns().AdjustToContents();

        // Instructions sheet
        var instructionsSheet = workbook.Worksheets.Add("Instructions");
        instructionsSheet.Cell(1, 1).Value = "Participant Import Instructions";
        instructionsSheet.Cell(1, 1).Style.Font.Bold = true;
        instructionsSheet.Cell(1, 1).Style.Font.FontSize = 14;

        instructionsSheet.Cell(3, 1).Value = "Required Columns:";
        instructionsSheet.Cell(3, 1).Style.Font.Bold = true;
        instructionsSheet.Cell(4, 1).Value = "• Email - Participant's email address (required)";
        instructionsSheet.Cell(5, 1).Value = "• Exercise Role - Role in this exercise (required)";

        instructionsSheet.Cell(7, 1).Value = "Optional Columns:";
        instructionsSheet.Cell(7, 1).Style.Font.Bold = true;
        instructionsSheet.Cell(8, 1).Value = "• Display Name - Full name of the participant";
        instructionsSheet.Cell(9, 1).Value = "• Organization Role - Role for new org members (OrgAdmin, OrgManager, OrgUser)";

        instructionsSheet.Cell(11, 1).Value = "Valid Exercise Roles:";
        instructionsSheet.Cell(11, 1).Style.Font.Bold = true;
        instructionsSheet.Cell(12, 1).Value = "• ExerciseDirector - Overall exercise authority";
        instructionsSheet.Cell(13, 1).Value = "• Controller - Delivers injects and manages scenario flow";
        instructionsSheet.Cell(14, 1).Value = "• Evaluator - Observes and documents player performance";
        instructionsSheet.Cell(15, 1).Value = "• Observer - Watches without interfering";
        instructionsSheet.Cell(16, 1).Value = "• Player - Participant being evaluated";

        instructionsSheet.Cell(18, 1).Value = "Notes:";
        instructionsSheet.Cell(18, 1).Style.Font.Bold = true;
        instructionsSheet.Cell(19, 1).Value = "• For existing organization members, the Organization Role column is ignored";
        instructionsSheet.Cell(20, 1).Value = "• For non-members, an invitation will be sent with the specified org role (defaults to OrgUser)";
        instructionsSheet.Cell(21, 1).Value = "• Delete example rows before importing your data";

        instructionsSheet.Columns().AdjustToContents();

        // Save to memory stream
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var content = stream.ToArray();

        return (content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "participant-import-template.xlsx");
    }

    private class ImportSession
    {
        public Guid SessionId { get; set; }
        public Guid ExerciseId { get; set; }
        public string FileName { get; set; } = null!;
        public FileParseResult ParseResult { get; set; } = null!;
        public IReadOnlyList<ClassifiedParticipantRow>? ClassifiedRows { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsExpired => DateTime.UtcNow - CreatedAt > TimeSpan.FromMinutes(SessionTimeoutMinutes);
    }

    #endregion
}
