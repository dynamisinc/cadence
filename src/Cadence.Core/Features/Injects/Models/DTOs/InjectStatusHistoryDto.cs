using Cadence.Core.Models.Entities;

namespace Cadence.Core.Features.Injects.Models.DTOs;

/// <summary>
/// DTO for inject status change history (audit trail).
/// </summary>
public record InjectStatusHistoryDto(
    Guid Id,
    Guid InjectId,
    InjectStatus FromStatus,
    InjectStatus ToStatus,
    string ChangedByUserId,
    string? ChangedByName,
    DateTime ChangedAt,
    string? Notes
);
